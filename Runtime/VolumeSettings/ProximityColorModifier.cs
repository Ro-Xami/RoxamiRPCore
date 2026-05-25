using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Roxami/ProximityColorModifier", typeof(UniversalRenderPipeline))]
    public class ProximityColorModifier : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isActive = new BoolParameter(false);
        public ColorParameter modifyColor = new ColorParameter(new UnityEngine.Color(1f, 0f, 0f, 1f));
        public MinFloatParameter proximityDistance = new MinFloatParameter(5f, 0f);
        public MinFloatParameter softness = new MinFloatParameter(1f, 0f);

        public bool IsActive() => isActive.value;

        public bool IsTileCompatible() => false;
    }
}
