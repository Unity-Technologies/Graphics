using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;
using System.Runtime.InteropServices;
using UnityEditor;

namespace UnityEngine.Rendering
{
    partial class ProbeGIBaking
    {
        #region Sky Occlusion

        public static SamplingResources m_SamplingResources;
        public static DynamicGISkyOcclusionResources m_DynamicGISkyOcclusionResources;
        public static RayTracingContext m_SkyOcclusionRayTracingContext;
        public static IRayTracingAccelStruct m_SkyOcclusionRayTracingAccelerationStructure;
        public static IRayTracingShader m_SkyOcclusionRayTracingShader;
        public static GraphicsBuffer m_RayTracingScratchBuffer;
        public static float m_SkyOcclusionOffsetRay = 0.015f;
        private static bool skyOcclusionBakeStarted = false;
        private static int sampleCountPerFrame = 16;
        private static int currentSampleId;
        private static int bakeSkyShadingDirection;
        private static GraphicsBuffer probePositionsBuffer;
        private static GraphicsBuffer occlusionOutputBuffer;
        private static GraphicsBuffer skyShadingBuffer;
        private static GraphicsBuffer skyShadingIndexBuffer;
        private static ComputeBuffer precomputedShadingDirections;
        private static GraphicsBuffer sobolBuffer;
        private static GraphicsBuffer cprBuffer;// Cranley Patterson rotation

        static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
        static readonly int _SampleId = Shader.PropertyToID("_SampleId");
        static readonly int _CellId = Shader.PropertyToID("_CellId");
        static readonly int _MaxBounces = Shader.PropertyToID("_MaxBounces");
        static readonly int _OffsetRay = Shader.PropertyToID("_OffsetRay");
        static readonly int _ProbePositions = Shader.PropertyToID("_ProbePositions");
        static readonly int _SkyOcclusionOut = Shader.PropertyToID("_SkyOcclusionOut");
        static readonly int _SkyShadingPrecomputedDirection = Shader.PropertyToID("_SkyShadingPrecomputedDirection");
        static readonly int _SkyShadingOut = Shader.PropertyToID("_SkyShadingOut");
        static readonly int _SkyShadingDirectionIndexOut = Shader.PropertyToID("_SkyShadingDirectionIndexOut");
        static readonly int _AverageAlbedo = Shader.PropertyToID("_AverageAlbedo");
        static readonly int _BackFaceCulling = Shader.PropertyToID("_BackFaceCulling");
        static readonly int _BakeSkyShadingDirection = Shader.PropertyToID("_BakeSkyShadingDirection");
        static readonly int _SobolBuffer = Shader.PropertyToID("_SobolBuffer");
        static readonly int _CPRBuffer = Shader.PropertyToID("_CPRBuffer");

        static void CreateSkyOcclusionRayTracingResources()
        {
            ProbeGIBaking.m_DynamicGISkyOcclusionResources = CreateSkyOcclusionResources("Assets/SkyOcclusionResources.asset");

            var packagePath = "Packages/com.unity.rendering.light-transport";
            RayTracingResources resources = ScriptableObject.CreateInstance<RayTracingResources>();
            ResourceReloader.ReloadAllNullIn(resources, packagePath);

            if (RayTracingContext.IsBackendSupported(RayTracingBackend.Hardware))
            {
                ProbeGIBaking.m_SkyOcclusionRayTracingContext = new RayTracingContext(RayTracingBackend.Hardware, resources);
                ProbeGIBaking.m_SkyOcclusionRayTracingShader = ProbeGIBaking.m_SkyOcclusionRayTracingContext.CreateRayTracingShader(ProbeGIBaking.m_DynamicGISkyOcclusionResources.hardwareRayTracingShader);
            }
            else
            {
                ProbeGIBaking.m_SkyOcclusionRayTracingContext = new RayTracingContext(RayTracingBackend.Compute, resources);
                ProbeGIBaking.m_SkyOcclusionRayTracingShader = ProbeGIBaking.m_SkyOcclusionRayTracingContext.CreateRayTracingShader(ProbeGIBaking.m_DynamicGISkyOcclusionResources.rayTracingShader);
            }
            ProbeGIBaking.m_SkyOcclusionRayTracingAccelerationStructure = ProbeGIBaking.m_SkyOcclusionRayTracingContext.CreateAccelerationStructure(new AccelerationStructureOptions());

            ProbeGIBaking.m_SamplingResources = ScriptableObject.CreateInstance<SamplingResources>();
            ResourceReloader.ReloadAllNullIn(ProbeGIBaking.m_SamplingResources, packagePath);
        }

