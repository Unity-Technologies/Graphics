#define GPU_RESIDENT_DRAWER_ALLOW_FORCE_ON

#if UNITY_EDITOR
#define GPU_RESIDENT_DRAWER_ENABLE_VALIDATION
#endif
//#define GPU_RESIDENT_DRAWER_ENABLE_DEEP_VALIDATION

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    internal struct InternalGPUResidentDrawerSettings
    {
        public RenderPipelineAsset renderPipelineAsset;
        public GPUResidentDrawerResources resources;
        public OnCullingCompleteCallback onCompleteCallback;
        public bool isManagedByUnitTest;

        public static readonly InternalGPUResidentDrawerSettings Default = new InternalGPUResidentDrawerSettings
        {
            renderPipelineAsset = null,
            resources = null,
            onCompleteCallback = null,
            isManagedByUnitTest = false,
        };
    }

    /// <summary>
    /// Static utility class for updating data post cull in begin camera rendering
    /// </summary>
    [BurstCompile]
    public partial class GPUResidentDrawer
    {
#if GPU_RESIDENT_DRAWER_ENABLE_VALIDATION || GPU_RESIDENT_DRAWER_ENABLE_DEEP_VALIDATION
        internal const bool EnableValidation = true;
#else
        internal const bool EnableValidation = false;
#endif

#if GPU_RESIDENT_DRAWER_ENABLE_DEEP_VALIDATION
        internal const bool EnableDeepValidation = true;
#else
        internal const bool EnableDeepValidation = false;
#endif

        internal static bool MaintainContext { get; set; } = false;
        internal static bool ForceOcclusion { get; set; } = false;

        private static GPUResidentDrawer s_Instance = null;

        private static uint s_InstanceVersion = 0;

        ////////////////////////////////////////
        // Public API for rendering pipelines //
        ////////////////////////////////////////

        #region Public API

        /// <summary>
        /// Utility function to test if instance occlusion culling is enabled
        /// </summary>
        /// <returns>True if instance occlusion culling is enabled</returns>
        public static bool IsInstanceOcclusionCullingEnabled()
        {
            if (s_Instance == null)
                return false;

            if (s_Instance.settings.mode != GPUResidentDrawerMode.InstancedDrawing)
                return false;

            if (s_Instance.settings.enableOcclusionCulling)
                return true;

            return false;
        }

        /// <summary>
        /// Utility function for updating data post cull in begin camera rendering
        /// </summary>
        /// <param name="context">
        /// Context containing the data to be set
        /// </param>
        public static void PostCullBeginCameraRendering(RenderRequestBatcherContext context)
        {
            s_Instance?.OnPostCullBeginCameraRendering(context);
        }

        /// <summary>
        /// Utility function for updating probe data after global ambient probe is set up
        /// </summary>
        public static void OnSetupAmbientProbe()
        {
            s_Instance?.UpdateAmbientProbeAndGPUBuffer(forceUpdate: false);
        }

        /// <summary>
        /// Utility function to run an occlusion test in compute to update indirect draws.
        /// This function will dispatch compute shaders to run the given occlusion test and
        /// update all indirect draws in the culling output for the given view.
        /// The next time a renderer list that uses this culling output is drawn, these
        /// indirect draw commands will contain only the instances that passed the given
        /// occlusion test.
        /// </summary>
        /// <param name="renderGraph">Render graph that will have a compute pass added.</param>
        /// <param name="settings">The view to update and occlusion test to use.</param>
        /// <param name="subviewOcclusionTests">Specifies the occluder subviews to use with each culling split index.</param>
        public static void InstanceOcclusionTest(RenderGraph renderGraph, in OcclusionCullingSettings settings, ReadOnlySpan<SubviewOcclusionTest> subviewOcclusionTests)
        {
            if (s_Instance == null || !s_Instance.m_InstanceDataSystem.hasBoundingSpheres)
                return;
            s_Instance.m_Culler.InstanceOcclusionTest(renderGraph, settings, subviewOcclusionTests, s_Instance.m_GRDContext);
        }

        /// <summary>
        /// Utility function used to update occluders using a depth buffer.
        /// This function will dispatch compute shaders to read the given depth buffer
        /// and build a mip pyramid of closest depths for use during occlusion culling.
        /// The next time an occlusion test is issed for this view, instances will be
        /// tested against the updated occluders.
        /// </summary>
        /// <param name="renderGraph">Render graph that will have a compute pass added.</param>
        /// <param name="occluderParameters">Parameter to specify the view and depth buffer to read.</param>
        /// <param name="occluderSubviewUpdates">Specifies which occluder subviews to update from slices of the input depth buffer.</param>
        public static void UpdateInstanceOccluders(RenderGraph renderGraph, in OccluderParameters occluderParameters, ReadOnlySpan<OccluderSubviewUpdate> occluderSubviewUpdates)
        {
            if (s_Instance == null || !s_Instance.m_InstanceDataSystem.hasBoundingSpheres)
                return;
            s_Instance.m_OcclusionCullingCommon.UpdateInstanceOccluders(renderGraph, s_Instance.m_GRDContext, occluderParameters, occluderSubviewUpdates);
        }

        /// <summary>
        /// Enable or disable GPUResidentDrawer based on the project settings.
        /// We call this every frame because GPUResidentDrawer can be enabled/disabled by the settings outside the render pipeline asset.
        /// </summary>
        public static void ReinitializeIfNeeded()
        {
#if UNITY_EDITOR
            if (!IsForcedOnViaCommandLine() && !MaintainContext && (IsProjectSupported() != IsInitialized()))
            {
                Reinitialize();
            }
#endif
        }

        #endregion

        #region Public Debug API

        /// <summary>
        /// Utility function to render an occlusion test heatmap debug overlay.
        /// </summary>
        /// <param name="renderGraph">Render graph that will have a compute pass added.</param>
        /// <param name="debugSettings">The rendering debugger debug settings to read parameters from.</param>
        /// <param name="viewID">The EntityId of the camera using a GPU occlusion test.</param>
        /// <param name="colorBuffer">The color buffer to render the overlay on.</param>
        public static void RenderDebugOcclusionTestOverlay(RenderGraph renderGraph, DebugDisplayGPUResidentDrawer debugSettings, EntityId viewID, TextureHandle colorBuffer)
        {
            s_Instance?.m_OcclusionCullingCommon.RenderDebugOcclusionTestOverlay(renderGraph, debugSettings, viewID, colorBuffer);
        }

        /// <summary>
        /// Utility function visualise the occluder pyramid in a debug overlay.
        /// </summary>
        /// <param name="renderGraph">Render graph that will have a compute pass added.</param>
        /// <param name="debugSettings">The rendering debugger debug settings to read parameters from.</param>
        /// <param name="screenPos">The screen position to render the overlay at.</param>
        /// <param name="maxHeight">The maximum screen height of the overlay.</param>
        /// <param name="colorBuffer">The color buffer to render the overlay on.</param>
        public static void RenderDebugOccluderOverlay(RenderGraph renderGraph, DebugDisplayGPUResidentDrawer debugSettings, Vector2 screenPos, float maxHeight, TextureHandle colorBuffer)
        {
            s_Instance?.m_OcclusionCullingCommon.RenderDebugOccluderOverlay(renderGraph, debugSettings, screenPos, maxHeight, colorBuffer);
        }

        #endregion

        internal static bool IsEnabledFromSettings() => GetGlobalSettingsFromRPAsset().mode != GPUResidentDrawerMode.Disabled;

        internal static bool IsInitialized() => s_Instance != null;

        internal static uint GetInstanceVersion() => s_InstanceVersion;

        internal static NativeReference<GPUArchetypeManager> GetGPUArchetypeManager() => s_Instance != null ? s_Instance.m_InstanceDataSystem.archetypeManager : default;

        internal static ref DefaultGPUComponents GetDefaultGPUComponents() => ref s_Instance.m_InstanceDataSystem.defaultGPUComponents;

        internal static GPUInstanceDataBuffer.ReadOnly GetInstanceDataBuffer() => s_Instance != null ? s_Instance.m_InstanceDataSystem.gpuBuffer.AsReadOnly() : default;

        internal static GPUInstanceDataBufferReadback<T> ReadbackInstanceDataBuffer<T>() where T : unmanaged => s_Instance != null ? s_Instance.m_InstanceDataSystem.ReadbackInstanceDataBuffer<T>() : default;

        internal static DebugRendererBatcherStats GetDebugStats() => s_Instance?.m_GRDContext.debugStats;

        internal static void PushMeshRendererUpdateBatches(NativeArray<MeshRendererUpdateBatch> batches) => s_Instance.m_WorldProcessor.PushMeshRendererUpdateBatches(batches);

        internal static void PushLODGroupUpdateBatches(NativeArray<LODGroupUpdateBatch> batches) => s_Instance.m_WorldProcessor.PushLODGroupUpdateBatches(batches);

        internal static void PushMeshRendererDeletionBatches(NativeArray<NativeArray<EntityId>> batches) => s_Instance.m_WorldProcessor.PushMeshRendererDeletionBatch(batches);

        internal static void PushLODGroupDeletionBatches(NativeArray<NativeArray<EntityId>> batches) => s_Instance.m_WorldProcessor.PushLODGroupDeletionBatch(batches);

        internal static DebugDisplayGPUResidentDrawer debugDisplaySettings => s_Instance?.m_DebugDisplaySettings;

        private void InsertIntoPlayerLoop()
        {
            var rootLoop = LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            bool isAdded = false;

            for (var i = 0; i < rootLoop.subSystemList.Length; i++)
            {
                var subSystem = rootLoop.subSystemList[i];

                // We have to update inside the PostLateUpdate systems, because we have to be able to get previous matrices from renderers.
                // Previous matrices are updated by renderer managers on UpdateAllRenderers which is part of PostLateUpdate.
                if (!isAdded && subSystem.type == typeof(PostLateUpdate))
                {
                    var subSubSystems = new List<PlayerLoopSystem>();
                    foreach (var subSubSystem in subSystem.subSystemList)
                    {
                        if (subSubSystem.type == typeof(PostLateUpdate.FinishFrameRendering))
                        {
                            PlayerLoopSystem s = default;
                            s.updateDelegate += PostPostLateUpdateStatic;
                            s.type = GetType();
                            subSubSystems.Add(s);
                            isAdded = true;
                        }

                        subSubSystems.Add(subSubSystem);
                    }

                    subSystem.subSystemList = subSubSystems.ToArray();
                    rootLoop.subSystemList[i] = subSystem;
                }
            }

            LowLevel.PlayerLoop.SetPlayerLoop(rootLoop);
        }

        private void RemoveFromPlayerLoop()
        {
            var rootLoop = LowLevel.PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < rootLoop.subSystemList.Length; i++)
            {
                var subsystem = rootLoop.subSystemList[i];
                if (subsystem.type != typeof(PostLateUpdate))
                    continue;

                var newList = new List<PlayerLoopSystem>();
                foreach (var subSubSystem in subsystem.subSystemList)
                {
                    if (subSubSystem.type != GetType())
                        newList.Add(subSubSystem);
                }
                subsystem.subSystemList = newList.ToArray();
                rootLoop.subSystemList[i] = subsystem;
            }
            LowLevel.PlayerLoop.SetPlayerLoop(rootLoop);
        }

