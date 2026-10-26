#if !UNITY_WEBGL_RENDERER_ONLY
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
#if ENABLE_PROFILER
    #region GRD Debug Suite Types

    /// <summary>
    /// Reason a renderer was excluded from the GRD path.
    /// Keep in sync with GRDExclusionReason in GPUDrivenProcessor.h.
    /// </summary>
    /// <remarks>
    /// WIRE FORMAT: values are received as raw bytes from native via
    /// <c>invalidRendererReason</c> in <see cref="GPUDrivenMeshRendererData"/>. Do not reorder
    /// existing values or change the underlying type — append new values before <see cref="Count"/>,
    /// and mirror the change in the C++ enum.
    /// </remarks>
    internal enum GRDExclusionReason : byte
    {
        None = 0,

        // Actionable — user can fix these
        LODAnimateCrossFading,
        CustomMaterialPropertyBlock,
        RenderCallback,
        NonStandardSortKey,
        ProxyVolumeProbe,
        BlendProbesWithAnchor,
        EnlightenVertexStream,
        MissingDOTSInstancing,
        NullMaterial,        // material slot or shader is null/missing
        TooManySubmeshes,
        MissingMesh,         // no mesh assigned to the renderer
        GPUDrivenDisabled,   // AllowGPUDrivenRendering explicitly set to false

        // Not actionable — renderer type or system-level constraint
        AnimationVisibility,
        TextMeshComponent,
        InactiveOrDisabled,  // inactive GameObject, disabled renderer, or ForceRenderingOff

        Count
    }

    /// <summary>
    /// High-level grouping of <see cref="GRDExclusionReason"/> values for Coverage reporting.
    /// Determines whether a renderer counts toward the Coverage % denominator.
    /// </summary>
    internal enum GRDExclusionCategory : byte
    {
        /// <summary>Renderer is on the GRD path. Numerator of Coverage %.</summary>
        OnPath,
        /// <summary>Renders via the standard SRP path. Counted in the Coverage % denominator
        /// because it represents a real GRD-vs-SRP decision the user can act on.</summary>
        Excluded,
        /// <summary>Asset is broken (missing mesh, null material) — not rendered by any path.
        /// Surfaced as an issue but excluded from Coverage % math.</summary>
        NonRendering,
        /// <summary>GameObject/Renderer is intentionally disabled. Not rendering, not a
        /// GRD-vs-SRP decision. Excluded from Coverage % math.</summary>
        Inactive,

        Count
    }

    /// <summary>
    /// The type of batch about the renderer.
    /// </summary>
    internal enum GRDBatchingType : byte
    {
        Untracked = 0,
        Unbatched = 1,
        GRD = 2,
        SRPBatcher = 3,
        NotRendered = 4,
    }

    internal static class GRDExclusionReasonExtensions
    {
        /// <summary>Maps each exclusion reason to its Coverage category.</summary>
        public static GRDExclusionCategory GetCategory(this GRDExclusionReason reason) => reason switch
        {
            GRDExclusionReason.None               => GRDExclusionCategory.OnPath,
            GRDExclusionReason.NullMaterial       => GRDExclusionCategory.NonRendering,
            GRDExclusionReason.MissingMesh        => GRDExclusionCategory.NonRendering,
            GRDExclusionReason.InactiveOrDisabled => GRDExclusionCategory.Inactive,
            _                                     => GRDExclusionCategory.Excluded,
        };

        /// <summary>Maps each exclusion reason to batch type.</summary>
        public static GRDBatchingType GetBatchingType(this GRDExclusionReason reason) => reason.GetCategory() switch
        {
            GRDExclusionCategory.OnPath => GRDBatchingType.GRD,

            GRDExclusionCategory.NonRendering or GRDExclusionCategory.Inactive => GRDBatchingType.NotRendered,

            // Excluded from the GRD path but still drawn via the standard SRP path.
            // The SRPBatcher/Unbatched split is INFERRED from the exclusion reason — a rough proxy for
            // SRP Batcher eligibility (shader compatibility), not observed from the actual draw.
            // Runtime batch breaking is a separate, draw-order-dependent concern and is not tracked here.
            GRDExclusionCategory.Excluded => reason switch
            {
                GRDExclusionReason.MissingDOTSInstancing => GRDBatchingType.SRPBatcher,
                GRDExclusionReason.LODAnimateCrossFading => GRDBatchingType.SRPBatcher,
                GRDExclusionReason.GPUDrivenDisabled => GRDBatchingType.SRPBatcher,
                _ => GRDBatchingType.Unbatched,
            },

            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, $"Unmapped category for {reason}"),
        };
    }

    /// <summary>
    /// Breakdown of GPU upload timings by dispatch type.
    /// </summary>
    internal struct GRDUploadTimingDetail
    {
        /// <summary>Time in ms for LocalToWorld, WorldToObject, bounding sphere dispatches.</summary>
        public float transformDispatchTime;
        /// <summary>Time in ms for PrevLocalToWorld motion update dispatches.</summary>
        public float motionDispatchTime;
        /// <summary>Time in ms for SH coefficient and occlusion probe dispatches.</summary>
        public float probeDispatchTime;
        /// <summary>Time in ms for GPU component override uploads (lightmap, SpeedTree wind, etc).</summary>
        public float componentOverrideTime;
    }

    /// <summary>
    /// Timings for each stage of the GRD pipeline.
    /// </summary>
    internal struct GRDPipelineStats
    {
        /// <summary>Time in ms for detecting scene object changes.</summary>
        public float dataCollectionTime;
        /// <summary>Time in ms for building draw command batches.</summary>
        public float batchBuildingTime;
        /// <summary>Time in ms for uploading instance data to the GPU.</summary>
        public float cpuToGpuUploadTime;
        /// <summary>Main-thread CPU time in ms spent dispatching the per-camera cull job tree
        /// (CreateCullJobTree). Does NOT include the actual job execution on worker threads.
        /// </summary>
        public float cullingScheduleTime;
        /// <summary>Per-dispatch-type upload timing breakdown.</summary>
        public GRDUploadTimingDetail uploadDetail;
    }

    /// <summary>
    /// Culling statistics aggregated by reason. All counts are per-frame for the camera view.
    /// totalInstances == sum of every other field below; this invariant is what the UI relies on
    /// to render correct percentages.
    /// </summary>
    internal struct GRDCullingStats
    {
        /// <summary>Total instances processed (sum of every other count below).</summary>
        public int totalInstances;
        /// <summary>Instances that passed all culling tests.</summary>
        public int visibleInstances;
        /// <summary>Instances rejected because the renderer is disabled at runtime
        /// (MeshRenderer.enabled=false or GameObject inactive).</summary>
        public int renderingDisabledCount;
        /// <summary>Instances culled by layer mask.</summary>
        public int layerCulledCount;
        /// <summary>Instances culled by frustum / receiver-sphere tests.</summary>
        public int frustumCulledCount;
        /// <summary>Instances culled by LODGroup selection.</summary>
        public int lodGroupCulledCount;
        /// <summary>Instances culled by small mesh threshold.</summary>
        public int smallMeshCulledCount;
        /// <summary>Instances culled by CPU occlusion buffer.</summary>
        public int occlusionCulledCount;
        /// <summary>Instances culled by GPU occlusion (post-CullingJob).</summary>
        public int gpuOcclusionCulledCount;
        /// <summary>Editor scene-mask / selection-outline / shadow-mode mismatch / lightmapped-shadow-caster cuts.</summary>
        public int otherCulledCount;

        /// <summary>Clear all statistics.</summary>
        public void Clear()
        {
            totalInstances = 0;
            visibleInstances = 0;
            renderingDisabledCount = 0;
            layerCulledCount = 0;
            frustumCulledCount = 0;
            lodGroupCulledCount = 0;
            smallMeshCulledCount = 0;
            occlusionCulledCount = 0;
            gpuOcclusionCulledCount = 0;
            otherCulledCount = 0;
        }
    }

    /// <summary>
    /// LOD level distribution statistics. Counts are CPU-side: an instance is bucketed when
    /// it passes CullingJob's visibility + LOD selection, before GPU occlusion runs. So
    /// (lod0 + lod1 + lod2 + lod3Plus) does not exactly equal
    /// <see cref="GRDCullingStats.visibleInstances"/> (which has GPU-occluded subtracted) —
    /// difference is typically small (sub-percent on most scenes).
    /// </summary>
    internal struct GRDLODStats
    {
        /// <summary>Instances at LOD level 0 (highest detail).</summary>
        public int lod0Count;
        /// <summary>Instances at LOD level 1.</summary>
        public int lod1Count;
        /// <summary>Instances at LOD level 2.</summary>
        public int lod2Count;
        /// <summary>Instances at LOD level 3 or higher.</summary>
        public int lod3PlusCount;

        /// <summary>Clear all statistics.</summary>
        public void Clear()
        {
            lod0Count = 0;
            lod1Count = 0;
            lod2Count = 0;
            lod3PlusCount = 0;
        }
    }

    /// <summary>
    /// Batching statistics emitted via ProfilerRecorder for editor consumers.
    /// Populated once per frame from the current draw batch set; cumulative semantics
    /// (latest snapshot persists).
    /// </summary>
    internal struct GRDBatchStats
    {
        /// <summary>Total draw batches (mesh/material combinations).</summary>
        public int drawBatchCount;
        /// <summary>Number of unique meshes across all batches.</summary>
        public int uniqueMeshCount;
        /// <summary>Number of unique materials across all batches.</summary>
        public int uniqueMaterialCount;
        /// <summary>Number of batches with a single instance — instancing yields no win for these.</summary>
        public int singleInstanceBatchCount;

        public void Clear()
        {
            drawBatchCount = 0;
            uniqueMeshCount = 0;
            uniqueMaterialCount = 0;
            singleInstanceBatchCount = 0;
        }
    }

    /// <summary>
    /// Combined debug statistics for GRD.
    /// </summary>
    internal class GRDDebugStats : IDisposable
    {
        /// <summary>Culling statistics by reason.</summary>
        public GRDCullingStats cullingStats;
        /// <summary>LOD distribution statistics.</summary>
        public GRDLODStats lodStats;
        /// <summary>Batch statistics.</summary>
        public GRDBatchStats batchStats;

        // Coverage tracking — source of truth.
        // Mutate only through RegisterGRDRenderers / UnregisterRenderers / RegisterExcludedRenderers.
        private readonly HashSet<EntityId> m_GRDRendererSet = new HashSet<EntityId>();
        private readonly Dictionary<EntityId, GRDExclusionReason> m_ExcludedRendererDict = new Dictionary<EntityId, GRDExclusionReason>();
        private readonly int[] m_PerReasonCount = new int[(int)GRDExclusionReason.Count];
        private readonly int[] m_PerCategoryCount = new int[(int)GRDExclusionCategory.Count]; // indexed by GRDExclusionCategory; OnPath slot unused

        private readonly Dictionary<EntityId, uint> m_SaveShaderValues = new Dictionary<EntityId, uint>();
        private bool m_BatchingTypeViewActive = false;

        /// <summary>Per-frame count of GRD-path renderers whose <c>renderingEnabled</c> bit is
        /// false (toggled disabled at runtime after registration). Updated by
        /// <see cref="GPUResidentDrawer"/> once per frame before counter emit.</summary>
        internal int liveDisabledGRDCount;

        /// <summary>Number of renderers on the GRD path (registered, regardless of runtime
        /// disable state).</summary>
        public int grdRendererCount => m_GRDRendererSet.Count;
        /// <summary>Number of renderers in the <see cref="GRDExclusionCategory.Excluded"/>
        /// category — render via SRP path, count toward Coverage % denominator.</summary>
        public int excludedRendererCount => m_PerCategoryCount[(int)GRDExclusionCategory.Excluded];
        /// <summary>Number of renderers in the <see cref="GRDExclusionCategory.NonRendering"/>
        /// category — broken assets, not rendered by any path.</summary>
        public int nonRenderingRendererCount => m_PerCategoryCount[(int)GRDExclusionCategory.NonRendering];
        /// <summary>Number of renderers that were inactive at registration time. Combine with
        /// <see cref="liveDisabledGRDCount"/> for the total currently-inactive count.</summary>
        public int inactiveAtRegistrationCount => m_PerCategoryCount[(int)GRDExclusionCategory.Inactive];
        /// <summary>Total currently-inactive renderers (snapshot + live-toggled). What the UI
        /// surfaces as "inactive" to the user.</summary>
        public int inactiveRendererCount => inactiveAtRegistrationCount + liveDisabledGRDCount;
        /// <summary>Percentage of GRD-vs-SRP candidates that took the GRD path. Excludes
        /// non-rendering and inactive renderers from the denominator — they aren't competing.</summary>
        public float coveragePercent
        {
            get
            {
                int denom = grdRendererCount + excludedRendererCount;
                return denom > 0 ? (float)grdRendererCount / denom * 100f : 0f;
            }
        }

        /// <summary>Returns the number of excluded renderers for a given reason.</summary>
        public int GetExcludedCountForReason(GRDExclusionReason reason) => m_PerReasonCount[(int)reason];

        public GRDDebugStats()
        {
            cullingStats = new GRDCullingStats();
            lodStats = new GRDLODStats();
            FrameDebuggerGRDLookup.Clear();
        }

        /// <summary>Clear all statistics.</summary>
        public void Clear()
        {
            if (m_BatchingTypeViewActive)
            {
                RestoreBatchingTypeUserValues();
            }

            cullingStats.Clear();
            lodStats.Clear();
            batchStats.Clear();
            m_GRDRendererSet.Clear();
            m_ExcludedRendererDict.Clear();
            System.Array.Clear(m_PerReasonCount, 0, m_PerReasonCount.Length);
            System.Array.Clear(m_PerCategoryCount, 0, m_PerCategoryCount.Length);
            liveDisabledGRDCount = 0;
            FrameDebuggerGRDLookup.Clear();
        }

        public void Dispose() => Clear();

        internal void ApplyBatchingTypeUserValues()
        {
            if (m_BatchingTypeViewActive)
            {
                Debug.LogWarning("GPUResidentDrawer: BatchingType debug view is already active");
                return;
            }

            m_BatchingTypeViewActive = true;
            m_SaveShaderValues.Clear();

            foreach (var id in m_GRDRendererSet)
            {
                SetBatchingTypeUserValue(id, GRDExclusionReason.None);
            }

            foreach (var entityIdAndReason in m_ExcludedRendererDict)
            {
                SetBatchingTypeUserValue(entityIdAndReason.Key, entityIdAndReason.Value);
            }
        }

        internal void RestoreBatchingTypeUserValues()
        {
            m_BatchingTypeViewActive = false;

            foreach (var entityIdAndShaderValue in m_SaveShaderValues)
            {
                if (Resources.EntityIdToObject(entityIdAndShaderValue.Key) is MeshRenderer renderer)
                {
                    renderer.SetShaderUserValue(entityIdAndShaderValue.Value);
                }
            }

            m_SaveShaderValues.Clear();
        }

        private void RestoreBatchingTypeUserValue(EntityId entityId)
        {
            if (!m_SaveShaderValues.Remove(entityId, out var originalShaderValue))
            {
                return;
            }

            if (Resources.EntityIdToObject(entityId) is MeshRenderer renderer)
            {
                renderer.SetShaderUserValue(originalShaderValue);
            }
        }

        private void SetBatchingTypeUserValue(EntityId entityId, GRDExclusionReason reason)
        {
            if (Resources.EntityIdToObject(entityId) is not MeshRenderer renderer)
            {
                return;
            }

            var batchingType = reason.GetBatchingType();
            if (batchingType == GRDBatchingType.NotRendered)
            {
                return;
            }

            if (!m_SaveShaderValues.ContainsKey(entityId))
            {
                var currentShaderValue = renderer.GetShaderUserValue();
                m_SaveShaderValues[entityId] = currentShaderValue;
            }

            renderer.SetShaderUserValue((uint)batchingType);
        }

        private void CheckSetBatchingTypeUserValue(EntityId entityId, GRDExclusionReason reason)
        {
            if (m_BatchingTypeViewActive)
            {
                SetBatchingTypeUserValue(entityId, reason);
            }
        }

        private void CheckRestoreBatchingTypeUserValue(EntityId entityId)
        {
            if (m_BatchingTypeViewActive)
            {
                RestoreBatchingTypeUserValue(entityId);
            }
        }

        internal void UnregisterRenderers(NativeArray<EntityId> ids)
        {
            foreach (var id in ids)
            {
                if (!m_GRDRendererSet.Remove(id) &&
                    m_ExcludedRendererDict.Remove(id, out var reason))
                {
                    m_PerReasonCount[(int)reason]--;
                    m_PerCategoryCount[(int)reason.GetCategory()]--;
                    FrameDebuggerGRDLookup.Remove(id);
                }

                CheckRestoreBatchingTypeUserValue(id);
            }
        }

        private void RegisterGRDRenderers(NativeArray<EntityId> ids)
        {
            foreach (var id in ids)
            {
                if (m_ExcludedRendererDict.Remove(id, out var oldReason))
                {
                    m_PerReasonCount[(int)oldReason]--;
                    m_PerCategoryCount[(int)oldReason.GetCategory()]--;
                    FrameDebuggerGRDLookup.Remove(id);
                }
                m_GRDRendererSet.Add(id); // HashSet.Add is idempotent — re-registering an already-tracked renderer is a no-op.

                CheckSetBatchingTypeUserValue(id, GRDExclusionReason.None);
            }
        }

        private void RegisterExcludedRenderers(NativeArray<EntityId> ids, NativeArray<byte> reasons)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var reason = (GRDExclusionReason)reasons[i];

                m_GRDRendererSet.Remove(id);

                if (m_ExcludedRendererDict.TryGetValue(id, out var oldReason))
                {
                    m_PerReasonCount[(int)oldReason]--;
                    m_PerCategoryCount[(int)oldReason.GetCategory()]--;
                }
                m_PerReasonCount[(int)reason]++;
                m_PerCategoryCount[(int)reason.GetCategory()]++;
                m_ExcludedRendererDict[id] = reason;
                FrameDebuggerGRDLookup.Add(id, (byte)reason);

                CheckSetBatchingTypeUserValue(id, reason);
            }
        }

        /// <summary>
        /// Records renderers as excluded from GRD, all with the same reason.
        /// </summary>
        internal void RegisterExcludedRenderers(NativeArray<EntityId> ids, GRDExclusionReason reason)
        {
            int newCategoryIndex = (int)reason.GetCategory();
            foreach (var id in ids)
            {
                m_GRDRendererSet.Remove(id);

                if (m_ExcludedRendererDict.TryGetValue(id, out var oldReason))
                {
                    m_PerReasonCount[(int)oldReason]--;
                    m_PerCategoryCount[(int)oldReason.GetCategory()]--;
                }
                m_PerReasonCount[(int)reason]++;
                m_PerCategoryCount[newCategoryIndex]++;
                m_ExcludedRendererDict[id] = reason;
                FrameDebuggerGRDLookup.Add(id, (byte)reason);

                CheckSetBatchingTypeUserValue(id, reason);
            }
        }

        /// <summary>
        /// Records the GRD-classification verdict (accepted + excluded with reasons) for a batch
        /// of renderers. Every GRD entry path must call this so counters reflect all considered
        /// renderers regardless of entry.
        /// </summary>
        internal void RecordRenderers(
            NativeArray<EntityId> accepted,
            NativeArray<EntityId> excluded,
            NativeArray<byte> excludedReasons)
        {
            if (excluded.Length > 0)
            {
                Assert.AreEqual(excluded.Length, excludedReasons.Length,
                    "excluded and excludedReasons arrays must have equal length.");
                RegisterExcludedRenderers(excluded, excludedReasons);
            }
            if (accepted.Length > 0)
                RegisterGRDRenderers(accepted);
        }
    }

    #endregion
