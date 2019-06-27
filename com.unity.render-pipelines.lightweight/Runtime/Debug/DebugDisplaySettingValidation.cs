
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsValidation : IDebugDisplaySettingsData
    {
        public DebugValidationMode validationMode;
        public float RangeMin = 0.0f;
        public float RangeMax = 1.0f;
        public bool AlsoHighlightAlphaOutsideRange = false;

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Validation";
            
            public SettingsPanel(DebugDisplaySettingsValidation data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "Validation Mode", autoEnum = typeof(DebugValidationMode), getter = () => (int)data.validationMode, setter = (value) => {}, getIndex = () => (int)data.validationMode, setIndex = (value) => data.validationMode = (DebugValidationMode)value});
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
