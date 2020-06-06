using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    public class DebugMaterialSettings : IDebugDisplaySettingsData
    {
        public DebugMaterialIndex DebugMaterialIndexData;
        public VertexAttributeDebugMode VertexAttributeDebugIndexData;

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Material";

            public SettingsPanel(DebugMaterialSettings data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "Material Override", autoEnum = typeof(DebugMaterialIndex), getter = () => (int)data.DebugMaterialIndexData, setter = (value) => {}, getIndex = () => (int)data.DebugMaterialIndexData, setIndex = (value) => data.DebugMaterialIndexData = (DebugMaterialIndex)value});
                AddWidget(new DebugUI.EnumField
                {
                    displayName = "Vertex Attribute", autoEnum = typeof(VertexAttributeDebugMode),
                    getter = () => (int)data.VertexAttributeDebugIndexData, setter = (value) => { },
                    getIndex = () => (int) data.VertexAttributeDebugIndexData,
                    setIndex = (value) => data.VertexAttributeDebugIndexData = (VertexAttributeDebugMode) value
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
