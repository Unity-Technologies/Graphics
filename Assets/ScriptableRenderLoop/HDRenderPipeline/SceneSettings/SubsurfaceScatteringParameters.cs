namespace UnityEngine.Experimental.Rendering
{
    [System.Serializable]
    public class SubsurfaceScatteringProfile
    {
        public const int numSamples = 7;

        Color     m_filter1Variance;
        Color     m_filter2Variance;
        float     m_filterLerpWeight;
        Vector4[] m_filterKernel;
        bool      m_kernelNeedsUpdate;

        // --- Methods ---

        public Color filter1Variance
        {
            get { return m_filter1Variance; }
            set { if (m_filter1Variance != value) { m_filter1Variance = value; m_kernelNeedsUpdate = true; } }
        }

        public Color filter2Variance
        {
            get { return m_filter2Variance; }
            set { if (m_filter2Variance != value) { m_filter2Variance = value; m_kernelNeedsUpdate = true; } }
        }

        public float filterLerpWeight
        {
            get { return m_filterLerpWeight; }
            set { if (m_filterLerpWeight != value) { m_filterLerpWeight = value; m_kernelNeedsUpdate = true; } }
        }

        public Vector4[] filterKernel
        {
            get { if (m_kernelNeedsUpdate) ComputeKernel(); return m_filterKernel; }
        }

        public static SubsurfaceScatteringProfile Default
        {
            get
            {
                SubsurfaceScatteringProfile profile = new SubsurfaceScatteringProfile();
                profile.filter1Variance  = new Color(0.3f, 0.3f, 0.3f, 0.0f);
                profile.filter2Variance  = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                profile.filterLerpWeight = 0.5f;
                profile.ComputeKernel();
                return profile;
            }
        }

        static float EvaluateZeroMeanGaussian(float x, float variance)
        {
            return Mathf.Exp(-x * x / (2 * variance)) / Mathf.Sqrt(2 * Mathf.PI * variance);
        }

        static float EvaluateGaussianCombination(float x, float variance1, float variance2, float lerpWeight)
        {
            return Mathf.Lerp(EvaluateZeroMeanGaussian(x, variance1),
                              EvaluateZeroMeanGaussian(x, variance2), lerpWeight);
        }

        void ComputeKernel()
        {
            // Our goal is to blur the image using a filter which is represented
            // as a product of a linear combination of two normalized 1D Gaussians
            // as suggested by Jimenez et al. in "Separable Subsurface Scattering".
            // A normalized (i.e. energy-preserving) 1D Gaussian with the mean of 0
            // is defined as follows: G1(x, v) = exp(-x² / (2 * v)) / sqrt(2 * Pi * v),
            // where 'v' is variance and 'x' is the radial distance from the origin.
            // Using the weight 'w', our 1D and the resulting 2D filters are given as:
            // A1(v1, v2, w, x)    = G1(x, v1) * (1 - w) + G1(r, v2) * w,
            // A2(v1, v2, w, x, y) = A1(v1, v2, w, x) * A1(v1, v2, w, y).
            // The resulting filter function is a non-Gaussian PDF.
            // It is separable by design, but generally not radially symmmetric.

            // TODO: importance-sample the truncated normal distribution:
            // https://people.sc.fsu.edu/~jburkardt/cpp_src/truncated_normal/truncated_normal.html
            // ALternatively, use a quadrature rule for the truncated normal PDF:
            // https://people.sc.fsu.edu/~jburkardt/cpp_src/truncated_normal_rule/truncated_normal_rule.html

            // For now, we use an ad-hoc approach.
            // We truncate the distribution at the radius of 3 standard deviations.
            float averageRadius1 = Mathf.Sqrt(m_filter1Variance.r)
                                 + Mathf.Sqrt(m_filter1Variance.g)
                                 + Mathf.Sqrt(m_filter1Variance.b);
            float averageRadius2 = Mathf.Sqrt(m_filter2Variance.r)
                                 + Mathf.Sqrt(m_filter2Variance.g)
                                 + Mathf.Sqrt(m_filter2Variance.b);
            float radius = Mathf.Lerp(averageRadius1, averageRadius2, m_filterLerpWeight);

            // We compute sample positions and weights using Gauss–Legendre quadrature.
            // The formula for the interval [a, b] is given here:
            // https://en.wikipedia.org/wiki/Gaussian_quadrature#Change_of_interval

            // Ref: http://keisan.casio.com/exec/system/1329114617
            float[] unitAbscissae = { 0.0f,        0.40584515f, -0.40584515f, 0.74153118f, -0.74153118f, 0.94910791f, -0.94910791f };
            float[] unitWeights   = { 0.41795918f, 0.38183005f,  0.38183005f, 0.27970539f,  0.27970539f, 0.12948496f,  0.12948496f };

            if (m_filterKernel == null)
            {
                m_filterKernel = new Vector4[numSamples];
            }

            for (int i = 0; i < numSamples; ++i)
            {
                // Perform the change of interval: {a, b} = {-radius, radius}.
                float weight   = radius * unitWeights[i];
                float position = radius * unitAbscissae[i];

                m_filterKernel[i].x = weight * EvaluateGaussianCombination(position, m_filter1Variance.r, m_filter2Variance.r, m_filterLerpWeight);
                m_filterKernel[i].y = weight * EvaluateGaussianCombination(position, m_filter1Variance.g, m_filter2Variance.g, m_filterLerpWeight);
                m_filterKernel[i].z = weight * EvaluateGaussianCombination(position, m_filter1Variance.b, m_filter2Variance.b, m_filterLerpWeight);
                m_filterKernel[i].w = position;
            }

            m_kernelNeedsUpdate = false;
        }
    }

    [System.Serializable]
    public class SubsurfaceScatteringParameters
    {
        public const int                     numProfiles = 1;
        public SubsurfaceScatteringProfile[] profiles;
        public float                         bilateralScale;

        // --- Methods ---

        public static SubsurfaceScatteringParameters Default
        {
            get
            {
                SubsurfaceScatteringParameters parameters = new SubsurfaceScatteringParameters();
                parameters.profiles = new SubsurfaceScatteringProfile[numProfiles];

                for (int i = 0; i < numProfiles; i++)
                {
                    parameters.profiles[i] = SubsurfaceScatteringProfile.Default;
                }

                parameters.bilateralScale = 0.0f;
                return parameters;
            }
        }
    }
}
