namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface that defines the target of a command.
    /// </summary>
    public interface ICommandTarget
    {
        /// <summary>
        /// Dispatches a command to this target and its parent, recursively.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="diagnosticsFlags">Diagnostic flags for the dispatch process.</param>
        void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None);
    }
}
