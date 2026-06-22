using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public class VolumeLightingFeature : ScriptableRendererFeature
    {
        [SerializeField]
        ComputeShader computeShader;
        
        [SerializeField] RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        
        RayMarchPass rayMarchPass;
        
        public override void Create()
        {
            rayMarchPass = new RayMarchPass(computeShader, renderPassEvent);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.SceneView && cameraType != CameraType.Game) 
                return;
#endif
            
            var volumeSettings = VolumeManager.instance.stack.GetComponent<VolumeLighting>();
            if (!volumeSettings || !volumeSettings.IsActive())
                return;
            
            rayMarchPass.UpdateSettings(volumeSettings);
            renderer.EnqueuePass(rayMarchPass);
        }

        protected override void Dispose(bool disposing)
        {
            rayMarchPass?.Dispose();
        }

        //================================================================================//
        //////////////////////////////////  RayMarch   /////////////////////////////////////
        //================================================================================//
        #region RayMarchPass
        class RayMarchPass : ScriptableRenderPass
        {
            public RayMarchPass(ComputeShader computeShader, RenderPassEvent renderPassEvent)
            {
                this.renderPassEvent = renderPassEvent;
                profilingSampler = new ProfilingSampler(bufferName);

                m_ComputeShader = computeShader;
                if (!m_ComputeShader) return;

                m_Kernel = computeShader.FindKernel(kernelName);
            }

            private static Material m_Material;
            private static Material material
            {
                get
                {
                    if (!m_Material)
                    {
                        var shader = Shader.Find(RoxamiShaderConst.volumeLightingShaderName);
                        if (!shader) return null;
                        
                        m_Material = CoreUtils.CreateEngineMaterial(shader);
                    }
                    return m_Material;
                }
            }
            
            private const string kernelName = "VolumeLightingRayMarch";
            private readonly int m_Kernel;
            private readonly ComputeShader m_ComputeShader;

            private RenderTextureDescriptor m_RenderTextureDescriptor;
            private const string volumeLightingRtName = "_VolumeLightingTexture";
            private static readonly int volumeLightingRtID = Shader.PropertyToID(volumeLightingRtName);
            private RTHandle volumeLightingRT;
            
            private readonly int texelSizeID = Shader.PropertyToID("_texelSize");
            private readonly int volumeLightingParamsID = Shader.PropertyToID("_volumeLightingParams");
            private readonly int volumeAdditionalLightDistancePowerID = Shader.PropertyToID("_VolumeAdditionalLightDistancePower");
            private readonly int volumeLightingDensityID = Shader.PropertyToID("_VolumeLightingDensity");
            private readonly int volumeLightingFadeID = Shader.PropertyToID("_VolumeLightingFade");
            private readonly int volumeLightBlurTexelSizeID = Shader.PropertyToID("_VolumeLighting_TexelSize");
            private const string directionalVolumeLightKeyword = "_DIRECTIONAL_VOLUME_LIGHT";
            private const string additionalVolumeLightsKeyword = "_ADDITIONAL_VOLUME_LIGHTS";
            
            private VolumeLighting m_RayMarchSettings;

            private readonly BlurRenderPass m_BlurRenderPass = new BlurRenderPass();
            
            private const string bufferName = "VolumeLighting";
            private CommandBuffer cmd;

            public void UpdateSettings(VolumeLighting rayMarchSettings)
            {
                m_RayMarchSettings = rayMarchSettings;
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_RayMarchSettings == null ||!m_ComputeShader || m_Kernel < 0) 
                    return;

                cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    DrawRayMarch(renderingData);

                    cmd.SetGlobalTexture(volumeLightingRtID, volumeLightingRT);
                    
                    DrawBlur(context, ref renderingData);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                volumeLightingRT?.Release();
                m_BlurRenderPass?.Dispose();
            }

            void DrawRayMarch(RenderingData renderingData)
            {
                m_RenderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                m_RenderTextureDescriptor.depthBufferBits = 0;
                m_RenderTextureDescriptor.enableRandomWrite = true;
                m_RenderTextureDescriptor.colorFormat = RenderTextureFormat.RGB111110Float;
                int width = m_RenderTextureDescriptor.width = Mathf.Max(2, m_RenderTextureDescriptor.width >> m_RayMarchSettings.downSample.value);
                int height = m_RenderTextureDescriptor.height = Mathf.Max(2, m_RenderTextureDescriptor.height >> m_RayMarchSettings.downSample.value);

                RenderingUtils.ReAllocateIfNeeded(ref volumeLightingRT, m_RenderTextureDescriptor, FilterMode.Bilinear, name: volumeLightingRtName);
                
                cmd.SetRenderTarget(volumeLightingRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                
                cmd.SetComputeVectorParam(m_ComputeShader, volumeLightingParamsID, 
                    new Vector4(
                        m_RayMarchSettings.stepSize.value, 
                        m_RayMarchSettings.maxStep.value, 
                        m_RayMarchSettings.maxRayLength.value, 
                        m_RayMarchSettings.randomStrength.value
                        ));
                
                cmd.SetComputeFloatParam(m_ComputeShader, 
                    volumeLightingDensityID, 
                    m_RayMarchSettings.density.value
                    );
                
                // cmd.SetComputeFloatParam(m_ComputeShader, 
                //     volumeAdditionalLightDistancePowerID, 
                //     m_RayMarchSettings.additionalLightDistancePower.value);

                if (m_RayMarchSettings.enableDirectionalLights.value)
                {
                    cmd.EnableShaderKeyword(directionalVolumeLightKeyword);
                }
                else
                {
                    cmd.DisableShaderKeyword(directionalVolumeLightKeyword);
                }

                if (m_RayMarchSettings.enableAdditionalLights.value)
                {
                    cmd.EnableShaderKeyword(additionalVolumeLightsKeyword);
                }
                else
                {
                    cmd.DisableShaderKeyword(additionalVolumeLightsKeyword);
                }
                
                cmd.SetComputeVectorParam(m_ComputeShader, texelSizeID,
                    new Vector4(width, height, 1 / (float)width, 1 / (float)height));
                
                cmd.SetComputeTextureParam(m_ComputeShader, m_Kernel, RoxamiShaderConst.cameraDepthTextureID, renderingData.cameraData.renderer.cameraDepthTargetHandle);
                // cmd.SetComputeTextureParam(m_ComputeShader, m_Kernel, RoxamiShaderConst.mainLightShadowmapTextureID, renderingData.roxamiRenderingData.mainLightShadowmapTexture);
                cmd.SetComputeTextureParam(m_ComputeShader, m_Kernel, volumeLightingRtID, volumeLightingRT);
                    
                int threadGroupX = Mathf.CeilToInt(width / 8.0f);
                int threadGroupY = Mathf.CeilToInt(height / 8.0f);
                cmd.DispatchCompute(m_ComputeShader, m_Kernel, threadGroupX, threadGroupY, 1);
            }

            void DrawBlur(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                m_BlurRenderPass.Setup(cmd, volumeLightingRT, volumeLightingRT, m_RayMarchSettings.blurSettings.value, m_RenderTextureDescriptor);
                m_BlurRenderPass.Execute(context, ref renderingData);
                
                cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
                cmd.SetGlobalTexture(volumeLightingRtID, volumeLightingRT);
                RoxamiCommonUtils.DrawFullScreenTriangle(cmd, material, 0);
            }
        }
        #endregion
    }
}

