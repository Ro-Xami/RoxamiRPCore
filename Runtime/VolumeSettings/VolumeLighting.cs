using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable]
    public class VolumeLightingRayMarchSettings
    {
        [Min(0f)] 
        public float intensity = 0;
        
        [Min(0f)] 
        public float power = 1f;
        
        [ColorUsage(false)] 
        public Color volumeLightColor = Color.white;
        
        [Range(0, 4)] 
        public int downSample = 1;
        
        [Min(0)]
        public int maxStep = 100;
        
        [Range(0.0f, 0.1f)] 
        public float stepSize = 0.1f;
        
        [Min(0f)]
        public float maxRayLength = 100f;
        
        [Range(0f, 100f)]
        public float randomStrength = 0.1f;
        
       public BlurSettings blurSettings = new BlurSettings();
    }
    
    [Serializable, VolumeComponentMenuForRenderPipeline("Roxami/VolumeLighting", typeof(UniversalRenderPipeline))]
    public class VolumeLighting : VolumeComponent, IPostProcessComponent
    {
        public MinFloatParameter density = new MinFloatParameter(1.0f, 0.0f);
        
        public BoolParameter enableDirectionalLights = new BoolParameter(false);
        
        public BoolParameter enableAdditionalLights = new BoolParameter(false);
        
        // public ClampedFloatParameter additionalLightDistancePower = new ClampedFloatParameter(1f, 0f, 1f);
        
        // public MinFloatParameter fadeStart = new MinFloatParameter(0f, 0f, true);
        //
        // public MinFloatParameter fadeLength = new MinFloatParameter(1f, 0f);

        public ClampedIntParameter downSample = new ClampedIntParameter(1, 0, 4);

        public MinIntParameter maxStep = new MinIntParameter(100, 0);

        public ClampedFloatParameter stepSize = new ClampedFloatParameter(0.1f, 0.0f, 0.1f);

        public MinFloatParameter maxRayLength = new MinFloatParameter(100f, 0f);

        public ClampedFloatParameter randomStrength = new ClampedFloatParameter(1f, 0.0f, 100f);
        
        public RoxamiBlurVolumeSettings blurSettings = new RoxamiBlurVolumeSettings(new BlurSettings());

        public bool IsActive()
        {
            return density.value > 0f && (enableDirectionalLights.value || enableAdditionalLights.value);
        }

        public bool IsTileCompatible() => false;
    }
}