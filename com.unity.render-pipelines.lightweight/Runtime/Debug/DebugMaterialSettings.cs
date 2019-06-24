
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering
{
    public class DebugMaterialSettings : IDebugDisplaySettingsData
    {
        public DebugMaterialIndex DebugMaterialIndexData;
        
        private class SettingsPanel : IDebugDisplaySettingsPanelDisposable
        {
            private readonly List<DebugUI.Widget> m_Widgets = new List<DebugUI.Widget>();
        
            public string PanelName => "Material";
            public DebugUI.Widget[] Widgets => m_Widgets.ToArray();

            protected void AddWidget(DebugUI.Widget widget)
            {
                m_Widgets.Add(widget);
            }
            
            public SettingsPanel(DebugMaterialSettings data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "Material Override", autoEnum = typeof(DebugMaterialIndex), getter = () => (int)data.DebugMaterialIndexData, setter = (value) => {}, getIndex = () => (int)data.DebugMaterialIndexData, setIndex = (value) => data.DebugMaterialIndexData = (DebugMaterialIndex)value});
            }

            public void Dispose()
            {
                m_Widgets.Clear();
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
