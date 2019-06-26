
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsOverlay : IDebugDisplaySettingsData
    {
        public DebugOverlayMode overlayMode;
        public float RangeMin = 0.0f;
        public float RangeMax = 1.0f;
        public bool AlsoHighlightAlphaOutsideRange = false;

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Overlay";
            
            public SettingsPanel(DebugDisplaySettingsOverlay data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "OverlayMode", autoEnum = typeof(DebugOverlayMode), getter = () => (int)data.overlayMode, setter = (value) => {}, getIndex = () => (int)data.overlayMode, setIndex = (value) => data.overlayMode = (DebugOverlayMode)value});
                AddWidget(new DebugUI.FloatField { displayName = "Range Min", getter = () => data.RangeMin, setter = (value) => data.RangeMin = value});
                AddWidget(new DebugUI.FloatField { displayName = "Range Max", getter = () => data.RangeMax, setter = (value) => data.RangeMax = value});
                AddWidget(new DebugUI.BoolField { displayName = "Highlight Out-Of-Range Alpha", getter = () => data.AlsoHighlightAlphaOutsideRange, setter = (value) => data.AlsoHighlightAlphaOutsideRange = value });
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
