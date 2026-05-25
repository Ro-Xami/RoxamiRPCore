using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable]
    public abstract class RoxamiCoreFeatureSettingsBase
    {
        public bool isActive = true;
    }
    
    public class RoxamiCoreFeature : ScriptableRendererFeature
    {
        //Settings
        [SerializeField] private HBAoSettings hbaoSettings;
        [SerializeField] private ActorForwardSettings actorForwardSettings;
        [SerializeField] private PixelStyleSettings pixelSettings;

        //Passes
        private static readonly InitializedPass initializedPass = new InitializedPass(RenderPassEvent.BeforeRendering);
        
        private HBAoRenderPass hbaoRenderPass;
        private readonly BlurRenderPass blurRenderPass = new BlurRenderPass();//use for hbao
        
        private ActorForwardRenderPass actorForwardRenderPass;
        
        private GlobalFogRenderPass globalFogRenderPass;
        
        private PixelStyleRenderPass pixelStyleRenderPass;
        
        public override void Create()
        {
            hbaoRenderPass = new HBAoRenderPass(hbaoSettings, blurRenderPass, RenderPassEvent.AfterRenderingGbuffer);
            
            actorForwardRenderPass = new ActorForwardRenderPass(actorForwardSettings, RenderPassEvent.AfterRenderingOpaques);
            
            globalFogRenderPass = new GlobalFogRenderPass(RenderPassEvent.AfterRenderingSkybox);
            
            pixelStyleRenderPass = new PixelStyleRenderPass(pixelSettings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(initializedPass);
            
            if (hbaoRenderPass != null && hbaoSettings.isActive)
            {
                renderer.EnqueuePass(hbaoRenderPass);
            }
            
            if (actorForwardRenderPass != null && actorForwardSettings.isActive)
            {
                renderer.EnqueuePass(actorForwardRenderPass);
            }
            
            if (globalFogRenderPass != null)
            {
                renderer.EnqueuePass(globalFogRenderPass);
            }

            if (pixelStyleRenderPass != null && pixelSettings.isActive)
            {
                renderer.EnqueuePass(pixelStyleRenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            hbaoRenderPass?.Dispose();
            actorForwardRenderPass?.Dispose();
            globalFogRenderPass?.Dispose();
            pixelStyleRenderPass?.Dispose();
        }

        /// <summary>
        /// 用于初始化的Pass
        /// </summary>
        private class InitializedPass : ScriptableRenderPass
        {
            public InitializedPass(RenderPassEvent renderPassEvent)
            {
                this.renderPassEvent = renderPassEvent;
                profilingSampler = new ProfilingSampler(bufferName);
            }

            private const string bufferName = "Initialized Pass";
            private CommandBuffer cmd;
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                cmd = CommandBufferPool.Get(bufferName);
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    cmd.DisableShaderKeyword(RoxamiShaderConst.hbaoKeyword);
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            public void Dispose()
            {
                
            }
        }
    }
}