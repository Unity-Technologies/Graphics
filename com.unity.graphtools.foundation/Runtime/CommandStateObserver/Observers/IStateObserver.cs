using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Base interface for state observers.
    /// </summary>
    public interface IStateObserver
    {
        /// <summary>
        /// The state components observed by the observer.
        /// </summary>
        IEnumerable<IStateComponent> ObservedStateComponents { get; }

        /// <summary>
        /// The state components modified by the observer.
        /// </summary>
        IEnumerable<IStateComponent> ModifiedStateComponents { get; }

        /// <summary>
        /// Observes the <see cref="IStateObserver.ObservedStateComponents"/> and modifies the <see cref="IStateObserver.ModifiedStateComponents"/>.
        /// </summary>
        void Observe();
    }
}
