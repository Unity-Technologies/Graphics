using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using Unity.Collections;

using RendererList = UnityEngine.Rendering.RendererUtils.RendererList;
using RendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static readonly Vector3[] s_BFProbeFaceRotations =
        {
            new Vector3(0, 90, 0),
            new Vector3(0, 270, 0),
            new Vector3(270, 0, 0),
            new Vector3(90, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(0, 180, 0),
        };

        private const int k_CubeSize = 64;
        private const int k_MaxProbeCount = 32;

        private RenderTexture m_BFProbeTest;

        private Material m_DebugMaterial;
        private Mesh m_DebugMesh;
        private Material m_SurfaceCacheMaterial;

        static readonly int _ViewProjMatrix = Shader.PropertyToID("_ViewProjMatrix");
        static readonly int _WorldSpaceCameraPos_Internal = Shader.PropertyToID("_WorldSpaceCameraPos_Internal");

        private CameraCache<int> m_BFProbeCameraCache = new CameraCache<int>();

        internal void InitializeBFProbes()
        {
            m_BFProbeTest = new RenderTexture(k_CubeSize, k_CubeSize, GraphicsFormat.B10G11R11_UFloatPack32, GraphicsFormat.D32_SFloat)
            {
                dimension = TextureDimension.CubeArray,
                volumeDepth = 6 * k_MaxProbeCount,
            };
            m_BFProbeTest.Create();
        }

        internal void ReleaseBFProbes()
        {
            m_BFProbeCameraCache.Dispose();
        }

        internal void RenderBFProbes(ScriptableRenderContext context)
        {
            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.BFProbeRender)))
            {
                var probes = Object.FindObjectsOfType<BFProbe>();
                if (probes.Length == 0)
                    return;

                var cmd = new CommandBuffer();
                cmd.name = "BFProbes";

                int probeCount = Mathf.Min(probes.Length, k_MaxProbeCount);
                for (int probeIndex = 0; probeIndex < probeCount; ++probeIndex)
                for (int faceIndex = 0; faceIndex < 6; ++faceIndex)
                {
                    Camera camera = m_BFProbeCameraCache.GetOrCreate(faceIndex, m_FrameCount, CameraType.Game);
                    camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    camera.gameObject.SetActive(false);
                    camera.targetTexture = m_BFProbeTest;
                    camera.nearClipPlane = 0.01f;
                    camera.farClipPlane = 1000.0f;
                    camera.fieldOfView = 90.0f;

                    camera.transform.position = probes[probeIndex].transform.position;
                    camera.transform.rotation = Quaternion.Euler(s_BFProbeFaceRotations[faceIndex]);

                    camera.TryGetCullingParameters(out var cullingParameters);
                    cullingParameters.cullingOptions = CullingOptions.DisablePerObjectCulling;
                    var cullingResults = context.Cull(ref cullingParameters);

                    var rendererListDesc = new RendererListDesc(HDShaderPassNames.s_ReadSurfaceCacheName, cullingResults, camera)
                    {
                        renderingLayerMask = ~DeferredMaterialBRG.RenderLayerMask,
                        renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque,
                        sortingCriteria = SortingCriteria.CommonOpaque,
                    };
                    var rendererList = context.CreateRendererList(rendererListDesc);

                    Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);

                    Matrix4x4 gpuView = GeometryUtils.CalculateWorldToCameraMatrixRHS(camera.transform.position, camera.transform.rotation);
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                        gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));
                    Vector3 cameraPos = camera.transform.position;

                    CoreUtils.SetRenderTarget(cmd, m_BFProbeTest, ClearFlag.All, depthSlice: 6*probeIndex + faceIndex);
                    cmd.SetViewport(new Rect(0, 0, k_CubeSize, k_CubeSize));
                    cmd.SetGlobalMatrix(_ViewProjMatrix, gpuProj * gpuView);
                    cmd.SetGlobalVector(_WorldSpaceCameraPos_Internal, new Vector4(cameraPos.x, cameraPos.y, cameraPos.z, 0.0f));
                    cmd.SetInvertCulling(true);
                    cmd.DrawRendererList(rendererList);
                    cmd.SetInvertCulling(false);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
            }
        }

        internal void RenderBFProbeDebug(Camera camera)
        {
            if (camera.cameraType != CameraType.SceneView && camera.cameraType != CameraType.Game)
                return;

            var probes = Object.FindObjectsOfType<BFProbe>();
            if (probes.Length == 0)
                return;

            if (m_DebugMaterial == null)
            {
                m_DebugMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugBFProbePS);
                m_DebugMaterial.enableInstancing = true;
            }
            if (m_DebugMesh == null)
                m_DebugMesh = defaultResources.assets.probeDebugSphere;

            var properties = new MaterialPropertyBlock();
            properties.SetTexture(HDShaderIDs._InputCubemap, m_BFProbeTest);

            int probeCount = Mathf.Min(probes.Length, k_MaxProbeCount);
            var matrices = new List<Matrix4x4>(probeCount);
            for (int probeIndex = 0; probeIndex < probeCount; ++probeIndex)
                matrices.Add(Matrix4x4.Translate(probes[probeIndex].transform.position));
            
            Graphics.DrawMeshInstanced(m_DebugMesh, 0, m_DebugMaterial, matrices.ToArray(), probeCount, properties, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
        }
    }
}
