#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Linq;
using UnityEngine.Profiling;

namespace UnityEngine.Experimental.Rendering
{
    using Brick = ProbeBrickIndex.Brick;
    using Flags = ProbeReferenceVolume.BrickFlags;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    class ProbePlacement
    {
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

        // static void TrackSceneRefs(Scene origin, Dictionary<Scene, int> sceneRefs)
        // {
        //     if (!sceneRefs.ContainsKey(origin))
        //         sceneRefs[origin] = 0;
        //     else
        //         sceneRefs[origin] += 1;
        // }

        // static protected int RenderersToVolumes(in List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers, in ProbeReferenceVolume.Volume cellVolume, List<ProbeReferenceVolume.Volume> volumes, Dictionary<Scene, int> sceneRefs)
        // {
        //     int num = 0;

        //     foreach (var kp in renderers)
        //     {
        //         var r = kp.component;
        //         var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI;
        //         bool contributeGI = (flags & StaticEditorFlags.ContributeGI) != 0;

        //         if (!r.enabled || !r.gameObject.activeSelf || !contributeGI)
        //             continue;

        //         ProbeReferenceVolume.Volume v = ToVolume(r.bounds);

        //         if (ProbeVolumePositioning.OBBIntersect(cellVolume, v))
        //         {
        //             volumes.Add(v);

        //             TrackSceneRefs(r.gameObject.scene, sceneRefs);

        //             num++;
        //         }
        //     }

        //     return num;
        // }

        // static protected int ProbeVolumesToVolumes(in List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes, ref ProbeReferenceVolume.Volume cellVolume, List<ProbeReferenceVolume.Volume> volumes, Dictionary<Scene, int> sceneRefs)
        // {
        //     int num = 0;

        //     foreach (var kp in probeVolumes)
        //     {
        //         var pv = kp.component;
        //         if (!pv.isActiveAndEnabled)
        //             continue;

        //         ProbeReferenceVolume.Volume indicatorVolume = new ProbeReferenceVolume.Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()), pv.maxSubdivisionMultiplier, pv.minSubdivisionMultiplier);

        //         if (ProbeVolumePositioning.OBBIntersect(cellVolume, indicatorVolume))
        //         {
        //             cellVolume.maxSubdivisionMultiplier = Mathf.Max(cellVolume.maxSubdivisionMultiplier, pv.maxSubdivisionMultiplier, pv.minSubdivisionMultiplier);
        //             volumes.Add(indicatorVolume);
        //             TrackSceneRefs(pv.gameObject.scene, sceneRefs);
        //             num++;
        //         }
        //     }

        //     return num;
        // }

        // static protected void CullVolumes(in List<ProbeReferenceVolume.Volume> cullees, in List<ProbeReferenceVolume.Volume> cullers, List<ProbeReferenceVolume.Volume> result)
        // {
        //     foreach (ProbeReferenceVolume.Volume v in cullers)
        //     {
        //         ProbeReferenceVolume.Volume lv = v;

        //         foreach (ProbeReferenceVolume.Volume c in cullees)
        //         {
        //             if (result.Contains(c))
        //                 continue;

        //             ProbeReferenceVolume.Volume lc = c;

        //             if (ProbeVolumePositioning.OBBIntersect(lv, lc))
        //                 result.Add(c);
        //         }
        //     }
        // }

        // static public void CreateInfluenceVolumes(ref ProbeReferenceVolume.Volume cellVolume, List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers, List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes,
        //     out List<ProbeReferenceVolume.Volume> culledVolumes, out Dictionary<Scene, int> sceneRefs)
        // {
        //     // Keep track of volumes and which scene they originated from
        //     sceneRefs = new Dictionary<Scene, int>();

        //     // Extract all influencers inside the cell
        //     List<ProbeReferenceVolume.Volume> influenceVolumes = new List<ProbeReferenceVolume.Volume>();
        //     RenderersToVolumes(renderers, cellVolume, influenceVolumes, sceneRefs);

        //     // Extract all ProbeVolumes inside the cell
        //     List<ProbeReferenceVolume.Volume> indicatorVolumes = new List<ProbeReferenceVolume.Volume>();
        //     ProbeVolumesToVolumes(probeVolumes, ref cellVolume, indicatorVolumes, sceneRefs);

        //     // Cull all influencers against ProbeVolumes
        //     culledVolumes = new List<ProbeReferenceVolume.Volume>();
        //     CullVolumes(influenceVolumes, indicatorVolumes, culledVolumes);
        // }

        // TODO: alloc this in the BakeCells function
        static RenderTextureDescriptor distanceFieldTextureDescriptor = new RenderTextureDescriptor
        {
            height = 64,
            width = 64,
            volumeDepth = 64,
            enableRandomWrite = true,
            dimension = TextureDimension.Tex3D,
            graphicsFormat = Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
            msaaSamples = 1,
        };

        public static List<Brick> SubdivideWithSDF(ProbeReferenceVolume.Volume cellVolume, ProbeReferenceVolume refVol, List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers, List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes)
        {
            RenderTexture sceneSDF = null;
            RenderTexture sceneSDF2 = null;
            RenderTexture dummyRenderTarget = null;
            var bricks = new List<Brick>();

            try
            {
                // Find the maximum subdivision level we can have in this cell (avoid extra work if not needed)
                int maxSubdivLevel = refVol.GetMaxSubdivision() - 1; // remove 1 because the last subdiv level is the cell size
                int startSubdivisionLevel = maxSubdivLevel - (refVol.GetMaxSubdivision(probeVolumes.Max(p => p.component.maxSubdivisionMultiplier)) - 1);

                // We assume that all the cells are cubes
                int maxBrickCountPerAxis = (int)Mathf.Pow(3, maxSubdivLevel);
                int sceneSDFSize = Mathf.NextPowerOfTwo(maxBrickCountPerAxis);

                sceneSDF = RenderTexture.GetTemporary(distanceFieldTextureDescriptor);
                sceneSDF.name = "Scene SDF";
                sceneSDF.Create();
                sceneSDF2 = RenderTexture.GetTemporary(distanceFieldTextureDescriptor);
                sceneSDF2.name = "Scene SDF Double Buffer";
                sceneSDF2.Create();
                dummyRenderTarget = RenderTexture.GetTemporary(sceneSDFSize * 2, sceneSDFSize * 2, 0, GraphicsFormat.R8_SNorm);

                var cmd = CommandBufferPool.Get("SDF Gen");

                RastersizeMeshes(cmd, cellVolume, sceneSDF, dummyRenderTarget, maxBrickCountPerAxis, renderers);

                GenerateDistanceField(cmd, sceneSDF, sceneSDF2);

                var bricksBuffer = new ComputeBuffer(maxBrickCountPerAxis * maxBrickCountPerAxis * maxBrickCountPerAxis, sizeof(float) * 3, ComputeBufferType.Append);
                var readbackCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

                HashSet<Brick> bricksList = new HashSet<Brick>();

                Graphics.ExecuteCommandBuffer(cmd);

                // Brick index offset based on the cell position
                var brickOffset = new Vector3Int((int)cellVolume.corner.x, (int)cellVolume.corner.y, (int)cellVolume.corner.z) / 3;

                var transform = refVol.GetTransform();
                for (int subdivisionLevel = startSubdivisionLevel; subdivisionLevel <= maxSubdivLevel; subdivisionLevel++)
                {
                    // Add the bricks from the probe volume min subdivision level:
                    int brickCountPerAxis = (int)Mathf.Pow(3, maxSubdivLevel - subdivisionLevel);
                    int brickSize = (int)Mathf.Pow(3, subdivisionLevel);

                    // Check if a probe volume inside the cell will add subdivision at this level
                    float localMinSubdiv = probeVolumes.Max(pv => pv.component.minSubdivisionMultiplier);

                    if (maxSubdivLevel - subdivisionLevel < refVol.GetMaxSubdivision(localMinSubdiv))
                    {
                        // Adds the bricks from the min subdivision setting of the volume
                        for (int x = 0; x < brickCountPerAxis; x++)
                        {
                            for (int y = 0; y < brickCountPerAxis; y++)
                            {
                                for (int z = 0; z < brickCountPerAxis; z++)
                                {
                                    var brick = new Brick(brickOffset + new Vector3Int(x * brickSize, y * brickSize, z * brickSize), subdivisionLevel);
                                    ProbeReferenceVolume.Volume brickVolume = ProbeVolumePositioning.CalculateBrickVolume(transform, brick);

                                    // TODO: collider check on the probe volume:
                                    // var closestPoint = collider.ClosestPoint(triggerPos);
                                    // var d = (closestPoint - triggerPos).sqrMagnitude;

                                    // minSqrDistance = Mathf.Min(minSqrDistance, d);

                                    // // Update the list of overlapping colliders
                                    // if (d <= sqrFadeRadius)
                                    //     volume.m_OverlappingColliders.Add(collider);

                                    // Find the local max from all overlapping probe volumes:
                                    // float localMaxSubdiv = 0;
                                    localMinSubdiv = 0;
                                    bool overlapVolume = false;
                                    foreach (var kp in probeVolumes)
                                    {
                                        var vol = kp.volume;
                                        if (ProbeVolumePositioning.OBBIntersect(vol, brickVolume))
                                        {
                                            // localMaxSubdiv = Mathf.Max(localMaxSubdiv, vol.maxSubdivisionMultiplier);
                                            // Do we use max for min subdiv too?
                                            localMinSubdiv = Mathf.Max(localMinSubdiv, vol.minSubdivisionMultiplier);
                                            overlapVolume = true;
                                        }
                                    }

                                    // Debug.Log(localMinSubdiv);
                                    // bool belowMaxSubdiv = subdivisionLevel <= refVol.GetMaxSubdivision(localMaxSubdiv);
                                    bool belowMinSubdiv = (maxSubdivLevel - subdivisionLevel) < refVol.GetMaxSubdivision(localMinSubdiv);

                                    // Keep bricks that overlap at least one probe volume, and at least one influencer (mesh)
                                    if (overlapVolume && belowMinSubdiv)
                                        bricksList.Add(brick);
                                }
                            }
                        }
                    }

                    cmd.Clear();
                    // TODO: clear the buffer in a compute shader
                    cmd.SetBufferData(bricksBuffer, new Vector3[maxBrickCountPerAxis * maxBrickCountPerAxis * maxBrickCountPerAxis]);
                    bricksBuffer.SetCounterValue(0);
                    // TODO: avoid subdividing more than the max local subdivision from probe volume
                    SubdivideFromDistanceField(cmd, cellVolume.CalculateAABB(), sceneSDF, bricksBuffer, brickCountPerAxis, maxBrickCountPerAxis);
                    cmd.CopyCounterValue(bricksBuffer, readbackCountBuffer, 0);
                    Graphics.ExecuteCommandBuffer(cmd);

                    var brickCountReadbackArray = new int[1];
                    readbackCountBuffer.GetData(brickCountReadbackArray, 0, 0, 1);
                    int readbackBrickCount = brickCountReadbackArray[0];

                    Vector3[] brickPositions = new Vector3[readbackBrickCount];
                    bricksBuffer.GetData(brickPositions, 0, 0, readbackBrickCount);

                    foreach (var pos in brickPositions)
                    {
                        var brick = new Brick(new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z)), subdivisionLevel);
                        bricksList.Add(brick);
                    }
                }

                // var bricksArray = new Vector3[maxBrickCount * maxBrickCount * maxBrickCount];
                // bricksBuffer.GetData(bricksArray);
                bricksBuffer.Release();
                readbackCountBuffer.Release();

                bricks = bricksList.ToList();

                Profiler.BeginSample("sort");
                // sort from larger to smaller bricks
                bricks.Sort((Brick lhs, Brick rhs) =>
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
            finally // Release resources in case a fatal error occurs
            {
                if (sceneSDF != null)
                    RenderTexture.ReleaseTemporary(sceneSDF);
                if (sceneSDF2 != null)
                    RenderTexture.ReleaseTemporary(sceneSDF2);
                if (dummyRenderTarget != null)
                    RenderTexture.ReleaseTemporary(dummyRenderTarget);
            }

            return bricks;
        }

        static void RastersizeMeshes(CommandBuffer cmd, ProbeReferenceVolume.Volume cellVolume, RenderTexture sceneSDF, RenderTexture dummyRenderTarget, int maxBrickCountPerAxis, List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers)
        {
            // TODO: group renderers by loaded scene + fill the map?
            var cellAABB = cellVolume.CalculateAABB();

            cmd.BeginSample("Clear");
            int clearKernel = subdivideSceneCS.FindKernel("Clear");
            cmd.SetComputeTextureParam(subdivideSceneCS, clearKernel, "_Output", sceneSDF);
            DispatchCompute(cmd, clearKernel, sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth);
            cmd.EndSample("Clear");

            // Hum, will this cause binding issues for other systems?
            cmd.SetRandomWriteTarget(4, sceneSDF);
            // cmd.ClearRandomWriteTargets();

            var mat = new Material(Shader.Find("Hidden/ProbeVolume/VoxelizeScene"));
            mat.SetVector("_OutputSize", new Vector3(sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth));
            mat.SetVector("_VolumeWorldOffset", cellAABB.center - cellAABB.extents);
            mat.SetVector("_VolumeSize", cellAABB.size);

            var topMatrix = GetCameraMatrixForAngle(Quaternion.Euler(90, 0, 0));
            var rightMatrix = GetCameraMatrixForAngle(Quaternion.Euler(0, 90, 0));
            var forwardMatrix = GetCameraMatrixForAngle(Quaternion.Euler(0, 0, 90));

            Matrix4x4 GetCameraMatrixForAngle(Quaternion rotation)
            {
                var worldToCamera = Matrix4x4.Rotate(rotation);
                // var projection = Matrix4x4.Ortho(-cellAABB.extents.x, cellAABB.extents.x, -cellAABB.extents.y, cellAABB.extents.y, -cellAABB.extents.z, cellAABB.extents.z);
                var projection = Matrix4x4.Ortho(-1, 1, -1, 1, -1, 1);
                return projection * worldToCamera;
            }

            // Voxelize all meshes

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
                        props.SetMatrix("_CameraMatrix", topMatrix);
                        cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, mat, 0, shaderPass: 0, props);
                        props.SetMatrix("_CameraMatrix", rightMatrix);
                        cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, mat, 0, shaderPass: 0, props);
                        props.SetMatrix("_CameraMatrix", forwardMatrix);
                        cmd.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, mat, 0, shaderPass: 0, props);
                    }
                }
            }

            cmd.ClearRandomWriteTargets();
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

        static void GenerateDistanceField(CommandBuffer cmd, RenderTexture sceneSDF, RenderTexture tmp)
        {
            // Generate distance field with JFA
            cmd.SetComputeVectorParam(subdivideSceneCS, "_Size", new Vector4(sceneSDF.width, 1.0f / sceneSDF.width));

            int clearKernel = subdivideSceneCS.FindKernel("Clear");
            int jumpFloodingKernel = subdivideSceneCS.FindKernel("JumpFlooding");
            int fillUVKernel = subdivideSceneCS.FindKernel("FillUVMap");
            int finalPassKernel = subdivideSceneCS.FindKernel("FinalPass");

            // TODO: try to get rid of the copies again
            cmd.BeginSample("Copy");
            for (int i = 0; i < sceneSDF.volumeDepth; i++)
                cmd.CopyTexture(sceneSDF, i, 0, tmp, i, 0);
            cmd.EndSample("Copy");

            // Jump flooding implementation based on https://www.comp.nus.edu.sg/~tants/jfa.html
            cmd.SetComputeTextureParam(subdivideSceneCS, fillUVKernel, "_Input", tmp);
            cmd.SetComputeTextureParam(subdivideSceneCS, fillUVKernel, "_Output", sceneSDF);
            DispatchCompute(cmd, fillUVKernel, sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth);

            int maxLevels = (int)Mathf.Log(sceneSDF.width, 2);
            for (int i = 0; i <= maxLevels; i++)
            {
                float offset = 1 << (maxLevels - i);
                cmd.SetComputeFloatParam(subdivideSceneCS, "_Offset", offset);
                cmd.SetComputeTextureParam(subdivideSceneCS, jumpFloodingKernel, "_Input", sceneSDF);
                cmd.SetComputeTextureParam(subdivideSceneCS, jumpFloodingKernel, "_Output", tmp);
                DispatchCompute(cmd, jumpFloodingKernel, sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth);

                cmd.BeginSample("Copy");
                for (int j = 0; j < sceneSDF.volumeDepth; j++)
                    cmd.CopyTexture(tmp, j, 0, sceneSDF, j, 0);
                cmd.EndSample("Copy");
            }

            cmd.SetComputeTextureParam(subdivideSceneCS, finalPassKernel, "_Input", tmp);
            cmd.SetComputeTextureParam(subdivideSceneCS, finalPassKernel, "_Output", sceneSDF);
            DispatchCompute(cmd, finalPassKernel, sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth);
        }

        static void SubdivideFromDistanceField(CommandBuffer cmd, Bounds volume, RenderTexture sceneSDF, ComputeBuffer buffer, int brickCount, int maxBrickCountPerAxis)
        {
            // TODO: cleanup: cache kernel names + cache shader ids
            int kernel = subdivideSceneCS.FindKernel("Subdivide");

            // We convert the world space volume position (of a corner) in bricks.
            // This is necessary to have correct brick position (the position calculated in the compute shader needs to be in number of bricks from the reference volume (origin)).
            Vector3 volumeBrickPosition = (volume.center - volume.extents) / 3.0f;
            cmd.SetComputeVectorParam(subdivideSceneCS, "_VolumeOffset", volumeBrickPosition);
            cmd.SetComputeBufferParam(subdivideSceneCS, kernel, "_Bricks", buffer);
            cmd.SetComputeVectorParam(subdivideSceneCS, "_MaxBrickSize", Vector3.one * brickCount);
            cmd.SetComputeVectorParam(subdivideSceneCS, "_VolumeSize", Vector3.one * maxBrickCountPerAxis);
            cmd.SetComputeVectorParam(subdivideSceneCS, "_SDFSize", new Vector3(sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth));
            cmd.SetComputeTextureParam(subdivideSceneCS, kernel, "_Input", sceneSDF);
            DispatchCompute(cmd, kernel, brickCount, brickCount, brickCount);
        }
    }
}

#endif
