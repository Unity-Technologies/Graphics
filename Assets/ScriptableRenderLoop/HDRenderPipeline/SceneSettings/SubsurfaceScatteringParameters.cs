using System;

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public class SubsurfaceScatteringProfile
    {
        public const int numSamples = 7;

        Color     m_filterVariance1;
        Color     m_filterVariance2;
        float     m_filterLerpWeight;
        Vector4[] m_filterKernel;
        bool      m_kernelNeedsUpdate;

        // --- Methods ---

        public Color filterVariance1
        {
            get { return m_filterVariance1; }
            set { if (m_filterVariance1 != value) { m_filterVariance1 = value; m_kernelNeedsUpdate = true; } }
        }

        public Color filterVariance2
        {
            get { return m_filterVariance2; }
            set { if (m_filterVariance2 != value) { m_filterVariance2 = value; m_kernelNeedsUpdate = true; } }
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
                profile.filterVariance1  = new Color(0.3f, 0.3f, 0.3f, 0.0f);
                profile.filterVariance2  = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                profile.filterLerpWeight = 0.5f;
                profile.ComputeKernel();
                return profile;
            }
        }

        static float Gaussian(float x, float variance)
        {
            return Mathf.Exp(-x * x / (2 * variance)) / Mathf.Sqrt(2 * Mathf.PI * variance);
        }

        static float GaussianCombination(float x, float variance1, float variance2, float lerpWeight)
        {
            return Mathf.Lerp(Gaussian(x, variance1), Gaussian(x, variance2), lerpWeight);
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

        // Ref: https://en.wikipedia.org/wiki/Halton_sequence
        static float VanDerCorput(uint b, uint i)
        {
            float r = 0;
            float f = 1;
            
            while (i > 0) 
            {
                f = f / b;
                r = r + f * (i % b);
                i = i / b;
            }

            return r;
        }

        // Ref: http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
        static float VanDerCorputBase2(uint i)
        {
            i = (i << 16) | (i >> 16);
            i = ((i & 0x00ff00ff) << 8) | ((i & 0xff00ff00) >> 8);
            i = ((i & 0x0f0f0f0f) << 4) | ((i & 0xf0f0f0f0) >> 4);
            i = ((i & 0x33333333) << 2) | ((i & 0xcccccccc) >> 2);
            i = ((i & 0x55555555) << 1) | ((i & 0xaaaaaaaa) >> 1);

            return i * (1.0f / 4294967296);
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
            float maxVariance1 = Mathf.Max(m_filterVariance1.r, m_filterVariance1.g, m_filterVariance1.b);
            float maxVariance2 = Mathf.Max(m_filterVariance2.r, m_filterVariance2.g, m_filterVariance2.b);

            // Importance sample two Gaussians based on the interpolation weight.
            float sd = Mathf.Lerp(Mathf.Sqrt(maxVariance1), Mathf.Sqrt(maxVariance2), m_filterLerpWeight);

            Vector3 weightSum = new Vector3(0, 0, 0); 

            for (uint i = 0; i < numSamples; i++)
            {
                float u   = VanDerCorputBase2(i + 1);
                float pos = NormalCdfInverse(u, sd);
                float pdf = Gaussian(pos, sd * sd);

                Vector3 val;
                val.x = GaussianCombination(pos, m_filterVariance1.r, m_filterVariance2.r, m_filterLerpWeight);
                val.y = GaussianCombination(pos, m_filterVariance1.g, m_filterVariance2.g, m_filterLerpWeight);
                val.z = GaussianCombination(pos, m_filterVariance1.b, m_filterVariance2.b, m_filterLerpWeight);

                m_filterKernel[i].x = val.x / (pdf * numSamples);
                m_filterKernel[i].y = val.y / (pdf * numSamples);
                m_filterKernel[i].z = val.z / (pdf * numSamples);
                m_filterKernel[i].w = pos;

                weightSum.x += m_filterKernel[i].x;
                weightSum.y += m_filterKernel[i].y;
                weightSum.z += m_filterKernel[i].z;
            }

            // Renormalize the weights to conserve energy.
            for (uint i = 0; i < numSamples; i++)
            {
                m_filterKernel[i].x *= 1.0f / weightSum.x;
                m_filterKernel[i].y *= 1.0f / weightSum.y;
                m_filterKernel[i].z *= 1.0f / weightSum.z;
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

                parameters.bilateralScale = 0.1f;
                return parameters;
            }
        }
    }
}
