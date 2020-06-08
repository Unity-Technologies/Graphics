using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    public struct SphericalHarmonicsL1
    {
        public Vector4 shAr;
        public Vector4 shAg;
        public Vector4 shAb;

        public static SphericalHarmonicsL1 GetNeutralValues()
        {
            SphericalHarmonicsL1 sh;
            sh.shAr = Vector4.zero;
            sh.shAg = Vector4.zero;
            sh.shAb = Vector4.zero;
            return sh;
        }
    }
}
