using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public class DebugDisplaySettings
    {
        private readonly HashSet<IDebugDisplaySettingsData> m_Settings = new HashSet<IDebugDisplaySettingsData>();
        
        private static readonly Lazy<DebugDisplaySettings> s_Instance = new Lazy<DebugDisplaySettings>(() => new DebugDisplaySettings());
        public static DebugDisplaySettings Instance => s_Instance.Value;

        public DebugMaterialSettings materialSettings { get; private set; }
        public DebugDisplaySettingsRendering renderingSettings { get; private set; }
        public DebugDisplaySettingsLighting Lighting { get; private set; }
        public DebugDisplaySettingsValidation Validation { get; private set; }
        
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
