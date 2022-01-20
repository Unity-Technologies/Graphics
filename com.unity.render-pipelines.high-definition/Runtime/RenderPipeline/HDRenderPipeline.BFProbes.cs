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


        private const int k_OctSize = (int)BFProbeConfig.StorageOctSize;
        private const int k_StorageWidthInProbes = (int)BFProbeConfig.StorageWidthInProbes;
        private const int k_StorageHeightInProbes = (int)BFProbeConfig.StorageHeightInProbes;
        private const int k_MaxProbeCount = (int)BFProbeConfig.StorageMaxProbeCount;

        private const int k_CubeSize = (int)BFProbeConfig.TempCubeSize;
        private const int k_MaxProbesPerBatch = (int)BFProbeConfig.TempMaxProbeCount;

        private RenderTexture m_BFProbeTemp;
        private RenderTexture m_BFProbeStorage;

        private Material m_DebugMaterial;
        private Mesh m_DebugMesh;
        private Material m_SurfaceCacheMaterial;

        static readonly int _ViewProjMatrixArray = Shader.PropertyToID("_ViewProjMatrixArray");
        static readonly int _WorldSpaceCameraPosArray = Shader.PropertyToID("_WorldSpaceCameraPosArray");

        static readonly int _BFProbeStorage = Shader.PropertyToID("_BFProbeStorage");
        static readonly int _BFProbeTempCol = Shader.PropertyToID("_BFProbeTempCol");
        static readonly int _BFProbeDestOffset = Shader.PropertyToID("_BFProbeDestOffset");
        static readonly int _BFProbeCopyCount = Shader.PropertyToID("_BFProbeCopyCount");

        private Matrix4x4[] m_ViewProjArray = new Matrix4x4[6*k_MaxProbesPerBatch];
        private Vector4[] m_CameraPosArray = new Vector4[6*k_MaxProbesPerBatch];

        private CameraCache<int> m_BFProbeCameraCache = new CameraCache<int>();

        internal void InitializeBFProbes()
        {
            RenderTextureDescriptor storageDesc = new RenderTextureDescriptor
            {
                dimension = TextureDimension.Tex2D,
                width = k_OctSize * k_StorageWidthInProbes,
                height = k_OctSize * k_StorageHeightInProbes,
                volumeDepth = 1,
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                depthBufferBits = 0,
                msaaSamples = 1,
                enableRandomWrite = true,
            };

            m_BFProbeStorage = new RenderTexture(storageDesc);
            m_BFProbeStorage.Create();

            RenderTextureDescriptor tempDesc = new RenderTextureDescriptor
            {
                dimension = TextureDimension.CubeArray,
                width = k_CubeSize,
                height = k_CubeSize,
                volumeDepth = 6 * k_MaxProbesPerBatch,
                graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32,
                depthStencilFormat = GraphicsFormat.D32_SFloat,
                msaaSamples = 1,
            };
            m_BFProbeTemp = RenderTexture.GetTemporary(tempDesc);
        }

        internal void ReleaseBFProbes()
        {
            m_BFProbeCameraCache.Dispose();
        }

        internal void RenderBFProbes(ScriptableRenderContext context)
        {
            var probes = Object.FindObjectsOfType<BFProbe>();
            if (probes.Length == 0)
                return;

            var cmd = new CommandBuffer();
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.BFProbeRender)))
            {
                cmd.name = "BFProbes";

                int probeCount = Mathf.Min(probes.Length, k_MaxProbeCount);
                int batchCount = HDUtils.DivRoundUp(probeCount, k_MaxProbesPerBatch);

                for (int batchIndex = 0; batchIndex < batchCount; ++batchIndex)
                {
                    int probeBase = batchIndex * k_MaxProbesPerBatch;
                    int probeCountInBatch = Mathf.Min(probeCount - probeBase, k_MaxProbesPerBatch);
                    for (int probeOffset = 0; probeOffset < probeCountInBatch; ++probeOffset)
                    for (int faceIndex = 0; faceIndex < 6; ++faceIndex)
                    {
                        Vector3 cameraPos = probes[probeBase + probeOffset].transform.position;
                        Quaternion cameraRotation = Quaternion.Euler(s_BFProbeFaceRotations[faceIndex]);

                        Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(90.0f, 1.0f, 0.01f, 10000.0f), false);

                        Matrix4x4 gpuView = GeometryUtils.CalculateWorldToCameraMatrixRHS(cameraPos, cameraRotation);
                        if (ShaderConfig.s_CameraRelativeRendering != 0)
                            gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));

                        int arrayIndex = 6*probeOffset + faceIndex;
                        m_ViewProjArray[arrayIndex] = gpuProj * gpuView;
                        m_CameraPosArray[arrayIndex] = new Vector4(cameraPos.x, cameraPos.y, cameraPos.z, 0.0f);
                    }

                    Camera camera = m_BFProbeCameraCache.GetOrCreate(batchIndex, m_FrameCount, CameraType.Game);
                    camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    camera.gameObject.SetActive(false);
                    camera.orthographic = true;
                    camera.orthographicSize = 10000.0f;

                    camera.TryGetCullingParameters(out var cullingParameters);
                    cullingParameters.cullingOptions = CullingOptions.DisablePerObjectCulling;
                    var cullingResults = context.Cull(ref cullingParameters);

                    var rendererListDesc = new RendererListDesc(HDShaderPassNames.s_ReadSurfaceCacheName, cullingResults, camera)
                    {
                        renderingLayerMask = ~DeferredMaterialBRG.RenderLayerMask,
                        renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque,
                    };
                    var rendererList = context.CreateRendererList(rendererListDesc);

                    // rasterize this batch of probes
                    CoreUtils.SetRenderTarget(cmd, m_BFProbeTemp, ClearFlag.All);
                    cmd.SetViewport(new Rect(0, 0, k_CubeSize, k_CubeSize));

                    cmd.SetGlobalMatrixArray(_ViewProjMatrixArray, m_ViewProjArray);
                    cmd.SetGlobalVectorArray(_WorldSpaceCameraPosArray, m_CameraPosArray);

                    cmd.SetInstanceMultiplier((uint)(6*probeCountInBatch));
                    cmd.DrawRendererList(rendererList);
                    cmd.SetInstanceMultiplier(0);

                    // copy into the storage
                    ComputeShader shader = defaultResources.shaders.packBFProbeCS;
                    int kernel = shader.FindKernel("Main");

                    cmd.SetComputeTextureParam(shader, kernel, _BFProbeStorage, m_BFProbeStorage);
                    cmd.SetComputeTextureParam(shader, kernel, _BFProbeTempCol, m_BFProbeTemp);
                    cmd.SetComputeIntParam(shader, _BFProbeDestOffset, probeBase);
                    cmd.SetComputeIntParam(shader, _BFProbeCopyCount, probeCountInBatch);

                    int pixelCount = probeCountInBatch * (int)BFProbeConfig.StorageOctSize * (int)BFProbeConfig.StorageOctSize;
                    cmd.DispatchCompute(shader, kernel, HDUtils.DivRoundUp(pixelCount, (int)BFProbeConfig.CopyThreadGroupSize), 1, 1);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();
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
            properties.SetTexture(_BFProbeStorage, m_BFProbeStorage);

            int probeCount = Mathf.Min(probes.Length, k_MaxProbeCount);
            var matrices = new List<Matrix4x4>(probeCount);
            for (int probeIndex = 0; probeIndex < probeCount; ++probeIndex)
                matrices.Add(Matrix4x4.Translate(probes[probeIndex].transform.position));
            
            Graphics.DrawMeshInstanced(m_DebugMesh, 0, m_DebugMaterial, matrices.ToArray(), probeCount, properties, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
        }
    }
}
