using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Represents an Action to process when the custom editor validates a command.
    /// </summary>
    public class CommandAction : GUIAction
    {
        private string m_CommandName;

        /// <summary>
        /// The Action to execute.
        /// </summary>
        public Action<IGUIState> onCommand;

        /// <summary>
        /// Initializes and returns an instance of CommandAction
        /// </summary>
        /// <param name="commandName">The name of the command. When the custom editor validates a command with this name, it triggers the action.</param>
        public CommandAction(string commandName)
        {
            m_CommandName = commandName;
        }

        /// <summary>
        /// Checks to see if the trigger condition has been met or not.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if the trigger condition has been met. Otherwise, returns `false`.</returns>
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
        /// Checks to see if the finish condition has been met or not. 
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if the trigger condition is finished. Otherwise, returns `false`.</returns>
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
        /// Calls the methods in its invocation list when the finish condition is met.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected override void OnFinish(IGUIState guiState)
        {
            if (onCommand != null)
                onCommand(guiState);
        }
    }
}
