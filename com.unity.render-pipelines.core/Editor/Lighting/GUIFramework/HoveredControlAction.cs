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
        /// Constructor
        /// </summary>
        /// <param name="control">The control</param>
        public HoveredControlAction(Control control)
        {
            m_HoveredControl = control;
        }

        /// <summary>
        /// Can trigger
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if can be triggered</returns>
        protected override bool CanTrigger(IGUIState guiState)
        {
            return guiState.nearestControl == hoveredControl.ID;
        }

        /// <summary>
        /// On Trigger
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        protected override void OnTrigger(IGUIState guiState)
        {
            m_HoveredControl.SetActionID(ID);
        }
    }
}
