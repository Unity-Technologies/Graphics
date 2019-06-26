using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.LWRP
{
    public enum FogType
    {
        Linear,
        Exp2,
        Height,
    }

    public enum FogColorType
    {
        Color,
        Gradient,
        CubeMap,
    }

    [Serializable, VolumeComponentMenu("Environment/Fog")]
    public sealed class Fog : VolumeComponent
    {
        // Density type
        public FogTypeParameter type = new FogTypeParameter(FogType.Linear);

        // Exp fog
        public FloatParameter density = new FloatParameter(0.005f);

        // Linear fog
        public FloatParameter nearFog = new FloatParameter(5f);
        public FloatParameter farFog = new FloatParameter(50f);

        // Height fog
        public FloatParameter height = new FloatParameter(0.5f);
        public FloatParameter heightFalloff = new FloatParameter(50f);
        public FloatParameter distanceOffset = new FloatParameter(5f);
        public FloatParameter distanceFalloff = new FloatParameter(25f);

        // Coloring
        public FogColorTypeParameter colorType = new FogColorTypeParameter(FogColorType.Color);

        public ColorParameter fogColor = new ColorParameter(Color.white, true, false, true);
        public NoInterpCubemapParameter cubemap = new NoInterpCubemapParameter(null);
    }

    [Serializable]
    public sealed class FogTypeParameter : VolumeParameter<FogType> { public FogTypeParameter(FogType value, bool overrideState = false) : base(value, overrideState) { } }
    [Serializable]
    public sealed class FogColorTypeParameter : VolumeParameter<FogColorType> { public FogColorTypeParameter(FogColorType value, bool overrideState = false) : base(value, overrideState) { } }
}
