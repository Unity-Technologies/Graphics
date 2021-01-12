using System;

namespace UnityEngine.Rendering
{
    [Serializable]
    public struct SphericalHarmonicsL1
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
}
