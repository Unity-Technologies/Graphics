using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.PathTracing.Core;
using System;
using UnityEngine;

#if ENABLE_IMAGECONVERSION_MODULE
using UnityEngine.Experimental.Rendering;
#endif

namespace Unity.PathTracing.Editor
{
    static internal class SphericalHarmonicsToLatLong
    {
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
#if ENABLE_IMAGECONVERSION_MODULE
            return ImageConversion.EncodeArrayToEXR(outputColors, GraphicsFormat.R32G32B32A32_SFloat, (uint)imageWidth, (uint)(imageHeight * probeCount));
#else
            Debug.Assert(false, "The Image Conversion Module is not available.");
            return Array.Empty<byte>();
#endif
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
