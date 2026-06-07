using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable]
    public class HBAoSettings : RoxamiCoreFeatureSettingsBase
    {
        public ComputeShader computeShader;
            
        [Range(0,4)]
        public int downsample = 0;
        
        // [Min(0.0f)] 
        // public float intensity = 1.0f;
        //
        // [Range(0.0f, 1.0f)] 
        // public float radius = 0.5f;
        //
        // [Min(0.0f)] 
        // public float maxStepSize = 10.0f;
        //
        // [Range(0.0f, 1.0f)] 
        // public float angleBias = 0.1f;
        
        [SerializeField] 
        public BlurSettings blurSettings = new BlurSettings();
    }
    
    public class HBAoRenderPass : ScriptableRenderPass
    {
        public HBAoRenderPass(HBAoSettings settings, BlurRenderPass blurRenderPass, RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
            this.m_HbaoSettings = settings;
            this.m_BlurRenderPass = blurRenderPass;
            
            cs = m_HbaoSettings.computeShader;

            if (!cs) return;
            kernel = cs.FindKernel(kernelName);
            
            m_BlurSettings = m_HbaoSettings.blurSettings;
            if (m_BlurSettings == null) return;

            profilingSampler = new ProfilingSampler(bufferName);
        }

        private const string kernelName = "HBAO";
        private readonly int kernel;
        private readonly ComputeShader cs;

        private const string hbaoRtName = "_HBAoTexture";
        private static readonly int hbaoRtID = Shader.PropertyToID(hbaoRtName);
        private RTHandle hbaoRT;
        
        private static readonly int texelSizeID = Shader.PropertyToID("_texelSize");
        private static readonly int hbaoParamsID = Shader.PropertyToID("_hbaoParams");
        private static readonly int hbaoStepSizeID = Shader.PropertyToID("_stepSize");
        private static readonly int hbaoDirectionalIntensityID = Shader.PropertyToID("_HbaoDirectionalIntensity");

        private RenderTextureDescriptor hbaoDescriptor;

        private const string bufferName = "RoXamiRP HBAO";
        private CommandBuffer cmd;
        private readonly HBAoSettings m_HbaoSettings;
        
        private readonly BlurRenderPass m_BlurRenderPass;
        private readonly BlurSettings m_BlurSettings;

        private HBAO volume;

        public void UpdateVolume(HBAO volume)
        {
            this.volume = volume;
        }

        public static void DisableKeyword(CommandBuffer commandBuffer)
        {
            commandBuffer.DisableShaderKeyword(RoxamiShaderConst.hbaoKeyword);
        }

        public override void Configure(CommandBuffer commandBuffer, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (m_HbaoSettings == null || !cs || kernel < 0) return;
            
            hbaoDescriptor = cameraTextureDescriptor;
            hbaoDescriptor.width = Mathf.Max(2, cameraTextureDescriptor.width >> m_HbaoSettings.downsample);
            hbaoDescriptor.height = Mathf.Max(2, cameraTextureDescriptor.height >> m_HbaoSettings.downsample);
            hbaoDescriptor.colorFormat = RenderTextureFormat.RFloat;
            hbaoDescriptor.depthBufferBits = 0;
            hbaoDescriptor.enableRandomWrite = true;

            RenderingUtils.ReAllocateIfNeeded(ref hbaoRT, hbaoDescriptor, FilterMode.Bilinear, name: hbaoRtName);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_HbaoSettings == null || volume == null || !cs || kernel < 0) return;
            
            cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                ComputeHBAO(renderingData);

                Blur(context, renderingData);

                cmd.EnableShaderKeyword(RoxamiShaderConst.hbaoKeyword);
                cmd.SetGlobalTexture(hbaoRtID, hbaoRT);
            }
            ExecuteCommandBuffer(context);
            
        }

        public void Dispose()
        {
            hbaoRT?.Release();
            
            m_BlurRenderPass?.Dispose();
        }

        void ComputeHBAO(RenderingData renderingData)
        {
            cmd.SetGlobalFloat(hbaoDirectionalIntensityID, volume.directionalIntensity.value);
            var hbaoParams = new Vector4(
                volume.intensity.value, 
                volume.radius.value, 
                volume.maxStepSize.value,
                volume.angleBias.value);
            
            int width = hbaoDescriptor.width;
            int height = hbaoDescriptor.height;

            cmd.SetRenderTarget(hbaoRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            
            cmd.SetComputeVectorParam(cs,
                texelSizeID, new Vector4(width, height, 1f / width, 1f / height));
            var tanHalfFovY = Mathf.Tan(renderingData.cameraData.camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cmd.SetComputeFloatParam(cs, hbaoStepSizeID, 
                renderingData.cameraData.camera.pixelHeight * volume.radius.value * 1.5f / tanHalfFovY / 2.0f);
            cmd.SetComputeVectorParam(cs, hbaoParamsID, hbaoParams);
            cmd.SetComputeTextureParam(cs, kernel,
                RoxamiShaderConst.cameraDepthTextureID, renderingData.cameraData.renderer.cameraDepthTargetHandle);
            cmd.SetComputeTextureParam(cs, kernel,
                hbaoRtID, hbaoRT);

            int threadGroupX = Mathf.CeilToInt(width / 8.0f);
            int threadGroupY = Mathf.CeilToInt(height / 8.0f);
            cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, 1);
            
            cmd.SetGlobalTexture(hbaoRtID, hbaoRT);
        }

        private void Blur(ScriptableRenderContext context, RenderingData renderingData)
        {
            m_BlurRenderPass.Setup(cmd,hbaoRT, hbaoRT, m_BlurSettings, hbaoDescriptor);
            m_BlurRenderPass.Execute(context, ref renderingData);
        }

        void ExecuteCommandBuffer(ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}