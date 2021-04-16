using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Path2D.GUIFramework
{
    internal class ClickAction : HoveredControlAction
    {
        private int m_Button;
        private bool m_UseEvent;

        public Action<IGUIState, Control> onClick;

        public ClickAction(Control control, int button, bool useEvent = true) : base(control)
        {
            m_Button = button;
            m_UseEvent = useEvent;
        }

        protected override bool GetTriggerContidtion(IGUIState guiState)
        {
            return guiState.mouseButton == m_Button && guiState.eventType == EventType.MouseDown;
        }

        protected override void OnTrigger(IGUIState guiState)
        {
            base.OnTrigger(guiState);

            if (onClick != null)
                onClick(guiState, hoveredControl);

            if (m_UseEvent)
                guiState.UseCurrentEvent();
        }

        protected override bool GetFinishContidtion(IGUIState guiState)
        {
            return true;
        }
    }
}
