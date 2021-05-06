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
    using Flags = ProbeReferenceVolume.BrickFlags;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    class ProbePlacement
    {
        [GenerateHLSL]
        struct GPUProbeVolumeOBB
        {
            public Vector3 corner;
            public Vector3 X;
            public Vector3 Y;
            public Vector3 Z;

            public int minSubdivisionLevel;
            public int maxSubdivisionLevel;
        }

        public class GPUSubdivisionContext : IDisposable
        {
            public int maxSubdivisionLevel;
            public int maxBrickCountPerAxis;

            public RenderTexture sceneSDF;
            public RenderTexture sceneSDF2;
            public RenderTexture dummyRenderTarget;

            // TODO: allocate one buffer for each subdivision level to avoid blocking GPU execution while reading data back
            public ComputeBuffer probeVolumesBuffer;
            public ComputeBuffer[] bricksBuffers;
            public ComputeBuffer[] readbackCountBuffers;

            public Vector3[] brickPositions;

            public GPUSubdivisionContext(int probeVolumeCount)
            {
                // Find the maximum subdivision level we can have in this cell (avoid extra work if not needed)
                maxSubdivisionLevel = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1; // remove 1 because the last subdiv level is the cell size
                maxBrickCountPerAxis = (int)Mathf.Pow(3, maxSubdivisionLevel); // cells are always cube

                // jump flooding algorithm works best with POT textures
                int sceneSDFSize = Mathf.NextPowerOfTwo(maxBrickCountPerAxis);
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
                bricksBuffers = new ComputeBuffer[maxSubdivisionLevel + 1];
                readbackCountBuffers = new ComputeBuffer[maxSubdivisionLevel + 1];
                for (int i = 0; i <= maxSubdivisionLevel; i++)
                {
                    int brickCountPerAxis = (int)Mathf.Pow(3, maxSubdivisionLevel - i);
                    bricksBuffers[i] = new ComputeBuffer(brickCountPerAxis * brickCountPerAxis * brickCountPerAxis, sizeof(float) * 3, ComputeBufferType.Append);
                    readbackCountBuffers[i] = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
                }

                brickPositions = new Vector3[maxBrickCountPerAxis * maxBrickCountPerAxis * maxBrickCountPerAxis];
            }

            public void Dispose()
            {
                RenderTexture.ReleaseTemporary(sceneSDF);
                RenderTexture.ReleaseTemporary(sceneSDF2);
                RenderTexture.ReleaseTemporary(dummyRenderTarget);
                probeVolumesBuffer.Release();

                for (int i = 0; i <= maxSubdivisionLevel; i++)
                {
                    bricksBuffers[i].Release();
                    readbackCountBuffers[i].Release();
                }
            }
        }

        static ComputeShader _subdivideSceneCS;
        static ComputeShader subdivideSceneCS
        {
            get
            {
                if (_subdivideSceneCS == null)
                    _subdivideSceneCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/ProbeVolumeSubdivide.compute");
                return _subdivideSceneCS;
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

        public static GPUSubdivisionContext AllocateGPUResources(int probeVolumeCount) => new GPUSubdivisionContext(probeVolumeCount);

        // TODO: split this function
        public static List<Brick> SubdivideWithSDF(ProbeReferenceVolume.Volume cellVolume, ProbeReferenceVolume refVol, GPUSubdivisionContext ctx, List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers, List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes)
        {
            List<Brick> finalBricks = new List<Brick>();
            HashSet<Brick> bricksSet = new HashSet<Brick>();
            var cellAABB = cellVolume.CalculateAABB();

            cellVolume.CalculateCenterAndSize(out var center, out var _);
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler($"Subdivide Cell {center}")))
            {
                RastersizeMeshes(cmd, cellVolume, ctx.sceneSDF, ctx.dummyRenderTarget, ctx.maxBrickCountPerAxis, renderers);

                GenerateDistanceField(cmd, ctx.sceneSDF, ctx.sceneSDF2);

                // Now that the distance field is generated, we can store the probe subdivision data inside sceneSDF2
                var probeSubdivisionData = ctx.sceneSDF2;
                VoxelizeProbeVolumeData(cmd, cellAABB, probeVolumes, ctx.sceneSDF2, ctx.probeVolumesBuffer, ctx.maxBrickCountPerAxis, ctx.maxSubdivisionLevel);

                // // TODO: try to remove this fence and execute all the subdivision in one go
                // Graphics.ExecuteCommandBuffer(cmd);

                List<AsyncGPUReadbackRequest> brickReadbackRequests = new List<AsyncGPUReadbackRequest>();

                // Find the maximum subdivision level we can have in this cell (avoid extra work if not needed)
                int startSubdivisionLevel = ctx.maxSubdivisionLevel - (refVol.GetMaxSubdivision(probeVolumes.Max(p => p.component.maxSubdivisionMultiplier)) - 1);
                for (int subdivisionLevel = startSubdivisionLevel; subdivisionLevel <= ctx.maxSubdivisionLevel; subdivisionLevel++)
                {
                    // Add the bricks from the probe volume min subdivision level:
                    int brickCountPerAxis = (int)Mathf.Pow(3, ctx.maxSubdivisionLevel - subdivisionLevel);
                    int clearBufferKernel = subdivideSceneCS.FindKernel("ClearBuffer");
                    var bricksBuffer = ctx.bricksBuffers[subdivisionLevel];
                    var brickCountReadbackBuffer = ctx.readbackCountBuffers[subdivisionLevel];

                    // cmd.Clear();
                    using (new ProfilingScope(cmd, new ProfilingSampler("Clear Bricks Buffer")))
                    {
                        cmd.SetComputeBufferParam(subdivideSceneCS, clearBufferKernel, "_BricksToClear", bricksBuffer);
                        DispatchCompute(cmd, clearBufferKernel, brickCountPerAxis * brickCountPerAxis * brickCountPerAxis, 1);
                        cmd.SetBufferCounterValue(bricksBuffer, 0);
                    }

                    SubdivideFromDistanceField(cmd, cellAABB, ctx.sceneSDF, probeSubdivisionData, bricksBuffer, brickCountPerAxis, ctx.maxBrickCountPerAxis, subdivisionLevel, ctx.maxSubdivisionLevel);
                    cmd.CopyCounterValue(bricksBuffer, brickCountReadbackBuffer, 0);
                    // Graphics.ExecuteCommandBuffer(cmd);
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
                                bricksSet.Add(brick);
                            }
                        }
                    });
                }

                cmd.WaitAllAsyncReadbackRequests();
                Graphics.ExecuteCommandBuffer(cmd);

                finalBricks = bricksSet.ToList();

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

            return finalBricks;
        }

        static void RastersizeMeshes(CommandBuffer cmd, ProbeReferenceVolume.Volume cellVolume, RenderTexture sceneSDF, RenderTexture dummyRenderTarget, int maxBrickCountPerAxis, List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Rasterize Meshes 3D")))
            {
                var cellAABB = cellVolume.CalculateAABB();

                using (new ProfilingScope(cmd, new ProfilingSampler("Clear")))
                {
                    int clearKernel = subdivideSceneCS.FindKernel("Clear");
                    cmd.SetComputeTextureParam(subdivideSceneCS, clearKernel, "_Output", sceneSDF);
                    DispatchCompute(cmd, clearKernel, sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth);
                }

                // Hum, will this cause binding issues for other systems?
                cmd.SetRandomWriteTarget(4, sceneSDF);

                var mat = new Material(Shader.Find("Hidden/ProbeVolume/VoxelizeScene"));
                mat.SetVector("_OutputSize", new Vector3(sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth));
                mat.SetVector("_VolumeWorldOffset", cellAABB.center - cellAABB.extents);
                mat.SetVector("_VolumeSize", cellAABB.size);

                var topMatrix = GetCameraMatrixForAngle(Quaternion.Euler(90, 0, 0));
                var rightMatrix = GetCameraMatrixForAngle(Quaternion.Euler(0, 90, 0));
                var forwardMatrix = GetCameraMatrixForAngle(Quaternion.Euler(0, 0, 90));

                Matrix4x4 GetCameraMatrixForAngle(Quaternion rotation)
                {
                    cellVolume.CalculateCenterAndSize(out var center, out var size);
                    Vector3 cameraSize = new Vector3(sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth) / 2.0f;
                    cameraSize = size / 2;
                    var worldToCamera = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
                    var projection = Matrix4x4.Ortho(-cameraSize.x, cameraSize.x, -cameraSize.y, cameraSize.y, 0, cameraSize.z * 2);
                    return Matrix4x4.Rotate(Quaternion.Euler((Time.realtimeSinceStartup * 10f) % 360, 0, 0));
                }

                // We need to bind at least something for rendering
                cmd.SetRenderTarget(dummyRenderTarget);
                cmd.SetViewport(new Rect(0, 0, dummyRenderTarget.width, dummyRenderTarget.height));
                var props = new MaterialPropertyBlock();
                foreach (var kp in renderers)
                {
                    // Only mesh renderers are supported for the voxelization.
                    var renderer = kp.component as MeshRenderer;

                    if (renderer == null)
                        continue;

                    if (cellAABB.Intersects(renderer.bounds))
                    {
                        if (renderer.TryGetComponent<MeshFilter>(out var meshFilter))
                        {
                            props.SetInt("_AxisSwizzle", 0);
                            cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, mat, 0, shaderPass: 0, props);
                            props.SetInt("_AxisSwizzle", 1);
                            cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, mat, 0, shaderPass: 0, props);
                            props.SetInt("_AxisSwizzle", 2);
                            cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, mat, 0, shaderPass: 0, props);
                        }
                    }
                }

                cmd.ClearRandomWriteTargets();
            }
        }

        static void DispatchCompute(CommandBuffer cmd, int kernel, int width, int height, int depth = 1)
        {
            subdivideSceneCS.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);
            cmd.DispatchCompute(
                subdivideSceneCS,
                kernel,
                Mathf.Max(1, Mathf.CeilToInt(width / (float)x)),
                Mathf.Max(1, Mathf.CeilToInt(height / (float)y)),
                Mathf.Max(1, Mathf.CeilToInt(depth / (float)z)));
        }

        static void GenerateDistanceField(CommandBuffer cmd, RenderTexture sceneSDF1, RenderTexture sceneSDF2)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("GenerateDistanceField")))
            {
                // Generate distance field with JFA
                cmd.SetComputeVectorParam(subdivideSceneCS, "_Size", new Vector4(sceneSDF1.width, 1.0f / sceneSDF1.width));

                int clearKernel = subdivideSceneCS.FindKernel("Clear");
                int jumpFloodingKernel = subdivideSceneCS.FindKernel("JumpFlooding");
                int fillUVKernel = subdivideSceneCS.FindKernel("FillUVMap");
                int finalPassKernel = subdivideSceneCS.FindKernel("FinalPass");

                // TODO: try to get rid of the copies again
                using (new ProfilingScope(cmd, new ProfilingSampler("Copy")))
                {
                    for (int i = 0; i < sceneSDF1.volumeDepth; i++)
                        cmd.CopyTexture(sceneSDF1, i, 0, sceneSDF2, i, 0);
                }
                // Swap(ref sceneSDF1, ref sceneSDF2);

                // Jump flooding implementation based on https://www.comp.nus.edu.sg/~tants/jfa.html
                using (new ProfilingScope(cmd, new ProfilingSampler("JumpFlooding")))
                {
                    cmd.SetComputeTextureParam(subdivideSceneCS, fillUVKernel, "_Input", sceneSDF2);
                    cmd.SetComputeTextureParam(subdivideSceneCS, fillUVKernel, "_Output", sceneSDF1);
                    DispatchCompute(cmd, fillUVKernel, sceneSDF1.width, sceneSDF1.height, sceneSDF1.volumeDepth);

                    int maxLevels = (int)Mathf.Log(sceneSDF1.width, 2);
                    for (int i = 0; i <= maxLevels; i++)
                    {
                        float offset = 1 << (maxLevels - i);
                        cmd.SetComputeFloatParam(subdivideSceneCS, "_Offset", offset);
                        cmd.SetComputeTextureParam(subdivideSceneCS, jumpFloodingKernel, "_Input", sceneSDF1);
                        cmd.SetComputeTextureParam(subdivideSceneCS, jumpFloodingKernel, "_Output", sceneSDF2);
                        DispatchCompute(cmd, jumpFloodingKernel, sceneSDF1.width, sceneSDF1.height, sceneSDF1.volumeDepth);

                        Swap(ref sceneSDF1, ref sceneSDF2);
                        // using (new ProfilingScope(cmd, new ProfilingSampler("Copy")))
                        // {
                        //     for (int j = 0; j < sceneSDF1.volumeDepth; j++)
                        //         cmd.CopyTexture(sceneSDF2, j, 0, sceneSDF1, j, 0);
                        // }
                    }
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Copy")))
                {
                    for (int j = 0; j < sceneSDF1.volumeDepth; j++)
                        cmd.CopyTexture(sceneSDF1, j, 0, sceneSDF2, j, 0);
                }
                void Swap(ref RenderTexture s1, ref RenderTexture s2)
                {
                    var tmp = s1;
                    s1 = s2;
                    s2 = tmp;
                }

                cmd.SetComputeTextureParam(subdivideSceneCS, finalPassKernel, "_Input", sceneSDF2);
                cmd.SetComputeTextureParam(subdivideSceneCS, finalPassKernel, "_Output", sceneSDF1);
                DispatchCompute(cmd, finalPassKernel, sceneSDF1.width, sceneSDF1.height, sceneSDF1.volumeDepth);
            }
        }

        static void VoxelizeProbeVolumeData(CommandBuffer cmd, Bounds cellAABB,
            List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes,
            RenderTexture target, ComputeBuffer probeVolumesBuffer, int maxBrickCountPerAxis, int maxSubdivLevel)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Voxelize Probe Volume Data")))
            {
                List<GPUProbeVolumeOBB> gpuProbeVolumes = new List<GPUProbeVolumeOBB>();

                // Prepare list of GPU probe volumes
                foreach (var kp in probeVolumes)
                {
                    int minSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision(kp.component.minSubdivisionMultiplier);
                    int maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision(kp.component.maxSubdivisionMultiplier);
                    gpuProbeVolumes.Add(new GPUProbeVolumeOBB{
                        corner = kp.volume.corner,
                        X = kp.volume.X,
                        Y = kp.volume.Y,
                        Z = kp.volume.Z,
                        minSubdivisionLevel = minSubdiv,
                        maxSubdivisionLevel = maxSubdiv,
                    });
                }

                cmd.SetBufferData(probeVolumesBuffer, gpuProbeVolumes);
                int kernel = subdivideSceneCS.FindKernel("VoxelizeProbeVolumeData");
                cmd.SetComputeBufferParam(subdivideSceneCS, kernel, "_ProbeVolumes", probeVolumesBuffer);
                cmd.SetComputeFloatParam(subdivideSceneCS, "_ProbeVolumeCount", probeVolumes.Count);
                cmd.SetComputeVectorParam(subdivideSceneCS, "_VolumeWorldOffset", cellAABB.center - cellAABB.extents);
                cmd.SetComputeVectorParam(subdivideSceneCS, "_MaxBrickSize", Vector3.one * maxBrickCountPerAxis);

                int subdivisionLevelCount = (int)Mathf.Log(maxBrickCountPerAxis, 3);
                for (int i = 0; i <= subdivisionLevelCount; i++)
                {
                    int brickCountPerAxis = (int)Mathf.Pow(3, maxSubdivLevel - i);
                    cmd.SetComputeFloatParam(subdivideSceneCS, "_BrickSize", cellAABB.size.x / brickCountPerAxis);
                    cmd.SetComputeTextureParam(subdivideSceneCS, kernel, "_Output", target, i);
                    DispatchCompute(cmd, kernel, brickCountPerAxis, brickCountPerAxis, brickCountPerAxis);
                }
            }
        }

        static void SubdivideFromDistanceField(CommandBuffer cmd, Bounds volume, RenderTexture sceneSDF, RenderTexture probeVolumeData, ComputeBuffer buffer, int brickCount, int maxBrickCountPerAxis, int subdivisionLevel, int maxSubdivisionLevel)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler($"Subdivide Bricks at level {Mathf.Log(brickCount, 3)}")))
            {
                // TODO: cleanup: cache kernel names + cache shader ids
                int kernel = subdivideSceneCS.FindKernel("Subdivide");

                // We convert the world space volume position (of a corner) in bricks.
                // This is necessary to have correct brick position (the position calculated in the compute shader needs to be in number of bricks from the reference volume (origin)).
                Vector3 volumeBrickPosition = (volume.center - volume.extents) / ProbeReferenceVolume.instance.MinBrickSize();
                cmd.SetComputeVectorParam(subdivideSceneCS, "_VolumeOffsetInBricks", volumeBrickPosition);
                cmd.SetComputeBufferParam(subdivideSceneCS, kernel, "_Bricks", buffer);
                cmd.SetComputeVectorParam(subdivideSceneCS, "_MaxBrickSize", Vector3.one * brickCount);
                cmd.SetComputeFloatParam(subdivideSceneCS, "_SubdivisionLevel", subdivisionLevel);
                cmd.SetComputeFloatParam(subdivideSceneCS, "_MaxSubdivisionLevel", maxSubdivisionLevel);
                cmd.SetComputeVectorParam(subdivideSceneCS, "_VolumeSizeInBricks", Vector3.one * maxBrickCountPerAxis);
                cmd.SetComputeVectorParam(subdivideSceneCS, "_SDFSize", new Vector3(sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth));
                cmd.SetComputeTextureParam(subdivideSceneCS, kernel, "_Input", sceneSDF);
                cmd.SetComputeTextureParam(subdivideSceneCS, kernel, "_ProbeVolumeData", probeVolumeData);
                DispatchCompute(cmd, kernel, brickCount, brickCount, brickCount);
            }
        }
    }
}

#endif
