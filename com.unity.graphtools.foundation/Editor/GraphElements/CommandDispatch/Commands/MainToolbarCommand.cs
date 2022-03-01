using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to set tracing on or off.
    /// </summary>
    public class ActivateTracingCommand : UndoableCommand
    {
        /// <summary>
        /// Whether tracing should be active or not.
        /// </summary>
        public bool Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivateTracingCommand"/> class.
        /// </summary>
        public ActivateTracingCommand()
        {
            UndoString = "Activate Tracing";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivateTracingCommand"/> class.
        /// </summary>
        /// <param name="value">True if tracing should be activated, false otherwise.</param>
        public ActivateTracingCommand(bool value) : this()
        {
            Value = value;

            if (!Value)
            {
                UndoString = "Deactivate Tracing";
            }
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="toolStateComponent">The window state component.</param>
        /// <param name="tracingStatusState">The tracing status state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(ToolStateComponent toolStateComponent, TracingStatusStateComponent tracingStatusState, ActivateTracingCommand command)
        {
            var graphModel = toolStateComponent.GraphModel;
            if (graphModel?.Stencil == null)
                return;

            using (var updater = tracingStatusState.UpdateScope)
            {
                updater.TracingEnabled = command.Value;
            }
        }
    }
}
