using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettings : IDebugDisplaySettingsQuery
    {
        private readonly HashSet<IDebugDisplaySettingsData> m_Settings = new HashSet<IDebugDisplaySettingsData>();

        private static readonly Lazy<DebugDisplaySettings> s_Instance = new Lazy<DebugDisplaySettings>(() => new DebugDisplaySettings());
        public static DebugDisplaySettings Instance => s_Instance.Value;

        public DebugMaterialSettings MaterialSettings { get; private set; }
        public DebugDisplaySettingsRendering RenderingSettings { get; private set; }
        public DebugDisplaySettingsLighting LightingSettings { get; private set; }
        public DebugDisplaySettingsValidation ValidationSettings { get; private set; }

        #region IDebugDisplaySettingsQuery
        public bool AreAnySettingsActive => MaterialSettings.AreAnySettingsActive ||
        LightingSettings.AreAnySettingsActive ||
        RenderingSettings.AreAnySettingsActive ||
        ValidationSettings.AreAnySettingsActive;

        public bool TryGetScreenClearColor(ref Color color)
        {
            return MaterialSettings.TryGetScreenClearColor(ref color) ||
                RenderingSettings.TryGetScreenClearColor(ref color) ||
                LightingSettings.TryGetScreenClearColor(ref color) ||
                ValidationSettings.TryGetScreenClearColor(ref color);
        }

        public bool IsLightingActive => MaterialSettings.IsLightingActive &&
        RenderingSettings.IsLightingActive &&
        LightingSettings.IsLightingActive &&
        ValidationSettings.IsLightingActive;

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
                            LightingSettings.IsPostProcessingAllowed &&
                            ValidationSettings.IsPostProcessingAllowed;
                    }

                    case DebugPostProcessingMode.Enabled:
                    {
                        return true;
                    }

                    default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(debugPostProcessingMode), $"Invalid post-processing state {debugPostProcessingMode}");
                    }
                } // End of switch.
            }
        }
        #endregion

        private TData Add<TData>(TData newData) where TData : IDebugDisplaySettingsData
        {
            m_Settings.Add(newData);
            return newData;
        }

        public DebugDisplaySettings()
        {
            Reset();
        }

        public void Reset()
        {
            m_Settings.Clear();

            MaterialSettings = Add(new DebugMaterialSettings());
            RenderingSettings = Add(new DebugDisplaySettingsRendering());
            LightingSettings = Add(new DebugDisplaySettingsLighting());
            ValidationSettings = Add(new DebugDisplaySettingsValidation());
        }

        public void ForEach(Action<IDebugDisplaySettingsData> onExecute)
        {
            foreach (IDebugDisplaySettingsData setting in m_Settings)
            {
                onExecute(setting);
            }
        }
    }
}
