using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsRendering : IDebugDisplaySettingsData
    {
        internal FullScreenDebugMode fullScreenDebugMode { get; private set; } = FullScreenDebugMode.None;
        internal bool enablePostProcessing { get; private set; } = true;

        public bool enableMsaa { get; private set; } = true;
        public bool enableHDR { get; private set; } = true;
        
        
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Rendering";
            
            public SettingsPanel(DebugDisplaySettingsRendering data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "Full Screen Modes", autoEnum = typeof(FullScreenDebugMode), getter = () => (int)data.fullScreenDebugMode, setter = (value) => {}, getIndex = () => (int)data.fullScreenDebugMode, setIndex = (value) => data.fullScreenDebugMode = (FullScreenDebugMode)value});
                AddWidget(new DebugUI.BoolField { displayName = "Post-processing", getter = () => data.enablePostProcessing, setter = (value) => data.enablePostProcessing = value });
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
            return enableMsaa || enableHDR || enablePostProcessing || fullScreenDebugMode != FullScreenDebugMode.None;
        }
    }
}
