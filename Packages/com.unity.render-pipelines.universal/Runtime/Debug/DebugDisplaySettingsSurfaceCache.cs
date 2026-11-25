#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using Unity.RenderPipelines.Core.Runtime.Shared;
using UnityEngine.PathTracing.Core;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

#if ENABLE_UIELEMENTS_MODULE && UNITY_ENABLE_CHECKS
using UnityEngine.UIElements;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Debug view modes for Surface Cache Global Illumination.
    /// The numeric values are consumed directly by the debug shader; None is never dispatched.
    /// </summary>
    internal enum SurfaceCacheDebugViewMode
    {
        None,
        CellIndex,
        StableIrradiance,
        FastIrradiance,
        CoefficientOfVariation,
        Drift,
        StdDev,
        UpdateCount,
        FlatNormal,
        Albedo,
        Emission,
        WorldCulling
    }

    /// <summary>
    /// Surface Cache Global Illumination related Rendering Debugger settings.
    /// </summary>
    [Serializable]
    internal class DebugDisplaySettingsSurfaceCache : IDebugDisplaySettingsData, ISerializedDebugDisplaySettings
    {
        /// <summary>
        /// Current Surface Cache debug view mode. The debug view is displayed as a fullscreen overlay when not None.
        /// </summary>
        public SurfaceCacheDebugViewMode viewMode { get; set; }

        /// <summary>
        /// Whether to show patch sample positions in the debug view.
        /// </summary>
        public bool showSamplePosition { get; set; }

        /// <summary>
        /// Whether the Scene view camera is ignored when deciding which entities Entity Culling keeps.
        /// </summary>
        public bool excludeSceneViewFromEntityCulling { get; set; }

        // Several renderers in the pipeline asset can each have an active Surface Cache GI feature
        static bool TryAggregateActiveFeatures(out int totalActiveCacheCount, out uint combinedWorldRenderingLayerMask)
        {
            totalActiveCacheCount = 0;
            combinedWorldRenderingLayerMask = 0;
            bool anyFeatureFound = false;

            if (UniversalRenderPipeline.asset == null)
                return false;

            foreach (var rendererData in UniversalRenderPipeline.asset.rendererDataList)
            {
                if (rendererData == null)
                    continue;

                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature is SurfaceCacheGIRendererFeature surfaceCacheFeature && surfaceCacheFeature.isActive && surfaceCacheFeature.IsValidForDebugging())
                    {
                        totalActiveCacheCount += surfaceCacheFeature.ActiveCacheCount;
                        combinedWorldRenderingLayerMask |= surfaceCacheFeature.WorldRenderingLayerMask;
                        anyFeatureFound = true;
                    }
                }
            }

            return anyFeatureFound;
        }

        struct MaterialAtlasSummary
        {
            public int MaterialCount;
            public AtlasUsage Albedo;
            public AtlasUsage Emission;
            public AtlasUsage Transmission;
            public long TotalAllocatedBytes;
        }

        // Each Surface Cache GI renderer feature owns a separate world with its own atlases, and the slice limit applies
        // to each of them, so per atlas type this reports the renderer feature that uses the most slices.
        static bool TryAggregateMaterialAtlasUsage(out MaterialAtlasSummary summary)
        {
            summary = default;
            bool anyFeatureFound = false;

            if (UniversalRenderPipeline.asset == null)
                return false;

            foreach (var rendererData in UniversalRenderPipeline.asset.rendererDataList)
            {
                if (rendererData == null)
                    continue;

                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature is SurfaceCacheGIRendererFeature surfaceCacheFeature && surfaceCacheFeature.isActive && surfaceCacheFeature.IsValidForDebugging())
                    {
                        surfaceCacheFeature.GetMaterialAtlasUsage(out int materialCount, out var albedo, out var emission, out var transmission);
                        summary.MaterialCount += materialCount;
                        if (!anyFeatureFound || albedo.UsedSliceCount > summary.Albedo.UsedSliceCount)
                            summary.Albedo = albedo;
                        if (!anyFeatureFound || emission.UsedSliceCount > summary.Emission.UsedSliceCount)
                            summary.Emission = emission;
                        if (!anyFeatureFound || transmission.UsedSliceCount > summary.Transmission.UsedSliceCount)
                            summary.Transmission = transmission;
                        summary.TotalAllocatedBytes += albedo.AllocatedBytes + emission.AllocatedBytes + transmission.AllocatedBytes;
                        anyFeatureFound = true;
                    }
                }
            }

            return anyFeatureFound;
        }

        static string FormatAtlas(in AtlasUsage usage)
        {
            var text = $"{usage.UsedSliceCount} / {usage.MaxSliceCount} · {DebugUIUtilities.FormatMemorySize(usage.AllocatedBytes)}";
            return usage.UsedSliceCount >= 0.9 * usage.MaxSliceCount ? "⚠ " + text : text;
        }

        static string FormatMaterialAtlas(Func<MaterialAtlasSummary, string> format)
        {
            return TryAggregateMaterialAtlasUsage(out var summary) ? format(summary) : "-";
        }

        internal static class Strings
        {
            public static readonly NameAndTooltip ViewMode = new() { name = "View Mode", tooltip = "Use the drop-down to select a Surface Cache debug visualization to display as an overlay." };
            public static readonly NameAndTooltip MapSize = new() { name = "Map Size", tooltip = "Set the size of the debug overlay in the debug view." };
            public static readonly NameAndTooltip ShowSamplePosition = new() { name = "Show Sample Position", tooltip = "Show patch sample positions in the debug view." };
            public static readonly NameAndTooltip ActiveCaches = new() { name = "Active Caches", tooltip = "Number of per-camera surface caches currently alive." };
            public static readonly NameAndTooltip EntityCullingSection = new() { name = "Entity Culling", tooltip = "Entity Culling statistics and options for the Surface Cache world." };
            public static readonly NameAndTooltip ExcludeSceneViewFromEntityCulling = new() { name = "Exclude Scene View", tooltip = "Exclude the Scene view camera when culling. Can be used to view culling against other cameras without the Scene view interfering." };
            public static readonly NameAndTooltip TrackedEntities = new() { name = "Entities", tooltip = "Number of entities that can contribute to Surface Cache, including culled entities." };
            public static readonly NameAndTooltip CulledEntities = new() { name = "Culled Entities", tooltip = "Number of entities that Entity Culling leaves out of the Surface Cache world, and their percentage of all entities." };
            public static readonly NameAndTooltip LiveEntities = new() { name = "Live Entities", tooltip = "Number of entities in the Surface Cache world, and their percentage of all entities." };
            public static readonly NameAndTooltip TrackedChunks = new() { name = "Chunks", tooltip = "Number of entity chunks that hold entities that can contribute to Surface Cache." };
            public static readonly NameAndTooltip CulledChunks = new() { name = "Culled Chunks", tooltip = "Number of chunks where Entity Culling leaves out every entity, and their percentage of all chunks." };
            public static readonly NameAndTooltip LiveChunks = new() { name = "Live Chunks", tooltip = "Number of chunks with at least one entity in the Surface Cache world, and their percentage of all chunks." };
            public static readonly NameAndTooltip WorldRenderingLayerMask = new() { name = "World Rendering Layer Mask", tooltip = "Combined (OR) rendering layer mask used for the shared world update this frame. This value is read only." };
            public static readonly NameAndTooltip MaterialAtlas = new() { name = "Material Atlas", tooltip = "GPU texture atlases that hold the albedo, emission and transmission of every material used by Surface Cache GI." };
            public static readonly NameAndTooltip Materials = new() { name = "Materials", tooltip = "Number of unique materials in the Surface Cache GI world." };
            public static readonly NameAndTooltip AlbedoAtlas = new() { name = "Albedo", tooltip = "Albedo texture atlas: slices used / device maximum (SystemInfo.maxTextureArraySlices), and VRAM allocated, which includes slices reserved ahead of use. ⚠ marks an atlas at or above 90% of the maximum." };
            public static readonly NameAndTooltip EmissionAtlas = new() { name = "Emission", tooltip = "Emission texture atlas: slices used / device maximum (SystemInfo.maxTextureArraySlices), and VRAM allocated, which includes slices reserved ahead of use. ⚠ marks an atlas at or above 90% of the maximum." };
            public static readonly NameAndTooltip TransmissionAtlas = new() { name = "Transmission", tooltip = "Transmission texture atlas: slices used / device maximum (SystemInfo.maxTextureArraySlices), and VRAM allocated, which includes slices reserved ahead of use. ⚠ marks an atlas at or above 90% of the maximum." };
            public static readonly NameAndTooltip TotalAtlasMemory = new() { name = "Total VRAM", tooltip = "Combined VRAM allocated by the albedo, emission and transmission atlases." };
        }

        class ReadOnlyValueField : DebugUI.Value
        {
#if ENABLE_UIELEMENTS_MODULE && UNITY_ENABLE_CHECKS
            protected override VisualElement Create()
            {
                var field = new TextField { label = displayName, isReadOnly = true };
                field.AddToClassList(TextField.alignedFieldUssClassName);
                field.SetEnabled(false);
                field.SetValueWithoutNotify(FormatString(GetValue()));
                field.schedule.Execute(() => field.SetValueWithoutNotify(FormatString(GetValue()))).Every((long)(refreshRate * 1000.0f));
                return field;
            }
#endif
        }

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateViewMode(DebugDisplaySettingsSurfaceCache data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.ViewMode,
                autoEnum = typeof(SurfaceCacheDebugViewMode),
                getter = () => (int)data.viewMode,
                setter = (value) => data.viewMode = (SurfaceCacheDebugViewMode)value,
                getIndex = () => (int)data.viewMode,
                setIndex = (value) => data.viewMode = (SurfaceCacheDebugViewMode)value
            };

            internal static DebugUI.Widget CreateMapSize(DebugDisplaySettingsSurfaceCache data) => new DebugUI.IntField
            {
                nameAndTooltip = Strings.MapSize,
                getter = () => UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.fullScreenDebugModeOutputSizeScreenPercent,
                setter = value => UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.fullScreenDebugModeOutputSizeScreenPercent = value,
                incStep = 10,
                incStepMult = 50,
                min = () => 0,
                max = () => 100,
                isHiddenCallback = () => data.viewMode == SurfaceCacheDebugViewMode.None
            };

            internal static DebugUI.Widget CreateShowSamplePosition(DebugDisplaySettingsSurfaceCache data) => new DebugUI.BoolField
            {
                nameAndTooltip = Strings.ShowSamplePosition,
                getter = () => data.showSamplePosition,
                setter = (value) => data.showSamplePosition = value,
                isHiddenCallback = () => data.viewMode == SurfaceCacheDebugViewMode.None
            };

            internal static DebugUI.Widget CreateActiveCaches(DebugDisplaySettingsSurfaceCache data) => new ReadOnlyValueField
            {
                nameAndTooltip = Strings.ActiveCaches,
                getter = () => TryAggregateActiveFeatures(out int cacheCount, out _) ? cacheCount.ToString() : "-",
                isHiddenCallback = () => data.viewMode == SurfaceCacheDebugViewMode.None
            };

            internal static DebugUI.Widget CreateEntityCulling(DebugDisplaySettingsSurfaceCache data) => new DebugUI.Foldout
            {
                nameAndTooltip = Strings.EntityCullingSection,
                opened = true,
                children =
                {
                    new ReadOnlyValueField
                    {
                        nameAndTooltip = Strings.TrackedEntities,
                        getter = () => FormatCount(SurfaceCacheEntityChangeBridge.TrackedEntityCount),
                    },
                    new ReadOnlyValueField
                    {
                        nameAndTooltip = Strings.CulledEntities,
                        getter = () => FormatCountWithShare(SurfaceCacheEntityChangeBridge.CulledEntityCount, SurfaceCacheEntityChangeBridge.TrackedEntityCount),
                    },
                    new ReadOnlyValueField
                    {
                        nameAndTooltip = Strings.LiveEntities,
                        getter = () => FormatCountWithShare(SurfaceCacheEntityChangeBridge.TrackedEntityCount - SurfaceCacheEntityChangeBridge.CulledEntityCount, SurfaceCacheEntityChangeBridge.TrackedEntityCount),
                    },
                    new ReadOnlyValueField
                    {
                        nameAndTooltip = Strings.TrackedChunks,
                        getter = () => FormatCount(SurfaceCacheEntityChangeBridge.TrackedChunkCount),
                    },
                    new ReadOnlyValueField
                    {
                        nameAndTooltip = Strings.CulledChunks,
                        getter = () => FormatCountWithShare(SurfaceCacheEntityChangeBridge.CulledChunkCount, SurfaceCacheEntityChangeBridge.TrackedChunkCount),
                    },
                    new ReadOnlyValueField
                    {
                        nameAndTooltip = Strings.LiveChunks,
                        getter = () => FormatCountWithShare(SurfaceCacheEntityChangeBridge.TrackedChunkCount - SurfaceCacheEntityChangeBridge.CulledChunkCount, SurfaceCacheEntityChangeBridge.TrackedChunkCount),
                    },
                    new DebugUI.BoolField
                    {
                        nameAndTooltip = Strings.ExcludeSceneViewFromEntityCulling,
                        getter = () => data.excludeSceneViewFromEntityCulling,
                        setter = value => data.excludeSceneViewFromEntityCulling = value,
                    },
                },
            };

            internal static string FormatCount(int count)
            {
                return SurfaceCacheEntityChangeBridge.IsConsumerPresent ? count.ToString("N0") : "-";
            }

            internal static string FormatCountWithShare(int count, int total)
            {
                if (!SurfaceCacheEntityChangeBridge.IsConsumerPresent)
                    return "-";

                float percentage = total > 0 ? 100.0f * count / total : 0.0f;
                return $"{count:N0} ({percentage:F1}%)";
            }

            internal static DebugUI.Widget CreateWorldRenderingLayerMask(DebugDisplaySettingsSurfaceCache data)
            {
                // No setter: DebugUI ignores edits when the setter is null, making the field read-only.
                // No layer color accessors: the field then omits its "Layers Color" child foldout.
                return new DebugUI.RenderingLayerField
                {
                    nameAndTooltip = Strings.WorldRenderingLayerMask,
                    getter = () =>
                    {
                        TryAggregateActiveFeatures(out _, out uint worldRenderingLayerMask);
                        return (RenderingLayerMask)worldRenderingLayerMask;
                    },
                    isHiddenCallback = () => data.viewMode == SurfaceCacheDebugViewMode.None
                };
            }

            internal static DebugUI.Widget CreateMaterialAtlas(DebugDisplaySettingsSurfaceCache data) => new DebugUI.Foldout
            {
                nameAndTooltip = Strings.MaterialAtlas,
                opened = true,
                children =
                {
                    new ReadOnlyValueField { nameAndTooltip = Strings.Materials, getter = () => FormatMaterialAtlas(s => s.MaterialCount.ToString()) },
                    new ReadOnlyValueField { nameAndTooltip = Strings.AlbedoAtlas, getter = () => FormatMaterialAtlas(s => FormatAtlas(s.Albedo)) },
                    new ReadOnlyValueField { nameAndTooltip = Strings.EmissionAtlas, getter = () => FormatMaterialAtlas(s => FormatAtlas(s.Emission)) },
                    new ReadOnlyValueField { nameAndTooltip = Strings.TransmissionAtlas, getter = () => FormatMaterialAtlas(s => FormatAtlas(s.Transmission)) },
                    new ReadOnlyValueField { nameAndTooltip = Strings.TotalAtlasMemory, getter = () => FormatMaterialAtlas(s => DebugUIUtilities.FormatMemorySize(s.TotalAllocatedBytes)) },
                }
            };
        }

        [DisplayInfo(name = "Surface Cache", order = 4)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsSurfaceCache>
        {
            public SettingsPanel(DebugDisplaySettingsSurfaceCache data)
                : base(data)
            {
                AddWidget(WidgetFactory.CreateViewMode(data));
                AddWidget(WidgetFactory.CreateMapSize(data));
                AddWidget(WidgetFactory.CreateShowSamplePosition(data));
                AddWidget(WidgetFactory.CreateActiveCaches(data));
                AddWidget(WidgetFactory.CreateWorldRenderingLayerMask(data));
                AddWidget(WidgetFactory.CreateMaterialAtlas(data));
                AddWidget(WidgetFactory.CreateEntityCulling(data));
            }
        }

        #region IDebugDisplaySettingsData

        /// <inheritdoc/>
        public bool AreAnySettingsActive => viewMode != SurfaceCacheDebugViewMode.None;

        /// <inheritdoc/>
        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}

#endif
