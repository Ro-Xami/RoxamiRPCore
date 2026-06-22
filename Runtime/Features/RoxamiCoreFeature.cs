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

        //Passes
        private static readonly InitializedPass initializedPass = new InitializedPass(RenderPassEvent.BeforeRendering);
        
        private HBAoRenderPass hbaoRenderPass;
        
        private readonly BlurRenderPass blurRenderPass = new BlurRenderPass();//use for hbao
        
        private ActorForwardRenderPass actorForwardRenderPass;
        
        private GlobalFogRenderPass globalFogRenderPass;

        private ProximityColorModifierPass proximityColorModifierPass;
        
        
        public override void Create()
        {
            hbaoRenderPass = new HBAoRenderPass(hbaoSettings, blurRenderPass, RenderPassEvent.AfterRenderingGbuffer);
            
            actorForwardRenderPass = new ActorForwardRenderPass(actorForwardSettings, RenderPassEvent.AfterRenderingOpaques);
            
            globalFogRenderPass = new GlobalFogRenderPass(RenderPassEvent.AfterRenderingSkybox);
            
            proximityColorModifierPass = new ProximityColorModifierPass(RenderPassEvent.AfterRenderingTransparents);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var isGameView = renderingData.cameraData.cameraType == CameraType.Game;
            
            var volume = VolumeManager.instance.stack;
            
            renderer.EnqueuePass(initializedPass);

            //HBAO
            var hbaoVolume = volume.GetComponent<HBAO>();
            if (hbaoRenderPass != null && hbaoVolume != null && hbaoVolume.IsActive())
            {
                hbaoRenderPass.UpdateVolume(hbaoVolume);
                renderer.EnqueuePass(hbaoRenderPass);
            }
            
            //DrawActor
            if (actorForwardRenderPass != null && actorForwardSettings.isActive)
            {
                renderer.EnqueuePass(actorForwardRenderPass);
            }
            
            //Fog
            var fogVolume = volume.GetComponent<RoxamiGlobalFog>();
            if (isGameView && globalFogRenderPass != null && fogVolume != null && fogVolume.IsActive())
            {
                globalFogRenderPass.UpdateVolume(fogVolume);
                renderer.EnqueuePass(globalFogRenderPass);
            }

            //ProximityColorModifier
            var proximityColorModifieVolume = volume.GetComponent<ProximityColorModifier>();
            if (isGameView && proximityColorModifierPass != null && proximityColorModifieVolume != null && proximityColorModifieVolume.IsActive())
            {
                proximityColorModifierPass.UpdateVolume(proximityColorModifieVolume);
                renderer.EnqueuePass(proximityColorModifierPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            hbaoRenderPass?.Dispose();
            actorForwardRenderPass?.Dispose();
            globalFogRenderPass?.Dispose();
            proximityColorModifierPass?.Dispose();
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
                cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    GlobalFogRenderPass.DisableKeyword(cmd);
                    HBAoRenderPass.DisableKeyword(cmd);
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
