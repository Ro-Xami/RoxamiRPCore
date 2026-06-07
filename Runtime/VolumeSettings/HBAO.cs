using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Roxami/HBAO", typeof(UniversalRenderPipeline))]
    public class HBAO : VolumeComponent, IPostProcessComponent
    {
        public MinFloatParameter intensity = new MinFloatParameter(1f, 0f);
        
        public ClampedFloatParameter directionalIntensity = new ClampedFloatParameter(0f, 0f, 1f);
        
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.5f, 0f, 1f);
        
        public MinFloatParameter maxStepSize = new MinFloatParameter(10.0f, 0f);
        
        public ClampedFloatParameter angleBias = new ClampedFloatParameter(0.1f, 0f, 1f);
        
        public bool IsActive()
        {
            return intensity.value != 0f;
        }

        public bool IsTileCompatible() => false;
    }
}