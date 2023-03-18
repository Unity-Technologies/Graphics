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

        static internal void BuildGridMeshes(ref Mesh grid, ref Mesh ring, ref Mesh ringLow)
        {
            Vector3[] CreateNormals(int size)
            {
                Vector3[] normals = new Vector3[size];
                for (int i = 0; i < size; ++i)
                    normals[i] = Vector3.up;
                return normals;
            }

            int i, y, ti, vi;

            // Build central grid
            int meshResolution = WaterConsts.k_WaterTessellatedMeshResolution;
            Vector3[] vertices = new Vector3[(meshResolution + 1) * (meshResolution + 1)];
            for (i = 0, y = 0; y <= meshResolution; y++)
            {
                for (int x = 0; x <= meshResolution; x++, i++)
                {
                    vertices[i] = new Vector3(x / (float)meshResolution - 0.5f, 0.0f, y / (float)meshResolution - 0.5f);
                }
            }

            int[] triangles = new int[meshResolution * meshResolution * 6];
            for (ti = 0, vi = 0, y = 0; y < meshResolution; y++, vi++)
            {
                for (int x = 0; x < meshResolution; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = vi + meshResolution + 1;
                    triangles[ti + 5] = vi + meshResolution + 2;
                }
            }

            grid = new Mesh()
            {
                vertices = vertices,
                normals = CreateNormals(vertices.Length),
                triangles = triangles,
            };

            // Build ring mesh with correct junctions
            int resX = meshResolution - meshResolution / 4, resY = meshResolution / 4 - 1;
            int subdivStart = meshResolution / 4, subdivCount = meshResolution;

            int quadCount = 2 * ((meshResolution - meshResolution / 4) * (meshResolution / 4 - 1) + meshResolution / 4);
            int triCount = quadCount + 3 * meshResolution / 2;

            float offsetX = meshResolution / 4, scale = 2.0f / meshResolution;
            vertices = new Vector3[(resY + 1) * (resX + 1) + (subdivStart + subdivCount + 1)];
            for (i = 0, y = 0; y <= resY; y++)
            {
                for (int x = 0; x <= resX; x++, i++)
                {
                    vertices[i] = new Vector3((x - offsetX) * scale - 0.5f, 0.0f, (y - resY - 1) * scale - 0.5f);
                }
            }
            for (int x = 0; x <= subdivStart; x++, i++)
                vertices[i] = new Vector3((x - offsetX) * scale - 0.5f, 0.0f, -0.5f);
            for (int x = 1; x <= subdivCount; x++, i++)
                vertices[i] = new Vector3(x * scale * 0.5f - 0.5f, 0.0f, -0.5f);

            triangles = new int[triCount * 3];
            for (ti = 0, vi = 0, y = 0; y < resY; y++, vi++)
            {
                for (int x = 0; x < resX; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = vi + resX + 1;
                    triangles[ti + 2] = vi + 1;

                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + resX + 1;
                    triangles[ti + 5] = vi + resX + 2;
                }
            }
            for (int x = 0; x < subdivStart; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + resX + 1;
                triangles[ti + 2] = vi + 1;

                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + resX + 1;
                triangles[ti + 5] = vi + resX + 2;
            }
            for (int x = 0; x < subdivCount / 2; x++, ti += 9, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + resX + x + 1;
                triangles[ti + 2] = vi + resX + x + 2;

                triangles[ti + 3] = vi;
                triangles[ti + 4] = vi + resX + x + 2;
                triangles[ti + 5] = vi + 1;

                triangles[ti + 6] = vi + 1;
                triangles[ti + 7] = vi + resX + x + 2;
                triangles[ti + 8] = vi + resX + x + 3;
            }

            ring = new Mesh()
            {
                vertices = vertices,
                normals = CreateNormals(vertices.Length),
                triangles = triangles,
            };

            // Build flat outer ring mesh, no need to handle junctions
            // Although we put the mesh at 0.999 instead of 1 to avoid missing pixels at junction when rasterizing
            float close = 0.999f, far = 1000.0f;
            ringLow = new Mesh();
            ringLow.vertices = new Vector3[] {
                new Vector3(-1f, 0f,  1f) * close,
                new Vector3( 1f, 0f,  1f) * close,
                new Vector3(-1f, 0f, -1f) * close,
                new Vector3( 1f, 0f, -1f) * close,

                new Vector3(-1f, 0f,  1f) * far,
                new Vector3( 1f, 0f,  1f) * far,
                new Vector3(-1f, 0f, -1f) * far,
                new Vector3( 1f, 0f, -1f) * far,
            };
            ringLow.normals = CreateNormals(ringLow.vertexCount);
            ringLow.triangles = new int[] {
                0, 4, 1,  1, 4, 5,
                1, 5, 3,  3, 5, 7,
                2, 3, 7,  2, 7, 6,
                0, 2, 6,  0, 6, 4,
            };
        }

        // Converts an angle to a 2d direction
        static internal float2 OrientationToDirection(float orientation)
        {
            float orientationRad = orientation * Mathf.Deg2Rad;
            float directionX = Mathf.Cos(orientationRad);
            float directionY = Mathf.Sin(orientationRad);
            return new float2(directionX, directionY);
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

        static internal float NormalizeAngle(float degrees)
        {
            float angle = degrees % 360.0f;
            return angle < 0.0 ? angle + 360.0f : angle;
        }

        static internal float EvaluateMaxAmplitude(float patchSize, float windSpeed)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
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

        static void SetupWaterShaderKeyword(CommandBuffer cmd, int bandCount, bool localCurrent)
        {
            CoreUtils.SetKeyword(cmd, "WATER_ONE_BAND", bandCount == 1);
            CoreUtils.SetKeyword(cmd, "WATER_TWO_BANDS", bandCount == 2);
            CoreUtils.SetKeyword(cmd, "WATER_THREE_BANDS", bandCount == 3);
            CoreUtils.SetKeyword(cmd, "WATER_LOCAL_CURRENT", localCurrent);
        }

        static void ResetWaterShaderKeyword(CommandBuffer cmd)
        {
            CoreUtils.SetKeyword(cmd, "WATER_ONE_BAND", false);
            CoreUtils.SetKeyword(cmd, "WATER_TWO_BANDS", false);
            CoreUtils.SetKeyword(cmd, "WATER_THREE_BANDS", false);
            CoreUtils.SetKeyword(cmd, "WATER_LOCAL_CURRENT", false);
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

        // Function that evaluates the bounds of a given patch based on it's index while accounting for deformation
        static void ComputePatchBounds(WaterRenderingParameters parameters, int id, int lod, out float2 center, out float2 size, out float2 rotation)
        {
            float2 _GridSize = new Vector2(parameters.waterRenderingCB._GridSize.x, parameters.waterRenderingCB._GridSize.y);
            float _MaxWaveDisplacement = parameters.waterCB._MaxWaveDisplacement;
            float4 _PatchOffset = parameters.waterRenderingCB._PatchOffset;

            //uint id = (uint)patch % 4;
            //float scale = 1 << (patch >> 2);
            float scale = 1 << lod;

            center = new float2(-0.25f, -0.75f);
            size = new float2(1.5f, 0.5f) * 0.5f;

            rotation = new float2(scale, 0);
            if (id == 1) rotation = new float2(0, -scale);
            if (id == 2) rotation = new float2(-scale, 0);
            if (id == 3) rotation = new float2(0, scale);

            center = center.x * rotation.xy + center.y * new float2(-rotation.y, rotation.x);
            size = size.x * abs(rotation.xy) + size.y * abs(rotation.yx);

            center = center * _GridSize + _PatchOffset.xz;
            size = size * _GridSize + _MaxWaveDisplacement;

            var bounds = new Bounds()
            {
                center = new float3(center.x, 0.0f, center.y),
                extents = new float3(size.x, 1.0f, size.y)
            };
        }

        static void DrawInstancedIndirectCPU(CommandBuffer cmd, WaterRenderingParameters parameters, int passIndex)
        {
            float maxWaveHeight = parameters.waterCB._MaxWaveHeight + parameters.waterRenderingCB._MaxWaterDeformation;
            uint maxLOD = parameters.waterRenderingCB._MaxLOD;
            Vector4 patchOffset = parameters.waterRenderingCB._PatchOffset;
            float2 regionCenter = parameters.waterRenderingCB._RegionCenter;
            float2 regionExtent = parameters.waterRenderingCB._RegionExtent;

            for (int lod = 0; lod < maxLOD; lod++)
            {
                for (int id = 0; id < 4; id++)
                {
                    ComputePatchBounds(parameters, id, lod, out var center, out var size, out var rotation);

                    if (!parameters.infinite && !all(abs(regionCenter - center) < regionExtent + size))
                        continue;

                    // Frustum cull the patch
                    OrientedBBox obb;
                    obb.center = float3(center.x, patchOffset.y, center.y);
                    obb.right = new float3(1, 0, 0);
                    obb.up = new float3(0, 1, 0);
                    obb.extentX = size.x;
                    obb.extentY = maxWaveHeight;
                    obb.extentZ = size.y;

                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                        obb.center -= parameters.cameraPosition;

                    if (!GeometryUtils.Overlap(obb, parameters.cameraFrustum, 6, 8))
                        continue;

                    // Propagate the data to the constant buffer
                    parameters.waterRenderingCB._PatchRotation.Set(rotation.x, rotation.y, 0.0f, 0.0f);
                    ConstantBuffer.Push(cmd, parameters.waterRenderingCB, parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterRendering);

                    // Draw the target patch
                    cmd.DrawMesh(parameters.ringMesh, Matrix4x4.identity, parameters.waterMaterial, 0, passIndex, parameters.mbp);
                }
            }
        }

        static bool FindPassIndex(Material material, string passName, out int passIndex)
        {
            passIndex = material.FindPass(passName);
            #if UNITY_EDITOR
            if (!UnityEditor.ShaderUtil.IsPassCompiled(material, passIndex))
            {
                UnityEditor.ShaderUtil.CompilePass(material, passIndex);
                return false;
            }
            #endif
            return true;
        }

        static void DrawInstancedQuads(CommandBuffer cmd, WaterRenderingParameters parameters, int passIndex, int lowResPassIndex, bool supportIndirectGPU,
            GraphicsBuffer patchDataBuffer, GraphicsBuffer indirectBuffer, GraphicsBuffer cameraFrustumBuffer)
        {
            var cb = parameters.waterRenderingCB;

            bool drawCentralPatch = true;
            bool drawInfinitePatch = parameters.drawInfiniteMesh;
            if (!parameters.infinite)
            {
                float2 offset = cb._RegionCenter - new Vector2(cb._PatchOffset.x, cb._PatchOffset.z);

                drawCentralPatch = (abs(offset.x) < cb._RegionExtent.x + cb._GridSize.x * 0.5f) &&
                                   (abs(offset.y) < cb._RegionExtent.y + cb._GridSize.y * 0.5f);

                if (parameters.drawInfiniteMesh)
                {
                    drawInfinitePatch = abs(offset.x) < abs(cb._RegionExtent.x - cb._GridSize.x * 0.5f) &&
                                        abs(offset.y) < abs(cb._RegionExtent.y - cb._GridSize.y * 0.5f);
                }
            }

            // Draw everything beyond distance fade with a single flat mesh
            if (drawInfinitePatch)
                cmd.DrawMesh(parameters.ringMeshLow, Matrix4x4.identity, parameters.waterMaterial, 0, lowResPassIndex, parameters.mbp);

            // Draw high res grid under the camera
            if (drawCentralPatch)
                cmd.DrawMesh(parameters.tessellableMesh, Matrix4x4.identity, parameters.waterMaterial, 0, passIndex, parameters.mbp);

            if (cb._MaxLOD > 0)
            {
                // Draw the remaining patches
                if (supportIndirectGPU)
                {
                    // Makes both constant buffers are properly injected
                    ConstantBuffer.Set<ShaderVariablesWater>(cmd, parameters.waterSimulation, HDShaderIDs._ShaderVariablesWater);
                    ConstantBuffer.Set<ShaderVariablesWaterRendering>(cmd, parameters.waterSimulation, HDShaderIDs._ShaderVariablesWaterRendering);

                    // Prepare the indirect parameters
                    cmd.SetComputeBufferParam(parameters.waterSimulation, parameters.patchEvaluation, HDShaderIDs._WaterPatchDataRW, patchDataBuffer);
                    cmd.SetComputeBufferParam(parameters.waterSimulation, parameters.patchEvaluation, HDShaderIDs._WaterInstanceDataRW, indirectBuffer);
                    cmd.SetComputeBufferParam(parameters.waterSimulation, parameters.patchEvaluation, HDShaderIDs._FrustumGPUBuffer, cameraFrustumBuffer);
                    cmd.DispatchCompute(parameters.waterSimulation, parameters.patchEvaluation, 1, 1, 1);

                    // Draw all the patches
                    cmd.DrawMeshInstancedIndirect(parameters.ringMesh, 0, parameters.waterMaterial, passIndex, indirectBuffer, 0, parameters.mbp);
                }
                else
                {
                    DrawInstancedIndirectCPU(cmd, parameters, passIndex);
                }
            }
        }

        static void DrawMeshRenderers(CommandBuffer cmd, WaterRenderingParameters parameters, int passIndex)
        {
            int numMeshRenderers = parameters.meshRenderers.Count;
            for (int meshRenderer = 0; meshRenderer < numMeshRenderers; ++meshRenderer)
            {
                MeshRenderer currentRenderer = parameters.meshRenderers[meshRenderer];
                if (currentRenderer != null)
                {
                    // can this be sent to cmd.DrawMesh ? that would avoid having to reupload the constant buffer
                    parameters.waterRenderingCB._WaterCustomMeshTransform = currentRenderer.transform.localToWorldMatrix;
                    parameters.waterRenderingCB._WaterCustomMeshTransform_Inverse = currentRenderer.transform.worldToLocalMatrix;
                    ConstantBuffer.Push(cmd, parameters.waterRenderingCB, parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterRendering);

                    MeshFilter filter;
                    currentRenderer.TryGetComponent(out filter);
                    if (filter != null)
                    {
                        Mesh mesh = filter.sharedMesh;
                        int numSubMeshes = mesh.subMeshCount;
                        for (int subMeshIdx = 0; subMeshIdx < numSubMeshes; ++subMeshIdx)
                            cmd.DrawMesh(mesh, Matrix4x4.identity, parameters.waterMaterial, subMeshIdx, passIndex, parameters.mbp);
                    }
                }
            }
        }

        static void DrawWaterSurface(CommandBuffer cmd, WaterRenderingParameters parameters, string passName, bool supportIndirectGPU,
            GraphicsBuffer patchDataBuffer, GraphicsBuffer indirectBuffer, GraphicsBuffer cameraFrustumBuffer)
        {
            int lowResPassIndex = 0;
            bool missingPass = !FindPassIndex(parameters.waterMaterial, passName, out var passIndex);
            if (parameters.instancedQuads)
                missingPass |= !FindPassIndex(parameters.waterMaterial, indirectBuffer == null ? k_WaterMaskPass : k_LowResGBufferPass, out lowResPassIndex);
            if (missingPass)
                return;


            // Bind the constant buffers
            ConstantBuffer.Set<ShaderVariablesWater>(parameters.waterMaterial, HDShaderIDs._ShaderVariablesWater);
            ConstantBuffer.Set<ShaderVariablesWaterRendering>(parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterRendering);
            ConstantBuffer.Set<ShaderVariablesWaterDeformation>(parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterDeformation);

            if (parameters.instancedQuads)
            {
                DrawInstancedQuads(cmd, parameters, passIndex, lowResPassIndex, supportIndirectGPU, patchDataBuffer, indirectBuffer, cameraFrustumBuffer);
            }
            else
            {
                // Based on if this is a custom mesh or not trigger the right geometry/geometries and shader pass
                if (!parameters.customMesh)
                {
                    cmd.DrawMesh(parameters.tessellableMesh, Matrix4x4.identity, parameters.waterMaterial, 0, passIndex, parameters.mbp);
                }
                else
                {
                    DrawMeshRenderers(cmd, parameters, passIndex);
                }
            }
        }
    }
}
