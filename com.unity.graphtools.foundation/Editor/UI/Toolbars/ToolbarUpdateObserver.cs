using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A basic observer for toolbar elements.
    /// </summary>
    public class ToolbarUpdateObserver : StateObserver
    {
        IToolbarElement m_ToolbarElement;

        ToolStateComponent m_ToolState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolbarUpdateObserver"/> class.
        /// </summary>
        /// <param name="element">The toolbar element updated by this observer.</param>
        /// <param name="toolState">The state to observe.</param>
        public ToolbarUpdateObserver(IToolbarElement element, ToolStateComponent toolState)
            : base(toolState)
        {
            m_ToolbarElement = element;
            m_ToolState = toolState;
        }

        public override void Observe()
        {
            using (var observation = this.ObserveState(m_ToolState))
            {
                if (observation.UpdateType != UpdateType.None)
                {
                    m_ToolbarElement.Update();
                }
            }
        }
    }
}
