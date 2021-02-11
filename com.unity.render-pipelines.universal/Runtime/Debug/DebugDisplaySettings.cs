
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

        public DebugMaterialSettings materialSettings { get; private set; }
        public DebugDisplaySettingsRendering renderingSettings { get; private set; }
        public DebugDisplaySettingsLighting Lighting { get; private set; }
        public DebugDisplaySettingsValidation Validation { get; private set; }

        #region IDebugDisplaySettingsQuery
        public bool AreAnySettingsActive => materialSettings.AreAnySettingsActive ||
                                            Lighting.AreAnySettingsActive ||
                                            renderingSettings.AreAnySettingsActive ||
                                            Validation.AreAnySettingsActive;

        public bool IsDebugMaterialActive => materialSettings.IsDebugMaterialActive ||
                                             Lighting.IsDebugMaterialActive ||
                                             renderingSettings.IsDebugMaterialActive ||
                                             Validation.IsDebugMaterialActive;

        public bool TryGetScreenClearColor(ref Color color)
        {
            return materialSettings.TryGetScreenClearColor(ref color) ||
                   renderingSettings.TryGetScreenClearColor(ref color) ||
                   Lighting.TryGetScreenClearColor(ref color) ||
                   Validation.TryGetScreenClearColor(ref color);
        }

        public bool IsLightingActive => materialSettings.IsLightingActive &&
                                        renderingSettings.IsLightingActive &&
                                        Lighting.IsLightingActive &&
                                        Validation.IsLightingActive;

        public bool IsPostProcessingAllowed
        {
            get
            {
                DebugPostProcessingMode debugPostProcessingMode = renderingSettings.debugPostProcessingMode;

                switch(debugPostProcessingMode)
                {
                    case DebugPostProcessingMode.Disabled:
                    {
                        return false;
                    }

                    case DebugPostProcessingMode.Auto:
                    {
                        // Only enable post-processing if we aren't using certain debug-views...
                        return materialSettings.IsPostProcessingAllowed &&
                               renderingSettings.IsPostProcessingAllowed &&
                               Lighting.IsPostProcessingAllowed &&
                               Validation.IsPostProcessingAllowed;
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

        private TData Add<TData>(TData newData) where TData: IDebugDisplaySettingsData
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

            materialSettings = Add(new DebugMaterialSettings());
            renderingSettings = Add(new DebugDisplaySettingsRendering());
            Lighting = Add(new DebugDisplaySettingsLighting());
            Validation = Add(new DebugDisplaySettingsValidation());
        }

        public void ForEach(Action<IDebugDisplaySettingsData> onExecute)
        {
            foreach(IDebugDisplaySettingsData setting in m_Settings)
            {
                onExecute(setting);
            }
        }
    }
}
