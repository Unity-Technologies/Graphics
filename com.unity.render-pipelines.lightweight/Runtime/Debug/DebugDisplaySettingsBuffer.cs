
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsBuffer : IDebugDisplaySettingsData
    {
        internal ForwardRenderer.FullScreenDebugMode FullScreenDebugMode = ForwardRenderer.FullScreenDebugMode.None;

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Buffer";
            
            public SettingsPanel(DebugDisplaySettingsBuffer data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "Full Screen Modes", autoEnum = typeof(ForwardRenderer.FullScreenDebugMode), getter = () => (int)data.FullScreenDebugMode, setter = (value) => {}, getIndex = () => (int)data.FullScreenDebugMode, setIndex = (value) => data.FullScreenDebugMode = (ForwardRenderer.FullScreenDebugMode)value});
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
