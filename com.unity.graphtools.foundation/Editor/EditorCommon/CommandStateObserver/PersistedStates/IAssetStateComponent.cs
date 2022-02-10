using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for state components that store data associated with an asset.
    /// </summary>
    /// <remarks>
    /// An IAssetStateComponent is state information that is tied to an asset only and will apply to any view.
    /// Example: the dirty state of the asset. When the asset is dirtied, we want to refresh all views
    /// that display the asset.
    /// </remarks>
    public interface IAssetStateComponent : IStateComponent
    {
        /// <summary>
        /// A unique key for the asset associated with this state component.
        /// </summary>
        string AssetKey { get; set; }
    }
}
