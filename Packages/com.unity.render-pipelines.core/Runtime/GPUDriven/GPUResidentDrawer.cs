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
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Static utility class for updating data post cull in begin camera rendering
    /// </summary>
    public class GPUResidentDrawer
    {
        internal static GPUResidentDrawer instance { get => s_Instance; }
        private static GPUResidentDrawer s_Instance = null;

        ////////////////////////////////////////
        // Public API for rendering pipelines //
        ////////////////////////////////////////

        #region Public API

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

        #endregion

        internal static DebugRendererBatcherStats GetDebugStats()
        {
            return s_Instance?.m_BatchersContext.debugStats;
        }

        private void InsertIntoPlayerLoop()
        {
            var rootLoop = LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            var newList = new List<PlayerLoopSystem>();
            bool isAdded = false;
            for (int i = 0; i < rootLoop.subSystemList.Length; i++)
            {
                // ensure we preserve all existing systems
                newList.Add(rootLoop.subSystemList[i]);

                var type = rootLoop.subSystemList[i].type;

                // We have to update after the PostLateUpdate systems, because we have to be able to get previous matrices from renderers.
                // Previous matrices are updated by renderer managers on UpdateAllRenderers which is part of PostLateUpdate.
                if (!isAdded && type == typeof(PostLateUpdate))
                {
                    PlayerLoopSystem s = default;
                    s.updateDelegate += PostPostLateUpdateStatic;
                    s.type = GetType();
                    newList.Add(s);
                    isAdded = true;
                }
            }

            rootLoop.subSystemList = newList.ToArray();
            LowLevel.PlayerLoop.SetPlayerLoop(rootLoop);

            try
            {
                // We inject to the player loop during the first frame so we have to call PostPostLateUpdate manually here once.
                // If an exception is not caught explicitly here, then the player loop becomes broken in the editor.
                PostPostLateUpdate();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void RemoveFromPlayerLoop()
        {
            var rootLoop = LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            var newList = new List<PlayerLoopSystem>();
            for (int i = 0; i < rootLoop.subSystemList.Length; i++)
            {
                var type = rootLoop.subSystemList[i].type;
                if (type != GetType())
                    newList.Add(rootLoop.subSystemList[i]);
            }

            rootLoop.subSystemList = newList.ToArray();
            LowLevel.PlayerLoop.SetPlayerLoop(rootLoop);
        }

#if UNITY_EDITOR
        private static void OnAssemblyReload()
        {
            if (s_Instance is not null)
                s_Instance.Dispose();
        }
#endif

        internal static bool IsProjectSupported(bool logReason = false)
        {
            bool supported = true;

            // The GPUResidentDrawer only has support when the RawBuffer path of providing data
            // ConstantBuffer path and any other unsupported platforms early out here
            if (BatchRendererGroup.BufferTarget != BatchBufferTarget.RawBuffer)
            {
                if(logReason)
                    Debug.LogWarning($"GPUResidentDrawer: The current platform does not support {BatchBufferTarget.RawBuffer.GetType()}");
                supported = false;
            }

#if UNITY_EDITOR
            if (EditorGraphicsSettings.batchRendererGroupShaderStrippingMode != BatchRendererGroupStrippingMode.KeepAll)
            {
                if(logReason)
                    Debug.LogWarning(
                    "GPUResidentDrawer: \"BatchRendererGroup Variants\" setting must be \"Keep All\". " +
                    " The current setting will cause errors when building a player because all DOTS instancing shaders will be stripped" +
                    " To fix, modify Graphics settings and set \"BatchRendererGroup Variants\" to \"Keep All\".");
                supported = false;
            }
#endif
            return supported;
        }

        internal static bool IsEnabled()
        {
            return s_Instance is not null;
        }

        private static GPUResidentDrawerSettings GetGlobalSettingsFromRPAsset()
        {
            var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (renderPipelineAsset is IGPUResidentRenderPipeline mbAsset)
                return mbAsset.gpuResidentDrawerSettings;

            return new GPUResidentDrawerSettings();
        }

        private static bool IsForcedOnViaCommandLine()
        {
#if UNITY_EDITOR && GPU_RESIDENT_DRAWER_ALLOW_FORCE_ON
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.Equals("-force-gpuresidentdrawer", StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
#endif
            return false;
        }

		internal static void Reinitialize()
        {
			Reinitialize(false);
		}

        internal static void Reinitialize(bool forceSupported)
        {
            var settings = GetGlobalSettingsFromRPAsset();

            // When compiling in the editor, we include a try catch block around our initialization logic to avoid leaving the editor window in a broken state if something goes wrong.
            // We can probably remove this in the future once the edit mode functionality stabilizes, but for now it's safest to have a fallback.
#if UNITY_EDITOR
            try
#endif
            {
                Recreate(settings, forceSupported);
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

        private static void Recreate(GPUResidentDrawerSettings settings, bool forceSupported)
        {
            if (IsForcedOnViaCommandLine())
            {
                forceSupported = true;
                settings.mode = GPUResidentDrawerMode.InstancedDrawing;
            }

            CleanUp();

            // nothing to create
            if (settings.mode == GPUResidentDrawerMode.Disabled)
                return;

#if UNITY_EDITOR
            // In play mode, the GPU Resident Drawer is always allowed.
            // In edit mode, the GPU Resident Drawer is only allowed if the user explicitly requests it with a setting.
            bool isAllowedInCurrentMode = EditorApplication.isPlayingOrWillChangePlaymode || settings.allowInEditMode;
            if (!isAllowedInCurrentMode)
            {
                return;
            }
#endif

            bool supported = true;
            if (!forceSupported)
            {
                var mbAsset = GraphicsSettings.currentRenderPipeline as IGPUResidentRenderPipeline;
                if (mbAsset == null)
                {
                    Debug.LogWarning("GPUResidentDrawer: Disabled due to current render pipeline not being of type IGPUResidentDrawerRenderPipeline");
                    supported = false;
                }
                else
                    supported &= mbAsset.IsGPUResidentDrawerSupportedBySRP(true);
                supported &= IsProjectSupported(true);
            }

            // not supported
            if (!supported)
            {
                Debug.LogWarning("GPUResidentDrawer: Disabled due to platform of support limitation. Please check the log.");
                return;
            }

            s_Instance = new GPUResidentDrawer(settings, 4096, 0);
        }

        internal GPUResidentBatcher batcher { get => m_Batcher; }
        internal GPUResidentDrawerSettings settings { get => m_Settings; }

        private GPUResidentDrawerSettings m_Settings;
        private GPUDrivenProcessor m_GPUDrivenProcessor = null;
        private RenderersBatchersContext m_BatchersContext = null;
        private GPUResidentBatcher m_Batcher = null;

        private ObjectDispatcher m_Dispatcher;

        private MeshRendererDrawer m_MeshRendererDrawer;

#if UNITY_EDITOR
        private NativeList<int> m_FrameCameraIDs;
        private bool m_FrameUpdateNeeded = false;

        static GPUResidentDrawer()
        {
			Lightmapping.bakeCompleted += Reinitialize;
        }
#endif

        private List<Object> m_ChangedMaterials;

        private GPUResidentDrawer(GPUResidentDrawerSettings settings, int maxInstanceCount, int maxTreeInstanceCount)
        {
            var resources = GraphicsSettings.GetRenderPipelineSettings<GPUResidentDrawerResources>();
            var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
            var mbAsset = renderPipelineAsset as IGPUResidentRenderPipeline;
            Debug.Assert(mbAsset != null, "No compatible Render Pipeline found");
            Assert.IsFalse(settings.mode == GPUResidentDrawerMode.Disabled);
            m_Settings = settings;

            var mode = settings.mode;
            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.instanceNumInfo = new InstanceNumInfo(meshRendererNum: maxInstanceCount, speedTreeNum: maxTreeInstanceCount);
            rbcDesc.supportDitheringCrossFade = settings.supportDitheringCrossFade;
            rbcDesc.smallMeshScreenPercentage = settings.smallMeshScreenPercentage;
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
            m_Dispatcher.EnableTypeTracking<LightmapSettings>();
            m_Dispatcher.EnableTypeTracking<Mesh>();
            m_Dispatcher.EnableTypeTracking<Material>();
            m_Dispatcher.EnableTransformTracking<LODGroup>(TransformTrackingType.GlobalTRS);

            m_MeshRendererDrawer = new MeshRendererDrawer(this, m_Dispatcher);
            m_ChangedMaterials = new List<Object>();

#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += OnAssemblyReload;
            m_FrameCameraIDs = new NativeList<int>(1, Allocator.Persistent);
#endif
            SceneManager.sceneLoaded += OnSceneLoaded;

            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
            RenderPipelineManager.endContextRendering += OnEndContextRendering;

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

            RemoveFromPlayerLoop();

            m_ChangedMaterials.Clear();
            m_ChangedMaterials = null;

            m_MeshRendererDrawer.Dispose();
            m_MeshRendererDrawer = null;

            m_Dispatcher.Dispose();
            m_Dispatcher = null;

            s_Instance = null;

            m_Batcher?.Dispose();

            m_BatchersContext.Dispose();
            m_GPUDrivenProcessor.Dispose();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(mode == LoadSceneMode.Additive)
                m_BatchersContext.UpdateAmbientProbeAndGpuBuffer(RenderSettings.ambientProbe, true);
        }

        private static void PostPostLateUpdateStatic()
        {
            s_Instance?.PostPostLateUpdate();
        }

        private void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (s_Instance is null)
                return;
#if UNITY_EDITOR
            EditorFrameUpdate(cameras);
#endif

            m_Batcher.OnBeginContextRendering();
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
        }
#endif
        private void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (s_Instance is null)
                return;

            m_Batcher.OnEndContextRendering();
        }

        private void PostPostLateUpdate()
        {
            Profiler.BeginSample("GPUResidentDrawer.DispatchChanges");
            var lodGroupTransformData = m_Dispatcher.GetTransformChangesAndClear<LODGroup>(TransformTrackingType.GlobalTRS, Allocator.TempJob);
            var lodGroupData = m_Dispatcher.GetTypeChangesAndClear<LODGroup>(Allocator.TempJob, noScriptingArray: true);
            var meshDataSorted = m_Dispatcher.GetTypeChangesAndClear<Mesh>(Allocator.TempJob, sortByInstanceID: true, noScriptingArray: true);
            var lightmapSettingsData = m_Dispatcher.GetTypeChangesAndClear<LightmapSettings>(Allocator.TempJob, noScriptingArray: true);
            m_Dispatcher.GetTypeChangesAndClear<Material>(m_ChangedMaterials, out var changedMateirlasID, out var destroyedMaterialsID, Allocator.TempJob);
            Profiler.EndSample();

            Profiler.BeginSample("GPUResindentDrawer.ProcessLightmapSettings");
            ProcessLightmapSettings(lightmapSettingsData.changedID, lightmapSettingsData.destroyedID);
            Profiler.EndSample();

            Profiler.BeginSample("GPUResindentDrawer.ProcessMaterials");
            ProcessMaterials(m_ChangedMaterials, changedMateirlasID, destroyedMaterialsID);
            Profiler.EndSample();

            Profiler.BeginSample("GPUResindentDrawer.ProcessMeshes");
            ProcessMeshes(meshDataSorted.destroyedID);
            Profiler.EndSample();

            Profiler.BeginSample("GPUResindentDrawer.ProcessLODGroups");
            ProcessLODGroups(lodGroupData.changedID, lodGroupData.destroyedID, lodGroupTransformData.transformedID);
            Profiler.EndSample();

            lodGroupTransformData.Dispose();
            lodGroupData.Dispose();
            meshDataSorted.Dispose();
            lightmapSettingsData.Dispose();
            changedMateirlasID.Dispose();
            destroyedMaterialsID.Dispose();

            Profiler.BeginSample("GPUResindentDrawer.ProcessDraws");
            m_MeshRendererDrawer.ProcessDraws();
            // Add more drawers here ...
            Profiler.EndSample();

            m_BatchersContext.UpdateAmbientProbeAndGpuBuffer(RenderSettings.ambientProbe);
            m_BatchersContext.UpdateInstanceMotions();

            m_Batcher.UpdateFrame();

#if UNITY_EDITOR
            m_FrameUpdateNeeded = false;
#endif
        }

        private void ProcessLightmapSettings(NativeArray<int> changed, NativeArray<int> destroyed)
        {
            if (changed.Length == 0 && destroyed.Length == 0)
                return;

            m_BatchersContext.lightmapManager.RecreateLightmaps();
        }

        private void ProcessMaterials(IList<Object> changed, NativeArray<int> changedID, NativeArray<int> destroyedID)
        {
            if(changedID.Length == 0 && destroyedID.Length == 0)
                return;

            var destroyedLightmappedMaterialsID = new NativeList<int>(Allocator.TempJob);
            m_BatchersContext.lightmapManager.DestroyMaterials(destroyedID, destroyedLightmappedMaterialsID);
            m_Batcher.DestroyMaterials(destroyedLightmappedMaterialsID.AsArray());
            destroyedLightmappedMaterialsID.Dispose();

            m_BatchersContext.lightmapManager.UpdateMaterials(changed, changedID);
            m_Batcher.DestroyMaterials(destroyedID);
        }

        private void ProcessMeshes(NativeArray<int> destroyedID)
        {
            if (destroyedID.Length == 0)
                return;

            var destroyedMeshInstances = new NativeList<InstanceHandle>(Allocator.TempJob);
            ScheduleQueryMeshInstancesJob(destroyedID, destroyedMeshInstances).Complete();
            m_Batcher.DestroyInstances(destroyedMeshInstances.AsArray());
            destroyedMeshInstances.Dispose();

            //@ Some rendererGroupID will not be invalidated when their mesh changed. We will need to update Mesh bounds, probes etc. manually for them.
            m_Batcher.DestroyMeshes(destroyedID);
        }

        private void ProcessLODGroups(NativeArray<int> changedID, NativeArray<int> destroyed, NativeArray<int> transformedID)
        {
            m_BatchersContext.DestroyLODGroups(destroyed);
            m_BatchersContext.UpdateLODGroups(changedID);
            m_BatchersContext.TransformLODGroups(transformedID);
        }

        internal void ProcessRenderers(NativeArray<int> rendererGroupsID)
        {
            Profiler.BeginSample("GPUResindentDrawer.ProcessMeshRenderers");

            var changedInstances = new NativeArray<InstanceHandle>(rendererGroupsID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            ScheduleQueryRendererGroupInstancesJob(rendererGroupsID, changedInstances).Complete();
            m_Batcher.DestroyInstances(changedInstances);
            changedInstances.Dispose();

            m_Batcher.UpdateRenderers(rendererGroupsID);

            Profiler.EndSample();
        }

        internal void TransformInstances(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices)
        {
            Profiler.BeginSample("GPUResindentDrawer.TransformInstances");

            m_BatchersContext.UpdateInstanceTransforms(instances, localToWorldMatrices);

            Profiler.EndSample();
        }

        internal void FreeInstances(NativeArray<InstanceHandle> instances)
        {
            Profiler.BeginSample("GPUResindentDrawer.FreeInstances");

            m_Batcher.DestroyInstances(instances);
            m_BatchersContext.FreeInstances(instances);

            Profiler.EndSample();
        }

        internal void FreeRendererGroupInstances(NativeArray<int> rendererGroupIDs)
        {
            if(rendererGroupIDs.Length == 0)
                return;

            Profiler.BeginSample("GPUResindentDrawer.FreeRendererGroupInstances");

            var instances = new NativeList<InstanceHandle>(rendererGroupIDs.Length, Allocator.TempJob);
            ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instances).Complete();
            m_Batcher.DestroyInstances(instances.AsArray());
            instances.Dispose();

            m_BatchersContext.FreeRendererGroupInstances(rendererGroupIDs);

            Profiler.EndSample();
        }

        //@ Implement later...
        internal InstanceHandle AppendNewInstance(int rendererGroupID, in Matrix4x4 instanceTransform)
        {
            throw new NotImplementedException();
        }

        //@ Additionally we need to implement the way to tie external transforms (not Transform components) with instances.
        //@ So that an individual instance could be transformed externally and then updated in the drawer.

        internal JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeArray<InstanceHandle> instances)
        {
            return m_BatchersContext.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instances);
        }

        internal JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeList<InstanceHandle> instances)
        {
            return m_BatchersContext.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instances);
        }

        internal JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeArray<int> instancesOffset, NativeArray<int> instancesCount, NativeList<InstanceHandle> instances)
        {
            return m_BatchersContext.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instancesOffset, instancesCount, instances);
        }

        internal JobHandle ScheduleQueryMeshInstancesJob(NativeArray<int> sortedMeshIDs, NativeList<InstanceHandle> instances)
        {
            return m_BatchersContext.ScheduleQueryMeshInstancesJob(sortedMeshIDs, instances);
        }
    }
}
