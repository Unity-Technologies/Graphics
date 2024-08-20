#define GPU_RESIDENT_DRAWER_ALLOW_FORCE_ON

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using static UnityEngine.ObjectDispatcher;
using Unity.Jobs;
using static UnityEngine.Rendering.RenderersParameters;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Static utility class for updating data post cull in begin camera rendering
    /// </summary>
    public partial class GPUResidentDrawer
    {
        internal static GPUResidentDrawer instance { get => s_Instance; }
        private static GPUResidentDrawer s_Instance = null;

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
            s_Instance?.batcher.PostCullBeginCameraRendering(context);
        }


        /// <summary>
        /// Utility function for updating probe data after global ambient probe is set up
        /// </summary>
        public static void OnSetupAmbientProbe()
        {
            s_Instance?.batcher.OnSetupAmbientProbe();
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
            s_Instance?.batcher.InstanceOcclusionTest(renderGraph, settings, subviewOcclusionTests);
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
            s_Instance?.batcher.UpdateInstanceOccluders(renderGraph, occluderParameters, occluderSubviewUpdates);
        }

        /// <summary>
        /// Enable or disable GPUResidentDrawer based on the project settings.
        /// We call this every frame because GPUResidentDrawer can be enabled/disabled by the settings outside the render pipeline asset.
        /// </summary>
        public static void ReinitializeIfNeeded()
        {
#if UNITY_EDITOR
            if (!IsForcedOnViaCommandLine() && (IsProjectSupported() != IsEnabled()))
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
        /// <param name="viewInstanceID">The instance ID of the camera using a GPU occlusion test.</param>
        /// <param name="colorBuffer">The color buffer to render the overlay on.</param>
        public static void RenderDebugOcclusionTestOverlay(RenderGraph renderGraph, DebugDisplayGPUResidentDrawer debugSettings, int viewInstanceID, TextureHandle colorBuffer)
        {
            s_Instance?.batcher.occlusionCullingCommon.RenderDebugOcclusionTestOverlay(renderGraph, debugSettings, viewInstanceID, colorBuffer);
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
            s_Instance?.batcher.occlusionCullingCommon.RenderDebugOccluderOverlay(renderGraph, debugSettings, screenPos, maxHeight, colorBuffer);
        }

        #endregion

        internal static DebugRendererBatcherStats GetDebugStats()
        {
            return s_Instance?.m_BatchersContext.debugStats;
        }

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
            if (s_Instance is not null)
                s_Instance.Dispose();
        }
#endif

        internal static bool IsEnabled()
        {
            return s_Instance is not null;
        }

        internal static GPUResidentDrawerSettings GetGlobalSettingsFromRPAsset()
        {
            var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (renderPipelineAsset is not IGPUResidentRenderPipeline mbAsset)
                return new GPUResidentDrawerSettings();

            var settings = mbAsset.gpuResidentDrawerSettings;
            if (IsForcedOnViaCommandLine())
                settings.mode = GPUResidentDrawerMode.InstancedDrawing;

            if (IsOcclusionForcedOnViaCommandLine())
                settings.enableOcclusionCulling = true;

            return settings;
        }

        /// <summary>
        /// Is GRD forced on via the command line via -force-gpuresidentdrawer. Editor only.
        /// </summary>
        /// <returns>true if forced on</returns>
        private static bool IsForcedOnViaCommandLine()
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
        private static bool IsOcclusionForcedOnViaCommandLine()
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
                CleanUp();
            }
#endif
        }

        private static void CleanUp()
        {
            if (s_Instance == null)
                return;

            s_Instance.Dispose();
            s_Instance = null;
        }

        private static void Recreate(GPUResidentDrawerSettings settings)
        {
            CleanUp();
            if (IsGPUResidentDrawerSupportedBySRP(settings, out var message, out var severity))
            {
                s_Instance = new GPUResidentDrawer(settings, 4096, 0);
            }
            else
            {
                LogMessage(message, severity);
            }
        }

        IntPtr m_ContextIntPtr = IntPtr.Zero;

        internal GPUResidentBatcher batcher { get => m_Batcher; }

        internal GPUResidentDrawerSettings settings { get => m_Settings; }

        private GPUResidentDrawerSettings m_Settings;
        private GPUDrivenProcessor m_GPUDrivenProcessor = null;
        private RenderersBatchersContext m_BatchersContext = null;
        private GPUResidentBatcher m_Batcher = null;

        private ObjectDispatcher m_Dispatcher;

