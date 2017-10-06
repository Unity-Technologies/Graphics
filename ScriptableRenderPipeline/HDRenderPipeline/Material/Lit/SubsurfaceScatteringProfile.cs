using System;

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

        public void Validate()
        {
            thicknessRemap.y = Mathf.Max(thicknessRemap.y, 0f);
            thicknessRemap.x = Mathf.Clamp(thicknessRemap.x, 0f, thicknessRemap.y);
            worldScale       = Mathf.Max(worldScale, 0.001f);

            // Old SSS Model >>>
            var c = new Color();
            c.r = Mathf.Max(0.05f, scatterDistance1.r);
            c.g = Mathf.Max(0.05f, scatterDistance1.g);
            c.b = Mathf.Max(0.05f, scatterDistance1.b);
            c.a = 0.0f;

            scatterDistance1 = c;

            c.r = Mathf.Max(0.05f, scatterDistance2.r);
            c.g = Mathf.Max(0.05f, scatterDistance2.g);
            c.b = Mathf.Max(0.05f, scatterDistance2.b);
            c.a = 0.0f;

            scatterDistance2 = c;
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
            m_ShapeParam.x = 1.0f / Mathf.Max(0.001f, scatteringDistance.r);
            m_ShapeParam.y = 1.0f / Mathf.Max(0.001f, scatteringDistance.g);
            m_ShapeParam.z = 1.0f / Mathf.Max(0.001f, scatteringDistance.b);

            // We importance sample the color channel with the widest scattering distance.
            float s = Mathf.Min(m_ShapeParam.x, m_ShapeParam.y, m_ShapeParam.z);

            // Importance sample the normalized diffusion profile for the computed value of 's'.
            // ------------------------------------------------------------------------------------
            // R(r, s)   = s * (Exp[-r * s] + Exp[-r * s / 3]) / (8 * Pi * r)
            // PDF(r, s) = s * (Exp[-r * s] + Exp[-r * s / 3]) / 4
            // CDF(r, s) = 1 - 1/4 * Exp[-r * s] - 3/4 * Exp[-r * s / 3]
            // ------------------------------------------------------------------------------------

            // Importance sample the near field kernel.
            for (int i = 0, n = SssConstants.SSS_N_SAMPLES_NEAR_FIELD; i < n; i++)
            {
                float p = (i + 0.5f) * (1.0f / n);
                float r = KernelCdfInverse(p, s);

                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                m_FilterKernelNearField[i].x = r;
                m_FilterKernelNearField[i].y = 1.0f / KernelPdf(r, s);
            }

            // Importance sample the far field kernel.
            for (int i = 0, n = SssConstants.SSS_N_SAMPLES_FAR_FIELD; i < n; i++)
            {
                float p = (i + 0.5f) * (1.0f / n);
                float r = KernelCdfInverse(p, s);

                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                m_FilterKernelFarField[i].x = r;
                m_FilterKernelFarField[i].y = 1.0f / KernelPdf(r, s);
            }

            m_MaxRadius = m_FilterKernelFarField[SssConstants.SSS_N_SAMPLES_FAR_FIELD - 1].x;

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
        // Below are the cached values. TODO: uncomment when SSS profile asset serialization is fixed.
        /*[NonSerialized]*/ public int       texturingModeFlags;        // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
        /*[NonSerialized]*/ public int       transmissionFlags;         // 2 bit/profile; 0 = inf. thick, 1 = thin, 2 = regular
        /*[NonSerialized]*/ public Vector4[] thicknessRemaps;           // Remap: 0 = start, 1 = end - start
        /*[NonSerialized]*/ public Vector4[] worldScales;               // Size of the world unit in meters (only the X component is used)
        /*[NonSerialized]*/ public Vector4[] shapeParams;               // RGB = S = 1 / D, A = filter radius
        /*[NonSerialized]*/ public Vector4[] transmissionTints;         // RGB = color, A = unused
        /*[NonSerialized]*/ public Vector4[] filterKernels;             // XY = near field, ZW = far field; 0 = radius, 1 = reciprocal of the PDF
        // Old SSS Model >>>
        public bool                          useDisneySSS;
        /*[NonSerialized]*/ public Vector4[] halfRcpWeightedVariances;
        /*[NonSerialized]*/ public Vector4[] halfRcpVariancesAndWeights;
        /*[NonSerialized]*/ public Vector4[] filterKernelsBasic;
        // <<< Old SSS Model

        // --- Public Methods ---

        public SubsurfaceScatteringSettings()
        {
            numProfiles        = 1;
            profiles           = new SubsurfaceScatteringProfile[numProfiles];
            profiles[0]        = null;
            texturingModeFlags = 0;
            transmissionFlags  = 0;
            thicknessRemaps    = null;
            worldScales        = null;
            shapeParams        = null;
            transmissionTints  = null;
            filterKernels      = null;
            // Old SSS Model >>>
            useDisneySSS               = true;
            halfRcpWeightedVariances   = null;
            halfRcpVariancesAndWeights = null;
            filterKernelsBasic         = null;
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

            foreach (var profile in profiles)
            {
                if (profile != null)
                    profile.Validate();
            }

            UpdateCache();
        }

        public void UpdateCache()
        {
            texturingModeFlags = transmissionFlags = 0;

            if (thicknessRemaps == null || thicknessRemaps.Length != SssConstants.SSS_N_PROFILES)
            {
                thicknessRemaps = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            if (worldScales == null || worldScales.Length != SssConstants.SSS_N_PROFILES)
            {
                worldScales = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            if (shapeParams == null || shapeParams.Length != SssConstants.SSS_N_PROFILES)
            {
                shapeParams = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            if (transmissionTints == null || transmissionTints.Length != SssConstants.SSS_N_PROFILES)
            {
                transmissionTints = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            const int filterKernelsNearFieldLen = SssConstants.SSS_N_PROFILES * SssConstants.SSS_N_SAMPLES_NEAR_FIELD;
            if (filterKernels == null || filterKernels.Length != filterKernelsNearFieldLen)
            {
                filterKernels = new Vector4[filterKernelsNearFieldLen];
            }

            // Old SSS Model >>>
            if (halfRcpWeightedVariances == null || halfRcpWeightedVariances.Length != SssConstants.SSS_N_PROFILES)
            {
                halfRcpWeightedVariances = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            if (halfRcpVariancesAndWeights == null || halfRcpVariancesAndWeights.Length != 2 * SssConstants.SSS_N_PROFILES)
            {
                halfRcpVariancesAndWeights = new Vector4[2 * SssConstants.SSS_N_PROFILES];
            }

            const int filterKernelsLen = SssConstants.SSS_N_PROFILES * SssConstants.SSS_BASIC_N_SAMPLES;
            if (filterKernelsBasic == null || filterKernelsBasic.Length != filterKernelsLen)
            {
                filterKernelsBasic = new Vector4[filterKernelsLen];
            }
            // <<< Old SSS Model

            for (int i = 0; i < SssConstants.SSS_N_PROFILES - 1; i++)
            {
                // If a profile is null, it means that it was never set in the HDRenderPipeline Asset or that the profile asset has been deleted.
                // In this case we want the users to be warned if a material uses one of those. This is why we fill the profile with pink transmission values.
                if (i >= numProfiles || profiles[i] == null)
                {
                    // Pink transmission
                    transmissionFlags |= 1 << i * 2;
                    transmissionTints[i] = new Vector4(100.0f, 0.0f, 100.0f, 1.0f);

                    // Default neutral values for the rest
                    worldScales[i] = Vector4.one;
                    shapeParams[i] = Vector4.zero;

                    for (int j = 0, n = SssConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
                    {
                        filterKernels[n * i + j].x = 0.0f;
                        filterKernels[n * i + j].y = 1.0f;
                        filterKernels[n * i + j].z = 0.0f;
                        filterKernels[n * i + j].w = 1.0f;
                    }

                    // Old SSS Model >>>
                    halfRcpWeightedVariances[i]           = Vector4.one;
                    halfRcpVariancesAndWeights[2 * i + 0] = Vector4.one;
                    halfRcpVariancesAndWeights[2 * i + 1] = Vector4.one;

                    for (int j = 0, n = SssConstants.SSS_BASIC_N_SAMPLES; j < n; j++)
                    {
                        filterKernelsBasic[n * i + j] = Vector4.one;
                        filterKernelsBasic[n * i + j].w = 0.0f;
                    }

                    continue;
                }

                Debug.Assert(numProfiles < 16, "Transmission flags (32-bit integer) cannot support more than 16 profiles.");

                texturingModeFlags |= (int)profiles[i].texturingMode    << i;
                transmissionFlags  |= (int)profiles[i].transmissionMode << i * 2;

                thicknessRemaps[i]   = new Vector4(profiles[i].thicknessRemap.x, profiles[i].thicknessRemap.y - profiles[i].thicknessRemap.x, 0.0f, 0.0f);
                worldScales[i]       = new Vector4(profiles[i].worldScale, 0, 0, 0);
                shapeParams[i]       = profiles[i].shapeParameter;
                shapeParams[i].w     = profiles[i].maxRadius;
                transmissionTints[i] = profiles[i].transmissionTint * 0.25f; // Premultiplied

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
                {
                    filterKernels[n * i + j].x = profiles[i].filterKernelNearField[j].x;
                    filterKernels[n * i + j].y = profiles[i].filterKernelNearField[j].y;

                    if (j < SssConstants.SSS_N_SAMPLES_FAR_FIELD)
                    {
                        filterKernels[n * i + j].z = profiles[i].filterKernelFarField[j].x;
                        filterKernels[n * i + j].w = profiles[i].filterKernelFarField[j].y;
                    }
                }

                // Old SSS Model >>>
                halfRcpWeightedVariances[i] = profiles[i].halfRcpWeightedVariances;

                Vector4 stdDev1 = ((1.0f / 3.0f) * SssConstants.SSS_BASIC_DISTANCE_SCALE) * profiles[i].scatterDistance1;
                Vector4 stdDev2 = ((1.0f / 3.0f) * SssConstants.SSS_BASIC_DISTANCE_SCALE) * profiles[i].scatterDistance2;

                // Multiply by 0.1 to convert from millimeters to centimeters. Apply the distance scale.
                // Rescale by 4 to counter rescaling of transmission tints.
                float a = 0.1f * SssConstants.SSS_BASIC_DISTANCE_SCALE;
                halfRcpVariancesAndWeights[2 * i + 0] = new Vector4(a * a * 0.5f / (stdDev1.x * stdDev1.x), a * a * 0.5f / (stdDev1.y * stdDev1.y), a * a * 0.5f / (stdDev1.z * stdDev1.z), 4 * (1.0f - profiles[i].lerpWeight));
                halfRcpVariancesAndWeights[2 * i + 1] = new Vector4(a * a * 0.5f / (stdDev2.x * stdDev2.x), a * a * 0.5f / (stdDev2.y * stdDev2.y), a * a * 0.5f / (stdDev2.z * stdDev2.z), 4 * profiles[i].lerpWeight);

                for (int j = 0, n = SssConstants.SSS_BASIC_N_SAMPLES; j < n; j++)
                {
                    filterKernelsBasic[n * i + j] = profiles[i].filterKernelBasic[j];
                }
                // <<< Old SSS Model
            }

            // Fill the neutral profile.
            {
                int i = SssConstants.SSS_NEUTRAL_PROFILE_ID;

                worldScales[i] = Vector4.one;
                shapeParams[i] = Vector4.zero;

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
                {
                    filterKernels[n * i + j].x = 0.0f;
                    filterKernels[n * i + j].y = 1.0f;
                    filterKernels[n * i + j].z = 0.0f;
                    filterKernels[n * i + j].w = 1.0f;
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
            // TODO: uncomment when SSS profile asset serialization is fixed.
            // UpdateCache();
        }
    }
}
