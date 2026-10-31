using System;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.Universal
{
    internal enum ScreenSpaceReflectionDebugViewMode
    {
        None,
        Reflections,
        RawReflections,
        Confidence,
        RawConfidence,
        MipLevel,
        Smoothness,
        ContactHardeningFactor,
        RayDistance,
    }

    [Serializable]
    internal class DebugDisplaySettingsScreenSpaceReflection : IDebugDisplaySettingsData, ISerializedDebugDisplaySettings
    {
        public ScreenSpaceReflectionDebugViewMode viewMode { get; set; }
        public float rayDistanceRange { get; set; } = 10;
        public bool showSceneColor { get; set; } = true;

        internal static class Strings
        {
            public static readonly NameAndTooltip ViewMode = new() { name = "View Mode", tooltip = "Which Screen Space Reflection debug visualization to display as an overlay." };
            public static readonly NameAndTooltip MapSize = new() { name = "Map Size", tooltip = "The size of the debug overlay texture in the view." };
            public static readonly NameAndTooltip ShowSceneColor = new() { name = "Show Scene Color", tooltip = "Whether to show the scene color where there is no screen space reflection, instead of black." };
            public static readonly NameAndTooltip RayDistanceRange = new() { name = "Ray Distance Range", tooltip = "Ray hit distance, in world units, that maps to the top of the color ramp." };
            public const string ContactHardeningRequired = "This view mode requires Contact Hardening to be enabled in the Screen Space Reflection volume override.";
            public const string RendererFeatureMissing = "These view modes require the Screen Space Reflection Renderer Feature to be enabled.";
        }

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateRendererFeatureMissingMessage() => new DebugUI.MessageBox
            {
                displayName = Strings.RendererFeatureMissing,
                style = DebugUI.MessageBox.Style.Warning,
                isHiddenCallback = () =>
                {
                    if (UniversalRenderPipeline.asset == null)
                        return true;

                    foreach (var rendererData in UniversalRenderPipeline.asset.rendererDataList)
                    {
                        if (rendererData == null)
                            continue;

                        foreach (var feature in rendererData.rendererFeatures)
                        {
                            if (feature is ScreenSpaceReflectionRendererFeature && feature.isActive)
                                return true;
                        }
                    }

                    return false;
                }
            };

            internal static DebugUI.Widget CreateViewMode(DebugDisplaySettingsScreenSpaceReflection data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.ViewMode,
                autoEnum = typeof(ScreenSpaceReflectionDebugViewMode),
                getter = () => (int)data.viewMode,
                setter = (value) => data.viewMode = (ScreenSpaceReflectionDebugViewMode)value,
                getIndex = () => (int)data.viewMode,
                setIndex = (value) => data.viewMode = (ScreenSpaceReflectionDebugViewMode)value
            };

            internal static DebugUI.Widget CreateMapSize(DebugDisplaySettingsScreenSpaceReflection data) => new DebugUI.IntField
            {
                nameAndTooltip = Strings.MapSize,
                getter = () => UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.fullScreenDebugModeOutputSizeScreenPercent,
                setter = value => UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.fullScreenDebugModeOutputSizeScreenPercent = value,
                incStep = 10,
                incStepMult = 50,
                min = () => 0,
                max = () => 100,
                isHiddenCallback = () => data.viewMode == ScreenSpaceReflectionDebugViewMode.None
            };

            internal static DebugUI.Widget CreateShowSceneColor(DebugDisplaySettingsScreenSpaceReflection data) => new DebugUI.BoolField
            {
                nameAndTooltip = Strings.ShowSceneColor,
                getter = () => data.showSceneColor,
                setter = (value) => data.showSceneColor = value,
                isHiddenCallback = () => data.viewMode != ScreenSpaceReflectionDebugViewMode.Reflections &&
                    data.viewMode != ScreenSpaceReflectionDebugViewMode.RawReflections
            };

            internal static DebugUI.Widget CreateRayDistanceRange(DebugDisplaySettingsScreenSpaceReflection data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.RayDistanceRange,
                getter = () => data.rayDistanceRange,
                setter = value => data.rayDistanceRange = value,
                incStep = 1.0f,
                incStepMult = 10.0f,
                min = () => 0.01f,
                max = () => 99.0f,
                isHiddenCallback = () => data.viewMode != ScreenSpaceReflectionDebugViewMode.RayDistance
            };

            internal static DebugUI.Widget CreateContactHardeningRequiredMessage(DebugDisplaySettingsScreenSpaceReflection data) => new DebugUI.MessageBox
            {
                displayName = Strings.ContactHardeningRequired,
                style = DebugUI.MessageBox.Style.Info,
                isHiddenCallback = () => data.viewMode != ScreenSpaceReflectionDebugViewMode.ContactHardeningFactor &&
                    data.viewMode != ScreenSpaceReflectionDebugViewMode.RayDistance
            };
        }

        [DisplayInfo(name = "Screen Space Reflection", order = 5)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsScreenSpaceReflection>
        {
            public SettingsPanel(DebugDisplaySettingsScreenSpaceReflection data)
                : base(data)
            {
                AddWidget(WidgetFactory.CreateViewMode(data));
                AddWidget(WidgetFactory.CreateMapSize(data));
                AddWidget(WidgetFactory.CreateShowSceneColor(data));
                AddWidget(WidgetFactory.CreateRayDistanceRange(data));
                AddWidget(WidgetFactory.CreateContactHardeningRequiredMessage(data));
                AddWidget(WidgetFactory.CreateRendererFeatureMissingMessage());
            }
        }

        /// <inheritdoc/>
        public bool AreAnySettingsActive => viewMode != ScreenSpaceReflectionDebugViewMode.None;

        /// <inheritdoc/>
        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }
    }
}