#if UNITY_EDITOR
        private static readonly bool s_IsForcedOnViaCommandLine;
        private static readonly bool s_IsOcclusionForcedOnViaCommandLine;

        private NativeList<int> m_FrameCameraIDs;
        private bool m_FrameUpdateNeeded = false;

        private bool m_SelectionChanged;

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

        private GPUResidentDrawer(GPUResidentDrawerSettings settings, int maxInstanceCount, int maxTreeInstanceCount)
        {
            var resources = GraphicsSettings.GetRenderPipelineSettings<GPUResidentDrawerResources>();
            var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
            var mbAsset = renderPipelineAsset as IGPUResidentRenderPipeline;
            Debug.Assert(mbAsset != null, "No compatible Render Pipeline found");
            Assert.IsFalse(settings.mode == GPUResidentDrawerMode.Disabled);
            m_Settings = settings;

            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.instanceNumInfo = new InstanceNumInfo(meshRendererNum: maxInstanceCount, speedTreeNum: maxTreeInstanceCount);
            rbcDesc.supportDitheringCrossFade = settings.supportDitheringCrossFade;
            rbcDesc.smallMeshScreenPercentage = settings.smallMeshScreenPercentage;
            rbcDesc.enableBoundingSpheresInstanceData = settings.enableOcclusionCulling;
            rbcDesc.enableCullerDebugStats = true; // for now, always allow the possibility of reading counter stats from the cullers.

            var instanceCullingBatcherDesc = InstanceCullingBatcherDesc.NewDefault();
#if UNITY_EDITOR
            instanceCullingBatcherDesc.brgPicking = settings.pickingShader;
            instanceCullingBatcherDesc.brgLoading = settings.loadingShader;
            instanceCullingBatcherDesc.brgError = settings.errorShader;
#endif

            m_GPUDrivenProcessor = new GPUDrivenProcessor();
            m_BatchersContext = new RenderersBatchersContext(rbcDesc, m_GPUDrivenProcessor, resources);
            m_Batcher = new GPUResidentBatcher(
                m_BatchersContext,
                instanceCullingBatcherDesc,
                m_GPUDrivenProcessor);

            m_Dispatcher = new ObjectDispatcher();
            m_Dispatcher.EnableTypeTracking<LODGroup>(TypeTrackingFlags.SceneObjects);
            m_Dispatcher.EnableTypeTracking<Mesh>();
            m_Dispatcher.EnableTypeTracking<Material>();
            m_Dispatcher.EnableTransformTracking<LODGroup>(TransformTrackingType.GlobalTRS);
            m_Dispatcher.EnableTypeTracking<MeshRenderer>(TypeTrackingFlags.SceneObjects);
            m_Dispatcher.EnableTransformTracking<MeshRenderer>(TransformTrackingType.GlobalTRS);

#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += OnAssemblyReload;
            m_FrameCameraIDs = new NativeList<int>(1, Allocator.Persistent);
#endif
            SceneManager.sceneLoaded += OnSceneLoaded;

            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
            RenderPipelineManager.endContextRendering += OnEndContextRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
#if UNITY_EDITOR
            Selection.selectionChanged += OnSelectionChanged;
#endif

            // GPU Resident Drawer only supports legacy lightmap binding.
            // Accordingly, we set the keyword globally across all shaders.
            const string useLegacyLightmapsKeyword = "USE_LEGACY_LIGHTMAPS";
            Shader.EnableKeyword(useLegacyLightmapsKeyword);

            InsertIntoPlayerLoop();
        }

        private void Dispose()
        {
            Assert.IsNotNull(s_Instance);

#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= OnAssemblyReload;
            if (m_FrameCameraIDs.IsCreated)
                m_FrameCameraIDs.Dispose();
#endif
            SceneManager.sceneLoaded -= OnSceneLoaded;

            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
            RenderPipelineManager.endContextRendering -= OnEndContextRendering;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
#if UNITY_EDITOR
            Selection.selectionChanged -= OnSelectionChanged;
#endif

            RemoveFromPlayerLoop();

            const string useLegacyLightmapsKeyword = "USE_LEGACY_LIGHTMAPS";
            Shader.DisableKeyword(useLegacyLightmapsKeyword);

            m_Dispatcher.Dispose();
            m_Dispatcher = null;

            s_Instance = null;

            m_Batcher?.Dispose();

            m_BatchersContext.Dispose();
            m_GPUDrivenProcessor.Dispose();

            m_ContextIntPtr = IntPtr.Zero;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Loaded scene might contain light probes that would affect existing objects. Hence we have to update all probes data.
            if(mode == LoadSceneMode.Additive)
                m_BatchersContext.UpdateAmbientProbeAndGpuBuffer(forceUpdate: true);
        }

        private static void PostPostLateUpdateStatic()
        {
            s_Instance?.PostPostLateUpdate();
        }

        private void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (s_Instance is null)
                return;

            // This logic ensures that EditorFrameUpdate is not called more than once after calling BeginContextRendering, unless EndContextRendering has also been called.
            if (m_ContextIntPtr == IntPtr.Zero)
            {
                m_ContextIntPtr = context.Internal_GetPtr();
#if UNITY_EDITOR
                EditorFrameUpdate(cameras);
#endif
                m_Batcher.OnBeginContextRendering();
            }
        }

