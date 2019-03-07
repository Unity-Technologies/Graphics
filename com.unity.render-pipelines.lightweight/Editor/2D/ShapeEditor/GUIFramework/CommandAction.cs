using System;
using UnityEngine;

namespace GUIFramework
{
    public class CommandAction : GUIAction
    {
        private string m_CommandName;

        public Action onCommand;

        public CommandAction(string commandName)
        {
            m_CommandName = commandName;
        }

        protected override bool GetFinishContidtion(IGUIState guiState)
        {
            return true;
        }

        protected override bool GetTriggerContidtion(IGUIState guiState)
        {
            if ((guiState.eventType == EventType.ValidateCommand || guiState.eventType == EventType.ExecuteCommand) && guiState.commandName == m_CommandName)
            {
                if (guiState.eventType == EventType.ExecuteCommand)
                {
                    guiState.UseCurrentEvent();
                    return true;
                }
            }

            return false;
        }

        protected override void OnTrigger(IGUIState guiState)
        {
            if (onCommand != null)
                onCommand();
        }
    }
}
