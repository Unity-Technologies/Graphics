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
    /// Physically Based Sky Volume Component.
    /// </summary>
    [VolumeComponentMenu("Sky/Physically Based Sky")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [SkyUniqueID((int)SkyType.PhysicallyBased)]
    [HDRPHelpURL("create-a-physically-based-sky")]
    public partial class PhysicallyBasedSky : SkySettings
    {
        /// <summary>
        /// The mode to render the sky.
        /// </summary>
        public enum RenderingMode
        {
            /// <summary>Use the default shader with the artistic overrides from the volume parameters.</summary>
            Default,
            /// <summary>Use a custom Material</summary>
            Material,
        };

        /* We use the measurements from Earth as the defaults. */
        const float k_DefaultEarthRadius = 6.3781f * 1000000;
        const float k_DefaultAirScatteringR = 5.8f / 1000000; // at 680 nm, without ozone
        const float k_DefaultAirScatteringG = 13.5f / 1000000; // at 550 nm, without ozone
        const float k_DefaultAirScatteringB = 33.1f / 1000000; // at 440 nm, without ozone
        const float k_DefaultAirScaleHeight = 8000;
        const float k_DefaultAerosolScaleHeight = 1200;
        static readonly float k_DefaultAerosolMaximumAltitude = LayerDepthFromScaleHeight(k_DefaultAerosolScaleHeight);
        static readonly float k_DefaultOzoneMinimumAltitude = 20.0f * 1000.0f; // 20km
        static readonly float k_DefaultOzoneLayerWidth = 20.0f * 1000.0f; // 20km

        internal static Material s_DefaultMaterial = null;

        /// <summary> Indicates a preset HDRP uses to simplify the Inspector. </summary>
        [Tooltip("Indicates a preset HDRP uses to simplify the Inspector.")]
        public EnumParameter<PhysicallyBasedSkyModel> type = new (PhysicallyBasedSkyModel.EarthAdvanced);

        /// <summary> Enable atmopsheric scattering on opaque and transparents </summary>
        [Tooltip("Enables atmospheric attenuation on objects when viewed from a distance. This is responsible for the blue tint on distant montains or clouds.")]
        public BoolParameter atmosphericScattering = new BoolParameter(true);


        /// <summary> Use the default shader or a custom material to render the atmosphere. </summary>
        [Tooltip("Indicates wether HDRP should use the default shader with the textures set on the profile or a custom material to render the planet and space.")]
        public EnumParameter<RenderingMode> renderingMode = new (RenderingMode.Default);

        /// <summary> The material used for sky rendering. </summary>
        [Tooltip("The material used to render the sky. It is recommended to use the **Physically Based Sky** Material type of ShaderGraph.")]
        public MaterialParameter material = new MaterialParameter(s_DefaultMaterial);


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
        public ColorParameter airTint = new ColorParameter(Color.white, hdr: false, showAlpha: false, showEyeDropper: true);

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
        public ClampedFloatParameter aerosolAnisotropy = new ClampedFloatParameter(0.8f, -1, 1);


        /// <summary> Controls the ozone density in the atmosphere. </summary>
        [Tooltip("Controls the ozone density in the atmosphere.")]
        public ClampedFloatParameter ozoneDensityDimmer = new ClampedFloatParameter(1.0f, 0, 1);
        /// <summary>Controls the minimum altitude of ozone in the atmosphere. </summary>
        [Tooltip("Controls the minimum altitude of ozone in the atmosphere.")]
        public MinFloatParameter ozoneMinimumAltitude = new MinFloatParameter(k_DefaultOzoneMinimumAltitude, 0);
        /// <summary> Controls the width of the ozone layer in the atmosphere. </summary>
        [Tooltip("Controls the width of the ozone layer in the atmosphere.")]
        public MinFloatParameter ozoneLayerWidth = new MinFloatParameter(k_DefaultOzoneLayerWidth, 0);


        /// <summary> Ground tint. </summary>
        [Tooltip("Specifies a color that HDRP uses to tint the Ground Color Texture.")]
        public ColorParameter groundTint = new ColorParameter(new Color(0.12f, 0.10f, 0.09f), hdr: false, showAlpha: false, showEyeDropper: false);

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
        public ColorParameter horizonTint = new ColorParameter(Color.white, hdr: false, showAlpha: false, showEyeDropper: true);

        /// <summary> Zenith tint. Does not affect the precomputation. </summary>
        [Tooltip("Specifies a color that HDRP uses to tint the point in the sky directly above the observer (the zenith). Does not affect the precomputation.")]
        public ColorParameter zenithTint = new ColorParameter(Color.white, hdr: false, showAlpha: false, showEyeDropper: true);

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
            Vector3 airAlb = Vector3.one;

            if (type.value == PhysicallyBasedSkyModel.Custom)
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

        internal Vector3 GetOzoneExtinctionCoefficient()
        {
            Vector3 absorption = new Vector3(0.00065f, 0.00188f, 0.00008f) / 1000.0f;
            if (type.value != PhysicallyBasedSkyModel.EarthSimple)
                absorption *= ozoneDensityDimmer.value;
            return absorption;
        }

        internal float GetOzoneLayerWidth()
        {
            if (type.value == PhysicallyBasedSkyModel.Custom)
                return ozoneLayerWidth.value;
            return k_DefaultOzoneLayerWidth;
        }

        internal float GetOzoneLayerMinimumAltitude()
        {
            if (type.value == PhysicallyBasedSkyModel.Custom)
                return ozoneMinimumAltitude.value;
            return k_DefaultOzoneMinimumAltitude;
        }

        PhysicallyBasedSky()
        {
            displayName = "Physically Based Sky";
        }


        internal int GetPrecomputationHashCode(HDCamera hdCamera)
        {
            int hash = GetPrecomputationHashCode();
            hash = hash * 23 + hdCamera.planet.radius.GetHashCode();
            hash = hash * 23 + hdCamera.planet.renderingSpace.GetHashCode();
            return hash;
        }

        internal int GetPrecomputationHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                // These parameters affect precomputation.
                hash = hash * 23 + type.GetHashCode();
                hash = hash * 23 + atmosphericScattering.GetHashCode();
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

                hash = hash * 23 + ozoneDensityDimmer.GetHashCode();
                hash = hash * 23 + ozoneMinimumAltitude.GetHashCode();
                hash = hash * 23 + ozoneLayerWidth.GetHashCode();
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
            ref var planet = ref HDCamera.GetOrCreate(camera).planet;

            int hash = GetHashCode();
            hash = hash * 23 + planet.radius.GetHashCode();
            hash = hash * 23 + planet.renderingSpace.GetHashCode();
            if (planet.renderingSpace != RenderingSpace.Camera)
                hash = hash * 23 + planet.center.GetHashCode();
            return hash;
        }

        /// <summary> Returns the hash code of the parameters of the sky. </summary>
        /// <returns> The hash code of the parameters of the sky. </returns>
        public override int GetHashCode()
        {
            int hash = GetPrecomputationHashCode();

            unchecked
            {
                // These parameters do NOT affect precomputation.
                hash = hash * 23 + renderingMode.GetHashCode();
                hash = hash * 23 + material.GetHashCode();
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

        static float OzoneDensity(float height, Vector2 ozoneScaleOffset)
        {
            return Mathf.Clamp01(1 - Mathf.Abs(height * ozoneScaleOffset.x + ozoneScaleOffset.y));
        }

        // See IntersectSphere in PhysicallyBasedSkyCommon.hlsl
        static internal Vector2 IntersectSphere(float sphereRadius, float cosChi, float radialDistance, float rcpRadialDistance)
        {
            float d = Mathf.Pow(sphereRadius * rcpRadialDistance, 2.0f) - Mathf.Clamp01(1.0f - cosChi * cosChi);
            return (d < 0.0f) ? new Vector2(d, d) : (radialDistance * new Vector2(-cosChi - Mathf.Sqrt(d), -cosChi + Mathf.Sqrt(d)));
        }

        static float ComputeOzoneOpticalDepth(float R, float r, float cosTheta, float ozoneMinimumAltitude, float ozoneLayerWidth)
        {
            float ozoneOD = 0.0f;

            Vector2 tInner = IntersectSphere(R + ozoneMinimumAltitude, cosTheta, r, 1.0f / r);
            Vector2 tOuter = IntersectSphere(R + ozoneMinimumAltitude + ozoneLayerWidth, cosTheta, r, 1.0f / r);
            float tEntry, tEntry2, tExit, tExit2;

            if (tInner.x < 0.0 && tInner.y >= 0.0) // Below the lower bound
            {
                // The ray starts at the intersection with the lower bound and ends at the intersection with the outer bound
                tEntry = tInner.y;
                tExit2 = tOuter.y;
                tEntry2 = tExit = (tExit2 - tEntry) * 0.5f;
            }
            else // Inside or above the volume
            {
                // The ray starts at the intersection with the outer bound, or at 0 if we are inside
                // The ray ends at the lower bound if we hit it, at the outer bound otherwise
                tEntry = Mathf.Max(tOuter.x, 0.0f);
                tExit = tInner.x >= 0.0 ? tInner.x : tOuter.y;

                // If we hit the lower bound, we may intersect the volume a second time
                if (tInner.x >= 0.0)
                {
                    tEntry2 = tInner.y;
                    tExit2 = tOuter.y;
                }
                else
                {
                    tExit2 = tExit;
                    tEntry2 = tExit = (tExit2 - tEntry) * 0.5f;
                }
            }

            uint count = 2;
            float rcpCount = 1.0f / count;
            float dt = (tExit - tEntry) * rcpCount;
            float dt2 = (tExit2 - tEntry2) * rcpCount;
            Vector2 ozoneScaleOffset = new Vector2(2.0f / ozoneLayerWidth, -2.0f * ozoneMinimumAltitude / ozoneLayerWidth - 1.0f);

            for (uint i = 0; i < count; i++)
            {
                float t = Mathf.Lerp(tEntry, tExit, (i + 0.5f) * rcpCount);
                float t2 = Mathf.Lerp(tEntry2, tExit2, (i + 0.5f) * rcpCount);
                float h = Mathf.Sqrt(r * r + t * (2 * r * cosTheta + t)) - R;
                float h2 = Mathf.Sqrt(r * r + t2 * (2 * r * cosTheta + t2)) - R;

                ozoneOD += OzoneDensity(h, ozoneScaleOffset) * dt;
                ozoneOD += OzoneDensity(h2, ozoneScaleOffset) * dt2;
            }

            return ozoneOD * 0.6f;
        }


        static Vector3 ComputeAtmosphericOpticalDepth(
            float airScaleHeight, float aerosolScaleHeight, in Vector3 airExtinctionCoefficient, float aerosolExtinctionCoefficient,
            float ozoneMinimumAltitude, float ozoneLayerWidth, Vector3 ozoneExtinctionCoefficient,
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

            float ozoneOD = alwaysAboveHorizon ? ComputeOzoneOpticalDepth(R, r, cosTheta, ozoneMinimumAltitude, ozoneLayerWidth) : 0.0f;

            Vector3 airExtinction = airExtinctionCoefficient;
            float aerosolExtinction = aerosolExtinctionCoefficient;
            Vector3 ozoneExtinction = ozoneExtinctionCoefficient;

            return new Vector3(optDepth.x * airExtinction.x + optDepth.y * aerosolExtinction + ozoneOD * ozoneExtinction.x,
                optDepth.x * airExtinction.y + optDepth.y * aerosolExtinction + ozoneOD * ozoneExtinction.y,
                optDepth.x * airExtinction.z + optDepth.y * aerosolExtinction + ozoneOD * ozoneExtinction.z);
        }

        // Computes transmittance along the light path segment.
        internal static Vector3 EvaluateAtmosphericAttenuation(
            float airScaleHeight, float aerosolScaleHeight, in Vector3 airExtinctionCoefficient, float aerosolExtinctionCoefficient,
            float ozoneMinimumAltitude, float ozoneLayerWidth, Vector3 ozoneExtinctionCoefficient,
            in Vector3 C, float R, in Vector3 L, in Vector3 X)
        {
            float r = Vector3.Distance(X, C);
            float cosHoriz = ComputeCosineOfHorizonAngle(r, R);
            float cosTheta = Vector3.Dot(X - C, L) * Rcp(r);

            if (cosTheta > cosHoriz) // Above horizon
            {
                Vector3 oDepth = ComputeAtmosphericOpticalDepth(
                    airScaleHeight, aerosolScaleHeight, airExtinctionCoefficient, aerosolExtinctionCoefficient,
                    ozoneMinimumAltitude, ozoneLayerWidth, ozoneExtinctionCoefficient,
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
            HDCamera.PlanetData planet = new();
            var profile = SkyManager.GetStaticLightingSky()?.profile;
            if (profile != null && profile.TryGet<VisualEnvironment>(out var env))
                planet.Set(cameraPosition, env);
            else
                planet.Init(VisualEnvironment.k_DefaultEarthRadius);

            return EvaluateAtmosphericAttenuation(
                GetAirScaleHeight(), GetAerosolScaleHeight(), GetAirExtinctionCoefficient(), GetAerosolExtinctionCoefficient(),
                GetOzoneLayerMinimumAltitude(), GetOzoneLayerWidth(), GetOzoneExtinctionCoefficient(),
                planet.center, planet.radius, sunDirection, cameraPosition);
        }

        /// <summary> Returns the type of the sky renderer. </summary>
        /// <returns> PhysicallyBasedSkyRenderer type. </returns>
        public override Type GetSkyRendererType() { return typeof(PhysicallyBasedSkyRenderer); }

        /// <summary>
        /// Called though reflection by the VolumeManager.
        /// </summary>
        static void Init()
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineRuntimeMaterials>(out var materials))
            {
                s_DefaultMaterial = materials.pbrSkyMaterial;
            }
        }
    }
}
