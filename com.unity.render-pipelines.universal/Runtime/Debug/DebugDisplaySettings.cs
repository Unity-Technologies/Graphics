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

        DebugDisplaySettingsCommon CommonSettings { get; set; }

        /// <summary>
        /// Material-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsMaterial MaterialSettings { get; private set; }

        /// <summary>
        /// Rendering-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsRendering RenderingSettings { get; private set; }

        /// <summary>
        /// Lighting-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsLighting LightingSettings { get; private set; }

        #region IDebugDisplaySettingsQuery

        /// <summary>
        /// Returns true if any of the debug settings are currently active.
        /// </summary>
        public bool AreAnySettingsActive => MaterialSettings.AreAnySettingsActive ||
        LightingSettings.AreAnySettingsActive ||
        RenderingSettings.AreAnySettingsActive;

        public bool TryGetScreenClearColor(ref Color color)
        {
            return MaterialSettings.TryGetScreenClearColor(ref color) ||
                RenderingSettings.TryGetScreenClearColor(ref color) ||
                LightingSettings.TryGetScreenClearColor(ref color);
        }

        /// <summary>
        /// Returns true if lighting is active for current state of debug settings.
        /// </summary>
        public bool IsLightingActive => MaterialSettings.IsLightingActive &&
        RenderingSettings.IsLightingActive &&
        LightingSettings.IsLightingActive;

        /// <summary>
        /// Returns true if the current state of debug settings allows post-processing.
        /// </summary>
        public bool IsPostProcessingAllowed
        {
            get
            {
                DebugPostProcessingMode debugPostProcessingMode = RenderingSettings.debugPostProcessingMode;

                switch (debugPostProcessingMode)
                {
                    case DebugPostProcessingMode.Disabled:
                    {
                        return false;
                    }

                    case DebugPostProcessingMode.Auto:
                    {
                        // Only enable post-processing if we aren't using certain debug-views...
                        return MaterialSettings.IsPostProcessingAllowed &&
                            RenderingSettings.IsPostProcessingAllowed &&
                            LightingSettings.IsPostProcessingAllowed;
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

        internal void Reset()
        {
            m_Settings.Clear();

            CommonSettings = Add(new DebugDisplaySettingsCommon());
            MaterialSettings = Add(new DebugDisplaySettingsMaterial());
            LightingSettings = Add(new DebugDisplaySettingsLighting());
            RenderingSettings = Add(new DebugDisplaySettingsRendering());
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
