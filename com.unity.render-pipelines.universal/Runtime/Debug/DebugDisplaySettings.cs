using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class DebugDisplaySettings : IDebugDisplaySettingsQuery
    {
        private readonly HashSet<IDebugDisplaySettingsData> m_Settings = new HashSet<IDebugDisplaySettingsData>();

        private static readonly Lazy<DebugDisplaySettings> s_Instance = new Lazy<DebugDisplaySettings>(() => new DebugDisplaySettings());

        /// <summary>
        /// The singleton instance that contains the current settings of URP Rendering Debugger.
        /// </summary>
        public static DebugDisplaySettings Instance => s_Instance.Value;

        DebugDisplaySettingsCommon commonSettings { get; set; }

        /// <summary>
        /// Material-related Rendering Debugger settings.
        /// </summary>
        public DebugDisplaySettingsMaterial materialSettings { get; private set; }

        /// <summary>
        /// Rendering-related Rendering Debugger settings.
        /// </summary>
        public DebugDisplaySettingsRendering renderingSettings { get; private set; }

        /// <summary>
        /// Lighting-related Rendering Debugger settings.
        /// </summary>
        public DebugDisplaySettingsLighting lightingSettings { get; private set; }

        #region IDebugDisplaySettingsQuery

        /// <summary>
        /// Returns true if any of the debug settings are currently active.
        /// </summary>
        public bool AreAnySettingsActive
        {
            get
            {
                foreach (IDebugDisplaySettingsData setting in m_Settings)
                {
                    if (setting.AreAnySettingsActive)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Attempts to get the color that should be used to clear the screen according to current debug settings.
        /// </summary>
        /// <param name="color">A reference to the screen clear color to use.</param>
        /// <returns>True if the color reference was updated, and false otherwise.</returns>
        public bool TryGetScreenClearColor(ref Color color)
        {
            foreach (IDebugDisplaySettingsData setting in m_Settings)
            {
                if (setting.TryGetScreenClearColor(ref color))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if lighting is active for current state of debug settings.
        /// </summary>
        public bool IsLightingActive
        {
            get
            {
                bool lightingActive = true;
                foreach (IDebugDisplaySettingsData setting in m_Settings)
                    lightingActive &= setting.IsLightingActive;
                return lightingActive;
            }
        }

        /// <summary>
        /// Returns true if the current state of debug settings allows post-processing.
        /// </summary>
        public bool IsPostProcessingAllowed
        {
            get
            {
                DebugPostProcessingMode debugPostProcessingMode = renderingSettings.postProcessingDebugMode;

                switch (debugPostProcessingMode)
                {
                    case DebugPostProcessingMode.Disabled:
                    {
                        return false;
                    }

                    case DebugPostProcessingMode.Auto:
                    {
                        // Only enable post-processing if we aren't using certain debug-views.
                        bool postProcessingAllowed = true;
                        foreach (IDebugDisplaySettingsData setting in m_Settings)
                            postProcessingAllowed &= setting.IsPostProcessingAllowed;
                        return postProcessingAllowed;
                    }

                    case DebugPostProcessingMode.Enabled:
                    {
                        return true;
                    }

                    default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(debugPostProcessingMode), $"Invalid post-processing state {debugPostProcessingMode}");
                    }
                }
            }
        }
        #endregion

        private TData Add<TData>(TData newData) where TData : IDebugDisplaySettingsData
        {
            m_Settings.Add(newData);
            return newData;
        }

        DebugDisplaySettings()
        {
            Reset();
        }

        /// <summary>
        /// Reset current debug data to default values.
        /// </summary>
        public void Reset()
        {
            m_Settings.Clear();

            commonSettings = Add(new DebugDisplaySettingsCommon());
            materialSettings = Add(new DebugDisplaySettingsMaterial());
            lightingSettings = Add(new DebugDisplaySettingsLighting());
            renderingSettings = Add(new DebugDisplaySettingsRendering());
        }

        internal void ForEach(Action<IDebugDisplaySettingsData> onExecute)
        {
            foreach (IDebugDisplaySettingsData setting in m_Settings)
            {
                onExecute(setting);
            }
        }
    }
}
