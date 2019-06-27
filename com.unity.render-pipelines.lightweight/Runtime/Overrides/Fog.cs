using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.LWRP
{
    public enum FogType
    {
        Off,
        Linear,
        Exp2,
        Height,
    }

    public enum FogColorType
    {
        Color = 0,
        //Gradient = 1,
        CubeMap = 2,
    }

    [Serializable, VolumeComponentMenu("Environment/Fog")]
    public sealed class Fog : VolumeComponent
    {
        // Density type
        public FogTypeParameter type = new FogTypeParameter(FogType.Off);

        // Exp fog
        public FloatParameter density = new FloatParameter(0.005f);

        // Linear fog
        public FloatParameter nearFog = new FloatParameter(5f);
        public FloatParameter farFog = new FloatParameter(50f);

        // Height fog
        public FloatParameter heightOffset = new FloatParameter(0.0f);

        // Coloring
        public FogColorTypeParameter colorType = new FogColorTypeParameter(FogColorType.Color);

        public ColorParameter fogColor = new ColorParameter(Color.white, true, false, true);
        public CubemapParameter cubemap = new CubemapParameter(null);
        public ClampedFloatParameter rotation = new ClampedFloatParameter(0f, -180f, 180);
        public ClampedFloatParameter exposure = new ClampedFloatParameter(1f, 0.01f, 10f);
    }

    [Serializable]
    public sealed class FogTypeParameter : VolumeParameter<FogType> { public FogTypeParameter(FogType value, bool overrideState = false) : base(value, overrideState) { } }
    [Serializable]
    public sealed class FogColorTypeParameter : VolumeParameter<FogColorType> { public FogColorTypeParameter(FogColorType value, bool overrideState = false) : base(value, overrideState) { } }
}
