using System;
using UnityEngine;

namespace UnityEditor.GUIFramework
{
    /// <summary>
    /// Represents an Action to process when the user clicks a particular mouse button a certain number of times.
    /// </summary>
    public class ClickAction : HoveredControlAction
    {
        private int m_Button;
        private bool m_UseEvent;
        /// <summary>
        /// The number of button clicks required to satisfy the trigger condition
        /// </summary>
        public int clickCount = 1;
        /// <summary>
        /// The Action to execute when the user satisfies the trigger condition.
        /// </summary>
        public Action<IGUIState, Control> onClick;
        private int m_ClickCounter = 0;

        /// <summary>
        /// Initializes and returns an instance of ClickAction
        /// </summary>
        /// <param name="control">Current control</param>
        /// <param name="button">The mouse button to check for.</param>
        /// <param name="useEvent">Whether to Use the current event after the trigger condition has been met.</param>
        public ClickAction(Control control, int button, bool useEvent = true) : base(control)
        {
            m_Button = button;
            m_UseEvent = useEvent;
        }

        /// <summary>
        /// Checks to see if the trigger condition has been met or not.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if the trigger condition has been met. Otherwise, returns false.</returns>
        protected override bool GetTriggerCondition(IGUIState guiState)
        {
            if (guiState.mouseButton == m_Button && guiState.eventType == EventType.MouseDown)
            {
                if (guiState.clickCount == 1)
                    m_ClickCounter = 0;

                ++m_ClickCounter;

                if (m_ClickCounter == clickCount)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Calls the methods in its invocation list when the trigger conditions are met.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected override void OnTrigger(IGUIState guiState)
        {
            base.OnTrigger(guiState);

            if (onClick != null)
                onClick(guiState, hoveredControl);

            if (m_UseEvent)
                guiState.UseEvent();
        }

        /// <summary>
        /// Checks to see if the finish condition has been met or not. For a ClickAction, this is always `true`.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true`.</returns>
        protected override bool GetFinishCondition(IGUIState guiState)
        {
            return true;
        }
    }
}
