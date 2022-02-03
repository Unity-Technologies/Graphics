using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    interface IInternalStateObserver
    {
        /// <summary>
        /// Gets the last observed component version of <paramref name="stateComponent"/>.
        /// </summary>
        /// <param name="stateComponent">The state component for which to get the last observed version.</param>
        /// <returns>Returns the last observed component version of <paramref name="stateComponent"/>.</returns>
        StateComponentVersion GetLastObservedComponentVersion(IStateComponent stateComponent);

        /// <summary>
        /// Updates the observed version for component <paramref name="stateComponent"/> to <paramref name="newVersion"/>.
        /// </summary>
        /// <param name="stateComponent">The state component for which to update the version.</param>
        /// <param name="newVersion">The new version.</param>
        void UpdateObservedVersion(IStateComponent stateComponent, StateComponentVersion newVersion);
    }

    static class StateObserverHelper
    {
        internal static IStateObserver CurrentObserver { get; set; }
    }
}
