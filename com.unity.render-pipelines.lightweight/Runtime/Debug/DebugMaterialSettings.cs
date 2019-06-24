
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering
{
    public class DebugMaterialSettings : IDebugDisplaySettingsData
    {
        public DebugMaterialMask debugMaterialMaskData;
        
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
                AddWidget(new DebugUI.EnumField { displayName = "Material Override", autoEnum = typeof(DebugMaterialMask), getter = () => (int)data.debugMaterialMaskData, setter = (value) => {}, getIndex = () => (int)data.debugMaterialMaskData, setIndex = (value) => data.debugMaterialMaskData = (DebugMaterialMask)value});
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
