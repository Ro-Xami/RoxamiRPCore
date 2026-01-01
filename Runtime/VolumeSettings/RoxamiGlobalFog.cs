using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public enum RoxamiFogMode
    {
        None,
        Linear,
        EXP,
        EXP2
    }

    [Serializable]
    public class RoxamiFogModeParameter : VolumeParameter<RoxamiFogMode>
    {
        public RoxamiFogModeParameter(RoxamiFogMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [Serializable, VolumeComponentMenuForRenderPipeline("Roxami/RoxamiGlobalFog", typeof(UniversalRenderPipeline))]
    public class RoxamiGlobalFog : VolumeComponent, IPostProcessComponent
    {
        public RoxamiFogModeParameter fogMode = new RoxamiFogModeParameter(RoxamiFogMode.None);
        public ColorParameter fogColor = new ColorParameter(Color.white);
        public ClampedFloatParameter density = new ClampedFloatParameter(0.01f, 0.0f, 0.1f);
        public MinFloatParameter fogStart = new MinFloatParameter(10, 0);
        public MinFloatParameter fogEnd = new MinFloatParameter(20, 0);

        public bool IsActive() => fogMode.value != RoxamiFogMode.None;

        public bool IsTileCompatible() => false;
    }
}