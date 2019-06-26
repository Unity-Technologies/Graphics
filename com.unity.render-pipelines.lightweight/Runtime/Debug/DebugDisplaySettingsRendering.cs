using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsRendering : IDebugDisplaySettingsData
    {
        public bool enableMsaa { get; private set; } = true;
        public bool enableHDR { get; private set; } = true;
        
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Rendering";
            
            public SettingsPanel(DebugDisplaySettingsRendering data)
            {
                AddWidget(new DebugUI.BoolField { displayName = "MSAA", getter = () => data.enableMsaa, setter = (value) => data.enableMsaa = value });
                AddWidget(new DebugUI.BoolField { displayName = "HDR", getter = () => data.enableHDR, setter = (value) => data.enableHDR = value });
            }
        }

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        public bool IsEnabled()
        {
            return enableMsaa || enableHDR;
        }
    }
}
