using UnityEngine;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.Universal
{
    class DebugDisplaySettingsLighting : IDebugDisplaySettingsData
    {
        internal DebugLightingMode DebugLightingMode { get; private set; }
        internal DebugLightingFeatureFlags DebugLightingFeatureFlagsMask { get; private set; }

        static class Strings
        {
            public static readonly NameAndTooltip LightingDebugMode = new() { name = "Lighting Debug Mode", tooltip = "Use the drop-down to select which lighting and shadow debug information to overlay on the screen." };
            public static readonly NameAndTooltip LightingFeatures = new() { name = "Lighting Features", tooltip = "Filter and debug selected lighting features in the system." };
        }

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateLightingDebugMode(DebugDisplaySettingsLighting data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.LightingDebugMode,
                autoEnum = typeof(DebugLightingMode),
                getter = () => (int)data.DebugLightingMode,
                setter = (value) => { },
                getIndex = () => (int)data.DebugLightingMode,
                setIndex = (value) => data.DebugLightingMode = (DebugLightingMode)value
            };

            internal static DebugUI.Widget CreateLightingFeatures(DebugDisplaySettingsLighting data) => new DebugUI.BitField
            {
                nameAndTooltip = Strings.LightingFeatures,
                getter = () => data.DebugLightingFeatureFlagsMask,
                setter = (value) => data.DebugLightingFeatureFlagsMask = (DebugLightingFeatureFlags)value,
                enumType = typeof(DebugLightingFeatureFlags),
            };
        }

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Lighting";

            public SettingsPanel(DebugDisplaySettingsLighting data)
            {
                AddWidget(DebugDisplaySettingsCommon.WidgetFactory.CreateMissingDebugShadersWarning());

                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Lighting Debug Modes",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateLightingDebugMode(data),
                        WidgetFactory.CreateLightingFeatures(data)
                    }
                });
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => (DebugLightingMode != DebugLightingMode.None) || (DebugLightingFeatureFlagsMask != DebugLightingFeatureFlags.None);

        public bool IsPostProcessingAllowed => (DebugLightingMode != DebugLightingMode.Reflections && DebugLightingMode != DebugLightingMode.ReflectionsWithSmoothness);

        public bool IsLightingActive => true;

        public bool TryGetScreenClearColor(ref Color color)
        {
            return false;
        }

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
