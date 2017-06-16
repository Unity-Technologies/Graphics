using System;
#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public class SssConstants
    {
        public const int SSS_N_PROFILES           = 8;  // Max. number of profiles, including the slot taken by the neutral profile
        public const int SSS_NEUTRAL_PROFILE_ID   = SSS_N_PROFILES - 1; // Does not result in blurring
        public const int SSS_N_SAMPLES_NEAR_FIELD = 55; // Used for extreme close ups; must be a Fibonacci number
        public const int SSS_N_SAMPLES_FAR_FIELD  = 21; // Used at a regular distance; must be a Fibonacci number
        public const int SSS_LOD_THRESHOLD        = 4;  // The LoD threshold of the near-field kernel (in pixels)
        public const int SSS_TRSM_MODE_NONE       = 0;
        public const int SSS_TRSM_MODE_THIN       = 1;
        // Old SSS Model >>>
        public const int SSS_BASIC_N_SAMPLES      = 11; // Must be an odd number
        public const int SSS_BASIC_DISTANCE_SCALE = 3;  // SSS distance units per centimeter
        // <<< Old SSS Model
    }

    [Serializable]
    public class SubsurfaceScatteringProfile : ScriptableObject
    {
        public enum TexturingMode    : uint { PreAndPostScatter = 0, PostScatter = 1 };
        public enum TransmissionMode : uint { None = SssConstants.SSS_TRSM_MODE_NONE, ThinObject = SssConstants.SSS_TRSM_MODE_THIN, Regular };

        [ColorUsage(false, true, 0f, 8f, 0.125f, 3f)]
        public Color            scatteringDistance;         // Per color channel (no meaningful units)
        [ColorUsage(false)]
        public Color            transmissionTint;           // Color, 0 to 1
        public TexturingMode    texturingMode;
        public TransmissionMode transmissionMode;
        public Vector2          thicknessRemap;             // X = min, Y = max (in millimeters)
        public float            worldScale;                 // Size of the world unit in meters
        [HideInInspector]
        public int              settingsIndex;              // SubsurfaceScatteringSettings.profiles[i]
        [SerializeField]
        Vector3                 m_ShapeParam;               // RGB = shape parameter: S = 1 / D
        [SerializeField]
        float                   m_MaxRadius;                // In millimeters
        [SerializeField]
        Vector2[]               m_FilterKernelNearField;    // X = radius, Y = reciprocal of the PDF
        [SerializeField]
        Vector2[]               m_FilterKernelFarField;     // X = radius, Y = reciprocal of the PDF
        // Old SSS Model >>>
        [ColorUsage(false, true, 0f, 8f, 0.125f, 3f)]
        public Color            scatterDistance1;
        [ColorUsage(false, true, 0f, 8f, 0.125f, 3f)]
        public Color            scatterDistance2;
        [Range(0f, 1f)]
        public float            lerpWeight;
        [SerializeField]
        Vector4                 m_HalfRcpWeightedVariances;
        [SerializeField]
        Vector4[]               m_FilterKernelBasic;
        // <<< Old SSS Model

        // --- Public Methods ---

        public SubsurfaceScatteringProfile()
        {
            scatteringDistance = Color.grey;
            transmissionTint   = Color.white;
            texturingMode      = TexturingMode.PreAndPostScatter;
            transmissionMode   = TransmissionMode.None;
            thicknessRemap     = new Vector2(0.0f, 5.0f);
            worldScale         = 1.0f;
            settingsIndex      = SssConstants.SSS_NEUTRAL_PROFILE_ID; // Updated by SubsurfaceScatteringSettings.OnValidate() once assigned
            // Old SSS Model >>>
            scatterDistance1   = new Color(0.3f, 0.3f, 0.3f, 0.0f);
            scatterDistance2   = new Color(0.5f, 0.5f, 0.5f, 0.0f);
            lerpWeight         = 1.0f;
            // <<< Old SSS Model

            BuildKernel();
        }

        // Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar.
        public void BuildKernel()
        {
            if (m_FilterKernelNearField == null || m_FilterKernelNearField.Length != SssConstants.SSS_N_SAMPLES_NEAR_FIELD)
            {
                m_FilterKernelNearField = new Vector2[SssConstants.SSS_N_SAMPLES_NEAR_FIELD];
            }

            if (m_FilterKernelFarField == null || m_FilterKernelFarField.Length != SssConstants.SSS_N_SAMPLES_FAR_FIELD)
            {
                m_FilterKernelFarField = new Vector2[SssConstants.SSS_N_SAMPLES_FAR_FIELD];
            }

            // Clamp to avoid artifacts.
            m_ShapeParam   = new Vector3();
            m_ShapeParam.x = Mathf.Min(1000f, 1.0f / scatteringDistance.r);
            m_ShapeParam.y = Mathf.Min(1000f, 1.0f / scatteringDistance.g);
            m_ShapeParam.z = Mathf.Min(1000f, 1.0f / scatteringDistance.b);

            // We importance sample the color channel with the widest scattering distance.
            float s = Mathf.Min(m_ShapeParam.x, m_ShapeParam.y, m_ShapeParam.z);

            // Importance sample the normalized diffusion profile for the computed value of 's'.
            // ------------------------------------------------------------------------------------
            // R(r, s)   = s * (Exp[-r * s] + Exp[-r * s / 3]) / (8 * Pi * r)
            // PDF(r, s) = s * (Exp[-r * s] + Exp[-r * s / 3]) / 4
            // CDF(r, s) = 1 - 1/4 * Exp[-r * s] - 3/4 * Exp[-r * s / 3]
            // ------------------------------------------------------------------------------------
            
            // Importance sample the near field kernel.
            for (int i = 0; i < SssConstants.SSS_N_SAMPLES_NEAR_FIELD; i++)
            {
                float p = i * (1.0f / SssConstants.SSS_N_SAMPLES_NEAR_FIELD);
                float r = KernelCdfInverse(p, s);
                
                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                m_FilterKernelNearField[i].x = r;
                m_FilterKernelNearField[i].y = 1.0f / KernelPdf(r, s);
            }

            m_MaxRadius = m_FilterKernelNearField[SssConstants.SSS_N_SAMPLES_NEAR_FIELD - 1].x;

            // Importance sample the far field kernel.
            for (int i = 0; i < SssConstants.SSS_N_SAMPLES_FAR_FIELD; i++)
            {
                float p = i * (1.0f / SssConstants.SSS_N_SAMPLES_FAR_FIELD);
                float r = KernelCdfInverse(p, s);

                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                m_FilterKernelFarField[i].x = r;
                m_FilterKernelFarField[i].y = 1.0f / KernelPdf(r, s);
            }

            // Old SSS Model >>>
            UpdateKernelAndVarianceData();
            // <<< Old SSS Model
        }

        // Old SSS Model >>>
        public void UpdateKernelAndVarianceData()
        {
            const int numSamples    = SssConstants.SSS_BASIC_N_SAMPLES;
            const int distanceScale = SssConstants.SSS_BASIC_DISTANCE_SCALE;

            if (m_FilterKernelBasic == null || m_FilterKernelBasic.Length != numSamples)
            {
                m_FilterKernelBasic = new Vector4[numSamples];
            }

            // Apply the three-sigma rule, and rescale.
            Color stdDev1 = ((1.0f / 3.0f) * distanceScale) * scatterDistance1;
            Color stdDev2 = ((1.0f / 3.0f) * distanceScale) * scatterDistance2;

            // Our goal is to blur the image using a filter which is represented
            // as a product of a linear combination of two normalized 1D Gaussians
            // as suggested by Jimenez et al. in "Separable Subsurface Scattering".
            // A normalized (i.e. energy-preserving) 1D Gaussian with the mean of 0
            // is defined as follows: G1(x, v) = exp(-x * x / (2 * v)) / sqrt(2 * Pi * v),
            // where 'v' is variance and 'x' is the radial distance from the origin.
            // Using the weight 'w', our 1D and the resulting 2D filters are given as:
            // A1(v1, v2, w, x)    = G1(x, v1) * (1 - w) + G1(r, v2) * w,
            // A2(v1, v2, w, x, y) = A1(v1, v2, w, x) * A1(v1, v2, w, y).
            // The resulting filter function is a non-Gaussian PDF.
            // It is separable by design, but generally not radially symmetric.

            // N.b.: our scattering distance is rather limited. Therefore, in order to allow
            // for a greater range of standard deviation values for flatter profiles,
            // we rescale the world using 'distanceScale', effectively reducing the SSS
            // distance units from centimeters to (1 / distanceScale).

            // Find the widest Gaussian across 3 color channels.
            float maxStdDev1 = Mathf.Max(stdDev1.r, stdDev1.g, stdDev1.b);
            float maxStdDev2 = Mathf.Max(stdDev2.r, stdDev2.g, stdDev2.b);

            Vector3 weightSum = new Vector3(0, 0, 0);

            float step = 1.0f / (numSamples - 1);

            // Importance sample the linear combination of two Gaussians.
            for (int i = 0; i < numSamples; i++)
            {
                // Generate 'u' on (0, 0.5] and (0.5, 1).
                float u = (i <= numSamples / 2) ? 0.5f - i * step // The center and to the left
                                                : i * step;       // From the center to the right

                u = Mathf.Clamp(u, 0.001f, 0.999f);

                float pos = GaussianCombinationCdfInverse(u, maxStdDev1, maxStdDev2, lerpWeight);
                float pdf = GaussianCombination(pos, maxStdDev1, maxStdDev2, lerpWeight);

                Vector3 val;
                val.x = GaussianCombination(pos, stdDev1.r, stdDev2.r, lerpWeight);
                val.y = GaussianCombination(pos, stdDev1.g, stdDev2.g, lerpWeight);
                val.z = GaussianCombination(pos, stdDev1.b, stdDev2.b, lerpWeight);

                // We do not divide by 'numSamples' since we will renormalize, anyway.
                m_FilterKernelBasic[i].x = val.x * (1 / pdf);
                m_FilterKernelBasic[i].y = val.y * (1 / pdf);
                m_FilterKernelBasic[i].z = val.z * (1 / pdf);
                m_FilterKernelBasic[i].w = pos;

                weightSum.x += m_FilterKernelBasic[i].x;
                weightSum.y += m_FilterKernelBasic[i].y;
                weightSum.z += m_FilterKernelBasic[i].z;
            }

            // Renormalize the weights to conserve energy.
            for (int i = 0; i < numSamples; i++)
            {
                m_FilterKernelBasic[i].x *= 1 / weightSum.x;
                m_FilterKernelBasic[i].y *= 1 / weightSum.y;
                m_FilterKernelBasic[i].z *= 1 / weightSum.z;
            }

            Vector4 weightedStdDev;
            weightedStdDev.x = Mathf.Lerp(stdDev1.r,  stdDev2.r,  lerpWeight);
            weightedStdDev.y = Mathf.Lerp(stdDev1.g,  stdDev2.g,  lerpWeight);
            weightedStdDev.z = Mathf.Lerp(stdDev1.b,  stdDev2.b,  lerpWeight);
            weightedStdDev.w = Mathf.Lerp(maxStdDev1, maxStdDev2, lerpWeight);

            // Store (1 / (2 * WeightedVariance)) per color channel.
            m_HalfRcpWeightedVariances.x = 0.5f / (weightedStdDev.x * weightedStdDev.x);
            m_HalfRcpWeightedVariances.y = 0.5f / (weightedStdDev.y * weightedStdDev.y);
            m_HalfRcpWeightedVariances.z = 0.5f / (weightedStdDev.z * weightedStdDev.z);
            m_HalfRcpWeightedVariances.w = 0.5f / (weightedStdDev.w * weightedStdDev.w);
        }
        // <<< Old SSS Model

        public Vector3 shapeParameter
        {
            // Set in BuildKernel().
            get { return m_ShapeParam; }
        }

        public float maxRadius
        {
            // Set in BuildKernel().
            get { return m_MaxRadius; }
        }

        public Vector2[] filterKernelNearField
        {
            // Set in BuildKernel().
            get { return m_FilterKernelNearField; }
        }
        
        public Vector2[] filterKernelFarField
        {
            // Set in BuildKernel().
            get { return m_FilterKernelFarField; }
        }

        // Old SSS Model >>>
        public Vector4[] filterKernelBasic
        {
            // Set via UpdateKernelAndVarianceData().
            get { return m_FilterKernelBasic; }
        }

        public Vector4 halfRcpWeightedVariances
        {
            // Set via UpdateKernelAndVarianceData().
            get { return m_HalfRcpWeightedVariances; }
        }
        // <<< Old SSS Model

        // --- Private Methods ---

        static float KernelVal(float r, float s)
        {
            return s * (Mathf.Exp(-r * s) + Mathf.Exp(-r * s * (1.0f / 3.0f))) / (8.0f * Mathf.PI * r);
        }

        // Computes the value of the integrand over a disk: (2 * PI * r) * KernelVal().
        static float KernelValCircle(float r, float s)
        {
            return 0.25f * s * (Mathf.Exp(-r * s) + Mathf.Exp(-r * s * (1.0f / 3.0f)));
        }

        static float KernelPdf(float r, float s)
        {
            return KernelValCircle(r, s);
        }

        static float KernelCdf(float r, float s)
        {
            return 1.0f - 0.25f * Mathf.Exp(-r * s) - 0.75f * Mathf.Exp(-r * s * (1.0f / 3.0f));
        }

        static float KernelCdfDerivative1(float r, float s)
        {
            return 0.25f * s * Mathf.Exp(-r * s) * (1.0f + Mathf.Exp(r * s * (2.0f / 3.0f)));
        }

        static float KernelCdfDerivative2(float r, float s)
        {
            return (-1.0f / 12.0f) * s * s * Mathf.Exp(-r * s) * (3.0f + Mathf.Exp(r * s * (2.0f / 3.0f)));
        }

        // The CDF is not analytically invertible, so we use Halley's Method of root finding.
        // { f(r, s, p) = CDF(r, s) - p = 0 } with the initial guess { r = (10^p - 1) / s }.
        static float KernelCdfInverse(float p, float s)
        {
            // Supply the initial guess.
            float r = (Mathf.Pow(10.0f, p) - 1.0f) / s;
            float t = float.MaxValue;

            while (true)
            {
                float f0 = KernelCdf(r, s) - p;
                float f1 = KernelCdfDerivative1(r, s);
                float f2 = KernelCdfDerivative2(r, s);
                float dr = f0 / (f1 * (1.0f - f0 * f2 / (2.0f * f1 * f1)));

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

        // Old SSS Model >>>
        static float Gaussian(float x, float stdDev)
        {
            float variance = stdDev * stdDev;
            return Mathf.Exp(-x * x / (2 * variance)) / Mathf.Sqrt(2 * Mathf.PI * variance);
        }

        static float GaussianCombination(float x, float stdDev1, float stdDev2, float lerpWeight)
        {
            return Mathf.Lerp(Gaussian(x, stdDev1), Gaussian(x, stdDev2), lerpWeight);
        }

        static float RationalApproximation(float t)
        {
            // Abramowitz and Stegun formula 26.2.23.
            // The absolute value of the error should be less than 4.5 e-4.
            float[] c = {2.515517f, 0.802853f, 0.010328f};
            float[] d = {1.432788f, 0.189269f, 0.001308f};
            return t - ((c[2] * t + c[1]) * t + c[0]) / (((d[2] * t + d[1]) * t + d[0]) * t + 1.0f);
        }

        // Ref: https://www.johndcook.com/blog/csharp_phi_inverse/
        static float NormalCdfInverse(float p, float stdDev)
        {
            float x;

            if (p < 0.5)
            {
                // F^-1(p) = - G^-1(p)
                x = -RationalApproximation(Mathf.Sqrt(-2.0f * Mathf.Log(p)));
            }
            else
            {
                // F^-1(p) = G^-1(1-p)
                x = RationalApproximation(Mathf.Sqrt(-2.0f * Mathf.Log(1.0f - p)));
            }

            return x * stdDev;
        }

        static float GaussianCombinationCdfInverse(float p, float stdDev1, float stdDev2, float lerpWeight)
        {
            return Mathf.Lerp(NormalCdfInverse(p, stdDev1), NormalCdfInverse(p, stdDev2), lerpWeight);
        }
        // <<< Old SSS Model
    }

    [Serializable]
    public class SubsurfaceScatteringSettings : ISerializationCallbackReceiver
    {
        public int                           numProfiles;               // Excluding the neutral profile
        public SubsurfaceScatteringProfile[] profiles;
        // Below are the cached values.
        [NonSerialized] public uint          texturingModeFlags;        // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
        [NonSerialized] public uint          transmissionFlags;         // 2 bit/profile; 0 = inf. thick, 1 = thin, 2 = regular
        [NonSerialized] public float[]       thicknessRemaps;           // Remap: 0 = start, 1 = end - start
        [NonSerialized] public float[]       worldScales;               // Size of the world unit in meters
        [NonSerialized] public Vector4[]     shapeParams;               // RGB = S = 1 / D, A = filter radius
        [NonSerialized] public Vector4[]     transmissionTints;         // RGB = color, A = unused
        [NonSerialized] public float[]       filterKernelsNearField;    // 0 = radius, 1 = reciprocal of the PDF
        [NonSerialized] public float[]       filterKernelsFarField;     // 0 = radius, 1 = reciprocal of the PDF
        // Old SSS Model >>>
        public bool                          useDisneySSS;
        [NonSerialized] public Vector4[]     halfRcpWeightedVariances;
        [NonSerialized] public Vector4[]     filterKernelsBasic;
        // <<< Old SSS Model

        // --- Public Methods ---

        public SubsurfaceScatteringSettings()
        {
            numProfiles            = 1;
            profiles               = new SubsurfaceScatteringProfile[numProfiles];
            profiles[0]            = null;
            texturingModeFlags     = 0;
            transmissionFlags      = 0;
            thicknessRemaps        = null;
            worldScales            = null;
            shapeParams            = null;
            transmissionTints      = null;
            filterKernelsNearField = null;
            filterKernelsFarField  = null;
            // Old SSS Model >>>
            useDisneySSS             = true;
            halfRcpWeightedVariances = null;
            filterKernelsBasic       = null;
            // <<< Old SSS Model

            UpdateCache();
        }

        public void OnValidate()
        {
            // Reserve one slot for the neutral profile.
            numProfiles = Math.Min(profiles.Length, SssConstants.SSS_N_PROFILES - 1);

            if (profiles.Length != numProfiles)
            {
                Array.Resize(ref profiles, numProfiles);
            }

            for (int i = 0; i < numProfiles; i++)
            {
                if (profiles[i] != null)
                {
                    // Assign the profile IDs.
                    profiles[i].settingsIndex = i;
                }
            }

            for (int i = 0; i < numProfiles; i++)
            {
                // Skip unassigned profiles.
                if (profiles[i] == null) continue;

                profiles[i].thicknessRemap.y = Mathf.Max(profiles[i].thicknessRemap.y, 0);
                profiles[i].thicknessRemap.x = Mathf.Clamp(profiles[i].thicknessRemap.x, 0, profiles[i].thicknessRemap.y);
                profiles[i].worldScale       = Mathf.Max(profiles[i].worldScale, 0.001f);

                // Old SSS Model >>>
                Color c = new Color();

                c.r = Mathf.Max(0.05f, profiles[i].scatterDistance1.r);
                c.g = Mathf.Max(0.05f, profiles[i].scatterDistance1.g);
                c.b = Mathf.Max(0.05f, profiles[i].scatterDistance1.b);
                c.a = 0.0f;

                profiles[i].scatterDistance1 = c;

                c.r = Mathf.Max(0.05f, profiles[i].scatterDistance2.r);
                c.g = Mathf.Max(0.05f, profiles[i].scatterDistance2.g);
                c.b = Mathf.Max(0.05f, profiles[i].scatterDistance2.b);
                c.a = 0.0f;

                profiles[i].scatterDistance2 = c;
                // <<< Old SSS Model

                profiles[i].BuildKernel();
            }

            UpdateCache();
        }

        public void UpdateCache()
        {
            texturingModeFlags = transmissionFlags = 0;

            const int thicknessRemapsLen = SssConstants.SSS_N_PROFILES * 2;
            if (thicknessRemaps == null || thicknessRemaps.Length != thicknessRemapsLen)
            {
                thicknessRemaps = new float[thicknessRemapsLen];
            }

            if (worldScales == null || worldScales.Length != SssConstants.SSS_N_PROFILES)
            {
                worldScales = new float[SssConstants.SSS_N_PROFILES];
            }

            if (shapeParams == null || shapeParams.Length != SssConstants.SSS_N_PROFILES)
            {
                shapeParams = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            if (transmissionTints == null || transmissionTints.Length != SssConstants.SSS_N_PROFILES)
            {
                transmissionTints = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            const int filterKernelsNearFieldLen = 2 * SssConstants.SSS_N_PROFILES * SssConstants.SSS_N_SAMPLES_NEAR_FIELD;
            if (filterKernelsNearField == null || filterKernelsNearField.Length != filterKernelsNearFieldLen)
            {
                filterKernelsNearField = new float[filterKernelsNearFieldLen];
            }

            const int filterKernelsFarFieldLen = 2 * SssConstants.SSS_N_PROFILES * SssConstants.SSS_N_SAMPLES_FAR_FIELD;
            if (filterKernelsFarField == null || filterKernelsFarField.Length != filterKernelsFarFieldLen)
            {
                filterKernelsFarField = new float[filterKernelsFarFieldLen];
            }

            // Old SSS Model >>>
            if (halfRcpWeightedVariances == null || halfRcpWeightedVariances.Length != SssConstants.SSS_N_PROFILES)
            {
                halfRcpWeightedVariances = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            const int filterKernelsLen = SssConstants.SSS_N_PROFILES * SssConstants.SSS_BASIC_N_SAMPLES;
            if (filterKernelsBasic == null || filterKernelsBasic.Length != filterKernelsLen)
            {
                filterKernelsBasic = new Vector4[filterKernelsLen];
            }
            // <<< Old SSS Model

            for (int i = 0; i < numProfiles; i++)
            {
                // Skip unassigned profiles.
                if (profiles[i] == null) continue;

                Debug.Assert(numProfiles < 16, "Transmission flags (32-bit integer) cannot support more than 16 profiles.");

                texturingModeFlags |= (uint)profiles[i].texturingMode    << i;
                transmissionFlags  |= (uint)profiles[i].transmissionMode << i * 2;

                thicknessRemaps[2 * i]     = profiles[i].thicknessRemap.x;
                thicknessRemaps[2 * i + 1] = profiles[i].thicknessRemap.y - profiles[i].thicknessRemap.x;
                worldScales[i]             = profiles[i].worldScale;
                shapeParams[i]             = profiles[i].shapeParameter;
                shapeParams[i].w           = profiles[i].maxRadius;
                transmissionTints[i]       = profiles[i].transmissionTint;

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
                {
                    filterKernelsNearField[2 * (n * i + j) + 0] = profiles[i].filterKernelNearField[j].x;
                    filterKernelsNearField[2 * (n * i + j) + 1] = profiles[i].filterKernelNearField[j].y;
                }

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_FAR_FIELD; j < n; j++)
                {
                    filterKernelsFarField[2 * (n * i + j) + 0] = profiles[i].filterKernelFarField[j].x;
                    filterKernelsFarField[2 * (n * i + j) + 1] = profiles[i].filterKernelFarField[j].y;
                }

                // Old SSS Model >>>
                halfRcpWeightedVariances[i] = profiles[i].halfRcpWeightedVariances;

                for (int j = 0, n = SssConstants.SSS_BASIC_N_SAMPLES; j < n; j++)
                {
                    filterKernelsBasic[n * i + j] = profiles[i].filterKernelBasic[j];
                }
                // <<< Old SSS Model
            }

            // Fill the neutral profile.
            {
                int i = SssConstants.SSS_NEUTRAL_PROFILE_ID;

                worldScales[i] = 1.0f;
                shapeParams[i] = Vector4.zero;

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
                {
                    filterKernelsNearField[2 * (n * i + j) + 0] = 0.0f;
                    filterKernelsNearField[2 * (n * i + j) + 1] = 1.0f;
                }

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_FAR_FIELD; j < n; j++)
                {
                    filterKernelsFarField[2 * (n * i + j) + 0] = 0.0f;
                    filterKernelsFarField[2 * (n * i + j) + 1] = 1.0f;
                }

                // Old SSS Model >>>
                halfRcpWeightedVariances[i] = Vector4.one;

                for (int j = 0, n = SssConstants.SSS_BASIC_N_SAMPLES; j < n; j++)
                {
                    filterKernelsBasic[n * i + j]   = Vector4.one;
                    filterKernelsBasic[n * i + j].w = 0.0f;
                }
                // <<< Old SSS Model
            }
        }

        public void OnBeforeSerialize()
        {
            // No special action required.
        }

        public void OnAfterDeserialize()
        {
            UpdateCache();
        }
    }

#if UNITY_EDITOR
    public class SubsurfaceScatteringProfileFactory
    {
        [MenuItem("Assets/Create/HDRenderPipeline/Subsurface Scattering Profile", priority = 666)]
        static void MenuCreateSubsurfaceScatteringProfile()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                ScriptableObject.CreateInstance<DoCreateSubsurfaceScatteringProfile>(),
                "New SSS Profile.asset", icon, null);
        }

        public static SubsurfaceScatteringProfile CreateSssProfileAtPath(string path)
        {
            var profile  = ScriptableObject.CreateInstance<SubsurfaceScatteringProfile>();
            profile.name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }
    }

    class DoCreateSubsurfaceScatteringProfile : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profiles = SubsurfaceScatteringProfileFactory.CreateSssProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profiles);
        }
    }

    [CustomEditor(typeof(SubsurfaceScatteringProfile))]
    public class SubsurfaceScatteringProfileEditor : Editor
    {
        private class Styles
        {
            public readonly GUIContent   sssProfilePreview0           = new GUIContent("Profile Preview");
            public readonly GUIContent   sssProfilePreview1           = new GUIContent("Shows the fraction of light scattered from the source (center).");
            public readonly GUIContent   sssProfilePreview2           = new GUIContent("The distance to the boundary of the image corresponds to the Max Radius.");
            public readonly GUIContent   sssProfilePreview3           = new GUIContent("Note that the intensity of pixels around the center may be clipped.");
            public readonly GUIContent   sssTransmittancePreview0     = new GUIContent("Transmittance Preview");
            public readonly GUIContent   sssTransmittancePreview1     = new GUIContent("Shows the fraction of light passing through the object for thickness values from the remap.");
            public readonly GUIContent   sssTransmittancePreview2     = new GUIContent("Can be viewed as a cross section of a slab of material illuminated by white light from the left.");
            public readonly GUIContent   sssProfileScatteringDistance = new GUIContent("Scattering Distance", "Determines the shape of the profile, and the blur radius of the filter per color channel. Alpha is ignored.");
            public readonly GUIContent   sssProfileTransmissionTint   = new GUIContent("Transmission tint", "Color which tints transmitted light. Alpha is ignored.");
            public readonly GUIContent   sssProfileMaxRadius          = new GUIContent("Max Radius", "Effective radius of the filter (in millimeters). The blur is energy-preserving, so a wide filter results in a large area with small contributions of individual samples. Reducing the distance increases the sharpness of the result.");
            public readonly GUIContent   sssTexturingMode             = new GUIContent("Texturing Mode", "Specifies when the diffuse texture should be applied.");
            public readonly GUIContent[] sssTexturingModeOptions      = new GUIContent[2]
            {
                new GUIContent("Pre- and post-scatter", "Texturing is performed during both the lighting and the SSS passes. Slightly blurs the diffuse texture. Choose this mode if your diffuse texture contains little to no SSS lighting."),
                new GUIContent("Post-scatter",          "Texturing is performed only during the SSS pass. Effectively preserves the sharpness of the diffuse texture. Choose this mode if your diffuse texture already contains SSS lighting (e.g. a photo of skin).")
            };
            public readonly GUIContent   sssProfileTransmissionMode = new GUIContent("Transmission Mode", "Configures the simulation of light passing through thin objects. Depends on the thickness value (which is applied in the normal direction).");
            public readonly GUIContent[] sssTransmissionModeOptions = new GUIContent[3]
            {
                new GUIContent("None",         "Disables transmission. Choose this mode for completely opaque, or very thick translucent objects."),
                new GUIContent("Thin Object",  "Choose this mode for thin objects, such as paper or leaves. Transmitted light reuses the shadowing state of the surface."),
                new GUIContent("Regular",      "Choose this mode for moderately thick objects. For performance reasons, transmitted light ignores occlusion (shadows).")
            };
            public readonly GUIContent   sssProfileMinMaxThickness = new GUIContent("Min-Max Thickness", "Shows the values of the thickness remap below (in millimeters).");
            public readonly GUIContent   sssProfileThicknessRemap  = new GUIContent("Thickness Remap", "Remaps the thickness parameter from [0, 1] to the desired range (in millimeters).");
            public readonly GUIContent   sssProfileWorldScale      = new GUIContent("World Scale", "Size of the world unit in meters.");
            // Old SSS Model >>>
            public readonly GUIContent   sssProfileScatterDistance1 = new GUIContent("Scattering Distance #1", "The radius (in centimeters) of the 1st Gaussian filter, one per color channel. Alpha is ignored. The blur is energy-preserving, so a wide filter results in a large area with small contributions of individual samples. Smaller values increase the sharpness.");
            public readonly GUIContent   sssProfileScatterDistance2 = new GUIContent("Scattering Distance #2", "The radius (in centimeters) of the 2nd Gaussian filter, one per color channel. Alpha is ignored. The blur is energy-preserving, so a wide filter results in a large area with small contributions of individual samples. Smaller values increase the sharpness.");
            public readonly GUIContent   sssProfileLerpWeight       = new GUIContent("Filter Interpolation", "Controls linear interpolation between the two Gaussian filters.");
            // <<< Old SSS Model
            public readonly GUIStyle     centeredMiniBoldLabel     = new GUIStyle(GUI.skin.label);

            public Styles()
            {
                centeredMiniBoldLabel.alignment = TextAnchor.MiddleCenter;
                centeredMiniBoldLabel.fontSize  = 10;
                centeredMiniBoldLabel.fontStyle = FontStyle.Bold;
            }
        }

        private static Styles styles
        {
            get
            {
                if (s_Styles == null)
                {
                    s_Styles = new Styles();
                }
                return s_Styles;
            }
        }

        private static Styles      s_Styles = null;

        private RenderTexture      m_ProfileImage, m_TransmittanceImage;
        private Material           m_ProfileMaterial, m_TransmittanceMaterial;
        private SerializedProperty m_ScatteringDistance, m_MaxRadius, m_ShapeParam, m_TransmissionTint,
                                   m_TexturingMode, m_TransmissionMode, m_ThicknessRemap, m_WorldScale;
        // Old SSS Model >>>
        private SerializedProperty m_ScatterDistance1, m_ScatterDistance2, m_LerpWeight;
        // <<< Old SSS Model

        void OnEnable()
        {
            m_ScatteringDistance    = serializedObject.FindProperty("scatteringDistance");
            m_MaxRadius             = serializedObject.FindProperty("m_MaxRadius");
            m_ShapeParam            = serializedObject.FindProperty("m_ShapeParam");
            m_TransmissionTint      = serializedObject.FindProperty("transmissionTint");
            m_TexturingMode         = serializedObject.FindProperty("texturingMode");
            m_TransmissionMode      = serializedObject.FindProperty("transmissionMode");
            m_ThicknessRemap        = serializedObject.FindProperty("thicknessRemap");
            m_WorldScale            = serializedObject.FindProperty("worldScale");
            // Old SSS Model >>>
            m_ScatterDistance1      = serializedObject.FindProperty("scatterDistance1");
            m_ScatterDistance2      = serializedObject.FindProperty("scatterDistance2");
            m_LerpWeight            = serializedObject.FindProperty("lerpWeight");
            // <<< Old SSS Model

            // These shaders don't need to be reference by RenderPipelineResource as they are not use at runtime
            m_ProfileMaterial       = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawSssProfile");
            m_TransmittanceMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawTransmittanceGraph");

            m_ProfileImage          = new RenderTexture(256, 256, 0, RenderTextureFormat.DefaultHDR);
            m_TransmittanceImage    = new RenderTexture( 16, 256, 0, RenderTextureFormat.DefaultHDR);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Old SSS Model >>>
            bool useDisneySSS;
            {
                HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                useDisneySSS = hdPipeline.sssSettings.useDisneySSS;
            }
            // <<< Old SSS Model

            EditorGUI.BeginChangeCheck();
            {
                if (useDisneySSS)
                {
                    EditorGUILayout.PropertyField(m_ScatteringDistance, styles.sssProfileScatteringDistance);
                
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(m_MaxRadius, styles.sssProfileMaxRadius);
                    GUI.enabled = true;
                }
                else
                {
                    EditorGUILayout.PropertyField(m_ScatterDistance1, styles.sssProfileScatterDistance1);
                    EditorGUILayout.PropertyField(m_ScatterDistance2, styles.sssProfileScatterDistance2);
                    EditorGUILayout.PropertyField(m_LerpWeight,       styles.sssProfileLerpWeight);
                }

                m_TexturingMode.intValue    = EditorGUILayout.Popup(styles.sssTexturingMode,           m_TexturingMode.intValue,    styles.sssTexturingModeOptions);
                m_TransmissionMode.intValue = EditorGUILayout.Popup(styles.sssProfileTransmissionMode, m_TransmissionMode.intValue, styles.sssTransmissionModeOptions);

                EditorGUILayout.PropertyField(m_TransmissionTint,   styles.sssProfileTransmissionTint);
                EditorGUILayout.PropertyField(m_ThicknessRemap, styles.sssProfileMinMaxThickness);
                Vector2 thicknessRemap = m_ThicknessRemap.vector2Value;
                EditorGUILayout.MinMaxSlider(styles.sssProfileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0.0f, 50.0f);
                m_ThicknessRemap.vector2Value = thicknessRemap;
                EditorGUILayout.PropertyField(m_WorldScale, styles.sssProfileWorldScale);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(styles.sssProfilePreview0, styles.centeredMiniBoldLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview1, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview2, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview3, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();
            }

            float   r = m_MaxRadius.floatValue;
            Vector3 S = m_ShapeParam.vector3Value;
            Vector4 T = m_TransmissionTint.colorValue;
            Vector2 R = m_ThicknessRemap.vector2Value;
            bool transmissionEnabled = m_TransmissionMode.intValue != (int)SubsurfaceScatteringProfile.TransmissionMode.None;

            // Draw the profile.
            m_ProfileMaterial.SetFloat( "_MaxRadius",  r);
            m_ProfileMaterial.SetVector("_ShapeParam", S);
            // Old SSS Model >>>
            Utilities.SelectKeyword(m_ProfileMaterial, "SSS_MODEL_DISNEY", "SSS_MODEL_BASIC", useDisneySSS);
            // Apply the three-sigma rule, and rescale.
            float   s       = (1.0f / 3.0f) * SssConstants.SSS_BASIC_DISTANCE_SCALE;
            float   rMax    = Mathf.Max(m_ScatterDistance1.colorValue.r, m_ScatterDistance1.colorValue.g, m_ScatterDistance1.colorValue.b,
                                        m_ScatterDistance2.colorValue.r, m_ScatterDistance2.colorValue.g, m_ScatterDistance2.colorValue.b);
            Vector4 stdDev1 = new Vector4(s * m_ScatterDistance1.colorValue.r, s * m_ScatterDistance1.colorValue.g, s * m_ScatterDistance1.colorValue.b);
            Vector4 stdDev2 = new Vector4(s * m_ScatterDistance2.colorValue.r, s * m_ScatterDistance2.colorValue.g, s * m_ScatterDistance2.colorValue.b);
            m_ProfileMaterial.SetVector("_StdDev1",   stdDev1);
            m_ProfileMaterial.SetVector("_StdDev2",   stdDev2);
            m_ProfileMaterial.SetFloat("_LerpWeight", m_LerpWeight.floatValue);
            m_ProfileMaterial.SetFloat("_MaxRadius",  rMax);
            // <<< Old SSS Model
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256, 256), m_ProfileImage, m_ProfileMaterial, ScaleMode.ScaleToFit, 1.0f);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(styles.sssTransmittancePreview0, styles.centeredMiniBoldLabel);
            EditorGUILayout.LabelField(styles.sssTransmittancePreview1, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(styles.sssTransmittancePreview2, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            // Draw the transmittance graph.
            m_TransmittanceMaterial.SetVector("_ShapeParam",       S);
            m_TransmittanceMaterial.SetVector("_TransmissionTint", transmissionEnabled ? T : Vector4.zero);
            m_TransmittanceMaterial.SetVector("_ThicknessRemap",   R);
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16, 16), m_TransmittanceImage, m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16.0f);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                // Validate each individual asset and update caches.
                hdPipeline.sssSettings.OnValidate();
            }
        }
    }
#endif
}
