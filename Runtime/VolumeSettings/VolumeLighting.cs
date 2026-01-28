using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public enum VolumeLightingType
    {
        None,
        RayMarching,
        RadioBlur
    }

    [Serializable]
    public class VolumeLightingRayMarchSettings
    {
        [Min(0f)] 
        public float intensity = 1;
        
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
    
    [Serializable]
    public class VolumeLightingRadioBlurSettings
    {
        [Min(0f)] public float intensity = 1;
        [Min(0)] public float clampMax = 10f;
        [Range(0f, 2f)] public float threshold = 1.2f;
    }
    
    [Serializable]
    public class VolumeLightingTypeParameter : VolumeParameter<VolumeLightingType>
    {
        public VolumeLightingTypeParameter(VolumeLightingType value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [Serializable]
    public class VolumeLightingRayMarchModeParameter : VolumeParameter<VolumeLightingRayMarchSettings>
    {
        public VolumeLightingRayMarchModeParameter(VolumeLightingRayMarchSettings value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [Serializable]
    public class VolumeLightingRadioBlurModeParameter : VolumeParameter<VolumeLightingRadioBlurSettings>
    {
        public VolumeLightingRadioBlurModeParameter(VolumeLightingRadioBlurSettings value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [Serializable, VolumeComponentMenuForRenderPipeline("Roxami/VolumeLighting", typeof(UniversalRenderPipeline))]
    public class VolumeLighting : VolumeComponent, IPostProcessComponent
    {
        public VolumeLightingTypeParameter type = new VolumeLightingTypeParameter(VolumeLightingType.None, false);
        
        public VolumeLightingRayMarchModeParameter rayMarchSettings = new VolumeLightingRayMarchModeParameter(new VolumeLightingRayMarchSettings());
        
        public VolumeLightingRadioBlurModeParameter radioBlurSettings = new VolumeLightingRadioBlurModeParameter(new VolumeLightingRadioBlurSettings());

        public bool IsActive()
        {
            return type.value != VolumeLightingType.None;
        }

        public bool IsTileCompatible() => false;
    }
}