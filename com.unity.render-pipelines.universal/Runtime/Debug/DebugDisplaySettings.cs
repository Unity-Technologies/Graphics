
using System;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
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

        public bool IsPostProcessingEnabled
        {
            get
            {
                PostProcessingState postProcessingState = renderingSettings.postProcessingState;

                switch(postProcessingState)
                {
                    case PostProcessingState.Disabled:
                    {
                        return false;
                    }

                    case PostProcessingState.Auto:
                    {
                        // Only enable post-processing if we aren't using certain debug-views...
                        return materialSettings.IsPostProcessingAllowed &&
                               renderingSettings.IsPostProcessingAllowed &&
                               Lighting.IsPostProcessingAllowed &&
                               Validation.IsPostProcessingAllowed;
                    }

                    case PostProcessingState.Enabled:
                    {
                        return true;
                    }

                    default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(postProcessingState), $"Invalid post-processing state {postProcessingState}");
                    }
                } // End of switch.
            }
        }

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
