using System;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    class DiffusionProfileConstants
    {
        public const int DIFFUSION_PROFILE_COUNT = 16; // Max. number of profiles, including the slot taken by the neutral profile
        public const int DIFFUSION_PROFILE_NEUTRAL_ID = 0;  // Does not result in blurring
        public const int SSS_PIXELS_PER_SAMPLE = 4;
    }

    enum DefaultSssSampleBudgetForQualityLevel
    {
        Low = 20,
        Medium = 40,
        High = 80,
        Max = 1000
    }

    enum DefaultSssDownsampleSteps
    {
        Low = 0,
        Medium = 0,
        High = 0,
        Max = 2
    }

    [Serializable]
    class DiffusionProfile : IEquatable<DiffusionProfile>
    {
        public enum TexturingMode : uint
        {
            [InspectorName("Pre and Post-Scatter")]
            [Tooltip("Partially applies the albedo to the Material twice, before and after the subsurface scattering pass, for a softer look.")]
            PreAndPostScatter = 0,
            [InspectorName("Post-Scatter")]
            [Tooltip("Applies the albedo to the Material after the subsurface scattering pass, so the contents of the albedo texture aren't blurred.")]
            PostScatter = 1
        }

        public enum TransmissionMode : uint
        {
            [InspectorName("Thick Object")]
            [Tooltip("Select this mode for geometrically thick objects. This mode uses shadow maps.")]
            Regular = 0,
            [InspectorName("Thin Object")]
            [Tooltip("Select this mode for thin, double-sided geometry, such as paper or leaves.")]
            ThinObject = 1
        }

        [ColorUsage(false, false)]
        public Color scatteringDistance; // Per color channel (no meaningful units)
        [Min(0.0f)]
        public float scatteringDistanceMultiplier;
        [ColorUsage(false, true)]
        public Color transmissionTint;           // HDR color
        [Tooltip("Specifies when HDRP applies the albedo of the Material.")]
        public TexturingMode texturingMode;
        [Range(1.0f, 2.0f)]
        public Vector2 smoothnessMultipliers;
        [Range(0.0f, 1.0f), Tooltip("Amount of mixing between the primary and secondary specular lobes.")]
        public float lobeMix;
        [Range(1.0f, 3.0f), Tooltip("Exponent on the cosine component of the diffuse lobe.\nHelps to simulate surfaces with strong subsurface scattering.")]
        public float diffuseShadingPower;
        public TransmissionMode transmissionMode;
        public Vector2 thicknessRemap;             // X = min, Y = max (in millimeters)
        public float worldScale;                 // Size of the world unit in meters
        public float ior;                        // 1.4 for skin (mean ~0.028)

        public Vector3 shapeParam { get; private set; }          // RGB = shape parameter: S = 1 / D
        public float filterRadius { get; private set; }          // In millimeters
        public float maxScatteringDistance { get; private set; } // No meaningful units

        // Unique hash used in shaders to identify the index in the diffusion profile array
        public uint hash = 0;

        // Here we need to have one parameter in the diffusion profile parameter because the deserialization call the default constructor
        public DiffusionProfile(bool dontUseDefaultConstructor)
        {
            ResetToDefault();
        }

        public void ResetToDefault()
        {
            scatteringDistance = Color.grey;
            scatteringDistanceMultiplier = 1;
            transmissionTint = Color.white;
            texturingMode = TexturingMode.PreAndPostScatter;
            smoothnessMultipliers = Vector2.one;
            lobeMix = 0.5f;
            diffuseShadingPower = 1.0f;
            transmissionMode = TransmissionMode.ThinObject;
            thicknessRemap = new Vector2(0f, 5f);
            worldScale = 1f;
            ior = 1.4f; // Typical value for skin specular reflectance
        }

        internal void Validate()
        {
            thicknessRemap.y = Mathf.Max(thicknessRemap.y, 0f);
            thicknessRemap.x = Mathf.Clamp(thicknessRemap.x, 0f, thicknessRemap.y);
            worldScale = Mathf.Max(worldScale, 0.001f);
            ior = Mathf.Clamp(ior, 1.0f, 2.0f);

            // Default values for serializable classes do not work, they are set to 0
            // if we detect this case, we initialize them to the right default
            if (diffuseShadingPower == 0.0f)
            {
                smoothnessMultipliers = Vector2.one;
                lobeMix = 0.5f;
                diffuseShadingPower = 1.0f;
            }

            UpdateKernel();
        }

        // Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar.
        void UpdateKernel()
        {
            Vector3 sd = scatteringDistanceMultiplier * (Vector3)(Vector4)scatteringDistance;

            // Rather inconvenient to support (S = Inf).
            shapeParam = new Vector3(Mathf.Min(16777216, 1.0f / sd.x),
                Mathf.Min(16777216, 1.0f / sd.y),
                Mathf.Min(16777216, 1.0f / sd.z));

            // Filter radius is, strictly speaking, infinite.
            // The magnitude of the function decays exponentially, but it is never truly zero.
            // To estimate the radius, we can use adapt the "three-sigma rule" by defining
            // the radius of the kernel by the value of the CDF which corresponds to 99.7%
            // of the energy of the filter.
            float cdf = 0.997f;

            // Importance sample the normalized diffuse reflectance profile for the computed value of 's'.
            // ------------------------------------------------------------------------------------
            // R[r, phi, s]   = s * (Exp[-r * s] + Exp[-r * s / 3]) / (8 * Pi * r)
            // PDF[r, phi, s] = r * R[r, phi, s]
            // CDF[r, s]      = 1 - 1/4 * Exp[-r * s] - 3/4 * Exp[-r * s / 3]
            // ------------------------------------------------------------------------------------
            // We importance sample the color channel with the widest scattering distance.
            maxScatteringDistance = Mathf.Max(sd.x, sd.y, sd.z);

            filterRadius = SampleBurleyDiffusionProfile(cdf, maxScatteringDistance);
        }

        static float DisneyProfile(float r, float s)
        {
            return s * (Mathf.Exp(-r * s) + Mathf.Exp(-r * s * (1.0f / 3.0f))) / (8.0f * Mathf.PI * r);
        }

        static float DisneyProfilePdf(float r, float s)
        {
            return r * DisneyProfile(r, s);
        }

        static float DisneyProfileCdf(float r, float s)
        {
            return 1.0f - 0.25f * Mathf.Exp(-r * s) - 0.75f * Mathf.Exp(-r * s * (1.0f / 3.0f));
        }

        static float DisneyProfileCdfDerivative1(float r, float s)
        {
            return 0.25f * s * Mathf.Exp(-r * s) * (1.0f + Mathf.Exp(r * s * (2.0f / 3.0f)));
        }

        static float DisneyProfileCdfDerivative2(float r, float s)
        {
            return (-1.0f / 12.0f) * s * s * Mathf.Exp(-r * s) * (3.0f + Mathf.Exp(r * s * (2.0f / 3.0f)));
        }

        // The CDF is not analytically invertible, so we use Halley's Method of root finding.
        // { f(r, s, p) = CDF(r, s) - p = 0 } with the initial guess { r = (10^p - 1) / s }.
        static float DisneyProfileCdfInverse(float p, float s)
        {
            // Supply the initial guess.
            float r = (Mathf.Pow(10f, p) - 1f) / s;
            float t = float.MaxValue;

            while (true)
            {
                float f0 = DisneyProfileCdf(r, s) - p;
                float f1 = DisneyProfileCdfDerivative1(r, s);
                float f2 = DisneyProfileCdfDerivative2(r, s);
                float dr = f0 / (f1 * (1f - f0 * f2 / (2f * f1 * f1)));

                if (Mathf.Abs(dr) < t)
                {
                    r = r - dr;
                    t = Mathf.Abs(dr);
                }
                else
                {
                    // Converged to the best result.
                    break;
                }
            }

            return r;
        }

        // https://zero-radiance.github.io/post/sampling-diffusion/
        // Performs sampling of a Normalized Burley diffusion profile in polar coordinates.
        // 'u' is the random number (the value of the CDF): [0, 1).
        // rcp(s) = 1 / ShapeParam = ScatteringDistance.
        // Returns the sampled radial distance, s.t. (u = 0 -> r = 0) and (u = 1 -> r = Inf).
        static float SampleBurleyDiffusionProfile(float u, float rcpS)
        {
            u = 1 - u; // Convert CDF to CCDF

            float g = 1 + (4 * u) * (2 * u + Mathf.Sqrt(1 + (4 * u) * u));
            float n = Mathf.Pow(g, -1.0f / 3.0f);                      // g^(-1/3)
            float p = (g * n) * n;                                   // g^(+1/3)
            float c = 1 + p + n;                                     // 1 + g^(+1/3) + g^(-1/3)
            float x = 3 * Mathf.Log(c / (4 * u));

            return x * rcpS;
        }

        public bool Equals(DiffusionProfile other)
        {
            if (other == null)
                return false;

            return scatteringDistance == other.scatteringDistance &&
                scatteringDistanceMultiplier == other.scatteringDistanceMultiplier &&
                transmissionTint == other.transmissionTint &&
                texturingMode == other.texturingMode &&
                transmissionMode == other.transmissionMode &&
                thicknessRemap == other.thicknessRemap &&
                worldScale == other.worldScale &&
                ior == other.ior;
        }
    }

    /// <summary>
    /// Class for Diffusion Profile settings
    /// </summary>
    [HDRPHelpURLAttribute("diffusion-profile-reference")]
    [Icon("Packages/com.unity.render-pipelines.high-definition/Editor/Icons/Processed/DiffusionProfile Icon.asset")]
    public partial class DiffusionProfileSettings : ScriptableObject
    {
        [SerializeField]
        internal DiffusionProfile profile;

        [NonSerialized] internal Vector4 worldScaleAndFilterRadiusAndThicknessRemap; // X = meters per world unit, Y = filter radius (in mm), Z = remap start, W = end - start
        [NonSerialized] internal Vector4 shapeParamAndMaxScatterDist;                // RGB = S = 1 / D, A = d = RgbMax(D)
        [NonSerialized] internal Vector4 transmissionTintAndFresnel0;                // RGB = color, A = fresnel0
        [NonSerialized] internal Vector4 disabledTransmissionTintAndFresnel0;        // RGB = black, A = fresnel0 - For debug to remove the transmission
        [NonSerialized] internal Vector4 dualLobeAndDiffusePower;                    // R = Smoothness A, G = Smoothness B, B = Lobe Mix, A = Diffuse Power - 1 (to have 0 as neutral value)
        [NonSerialized] internal int updateCount;

        /// <summary>
        /// Scattering distance. Determines the shape of the profile, and the blur radius of the filter per color channel. Alpha is ignored.
        /// </summary>
        public Color scatteringDistance
        {
            get
            {
                return profile.scatteringDistance * profile.scatteringDistanceMultiplier;
            }
            set
            {
                HDUtils.ConvertHDRColorToLDR(value, out profile.scatteringDistance, out profile.scatteringDistanceMultiplier);
                profile.Validate(); UpdateCache();
            }
        }

        /// <summary>
        /// Effective radius of the filter (in millimeters).
        /// </summary>
        public float maximumRadius { get => profile.filterRadius; }

        /// <summary>
        /// Index of refraction. For reference, skin is 1.4 and most materials are between 1.3 and 1.5.
        /// </summary>
        public float indexOfRefraction
        {
            get => profile.ior;
            set { profile.ior = value; profile.Validate(); UpdateCache(); }
        }

        /// <summary>
        /// Size of the world unit in meters.
        /// </summary>
        public float worldScale
        {
            get => profile.worldScale;
            set { profile.worldScale = value; profile.Validate(); UpdateCache(); }
        }

        /// <summary>
        /// Multiplier for the primary specular lobe. This multiplier is clamped between 1 and 2.
        /// </summary>
        public float primarySmoothnessMultiplier
        {
            get => profile.smoothnessMultipliers.y;
            set { profile.smoothnessMultipliers.y = Mathf.Clamp(value, 1, 2); UpdateCache(); }
        }

        /// <summary>
        /// Multiplier for the secondary specular lobe. This multiplier is clamped between 0 and 1.
        /// </summary>
        public float secondarySmoothnessMultiplier
        {
            get => profile.smoothnessMultipliers.x;
            set { profile.smoothnessMultipliers.x = Mathf.Clamp(value, 0, 1); UpdateCache(); }
        }

        /// <summary>
        /// Amount of mixing between the primary and secondary specular lobes.
        /// </summary>
        public float lobeMix
        {
            get => profile.lobeMix;
            set { profile.lobeMix = value; UpdateCache(); }
        }

        /// <summary>
        /// Exponent on the cosine component of the diffuse lobe.\nHelps to simulate non lambertian surfaces.
        /// </summary>
        public float diffuseShadingPower
        {
            get => profile.diffuseShadingPower;
            set { profile.diffuseShadingPower = value; UpdateCache(); }
        }

        /// <summary>
        /// Color which tints transmitted light. Alpha is ignored.
        /// </summary>
        public Color transmissionTint
        {
            get => profile.transmissionTint;
            set { profile.transmissionTint = value; profile.Validate(); UpdateCache(); }
        }

        void OnEnable()
        {
            if (profile == null)
                profile = new DiffusionProfile(true);

            profile.Validate();
            UpdateCache();

#if UNITY_EDITOR
            if (m_Version != MigrationDescription.LastVersion<Version>())
            {
                // Initial migration step requires creating assets
                // We delay the upgrade of the diffusion profile because in the OnEnable we are still
                // in the import of the current diffusion profile, so we can't create new assets of the same
                // type from here otherwise it will freeze the editor in an infinite import loop.
                // Thus we delay the upgrade of one editor frame so the import of this asset is finished.
                if (m_Version == Version.Initial)
                    UnityEditor.EditorApplication.delayCall += TryToUpgrade;
                else
                    k_Migration.Migrate(this);
            }

            UnityEditor.Rendering.HighDefinition.DiffusionProfileHashTable.UpdateDiffusionProfileHashNow(this);
#endif
        }

#if UNITY_EDITOR
        internal void Reset()
        {
            if (profile != null && profile.hash == 0)
            {
                profile.ResetToDefault();
                profile.hash = DiffusionProfileHashTable.GenerateUniqueHash(this);
            }
        }

#endif
        internal void UpdateCache()
        {
            worldScaleAndFilterRadiusAndThicknessRemap = new Vector4(profile.worldScale,
                profile.filterRadius,
                profile.thicknessRemap.x,
                profile.thicknessRemap.y - profile.thicknessRemap.x);
            shapeParamAndMaxScatterDist = profile.shapeParam;
            shapeParamAndMaxScatterDist.w = profile.maxScatteringDistance;
            // Convert ior to fresnel0
            float fresnel0 = (profile.ior - 1.0f) / (profile.ior + 1.0f);
            fresnel0 *= fresnel0; // square
            transmissionTintAndFresnel0 = new Vector4(profile.transmissionTint.r * 0.25f, profile.transmissionTint.g * 0.25f, profile.transmissionTint.b * 0.25f, fresnel0); // Premultiplied
            disabledTransmissionTintAndFresnel0 = new Vector4(0.0f, 0.0f, 0.0f, fresnel0);
            float smoothnessB = profile.lobeMix == 0.0f ? 1.0f : profile.smoothnessMultipliers.y; // this helps shader determine if dual lobe is active
            dualLobeAndDiffusePower = new Vector4(profile.smoothnessMultipliers.x, smoothnessB, profile.lobeMix, profile.diffuseShadingPower - 1.0f);

            updateCount++;
        }

        internal bool HasChanged(int update)
        {
            return update == updateCount;
        }

        /// <summary>
        /// Initialize the settings for the default diffusion profile.
        /// </summary>
        internal void SetDefaultParams()
        {
            worldScaleAndFilterRadiusAndThicknessRemap = new Vector4(1, 0, 0, 1);
            shapeParamAndMaxScatterDist = new Vector4(16777216, 16777216, 16777216, 0);
            transmissionTintAndFresnel0.w = 0.04f; // Match DEFAULT_SPECULAR_VALUE defined in Lit.hlsl
        }
    }
}
