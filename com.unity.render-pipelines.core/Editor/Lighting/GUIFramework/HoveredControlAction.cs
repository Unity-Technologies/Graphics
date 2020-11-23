using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Abstract class for HoveredControlAction
    /// </summary>
    public abstract class HoveredControlAction : GUIAction
    {
        private Control m_HoveredControl;

        /// <summary>
        /// Hovered Control
        /// </summary>
        public Control hoveredControl
        {
            get { return m_HoveredControl; }
        }

        /// <summary>
        /// Initializes and returns an instance of HoverControlAction
        /// </summary>
        /// <param name="control">The control</param>
        public HoveredControlAction(Control control)
        {
            m_HoveredControl = control;
        }

        /// <summary>
        /// Calls the methods in its invocation list to test if Can be triggered
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if can be triggered</returns>
        protected override bool CanTrigger(IGUIState guiState)
        {
            return guiState.nearestControl == hoveredControl.ID;
        }

        /// <summary>
        /// Calls the methods in its invocation list when triggered
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        protected override void OnTrigger(IGUIState guiState)
        {
            m_HoveredControl.SetActionID(ID);
        }
    }
}
