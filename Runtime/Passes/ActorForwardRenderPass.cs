using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable]
    public class ActorForwardSettings : RoxamiCoreFeatureSettingsBase
    {
        public LayerMask layerMask;
        [Min(0)] public float rimOffset;
        [Range(0, 0.01f)] public float rimThreshold;
    }
    
    public class ActorForwardRenderPass : ScriptableRenderPass
    {
        public ActorForwardRenderPass(ActorForwardSettings settings, RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
            this.settings = settings;
            profilingSampler = new ProfilingSampler(bufferName);
        }

        private readonly ActorForwardSettings settings;
        
        private static readonly ShaderTagId actorForwardShaderTagId = new ShaderTagId("ActorForward");
        private static readonly int actorRimParamsID = Shader.PropertyToID("_ActorRimParams");
        
        const string bufferName = "ActorForwardRenderPass";
        private CommandBuffer cmd;
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings == null) return;
            
            cmd = CommandBufferPool.Get(bufferName);
            using (new ProfilingScope(cmd, profilingSampler))
            {
                // cmd.SetGlobalVector(actorRimParamsID, new Vector4(settings.rimOffset, settings.rimThreshold));
                
                var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
                {
                    criteria = SortingCriteria.CommonTransparent,
                };
                
                var drawingSettings = new DrawingSettings(actorForwardShaderTagId, sortingSettings)
                {
                    enableInstancing = true,
                    perObjectData =
                        PerObjectData.Lightmaps |
                        PerObjectData.LightProbe |
                        PerObjectData.ShadowMask |
                        PerObjectData.ReflectionProbes |
                        PerObjectData.LightData |
                        PerObjectData.LightIndices
                };
                
                var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.layerMask);
                
                ExecuteCommandBuffer(context);
                
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            }

            ExecuteCommandBuffer(context);
            CommandBufferPool.Release(cmd);
        }

        void ExecuteCommandBuffer(ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        
        public void Dispose()
        {
            
        }
    }
}