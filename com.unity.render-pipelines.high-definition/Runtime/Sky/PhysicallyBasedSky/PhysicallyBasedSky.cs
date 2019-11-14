using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Sky/Physically Based Sky (Experimental)")]
    [SkyUniqueID((int)SkyType.PhysicallyBased)]
    public class PhysicallyBasedSky : SkySettings
    {
        /* We use the measurements from Earth as the defaults. */
        const float k_DefaultEarthRadius        =  6378.759f;
        const float k_DefaultAirExtinctionR     =  5.8f / 1000.0f; // at 680 nm, without ozone
        const float k_DefaultAirExtinctionG     = 13.5f / 1000.0f; // at 550 nm, without ozone
        const float k_DefaultAirExtinctionB     = 33.1f / 1000.0f; // at 440 nm, without ozone
        const float k_DefaultAirScaleHeight     = 8.0f;
        const float k_DefaultAirAlbedoR         = 0.9f; // BS values to account for absorption
        const float k_DefaultAirAlbedoG         = 0.9f; // due to the ozone layer. We assume that ozone
        const float k_DefaultAirAlbedoB         = 1.0f; // has the same height distribution as air (most certainly WRONG).

        [Tooltip("Simplifies the interface by using paramters suitable to simulate Earth.")]
        public BoolParameter earthPreset = new BoolParameter(true);
        [Tooltip("Radius of the planet (distance from the center to the sea level). Units: km.")]
        public MinFloatParameter planetaryRadius = new MinFloatParameter(k_DefaultEarthRadius, 0);
        [Tooltip("Position of the center of the planet in the world space. Units: km.")]
        // Does not affect the precomputation.
        public Vector3Parameter planetCenterPosition = new Vector3Parameter(new Vector3(0, -k_DefaultEarthRadius, 0));
        [Tooltip("Opacity (per color channel) of air as measured by an observer on the ground looking towards the zenith.")]
        public ClampedFloatParameter airDensityR = new ClampedFloatParameter(ZenithOpacityFromExtinctionAndScaleHeight(k_DefaultAirExtinctionR, k_DefaultAirScaleHeight), 0, 1);
        public ClampedFloatParameter airDensityG = new ClampedFloatParameter(ZenithOpacityFromExtinctionAndScaleHeight(k_DefaultAirExtinctionG, k_DefaultAirScaleHeight), 0, 1);
        public ClampedFloatParameter airDensityB = new ClampedFloatParameter(ZenithOpacityFromExtinctionAndScaleHeight(k_DefaultAirExtinctionB, k_DefaultAirScaleHeight), 0, 1);
        [Tooltip("Single scattering albedo of air molecules (per color channel). The value of 0 results in absorbing molecules, and the value of 1 results in scattering ones.")]
        public ColorParameter airTint = new ColorParameter(new Color(k_DefaultAirAlbedoR, k_DefaultAirAlbedoG, k_DefaultAirAlbedoB), hdr: false, showAlpha: false, showEyeDropper: true);
        [Tooltip("Depth of the atmospheric layer (from the sea level) composed of air particles. Controls the rate of height-based density falloff. Units: km.")]
        // We assume the exponential falloff of density w.r.t. the height.
        // We can interpret the depth as the height at which the density drops to 0.1% of the initial (sea level) value.
        public MinFloatParameter airMaximumAltitude = new MinFloatParameter(LayerDepthFromScaleHeight(k_DefaultAirScaleHeight), 0);
        // Note: aerosols are (fairly large) solid or liquid particles suspended in the air.
        [Tooltip("Opacity of aerosols as measured by an observer on the ground looking towards the zenith.")]
        public ClampedFloatParameter aerosolDensity = new ClampedFloatParameter(ZenithOpacityFromExtinctionAndScaleHeight(10.0f / 1000.0f, 1.2f), 0, 1);
        [Tooltip("Single scattering albedo of aerosol molecules (per color channel). The value of 0 results in absorbing molecules, and the value of 1 results in scattering ones.")]
        public ColorParameter aerosolTint = new ColorParameter(new Color(0.9f, 0.9f, 0.9f), hdr: false, showAlpha: false, showEyeDropper: true);
        [Tooltip("Depth of the atmospheric layer (from the sea level) composed of aerosol particles. Controls the rate of height-based density falloff. Units: km.")]
        // We assume the exponential falloff of density w.r.t. the height.
        // We can interpret the depth as the height at which the density drops to 0.1% of the initial (sea level) value.
        public MinFloatParameter aerosolMaximumAltitude = new MinFloatParameter(LayerDepthFromScaleHeight(1.2f), 0);
        [Tooltip("+1: forward  scattering. 0: almost isotropic. -1: backward scattering.")]
        public ClampedFloatParameter aerosolAnisotropy = new ClampedFloatParameter(0, -1, 1);
        [Tooltip("Number of scattering events.")]
        public ClampedIntParameter numberOfBounces = new ClampedIntParameter(8, 1, 10);
        [Tooltip("Albedo of the planetary surface.")]
        public ColorParameter groundTint = new ColorParameter(Color.white, hdr: false, showAlpha: false, showEyeDropper: false);
        // Hack. Does not affect the precomputation.
        public CubemapParameter groundColorTexture = new CubemapParameter(null);
        // Hack. Does not affect the precomputation.
        public CubemapParameter groundEmissionTexture = new CubemapParameter(null);
        // Hack. Does not affect the precomputation.
        public MinFloatParameter groundEmissionMultiplier = new MinFloatParameter(1, 0);
        // Hack. Does not affect the precomputation.
        public Vector3Parameter planetRotation = new Vector3Parameter(Vector3.zero);
        // Hack. Does not affect the precomputation.
        public CubemapParameter spaceEmissionTexture = new CubemapParameter(null);
        // Hack. Does not affect the precomputation.
        public MinFloatParameter spaceEmissionMultiplier = new MinFloatParameter(1, 0);
        // Hack. Does not affect the precomputation.
        public Vector3Parameter spaceRotation = new Vector3Parameter(Vector3.zero);

        static float ScaleHeightFromLayerDepth(float d)
        {
            // Exp[-d / H] = 0.001
            // -d / H = Log[0.001]
            // H = d / -Log[0.001]
            return d * 0.144765f;
        }

        static float LayerDepthFromScaleHeight(float H)
        {
            return H / 0.144765f;
        }

        static float ExtinctionFromZenithOpacityAndScaleHeight(float alpha, float H)
        {
            float opacity  = Mathf.Min(alpha, 0.999999f);
            float optDepth = -Mathf.Log(1 - opacity, 2.71828183f); // product of extinction and H

            return optDepth / H;
        }

        static float ZenithOpacityFromExtinctionAndScaleHeight(float ext, float H)
        {
            float optDepth = ext * H;

            return 1.0f - Mathf.Exp(-optDepth);
        }

        public float GetAirScaleHeight()
        {
            if (earthPreset.value)
            {
                return k_DefaultAirScaleHeight;
            }
            else
            {
                return ScaleHeightFromLayerDepth(airMaximumAltitude.value);
            }
        }

        public float GetPlanetaryRadius()
        {
            if (earthPreset.value)
            {
                return k_DefaultEarthRadius;
            }
            else
            {
                return planetaryRadius.value;
            }

        }

        public Vector3 GetAirExtinctionCoefficient()
        {
            Vector3 airExt = new Vector3();

            if (earthPreset.value)
            {
                airExt.x = k_DefaultAirExtinctionR;
                airExt.y = k_DefaultAirExtinctionG;
                airExt.z = k_DefaultAirExtinctionB;
            }
            else
            {
                airExt.x = ExtinctionFromZenithOpacityAndScaleHeight(airDensityR.value, GetAirScaleHeight());
                airExt.y = ExtinctionFromZenithOpacityAndScaleHeight(airDensityG.value, GetAirScaleHeight());
                airExt.z = ExtinctionFromZenithOpacityAndScaleHeight(airDensityB.value, GetAirScaleHeight());
            }

            return airExt;
        }

        public Vector3 GetAirAlbedo()
        {
            Vector3 airAlb = new Vector3();

            if (earthPreset.value)
            {
                airAlb.x = k_DefaultAirAlbedoR;
                airAlb.y = k_DefaultAirAlbedoG;
                airAlb.z = k_DefaultAirAlbedoB;
            }
            else
            {
                airAlb.x = airTint.value.r;
                airAlb.y = airTint.value.g;
                airAlb.z = airTint.value.b;
            }

            return airAlb;
        }

        public Vector3 GetAirScatteringCoefficient()
        {
            Vector3 airExt = GetAirExtinctionCoefficient();
            Vector3 airAlb = GetAirAlbedo();


            return new Vector3(airExt.x * airAlb.x,
                               airExt.y * airAlb.y,
                               airExt.z * airAlb.z);
        }

        public float GetAerosolScaleHeight()
        {
            return ScaleHeightFromLayerDepth(aerosolMaximumAltitude.value);
        }

        public float GetAerosolExtinctionCoefficient()
        {
            return ExtinctionFromZenithOpacityAndScaleHeight(aerosolDensity.value, GetAerosolScaleHeight());
        }

        public Vector3 GetAerosolScatteringCoefficient()
        {
            float aerExt = GetAerosolExtinctionCoefficient();

            return new Vector3(aerExt * aerosolTint.value.r,
                               aerExt * aerosolTint.value.g,
                               aerExt * aerosolTint.value.b);
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
                hash = hash * 23 + earthPreset.GetHashCode();
                hash = hash * 23 + planetaryRadius.GetHashCode();
                hash = hash * 23 + groundTint.GetHashCode();

                hash = hash * 23 + airMaximumAltitude.GetHashCode();
                hash = hash * 23 + airDensityR.GetHashCode();
                hash = hash * 23 + airDensityG.GetHashCode();
                hash = hash * 23 + airDensityB.GetHashCode();
                hash = hash * 23 + airTint.GetHashCode();

                hash = hash * 23 + aerosolMaximumAltitude.GetHashCode();
                hash = hash * 23 + aerosolDensity.GetHashCode();
                hash = hash * 23 + aerosolTint.GetHashCode();
                hash = hash * 23 + aerosolAnisotropy.GetHashCode();

                hash = hash * 23 + numberOfBounces.GetHashCode();
            }

            return hash;
        }

        public override int GetHashCode()
        {
            int hash = GetPrecomputationHashCode();

            unchecked
            {
                hash = hash * 23 + planetCenterPosition.GetHashCode();

                hash = hash * 23 + planetRotation.GetHashCode();
                if (groundColorTexture.value != null)
                    hash = hash * 23 + groundColorTexture.GetHashCode();
                if (groundEmissionTexture.value != null)
                    hash = hash * 23 + groundEmissionTexture.GetHashCode();
                hash = hash * 23 + groundEmissionMultiplier.GetHashCode();

                hash = hash * 23 + spaceRotation.GetHashCode();
                if (spaceEmissionTexture.value != null)
                    hash = hash * 23 + spaceEmissionTexture.GetHashCode();
                hash = hash * 23 + spaceEmissionMultiplier.GetHashCode();
            }

            return hash;
        }

        public override Type GetSkyRendererType() { return typeof(PhysicallyBasedSkyRenderer); }
    }
}
