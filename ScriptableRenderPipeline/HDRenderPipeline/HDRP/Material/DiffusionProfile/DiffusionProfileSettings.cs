using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public class DiffusionProfileConstants
    {
        public const int DIFFUSION_PROFILE_COUNT      = 16; // Max. number of profiles, including the slot taken by the neutral profile
        public const int DIFFUSION_PROFILE_NEUTRAL_ID = 0;  // Does not result in blurring
        public const int SSS_N_SAMPLES_NEAR_FIELD     = 55; // Used for extreme close ups; must be a Fibonacci number
        public const int SSS_N_SAMPLES_FAR_FIELD      = 21; // Used at a regular distance; must be a Fibonacci number
        public const int SSS_LOD_THRESHOLD            = 4;  // The LoD threshold of the near-field kernel (in pixels)
        // Old SSS Model >>>
        public const int SSS_BASIC_N_SAMPLES          = 11; // Must be an odd number
        public const int SSS_BASIC_DISTANCE_SCALE     = 3;  // SSS distance units per centimeter
        // <<< Old SSS Model
    }

    [Serializable]
    public sealed class DiffusionProfile
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

        public string name;

        [ColorUsage(false, true)]
        public Color            scatteringDistance;         // Per color channel (no meaningful units)
        [ColorUsage(false, true)]
        public Color            transmissionTint;           // HDR color
        public TexturingMode    texturingMode;
        public TransmissionMode transmissionMode;
        public Vector2          thicknessRemap;             // X = min, Y = max (in millimeters)
        public float            worldScale;                 // Size of the world unit in meters
        public float            ior;                        // 1.4 for skin (mean ~0.028)

        // Old SSS Model >>>
        [ColorUsage(false, true)]
        public Color            scatterDistance1;
        [ColorUsage(false, true)]
        public Color            scatterDistance2;
        [Range(0f, 1f)]
        public float            lerpWeight;
        // <<< Old SSS Model

        public Vector3          shapeParam { get; private set; }               // RGB = shape parameter: S = 1 / D
        public float            maxRadius { get; private set; }                // In millimeters
        public Vector2[]        filterKernelNearField { get; private set; }    // X = radius, Y = reciprocal of the PDF
        public Vector2[]        filterKernelFarField { get; private set; }     // X = radius, Y = reciprocal of the PDF
        public Vector4          halfRcpWeightedVariances { get; private set; }
        public Vector4[]        filterKernelBasic { get; private set; }

        public DiffusionProfile(string name)
        {
            this.name          = name;

            scatteringDistance = Color.grey;
            transmissionTint   = Color.white;
            texturingMode      = TexturingMode.PreAndPostScatter;
            transmissionMode   = TransmissionMode.ThinObject;
            thicknessRemap     = new Vector2(0f, 5f);
            worldScale         = 1f;
            ior                = 1.4f; // TYpical value for skin specular reflectance

            // Old SSS Model >>>
            scatterDistance1   = new Color(0.3f, 0.3f, 0.3f, 0f);
            scatterDistance2   = new Color(0.5f, 0.5f, 0.5f, 0f);
            lerpWeight         = 1f;
            // <<< Old SSS Model
        }

        public void Validate()
        {
            thicknessRemap.y = Mathf.Max(thicknessRemap.y, 0f);
            thicknessRemap.x = Mathf.Clamp(thicknessRemap.x, 0f, thicknessRemap.y);
            worldScale       = Mathf.Max(worldScale, 0.001f);
            ior              = Mathf.Clamp(ior, 1.0f, 2.0f);

            // Old SSS Model >>>
            scatterDistance1 = new Color
            {
                r = Mathf.Max(0.05f, scatterDistance1.r),
                g = Mathf.Max(0.05f, scatterDistance1.g),
                b = Mathf.Max(0.05f, scatterDistance1.b),
                a = 0.0f
            };

            scatterDistance2 = new Color
            {
                r = Mathf.Max(0.05f, scatterDistance2.r),
                g = Mathf.Max(0.05f, scatterDistance2.g),
                b = Mathf.Max(0.05f, scatterDistance2.b),
                a = 0f
            };
            // <<< Old SSS Model

            UpdateKernel();
        }

        // Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar.
        public void UpdateKernel()
        {
            if (filterKernelNearField == null || filterKernelNearField.Length != DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD)
                filterKernelNearField = new Vector2[DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD];

            if (filterKernelFarField == null || filterKernelFarField.Length != DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD)
                filterKernelFarField = new Vector2[DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD];

            // Clamp to avoid artifacts.
            shapeParam = new Vector3(
                1f / Mathf.Max(0.001f, scatteringDistance.r),
                1f / Mathf.Max(0.001f, scatteringDistance.g),
                1f / Mathf.Max(0.001f, scatteringDistance.b)
            );

            // We importance sample the color channel with the widest scattering distance.
            float s = Mathf.Min(shapeParam.x, shapeParam.y, shapeParam.z);

            // Importance sample the normalized diffuse reflectance profile for the computed value of 's'.
            // ------------------------------------------------------------------------------------
            // R[r, phi, s]   = s * (Exp[-r * s] + Exp[-r * s / 3]) / (8 * Pi * r)
            // PDF[r, phi, s] = r * R[r, phi, s]
            // CDF[r, s]      = 1 - 1/4 * Exp[-r * s] - 3/4 * Exp[-r * s / 3]
            // ------------------------------------------------------------------------------------

            // Importance sample the near field kernel.
            for (int i = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD; i < n; i++)
            {
                float p = (i + 0.5f) * (1.0f / n);
                float r = DisneyProfileCdfInverse(p, s);

                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                filterKernelNearField[i].x = r;
                filterKernelNearField[i].y = 1f / DisneyProfilePdf(r, s);
            }

            // Importance sample the far field kernel.
            for (int i = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD; i < n; i++)
            {
                float p = (i + 0.5f) * (1.0f / n);
                float r = DisneyProfileCdfInverse(p, s);

                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                filterKernelFarField[i].x = r;
                filterKernelFarField[i].y = 1f / DisneyProfilePdf(r, s);
            }

            maxRadius = filterKernelFarField[DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD - 1].x;

            // Old SSS Model >>>
            UpdateKernelAndVarianceData();
            // <<< Old SSS Model
        }

        // Old SSS Model >>>
        public void UpdateKernelAndVarianceData()
        {
            const int kNumSamples    = DiffusionProfileConstants.SSS_BASIC_N_SAMPLES;
            const int kDistanceScale = DiffusionProfileConstants.SSS_BASIC_DISTANCE_SCALE;

            if (filterKernelBasic == null || filterKernelBasic.Length != kNumSamples)
                filterKernelBasic = new Vector4[kNumSamples];

            // Apply the three-sigma rule, and rescale.
            var stdDev1 = ((1f / 3f) * kDistanceScale) * scatterDistance1;
            var stdDev2 = ((1f / 3f) * kDistanceScale) * scatterDistance2;

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

            var weightSum = Vector3.zero;

            float step = 1f / (kNumSamples - 1);

            // Importance sample the linear combination of two Gaussians.
            for (int i = 0; i < kNumSamples; i++)
            {
                // Generate 'u' on (0, 0.5] and (0.5, 1).
                float u = (i <= kNumSamples / 2) ? 0.5f - i * step // The center and to the left
                                                 : i * step;       // From the center to the right

                u = Mathf.Clamp(u, 0.001f, 0.999f);

                float pos = GaussianCombinationCdfInverse(u, maxStdDev1, maxStdDev2, lerpWeight);
                float pdf = GaussianCombination(pos, maxStdDev1, maxStdDev2, lerpWeight);

                Vector3 val;
                val.x = GaussianCombination(pos, stdDev1.r, stdDev2.r, lerpWeight);
                val.y = GaussianCombination(pos, stdDev1.g, stdDev2.g, lerpWeight);
                val.z = GaussianCombination(pos, stdDev1.b, stdDev2.b, lerpWeight);

                // We do not divide by 'numSamples' since we will renormalize, anyway.
                filterKernelBasic[i].x = val.x * (1 / pdf);
                filterKernelBasic[i].y = val.y * (1 / pdf);
                filterKernelBasic[i].z = val.z * (1 / pdf);
                filterKernelBasic[i].w = pos;

                weightSum.x += filterKernelBasic[i].x;
                weightSum.y += filterKernelBasic[i].y;
                weightSum.z += filterKernelBasic[i].z;
            }

            // Renormalize the weights to conserve energy.
            for (int i = 0; i < kNumSamples; i++)
            {
                filterKernelBasic[i].x *= 1 / weightSum.x;
                filterKernelBasic[i].y *= 1 / weightSum.y;
                filterKernelBasic[i].z *= 1 / weightSum.z;
            }

            Vector4 weightedStdDev;
            weightedStdDev.x = Mathf.Lerp(stdDev1.r,  stdDev2.r,  lerpWeight);
            weightedStdDev.y = Mathf.Lerp(stdDev1.g,  stdDev2.g,  lerpWeight);
            weightedStdDev.z = Mathf.Lerp(stdDev1.b,  stdDev2.b,  lerpWeight);
            weightedStdDev.w = Mathf.Lerp(maxStdDev1, maxStdDev2, lerpWeight);

            // Store (1 / (2 * WeightedVariance)) per color channel.
            // Warning: do not use halfRcpWeightedVariances.Set(). It will not work.
            halfRcpWeightedVariances = new Vector4(0.5f / (weightedStdDev.x * weightedStdDev.x),
                                                   0.5f / (weightedStdDev.y * weightedStdDev.y),
                                                   0.5f / (weightedStdDev.z * weightedStdDev.z),
                                                   0.5f / (weightedStdDev.w * weightedStdDev.w));
        }
        // <<< Old SSS Model

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
            float[] c = { 2.515517f, 0.802853f, 0.010328f };
            float[] d = { 1.432788f, 0.189269f, 0.001308f };
            return t - ((c[2] * t + c[1]) * t + c[0]) / (((d[2] * t + d[1]) * t + d[0]) * t + 1.0f);
        }

        // Ref: https://www.johndcook.com/blog/csharp_phi_inverse/
        static float NormalCdfInverse(float p, float stdDev)
        {
            float x;

            if (p < 0.5)
            {
                // F^-1(p) = - G^-1(p)
                x = -RationalApproximation(Mathf.Sqrt(-2f * Mathf.Log(p)));
            }
            else
            {
                // F^-1(p) = G^-1(1-p)
                x = RationalApproximation(Mathf.Sqrt(-2f * Mathf.Log(1f - p)));
            }

            return x * stdDev;
        }

        static float GaussianCombinationCdfInverse(float p, float stdDev1, float stdDev2, float lerpWeight)
        {
            return Mathf.Lerp(NormalCdfInverse(p, stdDev1), NormalCdfInverse(p, stdDev2), lerpWeight);
        }
        // <<< Old SSS Model
    }

    public sealed class DiffusionProfileSettings : ScriptableObject
    {
        public DiffusionProfile[] profiles;

        [NonSerialized] public uint      texturingModeFlags;        // 1 bit/profile: 0 = PreAndPostScatter, 1 = PostScatter
        [NonSerialized] public uint      transmissionFlags;         // 1 bit/profile: 0 = regular, 1 = thin
        [NonSerialized] public Vector4[] thicknessRemaps;           // Remap: 0 = start, 1 = end - start
        [NonSerialized] public Vector4[] worldScales;               // X = meters per world unit; Y = world units per meter
        [NonSerialized] public Vector4[] shapeParams;               // RGB = S = 1 / D, A = filter radius
        [NonSerialized] public Vector4[] transmissionTintsAndFresnel0; // RGB = color, A = fresnel0
        [NonSerialized] public Vector4[] disabledTransmissionTintsAndFresnel0; // RGB = black, A = fresnel0 - For debug to remove the transmission
        [NonSerialized] public Vector4[] filterKernels;             // XY = near field, ZW = far field; 0 = radius, 1 = reciprocal of the PDF

        // Old SSS Model >>>
        [NonSerialized] public Vector4[] halfRcpWeightedVariances;
        [NonSerialized] public Vector4[] halfRcpVariancesAndWeights;
        [NonSerialized] public Vector4[] filterKernelsBasic;
        // <<< Old SSS Model

        public DiffusionProfile this[int index]
        {
            get
            {
                if (index >= DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1)
                    throw new IndexOutOfRangeException("index");

                return profiles[index];
            }
        }

        void OnEnable()
        {
            // The neutral profile is not a part of the array.
            int profileArraySize = DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1;

            if (profiles != null && profiles.Length != profileArraySize)
                Array.Resize(ref profiles, profileArraySize);

            if (profiles == null)
                profiles = new DiffusionProfile[profileArraySize];

            for (int i = 0; i < profileArraySize; i++)
            {
                if (profiles[i] == null)
                    profiles[i] = new DiffusionProfile("Profile " + (i + 1));

                profiles[i].Validate();
            }

            ValidateArray(ref thicknessRemaps,   DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref worldScales,       DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref shapeParams,       DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref transmissionTintsAndFresnel0, DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref disabledTransmissionTintsAndFresnel0, DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref filterKernels,     DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD);

            // Old SSS Model >>>
            ValidateArray(ref halfRcpWeightedVariances,   DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref halfRcpVariancesAndWeights, DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 2);
            ValidateArray(ref filterKernelsBasic,         DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * DiffusionProfileConstants.SSS_BASIC_N_SAMPLES);

            Debug.Assert(DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID <= 32, "Transmission and Texture flags (32-bit integer) cannot support more than 32 profiles.");

            UpdateCache();
        }

        static void ValidateArray<T>(ref T[] array, int len)
        {
            if (array == null || array.Length != len)
                array = new T[len];
        }

        public void UpdateCache()
        {
            for (int i = 0; i < DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1; i++)
            {
                UpdateCache(i);
            }

            // Fill the neutral profile.
            int neutralId = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

            worldScales[neutralId] = Vector4.one;
            shapeParams[neutralId] = Vector4.zero;

            for (int j = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
            {
                filterKernels[n * neutralId + j].x = 0f;
                filterKernels[n * neutralId + j].y = 1f;
                filterKernels[n * neutralId + j].z = 0f;
                filterKernels[n * neutralId + j].w = 1f;
            }

            // Old SSS Model >>>
            halfRcpWeightedVariances[neutralId] = Vector4.one;

            for (int j = 0, n = DiffusionProfileConstants.SSS_BASIC_N_SAMPLES; j < n; j++)
            {
                filterKernelsBasic[n * neutralId + j]   = Vector4.one;
                filterKernelsBasic[n * neutralId + j].w = 0f;
            }
            // <<< Old SSS Model
        }

        public void UpdateCache(int p)
        {
            // 'p' is the profile array index. 'i' is the index in the shader (accounting for the neutral profile).
            int i = p + 1;

            // Erase previous value (This need to be done here individually as in the SSS editor we edit individual component)
            uint mask = 1u << i;
            texturingModeFlags &= ~mask;
            mask = 1u << i;
            transmissionFlags &= ~mask;

            texturingModeFlags |= (uint)profiles[p].texturingMode    << i;
            transmissionFlags  |= (uint)profiles[p].transmissionMode << i;

            thicknessRemaps[i]   = new Vector4(profiles[p].thicknessRemap.x, profiles[p].thicknessRemap.y - profiles[p].thicknessRemap.x, 0f, 0f);
            worldScales[i]       = new Vector4(profiles[p].worldScale, 1.0f / profiles[p].worldScale, 0f, 0f);
            shapeParams[i]       = profiles[p].shapeParam;
            shapeParams[i].w     = profiles[p].maxRadius;
            // Convert ior to fresnel0
            float fresnel0 = (profiles[p].ior - 1.0f) / (profiles[p].ior + 1.0f);
            fresnel0 *= fresnel0; // square
            transmissionTintsAndFresnel0[i] = new Vector4(profiles[p].transmissionTint.r * 0.25f, profiles[p].transmissionTint.g * 0.25f, profiles[p].transmissionTint.b * 0.25f, fresnel0); // Premultiplied
            disabledTransmissionTintsAndFresnel0[i] = new Vector4(0.0f, 0.0f, 0.0f, fresnel0);

            for (int j = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
            {
                filterKernels[n * i + j].x = profiles[p].filterKernelNearField[j].x;
                filterKernels[n * i + j].y = profiles[p].filterKernelNearField[j].y;

                if (j < DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD)
                {
                    filterKernels[n * i + j].z = profiles[p].filterKernelFarField[j].x;
                    filterKernels[n * i + j].w = profiles[p].filterKernelFarField[j].y;
                }
            }

            // Old SSS Model >>>
            halfRcpWeightedVariances[i] = profiles[p].halfRcpWeightedVariances;

            var stdDev1 = ((1f / 3f) * DiffusionProfileConstants.SSS_BASIC_DISTANCE_SCALE) * (Vector4)profiles[p].scatterDistance1;
            var stdDev2 = ((1f / 3f) * DiffusionProfileConstants.SSS_BASIC_DISTANCE_SCALE) * (Vector4)profiles[p].scatterDistance2;

            // Multiply by 0.1 to convert from millimeters to centimeters. Apply the distance scale.
            // Rescale by 4 to counter rescaling of transmission tints.
            float a = 0.1f * DiffusionProfileConstants.SSS_BASIC_DISTANCE_SCALE;
            halfRcpVariancesAndWeights[2 * i + 0] = new Vector4(a * a * 0.5f / (stdDev1.x * stdDev1.x), a * a * 0.5f / (stdDev1.y * stdDev1.y), a * a * 0.5f / (stdDev1.z * stdDev1.z), 4f * (1f - profiles[p].lerpWeight));
            halfRcpVariancesAndWeights[2 * i + 1] = new Vector4(a * a * 0.5f / (stdDev2.x * stdDev2.x), a * a * 0.5f / (stdDev2.y * stdDev2.y), a * a * 0.5f / (stdDev2.z * stdDev2.z), 4f * profiles[p].lerpWeight);

            for (int j = 0, n = DiffusionProfileConstants.SSS_BASIC_N_SAMPLES; j < n; j++)
            {
                filterKernelsBasic[n * i + j] = profiles[p].filterKernelBasic[j];
            }
            // <<< Old SSS Model
        }
    }
}
