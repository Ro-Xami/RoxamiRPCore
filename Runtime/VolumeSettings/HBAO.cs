using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Roxami/HBAO", typeof(UniversalRenderPipeline))]
    public class HBAO : VolumeComponent, IPostProcessComponent
    {
        public ClampedIntParameter downsample = new ClampedIntParameter(0, 0, 4);

        public RoxamiBlurVolumeSettings blurSettings = new RoxamiBlurVolumeSettings(new BlurSettings());

        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);
        
        public ClampedFloatParameter directionalIntensity = new ClampedFloatParameter(1f, 0f, 1f);
        
        public ClampedFloatParameter inDirectionalIntensity = new ClampedFloatParameter(1f, 0f, 1f);
        
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.5f, 0f, 1f);
        
        public MinFloatParameter maxStepSize = new MinFloatParameter(10.0f, 0f);
        
        public ClampedFloatParameter angleBias = new ClampedFloatParameter(0.1f, 0f, 1f);
        
        public bool IsActive()
        {
            return intensity.value > 0f;
        }

        public bool IsTileCompatible() => false;
    }
}
