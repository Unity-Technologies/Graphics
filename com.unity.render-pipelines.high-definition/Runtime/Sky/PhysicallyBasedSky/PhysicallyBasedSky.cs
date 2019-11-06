using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Sky/Physically Based Sky (Experimental)")]
    [SkyUniqueID((int)SkyType.PhysicallyBased)]
    public class PhysicallyBasedSky : SkySettings
    {
        /* We use the measurements from Earth as the defaults. */
        [Tooltip("Radius of the planet (distance from the core to the sea level). Units: km.")]
        public MinFloatParameter planetaryRadius = new MinFloatParameter(6378.759f, 0);
        [Tooltip("Position of the center of the planet in the world space. Units: km.")]
        // Does not affect the precomputation.
        public Vector3Parameter planetCenterPosition = new Vector3Parameter(new Vector3(0, -6378.759f, 0));
        [Tooltip("Opacity of air as measured by an observer on the ground looking towards the horizon.")]
        public ColorParameter airOpacity = new ColorParameter(new Color(0.816175f, 0.980598f, 0.999937f), hdr: false, showAlpha: false, showEyeDropper: true);
        [Tooltip("Single scattering albedo of air molecules (per color channel). Acts as a color. Ratio between the scattering and attenuation coefficients. The value of 0 results in absorbing molecules, and the value of 1 results in scattering ones.")]
        // Note: this allows us to account for absorption due to the ozone layer.
        // We assume that ozone has the same height distribution as air (CITATION NEEDED!).
        public ColorParameter airAlbedo = new ColorParameter(new Color(0.9f, 0.9f, 1.0f), hdr: false, showAlpha: false, showEyeDropper: true);
        [Tooltip("Depth of the atmospheric layer (from the sea level) composed of air particles. Controls the rate of height-based density falloff. Units: km.")]
        // We assume the exponential falloff of density w.r.t. the height.
        // We can interpret the depth as the height at which the density drops to 0.1% of the initial (sea level) value.
        public MinFloatParameter airMaximumAltitude = new MinFloatParameter(55.262f, 0);
        // Note: aerosols are (fairly large) solid or liquid particles in the air.
        [Tooltip("Opacity of aerosols as measured by an observer on the ground looking towards the horizon.")]
        public ClampedFloatParameter aerosolOpacity = new ClampedFloatParameter(0.5f, 0, 1);
        [Tooltip("Single scattering albedo of aerosol molecules. Ratio between the scattering and attenuation coefficients. The value of 0 results in absorbing molecules, and the value of 1 results in scattering ones.")]
        public ClampedFloatParameter aerosolAlbedo = new ClampedFloatParameter(0.9f, 0, 1);
        [Tooltip("Depth of the atmospheric layer (from the sea level) composed of aerosol particles. Controls the rate of height-based density falloff. Units: km.")]
        // We assume the exponential falloff of density w.r.t. the height.
        // We can interpret the depth as the height at which the density drops to 0.1% of the initial (sea level) value.
        public MinFloatParameter aerosolMaximumAltitude = new MinFloatParameter(8.28931f, 0);
        [Tooltip("+1: forward  scattering. 0: almost isotropic. -1: backward scattering.")]
        public ClampedFloatParameter aerosolAnisotropy = new ClampedFloatParameter(0, -1, 1);
        [Tooltip("Number of scattering events.")]
        public ClampedIntParameter numberOfBounces = new ClampedIntParameter(8, 1, 10);
        [Tooltip("Albedo of the planetary surface.")]
        public ColorParameter groundColor = new ColorParameter(new Color(0.4f, 0.25f, 0.15f), hdr: false, showAlpha: false, showEyeDropper: false);
        // Hack. Does not affect the precomputation.
        public CubemapParameter groundAlbedoTexture = new CubemapParameter(null);
        // Hack. Does not affect the precomputation.
        public CubemapParameter groundEmissionTexture = new CubemapParameter(null);
        // Hack. Does not affect the precomputation.
        public Vector3Parameter planetRotation = new Vector3Parameter(Vector3.zero);
        // Hack. Does not affect the precomputation.
        public CubemapParameter spaceEmissionTexture = new CubemapParameter(null);
        // Hack. Does not affect the precomputation.
        public Vector3Parameter spaceRotation = new Vector3Parameter(Vector3.zero);

        static float ScaleHeightFromLayerDepth(float d)
        {
            // Exp[-d / H] = 0.001
            // -d / H = Log[0.001]
            // H = d / -Log[0.001]
            return d * 0.144765f;
        }

        static float InvertOpticalDepth(float x, float H, float R)
        {
            float Z  = R / H;
            float ch = 0.5f * Mathf.Sqrt(0.5f * Mathf.PI) * (1 / Mathf.Sqrt(Z) + 2 * Mathf.Sqrt(Z));

            return x / (ch * H);
        }

        static float ConvertOpacityToExtinction(float alpha, float H, float R)
        {
            float opacity    = Mathf.Min(alpha, 0.99999f);
            float optDepth   = -Mathf.Log(1 - opacity, 2.71828183f);
            float extinction = InvertOpticalDepth(optDepth, H, R);

            return extinction;
        }

        public float GetAirScaleHeight()
        {
            return ScaleHeightFromLayerDepth(airMaximumAltitude.value);

        }

        public Vector3 GetAirExtinctionCoefficient()
        {
            Vector3 airExt = new Vector3();

            airExt.x = ConvertOpacityToExtinction(airOpacity.value.r, GetAirScaleHeight(), planetaryRadius.value);
            airExt.y = ConvertOpacityToExtinction(airOpacity.value.g, GetAirScaleHeight(), planetaryRadius.value);
            airExt.z = ConvertOpacityToExtinction(airOpacity.value.b, GetAirScaleHeight(), planetaryRadius.value);

            return airExt;
        }

        public Vector3 GetAirScatteringCoefficient()
        {
            Vector3 airExt = GetAirExtinctionCoefficient();

            return new Vector3(airExt.x * airAlbedo.value.r,
                               airExt.y * airAlbedo.value.g,
                               airExt.z * airAlbedo.value.b);
        }

        public float GetAerosolScaleHeight()
        {
            return ScaleHeightFromLayerDepth(aerosolMaximumAltitude.value);
        }

        public float GetAerosolExtinctionCoefficient()
        {
            return ConvertOpacityToExtinction(aerosolOpacity.value, GetAerosolScaleHeight(), planetaryRadius.value);
        }

        public float GetAerosolScatteringCoefficient()
        {
            float aerExt = GetAerosolExtinctionCoefficient();

            return aerExt * aerosolAlbedo.value;
        }

        PhysicallyBasedSky()
        {
            displayName = "Physically Based Sky (Experimental)";
        }

        public int GetPrecomputationHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                // No 'planetCenterPosition' or any textures, as they don't affect the precomputation.
                hash = hash * 23 + planetaryRadius.GetHashCode();
                hash = hash * 23 + airOpacity.GetHashCode();
                hash = hash * 23 + airAlbedo.GetHashCode();
                hash = hash * 23 + airMaximumAltitude.GetHashCode();
                hash = hash * 23 + aerosolOpacity.GetHashCode();
                hash = hash * 23 + aerosolAlbedo.GetHashCode();
                hash = hash * 23 + aerosolMaximumAltitude.GetHashCode();
                hash = hash * 23 + aerosolAnisotropy.GetHashCode();
                hash = hash * 23 + numberOfBounces.GetHashCode();
                hash = hash * 23 + groundColor.GetHashCode();
            }

            return hash;
        }

        public override int GetHashCode()
        {
            int hash = GetPrecomputationHashCode();

            unchecked
            {
                hash = hash * 23 + planetCenterPosition.GetHashCode();
                if (groundAlbedoTexture.value != null)
                    hash = hash * 23 + groundAlbedoTexture.GetHashCode();
                if (groundEmissionTexture.value != null)
                    hash = hash * 23 + groundEmissionTexture.GetHashCode();
                hash = hash * 23 + planetRotation.GetHashCode();
                if (spaceEmissionTexture.value != null)
                    hash = hash * 23 + spaceEmissionTexture.GetHashCode();
                hash = hash * 23 + spaceRotation.GetHashCode();
            }

            return hash;
        }

        public override Type GetSkyRendererType() { return typeof(PhysicallyBasedSkyRenderer); }
    }
}
