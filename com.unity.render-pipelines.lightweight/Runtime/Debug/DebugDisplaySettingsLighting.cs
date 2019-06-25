
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsLighting : IDebugDisplaySettingsData
    {
        public enum LightingDebugMode
        {
            None,
            ShadowCascades,
            LightOnly
            // TODO new mode
        }
        
        public LightingDebugMode m_LightingDebugMode;

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Lighting";
            
            public SettingsPanel(DebugDisplaySettingsLighting data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "LightingDebugMode", autoEnum = typeof(LightingDebugMode), getter = () => (int)data.m_LightingDebugMode, setter = (value) => {}, getIndex = () => (int)data.m_LightingDebugMode, setIndex = (value) => data.m_LightingDebugMode = (LightingDebugMode)value});
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
