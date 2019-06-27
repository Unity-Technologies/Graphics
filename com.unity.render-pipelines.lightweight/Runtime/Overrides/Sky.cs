using System;

namespace UnityEngine.Rendering.LWRP
{
    public enum SkyType
    {
        Default,
        Bruneton
    }

    [Serializable, VolumeComponentMenu("Environment/Sky")]
    public sealed class Sky : VolumeComponent
    {
        // Density
        [Tooltip("The type of sky to use.")]
        public SkyTypeParameter type = new SkyTypeParameter(SkyType.Default);
        
        public FloatParameter mieScattering = new FloatParameter(1.0f);
        public FloatParameter raleightScattering = new FloatParameter(1.0f);
        public FloatParameter ozoneDensity = new FloatParameter(1.0f);
        public FloatParameter phase = new FloatParameter(0.0f);
        public FloatParameter fogAmount = new FloatParameter(1.0f);
        public FloatParameter sunSize = new FloatParameter(1.0f);
        public FloatParameter sunEdge = new FloatParameter(1.0f);

        public FloatParameter exposure = new FloatParameter(10.0f);
        
        // Coloring
        [Tooltip("The coloring of the fog.")]
        public FogColorTypeParameter colorType = new FogColorTypeParameter(FogColorType.Color);
        //public CubemapParameter cubemap = new CubemapParameter();
    }

    [Serializable]
    public sealed class SkyTypeParameter : VolumeParameter<SkyType> { public SkyTypeParameter(SkyType value, bool overrideState = false) : base(value, overrideState) { } }
}
