using System;

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public class SubsurfaceScatteringProfile
    {
        public const int numSamples = 7;

        Color     m_StdDev1;
        Color     m_StdDev2;
        float     m_LerpWeight;
        Vector4[] m_FilterKernel;
        bool      m_KernelNeedsUpdate;

        // --- Methods ---

        public Color stdDev1
        {
            get { return m_StdDev1; }
            set { if (m_StdDev1 != value) { m_StdDev1 = value; m_KernelNeedsUpdate = true; } }
        }

        public Color stdDev2
        {
            get { return m_StdDev2; }
            set { if (m_StdDev2 != value) { m_StdDev2 = value; m_KernelNeedsUpdate = true; } }
        }

        public float lerpWeight
        {
            get { return m_LerpWeight; }
            set { if (m_LerpWeight != value) { m_LerpWeight = value; m_KernelNeedsUpdate = true; } }
        }

        public Vector4[] filterKernel
        {
            get { if (m_KernelNeedsUpdate) ComputeKernel(); return m_FilterKernel; }
        }

        public static SubsurfaceScatteringProfile Default
        {
            get
            {
                SubsurfaceScatteringProfile profile = new SubsurfaceScatteringProfile();
                profile.stdDev1    = new Color(0.3f, 0.3f, 0.3f, 0.0f);
                profile.stdDev2    = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                profile.lerpWeight = 0.5f;
                profile.ComputeKernel();
                return profile;
            }
        }

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
            if (m_FilterKernel == null)
            {
                m_FilterKernel = new Vector4[numSamples];
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
            float maxStdDev1 = Mathf.Max(m_StdDev1.r, m_StdDev1.g, m_StdDev1.b);
            float maxStdDev2 = Mathf.Max(m_StdDev2.r, m_StdDev2.g, m_StdDev2.b);

            Vector3 weightSum = new Vector3(0, 0, 0); 

            // Importance sample the linear combination of two Gaussians.
            for (uint i = 0; i < numSamples; i++)
            {
                float u   = VanDerCorputBase2(i + 1);
                float pos = GaussianCombinationCdfInverse(u, maxStdDev1, maxStdDev2, m_LerpWeight);
                float pdf = GaussianCombination(pos, maxStdDev1, maxStdDev2, m_LerpWeight);

                Vector3 val;
                val.x = GaussianCombination(pos, m_StdDev1.r, m_StdDev2.r, m_LerpWeight);
                val.y = GaussianCombination(pos, m_StdDev1.g, m_StdDev2.g, m_LerpWeight);
                val.z = GaussianCombination(pos, m_StdDev1.b, m_StdDev2.b, m_LerpWeight);

                m_FilterKernel[i].x = val.x / (pdf * numSamples);
                m_FilterKernel[i].y = val.y / (pdf * numSamples);
                m_FilterKernel[i].z = val.z / (pdf * numSamples);
                m_FilterKernel[i].w = pos;

                weightSum.x += m_FilterKernel[i].x;
                weightSum.y += m_FilterKernel[i].y;
                weightSum.z += m_FilterKernel[i].z;
            }

            // Renormalize the weights to conserve energy.
            for (uint i = 0; i < numSamples; i++)
            {
                m_FilterKernel[i].x *= 1.0f / weightSum.x;
                m_FilterKernel[i].y *= 1.0f / weightSum.y;
                m_FilterKernel[i].z *= 1.0f / weightSum.z;
            }

            m_KernelNeedsUpdate = false;
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
