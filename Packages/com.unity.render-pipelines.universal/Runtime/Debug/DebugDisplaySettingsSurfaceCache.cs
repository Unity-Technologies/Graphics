#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
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
        FlatNormal
    }

    /// <summary>
    /// Surface Cache Global Illumination related Rendering Debugger settings.
    /// </summary>
    [Serializable]
    internal class DebugDisplaySettingsSurfaceCache : IDebugDisplaySettingsData, ISerializedDebugDisplaySettings
    {
        /// <summary>
        /// Current Surface Cache debug view mode. The debug view replaces the screen irradiance output when not None.
        /// </summary>
        public SurfaceCacheDebugViewMode viewMode { get; set; }

        /// <summary>
        /// Whether to show patch sample positions in the debug view.
        /// </summary>
        public bool showSamplePosition { get; set; }

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

        internal static class Strings
        {
            public static readonly NameAndTooltip ViewMode = new() { name = "View Mode", tooltip = "Use the drop-down to select a Surface Cache debug visualization to render instead of the screen irradiance output." };
            public static readonly NameAndTooltip ShowSamplePosition = new() { name = "Show Sample Position", tooltip = "Show patch sample positions in the debug view." };
            public static readonly NameAndTooltip ActiveCaches = new() { name = "Active Caches", tooltip = "Number of per-camera surface caches currently alive." };
            public static readonly NameAndTooltip WorldRenderingLayerMask = new() { name = "World Rendering Layer Mask", tooltip = "Combined (OR) rendering layer mask used for the shared world update this frame. This value is read only." };
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

            internal static DebugUI.Widget CreateWorldRenderingLayerMask(DebugDisplaySettingsSurfaceCache data)
            {
                // No setter: DebugUI ignores edits when the setter is null, making the field read-only.
                var field = new DebugUI.RenderingLayerField
                {
                    nameAndTooltip = Strings.WorldRenderingLayerMask,
                    getter = () =>
                    {
                        TryAggregateActiveFeatures(out _, out uint worldRenderingLayerMask);
                        return (RenderingLayerMask)worldRenderingLayerMask;
                    },
                    isHiddenCallback = () => data.viewMode == SurfaceCacheDebugViewMode.None
                };

                // Drop RenderingLayerField's "Layers Color" child foldout; it belongs to the Rendering
                // Layers visualization in the Material panel and has no meaning for this read-only mask.
                field.children.Clear();

                return field;
            }
        }

        [DisplayInfo(name = "Surface Cache", order = 4)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsSurfaceCache>
        {
            public SettingsPanel(DebugDisplaySettingsSurfaceCache data)
                : base(data)
            {
                AddWidget(WidgetFactory.CreateViewMode(data));
                AddWidget(WidgetFactory.CreateShowSamplePosition(data));
                AddWidget(WidgetFactory.CreateActiveCaches(data));
                AddWidget(WidgetFactory.CreateWorldRenderingLayerMask(data));
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
