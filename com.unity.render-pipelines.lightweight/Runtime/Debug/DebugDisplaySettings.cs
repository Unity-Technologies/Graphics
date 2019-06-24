
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettings
    {
        private readonly HashSet<IDebugDisplaySettingsData> m_Settings = new HashSet<IDebugDisplaySettingsData>();
        
        private static readonly Lazy<DebugDisplaySettings> s_Instance = new Lazy<DebugDisplaySettings>(() => new DebugDisplaySettings());
        public static DebugDisplaySettings Instance => s_Instance.Value;

        public DebugDisplaySettingsTest Test { get; private set; }
        public DebugMaterialSettings materialSettings { get; private set; }
        
        private DebugDisplaySettingsTest m_DisplaySettingsTest;

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

            Test = Add(new DebugDisplaySettingsTest());
            materialSettings = Add(new DebugMaterialSettings());

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
