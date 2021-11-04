#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Linq;
using UnityEngine.Profiling;
using System;

namespace UnityEngine.Experimental.Rendering
{
    using Brick = ProbeBrickIndex.Brick;

    class ProbePlacement
    {
        const int k_MaxDistanceFieldTextureSize = 128;
        const int k_MaxSubdivisionInSubCell = 4;
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
            public int maxSubdivLevelInsideVolume;
            public float geometryDistanceOffset;
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
            public ComputeBuffer[] bricksBuffers;
            public ComputeBuffer[] readbackCountBuffers;

            public Vector3[] brickPositions;

            public GPUSubdivisionContext(int probeVolumeCount, int maxSubdivisionLevelFromAsset)
            {
                // Find the maximum subdivision level we can have in this cell (avoid extra work if not needed)
                this.maxSubdivisionLevel = maxSubdivisionLevelFromAsset - 1; // remove 1 because the last subdiv level is the cell size
                maxBrickCountPerAxis = (int)Mathf.Pow(3, maxSubdivisionLevel); // cells are always cube

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

                // Allocate one readback and bricks buffer per subdivision level
                maxSubdivisionLevelInSubCell = Mathf.Min(maxSubdivisionLevel, k_MaxSubdivisionInSubCell);
                maxBrickCountPerAxisInSubCell = (int)Mathf.Pow(3, maxSubdivisionLevelInSubCell);
                bricksBuffers = new ComputeBuffer[maxSubdivisionLevelInSubCell + 1];
                readbackCountBuffers = new ComputeBuffer[maxSubdivisionLevelInSubCell + 1];
                for (int i = 0; i <= maxSubdivisionLevelInSubCell; i++)
                {
                    int brickCountPerAxis = (int)Mathf.Pow(3, maxSubdivisionLevelInSubCell - i);
                    bricksBuffers[i] = new ComputeBuffer(brickCountPerAxis * brickCountPerAxis * brickCountPerAxis, sizeof(float) * 3, ComputeBufferType.Append);
                    readbackCountBuffers[i] = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
                }

                brickPositions = new Vector3[maxBrickCountPerAxisInSubCell * maxBrickCountPerAxisInSubCell * maxBrickCountPerAxisInSubCell];
            }

            public void Dispose()
            {
                RenderTexture.ReleaseTemporary(sceneSDF);
                RenderTexture.ReleaseTemporary(sceneSDF2);
                RenderTexture.ReleaseTemporary(dummyRenderTarget);
                probeVolumesBuffer.Release();

                for (int i = 0; i <= maxSubdivisionLevelInSubCell; i++)
                {
                    bricksBuffers[i].Release();
                    readbackCountBuffers[i].Release();
                }
            }
        }

