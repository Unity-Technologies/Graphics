using System.Collections.Generic;
using UnityEngine;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Lighting-related Rendering Debugger settings.
    /// </summary>
    public class DebugDisplaySettingsLighting : IDebugDisplaySettingsData
    {
        /// <summary>
        /// Current debug lighting mode.
        /// </summary>
        public DebugLightingMode lightingDebugMode { get; set; }

        /// <summary>
        /// Current debug lighting feature flags mask that allows selective disabling individual lighting components.
        /// </summary>
        public DebugLightingFeatureFlags lightingFeatureFlags { get; set; }

        static internal class Strings
        {
            public static readonly NameAndTooltip LightingDebugMode = new() { name = "Lighting Debug Mode", tooltip = "Use the drop-down to select which lighting and shadow debug information to overlay on the screen." };
            public static readonly NameAndTooltip LightingFeatures = new() { name = "Lighting Features", tooltip = "Filter and debug selected lighting features in the system." };
        }

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateLightingDebugMode(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.LightingDebugMode,
                autoEnum = typeof(DebugLightingMode),
                getter = () => (int)panel.data.lightingDebugMode,
                setter = (value) => panel.data.lightingDebugMode = (DebugLightingMode)value,
                getIndex = () => (int)panel.data.lightingDebugMode,
                setIndex = (value) => panel.data.lightingDebugMode = (DebugLightingMode)value
            };

            internal static DebugUI.Widget CreateLightingFeatures(SettingsPanel panel) => new DebugUI.BitField
            {
                nameAndTooltip = Strings.LightingFeatures,
                getter = () => panel.data.lightingFeatureFlags,
                setter = (value) => panel.data.lightingFeatureFlags = (DebugLightingFeatureFlags)value,
                enumType = typeof(DebugLightingFeatureFlags),
            };
        }

        [DisplayInfo(name = "Lighting", order = 3)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsLighting>
        {
            public SettingsPanel(DebugDisplaySettingsLighting data)
                : base(data)
            {
                AddWidget(DebugDisplaySettingsCommon.WidgetFactory.CreateMissingDebugShadersWarning());

                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Lighting Debug Modes",
                    flags = DebugUI.Flags.FrequentlyUsed,
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateLightingDebugMode(this),
                        WidgetFactory.CreateLightingFeatures(this)
                    }
                });
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => (lightingDebugMode != DebugLightingMode.None) || (lightingFeatureFlags != DebugLightingFeatureFlags.None);

        public bool IsPostProcessingAllowed => (lightingDebugMode != DebugLightingMode.Reflections && lightingDebugMode != DebugLightingMode.ReflectionsWithSmoothness);

        public bool IsLightingActive => true;

        public bool TryGetScreenClearColor(ref Color color)
        {
            return false;
        }

        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
