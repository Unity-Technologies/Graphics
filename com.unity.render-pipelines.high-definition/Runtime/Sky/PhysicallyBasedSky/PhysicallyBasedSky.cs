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
        [Tooltip("Distance at which air reduces background light intensity by 63%, measured at the sea level. Controls the density at the sea level (per color channel). Units: 1000 km.")]
        public ColorParameter airAttenuationDistance = new ColorParameter(new Color(1.0f/5.8f, 1.0f/13.5f, 1.0f/33.1f), hdr: true, showAlpha: false, showEyeDropper: false);
        [Tooltip("Single scattering albedo of air molecules (per color channel). Acts as a color. Ratio between the scattering and attenuation coefficients. The value of 0 results in absorbing molecules, and the value of 1 results in scattering ones.")]
        // Note: this allows us to account for absorption due to the ozone layer.
        // We assume that ozone has the same height distribution as air (CITATION NEEDED!).
        public ColorParameter airAlbedo = new ColorParameter(new Color(0.9f, 0.9f, 1.0f), hdr: false, showAlpha: false, showEyeDropper: false);
        [Tooltip("Depth of the atmospheric layer (from the sea level) composed of air particles. Controls the rate of height-based density falloff. Units: km.")]
        // We assume the exponential falloff of density w.r.t. the height.
        // We can interpret the depth as the height at which the density drops to 0.1% of the initial (sea level) value.
        public MinFloatParameter airMaximumAltitude = new MinFloatParameter(58.3f, 0);
        [Tooltip("Distance at which aerosols reduces background light intensity by 63%. Controls the density at the sea level. Units: 1000 km.")]
        // Note: aerosols are (fairly large) solid or liquid particles in the air.
        public MinFloatParameter aerosolAttenuationDistance = new MinFloatParameter(1.0f/2.0f, 0);
        [Tooltip("Single scattering albedo of aerosol molecules. Ratio between the scattering and attenuation coefficients. The value of 0 results in absorbing molecules, and the value of 1 results in scattering ones.")]
        public ClampedFloatParameter aerosolAlbedo = new ClampedFloatParameter(0.9f, 0, 1);
        [Tooltip("Depth of the atmospheric layer (from the sea level) composed of aerosol particles. Controls the rate of height-based density falloff. Units: km.")]
        // We assume the exponential falloff of density w.r.t. the height.
        // We can interpret the depth as the height at which the density drops to 0.1% of the initial (sea level) value.
        public MinFloatParameter aerosolMaximumAltitude = new MinFloatParameter(8.3f, 0);
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

        public float GetAirScaleHeight()
        {
            return ScaleHeightFromLayerDepth(airMaximumAltitude.value);

        }

        public float GetAerosolScaleHeight()
        {
            return ScaleHeightFromLayerDepth(aerosolMaximumAltitude.value);
        }

        public Vector3 GetAirExtinctionCoefficient()
        {
            return new Vector3(0.001f / airAttenuationDistance.value.r,
                               0.001f / airAttenuationDistance.value.g,
                               0.001f / airAttenuationDistance.value.b); // Convert to 1/km
        }

        public float GetAerosolExtinctionCoefficient()
        {
            return 0.001f / aerosolAttenuationDistance.value; // Convert to 1/km
        }

        public Vector3 GetAirScatteringCoefficient()
        {
            Vector3 airExt = GetAirExtinctionCoefficient();

            return new Vector3(airExt.x * airAlbedo.value.r,
                               airExt.y * airAlbedo.value.g,
                               airExt.z * airAlbedo.value.b);
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
                hash = hash * 23 + airAttenuationDistance.GetHashCode();
                hash = hash * 23 + airAlbedo.GetHashCode();
                hash = hash * 23 + airMaximumAltitude.GetHashCode();
                hash = hash * 23 + aerosolAttenuationDistance.GetHashCode();
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

        public override SkyRenderer CreateRenderer()
        {
            return new PhysicallyBasedSkyRenderer(this);
        }
    }
}
