using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class WaterSystem
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
            return EvaluateBandCount(surfaceType, ripplesOn && evaluateRipplesCPU);
        }

        static float EvaluateCausticsMaxLOD(WaterSurface.WaterCausticsResolution resolution)
        {
            switch (resolution)
            {
                case WaterSurface.WaterCausticsResolution.Caustics256:
                    return 2.0f;
                case WaterSurface.WaterCausticsResolution.Caustics512:
                    return 3.0f;
                case WaterSurface.WaterCausticsResolution.Caustics1024:
                    return 4.0f;
            }
            return 2.0f;
        }

        static internal int SanitizeCausticsBand(int band, int bandCount)
        {
            return Mathf.Min(band, bandCount - 1);
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

        static void SetupWaterShaderKeyword(CommandBuffer cmd, bool decalWorkflow, int bandCount, bool localCurrent)
        {
            CoreUtils.SetKeyword(cmd, "WATER_DECAL_PARTIAL", !decalWorkflow);
            CoreUtils.SetKeyword(cmd, "WATER_DECAL_COMPLETE", decalWorkflow);

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

        static void DrawInstancedQuads(CommandBuffer cmd, WaterRenderingData parameters, ref WaterSurfaceGBufferData surfaceData, int passIndex, int lowResPassIndex)
        {
            var cb = parameters.sharedPerCameraDataArray[surfaceData.surfaceIndex];

            bool drawCentralPatch = true;
            bool drawInfinitePatch = surfaceData.drawInfiniteMesh;
            if (!surfaceData.infinite)
            {
                drawCentralPatch = all(abs(cb._PatchOffset) < cb._RegionExtent + cb._GridSize * 0.5f);

                if (surfaceData.drawInfiniteMesh)
                    drawInfinitePatch = all(abs(cb._PatchOffset) < abs(cb._RegionExtent - cb._GridSize * 0.5f));
            }

            // Draw everything beyond distance fade with a single flat mesh
            if (drawInfinitePatch)
                cmd.DrawMesh(parameters.ringMeshLow, Matrix4x4.identity, surfaceData.waterMaterial, 0, lowResPassIndex, surfaceData.mpb);

            // Draw high res grid under the camera
            if (drawCentralPatch)
                cmd.DrawMesh(parameters.tessellableMesh, Matrix4x4.identity, surfaceData.waterMaterial, 0, passIndex, surfaceData.mpb);

            // Draw the remaining patches
            if (cb._MaxLOD > 0)
            {
                int patchEvaluation = surfaceData.infinite ? parameters.patchEvaluationInfinite : parameters.patchEvaluation;

                // Make sure both constant buffers are properly injected
                BindPerSurfaceConstantBuffer(cmd, parameters.waterSimulation, parameters.perSurfaceCB[surfaceData.surfaceIndex]);

                // Prepare the indirect parameters
                cmd.SetComputeConstantBufferParam(parameters.waterSimulation, HDShaderIDs._ShaderVariablesWaterPerCamera, parameters.perCameraCB, 0, parameters.perCameraCB.stride);
                cmd.SetComputeBufferParam(parameters.waterSimulation, patchEvaluation, HDShaderIDs._WaterPatchDataRW, parameters.patchDataBuffer);
                cmd.SetComputeBufferParam(parameters.waterSimulation, patchEvaluation, HDShaderIDs._WaterInstanceDataRW, parameters.indirectBuffer);
                cmd.DispatchCompute(parameters.waterSimulation, patchEvaluation, 1, 1, 1);

                // Draw all the patches
                cmd.DrawMeshInstancedIndirect(parameters.ringMesh, 0, surfaceData.waterMaterial, passIndex, parameters.indirectBuffer, 0, surfaceData.mpb);
            }
        }

        static void DrawMeshRenderers(CommandBuffer cmd, ref WaterSurfaceGBufferData surfaceData, int passIndex)
        {
            int numMeshRenderers = surfaceData.meshRenderers.Count;
            for (int meshRenderer = 0; meshRenderer < numMeshRenderers; ++meshRenderer)
            {
                MeshRenderer currentRenderer = surfaceData.meshRenderers[meshRenderer];
                if (currentRenderer != null && currentRenderer.TryGetComponent(out MeshFilter filter))
                {
                    Mesh mesh = filter.sharedMesh;
                    int numSubMeshes = mesh.subMeshCount;
                    for (int subMeshIdx = 0; subMeshIdx < numSubMeshes; ++subMeshIdx)
                        cmd.DrawMesh(mesh, currentRenderer.transform.localToWorldMatrix, surfaceData.waterMaterial, subMeshIdx, passIndex, surfaceData.mpb);
                }
            }
        }

        static void DrawWaterSurface(CommandBuffer cmd, string[] passNames, WaterRenderingData parameters, ref WaterSurfaceGBufferData surfaceData)
        {
            int lowResPassIndex = 0;
            bool missingPass = !FindPassIndex(surfaceData.waterMaterial, passNames[0], out var passIndex);
            if (surfaceData.instancedQuads)
                missingPass |= !FindPassIndex(surfaceData.waterMaterial, passNames[1], out lowResPassIndex);
            if (missingPass)
                return;

            surfaceData.mpb.SetConstantBuffer(HDShaderIDs._ShaderVariablesWaterPerCamera, parameters.perCameraCB, 0, parameters.perCameraCB.stride);

            if (surfaceData.instancedQuads)
            {
                DrawInstancedQuads(cmd, parameters, ref surfaceData, passIndex, lowResPassIndex);
            }
            else
            {
                // Based on if this is a custom mesh or not trigger the right geometry/geometries and shader pass
                if (!surfaceData.customMesh)
                {
                    cmd.DrawMesh(parameters.tessellableMesh, Matrix4x4.identity, surfaceData.waterMaterial, 0, passIndex, surfaceData.mpb);
                }
                else
                {
                    DrawMeshRenderers(cmd, ref surfaceData, passIndex);
                }
            }
        }
    }
}
