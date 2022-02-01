using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface for state component updaters.
    /// </summary>
    public interface IStateComponentUpdater : IDisposable
    {
        /// <summary>
        /// Initialize the updater with the state to update.
        /// </summary>
        /// <param name="state">The state to update.</param>
        void Initialize(IStateComponent state);

        /// <summary>
        /// Moves the content of another state component into this state component.
        /// </summary>
        /// <param name="other">The source state component.</param>
        /// <remarks>The <paramref name="other"/> state components will be discarded after the call to Move.
        /// This means you do not need to make a deep copy of the data: just copying the references is sufficient.
        /// </remarks>
        void Move(IStateComponent other);
    }
}
