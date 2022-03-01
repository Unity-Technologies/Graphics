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
        /// Saves the state component and move the state component associated with <paramref name="assetModel"/> in it.
        /// </summary>
        /// <param name="stateComponent">The state component to save and replace.</param>
        /// <param name="updater">The state component updater used to move the newly state component in <paramref name="stateComponent"/>.</param>
        /// <param name="assetModel">The asset model for which we want to load a state component.</param>
        /// <typeparam name="TComponent">The state component type.</typeparam>
        /// <typeparam name="TUpdater">The updater type.</typeparam>
        public static void SaveAndLoadAssetStateForAsset<TComponent, TUpdater>(TComponent stateComponent, TUpdater updater, IGraphAssetModel assetModel)
            where TComponent : StateComponent<TUpdater>, IAssetStateComponent, new()
            where TUpdater : class, IStateComponentUpdater, new()
        {
            var newAssetKey = PersistedState.MakeAssetKey(assetModel);
            PersistedState.StoreStateComponent(stateComponent, stateComponent.ComponentName, default, stateComponent.AssetKey);

            if (newAssetKey != stateComponent.AssetKey)
            {
                var newState = PersistedState.GetOrCreateAssetStateComponent<TComponent>(stateComponent.ComponentName, newAssetKey);
                updater.Move(newState);
            }
        }

        /// <summary>
        /// Saves the state component and move the state component associated with <paramref name="assetModel"/> in it.
        /// </summary>
        /// <param name="stateComponent">The state component to save and replace.</param>
        /// <param name="updater">The state component updater used to move the newly state component in <paramref name="stateComponent"/>.</param>
        /// <param name="assetModel">The asset model for which we want to load a state component.</param>
        /// <typeparam name="TComponent">The state component type.</typeparam>
        /// <typeparam name="TUpdater">The updater type.</typeparam>
        public static void SaveAndLoadAssetViewStateForAsset<TComponent, TUpdater>(TComponent stateComponent, TUpdater updater, IGraphAssetModel assetModel)
            where TComponent : StateComponent<TUpdater>, IAssetViewStateComponent, new()
            where TUpdater : class, IStateComponentUpdater, new()
        {
            var newAssetKey = PersistedState.MakeAssetKey(assetModel);
            PersistedState.StoreStateComponent(stateComponent, stateComponent.ComponentName, stateComponent.ViewGuid, stateComponent.AssetKey);

            if (newAssetKey != stateComponent.AssetKey)
            {
                var newState = PersistedState.GetOrCreateAssetViewStateComponent<TComponent>(stateComponent.ComponentName, stateComponent.ViewGuid, newAssetKey);
                updater.Move(newState);
            }
        }
    }
}
