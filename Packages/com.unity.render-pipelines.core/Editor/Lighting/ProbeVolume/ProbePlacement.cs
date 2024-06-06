#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using UnityEngine.Profiling;
using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    using Brick = ProbeBrickIndex.Brick;

    class ProbePlacement
    {
        const int k_MaxDistanceFieldTextureSize = 128;
        const int k_MaxSubdivisionInSubCell = 4; // Levels strictly higher than this can't be generated directly
        // The UAV binding index 4 isn't in use when we bake the probes and doesn't crash unity.
        const int k_RandomWriteBindingIndex = 4;

        [GenerateHLSL(needAccessors = false)]
        struct GPUProbeVolumeOBB
        {
            public Vector3 corner;
            public Vector3 X;
            public Vector3 Y;
            public Vector3 Z;

            public int minControllerSubdivLevel;
            public int maxControllerSubdivLevel;
            public int fillEmptySpaces;
            public int maxSubdivLevelInsideVolume;
        }

        public class GPUSubdivisionContext : IDisposable
        {
            public int maxSubdivisionLevel;
            public int maxBrickCountPerAxis;
            public int maxSubdivisionLevelInSubCell;
            public int maxBrickCountPerAxisInSubCell;

            public RenderTexture sceneSDF;
            public RenderTexture sceneSDF2;
            public RenderTexture dummyRenderTarget;

            public ComputeBuffer probeVolumesBuffer;
            public ComputeBuffer brickCountBuffer;
            public ComputeBuffer[] bricksBuffers;

            public Vector4[] brickPositions;

            public GPUSubdivisionContext(int probeVolumeCount, ProbeVolumeProfileInfo profile)
            {
                // Find the maximum subdivision level we can have in this cell (avoid extra work if not needed)
                maxSubdivisionLevel = profile.simplificationLevels;
                maxBrickCountPerAxis = profile.cellSizeInBricks; // cells are always cube

                // jump flooding algorithm works best with POT textures
                int sceneSDFSize = Mathf.NextPowerOfTwo(maxBrickCountPerAxis);
                // Limit the max resolution of the texture to avoid out of memory, for bigger cells, we split them into sub-cells for distance field computation.
                sceneSDFSize = Mathf.Clamp(sceneSDFSize, 64, k_MaxDistanceFieldTextureSize);

                RenderTextureDescriptor distanceFieldTextureDescriptor = new RenderTextureDescriptor
                {
                    height = sceneSDFSize,
                    width = sceneSDFSize,
                    volumeDepth = sceneSDFSize,
                    enableRandomWrite = true,
                    dimension = TextureDimension.Tex3D,
                    graphicsFormat = Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat, // we need 16 bit precision for the distance field
                    msaaSamples = 1,
                };

                sceneSDF = RenderTexture.GetTemporary(distanceFieldTextureDescriptor);
                sceneSDF.name = "Scene SDF";
                sceneSDF.Create();
                sceneSDF2 = RenderTexture.GetTemporary(distanceFieldTextureDescriptor);
                // We need mipmaps for the second map to store the probe volume min and max subdivision
                sceneSDF2.useMipMap = true;
                sceneSDF2.autoGenerateMips = false;
                sceneSDF2.name = "Scene SDF Double Buffer";
                sceneSDF2.Create();

                // Dummy render texture to bind during the voxelization of meshes
                dummyRenderTarget = RenderTexture.GetTemporary(sceneSDFSize, sceneSDFSize, 0, GraphicsFormat.R8_SNorm);

                int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPUProbeVolumeOBB));
                probeVolumesBuffer = new ComputeBuffer(probeVolumeCount, stride, ComputeBufferType.Structured);

                // Allocate one bricks buffer per subdivision level
                maxSubdivisionLevelInSubCell = Mathf.Min(maxSubdivisionLevel, k_MaxSubdivisionInSubCell);
                maxBrickCountPerAxisInSubCell = ProbeReferenceVolume.CellSize(maxSubdivisionLevelInSubCell);
                bricksBuffers = new ComputeBuffer[maxSubdivisionLevelInSubCell + 1];
                for (int i = 0; i <= maxSubdivisionLevelInSubCell; i++)
                {
                    int brickCountPerAxis = maxBrickCountPerAxisInSubCell / ProbeReferenceVolume.CellSize(i);
                    bricksBuffers[i] = new ComputeBuffer(brickCountPerAxis * brickCountPerAxis * brickCountPerAxis, sizeof(float) * 4, ComputeBufferType.Structured);
                }

                // Allocate a readback buffer
                brickCountBuffer = new ComputeBuffer(maxSubdivisionLevelInSubCell + 1, sizeof(uint), ComputeBufferType.Structured);

                brickPositions = new Vector4[maxBrickCountPerAxisInSubCell * maxBrickCountPerAxisInSubCell * maxBrickCountPerAxisInSubCell];
            }

            public void Dispose()
            {
                RenderTexture.ReleaseTemporary(sceneSDF);
                RenderTexture.ReleaseTemporary(sceneSDF2);
                RenderTexture.ReleaseTemporary(dummyRenderTarget);
                probeVolumesBuffer.Release();
                brickCountBuffer.Release();

                for (int i = 0; i <= maxSubdivisionLevelInSubCell; i++)
                    bricksBuffers[i].Release();
            }
        }

        static readonly int _BricksToClear = Shader.PropertyToID("_BricksToClear");
        static readonly int _BricksToClearCount = Shader.PropertyToID("_BricksToClearCount");
        static readonly int _BrickCountBuffer = Shader.PropertyToID("_BrickCountBuffer");
        static readonly int _Output = Shader.PropertyToID("_Output");
        static readonly int _OutputSize = Shader.PropertyToID("_OutputSize");
        static readonly int _VolumeWorldOffset = Shader.PropertyToID("_VolumeWorldOffset");
        static readonly int _VolumeSize = Shader.PropertyToID("_VolumeSize");
        static readonly int _AxisSwizzle = Shader.PropertyToID("_AxisSwizzle");
        static readonly int _TreePrototypeTransform = Shader.PropertyToID("_TreePrototypeTransform");
        static readonly int _TreeInstanceToWorld = Shader.PropertyToID("_TreeInstanceToWorld");
        static readonly int _Size = Shader.PropertyToID("_Size");
        static readonly int _Input = Shader.PropertyToID("_Input");
        static readonly int _Offset = Shader.PropertyToID("_Offset");
        static readonly int _ProbeVolumes = Shader.PropertyToID("_ProbeVolumes");
        static readonly int _ProbeVolumeCount = Shader.PropertyToID("_ProbeVolumeCount");
        static readonly int _MaxBrickCount = Shader.PropertyToID("_MaxBrickCount");
        static readonly int _VolumeOffsetInBricks = Shader.PropertyToID("_VolumeOffsetInBricks");
        static readonly int _Bricks = Shader.PropertyToID("_Bricks");
        static readonly int _SubdivisionLevel = Shader.PropertyToID("_SubdivisionLevel");
        static readonly int _MaxSubdivisionLevel = Shader.PropertyToID("_MaxSubdivisionLevel");
        static readonly int _VolumeSizeInBricks = Shader.PropertyToID("_VolumeSizeInBricks");
        static readonly int _SDFSize = Shader.PropertyToID("_SDFSize");
        static readonly int _ProbeVolumeData = Shader.PropertyToID("_ProbeVolumeData");
        static readonly int _BrickSize = Shader.PropertyToID("_BrickSize");
        static readonly int _ClearValue = Shader.PropertyToID("_ClearValue");

        static int s_ClearBufferKernel;
        static int s_ClearKernel;
        static int s_JumpFloodingKernel;
        static int s_FillUVKernel;
        static int s_FinalPassKernel;
        static int s_VoxelizeProbeVolumesKernel;
        static int s_SubdivideKernel;

        static ComputeShader _subdivideSceneCS;
        static ComputeShader subdivideSceneCS
        {
            get
            {
                if (_subdivideSceneCS == null)
                {
                    _subdivideSceneCS = GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeBakingResources>().subdivideSceneCS;

                    s_ClearBufferKernel = subdivideSceneCS.FindKernel("ClearBuffer");
                    s_ClearKernel = subdivideSceneCS.FindKernel("Clear");
                    s_JumpFloodingKernel = subdivideSceneCS.FindKernel("JumpFlooding");
                    s_FillUVKernel = subdivideSceneCS.FindKernel("FillUVMap");
                    s_FinalPassKernel = subdivideSceneCS.FindKernel("FinalPass");
                    s_VoxelizeProbeVolumesKernel = subdivideSceneCS.FindKernel("VoxelizeProbeVolumeData");
                    s_SubdivideKernel = subdivideSceneCS.FindKernel("Subdivide");
                }
                return _subdivideSceneCS;
            }
        }

        static Material _voxelizeMaterial;
        static Material voxelizeMaterial
        {
            get
            {
                if (_voxelizeMaterial == null)
                    _voxelizeMaterial = new Material(GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeBakingResources>().voxelizeSceneShader);

                return _voxelizeMaterial;
            }
        }

        public static GPUSubdivisionContext AllocateGPUResources(int probeVolumeCount, ProbeVolumeProfileInfo profile) => new GPUSubdivisionContext(probeVolumeCount, profile);

        static IEnumerable<(Bounds bounds, Vector3 parentPosition)> SubdivideVolumeIntoSubVolume(GPUSubdivisionContext ctx, Bounds bounds)
        {
            float subdivisionCount = ctx.maxBrickCountPerAxis / (float)ctx.maxBrickCountPerAxisInSubCell;
            var subVolumeSize = bounds.size / subdivisionCount;

            for (int x = 0; x < (int)subdivisionCount; x++)
            {
                for (int y = 0; y < (int)subdivisionCount; y++)
                    for (int z = 0; z < (int)subdivisionCount; z++)
                    {
                        var center = bounds.min + new Vector3((x + 0.5f) * subVolumeSize.x, (y + 0.5f) * subVolumeSize.y, (z + 0.5f) * subVolumeSize.z);
                        Bounds subBounds = new Bounds(center, subVolumeSize);
                        var parentCellPosition = new Vector3(x, y, z);

                        yield return (subBounds, parentCellPosition);
                    }
            }
        }

        public static Brick[] SubdivideCell(Vector3Int cellPosition, Bounds cellBounds, ProbeSubdivisionContext subdivisionCtx, GPUSubdivisionContext ctx, GIContributors contributors, List<(ProbeVolume component, ProbeReferenceVolume.Volume volume, Bounds bounds)> probeVolumes)
        {
            Brick[] finalBricks;
            HashSet<Brick> brickSet = new HashSet<Brick>();

            Profiler.BeginSample($"Subdivide Cell {cellBounds.center}");
            {
                // If the cell is too big so we split it into smaller cells and bake each one separately
                if (ctx.maxBrickCountPerAxis > k_MaxDistanceFieldTextureSize)
                {
                    foreach (var subVolume in SubdivideVolumeIntoSubVolume(ctx, cellBounds))
                    {
                        // redo the renderers and probe volume culling to avoid unnecessary work
                        // Calculate overlaping probe volumes to avoid unnecessary work
                        var overlappingProbeVolumes = new List<(ProbeVolume component, ProbeReferenceVolume.Volume volume, Bounds bounds)>();
                        foreach (var probeVolume in probeVolumes)
                        {
                            if (ProbeVolumePositioning.OBBAABBIntersect(probeVolume.volume, subVolume.bounds, probeVolume.bounds))
                                overlappingProbeVolumes.Add(probeVolume);
                        }

                        var filteredContributors = contributors.Filter(null, subVolume.bounds, overlappingProbeVolumes);

                        if (overlappingProbeVolumes.Count == 0 && filteredContributors.Count == 0)
                            continue;

                         // If any volume wants only subdivisions higher than what can be generated, we will need to filter out some bricks
                        bool requireSubFiltering = overlappingProbeVolumes.Any(p => p.component.overridesSubdivLevels && p.component.lowestSubdivLevelOverride > k_MaxSubdivisionInSubCell);
                        bool fastSubFiltering = overlappingProbeVolumes.All(p => p.component.overridesSubdivLevels && p.component.lowestSubdivLevelOverride > k_MaxSubdivisionInSubCell);
                        bool hasMaxSizedBricks = false;

                        var subBrickSet = new HashSet<Brick>();
                        SubdivideSubCell(subVolume.bounds, subdivisionCtx, ctx, filteredContributors, overlappingProbeVolumes, subBrickSet);
                        if (subBrickSet.Count == 0)
                            continue;

                        // Make sure no bricks with unwanted subdiv level are generated
                        if (requireSubFiltering && !fastSubFiltering)
                        {
                            foreach (var brick in subBrickSet)
                            {
                                float brickSize = ProbeReferenceVolume.instance.BrickSize(brick.subdivisionLevel);
                                Bounds brickBounds = new Bounds();
                                brickBounds.min = subdivisionCtx.profile.probeOffset + (Vector3)brick.position * ProbeReferenceVolume.instance.MinBrickSize();
                                brickBounds.max = brickBounds.min + new Vector3(brickSize, brickSize, brickSize);

                                // If any volume that overlaps this brick wants this subdiv level, we keep it
                                foreach (var p in probeVolumes)
                                {
                                    if (!ProbeVolumePositioning.OBBAABBIntersect(p.volume, brickBounds, p.bounds))
                                        continue;

                                    hasMaxSizedBricks |= brick.subdivisionLevel == k_MaxSubdivisionInSubCell;
                                    (int minSubdiv, int maxSubdiv) = ComputeSubdivRange(ctx.maxSubdivisionLevel, p.component, p.bounds, cellBounds);
                                    if (brick.subdivisionLevel < minSubdiv || brick.subdivisionLevel > maxSubdiv)
                                        continue;

                                    brickSet.Add(brick);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            foreach (var brick in subBrickSet)
                            {
                                hasMaxSizedBricks |= brick.subdivisionLevel == k_MaxSubdivisionInSubCell;
                                if (!requireSubFiltering)
                                    brickSet.Add(brick);
                            }
                        }

                        // In case there is at least one brick in the sub-cell, we need to spawn the parent brick.
                        if (hasMaxSizedBricks)
                        {
                            int cellSizeInBricks = ProbeReferenceVolume.CellSize(ctx.maxSubdivisionLevel);
                            float minBrickSize = subdivisionCtx.profile.minBrickSize;
                            Vector3 cellID = cellBounds.min / minBrickSize;
                            float parentSubdivLevel = 3.0f;
                            for (int i = k_MaxSubdivisionInSubCell; i < ctx.maxSubdivisionLevel; i++)
                            {
                                Vector3 subCellPos = (subVolume.parentPosition / parentSubdivLevel);
                                // Add the sub-cell offset:
                                int brickSize = (int)Mathf.Pow(3, i + 1);
                                Vector3Int subCellPosInt = new Vector3Int(Mathf.FloorToInt(subCellPos.x), Mathf.FloorToInt(subCellPos.y), Mathf.FloorToInt(subCellPos.z)) * brickSize;
                                Vector3Int parentSubCellPos = cellPosition * cellSizeInBricks + subCellPosInt;

                                // Find the corner in bricks of the parent volume:
                                brickSet.Add(new Brick(parentSubCellPos, i + 1));
                                parentSubdivLevel *= 3.0f;
                            }
                        }
                    }
                }
                else
                {
                    SubdivideSubCell(cellBounds, subdivisionCtx, ctx, contributors, probeVolumes, brickSet);
                }

                finalBricks = brickSet.ToArray();

                // TODO: this is really slow :/
                Profiler.BeginSample($"Sort {finalBricks.Length} bricks");
                // sort from larger to smaller bricks
                Array.Sort(finalBricks, (Brick lhs, Brick rhs) =>
                {
                    if (lhs.subdivisionLevel != rhs.subdivisionLevel)
                        return lhs.subdivisionLevel > rhs.subdivisionLevel ? -1 : 1;
                    if (lhs.position.z != rhs.position.z)
                        return lhs.position.z < rhs.position.z ? -1 : 1;
                    if (lhs.position.y != rhs.position.y)
                        return lhs.position.y < rhs.position.y ? -1 : 1;
                    if (lhs.position.x != rhs.position.x)
                        return lhs.position.x < rhs.position.x ? -1 : 1;

                    return 0;
                });
                Profiler.EndSample();
            }
            Profiler.EndSample();

            return finalBricks;
        }

        static void SubdivideSubCell(Bounds cellAABB, ProbeSubdivisionContext subdivisionCtx,
            GPUSubdivisionContext ctx, GIContributors contributors,
            List<(ProbeVolume component, ProbeReferenceVolume.Volume volume, Bounds bounds)> probeVolumes,
            HashSet<Brick> brickSet)
        {
            var firstLayerMask = probeVolumes.First().component.objectLayerMask;
            if (probeVolumes.Count > 1 && probeVolumes.Any(p => p.component.objectLayerMask != firstLayerMask))
            {
                // Pack list of probe volumes per layer mask so we can process multiple of volumes in a single voxelization step
                var probeVolumesPerLayers = new Dictionary<LayerMask, List<(ProbeVolume component, ProbeReferenceVolume.Volume volume, Bounds bounds)>>();

                foreach (var probeVolume in probeVolumes)
                {
                    if (!probeVolumesPerLayers.TryGetValue(probeVolume.component.objectLayerMask, out var probeVolumeList))
                        probeVolumeList = probeVolumesPerLayers[probeVolume.component.objectLayerMask] = new();
                    probeVolumeList.Add(probeVolume);
                }

                foreach (var probeVolumesPerLayer in probeVolumesPerLayers.Values)
                {
                    // re-filter contributors locally for these layers:
                    var contributorsPerLayer = contributors.FilterLayerMaskOnly(probeVolumesPerLayer.First().component.objectLayerMask);
                    // Subdivide the cell using  a list of probe volumes containing the same layer mask
                    SubdivideSubCell(cellAABB, subdivisionCtx, ctx, contributorsPerLayer, probeVolumesPerLayer, brickSet);
                }

                return;
            }

            float minBrickSize = subdivisionCtx.profile.minBrickSize;
            var cellOffset = subdivisionCtx.profile.probeOffset;

            var cmd = CommandBufferPool.Get($"Subdivide (Sub)Cell {cellAABB.center}");

            if (RasterizeGeometry(cmd, cellAABB, ctx, contributors))
            {
                // Only generate the distance field if there was an object rasterized
                GenerateDistanceField(cmd, ctx.sceneSDF, ctx.sceneSDF2);
            }
            else
            {
                // When the is no geometry, instead of computing the distance field, we clear it with a big value.
                using (new ProfilingScope(cmd, new ProfilingSampler("Clear")))
                {
                    cmd.SetComputeTextureParam(subdivideSceneCS, s_ClearKernel, _Output, ctx.sceneSDF);
                    cmd.SetComputeVectorParam(subdivideSceneCS, _Size, new Vector3(ctx.sceneSDF.width, ctx.sceneSDF.height, ctx.sceneSDF.volumeDepth));
                    cmd.SetComputeFloatParam(subdivideSceneCS, _ClearValue, 1000);
                    DispatchCompute(cmd, s_ClearKernel, ctx.sceneSDF.width, ctx.sceneSDF.height, ctx.sceneSDF.volumeDepth);
                }
            }

            // Now that the distance field is generated, we can store the probe subdivision data inside sceneSDF2
            var probeSubdivisionData = ctx.sceneSDF2;
            (int cellMinSubdiv, int cellMaxSubdiv) = VoxelizeProbeVolumeData(cmd, cellAABB, probeVolumes, ctx);

            // Clear the brick counter, equivalent to SetBufferCounterValue(0) but we can't use append buffers
            cmd.SetBufferData(ctx.brickCountBuffer, new int[ctx.maxSubdivisionLevelInSubCell + 1]);

            for (int subdivisionLevel = cellMinSubdiv; subdivisionLevel <= cellMaxSubdiv; subdivisionLevel++)
            {
                int brickCountPerAxis = ctx.maxBrickCountPerAxisInSubCell / ProbeReferenceVolume.CellSize(subdivisionLevel);
                var bricksBuffer = ctx.bricksBuffers[subdivisionLevel];

                using (new ProfilingScope(cmd, new ProfilingSampler("Clear Bricks Buffer")))
                {
                    cmd.SetComputeBufferParam(subdivideSceneCS, s_ClearBufferKernel, _BricksToClear, bricksBuffer);
                    int count = brickCountPerAxis * brickCountPerAxis * brickCountPerAxis;
                    cmd.SetComputeIntParam(subdivideSceneCS, _BricksToClearCount, count);
                    DispatchCompute(cmd, s_ClearBufferKernel, count, 1);
                }

                // Generate the list of bricks on the GPU
                SubdivideFromDistanceField(cmd, cellAABB, ctx, probeSubdivisionData, bricksBuffer, ctx.brickCountBuffer, brickCountPerAxis, subdivisionLevel, minBrickSize, cellOffset);

                // Capture locally the subdivision level to use it inside the lambda
                int localSubdivLevel = subdivisionLevel;
                cmd.RequestAsyncReadback(ctx.brickCountBuffer, sizeof(int), subdivisionLevel * sizeof(int), (data) => {
                    int readbackBrickCount = data.GetData<int>()[0];

                    if (readbackBrickCount > 0)
                    {
                        bricksBuffer.GetData(ctx.brickPositions, 0, 0, readbackBrickCount);
                        for (int i = 0; i < readbackBrickCount; i++)
                        {
                            var pos = ctx.brickPositions[i];
                            var brick = new Brick(new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z)), localSubdivLevel);
                            brickSet.Add(brick);
                        }
                    }
                });
            }

            // ExternalGPUProfiler.BeginGPUCapture();
            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            // ExternalGPUProfiler.EndGPUCapture();
        }

        static bool RasterizeGeometry(CommandBuffer cmd, Bounds cellAABB, GPUSubdivisionContext ctx, GIContributors contributors)
        {
            var props = new MaterialPropertyBlock();
            bool hasGeometry = contributors.Count > 0;

            // Setup voxelize material properties
            voxelizeMaterial.SetVector(_OutputSize, new Vector3(ctx.sceneSDF.width, ctx.sceneSDF.height, ctx.sceneSDF.volumeDepth));
            voxelizeMaterial.SetVector(_VolumeWorldOffset, cellAABB.center - cellAABB.extents);
            voxelizeMaterial.SetVector(_VolumeSize, cellAABB.size);

            if (hasGeometry)
            {
                using (new ProfilingScope(cmd, new ProfilingSampler("Clear")))
                {
                    cmd.SetComputeTextureParam(subdivideSceneCS, s_ClearKernel, _Output, ctx.sceneSDF);
                    cmd.SetComputeVectorParam(subdivideSceneCS, _Size, new Vector3(ctx.sceneSDF.width, ctx.sceneSDF.height, ctx.sceneSDF.volumeDepth));
                    cmd.SetComputeFloatParam(subdivideSceneCS, _ClearValue, 0);
                    DispatchCompute(cmd, s_ClearKernel, ctx.sceneSDF.width, ctx.sceneSDF.height, ctx.sceneSDF.volumeDepth);
                }
            }

            cmd.SetRandomWriteTarget(k_RandomWriteBindingIndex, ctx.sceneSDF);

            // We need to bind at least something for rendering
            cmd.SetRenderTarget(ctx.dummyRenderTarget);
            cmd.SetViewport(new Rect(0, 0, ctx.dummyRenderTarget.width, ctx.dummyRenderTarget.height));

            if (contributors.renderers.Count > 0)
            {
                using (new ProfilingScope(cmd, new ProfilingSampler("Rasterize Meshes 3D")))
                {
                    foreach (var kp in contributors.renderers)
                    {
                        // Only mesh renderers are supported for this voxelization pass.
                        var renderer = kp.component as MeshRenderer;

                        if (renderer == null || !cellAABB.Intersects(renderer.bounds)) // Not sure AABB check is useful
                            continue;
                        if (!renderer.TryGetComponent<MeshFilter>(out var meshFilter) || meshFilter.sharedMesh == null)
                            continue;
                        var matrix = renderer.transform.localToWorldMatrix;
                        for (int submesh = 0; submesh < meshFilter.sharedMesh.subMeshCount; submesh++)
                        {
                            props.SetInt(_AxisSwizzle, 0);
                            cmd.DrawMesh(meshFilter.sharedMesh, matrix, voxelizeMaterial, submesh, shaderPass: 1, props);
                            props.SetInt(_AxisSwizzle, 1);
                            cmd.DrawMesh(meshFilter.sharedMesh, matrix, voxelizeMaterial, submesh, shaderPass: 1, props);
                            props.SetInt(_AxisSwizzle, 2);
                            cmd.DrawMesh(meshFilter.sharedMesh, matrix, voxelizeMaterial, submesh, shaderPass: 1, props);
                        }
                    }
                }
            }

            if (contributors.terrains.Count > 0)
            {
                using (new ProfilingScope(cmd, new ProfilingSampler("Rasterize Terrains")))
                {
                    foreach (var kp in contributors.terrains)
                    {
                        var terrain = kp.component;
                        var terrainData = terrain.terrainData;
                        // Terrains can't be rotated or scaled
                        var transform = Matrix4x4.Translate(terrain.GetPosition());

                        props.SetTexture("_TerrainHeightmapTexture", terrainData.heightmapTexture);
                        props.SetTexture("_TerrainHolesTexture", terrainData.holesTexture);
                        props.SetVector("_TerrainSize", terrainData.size);
                        props.SetFloat("_TerrainHeightmapResolution", terrainData.heightmapResolution);

                        int terrainTileCount = terrainData.heightmapResolution * terrainData.heightmapResolution;
                        props.SetInt(_AxisSwizzle, 0);
                        cmd.DrawProcedural(transform, voxelizeMaterial, shaderPass: 0, MeshTopology.Quads, 4 * terrainTileCount, 1, props);
                        props.SetInt(_AxisSwizzle, 1);
                        cmd.DrawProcedural(transform, voxelizeMaterial, shaderPass: 0, MeshTopology.Quads, 4 * terrainTileCount, 1, props);
                        props.SetInt(_AxisSwizzle, 2);
                        cmd.DrawProcedural(transform, voxelizeMaterial, shaderPass: 0, MeshTopology.Quads, 4 * terrainTileCount, 1, props);

                        foreach (var prototype in kp.treePrototypes)
                        {
                            if (prototype.component == null || prototype.instances.Count == 0)
                                continue;
                            if (!prototype.component.TryGetComponent<MeshFilter>(out var meshFilter) || meshFilter.sharedMesh == null)
                                continue;

                            var mesh = meshFilter.sharedMesh;
                            // Max buffer size is 64KB, matrix is 64B, so limit to 1000 trees per prototype per cell, which should be fine
                            var matrices = new Matrix4x4[Mathf.Min(prototype.instances.Count, 1000)];
                            for (int i = 0; i < matrices.Length; i++)
                                matrices[i] = prototype.instances[i].transform;

                            props.SetMatrix(_TreePrototypeTransform, prototype.transform);
                            props.SetMatrixArray(_TreeInstanceToWorld, matrices);

                            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                            {
                                props.SetInt(_AxisSwizzle, 0);
                                cmd.DrawMeshInstancedProcedural(mesh, submesh, voxelizeMaterial, 2, matrices.Length, props);
                                props.SetInt(_AxisSwizzle, 1);
                                cmd.DrawMeshInstancedProcedural(mesh, submesh, voxelizeMaterial, 2, matrices.Length, props);
                                props.SetInt(_AxisSwizzle, 2);
                                cmd.DrawMeshInstancedProcedural(mesh, submesh, voxelizeMaterial, 2, matrices.Length, props);
                            }
                        }
                    }
                }
            }

            cmd.ClearRandomWriteTargets();

            return hasGeometry;
        }

        static void DispatchCompute(CommandBuffer cmd, int kernel, int width, int height, int depth = 1)
        {
            // If any issue occur on mac / intel GPU devices regarding the probe subdivision, it's likely to be
            // the GetKernelThreadGroupSizes returning wrong values.
            subdivideSceneCS.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);
            cmd.DispatchCompute(
                subdivideSceneCS,
                kernel,
                Mathf.Max(1, Mathf.CeilToInt(width / (float)x)),
                Mathf.Max(1, Mathf.CeilToInt(height / (float)y)),
                Mathf.Max(1, Mathf.CeilToInt(depth / (float)z)));
        }

        static void CopyTexture(CommandBuffer cmd, RenderTexture source, RenderTexture destination)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Copy")))
            {
                for (int i = 0; i < source.volumeDepth; i++)
                    cmd.CopyTexture(source, i, 0, destination, i, 0);
            }
        }

        static void GenerateDistanceField(CommandBuffer cmd, RenderTexture sceneSDF1, RenderTexture sceneSDF2)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("GenerateDistanceField")))
            {
                // Generate distance field with JFA
                cmd.SetComputeVectorParam(subdivideSceneCS, _Size, new Vector4(sceneSDF1.width, 1.0f / sceneSDF1.width));

                // We need those copies because there is a compute barrier bug only happening on low-resolution textures
                CopyTexture(cmd, sceneSDF1, sceneSDF2);

                // Jump flooding implementation based on https://www.comp.nus.edu.sg/~tants/jfa.html
                using (new ProfilingScope(cmd, new ProfilingSampler("JumpFlooding")))
                {
                    cmd.SetComputeTextureParam(subdivideSceneCS, s_FillUVKernel, _Input, sceneSDF2);
                    cmd.SetComputeTextureParam(subdivideSceneCS, s_FillUVKernel, _Output, sceneSDF1);
                    DispatchCompute(cmd, s_FillUVKernel, sceneSDF1.width, sceneSDF1.height, sceneSDF1.volumeDepth);

                    int maxLevels = (int)Mathf.Log(sceneSDF1.width, 2);
                    for (int i = 0; i <= maxLevels; i++)
                    {
                        float offset = 1 << (maxLevels - i);
                        cmd.SetComputeFloatParam(subdivideSceneCS, _Offset, offset);
                        cmd.SetComputeTextureParam(subdivideSceneCS, s_JumpFloodingKernel, _Input, sceneSDF1);
                        cmd.SetComputeTextureParam(subdivideSceneCS, s_JumpFloodingKernel, _Output, sceneSDF2);
                        DispatchCompute(cmd, s_JumpFloodingKernel, sceneSDF1.width, sceneSDF1.height, sceneSDF1.volumeDepth);

                        CopyTexture(cmd, sceneSDF2, sceneSDF1);
                    }
                }
                CopyTexture(cmd, sceneSDF2, sceneSDF1);

                cmd.SetComputeTextureParam(subdivideSceneCS, s_FinalPassKernel, _Input, sceneSDF2);
                cmd.SetComputeTextureParam(subdivideSceneCS, s_FinalPassKernel, _Output, sceneSDF1);
                DispatchCompute(cmd, s_FinalPassKernel, sceneSDF1.width, sceneSDF1.height, sceneSDF1.volumeDepth);
            }
        }

        static (int, int) ComputeSubdivRange(int maxSubdivLevel, ProbeVolume pv, Bounds aabb, Bounds cellAABB)
        {
            pv.GetSubdivisionOverride(maxSubdivLevel, out int minSubdiv, out int maxSubdiv);

            // Constrain the probe volume AABB inside the cell
            aabb.min = Vector3.Max(aabb.min, cellAABB.min);
            aabb.max = Vector3.Min(aabb.max, cellAABB.max);

            // Compute the max size of a brick that can fit in the biggest dimension of a probe volume
            // Avoids generating bricks bigger than the volume unless requested
            int volumeMax = ProbeVolumeBakingSet.MaxSubdivLevelInProbeVolume(aabb.size, maxSubdivLevel);
            volumeMax = Mathf.Max(volumeMax, minSubdiv);
            maxSubdiv = Mathf.Min(maxSubdiv, volumeMax);

            return (minSubdiv, maxSubdiv);
        }

        static (int, int) VoxelizeProbeVolumeData(CommandBuffer cmd, Bounds cellAABB,
            List<(ProbeVolume component, ProbeReferenceVolume.Volume volume, Bounds bounds)> probeVolumes,
            GPUSubdivisionContext ctx)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Voxelize Probe Volume Data")))
            {
                List<GPUProbeVolumeOBB> gpuProbeVolumes = new List<GPUProbeVolumeOBB>();
                int cellMinSubdiv = ctx.maxSubdivisionLevelInSubCell;
                int cellMaxSubdiv = 0;

                // Prepare list of GPU probe volumes
                foreach (var kp in probeVolumes)
                {
                    (int minSubdiv, int maxSubdiv) = ComputeSubdivRange(ctx.maxSubdivisionLevelInSubCell, kp.component, kp.bounds, cellAABB);

                    gpuProbeVolumes.Add(new GPUProbeVolumeOBB
                    {
                        corner = kp.volume.corner,
                        X = kp.volume.X,
                        Y = kp.volume.Y,
                        Z = kp.volume.Z,
                        minControllerSubdivLevel = minSubdiv,
                        maxControllerSubdivLevel = maxSubdiv,
                        fillEmptySpaces = kp.component.fillEmptySpaces ? 1 : 0,
                        maxSubdivLevelInsideVolume = 0, // unused
                    });

                    // Record global range
                    cellMinSubdiv = Mathf.Min(cellMinSubdiv, minSubdiv);
                    cellMaxSubdiv = Mathf.Max(cellMaxSubdiv, maxSubdiv);
                }

                cmd.SetBufferData(ctx.probeVolumesBuffer, gpuProbeVolumes);
                cmd.SetComputeBufferParam(subdivideSceneCS, s_VoxelizeProbeVolumesKernel, _ProbeVolumes, ctx.probeVolumesBuffer);
                cmd.SetComputeFloatParam(subdivideSceneCS, _ProbeVolumeCount, probeVolumes.Count);
                cmd.SetComputeVectorParam(subdivideSceneCS, _VolumeWorldOffset, cellAABB.center - cellAABB.extents);
                cmd.SetComputeVectorParam(subdivideSceneCS, _MaxBrickCount, Vector3.one * ctx.maxBrickCountPerAxisInSubCell);

                for (int i = cellMinSubdiv; i <= cellMaxSubdiv; i++)
                {
                    int brickCountPerAxis = ctx.maxBrickCountPerAxisInSubCell / ProbeReferenceVolume.CellSize(i);
                    cmd.SetComputeFloatParam(subdivideSceneCS, _SubdivisionLevel, i);
                    cmd.SetComputeFloatParam(subdivideSceneCS, _BrickSize, cellAABB.size.x / brickCountPerAxis);
                    cmd.SetComputeTextureParam(subdivideSceneCS, s_VoxelizeProbeVolumesKernel, _Output, ctx.sceneSDF2, i);
                    DispatchCompute(cmd, s_VoxelizeProbeVolumesKernel, brickCountPerAxis, brickCountPerAxis, brickCountPerAxis);
                }

                return (cellMinSubdiv, cellMaxSubdiv);
            }
        }

        static void SubdivideFromDistanceField(
            CommandBuffer cmd, Bounds volume, GPUSubdivisionContext ctx, RenderTexture probeVolumeData,
            ComputeBuffer buffer, ComputeBuffer brickCountBuffer, int brickCount, int subdivisionLevel, float minBrickSize, Vector3 cellOffset)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler($"Subdivide Bricks at level {Mathf.Log(brickCount, 3)}")))
            {
                // We convert the world space volume position (of a corner) in bricks.
                // This is necessary to have correct brick position (the position calculated in the compute shader needs to be in number of bricks from the reference volume (origin)).
                Vector3 volumeBrickPosition = (volume.center - volume.extents - cellOffset) / minBrickSize;
                cmd.SetComputeVectorParam(subdivideSceneCS, _VolumeOffsetInBricks, volumeBrickPosition);
                cmd.SetComputeBufferParam(subdivideSceneCS, s_SubdivideKernel, _Bricks, buffer);
                cmd.SetComputeBufferParam(subdivideSceneCS, s_SubdivideKernel, _BrickCountBuffer, brickCountBuffer);
                cmd.SetComputeVectorParam(subdivideSceneCS, _MaxBrickCount, Vector3.one * brickCount);
                cmd.SetComputeFloatParam(subdivideSceneCS, _SubdivisionLevel, subdivisionLevel);
                cmd.SetComputeFloatParam(subdivideSceneCS, _MaxSubdivisionLevel, ctx.maxSubdivisionLevelInSubCell);
                cmd.SetComputeVectorParam(subdivideSceneCS, _VolumeSizeInBricks, Vector3.one * ctx.maxBrickCountPerAxisInSubCell);
                cmd.SetComputeVectorParam(subdivideSceneCS, _SDFSize, new Vector3(ctx.sceneSDF.width, ctx.sceneSDF.height, ctx.sceneSDF.volumeDepth));
                cmd.SetComputeTextureParam(subdivideSceneCS, s_SubdivideKernel, _Input, ctx.sceneSDF);
                cmd.SetComputeTextureParam(subdivideSceneCS, s_SubdivideKernel, _ProbeVolumeData, probeVolumeData);
                DispatchCompute(cmd, s_SubdivideKernel, brickCount, brickCount, brickCount);
            }
        }
    }
}

#endif
