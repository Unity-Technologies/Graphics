using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Structure holding Spherical Harmonic L1 coefficient.
    /// </summary>
    [Serializable]
    public struct SphericalHarmonicsL1
    {
        /// <summary>
        /// Red channel of each of the three L1 SH coefficient.
        /// </summary>
        public Vector4 shAr;
        /// <summary>
        /// Green channel of each of the three L1 SH coefficient.
        /// </summary>
        public Vector4 shAg;
        /// <summary>
        /// Blue channel of each of the three L1 SH coefficient.
        /// </summary>
        public Vector4 shAb;

        /// <summary>
        /// A set of L1 coefficients initialized to zero.
        /// </summary>
        public static readonly SphericalHarmonicsL1 zero = new SphericalHarmonicsL1
        {
            shAr = Vector4.zero,
            shAg = Vector4.zero,
            shAb = Vector4.zero
        };

        // These operators are implemented so that SphericalHarmonicsL1 matches API of SphericalHarmonicsL2.

        /// <summary>
        /// Sum two SphericalHarmonicsL1.
        /// </summary>
        /// <param name="lhs">First SphericalHarmonicsL1.</param>
        /// <param name="rhs">Second SphericalHarmonicsL1.</param>
        /// <returns>The resulting SphericalHarmonicsL1.</returns>
        public static SphericalHarmonicsL1 operator +(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr + rhs.shAr,
            shAg = lhs.shAg + rhs.shAg,
            shAb = lhs.shAb + rhs.shAb
        };

        /// <summary>
        /// Subtract two SphericalHarmonicsL1.
        /// </summary>
        /// <param name="lhs">First SphericalHarmonicsL1.</param>
        /// <param name="rhs">Second SphericalHarmonicsL1.</param>
        /// <returns>The resulting SphericalHarmonicsL1.</returns>
        public static SphericalHarmonicsL1 operator -(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr - rhs.shAr,
            shAg = lhs.shAg - rhs.shAg,
            shAb = lhs.shAb - rhs.shAb
        };

        /// <summary>
        /// Multiply two SphericalHarmonicsL1.
        /// </summary>
        /// <param name="lhs">First SphericalHarmonicsL1.</param>
        /// <param name="rhs">Second SphericalHarmonicsL1.</param>
        /// <returns>The resulting SphericalHarmonicsL1.</returns>
        public static SphericalHarmonicsL1 operator *(SphericalHarmonicsL1 lhs, float rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr * rhs,
            shAg = lhs.shAg * rhs,
            shAb = lhs.shAb * rhs
        };

        /// <summary>
        /// Divide two SphericalHarmonicsL1.
        /// </summary>
        /// <param name="lhs">First SphericalHarmonicsL1.</param>
        /// <param name="rhs">Second SphericalHarmonicsL1.</param>
        /// <returns>The resulting SphericalHarmonicsL1.</returns>
        public static SphericalHarmonicsL1 operator /(SphericalHarmonicsL1 lhs, float rhs) => new SphericalHarmonicsL1()
        {
            shAr = lhs.shAr / rhs,
            shAg = lhs.shAg / rhs,
            shAb = lhs.shAb / rhs
        };

        /// <summary>
        /// Compare two SphericalHarmonicsL1.
        /// </summary>
        /// <param name="lhs">First SphericalHarmonicsL1.</param>
        /// <param name="rhs">Second SphericalHarmonicsL1.</param>
        /// <returns>Whether the SphericalHarmonicsL1 match.</returns>
        public static bool operator ==(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs)
        {
            return lhs.shAr == rhs.shAr
                && lhs.shAg == rhs.shAg
                && lhs.shAb == rhs.shAb;
        }

        /// <summary>
        /// Check two SphericalHarmonicsL1 inequality.
        /// </summary>
        /// <param name="lhs">First SphericalHarmonicsL1.</param>
        /// <param name="rhs">Second SphericalHarmonicsL1.</param>
        /// <returns>Whether the SphericalHarmonicsL1 are different.</returns>
        public static bool operator !=(SphericalHarmonicsL1 lhs, SphericalHarmonicsL1 rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compare this SphericalHarmonicsL1 with an object.
        /// </summary>
        /// <param name="other">The object to compare with.</param>
        /// <returns>Whether the SphericalHarmonicsL1 is equal to the object passed.</returns>
        public override bool Equals(object other)
        {
            if (!(other is SphericalHarmonicsL1)) return false;
            return this == (SphericalHarmonicsL1)other;
        }

        /// <summary>
        /// Produces an hash code of the SphericalHarmonicsL1.
        /// </summary>
        /// <returns>The hash code for this SphericalHarmonicsL1.</returns>
        public override int GetHashCode()
        {
            return ((17 * 23 + shAr.GetHashCode()) * 23 + shAg.GetHashCode()) * 23 + shAb.GetHashCode();
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
