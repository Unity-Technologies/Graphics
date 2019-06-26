
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettings
    {
        private readonly HashSet<IDebugDisplaySettingsData> m_Settings = new HashSet<IDebugDisplaySettingsData>();
        
        private static readonly Lazy<DebugDisplaySettings> s_Instance = new Lazy<DebugDisplaySettings>(() => new DebugDisplaySettings());
        public static DebugDisplaySettings Instance => s_Instance.Value;

        public DebugDisplaySettingsBuffer buffer { get; private set; }
        public DebugMaterialSettings materialSettings { get; private set; }
        public DebugDisplaySettingsRendering renderingSettings { get; private set; }
        public DebugDisplaySettingsLighting Lighting { get; private set; }
        
        private DebugDisplaySettingsBuffer m_DisplaySettingsBuffer;

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

            buffer = Add(new DebugDisplaySettingsBuffer());
            materialSettings = Add(new DebugMaterialSettings());
            renderingSettings = Add(new DebugDisplaySettingsRendering());
            Lighting = Add(new DebugDisplaySettingsLighting());
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
