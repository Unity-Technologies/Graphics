using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static Vector4[] m_PackedCoeffsProbe = new Vector4[7];

        // Ref: "Efficient Evaluation of Irradiance Environment Maps" from ShaderX 2
        static Vector3 SHEvalLinearL0L1(Vector3 N, Vector4 shAr, Vector4 shAg, Vector4 shAb)
        {
            Vector4 vA = new Vector4(N.x, N.y, N.z, 1.0f);

            Vector3 x1;
            // Linear (L1) + constant (L0) polynomial terms
            x1.x = Vector4.Dot(shAr, vA);
            x1.y = Vector4.Dot(shAg, vA);
            x1.z = Vector4.Dot(shAb, vA);

            return x1;
        }

        static Vector3 SHEvalLinearL2(Vector3 N, Vector4 shBr, Vector4 shBg, Vector4 shBb, Vector4 shC)
        {
            Vector3 x2;
            // 4 of the quadratic (L2) polynomials
            Vector4 vB = new Vector4(N.x * N.y, N.y * N.z, N.z * N.z, N.z * N.x);
            x2.x = Vector4.Dot(shBr, vB);
            x2.y = Vector4.Dot(shBg, vB);
            x2.z = Vector4.Dot(shBb, vB);

            // Final (5th) quadratic (L2) polynomial
            float vC = N.x * N.x - N.y * N.y;
            Vector3 x3 = new Vector3(0.0f, 0.0f, 0.0f);
            x3.x = shC.x * vC;
            x3.y = shC.y * vC;
            x3.z = shC.z * vC;
            return x2 + x3;
        }

        static Vector3 EvaluateAmbientProbe(in Vector4[] packedCoeffs, Vector3 direction)
        {
            Vector4 shAr = packedCoeffs[0];
            Vector4 shAg = packedCoeffs[1];
            Vector4 shAb = packedCoeffs[2];
            Vector4 shBr = packedCoeffs[3];
            Vector4 shBg = packedCoeffs[4];
            Vector4 shBb = packedCoeffs[5];
            Vector4 shCr = packedCoeffs[6];

            // Linear + constant polynomial terms
            Vector3 res = SHEvalLinearL0L1(direction, shAr, shAg, shAb);

            // Quadratic polynomials
            res += SHEvalLinearL2(direction, shBr, shBg, shBb, shCr);

            // Return the result
            return res;
        }

        static internal Vector3 EvaluateAmbientProbe(in SphericalHarmonicsL2 sh, Vector3 direction)
        {
            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffsProbe, sh);
            return EvaluateAmbientProbe(m_PackedCoeffsProbe, direction);
        }
    }
}
