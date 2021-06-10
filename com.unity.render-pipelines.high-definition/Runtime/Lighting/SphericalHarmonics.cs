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
        public static SphericalHarmonicsL1 operator +(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr + rhs.shAr,
            shAg = lhs.shAg + rhs.shAg,
            shAb = lhs.shAb + rhs.shAb
        };

        public static SphericalHarmonicsL1 operator -(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr - rhs.shAr,
            shAg = lhs.shAg - rhs.shAg,
            shAb = lhs.shAb - rhs.shAb
        };

        public static SphericalHarmonicsL1 operator *(SphericalHarmonicsL1 lhs, float rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr * rhs,
            shAg = lhs.shAg * rhs,
            shAb = lhs.shAb * rhs
        };

        public static SphericalHarmonicsL1 operator /(SphericalHarmonicsL1 lhs, float rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr / rhs,
            shAg = lhs.shAg / rhs,
            shAb = lhs.shAb / rhs
        };

        public static bool operator ==(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs)
        {
            return lhs.shAr == rhs.shAr
                && lhs.shAg == rhs.shAg
                && lhs.shAb == rhs.shAb;
        }

        public static bool operator !=(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object other)
        {
            if (!(other is SphericalHarmonicsL1)) return false;
            return this == (SphericalHarmonicsL1) other;
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

        // Sources (derivation in shadertoy):
        // https://www.shadertoy.com/view/NlsGWB
        // http://filmicworlds.com/blog/simple-and-fast-spherical-harmonic-rotation/
        // https://zvxryb.github.io/blog/2015/09/03/sh-lighting-part2/
        public static void Rotate(Matrix4x4 M, ref SphericalHarmonicsL2 sh)
        {
            RotateBandL1(M, ref sh);
            RotateBandL2(M, ref sh);
        }

        public static void RotateBandL1(Matrix4x4 M, ref SphericalHarmonicsL2 sh)
        {
            Vector3 x0 = SphericalHarmonicsL2Utils.GetCoefficient(sh, 1);
            Vector3 x1 = SphericalHarmonicsL2Utils.GetCoefficient(sh, 2);
            Vector3 x2 = SphericalHarmonicsL2Utils.GetCoefficient(sh, 3);
            
            Matrix4x4 SH = new Matrix4x4();
            SH.SetColumn(0, new Vector4(x2.x, x2.y, x2.z, 0.0f));
            SH.SetColumn(1, new Vector4(x0.x, x0.y, x0.z, 0.0f));
            SH.SetColumn(2, new Vector4(x1.x, x1.y, x1.z, 0.0f));
            SH.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

            x0 = SH.MultiplyPoint3x4(new Vector3(M[1, 0], M[1, 1], M[1, 2]));
            x1 = SH.MultiplyPoint3x4(new Vector3(M[2, 0], M[2, 1], M[2, 2]));
            x2 = SH.MultiplyPoint3x4(new Vector3(M[0, 0], M[0, 1], M[0, 2]));

            SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 1, x0);
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 2, x1);
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 3, x2);
        }

        public static void RotateBandL2(Matrix4x4 M, ref SphericalHarmonicsL2 sh)
        {
            Vector3 x0 = SphericalHarmonicsL2Utils.GetCoefficient(sh, 4);
            Vector3 x1 = SphericalHarmonicsL2Utils.GetCoefficient(sh, 5);
            Vector3 x2 = SphericalHarmonicsL2Utils.GetCoefficient(sh, 6);
            Vector3 x3 = SphericalHarmonicsL2Utils.GetCoefficient(sh, 7);
            Vector3 x4 = SphericalHarmonicsL2Utils.GetCoefficient(sh, 8);

            // Decomposed + factored version of 5x5 matrix multiply of invA * sh from source.
            Vector3 sh0 = x1 * 0.5f + (x3 * -0.5f + x4 * 2.0f);
            Vector3 sh1 = x0 * 0.5f + 3.0f * x2 - x3 * 0.5f + x4;
            Vector3 sh2 = x0;
            Vector3 sh3 = x3;
            Vector3 sh4 = x1;

            const float kInv = 1.41421356237f; // sqrt(2.0f);
            const float k3 = 0.25f;
            const float k4 = -1.0f / 6.0f;
            
            // Decomposed + factored version of 5x5 matrix multiply of 5 normals projected to 5 SH2 bands.
            // Column 0
            {
                Vector3 rn0 = new Vector3(M[0, 0], M[1, 0], M[2, 0]) * kInv; // (Vector3(1, 0, 0) * M) / k;
                x0 = (rn0.x * rn0.y) * sh0;
                x1 = (rn0.y * rn0.z) * sh0;
                x2 = (rn0.z * rn0.z * k3 + k4) * sh0;
                x3 = (rn0.x * rn0.z) * sh0;
                x4 = (rn0.x * rn0.x - rn0.y * rn0.y) * sh0;
            }

            // Column 1
            {
                Vector3 rn1 = new Vector3(M[0, 2], M[1, 2], M[2, 2]) * kInv; // (Vector3(0, 0, 1) * M) / k;
                x0 += (rn1.x * rn1.y) * sh1;
                x1 += (rn1.y * rn1.z) * sh1;
                x2 += (rn1.z * rn1.z * k3 + k4) * sh1;
                x3 += (rn1.x * rn1.z) * sh1;
                x4 += (rn1.x * rn1.x - rn1.y * rn1.y) * sh1;
            }

            // Column 2
            {
                Vector3 rn2 = new Vector3(M[0, 0] + M[0, 1], M[1, 0] + M[1, 1], M[2, 0] + M[2, 1]); // (Vector3(k, k, 0) * M) / k;
                x0 += (rn2.x * rn2.y) * sh2;
                x1 += (rn2.y * rn2.z) * sh2;
                x2 += (rn2.z * rn2.z * k3 + k4) * sh2;
                x3 += (rn2.x * rn2.z) * sh2;
                x4 += (rn2.x * rn2.x - rn2.y * rn2.y) * sh2;
            }

            // Column 3
            {
                Vector3 rn3 = new Vector3(M[0, 0] + M[0, 2], M[1, 0] + M[1, 2], M[2, 0] + M[2, 2]); // (Vector3(k, 0, k) * M) / k;
                x0 += (rn3.x * rn3.y) * sh3;
                x1 += (rn3.y * rn3.z) * sh3;
                x2 += (rn3.z * rn3.z * k3 + k4) * sh3;
                x3 += (rn3.x * rn3.z) * sh3;
                x4 += (rn3.x * rn3.x - rn3.y * rn3.y) * sh3;
            }

            // Column 4
            {
                Vector3 rn4 = new Vector3(M[0, 1] + M[0, 2], M[1, 1] + M[1, 2], M[2, 1] + M[2, 2]); // (Vector3(0, k, k) * M) / k;
                x0 += (rn4.x * rn4.y) * sh4;
                x1 += (rn4.y * rn4.z) * sh4;
                x2 += (rn4.z * rn4.z * k3 + k4) * sh4;
                x3 += (rn4.x * rn4.z) * sh4;
                x4 += (rn4.x * rn4.x - rn4.y * rn4.y) * sh4;
            }

            x4 *= 0.25f;

            SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 4, x0);
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 5, x1);
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 6, x2);
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 7, x3);
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 8, x4);
        }
    }

    /// <summary>
    /// A collection of utility functions used to access and set SphericalHarmonicsL2 in a more verbose way.
    /// </summary>
    public class SphericalHarmonicsL2Utils
    {
        /// <summary>
        /// Returns the L1 coefficients organized in such a way that are swizzled per channel rather than per coefficient.
        /// </summary>
        /// <param name ="sh"> The SphericalHarmonicsL2 data structure to use to query the information.</param>
        /// <param name ="L1_R">The red channel of all coefficient for the L1 band.</param>
        /// <param name ="L1_G">The green channel of all coefficient for the L1 band.</param>
        /// <param name ="L1_B">The blue channel of all coefficient for the L1 band.</param>
        public static void GetL1(SphericalHarmonicsL2 sh, out Vector3 L1_R, out Vector3 L1_G, out Vector3 L1_B)
        {
            L1_R = new Vector3(sh[0, 1],
                sh[0, 2],
                sh[0, 3]);

            L1_G = new Vector3(sh[1, 1],
                sh[1, 2],
                sh[1, 3]);

            L1_B = new Vector3(sh[2, 1],
                sh[2, 2],
                sh[2, 3]);
        }

        /// <summary>
        /// Returns all the L2 coefficients.
        /// </summary>
        /// <param name ="sh"> The SphericalHarmonicsL2 data structure to use to query the information.</param>
        /// <param name ="L2_0">The first coefficient for the L2 band.</param>
        /// <param name ="L2_1">The second coefficient for the L2 band.</param>
        /// <param name ="L2_2">The third coefficient for the L2 band.</param>
        /// <param name ="L2_3">The fourth coefficient for the L2 band.</param>
        /// <param name ="L2_4">The fifth coefficient for the L2 band.</param>
        public static void GetL2(SphericalHarmonicsL2 sh, out Vector3 L2_0, out Vector3 L2_1, out Vector3 L2_2, out Vector3 L2_3, out Vector3 L2_4)
        {
            L2_0 = new Vector3(sh[0, 4],
                sh[1, 4],
                sh[2, 4]);

            L2_1 = new Vector3(sh[0, 5],
                sh[1, 5],
                sh[2, 5]);

            L2_2 = new Vector3(sh[0, 6],
                sh[1, 6],
                sh[2, 6]);

            L2_3 = new Vector3(sh[0, 7],
                sh[1, 7],
                sh[2, 7]);

            L2_4 = new Vector3(sh[0, 8],
                sh[1, 8],
                sh[2, 8]);
        }

        /// <summary>
        /// Set L0 coefficient.
        /// </summary>
        /// <param name ="sh">The SphericalHarmonicsL2 data structure to store information on.</param>
        /// <param name ="L0">The L0 coefficient to set.</param>
        public static void SetL0(ref SphericalHarmonicsL2 sh, Vector3 L0)
        {
            sh[0, 0] = L0.x;
            sh[1, 0] = L0.y;
            sh[2, 0] = L0.z;
        }

        /// <summary>
        /// Set the red channel for each of the L1 coefficients.
        /// </summary>
        /// <param name ="sh">The SphericalHarmonicsL2 data structure to store information on.</param>
        /// <param name ="L1_R">The red channels for each L1 coefficient.</param>
        public static void SetL1R(ref SphericalHarmonicsL2 sh, Vector3 L1_R)
        {
            sh[0, 1] = L1_R.x;
            sh[0, 2] = L1_R.y;
            sh[0, 3] = L1_R.z;
        }

        /// <summary>
        /// Set the green channel for each of the L1 coefficients.
        /// </summary>
        /// <param name ="sh">The SphericalHarmonicsL2 data structure to store information on.</param>
        /// <param name ="L1_G">The green channels for each L1 coefficient.</param>
        public static void SetL1G(ref SphericalHarmonicsL2 sh, Vector3 L1_G)
        {
            sh[1, 1] = L1_G.x;
            sh[1, 2] = L1_G.y;
            sh[1, 3] = L1_G.z;
        }

        /// <summary>
        /// Set the blue channel for each of the L1 coefficients.
        /// </summary>
        /// <param name ="sh">The SphericalHarmonicsL2 data structure to store information on.</param>
        /// <param name ="L1_B">The blue channels for each L1 coefficient.</param>
        public static void SetL1B(ref SphericalHarmonicsL2 sh, Vector3 L1_B)
        {
            sh[2, 1] = L1_B.x;
            sh[2, 2] = L1_B.y;
            sh[2, 3] = L1_B.z;
        }

        /// <summary>
        /// Set all L1 coefficients per channel.
        /// </summary>
        /// <param name ="sh">The SphericalHarmonicsL2 data structure to store information on.</param>
        /// <param name ="L1_R">The red channels for each L1 coefficient.</param>
        /// <param name ="L1_G">The green channels for each L1 coefficient.</param>
        /// <param name ="L1_B">The blue channels for each L1 coefficient.</param>
        public static void SetL1(ref SphericalHarmonicsL2 sh, Vector3 L1_R, Vector3 L1_G, Vector3 L1_B)
        {
            SetL1R(ref sh, L1_R);
            SetL1G(ref sh, L1_G);
            SetL1B(ref sh, L1_B);
        }

        /// <summary>
        /// Set a spherical harmonics coefficient.
        /// </summary>
        /// <param name ="sh">The SphericalHarmonicsL2 data structure to store information on.</param>
        /// <param name ="index">The index of the coefficient that is set (must be less than 9).</param>
        /// <param name ="coefficient">The values of the coefficient is set.</param>
        public static void SetCoefficient(ref SphericalHarmonicsL2 sh, int index, Vector3 coefficient)
        {
            Debug.Assert(index < 9);
            sh[0, index] = coefficient.x;
            sh[1, index] = coefficient.y;
            sh[2, index] = coefficient.z;
        }

        /// <summary>
        /// Get a spherical harmonics coefficient.
        /// </summary>
        /// <param name ="sh">The SphericalHarmonicsL2 data structure to get information from.</param>
        /// <param name ="index">The index of the coefficient that is requested (must be less than 9).</param>
        /// <returns>The value of the requested coefficient.</returns>
        public static Vector3 GetCoefficient(SphericalHarmonicsL2 sh, int index)
        {
            Debug.Assert(index < 9);
            return new Vector3(sh[0, index], sh[1, index], sh[2, index]);
        }
    }
}
