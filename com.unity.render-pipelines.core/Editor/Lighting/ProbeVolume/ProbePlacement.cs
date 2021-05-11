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
            public float geometryDistanceOffset;
        }

        public class GPUSubdivisionContext : IDisposable
        {
            public int maxSubdivisionLevel;
            public int maxBrickCountPerAxis;

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

        public static List<Brick> SubdivideWithSDF(ProbeReferenceVolume.Volume cellVolume, ProbeSubdivisionContext subdivisionCtx, GPUSubdivisionContext ctx, List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers, List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes)
        {
            List<Brick> finalBricks = new List<Brick>();
            HashSet<Brick> bricksSet = new HashSet<Brick>();
            var cellAABB = cellVolume.CalculateAABB();
            float minBrickSize = subdivisionCtx.refVolume.profile.minBrickSize;
            cellVolume.CalculateCenterAndSize(out var center, out var _);

            Profiler.BeginSample($"Subdivide Cell {center}");
            {
                var cmd = CommandBufferPool.Get($"Subdivide Cell {center}");

                RastersizeMeshes(cmd, cellVolume, ctx.sceneSDF, ctx.dummyRenderTarget, ctx.maxBrickCountPerAxis, renderers);

                GenerateDistanceField(cmd, ctx.sceneSDF, ctx.sceneSDF2);

                // Now that the distance field is generated, we can store the probe subdivision data inside sceneSDF2
                var probeSubdivisionData = ctx.sceneSDF2;
                VoxelizeProbeVolumeData(cmd, cellAABB, probeVolumes, ctx, subdivisionCtx.refVolume);

                // Find the maximum subdivision level we can have in this cell (avoid extra work if not needed)
                int startSubdivisionLevel = ctx.maxSubdivisionLevel - (GetMaxSubdivision(subdivisionCtx.refVolume, probeVolumes.Max(p => p.component.maxSubdivisionMultiplier)) - 1);
                for (int subdivisionLevel = startSubdivisionLevel; subdivisionLevel <= ctx.maxSubdivisionLevel; subdivisionLevel++)
                {
                    // Add the bricks from the probe volume min subdivision level:
                    int brickCountPerAxis = (int)Mathf.Pow(3, ctx.maxSubdivisionLevel - subdivisionLevel);
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
            Profiler.EndSample();

            return finalBricks;
        }

        static void RastersizeMeshes(CommandBuffer cmd, ProbeReferenceVolume.Volume cellVolume, RenderTexture sceneSDF, RenderTexture dummyRenderTarget, int maxBrickCountPerAxis, List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Rasterize Meshes 3D")))
            {
                var cellAABB = cellVolume.CalculateAABB();

                using (new ProfilingScope(cmd, new ProfilingSampler("Clear")))
                {
                    cmd.SetComputeTextureParam(subdivideSceneCS, s_ClearKernel, _Output, sceneSDF);
                    DispatchCompute(cmd, s_ClearKernel, sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth);
                }

                cmd.SetRandomWriteTarget(4, sceneSDF);

                var mat = new Material(Shader.Find("Hidden/ProbeVolume/VoxelizeScene"));
                mat.SetVector(_OutputSize, new Vector3(sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth));
                mat.SetVector(_VolumeWorldOffset, cellAABB.center - cellAABB.extents);
                mat.SetVector(_VolumeSize, cellAABB.size);

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
                        if (renderer.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh != null)
                        {
                            for (int submesh = 0; submesh < meshFilter.sharedMesh.subMeshCount; submesh++)
                            {
                                props.SetInt(_AxisSwizzle, 0);
                                cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, mat, submesh, shaderPass: 0, props);
                                props.SetInt(_AxisSwizzle, 1);
                                cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, mat, submesh, shaderPass: 0, props);
                                props.SetInt(_AxisSwizzle, 2);
                                cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, mat, submesh, shaderPass: 0, props);
                            }
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

                void Swap(ref RenderTexture s1, ref RenderTexture s2)
                {
                    var tmp = s1;
                    s1 = s2;
                    s2 = tmp;
                }

                cmd.SetComputeTextureParam(subdivideSceneCS, s_FinalPassKernel, _Input, sceneSDF2);
                cmd.SetComputeTextureParam(subdivideSceneCS, s_FinalPassKernel, _Output, sceneSDF1);
                DispatchCompute(cmd, s_FinalPassKernel, sceneSDF1.width, sceneSDF1.height, sceneSDF1.volumeDepth);
            }
        }

        static int GetMaxSubdivision(ProbeReferenceVolumeAuthoring refVolAuth, float multiplier)
            => Mathf.CeilToInt(refVolAuth.profile.maxSubdivision * multiplier);

        static void VoxelizeProbeVolumeData(CommandBuffer cmd, Bounds cellAABB,
            List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes,
            GPUSubdivisionContext ctx, ProbeReferenceVolumeAuthoring refVolAuth)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Voxelize Probe Volume Data")))
            {
                List<GPUProbeVolumeOBB> gpuProbeVolumes = new List<GPUProbeVolumeOBB>();

                // Prepare list of GPU probe volumes
                foreach (var kp in probeVolumes)
                {
                    int minSubdiv = GetMaxSubdivision(refVolAuth, kp.component.minSubdivisionMultiplier);
                    int maxSubdiv = GetMaxSubdivision(refVolAuth, kp.component.maxSubdivisionMultiplier);
                    gpuProbeVolumes.Add(new GPUProbeVolumeOBB{
                        corner = kp.volume.corner,
                        X = kp.volume.X,
                        Y = kp.volume.Y,
                        Z = kp.volume.Z,
                        minSubdivisionLevel = minSubdiv,
                        maxSubdivisionLevel = maxSubdiv,
                        geometryDistanceOffset = kp.component.geometryDistanceOffset,
                    });
                }

                cmd.SetBufferData(ctx.probeVolumesBuffer, gpuProbeVolumes);
                cmd.SetComputeBufferParam(subdivideSceneCS, s_VoxelizeProbeVolumesKernel, _ProbeVolumes, ctx.probeVolumesBuffer);
                cmd.SetComputeFloatParam(subdivideSceneCS, _ProbeVolumeCount, probeVolumes.Count);
                cmd.SetComputeVectorParam(subdivideSceneCS, _VolumeWorldOffset, cellAABB.center - cellAABB.extents);
                cmd.SetComputeVectorParam(subdivideSceneCS, _MaxBrickSize, Vector3.one * ctx.maxBrickCountPerAxis);

                int subdivisionLevelCount = (int)Mathf.Log(ctx.maxBrickCountPerAxis, 3);
                for (int i = 0; i <= subdivisionLevelCount; i++)
                {
                    int brickCountPerAxis = (int)Mathf.Pow(3, ctx.maxSubdivisionLevel - i);
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
                cmd.SetComputeFloatParam(subdivideSceneCS, _MaxSubdivisionLevel, ctx.maxSubdivisionLevel);
                cmd.SetComputeVectorParam(subdivideSceneCS, _VolumeSizeInBricks, Vector3.one * ctx.maxBrickCountPerAxis);
                cmd.SetComputeVectorParam(subdivideSceneCS, _SDFSize, new Vector3(ctx.sceneSDF.width, ctx.sceneSDF.height, ctx.sceneSDF.volumeDepth));
                cmd.SetComputeTextureParam(subdivideSceneCS, s_SubdivideKernel, _Input, ctx.sceneSDF);
                cmd.SetComputeTextureParam(subdivideSceneCS, s_SubdivideKernel, _ProbeVolumeData, probeVolumeData);
                DispatchCompute(cmd, s_SubdivideKernel, brickCount, brickCount, brickCount);
            }
        }
    }
}

#endif
