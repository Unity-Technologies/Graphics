using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Helper methods for state components that are persisted to the file system.
    /// </summary>
    public static class PersistedStateComponentHelpers
    {
        /// <summary>
        /// Saves the state component and move the state component associated with <paramref name="graphModel"/> in it.
        /// </summary>
        /// <param name="stateComponent">The state component to save and replace.</param>
        /// <param name="updater">The state component updater used to move the newly state component in <paramref name="stateComponent"/>.</param>
        /// <param name="graphModel">The graph model for which we want to load a state component.</param>
        /// <typeparam name="TComponent">The state component type.</typeparam>
        /// <typeparam name="TUpdater">The updater type.</typeparam>
        public static void SaveAndLoadPersistedStateForGraph<TComponent, TUpdater>(TComponent stateComponent, TUpdater updater, IGraphModel graphModel)
            where TComponent : StateComponent<TUpdater>, IPersistedStateComponent, new()
            where TUpdater : class, IStateComponentUpdater, new()
        {
            var key = PersistedState.MakeGraphKey(graphModel);
            PersistedState.StoreStateComponent(stateComponent, stateComponent.ComponentName, stateComponent.ViewGuid, stateComponent.GraphKey);

            if (key != stateComponent.GraphKey)
            {
                var newState = PersistedState.GetOrCreatePersistedStateComponent<TComponent>(stateComponent.ComponentName, stateComponent.ViewGuid, key);
                updater.Move(newState);
            }
        }
    }
}
