using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Extension methods for <see cref="IStateObserver"/>.
    /// </summary>
    public static class StateObserverExtensions
    {
        /// <summary>
        /// Creates a new <see cref="Observation"/> instance that will update the observer's last observed version.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <param name="stateComponent">The observed state component.</param>
        /// <returns>An <see cref="Observation"/> object.</returns>
        public static Observation ObserveState(this IStateObserver observer, IStateComponent stateComponent)
        {
            return new Observation(observer, stateComponent);
        }

        /// <summary>
        /// Creates a new <see cref="Observation"/> instance that will not update the observer's last observed version.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <param name="stateComponent">The observed state component.</param>
        /// <returns>An <see cref="Observation"/> object.</returns>
        public static Observation PeekAtState(this IStateObserver observer, IStateComponent stateComponent)
        {
            return new Observation(observer, stateComponent, false);
        }
    }
}
