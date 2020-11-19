using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Action for click
    /// </summary>
    public class ClickAction : HoveredControlAction
    {
        private int m_Button;
        private bool m_UseEvent;
        /// <summary>
        /// Click count
        /// </summary>
        public int clickCount = 1;
        /// <summary>
        /// Action during onClick
        /// </summary>
        public Action<IGUIState, Control> onClick;
        private int m_ClickCounter = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="control">Current control</param>
        /// <param name="button">Button ID</param>
        /// <param name="useEvent">If use an event</param>
        public ClickAction(Control control, int button, bool useEvent = true) : base(control)
        {
            m_Button = button;
            m_UseEvent = useEvent;
        }

        /// <summary>
        /// Get if the trigger condition is validated or not
        /// </summary>
        /// <param name="guiState">The gui state</param>
        /// <returns></returns>
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
        /// On trigger
        /// </summary>
        /// <param name="guiState">The gui state</param>
        protected override void OnTrigger(IGUIState guiState)
        {
            base.OnTrigger(guiState);
            
            if (onClick != null)
                onClick(guiState, hoveredControl);

            if (m_UseEvent)
                guiState.UseEvent();
        }

        /// <summary>
        /// Get Finish Condition
        /// </summary>
        /// <param name="guiState">The gui state</param>
        /// <returns>Always true</returns>
        protected override bool GetFinishCondition(IGUIState guiState)
        {
            return true;
        }
    }
}
