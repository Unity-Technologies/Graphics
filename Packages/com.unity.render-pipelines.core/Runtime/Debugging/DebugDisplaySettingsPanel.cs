using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// The abstract common implementation of the <see cref="IDebugDisplaySettingsPanelDisposable"/>
    /// </summary>
    public abstract class DebugDisplaySettingsPanel : IDebugDisplaySettingsPanelDisposable
    {
        private readonly List<DebugUI.Widget> m_Widgets = new List<DebugUI.Widget>();

        private readonly DisplayInfoAttribute m_DisplayInfo;

        /// <summary>
        /// The Panel name
        /// </summary>
        public virtual string PanelName => m_DisplayInfo?.name ?? string.Empty;

        /// <summary>
        /// The order where this panel should be shown
        /// </summary>
        public virtual int Order => m_DisplayInfo?.order ?? 0;

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
            if (widget == null)
                throw new ArgumentNullException(nameof(widget));

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
        public virtual void Dispose()
        {
            Clear();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        protected DebugDisplaySettingsPanel()
        {
            m_DisplayInfo = GetType().GetCustomAttribute<DisplayInfoAttribute>();
            if (m_DisplayInfo == null)
                Debug.Log($"Type {GetType()} should specify the attribute {nameof(DisplayInfoAttribute)}");
        }
    }

    /// <summary>
    /// Class to help declare rendering debugger panels
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DebugDisplaySettingsPanel<T> : DebugDisplaySettingsPanel
        where T : IDebugDisplaySettingsData
    {
        internal T m_Data;

        /// <summary>
        /// Access to the data stored
        /// </summary>
        public T data
        {
            get => m_Data;
            internal set => m_Data = value;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="data">The data that the panel holds</param>
        protected DebugDisplaySettingsPanel(T data)
            : base()
        {
            m_Data = data;
        }
    }
}
