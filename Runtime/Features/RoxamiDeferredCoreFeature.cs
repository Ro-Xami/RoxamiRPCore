using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable]
    public class ClusteredLightingSettings
    {
        public const RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingDeferredLights;
        public ComputeShader computeShader;
        [Range(1, 20)] public int threadGroupX = 10;
        [Range(1, 20)] public int threadGroupY = 10;
        [Range(1, 20)] public int threadGroupZ = 10;
    }

    public class RoxamiDeferredCoreFeature : ScriptableRendererFeature
    {
        [SerializeField] ClusteredLightingSettings settings = new ClusteredLightingSettings();
        private ClusteredLightingPass clusteredLightingPass;
        
        public override void Create()
        {
            clusteredLightingPass = new ClusteredLightingPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings == null || clusteredLightingPass == null) return;
            
            renderer.EnqueuePass(clusteredLightingPass);
        }

        protected override void Dispose(bool disposing)
        {
            clusteredLightingPass?.Dispose();
        }
    }
}