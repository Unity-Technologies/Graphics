using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug Display Settings Panel
    /// </summary>
    public abstract class DebugDisplaySettingsPanel : IDebugDisplaySettingsPanelDisposable
    {
        private readonly List<DebugUI.Widget> m_Widgets = new List<DebugUI.Widget>();

        /// <summary>
        /// The panel name
        /// </summary>
        public abstract string PanelName { get; }
        /// <summary>
        /// The list of <see cref="DebugUI.Widget"/> that this panel contains
        /// </summary>
        public DebugUI.Widget[] Widgets => m_Widgets.ToArray();

        /// <summary>
        /// The flags of the panel
        /// </summary>
        public virtual DebugUI.Flags Flags => DebugUI.Flags.None;

        /// <summary>
        /// Add a widget to the panel
        /// </summary>
        /// <param name="widget"></param>
        protected void AddWidget(DebugUI.Widget widget)
        {
            m_Widgets.Add(widget);
        }

        /// <summary>
        /// Clears all the widgets from the panel
        /// </summary>
        protected void Clear()
        {
            m_Widgets.Clear();
        }

        /// <summary>
        /// Disposes the panel, and calls clear
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }
}
