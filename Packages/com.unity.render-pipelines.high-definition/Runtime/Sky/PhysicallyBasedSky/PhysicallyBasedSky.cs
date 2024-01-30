using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// The model used to control the complexity of the simulation.
    /// </summary>
    public enum PhysicallyBasedSkyModel
    {
        /// <summary>Suitable to simulate Earth</summary>
        EarthSimple,
        /// <summary>Suitable to simulate Earth</summary>
        EarthAdvanced,
        /// <summary>Suitable to simulate any planet</summary>
        Custom
    };

    /// <summary>
    /// Physically Based Sky model volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class PhysicallyBasedSkyModelParameter : VolumeParameter<PhysicallyBasedSkyModel>
    {
        /// <summary>
        /// constructor.
        /// </summary>
        /// <param name="value">Model parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public PhysicallyBasedSkyModelParameter(PhysicallyBasedSkyModel value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// Physically Based Sky Volume Component.
    /// </summary>
    [VolumeComponentMenuForRenderPipeline("Sky/Physically Based Sky", typeof(HDRenderPipeline))]
    [SkyUniqueID((int)SkyType.PhysicallyBased)]
    [HDRPHelpURLAttribute("Override-Physically-Based-Sky")]
    public partial class PhysicallyBasedSky : SkySettings
    {
        /* We use the measurements from Earth as the defaults. */
        const float k_DefaultEarthRadius = 6.3781f * 1000000;
        const float k_DefaultAirScatteringR = 5.8f / 1000000; // at 680 nm, without ozone
        const float k_DefaultAirScatteringG = 13.5f / 1000000; // at 550 nm, without ozone
        const float k_DefaultAirScatteringB = 33.1f / 1000000; // at 440 nm, without ozone
        const float k_DefaultAirScaleHeight = 8000;
        const float k_DefaultAirAlbedoR = 0.9f; // BS values to account for absorption
        const float k_DefaultAirAlbedoG = 0.9f; // due to the ozone layer. We assume that ozone
        const float k_DefaultAirAlbedoB = 1.0f; // has the same height distribution as air (most certainly WRONG).
        const float k_DefaultAerosolScaleHeight = 1200;
        static readonly float k_DefaultAerosolMaximumAltitude = LayerDepthFromScaleHeight(k_DefaultAerosolScaleHeight);

        /// <summary> Simplifies the interface by reducing the number of parameters available. </summary>
        public PhysicallyBasedSkyModelParameter type = new PhysicallyBasedSkyModelParameter(PhysicallyBasedSkyModel.EarthAdvanced);

        /// <summary> Allows to specify the location of the planet. If disabled, the planet is always below the camera in the world-space X-Z plane. </summary>
        [Tooltip("When enabled, you can define the planet in terms of a world-space position and radius. Otherwise, the planet is always below the Camera in the world-space x-z plane.")]
        public BoolParameter sphericalMode = new BoolParameter(true);

        /// <summary> World-space Y coordinate of the sea level of the planet. Units: meters. </summary>
        [Tooltip("Sets the world-space y coordinate of the planet's sea level in meters.")]
        public FloatParameter seaLevel = new FloatParameter(0);

        /// <summary> Radius of the planet (distance from the center of the planet to the sea level). Units: meters. </summary>
        [Tooltip("Sets the radius of the planet in meters. This is distance from the center of the planet to the sea level.")]
        public MinFloatParameter planetaryRadius = new MinFloatParameter(k_DefaultEarthRadius, 0);

        /// <summary> Position of the center of the planet in the world space. Units: meters. Does not affect the precomputation. </summary>
        [Tooltip("Sets the world-space position of the planet's center in meters.")]
        public Vector3Parameter planetCenterPosition = new Vector3Parameter(new Vector3(0, -k_DefaultEarthRadius, 0));

        /// <summary> Opacity (per color channel) of air as measured by an observer on the ground looking towards the zenith. </summary>
        [Tooltip("Controls the red color channel opacity of air at the point in the sky directly above the observer (zenith).")]
        public ClampedFloatParameter airDensityR = new ClampedFloatParameter(ZenithOpacityFromExtinctionAndScaleHeight(k_DefaultAirScatteringR, k_DefaultAirScaleHeight), 0, 1);

        /// <summary> Opacity (per color channel) of air as measured by an observer on the ground looking towards the zenith. </summary>
        [Tooltip("Controls the green color channel opacity of air at the point in the sky directly above the observer (zenith).")]
        public ClampedFloatParameter airDensityG = new ClampedFloatParameter(ZenithOpacityFromExtinctionAndScaleHeight(k_DefaultAirScatteringG, k_DefaultAirScaleHeight), 0, 1);

        /// <summary> Opacity (per color channel) of air as measured by an observer on the ground looking towards the zenith. </summary>
        [Tooltip("Controls the blue color channel opacity of air at the point in the sky directly above the observer (zenith).")]
        public ClampedFloatParameter airDensityB = new ClampedFloatParameter(ZenithOpacityFromExtinctionAndScaleHeight(k_DefaultAirScatteringB, k_DefaultAirScaleHeight), 0, 1);

        /// <summary> Single scattering albedo of air molecules (per color channel). The value of 0 results in absorbing molecules, and the value of 1 results in scattering ones. </summary>
        [Tooltip("Specifies the color that HDRP tints the air to. This controls the single scattering albedo of air molecules (per color channel). A value of 0 results in absorbing molecules, and a value of 1 results in scattering ones.")]
        public ColorParameter airTint = new ColorParameter(new Color(k_DefaultAirAlbedoR, k_DefaultAirAlbedoG, k_DefaultAirAlbedoB), hdr: false, showAlpha: false, showEyeDropper: true);

        /// <summary> Depth of the atmospheric layer (from the sea level) composed of air particles. Controls the rate of height-based density falloff. Units: meters. </summary>
        [Tooltip("Sets the depth, in meters, of the atmospheric layer, from sea level, composed of air particles. Controls the rate of height-based density falloff.")]
        // We assume the exponential falloff of density w.r.t. the height.
        // We can interpret the depth as the height at which the density drops to 0.1% of the initial (sea level) value.
        public MinFloatParameter airMaximumAltitude = new MinFloatParameter(LayerDepthFromScaleHeight(k_DefaultAirScaleHeight), 0);

        /// <summary> Opacity of aerosols as measured by an observer on the ground looking towards the zenith. </summary>
        [Tooltip("Controls the opacity of aerosols at the point in the sky directly above the observer (zenith).")]
        // Note: aerosols are (fairly large) solid or liquid particles suspended in the air.
        public ClampedFloatParameter aerosolDensity = new ClampedFloatParameter(ZenithOpacityFromExtinctionAndScaleHeight(10.0f / 1000000, k_DefaultAerosolScaleHeight), 0, 1);

        /// <summary> Single scattering albedo of aerosol molecules (per color channel). The value of 0 results in absorbing molecules, and the value of 1 results in scattering ones. </summary>
        [Tooltip("Specifies the color that HDRP tints aerosols to. This controls the single scattering albedo of aerosol molecules (per color channel). A value of 0 results in absorbing molecules, and a value of 1 results in scattering ones.")]
        public ColorParameter aerosolTint = new ColorParameter(new Color(0.9f, 0.9f, 0.9f), hdr: false, showAlpha: false, showEyeDropper: true);

        /// <summary> Depth of the atmospheric layer (from the sea level) composed of aerosol particles. Controls the rate of height-based density falloff. Units: meters. </summary>
        [Tooltip("Sets the depth, in meters, of the atmospheric layer, from sea level, composed of aerosol particles. Controls the rate of height-based density falloff.")]
        // We assume the exponential falloff of density w.r.t. the height.
        // We can interpret the depth as the height at which the density drops to 0.1% of the initial (sea level) value.
        public MinFloatParameter aerosolMaximumAltitude = new MinFloatParameter(k_DefaultAerosolMaximumAltitude, 0);

        /// <summary> Positive values for forward scattering, 0 for isotropic scattering. negative values for backward scattering. </summary>
        [Tooltip("Controls the direction of anisotropy. Set this to a positive value for forward scattering, a negative value for backward scattering, or 0 for isotropic scattering.")]
        public ClampedFloatParameter aerosolAnisotropy = new ClampedFloatParameter(0, -1, 1);

        /// <summary> Ground tint. </summary>
        [Tooltip("Specifies a color that HDRP uses to tint the Ground Color Texture.")]
        public ColorParameter groundTint = new ColorParameter(new Color(0.4f, 0.25f, 0.15f), hdr: false, showAlpha: false, showEyeDropper: false);

        /// <summary> Ground color texture. Does not affect the precomputation. </summary>
        [Tooltip("Specifies a Texture that represents the planet's surface. Does not affect the precomputation.")]
        public CubemapParameter groundColorTexture = new CubemapParameter(null);

        /// <summary> Ground emission texture. Does not affect the precomputation. </summary>
        [Tooltip("Specifies a Texture that represents the emissive areas of the planet's surface. Does not affect the precomputation.")]
        public CubemapParameter groundEmissionTexture = new CubemapParameter(null);

        /// <summary> Ground emission multiplier. Does not affect the precomputation. </summary>
        [Tooltip("Sets the multiplier that HDRP applies to the Ground Emission Texture.")]
        public MinFloatParameter groundEmissionMultiplier = new MinFloatParameter(1, 0);

        /// <summary> Rotation of the planet. Does not affect the precomputation. </summary>
        [Tooltip("Sets the orientation of the planet. Does not affect the precomputation.")]
        public Vector3Parameter planetRotation = new Vector3Parameter(Vector3.zero);

        /// <summary> Space emission texture. Does not affect the precomputation. </summary>
        [Tooltip("Specifies a Texture that represents the emissive areas of space. Does not affect the precomputation.")]
        public CubemapParameter spaceEmissionTexture = new CubemapParameter(null);

        /// <summary> Space emission multiplier. Does not affect the precomputation. </summary>
        [Tooltip("Sets the multiplier that HDRP applies to the Space Emission Texture. Does not affect the precomputation.")]
        public MinFloatParameter spaceEmissionMultiplier = new MinFloatParameter(1, 0);

        /// <summary> Rotation of space. Does not affect the precomputation. </summary>
        [Tooltip("Sets the orientation of space. Does not affect the precomputation.")]
        public Vector3Parameter spaceRotation = new Vector3Parameter(Vector3.zero);

        /// <summary> Color saturation. Does not affect the precomputation. </summary>
        [Tooltip("Controls the saturation of the sky color. Does not affect the precomputation.")]
        public ClampedFloatParameter colorSaturation = new ClampedFloatParameter(1, 0, 1);

        /// <summary> Opacity saturation. Does not affect the precomputation. </summary>
        [Tooltip("Controls the saturation of the sky opacity. Does not affect the precomputation.")]
        public ClampedFloatParameter alphaSaturation = new ClampedFloatParameter(1, 0, 1);

        /// <summary> Opacity multiplier. Does not affect the precomputation. </summary>
        [Tooltip("Sets the multiplier that HDRP applies to the opacity of the sky. Does not affect the precomputation.")]
        public ClampedFloatParameter alphaMultiplier = new ClampedFloatParameter(1, 0, 1);

        /// <summary> Horizon tint. Does not affect the precomputation. </summary>
        [Tooltip("Specifies a color that HDRP uses to tint the sky at the horizon. Does not affect the precomputation.")]
        public ColorParameter horizonTint = new ColorParameter(Color.white, hdr: false, showAlpha: false, showEyeDropper: false);

        /// <summary> Zenith tint. Does not affect the precomputation. </summary>
        [Tooltip("Specifies a color that HDRP uses to tint the point in the sky directly above the observer (the zenith). Does not affect the precomputation.")]
        public ColorParameter zenithTint = new ColorParameter(Color.white, hdr: false, showAlpha: false, showEyeDropper: false);

        /// <summary> Horizon-zenith shift. Does not affect the precomputation. </summary>
        [Tooltip("Controls how HDRP blends between the Horizon Tint and Zenith Tint. Does not affect the precomputation.")]
        public ClampedFloatParameter horizonZenithShift = new ClampedFloatParameter(0, -1, 1);

        static internal float ScaleHeightFromLayerDepth(float d)
        {
            // Exp[-d / H] = 0.001
            // -d / H = Log[0.001]
            // H = d / -Log[0.001]
            return d * 0.144765f;
        }

        static internal float LayerDepthFromScaleHeight(float H)
        {
            return H / 0.144765f;
        }

        static internal float ExtinctionFromZenithOpacityAndScaleHeight(float alpha, float H)
        {
            float opacity = Mathf.Min(alpha, 0.999999f);
            float optDepth = -Mathf.Log(1 - opacity, 2.71828183f); // product of extinction and H

            return optDepth / H;
        }

        static internal float ZenithOpacityFromExtinctionAndScaleHeight(float ext, float H)
        {
            float optDepth = ext * H;

            return 1 - Mathf.Exp(-optDepth);
        }

        internal float GetAirScaleHeight()
        {
            if (type.value != PhysicallyBasedSkyModel.Custom)
            {
                return k_DefaultAirScaleHeight;
            }
            else
            {
                return ScaleHeightFromLayerDepth(airMaximumAltitude.value);
            }
        }

        internal float GetMaximumAltitude()
        {
            if (type.value == PhysicallyBasedSkyModel.Custom)
                return Mathf.Max(airMaximumAltitude.value, aerosolMaximumAltitude.value);

            float aerosolMaxAltitude = (type.value == PhysicallyBasedSkyModel.EarthSimple) ? k_DefaultAerosolMaximumAltitude : aerosolMaximumAltitude.value;
            return Mathf.Max(LayerDepthFromScaleHeight(k_DefaultAirScaleHeight), aerosolMaxAltitude);
        }

        internal float GetPlanetaryRadius()
        {
            if (type.value != PhysicallyBasedSkyModel.Custom)
            {
                return k_DefaultEarthRadius;
            }
            else
            {
                return planetaryRadius.value;
            }
        }

        internal Vector3 GetPlanetCenterPosition(Vector3 camPosWS)
        {
            if (sphericalMode.value && (type.value != PhysicallyBasedSkyModel.EarthSimple))
            {
                return planetCenterPosition.value;
            }
            else // Planar mode
            {
                float R = GetPlanetaryRadius();
                float h = seaLevel.value;

                return new Vector3(camPosWS.x, -R + h, camPosWS.z);
            }
        }

        internal Vector3 GetAirExtinctionCoefficient()
        {
            Vector3 airExt = new Vector3();

            if (type.value != PhysicallyBasedSkyModel.Custom)
            {
                airExt.x = k_DefaultAirScatteringR;
                airExt.y = k_DefaultAirScatteringG;
                airExt.z = k_DefaultAirScatteringB;
            }
            else
            {
                airExt.x = ExtinctionFromZenithOpacityAndScaleHeight(airDensityR.value, GetAirScaleHeight());
                airExt.y = ExtinctionFromZenithOpacityAndScaleHeight(airDensityG.value, GetAirScaleHeight());
                airExt.z = ExtinctionFromZenithOpacityAndScaleHeight(airDensityB.value, GetAirScaleHeight());
            }

            return airExt;
        }

        internal Vector3 GetAirAlbedo()
        {
            Vector3 airAlb = new Vector3();

            if (type.value != PhysicallyBasedSkyModel.Custom)
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

        internal Vector3 GetAirScatteringCoefficient()
        {
            Vector3 airExt = GetAirExtinctionCoefficient();
            Vector3 airAlb = GetAirAlbedo();


            return new Vector3(airExt.x * airAlb.x,
                airExt.y * airAlb.y,
                airExt.z * airAlb.z);
        }

        internal float GetAerosolScaleHeight()
        {
            if (type.value == PhysicallyBasedSkyModel.EarthSimple)
            {
                return k_DefaultAerosolScaleHeight;
            }
            else
            {
                return ScaleHeightFromLayerDepth(aerosolMaximumAltitude.value);
            }
        }

        internal float GetAerosolAnisotropy()
        {
            if (type.value == PhysicallyBasedSkyModel.EarthSimple)
            {
                return 0;
            }
            else
            {
                return aerosolAnisotropy.value;
            }
        }

        internal float GetAerosolExtinctionCoefficient()
        {
            return ExtinctionFromZenithOpacityAndScaleHeight(aerosolDensity.value, GetAerosolScaleHeight());
        }

        internal Vector3 GetAerosolScatteringCoefficient()
        {
            float aerExt = GetAerosolExtinctionCoefficient();

            return new Vector3(aerExt * aerosolTint.value.r,
                aerExt * aerosolTint.value.g,
                aerExt * aerosolTint.value.b);
        }

        PhysicallyBasedSky()
        {
            displayName = "Physically Based Sky";
        }

        internal int GetPrecomputationHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
#if UNITY_2019_3 // In 2019.3, when we call GetHashCode on a VolumeParameter it generate garbage (due to the boxing of the generic parameter)
                // These parameters affect precomputation.
                hash = hash * 23 + type.overrideState.GetHashCode();
                hash = hash * 23 + planetaryRadius.overrideState.GetHashCode();
                hash = hash * 23 + groundTint.overrideState.GetHashCode();

                hash = hash * 23 + airMaximumAltitude.overrideState.GetHashCode();
                hash = hash * 23 + airDensityR.overrideState.GetHashCode();
                hash = hash * 23 + airDensityG.overrideState.GetHashCode();
                hash = hash * 23 + airDensityB.overrideState.GetHashCode();
                hash = hash * 23 + airTint.overrideState.GetHashCode();

                hash = hash * 23 + aerosolMaximumAltitude.overrideState.GetHashCode();
                hash = hash * 23 + aerosolDensity.overrideState.GetHashCode();
                hash = hash * 23 + aerosolTint.overrideState.GetHashCode();
                hash = hash * 23 + aerosolAnisotropy.overrideState.GetHashCode();
#else
                // These parameters affect precomputation.
                hash = hash * 23 + type.GetHashCode();
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
#endif
            }

            return hash;
        }

        /// <summary>
        /// Returns the hash code of the sky parameters.
        /// </summary>
        /// <param name="camera">The camera we want to use to compute the hash of the sky.</param>
        /// <returns>The hash code of the sky parameters.</returns>
        public override int GetHashCode(Camera camera)
        {
            int hash = GetHashCode();
            Vector3 cameraLocation = camera.transform.position;
            float r = Vector3.Distance(cameraLocation, GetPlanetCenterPosition(cameraLocation));
            float R = GetPlanetaryRadius();

            bool isPbrSkyActive = r > R; // Disable sky rendering below the ground

            hash = hash * 23 + isPbrSkyActive.GetHashCode();
            return hash;
        }

        /// <summary> Returns the hash code of the parameters of the sky. </summary>
        /// <returns> The hash code of the parameters of the sky. </returns>
        public override int GetHashCode()
        {
            int hash = GetPrecomputationHashCode();

            unchecked
            {
#if UNITY_2019_3 // In 2019.3, when we call GetHashCode on a VolumeParameter it generate garbage (due to the boxing of the generic parameter)
                // These parameters do NOT affect precomputation.
                hash = hash * 23 + sphericalMode.overrideState.GetHashCode();
                hash = hash * 23 + seaLevel.overrideState.GetHashCode();
                hash = hash * 23 + planetCenterPosition.overrideState.GetHashCode();
                hash = hash * 23 + planetRotation.overrideState.GetHashCode();

                if (groundColorTexture.value != null)
                    hash = hash * 23 + groundColorTexture.overrideState.GetHashCode();

                if (groundEmissionTexture.value != null)
                    hash = hash * 23 + groundEmissionTexture.overrideState.GetHashCode();

                hash = hash * 23 + groundEmissionMultiplier.overrideState.GetHashCode();

                hash = hash * 23 + spaceRotation.overrideState.GetHashCode();

                if (spaceEmissionTexture.value != null)
                    hash = hash * 23 + spaceEmissionTexture.overrideState.GetHashCode();

                hash = hash * 23 + spaceEmissionMultiplier.overrideState.GetHashCode();
                hash = hash * 23 + colorSaturation.overrideState.GetHashCode();
                hash = hash * 23 + alphaSaturation.overrideState.GetHashCode();
                hash = hash * 23 + alphaMultiplier.overrideState.GetHashCode();
                hash = hash * 23 + horizonTint.overrideState.GetHashCode();
                hash = hash * 23 + zenithTint.overrideState.GetHashCode();
                hash = hash * 23 + horizonZenithShift.overrideState.GetHashCode();
#else
                // These parameters do NOT affect precomputation.
                hash = hash * 23 + sphericalMode.GetHashCode();
                hash = hash * 23 + seaLevel.GetHashCode();
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
                hash = hash * 23 + colorSaturation.GetHashCode();
                hash = hash * 23 + alphaSaturation.GetHashCode();
                hash = hash * 23 + alphaMultiplier.GetHashCode();
                hash = hash * 23 + horizonTint.GetHashCode();
                hash = hash * 23 + zenithTint.GetHashCode();
                hash = hash * 23 + horizonZenithShift.GetHashCode();
#endif
            }

            return hash;
        }

        static float Saturate(float x)
        {
            return Mathf.Max(0, Mathf.Min(x, 1));
        }

        static float Rcp(float x)
        {
            return 1.0f / x;
        }

        static float Rsqrt(float x)
        {
            return Rcp(Mathf.Sqrt(x));
        }

        static float ComputeCosineOfHorizonAngle(float r, float R)
        {
            float sinHoriz = R * Rcp(r);
            return -Mathf.Sqrt(Saturate(1 - sinHoriz * sinHoriz));
        }

        static float ChapmanUpperApprox(float z, float cosTheta)
        {
            float c = cosTheta;
            float n = 0.761643f * ((1 + 2 * z) - (c * c * z));
            float d = c * z + Mathf.Sqrt(z * (1.47721f + 0.273828f * (c * c * z)));

            return 0.5f * c + (n * Rcp(d));
        }

        static float ChapmanHorizontal(float z)
        {
            float r = Rsqrt(z);
            float s = z * r; // sqrt(z)

            return 0.626657f * (r + 2 * s);
        }

        static Vector3 ComputeAtmosphericOpticalDepth(
            float airScaleHeight, float aerosolScaleHeight, in Vector3 airExtinctionCoefficient, float aerosolExtinctionCoefficient,
            float R, float r, float cosTheta, bool alwaysAboveHorizon = false)
        {
            Vector2 H = new Vector2(airScaleHeight, aerosolScaleHeight);
            Vector2 rcpH = new Vector2(Rcp(H.x), Rcp(H.y));

            Vector2 z = r * rcpH;
            Vector2 Z = R * rcpH;

            float cosHoriz = ComputeCosineOfHorizonAngle(r, R);
            float sinTheta = Mathf.Sqrt(Saturate(1 - cosTheta * cosTheta));

            Vector2 ch;
            ch.x = ChapmanUpperApprox(z.x, Mathf.Abs(cosTheta)) * Mathf.Exp(Z.x - z.x); // Rescaling adds 'exp'
            ch.y = ChapmanUpperApprox(z.y, Mathf.Abs(cosTheta)) * Mathf.Exp(Z.y - z.y); // Rescaling adds 'exp'

            if ((!alwaysAboveHorizon) && (cosTheta < cosHoriz)) // Below horizon, intersect sphere
            {
                float sinGamma = (r / R) * sinTheta;
                float cosGamma = Mathf.Sqrt(Saturate(1 - sinGamma * sinGamma));

                Vector2 ch_2;
                ch_2.x = ChapmanUpperApprox(Z.x, cosGamma); // No need to rescale
                ch_2.y = ChapmanUpperApprox(Z.y, cosGamma); // No need to rescale

                ch = ch_2 - ch;
            }
            else if (cosTheta < 0)   // Above horizon, lower hemisphere
            {
                // z_0 = n * r_0 = (n * r) * sin(theta) = z * sin(theta).
                // Ch(z, theta) = 2 * exp(z - z_0) * Ch(z_0, Pi/2) - Ch(z, Pi - theta).
                Vector2 z_0 = z * sinTheta;
                Vector2 b = new Vector2(Mathf.Exp(Z.x - z_0.x), Mathf.Exp(Z.x - z_0.x)); // Rescaling cancels out 'z' and adds 'Z'
                Vector2 a;
                a.x = 2 * ChapmanHorizontal(z_0.x);
                a.y = 2 * ChapmanHorizontal(z_0.y);
                Vector2 ch_2 = a * b;

                ch = ch_2 - ch;
            }

            Vector2 optDepth = ch * H;

            Vector3 airExtinction = airExtinctionCoefficient;
            float aerosolExtinction = aerosolExtinctionCoefficient;

            return new Vector3(optDepth.x * airExtinction.x + optDepth.y * aerosolExtinction,
                optDepth.x * airExtinction.y + optDepth.y * aerosolExtinction,
                optDepth.x * airExtinction.z + optDepth.y * aerosolExtinction);
        }

        // Computes transmittance along the light path segment.
        internal static Vector3 EvaluateAtmosphericAttenuation(
            float airScaleHeight, float aerosolScaleHeight, in Vector3 airExtinctionCoefficient, float aerosolExtinctionCoefficient,
            in Vector3 C, float R, in Vector3 L, in Vector3 X)
        {
            float r = Vector3.Distance(X, C);
            float cosHoriz = ComputeCosineOfHorizonAngle(r, R);
            float cosTheta = Vector3.Dot(X - C, L) * Rcp(r);

            if (cosTheta > cosHoriz) // Above horizon
            {
                Vector3 oDepth = ComputeAtmosphericOpticalDepth(
                    airScaleHeight, aerosolScaleHeight, airExtinctionCoefficient, aerosolExtinctionCoefficient,
                    R, r, cosTheta, true);

                Vector3 transm;

                transm.x = Mathf.Exp(-oDepth.x);
                transm.y = Mathf.Exp(-oDepth.y);
                transm.z = Mathf.Exp(-oDepth.z);

                return transm;
            }
            else
            {
                return Vector3.zero;
            }
        }

        internal override Vector3 EvaluateAtmosphericAttenuation(Vector3 sunDirection, Vector3 cameraPosition)
        {
            return EvaluateAtmosphericAttenuation(
                GetAirScaleHeight(), GetAerosolScaleHeight(), GetAirExtinctionCoefficient(), GetAerosolExtinctionCoefficient(),
                GetPlanetCenterPosition(cameraPosition), GetPlanetaryRadius(), sunDirection, cameraPosition);
        }

        /// <summary> Returns the type of the sky renderer. </summary>
        /// <returns> PhysicallyBasedSkyRenderer type. </returns>
        public override Type GetSkyRendererType() { return typeof(PhysicallyBasedSkyRenderer); }

        /// <summary> Doesn't have any effect. </summary>
        [SerializeField, Obsolete("Obsolete parameter, will be removed in 2023.3")]
        public ClampedIntParameter numberOfBounces = new ClampedIntParameter(3, 1, 10);
    }
}
