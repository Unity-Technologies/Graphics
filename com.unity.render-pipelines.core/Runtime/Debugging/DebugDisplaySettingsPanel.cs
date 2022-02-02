using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public abstract class DebugDisplaySettingsPanel : IDebugDisplaySettingsPanelDisposable
    {
        private readonly List<DebugUI.Widget> m_Widgets = new List<DebugUI.Widget>();

        public abstract string PanelName { get; }
        public DebugUI.Widget[] Widgets => m_Widgets.ToArray();
        public virtual DebugUI.Flags Flags => DebugUI.Flags.None;

        protected void AddWidget(DebugUI.Widget widget)
        {
            m_Widgets.Add(widget);
        }

        protected void Clear()
        {
            m_Widgets.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
