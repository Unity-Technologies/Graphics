using Unity.Mathematics;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static internal void GetFFTKernels(ComputeShader fourierTransformCS, WaterSimulationResolution resolution, out int rowKernel, out int columnKernel)
        {
            switch (resolution)
            {
                case WaterSimulationResolution.High256:
                {
                    rowKernel = fourierTransformCS.FindKernel("RowPassTi_256");
                    columnKernel = fourierTransformCS.FindKernel("ColPassTi_256");
                }
                break;
                case WaterSimulationResolution.Medium128:
                {
                    rowKernel = fourierTransformCS.FindKernel("RowPassTi_128");
                    columnKernel = fourierTransformCS.FindKernel("ColPassTi_128");
                }
                break;
                case WaterSimulationResolution.Low64:
                {
                    rowKernel = fourierTransformCS.FindKernel("RowPassTi_64");
                    columnKernel = fourierTransformCS.FindKernel("ColPassTi_64");
                }
                break;
                default:
                {
                    rowKernel = fourierTransformCS.FindKernel("RowPassTi_64");
                    columnKernel = fourierTransformCS.FindKernel("ColPassTi_64");
                }
                break;
            }
        }

        static internal float EvaluateFrequencyOffset(WaterSimulationResolution resolution)
        {
            switch (resolution)
            {
                case WaterSimulationResolution.High256:
                    return 0.5f;
                case WaterSimulationResolution.Medium128:
                    return 0.25f;
                case WaterSimulationResolution.Low64:
                    return 0.125f;
                default:
                    return 0.5f;
            }
        }

        static internal int EvaluateWaterNoiseSampleOffset(WaterSimulationResolution resolution)
        {
            switch (resolution)
            {
                case WaterSimulationResolution.High256:
                    return 0;
                case WaterSimulationResolution.Medium128:
                    return 64;
                case WaterSimulationResolution.Low64:
                    return 96;
                default:
                    return 0;
            }
        }

        static internal void BuildGridMesh(ref Mesh mesh)
        {
            mesh = new Mesh();
            Vector3[] vertices = new Vector3[(k_WaterTessellatedMeshResolution + 1) * (k_WaterTessellatedMeshResolution + 1)];
            for (int i = 0, y = 0; y <= k_WaterTessellatedMeshResolution; y++)
            {
                for (int x = 0; x <= k_WaterTessellatedMeshResolution; x++, i++)
                {
                    vertices[i] = new Vector3(x / (float)k_WaterTessellatedMeshResolution - 0.5f, 0.0f, y / (float)k_WaterTessellatedMeshResolution - 0.5f);
                }
            }
            mesh.vertices = vertices;

            int[] triangles = new int[k_WaterTessellatedMeshResolution * k_WaterTessellatedMeshResolution * 6];
            for (int ti = 0, vi = 0, y = 0; y < k_WaterTessellatedMeshResolution; y++, vi++)
            {
                for (int x = 0; x < k_WaterTessellatedMeshResolution; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = vi + k_WaterTessellatedMeshResolution + 1;
                    triangles[ti + 5] = vi + k_WaterTessellatedMeshResolution + 2;
                }
            }
            mesh.triangles = triangles;
        }

        // Converts an angle to a 2d direction
        static internal Vector2 OrientationToDirection(float orientation)
        {
            float orientationRad = orientation * Mathf.Deg2Rad;
            float directionX = Mathf.Cos(orientationRad);
            float directionY = Mathf.Sin(orientationRad);
            return new Vector2(directionX, directionY);
        }

        internal static float AmplitudeToWindSpeed(float normalizedAmplitude)
        {
            // First it is converted from normalized space to km/h then to m/s
            return normalizedAmplitude * 100.0f * 0.277778f;
        }

        // Function that loops thought all the current waves and computes the maximal wave height
        internal static void ComputeMaximumWaveHeight(float maxAmplitude, bool highBandCount, out Vector4 waveHeights, out float maxWaveHeight)
        {
            // Initialize the band data
            float b0 = 0.0f, b1 = 0.0f, b2 = 0.0f, b3 = 0.0f;

            // Evaluate the wave height for each band (lower frequencies)
            b0 = maxAmplitude;
            b1 = maxAmplitude;

            // Evaluate the wave height for each band (higher frequencies)
            if (highBandCount)
            {
                b2 = maxAmplitude;
                b3 = maxAmplitude;
            }

            // Output the wave heights
            waveHeights = new Vector4(b0, b1, b2, b3);
            // TODO have a better estimation for this
            maxWaveHeight = maxAmplitude * 2.0f;
        }

        static float EvaluatePolynomial3(float x, float c0, float c1, float c2, float c3)
        {
            float x2 = x * x;
            float x3 = x2 * x;
            return x3 * c3 + x2 * c2 + x * c1 + c0;
        }

        static internal float ComputeOceanMaxPatchSize(float maxAmplitude, float waveSize)
        {
            float patchSizeMin = EvaluatePolynomial3(maxAmplitude, -24.23f, 41.01f, -1.03f, 0.013f);
            float patchSizeMax = maxAmplitude * 50.0f;
            return Mathf.Lerp(patchSizeMin, patchSizeMax, waveSize);
        }

        // Function that evaluates the patch sizes of the 4 bands based on the max patch size
        static internal Vector4 ComputeBandPatchSizes(float maxPatchSize)
        {
            float range = maxPatchSize - k_MinPatchSize;
            float b0 = maxPatchSize;
            float b1 = maxPatchSize - 7.0f / 8.0f * range;
            float b2 = maxPatchSize - 31.0f / 32.0f * range;
            float b3 = maxPatchSize - 127.0f / 128.0f * range;
            return new Vector4(b0, b1, b2, b3);
        }

        // Function that applies a profile to the scattering color to make the edition range linear
        static internal Color RemapScatteringColor(Color scatteringColor)
        {
            float h, s, v;
            Color.RGBToHSV(scatteringColor, out h, out s, out v);
            v *= v;
            return Color.HSVToRGB(h, s, v);
        }

        // Function that returns a mip offset (for caustics) based on the simulation resolution
        static internal int EvaluateNormalMipOffset(WaterSimulationResolution resolution)
        {
            switch (resolution)
            {
                case WaterSimulationResolution.High256:
                    return 2;
                case WaterSimulationResolution.Medium128:
                    return 1;
                case WaterSimulationResolution.Low64:
                    return 0;
            }
            return 0;
        }

        static internal uint EvaluateNumberWaterPatches(uint numLOD)
        {
            switch (numLOD)
            {
                case 1:
                    return 1;
                case 2:
                    return 9;
                case 3:
                    return 25;
                case 4:
                    return 49;
            }
            return 1;
        }

        uint4 ShiftUInt(uint4 val, int numBits)
        {
            return new uint4(val.x >> 16, val.y >> 16, val.z >> 16, val.w >> 16);
        }

        uint4 WaterHashFunctionUInt4(uint3 coord)
        {
            uint4 x = coord.xyzz;
            x = (ShiftUInt(x, 16) ^ x.yzxy) * 0x45d9f3bu;
            x = (ShiftUInt(x, 16) ^ x.yzxz) * 0x45d9f3bu;
            x = (ShiftUInt(x, 16) ^ x.yzxx) * 0x45d9f3bu;
            return x;
        }

        float4 WaterHashFunctionFloat4(uint3 p)
        {
            uint4 hashed = WaterHashFunctionUInt4(p);
            return new float4(hashed.x, hashed.y, hashed.z, hashed.w) / (float)0xffffffffU;
        }
    }
}
