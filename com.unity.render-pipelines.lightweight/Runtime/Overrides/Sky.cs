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
        [Tooltip("One world space unit measured to real world units (meters)")]
        public FloatParameter LengthUnitInMeters = new FloatParameter(100.0f);

        public FloatParameter exposure = new FloatParameter(10.0f);
    }

    [Serializable]
    public sealed class SkyTypeParameter : VolumeParameter<SkyType> { public SkyTypeParameter(SkyType value, bool overrideState = false) : base(value, overrideState) { } }
}