#if UNITY_EDITOR
        private static void OnAssemblyReload()
        {
            Cleanup();
        }
#endif

        internal static GPUResidentDrawerSettings GetGlobalSettingsFromRPAsset()
        {
            var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (renderPipelineAsset is not IGPUResidentRenderPipeline mbAsset)
                return new GPUResidentDrawerSettings();

            var settings = mbAsset.gpuResidentDrawerSettings;
            if (IsForcedOnViaCommandLine())
                settings.mode = GPUResidentDrawerMode.InstancedDrawing;

            if (IsOcclusionForcedOnViaCommandLine() || ForceOcclusion)
                settings.enableOcclusionCulling = true;

            return settings;
        }

        /// <summary>
        /// Is GRD forced on via the command line via -force-gpuresidentdrawer. Editor only.
        /// </summary>
        /// <returns>true if forced on</returns>
        internal static bool IsForcedOnViaCommandLine()
        {
#if UNITY_EDITOR
            return s_IsForcedOnViaCommandLine;
#else
            return false;
#endif
        }

        /// <summary>
        /// Is occlusion culling forced on via the command line via -force-gpuocclusion. Editor only.
        /// </summary>
        /// <returns>true if forced on</returns>
        internal static bool IsOcclusionForcedOnViaCommandLine()
        {
#if UNITY_EDITOR
            return s_IsOcclusionForcedOnViaCommandLine;
#else
            return false;
#endif
        }

        internal static void Reinitialize()
        {
            var settings = GetGlobalSettingsFromRPAsset();

            // When compiling in the editor, we include a try catch block around our initialization logic to avoid leaving the editor window in a broken state if something goes wrong.
            // We can probably remove this in the future once the edit mode functionality stabilizes, but for now it's safest to have a fallback.
#if UNITY_EDITOR
            try
#endif
            {
                Recreate(settings);
            }
#if UNITY_EDITOR
            catch (Exception exception)
            {
                Debug.LogError($"The GPU Resident Drawer encountered an error during initialization. The standard SRP path will be used instead. [Error: {exception.Message}]");
                Debug.LogError($"GPU Resident drawer stack trace: {exception.StackTrace}");
                Cleanup();
            }
#endif
        }

        private static void Cleanup()
        {
            if (s_Instance == null)
                return;

            s_Instance.Dispose();
            s_Instance = null;
            ++s_InstanceVersion;
        }

        private static void Recreate(GPUResidentDrawerSettings settings)
        {
            Cleanup();

            if (IsGPUResidentDrawerSupportedBySRP(settings, out var message, out var severity))
            {
                s_Instance = new GPUResidentDrawer(settings);
                ++s_InstanceVersion;
            }
            else
            {
                LogMessage(message, severity);
            }
        }

        private IntPtr m_ContextIntPtr = IntPtr.Zero;
        private GPUResidentDrawerSettings m_Settings;
        private InternalGPUResidentDrawerSettings m_InternalSettings;

        // Core Systems
        internal GPUDrivenProcessor m_GPUDrivenProcessor;
        internal ObjectDispatcher m_ObjectDispatcher;
        internal InstanceDataSystem m_InstanceDataSystem;
        internal LODGroupDataSystem m_LODGroupDataSystem;
        internal InstanceCuller m_Culler;
        internal OcclusionCullingCommon m_OcclusionCullingCommon;
        internal InstanceCullingBatcher m_Batcher;
        internal GPUResidentContext m_GRDContext;
        internal SpeedTreeWindGPUDataUpdater m_SpeedTreeWindGPUDataUpdater;
        internal WorldProcessor m_WorldProcessor;

        internal GPUResidentDrawerSettings settings => m_Settings;

        private DebugDisplayGPUResidentDrawer m_DebugDisplaySettings = null;

