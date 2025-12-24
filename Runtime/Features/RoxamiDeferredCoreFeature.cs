using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [CreateAssetMenu(menuName = "RoxamiRendering/DeferredLights", fileName = "DeferredLights")]
    public class RoxamiDeferredCoreFeature : RoxamiAdditionalRendererData
    {
        [SerializeField] ClusteredLightingSettings settings = new ClusteredLightingSettings();
        private ClusteredLightingPass clusteredLightingPass;

        public override RoxamiDeferredLights CreateDeferredRenderPass()
        {
            if (settings == null) return null;

            clusteredLightingPass = new ClusteredLightingPass(settings);
            return clusteredLightingPass;
        }

        protected override void Dispose(bool disposing)
        {
            clusteredLightingPass?.Dispose();
        }
    }
}