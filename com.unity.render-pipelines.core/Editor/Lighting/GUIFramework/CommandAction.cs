using System;
using UnityEngine;

namespace UnityEditor
{
    public class CommandAction : GUIAction
    {
        private string m_CommandName;

        public Action<IGUIState> onCommand;

        public CommandAction(string commandName)
        {
            m_CommandName = commandName;
        }

        protected override bool GetTriggerCondition(IGUIState guiState)
        {
            if (guiState.eventType == EventType.ValidateCommand && guiState.commandName == m_CommandName)
            {
                guiState.UseEvent();
                return true;
            }

            return false;
        }
        
        protected override bool GetFinishCondition(IGUIState guiState)
        {
            if (guiState.eventType == EventType.ExecuteCommand && guiState.commandName == m_CommandName)
            {
                guiState.UseEvent();
                
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
