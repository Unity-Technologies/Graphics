using System;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.PathTracing.Core
{
    static internal class SphericalHarmonicsUtil
    {
        // 1/2 * sqrt(1/π)
        const float SH_L0_Normalization = 0.2820947917738781434740397257803862929220253146644994284220428608f;
        // 1/2 * sqrt(3/π)
        const float SH_L1_Normalization = 0.4886025119029199215863846228383470045758856081942277021382431574f;
        // sqrt(15/π)/2
        const float SH_L2_2_Normalization = 1.0925484305920790705433857058026884026904329595042589753478516999f;
        // sqrt(15/π)/2
        const float SH_L2_1_Normalization = SH_L2_2_Normalization;
        // sqrt(5/π)/4
        const float SH_L20_Normalization = 0.3153915652525200060308936902957104933242475070484115878434078878f;
        // sqrt(15/π)/2
        const float SH_L21_Normalization = SH_L2_2_Normalization;
        // sqrt(15/π)/4
        const float SH_L22_Normalization = 0.5462742152960395352716928529013442013452164797521294876739258499f;


        // Basis function Y_0.
        static float SHL0()
        {
            return SH_L0_Normalization;
        }

        // Basis function Y_1,-1.
        static float SHL1_1(float3 direction)
        {
            return SH_L1_Normalization * direction.x;
        }

        // Basis function Y_1,0.
        static float SHL10(float3 direction)
        {
            return SH_L1_Normalization * direction.y;
        }

        // Basis function Y_1,1.
        static float SHL11(float3 direction)
        {
            return SH_L1_Normalization * direction.z;
        }

        // Basis function Y_2,-2.
        static float SHL2_2(float3 direction)
        {
            return SH_L2_2_Normalization * direction.x * direction.y;
        }

        // Basis function Y_2,-1.
        static float SHL2_1(float3 direction)
        {
            return SH_L2_1_Normalization * direction.y * direction.z;
        }

        // Basis function Y_2,0.
        static float SHL20(float3 direction)
        {
            return SH_L20_Normalization * (3.0f * direction.z * direction.z - 1.0f);
        }

        // Basis function Y_2,1.
        static float SHL21(float3 direction)
        {
            return SH_L21_Normalization * direction.x * direction.z;
        }

        // Basis function Y_2,2.
        static float SHL22(float3 direction)
        {
            return SH_L22_Normalization * (direction.x * direction.x - direction.y * direction.y);
        }

        static public float3 EvaluateSH(Span<float> sh, float3 direction)
        {
            float3 res = new float3();
            res += new float3(sh[0], sh[9] , sh[18]) * SHL0();
            res += new float3(sh[1], sh[10], sh[19]) * SHL1_1(direction);
            res += new float3(sh[2], sh[11], sh[20]) * SHL10(direction);
            res += new float3(sh[3], sh[12], sh[21]) * SHL11(direction);
            res += new float3(sh[4], sh[13], sh[22]) * SHL2_2(direction);
            res += new float3(sh[5], sh[14], sh[23]) * SHL2_1(direction);
            res += new float3(sh[6], sh[15], sh[24]) * SHL20(direction);
            res += new float3(sh[7], sh[16], sh[25]) * SHL21(direction);
            res += new float3(sh[8], sh[17], sh[26]) * SHL22(direction);
            return res;
        }

        static public byte[] SHL2TolatLongEXR(float[] probesShData, int imageHeight, int maxProbeCount = 10)
        {
            Assert.AreEqual(probesShData.Length % 27, 0);
            int probeCount = math.min(probesShData.Length / 27, maxProbeCount);

            int imageWidth = imageHeight * 2;
            var outputColors = new Color[imageWidth * imageHeight * probeCount];

            for (int probe = 0; probe < probeCount; ++probe)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    for (int x = 0; x < imageWidth; x++)
                    {
                        float2 imageUv = (new float2(x, y) + 0.5f) / new float2(imageWidth, imageHeight);
                        float3 dir = LatlongCoordsToDirection(imageUv);

                        var eval = SphericalHarmonicsUtil.EvaluateSH(new Span<float>(probesShData, probe * 27, 27), dir);
                        outputColors[(y + probe * imageHeight) * imageWidth + x] = new Color(eval.x, eval.y, eval.z);
                    }
                }
            }

            return ImageConversion.EncodeArrayToEXR(outputColors, GraphicsFormat.R32G32B32A32_SFloat, (uint)imageWidth, (uint)(imageHeight * probeCount));
        }

        static float3 LatlongCoordsToDirection(float2 coord)
        {
            float theta = coord.y * math.PI;
            float phi = (coord.x * 2.0f * math.PI);

            float cosTheta = math.cos(theta);
            float sinTheta = math.sqrt(1.0f - math.min(1.0f, cosTheta * cosTheta));
            float cosPhi = math.cos(phi);
            float sinPhi = math.sin(phi);

            float3 direction = new float3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);
            return direction;
        }
    }
}