        static readonly int _BricksToClear = Shader.PropertyToID("_BricksToClear");
        static readonly int _Output = Shader.PropertyToID("_Output");
        static readonly int _OutputSize = Shader.PropertyToID("_OutputSize");
        static readonly int _VolumeWorldOffset = Shader.PropertyToID("_VolumeWorldOffset");
        static readonly int _VolumeSize = Shader.PropertyToID("_VolumeSize");
        static readonly int _AxisSwizzle = Shader.PropertyToID("_AxisSwizzle");
        static readonly int _Size = Shader.PropertyToID("_Size");
        static readonly int _Input = Shader.PropertyToID("_Input");
        static readonly int _Offset = Shader.PropertyToID("_Offset");
        static readonly int _ProbeVolumes = Shader.PropertyToID("_ProbeVolumes");
        static readonly int _ProbeVolumeCount = Shader.PropertyToID("_ProbeVolumeCount");
        static readonly int _MaxBrickSize = Shader.PropertyToID("_MaxBrickSize");
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
                    _subdivideSceneCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/ProbeVolumeSubdivide.compute");
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
                    _voxelizeMaterial = new Material(Shader.Find("Hidden/ProbeVolume/VoxelizeScene"));
                return _voxelizeMaterial;
            }
        }

        static public ProbeReferenceVolume.Volume ToVolume(Bounds bounds)
        {
            ProbeReferenceVolume.Volume v = new ProbeReferenceVolume.Volume();
            v.corner = bounds.center - bounds.size * 0.5f;
            v.X = new Vector3(bounds.size.x, 0, 0);
            v.Y = new Vector3(0, bounds.size.y, 0);
            v.Z = new Vector3(0, 0, bounds.size.z);
            return v;
        }

        public static GPUSubdivisionContext AllocateGPUResources(int probeVolumeCount, int maxSubdivisionLevel) => new GPUSubdivisionContext(probeVolumeCount, maxSubdivisionLevel);

        static IEnumerable<(ProbeReferenceVolume.Volume volume, Vector3 parentPosition)> SubdivideVolumeIntoSubVolume(GPUSubdivisionContext ctx, ProbeReferenceVolume.Volume volume)
        {
            volume.CalculateCenterAndSize(out var center, out var size);
            float maxBrickInSubCell = Mathf.Pow(3, k_MaxSubdivisionInSubCell);
            float subdivisionCount = ctx.maxBrickCountPerAxis / (float)ctx.maxBrickCountPerAxisInSubCell;
            var subVolumeSize = size / subdivisionCount;

            for (int x = 0; x < (int)subdivisionCount; x++)
            {
                for (int y = 0; y < (int)subdivisionCount; y++)
                    for (int z = 0; z < (int)subdivisionCount; z++)
                    {
                        var subVolume = new ProbeReferenceVolume.Volume()
                        {
                            corner = volume.corner + new Vector3(x * subVolumeSize.x, y * subVolumeSize.y, z * subVolumeSize.z),
                            X = volume.X / subdivisionCount,
                            Y = volume.Y / subdivisionCount,
                            Z = volume.Z / subdivisionCount,
                            maxSubdivisionMultiplier = volume.maxSubdivisionMultiplier,
                            minSubdivisionMultiplier = volume.minSubdivisionMultiplier,
                        };
                        var parentCellPosition = new Vector3(x, y, z);

                        yield return (subVolume, parentCellPosition);
                    }
            }
        }

        public static List<Brick> SubdivideCell(ProbeReferenceVolume.Volume cellVolume, ProbeSubdivisionContext subdivisionCtx, GPUSubdivisionContext ctx, List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers, List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes)
        {
            List<Brick> finalBricks = new List<Brick>();
            HashSet<Brick> brickSet = new HashSet<Brick>();
            cellVolume.CalculateCenterAndSize(out var center, out var _);
            var cellAABB = cellVolume.CalculateAABB();

            Profiler.BeginSample($"Subdivide Cell {center}");
            {
                // If the cell is too big so we split it into smaller cells and bake each one separately
                if (ctx.maxBrickCountPerAxis > k_MaxDistanceFieldTextureSize)
                {
                    foreach (var subVolume in SubdivideVolumeIntoSubVolume(ctx, cellVolume))
                    {
                        // redo the renderers and probe volume culling to avoid unnecessary work
                        // Calculate overlaping probe volumes to avoid unnecessary work
                        var overlappingProbeVolumes = new List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)>();
                        foreach (var probeVolume in probeVolumes)
                        {
                            if (ProbeVolumePositioning.OBBIntersect(probeVolume.volume, subVolume.volume))
                                overlappingProbeVolumes.Add(probeVolume);
                        }

                        // Calculate valid renderers to avoid unnecessary work (a renderer needs to overlap a probe volume and match the layer)
                        var overlappingRenderers = new List<(Renderer component, ProbeReferenceVolume.Volume volume)>();
                        foreach (var renderer in renderers)
                        {
                            foreach (var probeVolume in overlappingProbeVolumes)
                            {
                                if (ProbeVolumePositioning.OBBIntersect(renderer.volume, probeVolume.volume)
                                    && ProbeVolumePositioning.OBBIntersect(renderer.volume, subVolume.volume))
                                    overlappingRenderers.Add(renderer);
                            }
                        }

                        // Calculate overlapping terrains to avoid unnecessary work
                        var overlappingTerrains = new List<(Terrain terrain, ProbeReferenceVolume.Volume volume)>();
                        foreach (var terrain in subdivisionCtx.terrains)
                        {
                            foreach (var probeVolume in overlappingProbeVolumes)
                            {
                                if (ProbeVolumePositioning.OBBIntersect(terrain.volume, probeVolume.volume)
                                    && ProbeVolumePositioning.OBBIntersect(terrain.volume, subVolume.volume))
                                    overlappingTerrains.Add(terrain);
                            }
                        }

                        if (overlappingRenderers.Count == 0 && overlappingProbeVolumes.Count == 0 && overlappingTerrains.Count == 0)
                            continue;

                        int brickCount = brickSet.Count;
                        SubdivideSubCell(subVolume.volume, subdivisionCtx, ctx, overlappingRenderers, overlappingProbeVolumes, overlappingTerrains, brickSet);

                        // In case there is at least one brick in the sub-cell, we need to spawn the parent brick.
                        if (brickCount != brickSet.Count)
                        {
                            float minBrickSize = subdivisionCtx.profile.minBrickSize;
                            Vector3 cellID = (cellAABB.center - cellAABB.extents) / minBrickSize;
                            float parentSubdivLevel = 3.0f;
                            for (int i = k_MaxSubdivisionInSubCell; i < ctx.maxSubdivisionLevel; i++)
                            {
                                Vector3 subCellPos = (subVolume.parentPosition / parentSubdivLevel);
                                // Add the sub-cell offset:
                                int brickSize = (int)Mathf.Pow(3, i + 1);
                                Vector3Int subCellPosInt = new Vector3Int(Mathf.FloorToInt(subCellPos.x), Mathf.FloorToInt(subCellPos.y), Mathf.FloorToInt(subCellPos.z)) * brickSize;
                                Vector3Int parentSubCellPos = new Vector3Int(Mathf.RoundToInt(cellID.x), Mathf.RoundToInt(cellID.y), Mathf.RoundToInt(cellID.z)) + subCellPosInt;

                                if (IsParentBrickInProbeVolume(parentSubCellPos, minBrickSize, brickSize))
                                {
                                    // Find the corner in bricks of the parent volume:
                                    brickSet.Add(new Brick(parentSubCellPos, i + 1));
                                    parentSubdivLevel *= 3.0f;
                                }
                            }
                        }
                    }
                }
                else
                {
                    SubdivideSubCell(cellVolume, subdivisionCtx, ctx, renderers, probeVolumes, subdivisionCtx.terrains, brickSet);
                }

                bool IsParentBrickInProbeVolume(Vector3Int parentSubCellPos, float minBrickSize, int brickSize)
                {
                    Vector3 center = (Vector3)parentSubCellPos * minBrickSize + Vector3.one * brickSize * minBrickSize / 2.0f;
                    Bounds parentAABB = new Bounds(center, Vector3.one * brickSize * minBrickSize);

                    bool generateParentBrick = false;
                    foreach (var probeVolume in probeVolumes)
                    {
                        var pvAABB = probeVolume.volume.CalculateAABB();
                        if (pvAABB.Contains(parentAABB.min) && pvAABB.Contains(parentAABB.max))
                            generateParentBrick = true;
                    }

                    return generateParentBrick;
                }

                finalBricks = brickSet.ToList();

                // TODO: this is really slow :/
                Profiler.BeginSample($"Sort {finalBricks.Count} bricks");
                // sort from larger to smaller bricks
                finalBricks.Sort((Brick lhs, Brick rhs) =>
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

        static void SubdivideSubCell(ProbeReferenceVolume.Volume cellVolume, ProbeSubdivisionContext subdivisionCtx,
            GPUSubdivisionContext ctx, List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers,
            List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes,
            List<(Terrain terrain, ProbeReferenceVolume.Volume volume)> terrains, HashSet<Brick> brickSet)
        {
            var cellAABB = cellVolume.CalculateAABB();
            float minBrickSize = subdivisionCtx.profile.minBrickSize;

            cellVolume.CalculateCenterAndSize(out var center, out var _);
            var cmd = CommandBufferPool.Get($"Subdivide (Sub)Cell {center}");

            if (RastersizeGeometry(cmd, cellVolume, ctx, renderers, terrains))
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
            VoxelizeProbeVolumeData(cmd, cellAABB, probeVolumes, ctx);

            // Find the maximum subdivision level we can have in this cell (avoid extra work if not needed)
            int startSubdivisionLevel = Mathf.Max(0, ctx.maxSubdivisionLevelInSubCell - GetMaxSubdivision(ctx, probeVolumes.Max(p => p.component.GetMaxSubdivMultiplier())));
            for (int subdivisionLevel = startSubdivisionLevel; subdivisionLevel <= ctx.maxSubdivisionLevelInSubCell; subdivisionLevel++)
            {
                // Add the bricks from the probe volume min subdivision level:
                int brickCountPerAxis = (int)Mathf.Pow(3, ctx.maxSubdivisionLevelInSubCell - subdivisionLevel);
                var bricksBuffer = ctx.bricksBuffers[subdivisionLevel];
                var brickCountReadbackBuffer = ctx.readbackCountBuffers[subdivisionLevel];

                using (new ProfilingScope(cmd, new ProfilingSampler("Clear Bricks Buffer")))
                {
                    cmd.SetComputeBufferParam(subdivideSceneCS, s_ClearBufferKernel, _BricksToClear, bricksBuffer);
                    DispatchCompute(cmd, s_ClearBufferKernel, brickCountPerAxis * brickCountPerAxis * brickCountPerAxis, 1);
                    cmd.SetBufferCounterValue(bricksBuffer, 0);
                }

                // Generate the list of bricks on the GPU
                SubdivideFromDistanceField(cmd, cellAABB, ctx, probeSubdivisionData, bricksBuffer, brickCountPerAxis, subdivisionLevel, minBrickSize);

                cmd.CopyCounterValue(bricksBuffer, brickCountReadbackBuffer, 0);
                // Capture locally the subdivision level to use it inside the lambda
                int localSubdivLevel = subdivisionLevel;
                cmd.RequestAsyncReadback(brickCountReadbackBuffer, sizeof(int), 0, (data) => {
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

            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        static bool RastersizeGeometry(CommandBuffer cmd, ProbeReferenceVolume.Volume cellVolume, GPUSubdivisionContext ctx,
            List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers,
            List<(Terrain terrain, ProbeReferenceVolume.Volume volume)> terrains)
        {
            var topMatrix = GetCameraMatrixForAngle(Quaternion.Euler(90, 0, 0));
            var rightMatrix = GetCameraMatrixForAngle(Quaternion.Euler(0, 90, 0));
            var forwardMatrix = GetCameraMatrixForAngle(Quaternion.Euler(0, 0, 90));
            var props = new MaterialPropertyBlock();
            bool hasGeometry = renderers.Count > 0 || terrains.Count > 0;
            var cellAABB = cellVolume.CalculateAABB();

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

            if (renderers.Count > 0)
            {
                using (new ProfilingScope(cmd, new ProfilingSampler("Rasterize Meshes 3D")))
                {
                    foreach (var kp in renderers)
                    {
                        // Only mesh renderers are supported for this voxelization pass.
                        var renderer = kp.component as MeshRenderer;

                        if (renderer == null)
                            continue;

                        if (cellAABB.Intersects(renderer.bounds))
                        {
                            if (renderer.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh != null)
                            {
                                for (int submesh = 0; submesh < meshFilter.sharedMesh.subMeshCount; submesh++)
                                {
                                    props.SetInt(_AxisSwizzle, 0);
                                    cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, voxelizeMaterial, submesh, shaderPass: 0, props);
                                    props.SetInt(_AxisSwizzle, 1);
                                    cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, voxelizeMaterial, submesh, shaderPass: 0, props);
                                    props.SetInt(_AxisSwizzle, 2);
                                    cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, voxelizeMaterial, submesh, shaderPass: 0, props);
                                }
                            }
                        }
                    }
                }
            }

            if (terrains.Count > 0)
            {
                using (new ProfilingScope(cmd, new ProfilingSampler("Rasterize Terrains")))
                {
                    foreach (var kp in terrains)
                    {
                        var terrainData = kp.terrain.terrainData;
                        // Terrains can't be rotated or scaled
                        var transform = Matrix4x4.Translate(kp.terrain.GetPosition());

                        props.SetTexture("_TerrainHeightmapTexture", terrainData.heightmapTexture);
                        props.SetTexture("_TerrainHolesTexture", terrainData.holesTexture);
                        props.SetVector("_TerrainSize", terrainData.size);
                        props.SetFloat("_TerrainHeightmapResolution", terrainData.heightmapResolution);

                        int terrainTileCount = terrainData.heightmapResolution * terrainData.heightmapResolution;
                        props.SetInt(_AxisSwizzle, 0);
                        cmd.DrawProcedural(transform, voxelizeMaterial, shaderPass: 1, MeshTopology.Quads, 4 * terrainTileCount, 1, props);
                        props.SetInt(_AxisSwizzle, 1);
                        cmd.DrawProcedural(transform, voxelizeMaterial, shaderPass: 1, MeshTopology.Quads, 4 * terrainTileCount, 1, props);
                        props.SetInt(_AxisSwizzle, 2);
                        cmd.DrawProcedural(transform, voxelizeMaterial, shaderPass: 1, MeshTopology.Quads, 4 * terrainTileCount, 1, props);
                    }
                }
            }

            Matrix4x4 GetCameraMatrixForAngle(Quaternion rotation)
            {
                cellVolume.CalculateCenterAndSize(out var center, out var size);
                Vector3 cameraSize = new Vector3(ctx.sceneSDF.width, ctx.sceneSDF.height, ctx.sceneSDF.volumeDepth) / 2.0f;
                cameraSize = size / 2;
                var worldToCamera = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
                var projection = Matrix4x4.Ortho(-cameraSize.x, cameraSize.x, -cameraSize.y, cameraSize.y, 0, cameraSize.z * 2);
                return Matrix4x4.Rotate(Quaternion.Euler((Time.realtimeSinceStartup * 10f) % 360, 0, 0));
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

        static int GetMaxSubdivision(GPUSubdivisionContext ctx, float multiplier)
            => Mathf.CeilToInt(ctx.maxSubdivisionLevelInSubCell * multiplier);

        static void VoxelizeProbeVolumeData(CommandBuffer cmd, Bounds cellAABB,
            List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes,
            GPUSubdivisionContext ctx)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Voxelize Probe Volume Data")))
            {
                List<GPUProbeVolumeOBB> gpuProbeVolumes = new List<GPUProbeVolumeOBB>();

                // Prepare list of GPU probe volumes
                foreach (var kp in probeVolumes)
                {
                    int minSubdiv = GetMaxSubdivision(ctx, kp.component.GetMinSubdivMultiplier());
                    int maxSubdiv = GetMaxSubdivision(ctx, kp.component.GetMaxSubdivMultiplier());

                    // Constrain the probe volume AABB inside the cell
                    var pvAABB = kp.volume.CalculateAABB();
                    pvAABB.min = Vector3.Max(pvAABB.min, cellAABB.min);
                    pvAABB.max = Vector3.Min(pvAABB.max, cellAABB.max);

                    // Compute the max size of a brick that can fit in the smallest dimension of a probe volume
                    float minSizedDim = Mathf.Min(pvAABB.size.x, Mathf.Min(pvAABB.size.y, pvAABB.size.z));
                    float minSideInBricks = Mathf.CeilToInt(minSizedDim / ProbeReferenceVolume.instance.MinBrickSize());
                    int absoluteMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
                    minSideInBricks =  Mathf.Max(minSideInBricks, Mathf.Pow(3, absoluteMaxSubdiv - maxSubdiv));
                    int subdivLevel = Mathf.FloorToInt(Mathf.Log(minSideInBricks, 3));
                    gpuProbeVolumes.Add(new GPUProbeVolumeOBB
                    {
                        corner = kp.volume.corner,
                        X = kp.volume.X,
                        Y = kp.volume.Y,
                        Z = kp.volume.Z,
                        minControllerSubdivLevel = minSubdiv,
                        maxControllerSubdivLevel = maxSubdiv,
                        maxSubdivLevelInsideVolume = subdivLevel,
                        geometryDistanceOffset = kp.component.geometryDistanceOffset,
                    });
                }

                cmd.SetBufferData(ctx.probeVolumesBuffer, gpuProbeVolumes);
                cmd.SetComputeBufferParam(subdivideSceneCS, s_VoxelizeProbeVolumesKernel, _ProbeVolumes, ctx.probeVolumesBuffer);
                cmd.SetComputeFloatParam(subdivideSceneCS, _ProbeVolumeCount, probeVolumes.Count);
                cmd.SetComputeVectorParam(subdivideSceneCS, _VolumeWorldOffset, cellAABB.center - cellAABB.extents);
                cmd.SetComputeVectorParam(subdivideSceneCS, _MaxBrickSize, Vector3.one * ctx.maxBrickCountPerAxisInSubCell);

                int subdivisionLevelCount = (int)Mathf.Log(ctx.maxBrickCountPerAxisInSubCell, 3);
                for (int i = 0; i <= subdivisionLevelCount; i++)
                {
                    int brickCountPerAxis = (int)Mathf.Pow(3, ctx.maxSubdivisionLevelInSubCell - i);
                    cmd.SetComputeFloatParam(subdivideSceneCS, _BrickSize, cellAABB.size.x / brickCountPerAxis);
                    cmd.SetComputeTextureParam(subdivideSceneCS, s_VoxelizeProbeVolumesKernel, _Output, ctx.sceneSDF2, i);
                    DispatchCompute(cmd, s_VoxelizeProbeVolumesKernel, brickCountPerAxis, brickCountPerAxis, brickCountPerAxis);
                }
            }
        }

        static void SubdivideFromDistanceField(
            CommandBuffer cmd, Bounds volume, GPUSubdivisionContext ctx, RenderTexture probeVolumeData,
            ComputeBuffer buffer, int brickCount, int subdivisionLevel, float minBrickSize)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler($"Subdivide Bricks at level {Mathf.Log(brickCount, 3)}")))
            {
                // We convert the world space volume position (of a corner) in bricks.
                // This is necessary to have correct brick position (the position calculated in the compute shader needs to be in number of bricks from the reference volume (origin)).
                Vector3 volumeBrickPosition = (volume.center - volume.extents) / minBrickSize;
                cmd.SetComputeVectorParam(subdivideSceneCS, _VolumeOffsetInBricks, volumeBrickPosition);
                cmd.SetComputeBufferParam(subdivideSceneCS, s_SubdivideKernel, _Bricks, buffer);
                cmd.SetComputeVectorParam(subdivideSceneCS, _MaxBrickSize, Vector3.one * brickCount);
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
