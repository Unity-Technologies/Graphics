using System;
using UnityEngine;

namespace UnityEditor
{
    public class HandlesManipulator
    {
        public Func<IGUIState, bool> enable;
        public Action<IGUIState> onGui;
        public Action<IGUIState> onEndLayout;
        private bool m_Enabled = false;

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

        public void EndLayout(IGUIState guiState)
        {
            if (m_Enabled)
            {
                if (onEndLayout != null)
                    onEndLayout(guiState);
            }
        }

        protected bool IsEnabled(IGUIState guiState)
        {
            if (enable != null)
                return enable(guiState);

            return true;
        }
    }
}
