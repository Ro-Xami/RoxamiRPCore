using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable]
    public class PixelStyleSettings : RoxamiCoreFeatureSettingsBase
    {
        [Range(0, 1)] public float downSample = 0.5f;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
    }
    
    public class PixelStyleRenderPass : ScriptableRenderPass
    {
        public PixelStyleRenderPass(PixelStyleSettings settings)
        {
            this.renderPassEvent = settings.renderPassEvent;
            this.settings = settings;
            profilingSampler = new ProfilingSampler(bufferName);
        }

        private readonly PixelStyleSettings settings;
        
        const string bufferName = "PixelStyle";
        private CommandBuffer cmd;

        private const string rtName = "PixelStyleDownSampleRT";
        private RTHandle downSampleRT;
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            cmd = CommandBufferPool.Get(bufferName);
            using (new ProfilingScope(cmd, profilingSampler))
            {
                var colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.width = Mathf.Max(2, (int)(descriptor.width * settings.downSample));
                descriptor.height = Mathf.Max(2, (int)(descriptor.height * settings.downSample));
                descriptor.depthBufferBits = 0;

                RenderingUtils.ReAllocateIfNeeded(ref downSampleRT, descriptor, FilterMode.Point, name: rtName);
                cmd.Blit(colorTarget, downSampleRT);
                cmd.Blit(downSampleRT, colorTarget);
            }
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            downSampleRT?.Release();
        }
    }
    
}