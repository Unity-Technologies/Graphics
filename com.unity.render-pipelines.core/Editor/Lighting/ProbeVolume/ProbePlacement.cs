#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Linq;

namespace UnityEngine.Rendering
{
    using Brick = ProbeBrickIndex.Brick;
    using Flags = ProbeReferenceVolume.BrickFlags;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    internal class ProbePlacement
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

        static protected ProbeReferenceVolume.Volume ToVolume(Bounds bounds)
        {
            ProbeReferenceVolume.Volume v = new ProbeReferenceVolume.Volume();
            v.corner = bounds.center - bounds.size * 0.5f;
            v.X = new Vector3(bounds.size.x, 0, 0);
            v.Y = new Vector3(0, bounds.size.y, 0);
            v.Z = new Vector3(0, 0, bounds.size.z);
            return v;
        }

        static void TrackSceneRefs(Scene origin, ref Dictionary<Scene, int> sceneRefs)
        {
            if (!sceneRefs.ContainsKey(origin))
                sceneRefs[origin] = 0;
            else
                sceneRefs[origin] += 1;
        }

        static protected int RenderersToVolumes(ref Renderer[] renderers, ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRefs)
        {
            int num = 0;

            foreach (Renderer r in renderers)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI;
                bool contributeGI = (flags & StaticEditorFlags.ContributeGI) != 0;

                if (!r.enabled || !r.gameObject.activeSelf || !contributeGI)
                    continue;

                ProbeReferenceVolume.Volume v = ToVolume(r.bounds);

                if (ProbeVolumePositioning.OBBIntersect(ref cellVolume, ref v))
                {
                    volumes.Add(v);

                    TrackSceneRefs(r.gameObject.scene, ref sceneRefs);

                    num++;
                }
            }

