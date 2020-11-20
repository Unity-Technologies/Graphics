using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Command Acction
    /// </summary>
    public class CommandAction : GUIAction
    {
        private string m_CommandName;

        /// <summary>
        /// The command
        /// </summary>
        public Action<IGUIState> onCommand;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="commandName">Command name</param>
        public CommandAction(string commandName)
        {
            m_CommandName = commandName;
        }

        /// <summary>
        /// Get trigger condition
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if the trigger condition is validated</returns>
        protected override bool GetTriggerCondition(IGUIState guiState)
        {
            if (guiState.eventType == EventType.ValidateCommand && guiState.commandName == m_CommandName)
            {
                guiState.UseEvent();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get finish conditions
        /// </summary>
        /// <param name="guiState">The gui state</param>
        /// <returns>true if the trigger condition is finished</returns>
        protected override bool GetFinishCondition(IGUIState guiState)
        {
            if (guiState.eventType == EventType.ExecuteCommand && guiState.commandName == m_CommandName)
            {
                guiState.UseEvent();

                return true;
            }

            return false;
        }

        /// <summary>
        /// On finish
        /// </summary>
        /// <param name="guiState">The gui state</param>
        protected override void OnFinish(IGUIState guiState)
        {
            if (onCommand != null)
                onCommand(guiState);
        }
    }
}
