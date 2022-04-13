using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// The abstract common implementation of the <see cref="IDebugDisplaySettingsPanelDisposable"/>
    /// </summary>
    public abstract class DebugDisplaySettingsPanel : IDebugDisplaySettingsPanelDisposable
    {
        private readonly List<DebugUI.Widget> m_Widgets = new List<DebugUI.Widget>();

        /// <summary>
        /// The Panel name
        /// </summary>
        public abstract string PanelName { get; }

        /// <summary>
        /// The collection of widgets that are in this panel
        /// </summary>
        public DebugUI.Widget[] Widgets => m_Widgets.ToArray();

        /// <summary>
        /// The <see cref="DebugUI.Flags"/> for this panel
        /// </summary>
        public virtual DebugUI.Flags Flags => DebugUI.Flags.None;

        /// <summary>
        /// Adds a widget to the panel
        /// </summary>
        /// <param name="widget">The <see cref="DebugUI.Widget"/> to be added.</param>
        protected void AddWidget(DebugUI.Widget widget)
        {
            m_Widgets.Add(widget);
        }

        /// <summary>
        /// Clears the widgets list
        /// </summary>
        protected void Clear()
        {
            m_Widgets.Clear();
        }

        /// <summary>
        /// Disposes the panel
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }
}
