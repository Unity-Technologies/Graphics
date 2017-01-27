using System;

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
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

        static double RationalApproximation(double t)
        {
            // Abramowitz and Stegun formula 26.2.23.
            // The absolute value of the error should be less than 4.5 e-4.
            double[] c = {2.515517, 0.802853, 0.010328};
            double[] d = {1.432788, 0.189269, 0.001308};
            return t - ((c[2] * t + c[1]) * t + c[0]) / (((d[2] * t + d[1]) * t + d[0]) * t + 1.0);
        }
 
        // Ref: https://www.johndcook.com/blog/csharp_phi_inverse/
        static double NormalCDFInverse(double p, double stdDeviation)
        {
            double x;

            if (p < 0.5)
            {
                // F^-1(p) = - G^-1(p)
                x = -RationalApproximation(Math.Sqrt(-2.0 * Math.Log(p)));
            }
            else
            {
                // F^-1(p) = G^-1(1-p)
                x = RationalApproximation(Math.Sqrt(-2.0*Math.Log(1.0 - p)));
            }

            return x * stdDeviation;
        }

        // Ref: http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
        double VanDerCorputBase2(uint bits)
        {
            bits = (bits << 16) | (bits >> 16);
            bits = ((bits & 0x00ff00ff) << 8) | ((bits & 0xff00ff00) >> 8);
            bits = ((bits & 0x0f0f0f0f) << 4) | ((bits & 0xf0f0f0f0) >> 4);
            bits = ((bits & 0x33333333) << 2) | ((bits & 0xcccccccc) >> 2);
            bits = ((bits & 0x55555555) << 1) | ((bits & 0xaaaaaaaa) >> 1);
            return bits * 2.3283064365386963e-10; // 0x100000000
        }

        void ComputeKernel()
        {
            if (m_filterKernel == null)
            {
                m_filterKernel = new Vector4[numSamples];
            }

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

            // Find the widest Gaussian across 3 color channels.
            float maxStdDev1 = Mathf.Sqrt(Mathf.Max(m_filter1Variance.r, m_filter1Variance.g, m_filter1Variance.b));
            float maxStdDev2 = Mathf.Sqrt(Mathf.Max(m_filter2Variance.r, m_filter2Variance.g, m_filter2Variance.b));

            // Importance sample two Gaussians based on the interpolation weight.
            for (uint i = 0; i < numSamples; ++i)
            {
                double u1 = (i + 0.5) / numSamples;
                double u2 = VanDerCorputBase2(i + 1);
                double sd = u1 < m_filterLerpWeight ? maxStdDev1 : maxStdDev2;
                float pos = (float)NormalCDFInverse(u2, sd);
                // Since our filter is normalized, f(x) / p(x) = 1.
                m_filterKernel[i].x = 1.0f / numSamples;
                m_filterKernel[i].y = 1.0f / numSamples;
                m_filterKernel[i].z = 1.0f / numSamples;
                m_filterKernel[i].w = pos;
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
