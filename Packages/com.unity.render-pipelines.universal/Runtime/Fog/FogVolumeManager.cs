#if VOLUMETRIC_FOG

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.Rendering.Universal
{
    sealed class FogVolumeManager : IDisposable
    {
        const int k_CapacityGranularity = 16;
        const int k_TransformBatchSize = 32;

        static readonly ProfilingSampler s_BeginContextRenderingSampler = new ProfilingSampler($"{nameof(FogVolumeManager)}.{nameof(OnBeginContextRendering)}");
        static readonly ProfilingSampler s_ScheduleCullingSampler = new ProfilingSampler($"{nameof(FogVolumeManager)}.{nameof(ScheduleCulling)}");
        static readonly ProfilingSampler s_CompleteCullingSampler = new ProfilingSampler($"{nameof(FogVolumeManager)}.{nameof(CompleteCulling)}");

        readonly List<FogVolume> m_FogVolumes = new List<FogVolume>();
        readonly Dictionary<FogVolume, int> m_FogVolumeIndices = new Dictionary<FogVolume, int>();

        TransformAccessArray m_Transforms;
        NativeArray<FogVolumeBox> m_Boxes;
        NativeArray<FogVolumeProperties> m_Properties;
        NativeArray<int> m_Layers;

        NativeArray<FogVolumeSortKey> m_SortKeys;
        NativeArray<FogVolumeData> m_VisibleFogVolumeData;
        NativeArray<FogVolumeBlendMode> m_VisibleBlendModes;
        NativeReference<int> m_VisibleCount;

        JobHandle m_JobHandle;

        internal int fogVolumeCount => m_FogVolumes.Count;
        internal NativeArray<FogVolumeData> visibleFogVolumeData => m_VisibleFogVolumeData;
        internal NativeArray<FogVolumeBlendMode> visibleBlendModes => m_VisibleBlendModes;

        internal FogVolumeManager()
        {
            FogVolume.fogVolumeEnabled += OnFogVolumeEnabled;
            FogVolume.fogVolumeDisabled += OnFogVolumeDisabled;
            FogVolume.fogVolumeChanged += OnFogVolumeChanged;
            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
#if UNITY_EDITOR
            SceneVisibilityManager.visibilityChanged += OnSceneVisibilityChanged;
#endif

            // Runs once on instantiation to pick up fog volumes enabled before the manager existed.
            foreach (var fogVolume in Object.FindObjectsByType<FogVolume>())
            {
                if (fogVolume.isActiveAndEnabled)
                    OnFogVolumeEnabled(fogVolume);
            }
        }

        public void Dispose()
        {
            FogVolume.fogVolumeEnabled -= OnFogVolumeEnabled;
            FogVolume.fogVolumeDisabled -= OnFogVolumeDisabled;
            FogVolume.fogVolumeChanged -= OnFogVolumeChanged;
            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
#if UNITY_EDITOR
            SceneVisibilityManager.visibilityChanged -= OnSceneVisibilityChanged;
#endif

            if (m_FogVolumes.Count > 0)
                Deallocate();

            m_FogVolumes.Clear();
            m_FogVolumeIndices.Clear();
        }

        void OnFogVolumeEnabled(FogVolume fogVolume)
        {
            int index = m_FogVolumes.Count;
            if (!m_FogVolumeIndices.TryAdd(fogVolume, index))
                return;

            if (m_FogVolumes.Count == 0)
            {
                int initialCapacity = GetCapacity(1);
                m_Transforms = new TransformAccessArray(initialCapacity);
                m_Boxes = new NativeArray<FogVolumeBox>(initialCapacity, Allocator.Persistent);
                m_Properties = new NativeArray<FogVolumeProperties>(initialCapacity, Allocator.Persistent);
                m_Layers = new NativeArray<int>(initialCapacity, Allocator.Persistent);
                m_SortKeys = new NativeArray<FogVolumeSortKey>(initialCapacity, Allocator.Persistent);
                m_VisibleFogVolumeData = new NativeArray<FogVolumeData>(initialCapacity, Allocator.Persistent);
                m_VisibleBlendModes = new NativeArray<FogVolumeBlendMode>(initialCapacity, Allocator.Persistent);
                m_VisibleCount = new NativeReference<int>(Allocator.Persistent);
            }

            m_JobHandle.Complete();

            m_FogVolumes.Add(fogVolume);
            if (m_Boxes.Length < m_FogVolumes.Count)
            {
                int capacity = GetCapacity(m_FogVolumes.Count);
                m_Boxes.ResizeArray(capacity);
                m_Properties.ResizeArray(capacity);
                m_Layers.ResizeArray(capacity);
                m_SortKeys.ResizeArray(capacity);
                m_VisibleFogVolumeData.ResizeArray(capacity);
                m_VisibleBlendModes.ResizeArray(capacity);
            }

            m_Transforms.Add(fogVolume.transform);
            m_Boxes[index] = new FogVolumeBox(fogVolume.transform.localToWorldMatrix);
            m_Layers[index] = fogVolume.gameObject.layer;
            WriteProperties(fogVolume, index);
        }

        void OnFogVolumeDisabled(FogVolume fogVolume)
        {
            if (!m_FogVolumeIndices.Remove(fogVolume, out int index))
                return;

            m_JobHandle.Complete();

            int lastIndex = m_FogVolumes.Count - 1;
            var lastFogVolume = m_FogVolumes[lastIndex];
            m_FogVolumes[index] = lastFogVolume;
            m_FogVolumes.RemoveAt(lastIndex);
            if (index != lastIndex)
                m_FogVolumeIndices[lastFogVolume] = index;

            if (m_FogVolumes.Count == 0)
            {
                Deallocate();
                return;
            }

            m_Transforms.RemoveAtSwapBack(index);
            m_Boxes[index] = m_Boxes[lastIndex];
            m_Properties[index] = m_Properties[lastIndex];
            m_Layers[index] = m_Layers[lastIndex];
        }

        void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (m_FogVolumes.Count == 0)
                return;

            using var _ = new ProfilingScope(s_BeginContextRenderingSampler);
            m_JobHandle.Complete();

            for (int i = 0; i < m_FogVolumes.Count; ++i)
                m_Layers[i] = m_FogVolumes[i].gameObject.layer;

            var transformJob = new FogVolumeTransformJob { boxes = m_Boxes };
            m_JobHandle = transformJob.ScheduleReadOnly(m_Transforms, k_TransformBatchSize);
            JobHandle.ScheduleBatchedJobs();
        }

        void OnFogVolumeChanged(FogVolume fogVolume)
        {
            if (!m_FogVolumeIndices.TryGetValue(fogVolume, out int index))
                return;

            m_JobHandle.Complete();
            WriteProperties(fogVolume, index);
        }

#if UNITY_EDITOR
        void OnSceneVisibilityChanged()
        {
            m_JobHandle.Complete();
            for (int i = 0; i < m_FogVolumes.Count; ++i)
                WriteProperties(m_FogVolumes[i], i);
        }
#endif

        internal void ScheduleCulling(
            UniversalCameraData cameraData,
            in VBufferParams vBufferParams,
            float cutoffDistance)
        {
            if (m_FogVolumes.Count == 0)
                return;

            using var _ = new ProfilingScope(s_ScheduleCullingSampler);

            var camera = cameraData.camera;
            var worldToCamera = cameraData.GetViewMatrix();
            var projection = cameraData.GetProjectionMatrixNoJitter();
            var cullingJob = new FogVolumeCullingJob
            {
                boxes = m_Boxes,
                properties = m_Properties,
                layers = m_Layers,
                fogVolumeCount = m_FogVolumes.Count,
                cullingMask = camera.cullingMask,
                worldToClip = projection * worldToCamera,
                worldToGPUClip = GL.GetGPUProjectionMatrix(projection, true) * worldToCamera,
                cameraPosition = camera.transform.position,
                maxDistance = camera.nearClipPlane + cutoffDistance,
                encodingParams = vBufferParams.encodingParams,
                sliceCount = vBufferParams.sliceCount,
                voxelSizeNDC = math.float2(2.0f * vBufferParams.viewportSize.z, 2.0f * vBufferParams.viewportSize.w),
                sortKeys = m_SortKeys,
                fogVolumeData = m_VisibleFogVolumeData,
                blendModes = m_VisibleBlendModes,
                visibleCount = m_VisibleCount,
            };
#if UNITY_EDITOR
            // A hidden prefab context is applied by native culling, not by the camera's scene culling mask.
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (camera.cameraType == CameraType.SceneView &&
                prefabStage != null &&
                CoreUtils.IsSceneViewPrefabStageContextHidden())
                cullingJob.sceneCullingMask = EditorSceneManager.GetSceneCullingMask(prefabStage.scene);
            else
                cullingJob.sceneCullingMask = RenderingUtils.GetSceneCullingMaskFromCamera(camera);
#endif

            m_JobHandle = cullingJob.Schedule(m_JobHandle);
            JobHandle.ScheduleBatchedJobs();
        }

        internal int CompleteCulling()
        {
            using var _ = new ProfilingScope(s_CompleteCullingSampler);
            m_JobHandle.Complete();
            return m_VisibleCount.Value;
        }

        void WriteProperties(FogVolume fogVolume, int index)
        {
            Color albedo = fogVolume.albedo;
            float density = fogVolume.density;

            var properties = new FogVolumeProperties
            {
                scattering = math.float4(albedo.r * density, albedo.g * density, albedo.b * density, density),
                densityTextureTiling = fogVolume.densityTextureTiling,
                densityTextureScrollSpeed = fogVolume.densityTextureScrollSpeed,
                blendPriority = fogVolume.blendPriority,
                entityId = fogVolume.GetEntityId(),
                densityMode = fogVolume.densityMode,
                blendMode = fogVolume.blendMode,
            };
#if UNITY_EDITOR
            var gameObject = fogVolume.gameObject;
            properties.sceneCullingMask = SceneVisibilityManager.instance.IsHidden(gameObject)
                ? SceneCullingMasks.GameViewObjects
                : gameObject.sceneCullingMask;
#endif
            m_Properties[index] = properties;
        }

        internal FogVolume GetVisibleFogVolume(int visibleIndex)
        {
            return m_FogVolumes[m_SortKeys[visibleIndex].fogVolumeIndex];
        }

        void Deallocate()
        {
            m_JobHandle.Complete();
            m_Transforms.Dispose();
            m_Boxes.Dispose();
            m_Properties.Dispose();
            m_Layers.Dispose();
            m_SortKeys.Dispose();
            m_VisibleFogVolumeData.Dispose();
            m_VisibleBlendModes.Dispose();
            m_VisibleCount.Dispose();
        }

        static int GetCapacity(int count)
        {
            return CoreUtils.DivRoundUp(count, k_CapacityGranularity) * k_CapacityGranularity;
        }
    }
}

#endif
