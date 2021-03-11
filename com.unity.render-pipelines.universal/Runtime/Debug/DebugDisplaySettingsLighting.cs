using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsLighting : IDebugDisplaySettingsData
    {
        public DebugLightingMode DebugLightingMode;
        internal DebugLightingFeatureFlags DebugLightingFeatureFlagsMask;

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateLightingMode(DebugDisplaySettingsLighting data) => new DebugUI.EnumField
            {
                displayName = "Lighting Mode", autoEnum = typeof(DebugLightingMode),
                getter = () => (int)data.DebugLightingMode,
                setter = (value) => {},
                getIndex = () => (int)data.DebugLightingMode,
                setIndex = (value) => data.DebugLightingMode = (DebugLightingMode)value
            };

            internal static DebugUI.Widget CreateLightingFeatures(DebugDisplaySettingsLighting data) => new DebugUI.BitField
            {
                displayName = "Lighting Features",
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
                AddWidget(WidgetFactory.CreateLightingMode(data));
                AddWidget(WidgetFactory.CreateLightingFeatures(data));
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => (DebugLightingMode != DebugLightingMode.None) ||
        (DebugLightingFeatureFlagsMask != DebugLightingFeatureFlags.None);

        public bool IsPostProcessingAllowed => true;

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
