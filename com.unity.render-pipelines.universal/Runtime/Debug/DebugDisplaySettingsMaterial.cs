using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class DebugDisplaySettingsMaterial : IDebugDisplaySettingsData
    {
        public DebugMaterialMode DebugMaterialModeData;
        public DebugVertexAttributeMode DebugVertexAttributeIndexData;

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateMaterialOverride(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                displayName = "Material Override",
                autoEnum = typeof(DebugMaterialMode),
                getter = () => (int)data.DebugMaterialModeData,
                setter = (value) => {},
                getIndex = () => (int)data.DebugMaterialModeData,
                setIndex = (value) => data.DebugMaterialModeData = (DebugMaterialMode)value
            };

            internal static DebugUI.Widget CreateVertexAttribute(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                displayName = "Vertex Attribute",
                autoEnum = typeof(DebugVertexAttributeMode),
                getter = () => (int)data.DebugVertexAttributeIndexData,
                setter = (value) => {},
                getIndex = () => (int)data.DebugVertexAttributeIndexData,
                setIndex = (value) => data.DebugVertexAttributeIndexData = (DebugVertexAttributeMode)value
            };
        }

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Material";
            public SettingsPanel(DebugDisplaySettingsMaterial data)
            {
                AddWidget(WidgetFactory.CreateMaterialOverride(data));
                AddWidget(WidgetFactory.CreateVertexAttribute(data));
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => (DebugMaterialModeData != DebugMaterialMode.None) ||
        (DebugVertexAttributeIndexData != DebugVertexAttributeMode.None);
        public bool IsPostProcessingAllowed => !AreAnySettingsActive;
        public bool IsLightingActive => !AreAnySettingsActive;

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
