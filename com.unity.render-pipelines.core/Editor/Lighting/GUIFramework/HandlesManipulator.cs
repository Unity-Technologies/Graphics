using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Represents a handle in the custom editor.
    /// </summary>
    public class HandlesManipulator
    {
        /// <summary>
        /// Func for getEnable
        /// </summary>
        public Func<IGUIState, bool> enable;
        /// <summary>
        /// func for OnGUI
        /// </summary>
        public Action<IGUIState> onGui;
        /// <summary>
        /// Func for OnEndLayout
        /// </summary>
        public Action<IGUIState> onEndLayout;
        private bool m_Enabled = false;

        /// <summary>
        /// Begins the layout for this handle. A call to EndLayout must always follow a call to this function.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void OnGUI(IGUIState guiState)
        {
            if (guiState.eventType == EventType.Layout)
                m_Enabled = IsEnabled(guiState);

            if (m_Enabled)
            {
                if (onGui != null)
                    onGui(guiState);
            }
        }

        /// <summary>
        /// Ends the layout for this handle. This function must always follow a call to BeginLayout().
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void EndLayout(IGUIState guiState)
        {
            if (m_Enabled)
            {
                if (onEndLayout != null)
                    onEndLayout(guiState);
            }
        }

        /// <summary>
        /// Checks whether the handle is enabled in the custom editor.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if the handle is enabled. Otherwise, returns `false`.</returns>
        protected bool IsEnabled(IGUIState guiState)
        {
            if (enable != null)
                return enable(guiState);

            return true;
        }
    }
}
