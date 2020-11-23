using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// SliderAction implementation of a ClickAction
    /// </summary>
    public class SliderAction : ClickAction
    {
        private SliderData m_SliderData;

        /// <summary>
        /// Action for OnSliderBegin
        /// </summary>
        public Action<IGUIState, Control, Vector3> onSliderBegin;
        /// <summary>
        /// Action for OnSliderChanged
        /// </summary>
        public Action<IGUIState, Control, Vector3> onSliderChanged;
        /// <summary>
        /// Action for OnSliderEnd
        /// </summary>
        public Action<IGUIState, Control, Vector3> onSliderEnd;

        /// <summary>
        /// Initializes and returns an instance of SliderAction
        /// </summary>
        /// <param name="control">Control</param>
        public SliderAction(Control control) : base(control, 0, false)
        {
        }

        /// <summary>
        /// GetFinishedCondiction
        /// </summary>
        /// <param name="guiState">The GUIState</param>
        /// <returns>true if finish condition are validated</returns>
        protected override bool GetFinishCondition(IGUIState guiState)
        {
            return guiState.eventType == EventType.MouseUp && guiState.mouseButton == 0;
        }

        /// <summary>
        /// On Trigger
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
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

        /// <summary>
        /// On Finished
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected override void OnFinish(IGUIState guiState)
        {
            if (onSliderEnd != null)
                onSliderEnd(guiState, hoveredControl, m_SliderData.position);

            guiState.UseEvent();
            guiState.Repaint();
        }

        /// <summary>
        /// On Perform
        /// </summary>
        /// <param name="guiState">The GUIState</param>
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