            return num;
        }

        static protected int NavPathsToVolumes(ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRefs)
        {
            // TODO
            return 0;
        }

        static protected int ImportanceVolumesToVolumes(ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRefs)
        {
            // TODO
            return 0;
        }

        static protected int LightsToVolumes(ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRefs)
        {
            // TODO
            return 0;
        }

        static protected int ProbeVolumesToVolumes(ref ProbeVolume[] probeVolumes, ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRefs)
        {
            int num = 0;

            foreach (ProbeVolume pv in probeVolumes)
            {
                if (!pv.isActiveAndEnabled)
                    continue;

                ProbeReferenceVolume.Volume indicatorVolume = new ProbeReferenceVolume.Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()), pv.parameters.maxSubdivisionMultiplier, pv.parameters.minSubdivisionMultiplier);

                if (ProbeVolumePositioning.OBBIntersect(ref cellVolume, ref indicatorVolume))
                {
                    cellVolume.maxSubdivisionMultiplier = Mathf.Max(cellVolume.maxSubdivisionMultiplier, pv.parameters.maxSubdivisionMultiplier, pv.parameters.minSubdivisionMultiplier);
                    volumes.Add(indicatorVolume);
                    TrackSceneRefs(pv.gameObject.scene, ref sceneRefs);
                    num++;
                }
            }

            return num;
        }

        static protected void CullVolumes(ref List<ProbeReferenceVolume.Volume> cullees, ref List<ProbeReferenceVolume.Volume> cullers, ref List<ProbeReferenceVolume.Volume> result)
        {
            foreach (ProbeReferenceVolume.Volume v in cullers)
            {
                ProbeReferenceVolume.Volume lv = v;

                foreach (ProbeReferenceVolume.Volume c in cullees)
                {
                    if (result.Contains(c))
                        continue;

                    ProbeReferenceVolume.Volume lc = c;

                    if (ProbeVolumePositioning.OBBIntersect(ref lv, ref lc))
                        result.Add(c);
                }
            }
        }

        static public void CreateInfluenceVolumes(ref ProbeReferenceVolume.Volume cellVolume, Renderer[] renderers, ProbeVolume[] probeVolumes,
            out List<ProbeReferenceVolume.Volume> culledVolumes, out Dictionary<Scene, int> sceneRefs)
        {
            // Keep track of volumes and which scene they originated from
            sceneRefs = new Dictionary<Scene, int>();

            // Extract all influencers inside the cell
            List<ProbeReferenceVolume.Volume> influenceVolumes = new List<ProbeReferenceVolume.Volume>();
            RenderersToVolumes(ref renderers, ref cellVolume, ref influenceVolumes, ref sceneRefs);
            NavPathsToVolumes(ref cellVolume, ref influenceVolumes, ref sceneRefs);
            ImportanceVolumesToVolumes(ref cellVolume, ref influenceVolumes, ref sceneRefs);
            LightsToVolumes(ref cellVolume, ref influenceVolumes, ref sceneRefs);

            // Extract all ProbeVolumes inside the cell
            List<ProbeReferenceVolume.Volume> indicatorVolumes = new List<ProbeReferenceVolume.Volume>();
            ProbeVolumesToVolumes(ref probeVolumes, ref cellVolume, ref indicatorVolumes, ref sceneRefs);

            // Cull all influencers against ProbeVolumes
            culledVolumes = new List<ProbeReferenceVolume.Volume>();
            CullVolumes(ref influenceVolumes, ref indicatorVolumes, ref culledVolumes);
        }

        public static void SubdivisionAlgorithm(ProbeReferenceVolume.Volume cellVolume, List<ProbeReferenceVolume.Volume> probeVolumes, List<ProbeReferenceVolume.Volume> influenceVolumes, RefTrans refTrans, List<Brick> inBricks, int subdivisionLevel, List<Flags> outFlags)
        {
            Flags f = new Flags();
            for (int i = 0; i < inBricks.Count; i++)
            {
                ProbeReferenceVolume.Volume brickVolume = ProbeVolumePositioning.CalculateBrickVolume(ref refTrans, inBricks[i]);

                // Find the local max from all overlapping probe volumes:
                float localMaxSubdiv = 0;
                float localMinSubdiv = 0;
                foreach (ProbeReferenceVolume.Volume v in probeVolumes)
                {
                    ProbeReferenceVolume.Volume vol = v;
                    if (ProbeVolumePositioning.OBBIntersect(ref vol, ref brickVolume))
                    {
                        localMaxSubdiv = Mathf.Max(localMaxSubdiv, vol.maxSubdivisionMultiplier);
                        // Do we use max for min subdiv too?
                        localMinSubdiv = Mathf.Max(localMinSubdiv, vol.minSubdivisionMultiplier);
                    }
                }

                bool belowMaxSubdiv = subdivisionLevel <= ProbeReferenceVolume.instance.GetMaxSubdivision(localMaxSubdiv);
                bool belowMinSubdiv = subdivisionLevel <= ProbeReferenceVolume.instance.GetMaxSubdivision(localMinSubdiv);

                // Keep bricks that overlap at least one probe volume, and at least one influencer (mesh)
                if (belowMinSubdiv || (belowMaxSubdiv && ShouldKeepBrick(probeVolumes, brickVolume) && ShouldKeepBrick(influenceVolumes, brickVolume)))
                {
                    f.subdivide = true;

                    // Transform into refvol space
                    brickVolume.Transform(refTrans.refSpaceToWS.inverse);
                    ProbeReferenceVolume.Volume cellVolumeTrans = new ProbeReferenceVolume.Volume(cellVolume);
                    cellVolumeTrans.Transform(refTrans.refSpaceToWS.inverse);
                    cellVolumeTrans.maxSubdivisionMultiplier = localMaxSubdiv;

                    // Discard parent brick if it extends outside of the cell, to prevent duplicates
                    var brickVolumeMax = brickVolume.corner + brickVolume.X + brickVolume.Y + brickVolume.Z;
                    var cellVolumeMax = cellVolumeTrans.corner + cellVolumeTrans.X + cellVolumeTrans.Y + cellVolumeTrans.Z;

                    f.discard = brickVolumeMax.x > cellVolumeMax.x ||
                        brickVolumeMax.y > cellVolumeMax.y ||
                        brickVolumeMax.z > cellVolumeMax.z ||
                        brickVolume.corner.x < cellVolumeTrans.corner.x ||
                        brickVolume.corner.y < cellVolumeTrans.corner.y ||
                        brickVolume.corner.z < cellVolumeTrans.corner.z;
                }
                else
                {
                    f.discard = true;
                    f.subdivide = false;
                }
                outFlags.Add(f);
            }
        }

        internal static bool ShouldKeepBrick(List<ProbeReferenceVolume.Volume> volumes, ProbeReferenceVolume.Volume brick)
        {
            foreach (ProbeReferenceVolume.Volume v in volumes)
            {
                ProbeReferenceVolume.Volume vol = v;
                if (ProbeVolumePositioning.OBBIntersect(ref vol, ref brick))
                    return true;
            }

            return false;
        }

        public static void Subdivide(ProbeReferenceVolume.Volume cellVolume, ProbeReferenceVolume refVol, List<ProbeReferenceVolume.Volume> influencerVolumes,
            ref Vector3[] positions, ref List<ProbeBrickIndex.Brick> bricks)
        {
            // TODO move out
            var indicatorVolumes = new List<ProbeReferenceVolume.Volume>();
            foreach (ProbeVolume pv in UnityEngine.Object.FindObjectsOfType<ProbeVolume>())
            {
                if (!pv.enabled)
                    continue;

                indicatorVolumes.Add(new ProbeReferenceVolume.Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()), pv.parameters.maxSubdivisionMultiplier, pv.parameters.minSubdivisionMultiplier));
            }

            ProbeReferenceVolume.SubdivisionDel subdivDel =
                (RefTrans refTrans, int subdivisionLevel, List<Brick> inBricks, List<Flags> outFlags) =>
            { SubdivisionAlgorithm(cellVolume, indicatorVolumes, influencerVolumes, refTrans, inBricks, subdivisionLevel, outFlags); };

            bricks = new List<ProbeBrickIndex.Brick>();

            // get a list of bricks for this volume
            int numProbes;
            refVol.CreateBricks(new List<ProbeReferenceVolume.Volume>() { cellVolume }, influencerVolumes, subdivDel, bricks, out numProbes);

            positions = new Vector3[numProbes];
            refVol.ConvertBricks(bricks, positions);
        }

        public static void SubdivideWithSDF(ProbeReferenceVolume.Volume cellVolume, ProbeReferenceVolume refVol, List<ProbeReferenceVolume.Volume> influencerVolumes,
            ref Vector3[] positions, ref List<ProbeBrickIndex.Brick> bricks)
        {
            // Camera bakingCamera;
            // GameObject bakingCameraGO;
            // RenderTexture dummyRT;
            RenderTexture sceneSDF = null;
            RenderTexture sceneSDF2 = null;
            RenderTexture dummyRenderTarget = null;

            try
            {
                // bakingCameraGO = new GameObject("Baking Camera") { /*hideFlags = HideFlags.HideAndDontSave*/ }; // TODO: hide
                // bakingCamera = bakingCameraGO.AddComponent<Camera>();

                sceneSDF = new RenderTexture(64, 64, 0, Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat)
                {
                    name = "Scene SDF",
                    dimension = TextureDimension.Tex3D,
                    volumeDepth = 64,
                    enableRandomWrite = true,
                };
                sceneSDF.Create();
                sceneSDF2 = new RenderTexture(64, 64, 0, Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat)
                {
                    name = "Scene SDF Double Buffer",
                    dimension = TextureDimension.Tex3D,
                    volumeDepth = 64,
                    enableRandomWrite = true,
                };
                sceneSDF2.Create();
                dummyRenderTarget = RenderTexture.GetTemporary(128, 128, 0, GraphicsFormat.R8_SNorm);

                var cmd = CommandBufferPool.Get("SDF Gen");

                RastersizeMeshes(cmd, cellVolume, sceneSDF, dummyRenderTarget);

                GenerateDistanceField(cmd, sceneSDF, sceneSDF2);

                int maxBricks = 64;
                var bricksBuffer = new ComputeBuffer(maxBricks * maxBricks * maxBricks, sizeof(float) * 3, ComputeBufferType.Append);
                bricksBuffer.SetData(new Vector3[maxBricks * maxBricks * maxBricks]); // initialize the buffer to 0
                bricksBuffer.SetCounterValue(0);
                SubdivideFromDistanceField(cmd, cellVolume.CalculateAABB(), sceneSDF, bricksBuffer, maxBricks);

                Graphics.ExecuteCommandBuffer(cmd);

                var bricksArray = new Vector3[maxBricks * maxBricks * maxBricks];
                bricksBuffer.GetData(bricksArray);
                bricksBuffer.Release();

                // TODO: convert the position into brick position (int index in the cell, starting at 0 at a corner)
                bricks = bricksArray.Where(b => b.magnitude > 0).Select(p => new Brick(new Vector3Int((int)p.x, (int)p.y, (int)p.z), refVol.GetMaxSubdivision())).ToList();
                positions = bricksArray.Where(b => b.magnitude > 0).ToArray();
            }
            finally // Release resources in case a fatal error occurs
            {
                // TODO: destroy!
                sceneSDF?.Release();
                sceneSDF2?.Release();
                if (dummyRenderTarget != null)
                    RenderTexture.ReleaseTemporary(dummyRenderTarget);
                // CoreUtils.Destroy(bakingCameraGO);
            }

            // // TODO move out
            // var indicatorVolumes = new List<ProbeReferenceVolume.Volume>();
            // foreach (ProbeVolume pv in UnityEngine.Object.FindObjectsOfType<ProbeVolume>())
            // {
            //     if (!pv.enabled)
            //         continue;

            //     indicatorVolumes.Add(new ProbeReferenceVolume.Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()), pv.parameters.maxSubdivisionMultiplier, pv.parameters.minSubdivisionMultiplier));
            // }

            // ProbeReferenceVolume.SubdivisionDel subdivDel =
            //     (RefTrans refTrans, int subdivisionLevel, List<Brick> inBricks, List<Flags> outFlags) =>
            // { SubdivisionAlgorithm(cellVolume, indicatorVolumes, influencerVolumes, refTrans, inBricks, subdivisionLevel, outFlags); };

            // bricks = new List<ProbeBrickIndex.Brick>();

            // // get a list of bricks for this volume
            // int numProbes;
            // refVol.CreateBricks(new List<ProbeReferenceVolume.Volume>() { cellVolume }, influencerVolumes, subdivDel, bricks, out numProbes);

            // positions = new Vector3[numProbes];
            // refVol.ConvertBricks(bricks, positions);
        }

        static void RastersizeMeshes(CommandBuffer cmd, ProbeReferenceVolume.Volume cellVolume, RenderTexture sceneSDF, RenderTexture dummyRenderTarget)
        {
            var renderers = Object.FindObjectsOfType<MeshRenderer>();

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
            mat.SetVector("_OutputSize", new Vector3(64, 64, 64));
            mat.SetVector("_VolumeWorldOffset", cellAABB.center - cellAABB.extents);
            mat.SetVector("_VolumeSize", cellAABB.extents * 2);

            var topMatrix = GetCameraMatrixForAngle(Quaternion.Euler(90, 0, 0));
            var rightMatrix = GetCameraMatrixForAngle(Quaternion.Euler(0, 90, 0));
            var forwardMatrix = GetCameraMatrixForAngle(Quaternion.Euler(0, 0, 90));

            Matrix4x4 GetCameraMatrixForAngle(Quaternion rotation)
            {
                var worldToCamera = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
                // var projection = Matrix4x4.Ortho(-cellAABB.extents.x, cellAABB.extents.x, -cellAABB.extents.y, cellAABB.extents.y, -cellAABB.extents.z, cellAABB.extents.z);
                var projection = Matrix4x4.Ortho(-1, 1, -1, 1, -1, 1);
                return projection * worldToCamera;
            }

            // Voxelize all meshes

            // We need to bind at least something for rendering
            cmd.SetRenderTarget(dummyRenderTarget);
            cmd.SetViewport(new Rect(0, 0, 128, 128));
            var props = new MaterialPropertyBlock();
            foreach (MeshRenderer renderer in renderers)
            {
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
            cmd.DispatchCompute(subdivideSceneCS, kernel, (int)Mathf.Max(1, width / x), (int)Mathf.Max(1, height / y), (int)Mathf.Max(1, depth / z));
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

        static void SubdivideFromDistanceField(CommandBuffer cmd, Bounds volume, RenderTexture sceneSDF, ComputeBuffer buffer, int maxBricks)
        {
            int kernel = subdivideSceneCS.FindKernel("Subdivide");

            cmd.SetComputeVectorParam(subdivideSceneCS, "_VolumeWorldOffset", volume.center - volume.extents);
            cmd.SetComputeBufferParam(subdivideSceneCS, kernel, "_Bricks", buffer);
            cmd.SetComputeVectorParam(subdivideSceneCS, "_MaxBrickSize", Vector3.one * maxBricks);
            cmd.SetComputeVectorParam(subdivideSceneCS, "_VolumeSize", volume.size);
            cmd.SetComputeVectorParam(subdivideSceneCS, "_SDFSize", new Vector3(sceneSDF.width, sceneSDF.height, sceneSDF.volumeDepth));
            cmd.SetComputeTextureParam(subdivideSceneCS, kernel, "_Input", sceneSDF);
            DispatchCompute(cmd, kernel, maxBricks, maxBricks, maxBricks);
        }
    }
}

#endif
