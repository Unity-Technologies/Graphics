using System;
using UnityEngine;

namespace UnityEditor
{
    public class ClickAction : HoveredControlAction
    {
        private int m_Button;
        private bool m_UseEvent;
        public int clickCount = 1;
        public Action<IGUIState, Control> onClick;
        private int m_ClickCounter = 0;

        public ClickAction(Control control, int button, bool useEvent = true) : base(control)
        {
            m_Button = button;
            m_UseEvent = useEvent;
        }

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

        protected override void OnTrigger(IGUIState guiState)
        {
            base.OnTrigger(guiState);
            
            if (onClick != null)
                onClick(guiState, hoveredControl);

            if (m_UseEvent)
                guiState.UseEvent();
        }

        protected override bool GetFinishCondition(IGUIState guiState)
        {
            return true;
        }
    }
}
