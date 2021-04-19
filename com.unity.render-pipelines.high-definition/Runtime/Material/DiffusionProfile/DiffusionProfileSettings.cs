using System;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    class DiffusionProfileConstants
    {
        public const int DIFFUSION_PROFILE_COUNT      = 16; // Max. number of profiles, including the slot taken by the neutral profile
        public const int DIFFUSION_PROFILE_NEUTRAL_ID = 0;  // Does not result in blurring
        public const int SSS_PIXELS_PER_SAMPLE        = 4;
    }

    enum DefaultSssSampleBudgetForQualityLevel
    {
        Low    = 20,
        Medium = 40,
        High   = 80,
        Max    = 1000
    }

    [Serializable]
    class DiffusionProfile : IEquatable<DiffusionProfile>
    {
        public enum TexturingMode : uint
        {
            PreAndPostScatter = 0,
            PostScatter = 1
        }

        public enum TransmissionMode : uint
        {
            Regular = 0,
            ThinObject = 1
        }

        [ColorUsage(false, true)]
        public Color            scatteringDistance;         // Per color channel (no meaningful units)
        [ColorUsage(false, true)]
        public Color            transmissionTint;           // HDR color
        public TexturingMode    texturingMode;
        public TransmissionMode transmissionMode;
        public Vector2          thicknessRemap;             // X = min, Y = max (in millimeters)
        public float            worldScale;                 // Size of the world unit in meters
        public float            ior;                        // 1.4 for skin (mean ~0.028)

        public Vector3          shapeParam   { get; private set; }          // RGB = shape parameter: S = 1 / D
        public float            filterRadius { get; private set; }          // In millimeters
        public float            maxScatteringDistance { get; private set; } // No meaningful units

        // Unique hash used in shaders to identify the index in the diffusion profile array
        public uint             hash = 0;

        // Here we need to have one parameter in the diffusion profile parameter because the deserialization call the default constructor
        public DiffusionProfile(bool dontUseDefaultConstructor)
        {
            ResetToDefault();
        }

        public void ResetToDefault()
        {
            scatteringDistance = Color.grey;
            transmissionTint = Color.white;
            texturingMode = TexturingMode.PreAndPostScatter;
            transmissionMode = TransmissionMode.ThinObject;
            thicknessRemap = new Vector2(0f, 5f);
            worldScale = 1f;
            ior = 1.4f; // Typical value for skin specular reflectance
        }

        internal void Validate()
        {
            thicknessRemap.y = Mathf.Max(thicknessRemap.y, 0f);
            thicknessRemap.x = Mathf.Clamp(thicknessRemap.x, 0f, thicknessRemap.y);
            worldScale       = Mathf.Max(worldScale, 0.001f);
            ior              = Mathf.Clamp(ior, 1.0f, 2.0f);

            UpdateKernel();
        }

        // Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar.
        void UpdateKernel()
        {
            Vector3 sd = (Vector3)(Vector4)scatteringDistance;

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
            float n = Mathf.Pow(g, -1.0f/3.0f);                      // g^(-1/3)
            float p = (g * n) * n;                                   // g^(+1/3)
            float c = 1 + p + n;                                     // 1 + g^(+1/3) + g^(-1/3)
            float x = 3 * Mathf.Log(c / (4 * u));

            return x * rcpS;
        }

        public bool Equals(DiffusionProfile other)
        {
            if (other == null)
                return false;

            return  scatteringDistance == other.scatteringDistance &&
                    transmissionTint == other.transmissionTint &&
                    texturingMode == other.texturingMode &&
                    transmissionMode == other.transmissionMode &&
                    thicknessRemap == other.thicknessRemap &&
                    worldScale == other.worldScale &&
                    ior == other.ior;
        }
    }

    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Diffusion-Profile" + Documentation.endURL)]
    internal partial class DiffusionProfileSettings : ScriptableObject
    {
        [SerializeField]
        internal DiffusionProfile profile;

        [NonSerialized] internal Vector4 worldScaleAndFilterRadiusAndThicknessRemap; // X = meters per world unit, Y = filter radius (in mm), Z = remap start, W = end - start
        [NonSerialized] internal Vector4 shapeParamAndMaxScatterDist;                // RGB = S = 1 / D, A = d = RgbMax(D)
        [NonSerialized] internal Vector4 transmissionTintAndFresnel0;                // RGB = color, A = fresnel0
        [NonSerialized] internal Vector4 disabledTransmissionTintAndFresnel0;        // RGB = black, A = fresnel0 - For debug to remove the transmission
        [NonSerialized] internal int updateCount;

        void OnEnable()
        {
            if (profile == null)
                profile = new DiffusionProfile(true);

            profile.Validate();
            UpdateCache();

#if UNITY_EDITOR
            if (m_Version != MigrationDescription.LastVersion<Version>())
            {
                // We delay the upgrade of the diffusion profile because in the OnEnable we are still
                // in the import of the current diffusion profile, so we can't create new assets of the same
                // type from here otherwise it will freeze the editor in an infinite import loop.
                // Thus we delay the upgrade of one editor frame so the import of this asset is finished.
                UnityEditor.EditorApplication.delayCall += TryToUpgrade;
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
            shapeParamAndMaxScatterDist   = profile.shapeParam;
            shapeParamAndMaxScatterDist.w = profile.maxScatteringDistance;
            // Convert ior to fresnel0
            float fresnel0 = (profile.ior - 1.0f) / (profile.ior + 1.0f);
            fresnel0 *= fresnel0; // square
            transmissionTintAndFresnel0 = new Vector4(profile.transmissionTint.r * 0.25f, profile.transmissionTint.g * 0.25f, profile.transmissionTint.b * 0.25f, fresnel0); // Premultiplied
            disabledTransmissionTintAndFresnel0 = new Vector4(0.0f, 0.0f, 0.0f, fresnel0);

            updateCount++;
        }

        internal bool HasChanged(int update)
        {
            return update == updateCount;
        }

        /// <summary>
        /// Initialize the settings for the default diffusion profile.
        /// </summary>
        public void SetDefaultParams()
        {
            worldScaleAndFilterRadiusAndThicknessRemap = new Vector4(1, 0, 0, 1);
            shapeParamAndMaxScatterDist                = new Vector4(16777216, 16777216, 16777216, 0);
            transmissionTintAndFresnel0.w              = 0.04f; // Match DEFAULT_SPECULAR_VALUE defined in Lit.hlsl
        }
    }
}
