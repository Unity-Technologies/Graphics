using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Lighting-related Rendering Debugger settings.
    /// </summary>
    [URPHelpURL("features/rendering-debugger-reference", "lighting")]
    [Serializable]
    public class DebugDisplaySettingsLighting : IDebugDisplaySettingsData, ISerializedDebugDisplaySettings
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
            internal static DebugUI.Widget CreateLightingDebugMode(DebugDisplaySettingsLighting data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.LightingDebugMode,
                autoEnum = typeof(DebugLightingMode),
                getter = () => (int)data.lightingDebugMode,
                setter = (value) => data.lightingDebugMode = (DebugLightingMode)value,
                getIndex = () => (int)data.lightingDebugMode,
                setIndex = (value) => data.lightingDebugMode = (DebugLightingMode)value
            };

            internal static DebugUI.Widget CreateLightingFeatures(DebugDisplaySettingsLighting data) => new DebugUI.BitField
            {
                nameAndTooltip = Strings.LightingFeatures,
                getter = () => data.lightingFeatureFlags,
                setter = (value) => data.lightingFeatureFlags = (DebugLightingFeatureFlags)value,
                enumType = typeof(DebugLightingFeatureFlags),
            };

            internal static DebugUI.Widget CreateHDRDebugMode(DebugDisplaySettingsLighting data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.HDRDebugMode,
                autoEnum = typeof(HDRDebugMode),
                getter = () => (int)data.hdrDebugMode,
                setter = (value) => data.hdrDebugMode = (HDRDebugMode)value,
                getIndex = () => (int)data.hdrDebugMode,
                setIndex = (value) => data.hdrDebugMode = (HDRDebugMode)value
            };
        }

        [DisplayInfo(name = "Lighting", order = 3)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsLighting>
        {
            public SettingsPanel(DebugDisplaySettingsLighting data)
                : base(data)
            {
                AddWidget(new DebugUI.RuntimeDebugShadersMessageBox());

                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Lighting Debug Modes",
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateLightingDebugMode(data),
                        WidgetFactory.CreateHDRDebugMode(data),
                        WidgetFactory.CreateLightingFeatures(data)
                    },
                    documentationUrl = typeof(DebugDisplaySettingsLighting).GetCustomAttribute<HelpURLAttribute>()?.URL
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
