using System;

namespace UnityEngine.Rendering.HighDefinition
{
    struct ZonalHarmonicsL2
    {
        public float[] coeffs; // Must have the size of 3

        public static ZonalHarmonicsL2 GetHenyeyGreensteinPhaseFunction(float anisotropy)
        {
            float g = anisotropy;

            var zh = new ZonalHarmonicsL2();
            zh.coeffs = new float[3];

            zh.coeffs[0] = 0.5f * Mathf.Sqrt(1.0f / Mathf.PI);
            zh.coeffs[1] = 0.5f * Mathf.Sqrt(3.0f / Mathf.PI) * g;
            zh.coeffs[2] = 0.5f * Mathf.Sqrt(5.0f / Mathf.PI) * g * g;

            return zh;
        }

        public static void GetCornetteShanksPhaseFunction(ZonalHarmonicsL2 zh, float anisotropy)
        {
            float g = anisotropy;

            zh.coeffs[0] = 0.282095f;
            zh.coeffs[1] = 0.293162f * g * (4.0f + (g * g)) / (2.0f + (g * g));
            zh.coeffs[2] = (0.126157f + 1.44179f * (g * g) + 0.324403f * (g * g) * (g * g)) / (2.0f + (g * g));
        }
    }

    [Serializable]
    internal struct SphericalHarmonicsL1
    {
        public Vector4 shAr;
        public Vector4 shAg;
        public Vector4 shAb;

        public static readonly SphericalHarmonicsL1 zero = new SphericalHarmonicsL1
        {
            shAr = Vector4.zero,
            shAg = Vector4.zero,
            shAb = Vector4.zero
        };

        // These operators are implemented so that SphericalHarmonicsL1 matches API of SphericalHarmonicsL2.
        public static SphericalHarmonicsL1 operator+(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr + rhs.shAr,
            shAg = lhs.shAg + rhs.shAg,
            shAb = lhs.shAb + rhs.shAb
        };

        public static SphericalHarmonicsL1 operator-(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr - rhs.shAr,
            shAg = lhs.shAg - rhs.shAg,
            shAb = lhs.shAb - rhs.shAb
        };

        public static SphericalHarmonicsL1 operator*(SphericalHarmonicsL1 lhs, float rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr * rhs,
            shAg = lhs.shAg * rhs,
            shAb = lhs.shAb * rhs
        };

        public static SphericalHarmonicsL1 operator/(SphericalHarmonicsL1 lhs, float rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr / rhs,
            shAg = lhs.shAg / rhs,
            shAb = lhs.shAb / rhs
        };

        public static bool operator==(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs)
        {
            return lhs.shAr == rhs.shAr
                && lhs.shAg == rhs.shAg
                && lhs.shAb == rhs.shAb;
        }

        public static bool operator!=(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object other)
        {
            if (!(other is SphericalHarmonicsL1)) return false;
            return this == (SphericalHarmonicsL1)other;
        }

        public override int GetHashCode()
        {
            return ((17 * 23 + shAr.GetHashCode()) * 23 + shAg.GetHashCode()) * 23 + shAb.GetHashCode();
        }
    }

    class SphericalHarmonicMath
    {
        // Ref: "Stupid Spherical Harmonics Tricks", p. 6.
        public static SphericalHarmonicsL2 Convolve(SphericalHarmonicsL2 sh, ZonalHarmonicsL2 zh)
        {
            for (int l = 0; l <= 2; l++)
            {
                float n = Mathf.Sqrt((4.0f * Mathf.PI) / (2 * l + 1));
                float k = zh.coeffs[l];
                float p = n * k;

                for (int m = -l; m <= l; m++)
                {
                    int i = l * (l + 1) + m;

                    for (int c = 0; c < 3; c++)
                    {
                        sh[c, i] *= p;
                    }
                }
            }

            return sh;
        }

        const float c0 = 0.28209479177387814347f; // 1/2  * sqrt(1/Pi)
        const float c1 = 0.32573500793527994772f; // 1/3  * sqrt(3/Pi)
        const float c2 = 0.27313710764801976764f; // 1/8  * sqrt(15/Pi)
        const float c3 = 0.07884789131313000151f; // 1/16 * sqrt(5/Pi)
        const float c4 = 0.13656855382400988382f; // 1/16 * sqrt(15/Pi)

        // Compute the inverse of SphericalHarmonicsL2::kNormalizationConstants.
        // See SetSHEMapConstants() in "Stupid Spherical Harmonics Tricks".

        static float[] invNormConsts = { 1 / c0, -1 / c1, 1 / c1, -1 / c1, 1 / c2, -1 / c2, 1 / c3, -1 / c2, 1 / c4 };

        // Undoes coefficient rescaling due to the convolution with the clamped cosine kernel
        // to obtain the canonical values of SH.
        public static SphericalHarmonicsL2 UndoCosineRescaling(SphericalHarmonicsL2 sh)
        {
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    sh[c, i] *= invNormConsts[i];
                }
            }

            return sh;
        }

        const float k0 = 0.28209479177387814347f; // {0, 0} : 1/2 * sqrt(1/Pi)
        const float k1 = 0.48860251190291992159f; // {1, 0} : 1/2 * sqrt(3/Pi)
        const float k2 = 1.09254843059207907054f; // {2,-2} : 1/2 * sqrt(15/Pi)
        const float k3 = 0.31539156525252000603f; // {2, 0} : 1/4 * sqrt(5/Pi)
        const float k4 = 0.54627421529603953527f; // {2, 2} : 1/4 * sqrt(15/Pi)

        static float[] ks = { k0, -k1, k1, -k1, k2, -k2, k3, -k2, k4 };


        // Premultiplies the SH with the polynomial coefficients of SH basis functions,
        // which avoids using any constants during SH evaluation.
        // The resulting evaluation takes the form:
        // (c_0 - c_6) + c_1 y + c_2 z + c_3 x + c_4 x y + c_5 y z + c_6 (3 z^2) + c_7 x z + c_8 (x^2 - y^2)
        public static SphericalHarmonicsL2 PremultiplyCoefficients(SphericalHarmonicsL2 sh)
        {
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    sh[c, i] *= ks[i];
                }
            }

            return sh;
        }

        public static SphericalHarmonicsL2 RescaleCoefficients(SphericalHarmonicsL2 sh, float scalar)
        {
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    sh[c, i] *= scalar;
                }
            }

            return sh;
        }

        // Packs coefficients so that we can use Peter-Pike Sloan's shader code.
        // Does not perform premultiplication with coefficients of SH basis functions.
        // See SetSHEMapConstants() in "Stupid Spherical Harmonics Tricks".
        public static void PackCoefficients(Vector4[] packedCoeffs, SphericalHarmonicsL2 sh)
        {
            // Constant + linear
            for (int c = 0; c < 3; c++)
            {
                packedCoeffs[c].Set(sh[c, 3], sh[c, 1], sh[c, 2], sh[c, 0] - sh[c, 6]);
            }

            // Quadratic (4/5)
            for (int c = 0; c < 3; c++)
            {
                packedCoeffs[3 + c].Set(sh[c, 4], sh[c, 5], sh[c, 6] * 3.0f, sh[c, 7]);
            }

            // Quadratic (5)
            packedCoeffs[6].Set(sh[0, 8], sh[1, 8], sh[2, 8], 1.0f);
        }
    }
}
