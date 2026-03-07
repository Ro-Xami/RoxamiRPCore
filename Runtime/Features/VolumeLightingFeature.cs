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
        
        // RadioBlurPass radioBlurPass;
        // Material radioBlurMaterial;
        //
        // [Serializable]
        // class RadioBlurSettings
        // {
        //     [Range(2, 12)]
        //     public int sampleCount = 6;
        //     
        //     [Range(1, 6)]
        //     public int blurIterations = 3;
        //     
        //     [Min(1f)]
        //     public float blurSize = 1f;
        //     
        // }
        
        public override void Create()
        {
            rayMarchPass = new RayMarchPass(computeShader, renderPassEvent);
            
            //CreateRadioBlurTypePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            //if (!IsGameOrSceneCamera(renderingData.cameraData.camera)) return;
#endif
            AddTypePass(renderer, renderingData);
        }

        protected override void Dispose(bool disposing)
        {
            //radioBlurPass?.Dispose();
            rayMarchPass?.Dispose();
        }
        
        private void AddTypePass(ScriptableRenderer renderer, RenderingData renderingData)
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<VolumeLighting>();
            if (!volumeSettings || !volumeSettings.IsActive())
                return;
            
            switch (volumeSettings.type.value)
            {
                case VolumeLightingType.RayMarching:
                    if (rayMarchPass != null)
                    {
                        rayMarchPass.UpdateSettings(volumeSettings.rayMarchSettings.value);
                        renderer.EnqueuePass(rayMarchPass);
                    }
                    break;
                
                case VolumeLightingType.RadioBlur:
                    // if (radioBlurPass != null && radioBlurSettings is not { blurIterations: < 1 })
                    // {
                    //     radioBlurPass.UpdateSettings(); = volumeSettings;
                    //     renderer.EnqueuePass(radioBlurPass);
                    // }
                    break;
            }
        }
        
        // private void CreateRadioBlurTypePass()
        // {
        //     var shader = Shader.Find("RoXamiRP/Hide/RadioBlurVolumeLighting");
        //     if (!shader) return;
        //     
        //     radioBlurMaterial = CoreUtils.CreateEngineMaterial(shader);
        //     
        //     if (radioBlurSettings == null) return;
        //
        //     radioBlurPass = new RadioBlurPass(radioBlurSettings, radioBlurMaterial);
        // }
        
        //================================================================================//
        //////////////////////////////////  RadioBlur  /////////////////////////////////////
        //================================================================================//

        
        #region RadioBlur
        // class RadioBlurPass : ScriptableRenderPass
        // {
        //     private const float maxVoL = 0.5f;
        //     private readonly RadioBlurSettings settings;
        //     readonly Material m_Material;
        //     private float VoL = -1f;
        //     public VolumeLighting volumeSettings;
        //
        //     private const string bufferName = "VolumeLighting";
        //     private CommandBuffer cmd;
        //
        //     private readonly int volumeLightIntensityID = Shader.PropertyToID("_VolumeLighting_RadioBlur_Intensity");
        //     private readonly int volumeLightingRadioBlurBlurParamsID = Shader.PropertyToID("_VolumeLighting_RadioBlur_BlurParams");
        //     private readonly int volumeLightBlurTexelSizeID = Shader.PropertyToID("_VolumeLighting_TexelSize");
        //
        //     private const string radioBlurVolumeLightingRtAName = "_VolumeLightingRadioBlurTextureA";
        //     private const string radioBlurVolumeLightingRtBName = "_VolumeLightingRadioBlurTextureB";
        //     // static readonly int radioBlurVolumeLightingRtAID = Shader.PropertyToID(radioBlurVolumeLightingRtAName);
        //     // static readonly int radioBlurVolumeLightingRtBID = Shader.PropertyToID(radioBlurVolumeLightingRtBName);
        //     private RTHandle radioBlurVolumeLightingRtA;
        //     private RTHandle radioBlurVolumeLightingRtB;
        //     
        //     public RadioBlurPass(RadioBlurSettings settings, Material material)
        //     {
        //         renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        //         this.settings = settings;
        //         m_Material = material;
        //         
        //         profilingSampler = new ProfilingSampler(bufferName);
        //     }
        //
        //     public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        //     {
        //         CalculateVoL(renderingData, out Vector3 mainLightDir, out VoL);
        //         
        //         if (!(VoL > maxVoL)) return;
        //
        //         cmd = CommandBufferPool.Get(bufferName);
        //         
        //         using (new ProfilingScope(cmd, profilingSampler))
        //         {
        //             Render(renderingData, mainLightDir, VoL);
        //         }
        //         
        //         context.ExecuteCommandBuffer(cmd);
        //         cmd.Clear();
        //         
        //         CommandBufferPool.Release(cmd);
        //     }
        //
        //     public void Dispose()
        //     {
        //         radioBlurVolumeLightingRtA?.Release();
        //         radioBlurVolumeLightingRtB?.Release();
        //     }
        //     
        //     void Render(RenderingData renderingData, Vector3 mainLightDir, float VoL)
        //     {
        //         cmd.SetGlobalTexture(RoxamiShaderConst.cameraDepthTextureID, renderingData.cameraData.renderer.cameraDepthTargetHandle);
        //         DrawDontCareStore(cmd, 
        //             renderingData.renderer.GetCameraColorBufferRT(), radioBlurVolumeLightingRtA,
        //             m_Material, 0);
        //         
        //         var cameraData = renderingData.cameraData;
        //
        //         RoXamiRTHandlePool.GetRTHandleIfNeeded(
        //             ref radioBlurVolumeLightingRtA, 
        //             cameraData.cameraColorDescriptor, cameraData.cameraColorFilterMode, radioBlurVolumeLightingRtAName);
        //         
        //         RoXamiRTHandlePool.GetRTHandleIfNeeded(
        //             ref radioBlurVolumeLightingRtB, 
        //             cameraData.cameraColorDescriptor, cameraData.cameraColorFilterMode, radioBlurVolumeLightingRtBName);
        //         
        //         Vector2 blurUV = GetDirectionalLightScreenUV(renderingData.cameraData.camera, mainLightDir);
        //         cmd.SetGlobalVector(volumeLightingRadioBlurBlurParamsID, 
        //             new Vector4(settings.sampleCount, settings.blurSize, blurUV.x, blurUV.y));
        //
        //         float intensity = VoL * volumeSettings.radioBlurSettings.intensity;
        //         intensity *= intensity;
        //         cmd.SetGlobalFloat(volumeLightIntensityID, Mathf.SmoothStep(0, maxVoL, intensity));
        //         
        //         int width = renderingData.cameraData.width;
        //         int height = renderingData.cameraData.height;
        //         cmd.SetGlobalVector(volumeLightBlurTexelSizeID,
        //             new Vector4(width, height, 1 / (float)width, 1 / (float)height));
        //         
        //         for (int i = 0; i < settings.blurIterations; i++)
        //         {
        //             bool isAB = i % 2 == 0;
        //             bool isFinalDraw = i == settings.blurIterations - 1;
        //             
        //             DrawDontCareStore(cmd, 
        //                 //from
        //                 isAB? radioBlurVolumeLightingRtA : radioBlurVolumeLightingRtB, 
        //                 //to
        //                 isFinalDraw? renderingData.renderer.GetCameraColorBufferRT(): 
        //                 isAB? radioBlurVolumeLightingRtB : radioBlurVolumeLightingRtA, 
        //                 //mat
        //                 m_Material, isFinalDraw ? 2: 1);
        //         }
        //     }
        //     
        //     Vector2 GetDirectionalLightScreenUV(Camera cam, Vector3 lightDirection)
        //     {
        //         Vector3 lightDir = -lightDirection.normalized;
        //         float distance = cam.farClipPlane * 0.5f;
        //         Vector3 worldPos = cam.transform.position + lightDir * distance;
        //         Vector3 screenPos = cam.WorldToViewportPoint(worldPos);
        //         
        //         return new Vector2(screenPos.x, screenPos.y);
        //     }
        //     
        //     void CalculateVoL(RenderingData renderingData, out Vector3 mainLightDir, out float VoL)
        //     {
        //         var mainLightIndex = renderingData.lightData.mainLightIndex;
        //         if (mainLightIndex == -1)
        //         {
        //             VoL = -1;
        //             mainLightDir = Vector3.zero;
        //             return;
        //         }
        //         
        //         var mainLight = renderingData.lightData.visibleLights[mainLightIndex].light;
        //         
        //         mainLightDir = mainLight.transform.forward.normalized;
        //         Vector3 cameraDir = renderingData.cameraData.camera.transform.forward.normalized;
        //
        //         VoL = -Vector3.Dot(cameraDir, mainLightDir);
        //     }
        //     
        // }
        #endregion

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
            private readonly int volumeLightBlurTexelSizeID = Shader.PropertyToID("_VolumeLighting_TexelSize");
            
            private VolumeLightingRayMarchSettings m_RayMarchSettings;

            private readonly BlurRenderPass m_BlurRenderPass = new BlurRenderPass();
            
            private const string bufferName = "VolumeLighting";
            private CommandBuffer cmd;

            public void UpdateSettings(VolumeLightingRayMarchSettings rayMarchSettings)
            {
                m_RayMarchSettings = rayMarchSettings;
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // !material || 
                if (m_RayMarchSettings == null ||!m_ComputeShader || m_Kernel < 0) 
                    return;

                cmd = CommandBufferPool.Get(bufferName);
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
                m_RenderTextureDescriptor.colorFormat = RenderTextureFormat.RFloat;
                int width = m_RenderTextureDescriptor.width = Mathf.Max(2, m_RenderTextureDescriptor.width >> m_RayMarchSettings.downSample);
                int height = m_RenderTextureDescriptor.height = Mathf.Max(2, m_RenderTextureDescriptor.height >> m_RayMarchSettings.downSample);

                RenderingUtils.ReAllocateIfNeeded(ref volumeLightingRT, m_RenderTextureDescriptor, FilterMode.Bilinear, name: volumeLightingRtName);
                
                cmd.SetRenderTarget(volumeLightingRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                
                cmd.SetComputeVectorParam(m_ComputeShader, volumeLightingParamsID, 
                    new Vector4(m_RayMarchSettings.stepSize, m_RayMarchSettings.maxStep, m_RayMarchSettings.maxRayLength, m_RayMarchSettings.randomStrength));
                
                cmd.SetComputeVectorParam(m_ComputeShader, texelSizeID,
                    new Vector4(width, height, 1 / (float)width, 1 / (float)height));
                
                cmd.SetComputeTextureParam(m_ComputeShader, m_Kernel, RoxamiShaderConst.cameraDepthTextureID, renderingData.cameraData.renderer.cameraDepthTargetHandle);
                cmd.SetComputeTextureParam(m_ComputeShader, m_Kernel, RoxamiShaderConst.mainLightShadowmapTextureID, renderingData.roxamiRenderingData.mainLightShadowmapTexture);
                cmd.SetComputeTextureParam(m_ComputeShader, m_Kernel, volumeLightingRtID, volumeLightingRT);
                    
                int threadGroupX = Mathf.CeilToInt(width / 8.0f);
                int threadGroupY = Mathf.CeilToInt(height / 8.0f);
                cmd.DispatchCompute(m_ComputeShader, m_Kernel, threadGroupX, threadGroupY, 1);
            }

            void DrawBlur(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                m_BlurRenderPass.Setup(cmd, volumeLightingRT, volumeLightingRT, m_RayMarchSettings.blurSettings, m_RenderTextureDescriptor);
                m_BlurRenderPass.Execute(context, ref renderingData);
                
                cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
                cmd.SetGlobalTexture(volumeLightingRtID, volumeLightingRT);
                RoxamiCommonUtils.DrawFullScreenTriangle(cmd, material, 0);
            }
        }
        #endregion
    }
}

