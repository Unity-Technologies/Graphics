using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D.GUIFramework
{
    internal class SliderAction : ClickAction
    {
        private SliderData m_SliderData;

        public Action<IGUIState, Control, Vector3> onSliderBegin;
        public Action<IGUIState, Control, Vector3> onSliderChanged;
        public Action<IGUIState, Control, Vector3> onSliderEnd;

        public SliderAction(Control control) : base(control, 0, false)
        {
        }

        protected override bool GetFinishContidtion(IGUIState guiState)
        {
            return guiState.eventType == EventType.MouseUp && guiState.mouseButton == 0;
        }

        protected override void OnTrigger(IGUIState guiState)
        {
            base.OnTrigger(guiState);

            m_SliderData.position = hoveredControl.hotLayoutData.position;
            m_SliderData.forward = hoveredControl.hotLayoutData.forward;
            m_SliderData.right = hoveredControl.hotLayoutData.right;
            m_SliderData.up = hoveredControl.hotLayoutData.up;

            if (onSliderBegin != null)
                onSliderBegin(guiState, hoveredControl, m_SliderData.position);
        }

        protected override void OnFinish(IGUIState guiState)
        {
            if (onSliderEnd != null)
                onSliderEnd(guiState, hoveredControl, m_SliderData.position);

            guiState.UseCurrentEvent();
            guiState.Repaint();
        }

        protected override void OnPerform(IGUIState guiState)
        {
            Vector3 newPosition;
            var changed = guiState.Slider(ID, m_SliderData, out newPosition);

            if (changed)
            {
                m_SliderData.position = newPosition;

                if (onSliderChanged != null)
                    onSliderChanged(guiState, hoveredControl, newPosition);
            }
        }
    }
}
