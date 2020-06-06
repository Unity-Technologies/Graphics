using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    public class DebugDisplaySettingsLighting : IDebugDisplaySettingsData
    {
        public LightingDebugMode m_LightingDebugMode;
        internal PBRLightingDebugMode m_PBRLightingDebugMode;
        
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Lighting";
            
            public SettingsPanel(DebugDisplaySettingsLighting data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "LightingDebugMode", autoEnum = typeof(LightingDebugMode), 
                    getter = () => (int)data.m_LightingDebugMode, 
                    setter = (value) => {}, 
                    getIndex = () => (int)data.m_LightingDebugMode, 
                    setIndex = (value) => data.m_LightingDebugMode = (LightingDebugMode)value});

                AddWidget(new DebugUI.BitField { displayName = "PBR Lighting Debug Mode", 
                    getter = () => data.m_PBRLightingDebugMode, 
                    setter = (value) => data.m_PBRLightingDebugMode = (PBRLightingDebugMode)value, 
                    enumType = typeof(PBRLightingDebugMode),
                });
            }
        }

        #region IDebugDisplaySettingsData
        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }
        #endregion
    }
}
