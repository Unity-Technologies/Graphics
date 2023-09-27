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

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering
{
    internal class GPUResidentDrawer
    {
        private static GPUResidentDrawer s_Instance = null;

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

        public static bool IsProjectSupported(bool logReason = false)
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

        public static bool IsEnabled()
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

        private static GPUResidentDrawerResources GetResourcesFromRPAsset()
        {
            var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (renderPipelineAsset is IGPUResidentRenderPipeline mbAsset)
                return mbAsset.gpuResidentDrawerResources;

            return null;
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

        private static void Reinitialize(GPUResidentDrawerMode overrideMode, bool forceSupported = false)
        {
            var settings = GetGlobalSettingsFromRPAsset();
            settings.mode = overrideMode;
            var resources = GetResourcesFromRPAsset();
            Recreate(settings, resources, forceSupported);
        }

		public static void Reinitialize()
        {
			Reinitialize(false);
		}

        public static void Reinitialize(bool forceSupported)
        {
            var settings = GetGlobalSettingsFromRPAsset();
            var resources = GetResourcesFromRPAsset();

            // When compiling in the editor, we include a try catch block around our initialization logic to avoid leaving the editor window in a broken state if something goes wrong.
            // We can probably remove this in the future once the edit mode functionality stabilizes, but for now it's safest to have a fallback.
#if UNITY_EDITOR
            try
#endif
            {
                Recreate(settings, resources, forceSupported);
            }
#if UNITY_EDITOR
            catch (Exception exception)
            {
                Debug.LogError($"The GPU Resident Drawer encountered an error during initialization. The standard SRP path will be used instead. [Error: {exception.Message}]");
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

        private static void Recreate(GPUResidentDrawerSettings settings, GPUResidentDrawerResources resources, bool forceSupported)
        {
            if (IsForcedOnViaCommandLine())
            {
                forceSupported = true;
                settings.mode = GPUResidentDrawerMode.InstancedDrawing;
#if UNITY_EDITOR
                // If we force the batcher on we might not get resources from the SRP global asset
                // Create a temp object here instead.
                if (resources is null)
                {
                    resources = ScriptableObject.CreateInstance<GPUResidentDrawerResources>();
                    resources.hideFlags = HideFlags.HideAndDontSave;
                    ResourceReloader.ReloadAllNullIn(resources, "Packages/com.unity.render-pipelines.core/");
                }
#endif
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

            s_Instance = new GPUResidentDrawer(settings, resources, 4096);
        }

        internal GPUResidentDrawerSettings m_Settings;

        private GPUDrivenProcessor m_GPUDrivenProcessor = null;
        private RenderersBatchersContext m_BatchersContext = null;
        private BaseRendererBatcher m_Batcher = null;

        private ObjectDispatcher m_Dispatcher;

#if UNITY_EDITOR
        static GPUResidentDrawer()
        {
            Reinitialize();

			Lightmapping.bakeCompleted += Reinitialize;
        }
#endif

        private GPUResidentDrawer(GPUResidentDrawerSettings settings, GPUResidentDrawerResources resources, int maxInstanceCount)
        {
            var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
            var mbAsset = renderPipelineAsset as IGPUResidentRenderPipeline;
            Debug.Assert(mbAsset != null, "No compatible Render Pipeline found");
            Assert.IsFalse(settings.mode == GPUResidentDrawerMode.Disabled);
            m_Settings = settings;

            var mode = settings.mode;
            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.maxInstances = maxInstanceCount;
            rbcDesc.supportDitheringCrossFade = settings.supportDitheringCrossFade;

            m_GPUDrivenProcessor = new GPUDrivenProcessor();
            m_BatchersContext = new RenderersBatchersContext(rbcDesc, m_GPUDrivenProcessor, resources);

            Shader brgPicking = null;
#if UNITY_EDITOR
            brgPicking = settings.pickingShader;
#endif
            Shader brgLoading = settings.loadingShader;
            Shader brgError = settings.errorShader;

            m_Batcher = new InstanceCullingBatcher(m_BatchersContext, InstanceCullingBatcherDesc.NewDefault(), m_GPUDrivenProcessor, brgPicking, brgLoading, brgError);

            m_Dispatcher = new ObjectDispatcher();
            m_Dispatcher.EnableTypeTracking<MeshRenderer>(TypeTrackingFlags.SceneObjects);
            m_Dispatcher.EnableTypeTracking<LODGroup>(TypeTrackingFlags.SceneObjects);
            m_Dispatcher.EnableTypeTracking<LightmapSettings>();
            m_Dispatcher.EnableTypeTracking<Mesh>();
            m_Dispatcher.EnableTypeTracking<Material>();
            m_Dispatcher.EnableTransformTracking<MeshRenderer>(TransformTrackingType.GlobalTRS);
            m_Dispatcher.EnableTransformTracking<LODGroup>(TransformTrackingType.GlobalTRS);

#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += OnAssemblyReload;
#endif
            SceneManager.sceneLoaded += OnSceneLoaded;

            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;

            InsertIntoPlayerLoop();
        }

        private void Dispose()
        {
            Assert.IsNotNull(s_Instance);

#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= OnAssemblyReload;
#endif
            SceneManager.sceneLoaded -= OnSceneLoaded;

            RemoveFromPlayerLoop();

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
        }

        private void PostPostLateUpdate()
        {
            Profiler.BeginSample("DispatchChanges");
            var lodGroupTransformData = m_Dispatcher.GetTransformChangesAndClear<LODGroup>(TransformTrackingType.GlobalTRS, Allocator.TempJob);
            var rendererTransformData = m_Dispatcher.GetTransformChangesAndClear<MeshRenderer>(TransformTrackingType.GlobalTRS, Allocator.TempJob);
            var lodGroupData = m_Dispatcher.GetTypeChangesAndClear<LODGroup>(Allocator.TempJob, noScriptingArray: true);
            var rendererData = m_Dispatcher.GetTypeChangesAndClear<MeshRenderer>(Allocator.TempJob, noScriptingArray: true);
            var meshDataSorted = m_Dispatcher.GetTypeChangesAndClear<Mesh>(Allocator.TempJob, sortByInstanceID: true, noScriptingArray: true);
            var materialData = m_Dispatcher.GetTypeChangesAndClear<Material>(Allocator.TempJob);
            var lightmapSettingsData = m_Dispatcher.GetTypeChangesAndClear<LightmapSettings>(Allocator.TempJob, noScriptingArray: true);
            Profiler.EndSample();

            Profiler.BeginSample("QueryInstanceData");
            var changedRendererInstances = new NativeArray<InstanceHandle>(rendererData.changedID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var destroyedRendererInstances = new NativeArray<InstanceHandle>(rendererData.destroyedID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var transformedInstances = new NativeArray<InstanceHandle>(rendererTransformData.transformedID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var changedMeshInstanceIndexPairs = new NativeList<KeyValuePair<InstanceHandle, int>>(Allocator.TempJob);
            var destroyedMeshInstances = new NativeList<InstanceHandle>(Allocator.TempJob);

            m_BatchersContext.QueryInstanceData(rendererData.changedID, rendererData.destroyedID, rendererTransformData.transformedID, meshDataSorted.changedID, meshDataSorted.destroyedID,
                changedRendererInstances, destroyedRendererInstances, transformedInstances, changedMeshInstanceIndexPairs, destroyedMeshInstances);
            Profiler.EndSample();

            Profiler.BeginSample("UpdateLightmapSettings");
            UpdateLightmapSettings(lightmapSettingsData.changedID, lightmapSettingsData.destroyedID);
            Profiler.EndSample();

            Profiler.BeginSample("UpdateMaterials");
            UpdateMaterials((Material[])materialData.changed, materialData.changedID, materialData.destroyedID);
            Profiler.EndSample();

            Profiler.BeginSample("UpdateLODGroups");
            UpdateLODGroups(lodGroupData.changedID, lodGroupData.destroyedID);
            Profiler.EndSample();

            Profiler.BeginSample("UpdateRenderers");
            UpdateRenderers(rendererData.changedID, changedRendererInstances, destroyedRendererInstances, destroyedMeshInstances.AsArray(),
                materialData.destroyedID, meshDataSorted.destroyedID);
            Profiler.EndSample();

            Profiler.BeginSample("TransformLODGroups");
            TransformLODGroups(lodGroupTransformData.transformedID);
            Profiler.EndSample();

            Profiler.BeginSample("TransformRenderers");
            TransformRenderers(transformedInstances, rendererTransformData.localToWorldMatrices);
            Profiler.EndSample();

            // This is very important to free instances after all updates as we don't have InstanceHandle generation yet.
            // So if we free instances and then update, some InstanceHandles might belong to a different instance data, because they were recycled and reallocated.
            Profiler.BeginSample("FreeInstances");
            FreeInstances(destroyedRendererInstances, destroyedMeshInstances.AsArray());
            Profiler.EndSample();

            m_BatchersContext.UpdateAmbientProbeAndGpuBuffer(RenderSettings.ambientProbe);

            changedRendererInstances.Dispose();
            destroyedRendererInstances.Dispose();
            transformedInstances.Dispose();
            changedMeshInstanceIndexPairs.Dispose();
            destroyedMeshInstances.Dispose();

            lodGroupTransformData.Dispose();
            rendererTransformData.Dispose();
            lodGroupData.Dispose();
            rendererData.Dispose();
            meshDataSorted.Dispose();
            materialData.Dispose();
            lightmapSettingsData.Dispose();

            m_Batcher.UpdateFrame();
        }

        private void UpdateLightmapSettings(NativeArray<int> changed, NativeArray<int> destroyed)
        {
            if (changed.Length == 0 && destroyed.Length == 0)
                return;

            m_BatchersContext.lightmapManager.RecreateLightmaps();
        }

        private void UpdateMaterials(Material[] changed, NativeArray<int> changedID, NativeArray<int> destroyedID)
        {
            if (destroyedID.Length > 0)
            {
                var destroyedLightmappedMaterialsID = new NativeList<int>(Allocator.TempJob);
                m_BatchersContext.lightmapManager.DestroyMaterials(destroyedID, destroyedLightmappedMaterialsID);
                m_Batcher.DestroyMaterials(destroyedLightmappedMaterialsID.AsArray());
                destroyedLightmappedMaterialsID.Dispose();
            }

            if (changed.Length > 0)
            {
                m_BatchersContext.lightmapManager.UpdateMaterials(changed, changedID);
            }
        }

        private void UpdateRenderers(NativeArray<int> changedID, NativeArray<InstanceHandle> changedRendererInstances,
            NativeArray<InstanceHandle> destroyedRendererInstances, NativeArray<InstanceHandle> destroyedMeshInstances,
            NativeArray<int> destroyedMaterials, NativeArray<int> destroyedMeshes)
        {
            Profiler.BeginSample("UpdateRenderers");

            var destroyedInstances = new NativeList<InstanceHandle>(changedRendererInstances.Length + destroyedRendererInstances.Length + destroyedMeshInstances.Length, Allocator.TempJob);
            destroyedInstances.AddRange(changedRendererInstances);
            destroyedInstances.AddRange(destroyedRendererInstances);
            destroyedInstances.AddRange(destroyedMeshInstances);

            m_Batcher.DestroyInstances(destroyedInstances.AsArray());
            m_Batcher.DestroyMaterials(destroyedMaterials);
            m_Batcher.DestroyMeshes(destroyedMeshes);

            destroyedInstances.Dispose();

            Profiler.EndSample();

            Profiler.BeginSample("Batcher.UpdateRenderers");
            m_Batcher.UpdateRenderers(changedID);
            Profiler.EndSample();
        }

        private void UpdateLODGroups(NativeArray<int> changedID, NativeArray<int> destroyed)
        {
            if (changedID.Length == 0 && destroyed.Length == 0)
                return;

            m_BatchersContext.DestroyLODGroups(destroyed);
            m_BatchersContext.UpdateLODGroups(changedID);
        }

        public void TransformRenderers(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices)
        {
            m_BatchersContext.TransformInstances(instances, localToWorldMatrices);
        }

        public void TransformLODGroups(NativeArray<int> lodGroupsID)
        {
            m_BatchersContext.TransformLODGroups(lodGroupsID);
        }

        private void FreeInstances(NativeArray<InstanceHandle> destroyedRendererInstances, NativeArray<InstanceHandle> destroyedMeshInstances)
        {
            m_BatchersContext.FreeInstances(destroyedRendererInstances);
            m_BatchersContext.FreeInstances(destroyedMeshInstances);
        }
    }
}
