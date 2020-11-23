using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Handles Manipulator
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
        /// Calls the methods in its invocation list when enter in GUI
        /// </summary>
        /// <param name="guiState">The GUI State</param>
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
        /// EndLayout should be called to close an invocation of a BeginLayout
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        public void EndLayout(IGUIState guiState)
        {
            if (m_Enabled)
            {
                if (onEndLayout != null)
                    onEndLayout(guiState);
            }
        }

        /// <summary>
        /// Calls the methods in its invocation list to test if enabled
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if enabled</returns>
        protected bool IsEnabled(IGUIState guiState)
        {
            if (enable != null)
                return enable(guiState);

            return true;
        }
    }
}
