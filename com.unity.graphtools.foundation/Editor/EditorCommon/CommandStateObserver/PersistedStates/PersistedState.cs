using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Used to persist the tool editor state to file system, in the k_StateCache folder.
    /// </summary>
    public static class PersistedState
    {
        static readonly StateCache k_StateCache = new StateCache("Library/StateCache/ToolState/");

        /// <summary>
        /// Generates a unique key for a <see cref="IGraphAssetModel"/>.
        /// </summary>
        /// <param name="graphAssetModel">The asset for which to generate a key.</param>
        /// <returns>A unique key for the asset.</returns>
        public static string MakeAssetKey(IGraphAssetModel graphAssetModel)
        {
            var obj = graphAssetModel as Object;
            if (obj == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long localId))
                return graphAssetModel?.Name ?? "";

            return $"{guid}/{localId}";
        }

        static Hash128 GetComponentStorageHash(string componentName, Hash128 viewGuid, string assetKey = "")
        {
            Hash128 hash = default;
            hash.Append(componentName);
            hash.Append(viewGuid.ToString());
            hash.Append(assetKey);
            return hash;
        }

        /// <summary>
        /// Gets a state component of type <typeparamref name="TComponent"/> associated to <paramref name="viewGuid"/>. If none exists, creates a new one.
        /// </summary>
        /// <param name="componentName">The name of the component. If null, the component type name will be used.</param>
        /// <param name="viewGuid">The guid identifying the view.</param>
        /// <typeparam name="TComponent">The type of component to create.</typeparam>
        /// <returns>A state component of the requested type, loaded from the state cache or newly created.</returns>
        public static TComponent GetOrCreateViewStateComponent<TComponent>(string componentName, Hash128 viewGuid)
            where TComponent : class, IViewStateComponent, new()
        {
            componentName ??= typeof(TComponent).FullName;

            var componentKey = GetComponentStorageHash(componentName, viewGuid);
            return k_StateCache.GetState(componentKey,
                () => new TComponent
                {
                    ViewGuid = viewGuid,
                });
        }

        /// <summary>
        /// Gets a state component of type <typeparamref name="TComponent"/> associated to the asset. If none exists, creates a new one.
        /// </summary>
        /// <param name="componentName">The name of the component. If null, the component type name will be used.</param>
        /// <param name="assetKey">A unique key representing the asset backing this component.</param>
        /// <typeparam name="TComponent">The type of component to create.</typeparam>
        /// <returns>A state component of the requested type, loaded from the state cache or newly created.</returns>
        public static TComponent GetOrCreateAssetStateComponent<TComponent>(string componentName, string assetKey)
            where TComponent : class, IAssetStateComponent, new()
        {
            componentName ??= typeof(TComponent).FullName;

            var componentKey = GetComponentStorageHash(componentName, default, assetKey);
            return k_StateCache.GetState(componentKey,
                () => new TComponent
                {
                    AssetKey = assetKey
                });
        }

        /// <summary>
        /// Gets a state component of type <typeparamref name="TComponent"/> associated to <paramref name="viewGuid"/> and the asset.
        /// If none exists, creates a new one.
        /// </summary>
        /// <param name="componentName">The name of the component. If null, the component type name will be used.</param>
        /// <param name="viewGuid">The guid identifying the view.</param>
        /// <param name="assetKey">A unique key representing the asset backing this component.</param>
        /// <typeparam name="TComponent">The type of component to create.</typeparam>
        /// <returns>A state component of the requested type, loaded from the state cache or newly created.</returns>
        public static TComponent GetOrCreateAssetViewStateComponent<TComponent>(string componentName, Hash128 viewGuid, string assetKey)
            where TComponent : class, IAssetViewStateComponent, new()
        {
            componentName ??= typeof(TComponent).FullName;

            var componentKey = GetComponentStorageHash(componentName, viewGuid, assetKey);
            return k_StateCache.GetState(componentKey,
                () => new TComponent
                {
                    ViewGuid = viewGuid,
                    AssetKey = assetKey
                });
        }

        /// <summary>
        /// Adds a state component to the state cache, using <paramref name="componentName"/>,
        /// <paramref name="viewGuid"/> and <paramref name="assetKey"/> to build a unique key for the state component.
        /// </summary>
        /// <param name="stateComponent">The state component to write.</param>
        /// <param name="componentName">The name of the state component.</param>
        /// <param name="viewGuid">The view GUID for the state component. Can be default
        /// if no view is associated with the state component.</param>
        /// <param name="assetKey">The asset key for the state component. Can be default
        /// if no asset is associated with the state component.</param>
        public static void StoreStateComponent(IStateComponent stateComponent, string componentName, Hash128 viewGuid, string assetKey)
        {
            var componentKey = GetComponentStorageHash(componentName, viewGuid, assetKey);
            k_StateCache.StoreState(componentKey, stateComponent);
        }

        /// <summary>
        /// Writes all state components to disk.
        /// </summary>
        public static void Flush()
        {
            k_StateCache.Flush();
        }
    }
}
