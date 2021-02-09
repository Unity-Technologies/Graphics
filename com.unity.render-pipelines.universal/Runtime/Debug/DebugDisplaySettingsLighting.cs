using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsLighting : IDebugDisplaySettingsData
    {
        public LightingDebugMode m_LightingDebugMode;
        internal DebugLightingFeature m_DebugLightingFeatureMask;

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Lighting";

            public SettingsPanel(DebugDisplaySettingsLighting data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "Lighting Mode", autoEnum = typeof(LightingDebugMode),
                    getter = () => (int)data.m_LightingDebugMode,
                    setter = (value) => {},
                    getIndex = () => (int)data.m_LightingDebugMode,
                    setIndex = (value) => data.m_LightingDebugMode = (LightingDebugMode)value});

                AddWidget(new DebugUI.BitField { displayName = "Lighting Features",
                    getter = () => data.m_DebugLightingFeatureMask,
                    setter = (value) => data.m_DebugLightingFeatureMask = (DebugLightingFeature)value,
                    enumType = typeof(DebugLightingFeature),
                });
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => (m_LightingDebugMode != LightingDebugMode.None) ||
                                             (m_DebugLightingFeatureMask != DebugLightingFeature.None);

        public bool IsPostProcessingAllowed => true;

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }
        #endregion
    }
}
