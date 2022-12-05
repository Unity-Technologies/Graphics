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

        /// <summary>
        /// Current HDR debug mode.
        /// </summary>
        public HDRDebugMode hdrDebugMode { get; set; }

        static internal class Strings
        {
            public static readonly NameAndTooltip LightingDebugMode = new() { name = "Lighting Debug Mode", tooltip = "Use the drop-down to select which lighting and shadow debug information to overlay on the screen." };
            public static readonly NameAndTooltip LightingFeatures = new() { name = "Lighting Features", tooltip = "Filter and debug selected lighting features in the system." };
            public static readonly NameAndTooltip HDRDebugMode = new() { name = "HDR Debug Mode", tooltip = "Select which HDR brightness debug information to overlay on the screen." };
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

            internal static DebugUI.Widget CreateHDRDebugMode(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.HDRDebugMode,
                autoEnum = typeof(HDRDebugMode),
                getter = () => (int)panel.data.hdrDebugMode,
                setter = (value) => panel.data.hdrDebugMode = (HDRDebugMode)value,
                getIndex = () => (int)panel.data.hdrDebugMode,
                setIndex = (value) => panel.data.hdrDebugMode = (HDRDebugMode)value
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
                        WidgetFactory.CreateHDRDebugMode(this),
                        WidgetFactory.CreateLightingFeatures(this)
                    }
                });
            }
        }

        #region IDebugDisplaySettingsData

        /// <inheritdoc/>
        public bool AreAnySettingsActive => (lightingDebugMode != DebugLightingMode.None) || (lightingFeatureFlags != DebugLightingFeatureFlags.None) || (hdrDebugMode != HDRDebugMode.None);

        /// <inheritdoc/>
        public bool IsPostProcessingAllowed => (lightingDebugMode != DebugLightingMode.Reflections && lightingDebugMode != DebugLightingMode.ReflectionsWithSmoothness);

        /// <inheritdoc/>
        public bool IsLightingActive => true;

        /// <inheritdoc/>
        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
