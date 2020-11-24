using System;
using UnityEngine;

namespace UnityEditor.GUIFramework
{
    /// <summary>
    /// Interface for HoveredControlActions.
    /// </summary>
    public abstract class HoveredControlAction : GUIAction
    {
        private Control m_HoveredControl;

        /// <summary>
        /// The hovered control.
        /// </summary>
        public Control hoveredControl
        {
            get { return m_HoveredControl; }
        }

        /// <summary>
        /// Initializes and returns an instance of HoverControlAction.
        /// </summary>
        /// <param name="control">The control to execcute an action for on hover.</param>
        public HoveredControlAction(Control control)
        {
            m_HoveredControl = control;
        }

        /// <summary>
        /// Determines whether the HoveredControlAction can trigger.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if the HoveredControlAction can trigger. Otherwise, returns `false`.</returns>
        protected override bool CanTrigger(IGUIState guiState)
        {
            return guiState.nearestControl == hoveredControl.ID;
        }

        /// <summary>
        /// Calls the methods in its invocation list when triggered.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected override void OnTrigger(IGUIState guiState)
        {
            m_HoveredControl.SetActionID(ID);
        }
    }
}