#if UNITY_EDITOR
        // If running in the editor the player loop might not run
        // In order to still have a single frame update we keep track of the camera ids
        // A frame update happens in case the first camera is rendered again
        private void EditorFrameUpdate(List<Camera> cameras)
        {
            bool newFrame = false;
            foreach (Camera camera in cameras)
            {
                int instanceID = camera.GetInstanceID();
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
                    m_Batcher.UpdateFrame();
                else
                    m_FrameUpdateNeeded = true;
            }

            ProcessSelection();
        }

        private void OnSelectionChanged()
        {
            m_SelectionChanged = true;
        }

        private void ProcessSelection()
        {
            if(!m_SelectionChanged)
                return;

            m_SelectionChanged = false;

            Object[] renderers = Selection.GetFiltered(typeof(MeshRenderer), SelectionMode.Deep);

            var rendererIDs = new NativeArray<int>(renderers.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < renderers.Length; ++i)
                rendererIDs[i] = renderers[i] ? renderers[i].GetInstanceID() : 0;

            m_Batcher.UpdateSelectedRenderers(rendererIDs);

            rendererIDs.Dispose();
        }
#endif

        private void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (s_Instance is null)
                return;
            
            if (m_ContextIntPtr == context.Internal_GetPtr())
            {
                m_ContextIntPtr = IntPtr.Zero;
                m_Batcher.OnEndContextRendering();
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            m_Batcher.OnBeginCameraRendering(camera);
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            m_Batcher.OnEndCameraRendering(camera);
        }

        private void PostPostLateUpdate()
        {
            m_BatchersContext.UpdateAmbientProbeAndGpuBuffer(forceUpdate: false);

            Profiler.BeginSample("GPUResidentDrawer.DispatchChanges");
            var lodGroupTransformData = m_Dispatcher.GetTransformChangesAndClear<LODGroup>(TransformTrackingType.GlobalTRS, Allocator.TempJob);
            var lodGroupData = m_Dispatcher.GetTypeChangesAndClear<LODGroup>(Allocator.TempJob, noScriptingArray: true);
            var meshDataSorted = m_Dispatcher.GetTypeChangesAndClear<Mesh>(Allocator.TempJob, sortByInstanceID: true, noScriptingArray: true);
            var materialData = m_Dispatcher.GetTypeChangesAndClear<Material>(Allocator.TempJob);
            var rendererData = m_Dispatcher.GetTypeChangesAndClear<MeshRenderer>(Allocator.TempJob, noScriptingArray: true);
            Profiler.EndSample();

            Profiler.BeginSample("GPUResidentDrawer.FindUnsupportedMaterials");
            NativeList<int> unsupportedMaterials = FindUnsupportedMaterials(materialData.changedID);
            Profiler.EndSample();

            Profiler.BeginSample("GPUResidentDrawer.FindUnsupportedRenderers");
            NativeList<int> unsupportedRenderers = FindUnsupportedRenderers(unsupportedMaterials.AsArray());
            Profiler.EndSample();

            Profiler.BeginSample("GPUResidentDrawer.ProcessMaterials");
            ProcessMaterials(materialData.destroyedID, unsupportedMaterials.AsArray());
            Profiler.EndSample();

            Profiler.BeginSample("GPUResidentDrawer.ProcessMeshes");
            ProcessMeshes(meshDataSorted.destroyedID);
            Profiler.EndSample();

            Profiler.BeginSample("GPUResidentDrawer.ProcessLODGroups");
            ProcessLODGroups(lodGroupData.changedID, lodGroupData.destroyedID, lodGroupTransformData.transformedID);
            Profiler.EndSample();

            Profiler.BeginSample("GPUResidentDrawer.ProcessRenderers");
            ProcessRenderers(rendererData, unsupportedRenderers.AsArray());
            Profiler.EndSample();

            lodGroupTransformData.Dispose();
            lodGroupData.Dispose();
            meshDataSorted.Dispose();
            materialData.Dispose();
            rendererData.Dispose();
            unsupportedMaterials.Dispose();
            unsupportedRenderers.Dispose();

            m_BatchersContext.UpdateInstanceMotions();

            m_Batcher.UpdateFrame();

#if UNITY_EDITOR
            m_FrameUpdateNeeded = false;
#endif
        }

        private void ProcessMaterials(NativeArray<int> destroyedID, NativeArray<int> unsupportedMaterials)
        {
            if (destroyedID.Length > 0)
                m_Batcher.DestroyMaterials(destroyedID);

            if (unsupportedMaterials.Length > 0)
                m_Batcher.DestroyMaterials(unsupportedMaterials);
        }

        private void ProcessMeshes(NativeArray<int> destroyedID)
        {
            if (destroyedID.Length == 0)
                return;

            var destroyedMeshInstances = new NativeList<InstanceHandle>(Allocator.TempJob);
            ScheduleQueryMeshInstancesJob(destroyedID, destroyedMeshInstances).Complete();
            m_Batcher.DestroyInstances(destroyedMeshInstances.AsArray());
            destroyedMeshInstances.Dispose();

            //@ Check if we need to update instance bounds and light probe sampling positions after mesh is destroyed.
            m_Batcher.DestroyMeshes(destroyedID);
        }

        private void ProcessLODGroups(NativeArray<int> changedID, NativeArray<int> destroyed, NativeArray<int> transformedID)
        {
            m_BatchersContext.DestroyLODGroups(destroyed);
            m_BatchersContext.UpdateLODGroups(changedID);
            m_BatchersContext.TransformLODGroups(transformedID);
        }

        private void ProcessRenderers(TypeDispatchData rendererChanges, NativeArray<int> unsupportedRenderers)
        {
            Profiler.BeginSample("GPUResidentDrawer.ProcessRenderers");

            var changedInstances = new NativeArray<InstanceHandle>(rendererChanges.changedID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            ScheduleQueryRendererGroupInstancesJob(rendererChanges.changedID, changedInstances).Complete();
            m_Batcher.DestroyInstances(changedInstances);
            changedInstances.Dispose();
            m_Batcher.UpdateRenderers(rendererChanges.changedID);

            FreeRendererGroupInstances(rendererChanges.destroyedID, unsupportedRenderers);

            Profiler.EndSample();

            Profiler.BeginSample("GPUResidentDrawer.TransformMeshRenderers");
            var transformChanges = m_Dispatcher.GetTransformChangesAndClear<MeshRenderer>(TransformTrackingType.GlobalTRS, Allocator.TempJob);
            var transformedInstances = new NativeArray<InstanceHandle>(transformChanges.transformedID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            ScheduleQueryRendererGroupInstancesJob(transformChanges.transformedID, transformedInstances).Complete();
            // We can pull localToWorldMatrices directly from the renderers if we are doing update after PostLatUpdate.
            // This will save us transform re computation as matrices are ready inside renderer's TransformInfo.
            TransformInstances(transformedInstances, transformChanges.localToWorldMatrices);
            transformedInstances.Dispose();
            transformChanges.Dispose();
            Profiler.EndSample();
        }

        private void TransformInstances(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices)
        {
            Profiler.BeginSample("GPUResidentDrawer.TransformInstances");

            m_BatchersContext.UpdateInstanceTransforms(instances, localToWorldMatrices);

            Profiler.EndSample();
        }

        private void FreeInstances(NativeArray<InstanceHandle> instances)
        {
            Profiler.BeginSample("GPUResidentDrawer.FreeInstances");

            m_Batcher.DestroyInstances(instances);
            m_BatchersContext.FreeInstances(instances);

            Profiler.EndSample();
        }

        private void FreeRendererGroupInstances(NativeArray<int> rendererGroupIDs, NativeArray<int> unsupportedRendererGroupIDs)
        {
            Profiler.BeginSample("GPUResidentDrawer.FreeRendererGroupInstances");

            m_Batcher.FreeRendererGroupInstances(rendererGroupIDs);

            if (unsupportedRendererGroupIDs.Length > 0)
            {
                m_Batcher.FreeRendererGroupInstances(unsupportedRendererGroupIDs);
                m_GPUDrivenProcessor.DisableGPUDrivenRendering(unsupportedRendererGroupIDs);
            }

            Profiler.EndSample();
        }

        //@ Implement later...
        private InstanceHandle AppendNewInstance(int rendererGroupID, in Matrix4x4 instanceTransform)
        {
            throw new NotImplementedException();
        }

        //@ Additionally we need to implement the way to tie external transforms (not Transform components) with instances.
        //@ So that an individual instance could be transformed externally and then updated in the drawer.

        private JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeArray<InstanceHandle> instances)
        {
            return m_BatchersContext.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instances);
        }

        private JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeList<InstanceHandle> instances)
        {
            return m_BatchersContext.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instances);
        }

        private JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeArray<int> instancesOffset, NativeArray<int> instancesCount, NativeList<InstanceHandle> instances)
        {
            return m_BatchersContext.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instancesOffset, instancesCount, instances);
        }

        private JobHandle ScheduleQueryMeshInstancesJob(NativeArray<int> sortedMeshIDs, NativeList<InstanceHandle> instances)
        {
            return m_BatchersContext.ScheduleQueryMeshInstancesJob(sortedMeshIDs, instances);
        }

        private NativeList<int> FindUnsupportedMaterials(NativeArray<int> changedMaterialIDs)
        {
            NativeList<int> unsupportedMaterials = new NativeList<int>(Allocator.TempJob);

            if (changedMaterialIDs.Length > 0)
            {
                new FindUnsupportedMaterialsJob
                {
                    changedMaterialIDs = changedMaterialIDs,
                    batchMaterialHash = m_Batcher.instanceCullingBatcher.batchMaterialHash,
                    unsupportedMaterialIDs = unsupportedMaterials,
                }.Run();
            }

            return unsupportedMaterials;
        }

        private NativeList<int> FindUnsupportedRenderers(NativeArray<int> unsupportedMaterials)
        {
            NativeList<int> unsupportedRenderers = new NativeList<int>(Allocator.TempJob);

            if (unsupportedMaterials.Length > 0)
            {
                new FindUnsupportedRenderersJob
                {
                    unsupportedMaterials = unsupportedMaterials.AsReadOnly(),
                    materialIDArrays = m_BatchersContext.sharedInstanceData.materialIDArrays,
                    rendererGroups = m_BatchersContext.sharedInstanceData.rendererGroupIDs,
                    unsupportedRenderers = unsupportedRenderers,
                }.Run();
            }

            return unsupportedRenderers;
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private struct FindUnsupportedMaterialsJob : IJob
        {
            [ReadOnly] public NativeParallelHashMap<int, BatchMaterialID> batchMaterialHash;
            [ReadOnly] public NativeArray<int> changedMaterialIDs;

            public NativeList<int> unsupportedMaterialIDs;

            public unsafe void Execute()
            {
                var changedUsedMaterialIDs = new NativeList<int>(4, Allocator.Temp);

                foreach (var materialID in changedMaterialIDs)
                {
                    if (batchMaterialHash.ContainsKey(materialID))
                        changedUsedMaterialIDs.Add(materialID);
                }

                if (changedUsedMaterialIDs.IsEmpty)
                    return;

                unsupportedMaterialIDs.Resize(changedUsedMaterialIDs.Length, NativeArrayOptions.UninitializedMemory);
                int unsupportedMaterialCount = GPUDrivenProcessor.FindUnsupportedMaterialIDs(changedUsedMaterialIDs.AsArray(), unsupportedMaterialIDs.AsArray());
                unsupportedMaterialIDs.Resize(unsupportedMaterialCount, NativeArrayOptions.ClearMemory);
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private struct FindUnsupportedRenderersJob : IJob
        {
            [ReadOnly] public NativeArray<int>.ReadOnly unsupportedMaterials;
            [ReadOnly] public NativeArray<SmallIntegerArray>.ReadOnly materialIDArrays;
            [ReadOnly] public NativeArray<int>.ReadOnly rendererGroups;

            public NativeList<int> unsupportedRenderers;

            public unsafe void Execute()
            {
                if (unsupportedMaterials.Length == 0)
                    return;

                for (int arrayIndex = 0; arrayIndex < materialIDArrays.Length; arrayIndex++)
                {
                    var materialIDs = materialIDArrays[arrayIndex];
                    int rendererID = rendererGroups[arrayIndex];

                    for (int i = 0; i < materialIDs.Length; i++)
                    {
                        int materialID = materialIDs[i];

                        if (unsupportedMaterials.Contains(materialID))
                        {
                            unsupportedRenderers.Add(rendererID);
                            break;
                        }
                    }
                }
            }
        }
    }
}
