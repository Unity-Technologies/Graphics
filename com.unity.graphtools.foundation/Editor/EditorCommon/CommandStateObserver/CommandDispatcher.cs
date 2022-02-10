using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command dispatcher class for graph tools.
    /// </summary>
    public class CommandDispatcher : Dispatcher
    {
        internal string LastDispatchedCommandName { get; private set; }

        /// <summary>
        /// Diagnostic flags to add to every <see cref="Dispatch"/> call.
        /// </summary>
        public Diagnostics DiagnosticFlags { get; set; }

        /// <inheritdoc />
        protected override void PreDispatchCommand(ICommand command)
        {
            base.PreDispatchCommand(command);
            LastDispatchedCommandName = command.GetType().Name;
        }

        /// <inheritdoc />
        public override void Dispatch(ICommand command, Diagnostics diagnosticFlags = Diagnostics.None)
        {
            base.Dispatch(command, diagnosticFlags | DiagnosticFlags);
        }
    }
}