        static void StartBakeSkyOcclusion()
        {
            Debug.Assert(m_BakingBatch.uniqueProbeCount == m_BakingBatch.uniqueProbePositions.Length);

            CreateSkyOcclusionRayTracingResources();

            AddGPURayTracingOccluders();
            m_BakingBatch.skyOcclusionData = new Vector4[m_BakingBatch.uniqueProbeCount];
            if (m_BakingSet.skyOcclusionShadingDirection)
            {
                m_BakingBatch.skyShadingDirectionIndices = new uint[m_BakingBatch.uniqueProbeCount];
            }

            ProbeGIBaking.m_RayTracingScratchBuffer = RayTracingHelper.CreateScratchBufferForBuildAndDispatch(ProbeGIBaking.m_SkyOcclusionRayTracingAccelerationStructure,
                ProbeGIBaking.m_SkyOcclusionRayTracingShader, (uint)m_BakingBatch.uniqueProbeCount, 1, 1);

            var buildCmd = new CommandBuffer();
            ProbeGIBaking.m_SkyOcclusionRayTracingAccelerationStructure.Build(buildCmd, ProbeGIBaking.m_RayTracingScratchBuffer);

            Graphics.ExecuteCommandBuffer(buildCmd);
            buildCmd.Dispose();

            currentSampleId = 0;

            int numProbes = m_BakingBatch.uniqueProbePositions.Length;
            bakeSkyShadingDirection = (m_BakingBatch.skyShadingDirectionIndices != null && m_BakingBatch.skyShadingDirectionIndices.Length == m_BakingBatch.uniqueProbePositions.Length) ? 1 : 0;

            probePositionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numProbes, Marshal.SizeOf<Vector3>());
            occlusionOutputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numProbes, Marshal.SizeOf<Vector4>());
            skyShadingBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (bakeSkyShadingDirection > 0) ? numProbes : 1, Marshal.SizeOf<Vector3>());
            skyShadingIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (bakeSkyShadingDirection > 0) ? numProbes : 1, Marshal.SizeOf<uint>());

            int sobolBufferSize = (int)(UnityEngine.Rendering.UnifiedRayTracing.SobolData.SobolDims * UnityEngine.Rendering.UnifiedRayTracing.SobolData.SobolSize);
            sobolBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, sobolBufferSize, Marshal.SizeOf<uint>());
            sobolBuffer.SetData(UnityEngine.Rendering.UnifiedRayTracing.SobolData.SobolMatrices);

            cprBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, UnityEngine.Rendering.UnifiedRayTracing.SamplingResources.cranleyPattersonRotationBufferSize, Marshal.SizeOf<float>());
            cprBuffer.SetData(UnityEngine.Rendering.UnifiedRayTracing.SamplingResources.GetCranleyPattersonRotations());

            if (bakeSkyShadingDirection > 0)
            {
                if (ProbeReferenceVolume.instance.GetRuntimeResources().SkyPrecomputedDirections == null)
                    ProbeReferenceVolume.instance.InitDynamicSkyPrecomputedDirections();
                precomputedShadingDirections = ProbeReferenceVolume.instance.GetRuntimeResources().SkyPrecomputedDirections;
            }
            else
            {
                precomputedShadingDirections = new ComputeBuffer(1, Marshal.SizeOf<Vector3>());
            }

            probePositionsBuffer.SetData(m_BakingBatch.uniqueProbePositions);
            skyOcclusionBakeStarted = true;
        }
        static void FreeSkyOcclusionBakingResources()
        {
            UnityEditor.Lightmapping.ResetAdditionalBakeDelegate();
            UnityEditor.Experimental.Lightmapping.probesIgnoreDirectEnvironment = false;
            UnityEditor.Experimental.Lightmapping.probesIgnoreIndirectEnvironment = false;

            if (!skyOcclusionBakeStarted)
                return;

            probePositionsBuffer?.Dispose();
            occlusionOutputBuffer?.Dispose();
            skyShadingBuffer?.Dispose();
            skyShadingIndexBuffer?.Dispose();
            sobolBuffer?.Dispose();
            cprBuffer?.Dispose();
            precomputedShadingDirections?.Dispose();

            m_DynamicGISkyOcclusionResources = null;
            ProbeGIBaking.m_SkyOcclusionRayTracingAccelerationStructure.Dispose();
            ProbeGIBaking.m_SkyOcclusionRayTracingContext.Dispose();
            ProbeGIBaking.m_RayTracingScratchBuffer?.Dispose();
            skyOcclusionBakeStarted = false;
        }

        static public UnityEditor.Lightmapping.AdditionalBakeDelegate skyOcclusionDelegate = (ref float progress, ref bool done) =>
        {
            if (m_BakingBatch==null || m_BakingBatch.uniqueProbeCount==0 ||
            m_BakingBatch.uniqueProbePositions == null || m_BakingBatch.uniqueProbePositions.Length == 0)
            {
                progress = 1.0f;
                done = true;
                return;
            }

            progress = 0.0f;
            done = false;

            if (!skyOcclusionBakeStarted)
            {
                StartBakeSkyOcclusion();
                return;
            }

            done = (currentSampleId >= m_BakingSet.skyOcclusionBakingSamples) || (!Lightmapping.isRunning);
            if (done)
            {
                return;
            }

            GatherSkyOcclusionGPU(ProbeGIBaking.m_SkyOcclusionRayTracingContext,
                ProbeGIBaking.m_SkyOcclusionRayTracingShader, ProbeGIBaking.m_RayTracingScratchBuffer,
                m_BakingBatch.uniqueProbePositions.Length,
                currentSampleId, sampleCountPerFrame, m_BakingSet.skyOcclusionBakingSamples,
                m_BakingSet.skyOcclusionBakingBounces, m_BakingSet.skyOcclusionAverageAlbedo, m_BakingSet.skyOcclusionBackFaceCulling,
                bakeSkyShadingDirection,
                m_BakingBatch.skyOcclusionData, m_BakingBatch.skyShadingDirectionIndices);

            currentSampleId += sampleCountPerFrame;
            done = (currentSampleId >= m_BakingSet.skyOcclusionBakingSamples);
            progress = (float)currentSampleId / m_BakingSet.skyOcclusionBakingSamples;


            if (done)
            {
                currentBakingState = BakingStage.SkyOcclusionDone;
            }
            else
            {
                currentBakingState = BakingStage.BakingSkyOcclusion;
            }
        };

        static void GatherSkyOcclusionGPU(RayTracingContext context,
            IRayTracingShader skyOccShader, GraphicsBuffer scratchBuffer,
            int numProbes,
            int startSampleId, int sampleCount, int maxSampleCount,
            int maxBounces, float averageAlbedo, bool backFaceCulling,
            int bakeSkyShadingDirection,
            Vector4[] skyOcclusionDataL0L1, uint[] skyShadingDirectionIndices
                                              )
        {
            Debug.Assert(skyOcclusionDataL0L1.Length > 0);
            Debug.Assert(skyOcclusionDataL0L1.Length == numProbes);

            int backFaceCullingInt = backFaceCulling ? 1 : 0;
            int cellIndex = 0;

            var cmd = new CommandBuffer();

            // Sample all paths (1 per probe) in one pass
            const int sampleCountPerDispatch = 1;
            for (int sampleId = startSampleId; sampleId < startSampleId + sampleCount; sampleId+= sampleCountPerDispatch)
            {
                skyOccShader.SetAccelerationStructure(cmd, "_AccelStruct", ProbeGIBaking.m_SkyOcclusionRayTracingAccelerationStructure);
                RayTracingContext.BindSamplingTextures(cmd, ProbeGIBaking.m_SamplingResources);

                skyOccShader.SetIntParam(cmd, _SampleCount, maxSampleCount);
                skyOccShader.SetIntParam(cmd, _SampleId, sampleId);
                skyOccShader.SetIntParam(cmd, _CellId, cellIndex);
                skyOccShader.SetIntParam(cmd, _MaxBounces, maxBounces);
                skyOccShader.SetFloatParam(cmd, _OffsetRay, m_SkyOcclusionOffsetRay);
                skyOccShader.SetBufferParam(cmd, _ProbePositions, probePositionsBuffer);
                skyOccShader.SetBufferParam(cmd, _SkyOcclusionOut, occlusionOutputBuffer);
                skyOccShader.SetBufferParam(cmd, _SkyShadingPrecomputedDirection, precomputedShadingDirections);
                skyOccShader.SetBufferParam(cmd, _SkyShadingOut, skyShadingBuffer);
                skyOccShader.SetBufferParam(cmd, _SkyShadingDirectionIndexOut, skyShadingIndexBuffer);
                skyOccShader.SetFloatParam(cmd, _AverageAlbedo, averageAlbedo);
                skyOccShader.SetIntParam(cmd, _BackFaceCulling, backFaceCullingInt);
                skyOccShader.SetIntParam(cmd, _BakeSkyShadingDirection, bakeSkyShadingDirection);

                skyOccShader.SetBufferParam(cmd, _SobolBuffer, sobolBuffer);
                skyOccShader.SetBufferParam(cmd, _CPRBuffer, cprBuffer);
                
                skyOccShader.Dispatch(cmd, scratchBuffer,(uint)numProbes, 1, 1);

                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
            occlusionOutputBuffer.GetData(skyOcclusionDataL0L1);

            if (bakeSkyShadingDirection > 0)
            {
                skyShadingIndexBuffer.GetData(skyShadingDirectionIndices);
            }
            cmd.Dispose();
        }

        internal static DynamicGISkyOcclusionResources CreateSkyOcclusionResources(string assetPath)
        {
            var packagePath = "Packages/com.unity.render-pipelines.core";
            DynamicGISkyOcclusionResources resources = UnityEditor.AssetDatabase.LoadAssetAtPath<DynamicGISkyOcclusionResources>(assetPath);
            resources = ScriptableObject.CreateInstance<DynamicGISkyOcclusionResources>();
            ResourceReloader.ReloadAllNullIn(resources, packagePath);

            return resources;
        }

        internal static uint LinearSearchClosestDirection(Vector3[] precomputedDirections, Vector3 direction)
        {
            uint indexMax = 255;
            float bestDot = -10.0f;
            uint bestIndex = 0;

            for (uint index = 0; index < indexMax; index++)
            {
                float currentDot = Vector3.Dot(direction, precomputedDirections[index]);
                if (currentDot > bestDot)
                {
                    bestDot = currentDot;
                    bestIndex = index;
                }
            }
            return bestIndex;
        }

        static void AddGPURayTracingOccluders()
        {
            if (!m_BakingSet.skyOcclusion)
                return;

            for (int sceneIndex = 0; sceneIndex < SceneManagement.SceneManager.sceneCount; ++sceneIndex)
            {
                SceneManagement.Scene scene = SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;

                var contributors = GIContributors.Find(GIContributors.ContributorFilter.All, scene);
                foreach (var renderer in contributors.renderers)
                {
                    var matIndices = GetMaterialIndices(renderer.component);
                    uint mask = GetInstanceMask(renderer.component.shadowCastingMode);

                    if (renderer.component.isPartOfStaticBatch)
                    {
                        Debug.LogError("Static batching should be disabled when using sky occlusion support.");
                    }

                    AddInstance(m_SkyOcclusionRayTracingAccelerationStructure, renderer.component as MeshRenderer, mask, matIndices);
                }
                foreach (var terrain in contributors.terrains)
                {
                    uint matIndex = 0;
                    uint mask = GetInstanceMask(terrain.component.shadowCastingMode);

                    AddInstance(m_SkyOcclusionRayTracingAccelerationStructure, terrain.component, mask, matIndex);
                }
            }
        }
        #endregion
    }
}
