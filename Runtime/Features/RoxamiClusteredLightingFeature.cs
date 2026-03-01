using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [CreateAssetMenu(menuName = "RoxamiRendering/DeferredLights", fileName = "DeferredLights")]
    public class RoxamiClusteredLightingFeature : RoxamiAdditionalRendererData
    {
        private static RoxamiClusteredLightingFeature m_Instance;
        
        [SerializeField] ClusteredLightingSettings settings = new ClusteredLightingSettings();
        private ClusteredLightingPass clusteredLightingPass;

        public override RoxamiDeferredLights CreateDeferredRenderPass()
        {
            m_Instance = this;
            
            if (settings == null) return null;

            clusteredLightingPass = new ClusteredLightingPass(settings);
            return clusteredLightingPass;
        }

        protected override void Dispose(bool disposing)
        {
            clusteredLightingPass?.Dispose();
        }

        public static bool TryGetInstance(out RoxamiClusteredLightingFeature feature)
        {
            feature = m_Instance;
            
            if (m_Instance != null)
                return true;

            return false;
        }

        public void SetRenderingDebugMode(bool active, RoxamiRenderingDebugOutput output)
        {
            if (settings == null) return;

            settings.isRenderingDebug = active;
            settings.renderingOutput = output;
        }
    }
}