using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D.GUIFramework
{
    internal class CommandAction : GUIAction
    {
        private string m_CommandName;

        public Action<IGUIState> onCommand;

        public CommandAction(string commandName)
        {
            m_CommandName = commandName;
        }

        protected override bool GetTriggerContidtion(IGUIState guiState)
        {
            if (guiState.eventType == EventType.ValidateCommand && guiState.commandName == m_CommandName)
            {
                guiState.UseCurrentEvent();
                return true;
            }

            return false;
        }

        protected override bool GetFinishContidtion(IGUIState guiState)
        {
            if (guiState.eventType == EventType.ExecuteCommand && guiState.commandName == m_CommandName)
            {
                guiState.UseCurrentEvent();

                return true;
            }

            return false;
        }

        protected override void OnFinish(IGUIState guiState)
        {
            if (onCommand != null)
                onCommand(guiState);
        }
    }
}
