
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering
{
    // TODO: Remove this once we've got some values
    public class DebugDisplaySettingsTest : IDebugDisplaySettingsData
    {
        private enum TestEnum
        {
            First,
            Second,
            Third
        }
        
        public bool m_Boolean;
        internal ForwardRenderer.FullScreenDebugMode m_Enum = ForwardRenderer.FullScreenDebugMode.None;
        private Color m_Color;
        private float m_Float;
        private int m_Integer;

        private class SettingsPanel : IDebugDisplaySettingsPanelDisposable
        {
            private readonly List<DebugUI.Widget> m_Widgets = new List<DebugUI.Widget>();
        
            public string PanelName => "Test";
            public DebugUI.Widget[] Widgets => m_Widgets.ToArray();

            protected void AddWidget(DebugUI.Widget widget)
            {
                m_Widgets.Add(widget);
            }
            
            public SettingsPanel(DebugDisplaySettingsTest data)
            {
                AddWidget(new DebugUI.BoolField { displayName = "Boolean", getter = () => data.m_Boolean, setter = (value) => data.m_Boolean = value});
                AddWidget(new DebugUI.EnumField { displayName = "Full Screen Modes", autoEnum = typeof(ForwardRenderer.FullScreenDebugMode), getter = () => (int)data.m_Enum, setter = (value) => {}, getIndex = () => (int)data.m_Enum, setIndex = (value) => data.m_Enum = (ForwardRenderer.FullScreenDebugMode)value});
                AddWidget(new DebugUI.ColorField { displayName = "Color", getter = () => data.m_Color, setter = (value) => data.m_Color = value, showAlpha = false, hdr = true });
                AddWidget(new DebugUI.FloatField { displayName = "Float", getter = () => data.m_Float, setter = (value) => data.m_Float = value});
                AddWidget(new DebugUI.IntField { displayName = "Integer", getter = () => data.m_Integer, setter = (value) => data.m_Integer = value});
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