#endif // ENABLE_PROFILER

    internal struct InstanceCullerViewStats
    {
        public BatchCullingViewType viewType;
        public EntityId viewID;
        public int splitIndex;
        public int visibleInstancesOnCPU;
        public int visibleInstancesOnGPU;
        public int visiblePrimitivesOnCPU;
        public int visiblePrimitivesOnGPU;
        public int drawCommands;
    }

    internal enum InstanceOcclusionEventType
    {
        OcclusionTest,
        OccluderUpdate,
    }

    internal struct InstanceOcclusionEventStats
    {
        public EntityId viewID;
        public InstanceOcclusionEventType eventType;
        public int occluderVersion;
        public int subviewMask;
        public OcclusionTest occlusionTest;
        public int visibleInstances;
        public int culledInstances;
        public int visiblePrimitives;
        public int culledPrimitives;
    }

    internal struct DebugOccluderStats
    {
        public EntityId viewID;
        public int subviewCount;
        public Vector2Int occluderMipLayoutSize;
    }

    internal class DebugRendererBatcherStats : IDisposable
    {
        public NativeList<InstanceCullerViewStats> instanceCullerStats;
        public NativeList<InstanceOcclusionEventStats> instanceOcclusionEventStats;
        public NativeList<DebugOccluderStats> occluderStats;
        // Camera-view only. Shadow / light / SelectionOutline views are excluded so this aligns
        // with visibleInstances (also camera-only) when the consumer subtracts one from the other.
        // Mixing views would push the diff negative on multi-camera scenes.
        public int cameraGPUOcclusionCulled;

        public DebugRendererBatcherStats()
        {
            instanceCullerStats = new NativeList<InstanceCullerViewStats>(Allocator.Persistent);
            instanceOcclusionEventStats = new NativeList<InstanceOcclusionEventStats>(Allocator.Persistent);
            occluderStats = new NativeList<DebugOccluderStats>(Allocator.Persistent);
        }

        public void FinalizeInstanceCullerViewStats()
        {
            cameraGPUOcclusionCulled = 0;

            // For each view, update the on GPU instance and primitive counts. The final rendered primitive and
            // instance count can be found at the last pass of all the occlusion passes.
            for (int viewIndex = 0; viewIndex < instanceCullerStats.Length; viewIndex++)
            {
                InstanceCullerViewStats cullerStats = instanceCullerStats[viewIndex];
                InstanceOcclusionEventStats lastOcclusionEventStats = GetLastInstanceOcclusionEventStatsForView(viewIndex);

                if (lastOcclusionEventStats.viewID == cullerStats.viewID)
                {
                    // The Min test is because the SelectionOutline view (and probably picking as well) share the same viewInstanceID with
                    // the scene camera for instance, so we pick up the camera's occlusion event. And we can't have more instances on GPU than we had on CPU.
                    cullerStats.visibleInstancesOnGPU = Math.Min(lastOcclusionEventStats.visibleInstances, cullerStats.visibleInstancesOnCPU);
                    cullerStats.visiblePrimitivesOnGPU = Math.Min(lastOcclusionEventStats.visiblePrimitives, cullerStats.visiblePrimitivesOnCPU);
                    if (cullerStats.viewType == BatchCullingViewType.Camera)
                        cameraGPUOcclusionCulled += lastOcclusionEventStats.culledInstances;
                }
                else
                {
                    // There was no occlusion culling for this view, so reuse the same counts as on the CPU.
                    cullerStats.visibleInstancesOnGPU = cullerStats.visibleInstancesOnCPU;
                    cullerStats.visiblePrimitivesOnGPU = cullerStats.visiblePrimitivesOnCPU;
                }

                instanceCullerStats[viewIndex] = cullerStats;
            }
        }

        private InstanceOcclusionEventStats GetLastInstanceOcclusionEventStatsForView(int viewIndex)
        {
            if (viewIndex < instanceCullerStats.Length)
            {
                EntityId viewID = instanceCullerStats[viewIndex].viewID;
                for (int passIndex = instanceOcclusionEventStats.Length - 1; passIndex >= 0; passIndex--)
                {
                    if (instanceOcclusionEventStats[passIndex].viewID == viewID)
                        return instanceOcclusionEventStats[passIndex];
                }
            }

            return new InstanceOcclusionEventStats();
        }

        public void Dispose()
        {
            if (instanceCullerStats.IsCreated)
                instanceCullerStats.Dispose();
            if (instanceOcclusionEventStats.IsCreated)
                instanceOcclusionEventStats.Dispose();
            if (occluderStats.IsCreated)
                occluderStats.Dispose();
        }
    }

    internal struct OcclusionCullingDebugOutput
    {
        public RTHandle occluderDepthPyramid;
        public GraphicsBuffer occlusionDebugOverlay;
        public OcclusionCullingDebugShaderVariables cb;
    }
}

#endif // !UNITY_WEBGL_RENDERER_ONLY
