using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Sky/Procedural Sky")]
    [SkyUniqueID((int)SkyType.Procedural)]
    public class ProceduralSky : SkySettings
    {
        [Tooltip("Sets the size modifier of the sun disk.")]
        public ClampedFloatParameter sunSize = new ClampedFloatParameter(0.04f, 0.0f, 1.0f);
        [Tooltip("Sets the size convergence of the sun, smaller values make the sun appear larger.")]
        public ClampedFloatParameter sunSizeConvergence = new ClampedFloatParameter(5.0f, 1.0f, 10.0f);
        [Tooltip("Sets the density of the atmosphere.")]
        public ClampedFloatParameter atmosphereThickness = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
        [Tooltip("Sets the color of the sky.")]
        public ColorParameter skyTint = new ColorParameter(new Color(0.5f, 0.5f, 0.5f, 1.0f));
        [Tooltip("Sets the color of the ground, the area below the horizon.")]
        public ColorParameter groundColor = new ColorParameter(new Color(0.369f, 0.349f, 0.341f, 1.0f));
        [Tooltip("When enabled, HDRP displays the sun disk.")]
        public BoolParameter enableSunDisk = new BoolParameter(true);

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
#if UNITY_2019_3 // In 2019.3, when we call GetHashCode on a VolumeParameter it generate garbage (due to the boxing of the generic parameter)
                hash = hash * 23 + sunSize.value.GetHashCode();
                hash = hash * 23 + sunSizeConvergence.value.GetHashCode();
                hash = hash * 23 + atmosphereThickness.value.GetHashCode();
                hash = hash * 23 + skyTint.value.GetHashCode();
                hash = hash * 23 + groundColor.value.GetHashCode();
                hash = hash * 23 + multiplier.value.GetHashCode();
                hash = hash * 23 + enableSunDisk.value.GetHashCode();

                hash = hash * 23 + sunSize.overrideState.GetHashCode();
                hash = hash * 23 + sunSizeConvergence.overrideState.GetHashCode();
                hash = hash * 23 + atmosphereThickness.overrideState.GetHashCode();
                hash = hash * 23 + skyTint.overrideState.GetHashCode();
                hash = hash * 23 + groundColor.overrideState.GetHashCode();
                hash = hash * 23 + multiplier.overrideState.GetHashCode();
                hash = hash * 23 + enableSunDisk.overrideState.GetHashCode();
#else
                hash = hash * 23 + sunSize.GetHashCode();
                hash = hash * 23 + sunSizeConvergence.GetHashCode();
                hash = hash * 23 + atmosphereThickness.GetHashCode();
                hash = hash * 23 + skyTint.GetHashCode();
                hash = hash * 23 + groundColor.GetHashCode();
                hash = hash * 23 + multiplier.GetHashCode();
                hash = hash * 23 + enableSunDisk.GetHashCode();
#endif
            }

            return hash;
        }

        public override System.Type GetSkyRendererType() { return typeof(ProceduralSkyRenderer); }
    }
}
