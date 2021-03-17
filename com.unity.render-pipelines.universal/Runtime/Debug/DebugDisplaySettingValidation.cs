using UnityEngine;

namespace UnityEngine.Rendering.Universal
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
                AddWidget(new DebugUI.FloatField { displayName = "Pixel Value Range Min", getter = () => data.RangeMin, setter = (value) => data.RangeMin = value});
                AddWidget(new DebugUI.FloatField { displayName = "Pixel Value Range Max", getter = () => data.RangeMax, setter = (value) => data.RangeMax = value});
                AddWidget(new DebugUI.BoolField { displayName = "Highlight Out-Of-Range Alpha", getter = () => data.AlsoHighlightAlphaOutsideRange, setter = (value) => data.AlsoHighlightAlphaOutsideRange = value });
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => (validationMode != DebugValidationMode.None);

        public bool IsPostProcessingAllowed => true;

        public bool IsLightingActive => true;

        public bool TryGetScreenClearColor(ref Color color)
        {
            return false;
        }

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
