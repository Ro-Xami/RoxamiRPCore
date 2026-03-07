using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Roxami/DistanceField", typeof(UniversalRenderPipeline))]
    public class DistanceField : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isActive = new BoolParameter(false);
        
        public RoxamiBlurVolumeSettings blurSettings = new RoxamiBlurVolumeSettings(
            new BlurSettings()
            {
                blurType = BlurType.Gaussian,
                blurRadios = 1.0f,
                iterations = 2,
                downSample = 1,
            }, false);
        
        public MinFloatParameter distance = new MinFloatParameter(5f, 0f);
        public MinFloatParameter distanceRange = new MinFloatParameter(2f, 0f);
        public MinFloatParameter near = new MinFloatParameter(5f, 0f);
        public MinFloatParameter far = new MinFloatParameter(5f, 0f);
        
        public bool IsActive() => isActive.value;

        public bool IsTileCompatible() => false;
    }
}