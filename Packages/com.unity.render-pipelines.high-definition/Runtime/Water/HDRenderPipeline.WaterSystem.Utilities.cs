using Unity.Mathematics;
using static Unity.Mathematics.math;

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
            int meshResolution = WaterConsts.k_WaterTessellatedMeshResolution;
            mesh = new Mesh();
            Vector3[] vertices = new Vector3[(meshResolution + 1) * (meshResolution + 1)];
            for (int i = 0, y = 0; y <= meshResolution; y++)
            {
                for (int x = 0; x <= meshResolution; x++, i++)
                {
                    vertices[i] = new Vector3(x / (float)meshResolution - 0.5f, 0.0f, y / (float)meshResolution - 0.5f);
                }
            }
            mesh.vertices = vertices;

            Vector3[] normals = new Vector3[(meshResolution + 1) * (meshResolution + 1)];
            for (int i = 0, y = 0; y <= meshResolution; y++)
            {
                for (int x = 0; x <= meshResolution; x++, i++)
                {
                    normals[i] = new Vector3(0, 1, 0);
                }
            }
            mesh.normals = normals;

            int[] triangles = new int[meshResolution * meshResolution * 6];
            for (int ti = 0, vi = 0, y = 0; y < meshResolution; y++, vi++)
            {
                for (int x = 0; x < meshResolution; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = vi + meshResolution + 1;
                    triangles[ti + 5] = vi + meshResolution + 2;
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


        static internal float EvaluateSwellSecondPatchSize(float maxPatchSize)
        {
            float relativeSwellPatchSize = (maxPatchSize - WaterConsts.k_SwellMinPatchSize) / (WaterConsts.k_SwellMaxPatchSize - WaterConsts.k_SwellMinPatchSize);
            return Mathf.Lerp(WaterConsts.k_SwellMinRatio, WaterConsts.k_SwellMaxRatio, relativeSwellPatchSize);
        }

        static internal float SampleMaxAmplitudeTable(int2 pixelCoord)
        {
            int clampedX = Mathf.Clamp(pixelCoord.x, 0, WaterConsts.k_TableResolution - 1);
            int clampedY = Mathf.Clamp(pixelCoord.y, 0, WaterConsts.k_TableResolution - 1);
            int tapIndex = clampedX + clampedY * WaterConsts.k_TableResolution;
            return WaterConsts.k_MaximumAmplitudeTable[tapIndex];
        }

        static internal float EvaluateMaxAmplitude(float patchSize, float windSpeed)
        {
            // Convert the position from uv to floating pixel coords (for the bilinear interpolation)
            Vector2 uv = new Vector2(windSpeed / WaterConsts.k_SwellMaximumWindSpeed, Mathf.Clamp((patchSize - 25.0f) / 4975.0f, 0.0f, 1.0f));
            PrepareCoordinates(uv, WaterConsts.k_TableResolution - 1, out int2 pixelCoord, out float2 fract);

            // Evaluate the UV for this sample
            float p0 = SampleMaxAmplitudeTable(pixelCoord);
            float p1 = SampleMaxAmplitudeTable(pixelCoord + new int2(1, 0));
            float p2 = SampleMaxAmplitudeTable(pixelCoord + new int2(0, 1));
            float p3 = SampleMaxAmplitudeTable(pixelCoord + new int2(1, 1));

            // Do the bilinear interpolation
            float i0 = lerp(p0, p1, fract.x);
            float i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
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

        static void SetupWaterShaderKeyword(CommandBuffer cmd, int bandCount)
        {
            if (bandCount == 1)
            {
                CoreUtils.SetKeyword(cmd, "WATER_ONE_BAND", true);
                CoreUtils.SetKeyword(cmd, "WATER_TWO_BANDS", false);
                CoreUtils.SetKeyword(cmd, "WATER_THREE_BANDS", false);
            }
            else if (bandCount == 2)
            {
                CoreUtils.SetKeyword(cmd, "WATER_ONE_BAND", false);
                CoreUtils.SetKeyword(cmd, "WATER_TWO_BANDS", true);
                CoreUtils.SetKeyword(cmd, "WATER_THREE_BANDS", false);
            }
            else
            {
                CoreUtils.SetKeyword(cmd, "WATER_ONE_BAND", false);
                CoreUtils.SetKeyword(cmd, "WATER_TWO_BANDS", false);
                CoreUtils.SetKeyword(cmd, "WATER_THREE_BANDS", true);
            }
        }

        static internal int EvaluateBandCount(WaterSurfaceType surfaceType, bool ripplesOn)
        {
            switch (surfaceType)
            {
                case WaterSurfaceType.OceanSeaLake:
                    return ripplesOn ? 3 : 2;
                case WaterSurfaceType.River:
                    return ripplesOn ? 2 : 1;
                case WaterSurfaceType.Pool:
                    return 1;
            }
            return 1;
        }

        static internal int EvaluateCPUBandCount(WaterSurfaceType surfaceType, bool ripplesOn, bool evaluateRipplesCPU)
        {
            switch (surfaceType)
            {
                case WaterSurfaceType.OceanSeaLake:
                    return evaluateRipplesCPU ? (ripplesOn ? 3 : 2) : 2;
                case WaterSurfaceType.River:
                    return evaluateRipplesCPU ? (ripplesOn ? 1 : 1) : 1;
                case WaterSurfaceType.Pool:
                    return 1;
            }
            return 1;
        }

        internal static readonly float[] sizeMultiplier = new float[] {1.0f, 4.0f, 32.0f, 128.0f};
        internal static readonly float[] offsets = new float[]{0.0f, 0.5f, 4.5f, 36.5f};

        // Function that evaluates the bounds of a given grid based on it's coordinates
        static void ComputeGridBounds(int x, int y, float centerGridSize,
                            out float2 center,
                            out float2 size)
        {
            int absX = abs(x);
            int absY = abs(y);
            float signX = sign(x);
            float signY = sign(y);

            // Size of the patch
            size = float2(centerGridSize * sizeMultiplier[absX], centerGridSize * sizeMultiplier[absY]);

            // Offset position of the patch
            center = float2(signX * (offsets[absX] * centerGridSize + size.x * 0.5f), signY * (offsets[absY] * centerGridSize + size.y * 0.5f));
        }
    }
}
