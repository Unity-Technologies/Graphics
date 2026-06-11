#if !UNITY_WEBGL_RENDERER_ONLY && ENABLE_PROFILER
using Unity.Profiling;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// ProfilerCounter declarations for GPU Resident Drawer.
    /// Emitted every frame in PostPostLateUpdate; read by the GRD Profiler module in the Editor.
    /// </summary>
    /// <remarks>
    /// Counters land under "User &gt; GPU Resident Drawer" in the Profiler Module Editor. Same
    /// grouping convention other Unity-shipped C# packages use (Entities, NetCode, …). Unity's
    /// builtin "Render" group is reserved for counters registered from native (C++); see
    /// <c>ProfilerUnsafeUtility.bindings.cpp</c> — managed CreateCounterValue auto-applies
    /// <c>Marker::kScriptUser</c>, forcing user classification regardless of the marker flags
    /// passed from C#.
    /// </remarks>
    static class GRDProfilerCounters
    {
        internal const string k_CategoryName = "GPU Resident Drawer";
        internal static readonly ProfilerCategory k_Category = new(k_CategoryName);

        // --- Pipeline Timing (top-level stages) ---
        // All four stages are main-thread CPU time. The Culling Schedule stage measures only
        // the main-thread time spent building the per-camera cull job graph (CreateCullJobTree);
        // actual job execution is not included — see GRDPipelineStats.cullingScheduleTime.
        internal const string k_DataCollection = "Data Collection";
        internal const string k_BatchBuilding = "Batch Building";
        internal const string k_CpuToGpuUpload = "CPU to GPU Upload";
        internal const string k_CullingSchedule = "Culling Schedule";

        // --- Pipeline Timing (upload sub-breakdown) ---
        internal const string k_TransformDispatch = "Transform Dispatch";
        internal const string k_MotionDispatch = "Motion Dispatch";
        internal const string k_ProbeDispatch = "Probe Dispatch";
        internal const string k_ComponentOverride = "Component Override";

        // --- Coverage ---
        // GRD vs Excluded are the two segments competing for the GRD path; CoveragePercent is
        // GRD / (GRD + Excluded). NonRendering and Inactive are tracked but kept out of the
        // ratio because they don't represent a GRD-vs-SRP decision (broken assets / disabled
        // GameObjects respectively).
        internal const string k_GRDRenderers = "GRD Renderers";
        internal const string k_ExcludedRenderers = "Excluded Renderers";
        internal const string k_NonRenderingRenderers = "Non-Rendering Renderers";
        internal const string k_InactiveRenderers = "Inactive Renderers";
        internal const string k_CoveragePercent = "Coverage %";

        // --- Culling --- (sum of the 9 sub-counters below == k_TotalInstances by construction;
        // k_TotalInstances is emitted explicitly so consumers can use it as a denominator
        // without having to know — and stay in sync with — the full sub-counter set.)
        internal const string k_TotalInstances = "Total Instances";
        internal const string k_VisibleInstances = "Visible Instances";
        internal const string k_DisabledRendererCulled = "Disabled Renderer Culled";
        internal const string k_LayerCulled = "Layer Culled";
        internal const string k_FrustumCulled = "Frustum Culled";
        internal const string k_OcclusionCulled = "Occlusion Culled";
        internal const string k_GpuOcclusionCulled = "GPU Occlusion Culled";
        internal const string k_LODGroupCulled = "LOD Group Culled";
        internal const string k_SmallMeshCulled = "Small Mesh Culled";
        internal const string k_OtherCulled = "Other Culled";

        // --- LOD Distribution ---
        internal const string k_LOD0 = "LOD 0";
        internal const string k_LOD1 = "LOD 1";
        internal const string k_LOD2 = "LOD 2";
        internal const string k_LOD3Plus = "LOD 3+";

        // --- Exclusion Reasons (per-reason breakdown) ---
        internal const string k_ExclLODAnimateCrossFading = "Excl: LOD Animate CrossFading";
        internal const string k_ExclCustomMaterialPropertyBlock = "Excl: Custom MaterialPropertyBlock";
        internal const string k_ExclRenderCallback = "Excl: Render Callback";
        internal const string k_ExclNonStandardSortKey = "Excl: Non-Standard Sort Key";
        internal const string k_ExclProxyVolumeProbe = "Excl: Proxy Volume Probe";
        internal const string k_ExclBlendProbesWithAnchor = "Excl: Blend Probes With Anchor";
        internal const string k_ExclEnlightenVertexStream = "Excl: Enlighten Vertex Stream";
        internal const string k_ExclMissingDOTSInstancing = "Excl: Missing DOTS Instancing";
        internal const string k_ExclNullMaterial = "Excl: Null Material";
        internal const string k_ExclTooManySubmeshes = "Excl: Too Many Submeshes";
        internal const string k_ExclMissingMesh = "Excl: Missing Mesh";
        internal const string k_ExclGPUDrivenDisabled = "Excl: GPU Driven Disabled";
        internal const string k_ExclAnimationVisibility = "Excl: Animation Visibility";
        internal const string k_ExclTextMeshComponent = "Excl: TextMesh Component";
        internal const string k_ExclInactiveOrDisabled = "Excl: Inactive Or Disabled";

        /// <summary>
        /// Maps GRDExclusionReason enum to counter name. Index = (int)reason.
        /// None (0) maps to null.
        /// </summary>
        internal static readonly string[] k_ExclusionReasonCounterNames = new string[(int)GRDExclusionReason.Count]
        {
            null, // None
            k_ExclLODAnimateCrossFading,
            k_ExclCustomMaterialPropertyBlock,
            k_ExclRenderCallback,
            k_ExclNonStandardSortKey,
            k_ExclProxyVolumeProbe,
            k_ExclBlendProbesWithAnchor,
            k_ExclEnlightenVertexStream,
            k_ExclMissingDOTSInstancing,
            k_ExclNullMaterial,
            k_ExclTooManySubmeshes,
            k_ExclMissingMesh,
            k_ExclGPUDrivenDisabled,
            k_ExclAnimationVisibility,
            k_ExclTextMeshComponent,
            k_ExclInactiveOrDisabled,
        };

        // ===== Counter instances =====

        // Pipeline Timing (TimeNanoseconds for StackedTimeArea chart compatibility)
        static readonly ProfilerCounterValue<long> s_DataCollection = NewTimingCounter(k_DataCollection);
        static readonly ProfilerCounterValue<long> s_BatchBuilding = NewTimingCounter(k_BatchBuilding);
        static readonly ProfilerCounterValue<long> s_CpuToGpuUpload = NewTimingCounter(k_CpuToGpuUpload);
        static readonly ProfilerCounterValue<long> s_CullingSchedule = NewTimingCounter(k_CullingSchedule);

        // Upload sub-breakdown
        static readonly ProfilerCounterValue<long> s_TransformDispatch = NewTimingCounter(k_TransformDispatch);
        static readonly ProfilerCounterValue<long> s_MotionDispatch = NewTimingCounter(k_MotionDispatch);
        static readonly ProfilerCounterValue<long> s_ProbeDispatch = NewTimingCounter(k_ProbeDispatch);
        static readonly ProfilerCounterValue<long> s_ComponentOverride = NewTimingCounter(k_ComponentOverride);

        // Coverage — % is exposed as float so the chart legend reads "94.32%" not "94%".
        static readonly ProfilerCounterValue<int> s_GRDRenderers = NewCumulativeCounter(k_GRDRenderers);
        static readonly ProfilerCounterValue<int> s_ExcludedRenderers = NewCumulativeCounter(k_ExcludedRenderers);
        static readonly ProfilerCounterValue<int> s_NonRenderingRenderers = NewCumulativeCounter(k_NonRenderingRenderers);
        static readonly ProfilerCounterValue<int> s_InactiveRenderers = NewCumulativeCounter(k_InactiveRenderers);
        static readonly ProfilerCounterValue<float> s_CoveragePercent = NewPercentCounter(k_CoveragePercent);

        // Culling
        static readonly ProfilerCounterValue<int> s_TotalInstances = NewPerFrameCounter(k_TotalInstances);
        static readonly ProfilerCounterValue<int> s_VisibleInstances = NewPerFrameCounter(k_VisibleInstances);
        static readonly ProfilerCounterValue<int> s_RenderingDisabledCulled = NewPerFrameCounter(k_DisabledRendererCulled);
        static readonly ProfilerCounterValue<int> s_LayerCulled = NewPerFrameCounter(k_LayerCulled);
        static readonly ProfilerCounterValue<int> s_FrustumCulled = NewPerFrameCounter(k_FrustumCulled);
        static readonly ProfilerCounterValue<int> s_OcclusionCulled = NewPerFrameCounter(k_OcclusionCulled);
        static readonly ProfilerCounterValue<int> s_GpuOcclusionCulled = NewPerFrameCounter(k_GpuOcclusionCulled);
        static readonly ProfilerCounterValue<int> s_LODGroupCulled = NewPerFrameCounter(k_LODGroupCulled);
        static readonly ProfilerCounterValue<int> s_SmallMeshCulled = NewPerFrameCounter(k_SmallMeshCulled);
        static readonly ProfilerCounterValue<int> s_OtherCulled = NewPerFrameCounter(k_OtherCulled);

        // LOD Distribution
        static readonly ProfilerCounterValue<int> s_LOD0 = NewPerFrameCounter(k_LOD0);
        static readonly ProfilerCounterValue<int> s_LOD1 = NewPerFrameCounter(k_LOD1);
        static readonly ProfilerCounterValue<int> s_LOD2 = NewPerFrameCounter(k_LOD2);
        static readonly ProfilerCounterValue<int> s_LOD3Plus = NewPerFrameCounter(k_LOD3Plus);

        // Exclusion Reasons (per-reason)
        static readonly ProfilerCounterValue<int>[] s_ExclusionReasonCounters = CreateExclusionReasonCounters();

        static ProfilerCounterValue<int>[] CreateExclusionReasonCounters()
        {
            // Index 0 (None) is intentionally left as default(ProfilerCounterValue<int>) — "not
            // excluded" has no associated counter. EmitAll iterates from i=1 so the empty slot
            // is never written.
            var counters = new ProfilerCounterValue<int>[(int)GRDExclusionReason.Count];
            for (int i = 1; i < (int)GRDExclusionReason.Count; i++)
            {
                var name = k_ExclusionReasonCounterNames[i];
                if (name != null)
                    counters[i] = NewCumulativeCounter(name);
            }
            return counters;
        }

        // ===== Registration =====

        // Touching the type forces all static field initializers to run, which is how
        // ProfilerCounterValue<T> instances get registered with the Profiler. Without this,
        // counters only appear in the Profiler's Module Editor after the first frame where
        // Profiler.enabled is true (i.e. after recording starts) — confusing because GRD is
        // running but its counters are missing from the "Add Counter" dropdown.
        internal static void EnsureRegistered() { }

        // ===== Emit =====

        internal static void EmitAll(GRDDebugStats stats, GRDPipelineStats pipelineStats)
        {
            if (stats == null)
                return;

            // Pipeline timing (ms → nanoseconds for StackedTimeArea)
            s_DataCollection.Value = MsToNs(pipelineStats.dataCollectionTime);
            s_BatchBuilding.Value = MsToNs(pipelineStats.batchBuildingTime);
            s_CpuToGpuUpload.Value = MsToNs(pipelineStats.cpuToGpuUploadTime);
            s_CullingSchedule.Value = MsToNs(pipelineStats.cullingScheduleTime);

            // Upload sub-breakdown
            s_TransformDispatch.Value = MsToNs(pipelineStats.uploadDetail.transformDispatchTime);
            s_MotionDispatch.Value = MsToNs(pipelineStats.uploadDetail.motionDispatchTime);
            s_ProbeDispatch.Value = MsToNs(pipelineStats.uploadDetail.probeDispatchTime);
            s_ComponentOverride.Value = MsToNs(pipelineStats.uploadDetail.componentOverrideTime);

            // Coverage
            s_GRDRenderers.Value = stats.grdRendererCount;
            s_ExcludedRenderers.Value = stats.excludedRendererCount;
            s_NonRenderingRenderers.Value = stats.nonRenderingRendererCount;
            s_InactiveRenderers.Value = stats.inactiveRendererCount;
            s_CoveragePercent.Value = stats.coveragePercent;

            // Culling
            s_TotalInstances.Value = stats.cullingStats.totalInstances;
            s_VisibleInstances.Value = stats.cullingStats.visibleInstances;
            s_RenderingDisabledCulled.Value = stats.cullingStats.renderingDisabledCount;
            s_LayerCulled.Value = stats.cullingStats.layerCulledCount;
            s_FrustumCulled.Value = stats.cullingStats.frustumCulledCount;
            s_OcclusionCulled.Value = stats.cullingStats.occlusionCulledCount;
            s_GpuOcclusionCulled.Value = stats.cullingStats.gpuOcclusionCulledCount;
            s_LODGroupCulled.Value = stats.cullingStats.lodGroupCulledCount;
            s_SmallMeshCulled.Value = stats.cullingStats.smallMeshCulledCount;
            s_OtherCulled.Value = stats.cullingStats.otherCulledCount;

            // LOD distribution
            s_LOD0.Value = stats.lodStats.lod0Count;
            s_LOD1.Value = stats.lodStats.lod1Count;
            s_LOD2.Value = stats.lodStats.lod2Count;
            s_LOD3Plus.Value = stats.lodStats.lod3PlusCount;

            // Exclusion reasons
            for (int i = 1; i < (int)GRDExclusionReason.Count; i++)
                s_ExclusionReasonCounters[i].Value = stats.GetExcludedCountForReason((GRDExclusionReason)i);
        }

        // ===== Test hooks =====

        // Returns every counter name this class registers, in stable enumeration order.
        // Used by GRDProfilerCounterNamesTests to verify the editor-side mirror in
        // Modules/ProfilerEditor/.../GRDCounterNames.cs has not drifted.
        internal static string[] GetCounterNamesForValidation()
        {
            var names = new System.Collections.Generic.List<string>(48)
            {
                k_DataCollection, k_BatchBuilding, k_CpuToGpuUpload, k_CullingSchedule,
                k_TransformDispatch, k_MotionDispatch, k_ProbeDispatch, k_ComponentOverride,
                k_GRDRenderers, k_ExcludedRenderers, k_NonRenderingRenderers, k_InactiveRenderers, k_CoveragePercent,
                k_TotalInstances,
                k_VisibleInstances, k_DisabledRendererCulled, k_LayerCulled, k_FrustumCulled,
                k_OcclusionCulled, k_GpuOcclusionCulled, k_LODGroupCulled, k_SmallMeshCulled, k_OtherCulled,
                k_LOD0, k_LOD1, k_LOD2, k_LOD3Plus,
            };
            for (int i = 1; i < (int)GRDExclusionReason.Count; i++)
            {
                if (k_ExclusionReasonCounterNames[i] != null)
                    names.Add(k_ExclusionReasonCounterNames[i]);
            }
            return names.ToArray();
        }

        internal static string CategoryNameForValidation => k_CategoryName;

        // ===== Helpers =====

        static long MsToNs(float ms) => (long)(ms * 1_000_000f);

        static ProfilerCounterValue<long> NewTimingCounter(string name) => new(
            k_Category, name, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        static ProfilerCounterValue<int> NewPerFrameCounter(string name) => new(
            k_Category, name, ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        static ProfilerCounterValue<int> NewCumulativeCounter(string name) => new(
            k_Category, name, ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame);

        static ProfilerCounterValue<float> NewPercentCounter(string name) => new(
            k_Category, name, ProfilerMarkerDataUnit.Percent,
            ProfilerCounterOptions.FlushOnEndOfFrame);
    }
}

#endif // !UNITY_WEBGL_RENDERER_ONLY