#if UNITY_EDITOR
        private static readonly bool s_IsForcedOnViaCommandLine;
        private static readonly bool s_IsOcclusionForcedOnViaCommandLine;

        private NativeList<EntityId> m_FrameCameraIDs;
        private bool m_FrameUpdateNeeded = false;

        static GPUResidentDrawer()
        {
            Lightmapping.bakeCompleted += Reinitialize;

#if GPU_RESIDENT_DRAWER_ALLOW_FORCE_ON
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.Equals("-force-gpuresidentdrawer", StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.Log("GPU Resident Drawer forced on via commandline");
                    s_IsForcedOnViaCommandLine = true;
                }
                if (arg.Equals("-force-gpuocclusion", StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.Log("GPU occlusion culling forced on via commandline");
                    s_IsOcclusionForcedOnViaCommandLine = true;
                }
            }
#endif
        }
#endif

        internal GPUResidentDrawer(in GPUResidentDrawerSettings settings) : this(settings, InternalGPUResidentDrawerSettings.Default) {}

        internal GPUResidentDrawer(in GPUResidentDrawerSettings settings, in InternalGPUResidentDrawerSettings internalSettings)
        {
            Assert.IsTrue(settings.mode != GPUResidentDrawerMode.Disabled);

            var resources = internalSettings.resources != null ? internalSettings.resources : GraphicsSettings.GetRenderPipelineSettings<GPUResidentDrawerResources>();
            var renderPipelineAsset = internalSettings.renderPipelineAsset != null ? internalSettings.renderPipelineAsset : GraphicsSettings.currentRenderPipeline;

            if (renderPipelineAsset is not IGPUResidentRenderPipeline)
                Assert.IsTrue(internalSettings.isManagedByUnitTest, "No compatible Render Pipeline found");

            m_Settings = settings;
            m_InternalSettings = internalSettings;
            m_GPUDrivenProcessor = new GPUDrivenProcessor();
            //@ Disable partial rendering for now as it hasn't been widely tested in the main branch.
            //@ This feature was initially developed alongside deferred materials and was controlled by deferred materials settings.
            //@ It enables rendering only the materials and sub-meshes within the same object that are supported by the BRG, while SRP handles the rest.
            m_GPUDrivenProcessor.enablePartialRendering = false;

            m_ObjectDispatcher = new ObjectDispatcher();
            m_InstanceDataSystem = new InstanceDataSystem(1024, settings.enableOcclusionCulling, resources);
            m_LODGroupDataSystem = new LODGroupDataSystem(settings.supportDitheringCrossFade);
            m_Culler = new InstanceCuller();
            m_OcclusionCullingCommon = new OcclusionCullingCommon();
            m_Batcher = new InstanceCullingBatcher();
            m_GRDContext = new GPUResidentContext(settings,
                m_InstanceDataSystem,
                m_LODGroupDataSystem,
                m_Culler,
                m_OcclusionCullingCommon,
                m_Batcher,
                resources);
            m_SpeedTreeWindGPUDataUpdater = new SpeedTreeWindGPUDataUpdater();
            m_WorldProcessor = new WorldProcessor();

            m_ObjectDispatcher.EnableTypeTracking<Mesh>();
            m_ObjectDispatcher.EnableTypeTracking<Material>();
            m_ObjectDispatcher.EnableTypeTracking<MeshRenderer>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            m_ObjectDispatcher.EnableTypeTracking<LODGroup>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            m_ObjectDispatcher.EnableTypeTracking<Camera>(ObjectDispatcher.TypeTrackingFlags.SceneObjects | ObjectDispatcher.TypeTrackingFlags.EditorOnlyObjects);
            m_ObjectDispatcher.EnableTransformTracking<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            m_ObjectDispatcher.EnableTransformTracking<LODGroup>(ObjectDispatcher.TransformTrackingType.GlobalTRS);

            m_Culler.Initialize(resources, m_GRDContext.debugStats);
            m_OcclusionCullingCommon.Initialize(resources);
            m_Batcher.Initialize(m_GRDContext, settings, OnFinishedCulling, internalSettings.onCompleteCallback);
            m_SpeedTreeWindGPUDataUpdater.Initialize(m_InstanceDataSystem, m_Culler);
            m_WorldProcessor.Initialize(m_GPUDrivenProcessor, m_ObjectDispatcher, m_GRDContext);

#if UNITY_EDITOR
            m_FrameCameraIDs = new NativeList<EntityId>(1, Allocator.Persistent);
            if (!internalSettings.isManagedByUnitTest)
                AssemblyReloadEvents.beforeAssemblyReload += OnAssemblyReload;
#endif
            SceneManager.sceneLoaded += OnSceneLoaded;

            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
            RenderPipelineManager.endContextRendering += OnEndContextRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

            if (!internalSettings.isManagedByUnitTest)
                InsertIntoPlayerLoop();

            string extraText = IsForcedOnViaCommandLine() ? " (forced on via commandline)" : "";
            extraText = MaintainContext ? " (forced on via MaintainContext)" : extraText;
            if (settings.enableOcclusionCulling)
            {
                string occlusionText = IsOcclusionForcedOnViaCommandLine() ? " (forced on via commandline)" : "";
                occlusionText = ForceOcclusion ? " (forced on via ForceOcclusion)" : occlusionText;
                extraText = $"{extraText} with GPU Occlusion Culling{occlusionText}";
            }
            Console.WriteLine($"GPU Resident Drawer created{extraText}.");
        }

        internal void Dispose()
        {
            NativeArray<EntityId> rendererIDs = m_InstanceDataSystem.renderWorld.instanceIDs;
            if (rendererIDs.Length > 0)
                m_GPUDrivenProcessor.DisableGPUDrivenRendering(rendererIDs);

#if UNITY_EDITOR
            if (!m_InternalSettings.isManagedByUnitTest)
                AssemblyReloadEvents.beforeAssemblyReload -= OnAssemblyReload;

            if (m_FrameCameraIDs.IsCreated)
                m_FrameCameraIDs.Dispose();
#endif
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Note: Those RenderPipelineManager callbacks do not run when using built-in editor debug views such as lightmap, shadowmask etc
            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
            RenderPipelineManager.endContextRendering -= OnEndContextRendering;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            if (!m_InternalSettings.isManagedByUnitTest)
                RemoveFromPlayerLoop();

            m_WorldProcessor.Dispose();
            m_WorldProcessor = null;
            m_SpeedTreeWindGPUDataUpdater.Dispose();
            m_SpeedTreeWindGPUDataUpdater = null;
            m_Batcher.Dispose();
            m_Batcher = null;
            m_OcclusionCullingCommon?.Dispose();
            m_OcclusionCullingCommon = null;
            m_Culler.Dispose();
            m_Culler = null;
            m_InstanceDataSystem.Dispose();
            m_InstanceDataSystem = null;
            m_LODGroupDataSystem.Dispose();
            m_LODGroupDataSystem = null;
            m_GRDContext.Dispose();
            m_GRDContext = null;
            m_ObjectDispatcher.Dispose();
            m_ObjectDispatcher = null;
            m_GPUDrivenProcessor.Dispose();
            m_GPUDrivenProcessor = null;
            m_ContextIntPtr = IntPtr.Zero;

            Console.WriteLine($"GPU Resident Drawer disposed.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Loaded scene might contain light probes that would affect existing objects. Hence we have to update all probes data.
            if (mode == LoadSceneMode.Additive)
                UpdateAmbientProbeAndGPUBuffer(forceUpdate: true);
        }

        private static void PostPostLateUpdateStatic()
        {
            s_Instance?.PostPostLateUpdate();
        }

#if UNITY_EDITOR
        // If running in the editor the player loop might not run
        // In order to still have a single frame update we keep track of the camera ids
        // A frame update happens in case the first camera is rendered again
        // Note: This doesn't run when using built-in debug views such as lightmaps, shadowmask and etc
        private void EditorFrameUpdate(List<Camera> cameras)
        {
            bool newFrame = false;
            foreach (Camera camera in cameras)
            {
                EntityId instanceID = camera.GetEntityId();
                if (m_FrameCameraIDs.Length == 0 || m_FrameCameraIDs.Contains(instanceID))
                {
                    newFrame = true;
                    m_FrameCameraIDs.Clear();
                }
                m_FrameCameraIDs.Add(instanceID);
            }

            if (newFrame)
            {
                if (m_FrameUpdateNeeded)
                {
                    CullerUpdateFrame();
                }
                else
                {
                    m_FrameUpdateNeeded = true;
                }
            }
        }
#endif

        private void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            // This logic ensures that EditorFrameUpdate is not called more than once after calling BeginContextRendering, unless EndContextRendering has also been called.
            if (m_ContextIntPtr == IntPtr.Zero)
            {
                m_ContextIntPtr = context.Internal_GetPtr();
#if UNITY_EDITOR
                EditorFrameUpdate(cameras);
#endif
                m_SpeedTreeWindGPUDataUpdater.OnBeginContextRendering();
            }

            if (m_DebugDisplaySettings == null)
                m_DebugDisplaySettings = DebugDisplaySerializer.Get<DebugDisplayGPUResidentDrawer>();
        }

        private void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            ParallelBitArray compactedVisibilityMasks = m_Culler.GetCompactedVisibilityMasks(syncCullingJobs: true);

            if (m_ContextIntPtr == context.Internal_GetPtr())
            {
                m_ContextIntPtr = IntPtr.Zero;
                if (compactedVisibilityMasks.IsCreated)
                    m_InstanceDataSystem.UpdatePerFrameInstanceVisibility(compactedVisibilityMasks);
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            m_Culler.OnBeginCameraRendering(camera);
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            m_Culler.OnEndCameraRendering(camera);
        }

        private void OnFinishedCulling(IntPtr customCullingResult)
        {
            m_Culler.EnsureValidOcclusionTestResults(viewID : EntityId.From((ulong)customCullingResult));
            m_SpeedTreeWindGPUDataUpdater.UpdateGPUData();
        }

        private void OnPostCullBeginCameraRendering(RenderRequestBatcherContext context) {}

        private void UpdateAmbientProbeAndGPUBuffer(bool forceUpdate)
        {
            if (forceUpdate || m_GRDContext.cachedAmbientProbe != RenderSettings.ambientProbe)
            {
                m_GRDContext.cachedAmbientProbe = RenderSettings.ambientProbe;
                m_InstanceDataSystem.UpdateAllInstanceProbes();
            }
        }

        private void CullerUpdateFrame()
        {
            m_Culler.UpdateFrame(m_InstanceDataSystem.renderWorld.cameraCount);
            m_OcclusionCullingCommon.UpdateFrame();
            if (m_GRDContext.debugStats != null)
                m_OcclusionCullingCommon.UpdateOccluderStats(m_GRDContext.debugStats);
        }

        private void PostPostLateUpdate()
        {
            UpdateAmbientProbeAndGPUBuffer(forceUpdate: false);
            m_WorldProcessor.Update();
            CullerUpdateFrame();

#if UNITY_EDITOR
            m_FrameUpdateNeeded = false;
#endif
        }
    }
}
