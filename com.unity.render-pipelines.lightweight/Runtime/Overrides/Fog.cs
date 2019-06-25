using System;

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
        [Tooltip("The type of fog to use.")]
        public FogTypeParameter type = new FogTypeParameter(FogType.Linear);
        
        [Tooltip("The coloring of the fog.")]
        public FogColorTypeParameter colorType = new FogColorTypeParameter(FogColorType.Color);
        
        public ColorParameter fogColor = new ColorParameter(Color.white, true, false, true);
        public FloatParameter density = new FloatParameter(0.005f);
        
        public FloatParameter nearFog = new FloatParameter(5f);
        public FloatParameter farFog = new FloatParameter(50f);
        
    }

    [Serializable]
    public sealed class FogTypeParameter : VolumeParameter<FogType> { public FogTypeParameter(FogType value, bool overrideState = false) : base(value, overrideState) { } }
    [Serializable]
    public sealed class FogColorTypeParameter : VolumeParameter<FogColorType> { public FogColorTypeParameter(FogColorType value, bool overrideState = false) : base(value, overrideState) { } }
}
