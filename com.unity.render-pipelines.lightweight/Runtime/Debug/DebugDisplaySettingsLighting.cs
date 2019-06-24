
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsLighting : IDebugDisplaySettingsData
    {
        public enum LightingDebugMode
        {
            None,
            ShadowCascades,
            // TODO new mode
        }
        
        private enum TestEnum
        {
            First,
            Second,
            Third
        }
        
        public LightingDebugMode m_LightingDebugMode;
        
        public bool m_Boolean;
        private TestEnum m_Enum;
        private Color m_Color;
        private float m_Float;
        private int m_Integer;

        private class SettingsPanel : IDebugDisplaySettingsPanelDisposable
        {
            private readonly List<DebugUI.Widget> m_Widgets = new List<DebugUI.Widget>();
        
            public string PanelName => "Lighting";
            public DebugUI.Widget[] Widgets => m_Widgets.ToArray();

            protected void AddWidget(DebugUI.Widget widget)
            {
                m_Widgets.Add(widget);
            }
            
            public SettingsPanel(DebugDisplaySettingsLighting data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "LightingDebugMode", autoEnum = typeof(LightingDebugMode), getter = () => (int)data.m_LightingDebugMode, setter = (value) => {}, getIndex = () => (int)data.m_LightingDebugMode, setIndex = (value) => data.m_LightingDebugMode = (LightingDebugMode)value});
                
                AddWidget(new DebugUI.BoolField { displayName = "Boolean", getter = () => data.m_Boolean, setter = (value) => data.m_Boolean = value});
                AddWidget(new DebugUI.EnumField { displayName = "Enum", autoEnum = typeof(TestEnum), getter = () => (int)data.m_Enum, setter = (value) => {}, getIndex = () => (int)data.m_Enum, setIndex = (value) => data.m_Enum = (TestEnum)value});
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
