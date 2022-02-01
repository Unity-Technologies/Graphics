using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for implementations of <see cref="IAssetStateComponent"/>.
    /// </summary>
    [Serializable]
    public abstract class AssetStateComponent<TUpdater> : StateComponent<TUpdater>, IAssetStateComponent
        where TUpdater : class, IStateComponentUpdater, new()
    {
        [SerializeField]
        string m_AssetKey;

        /// <inheritdoc />
        public string AssetKey
        {
            get => m_AssetKey;
            set => m_AssetKey = value;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PersistedState.StoreStateComponent(this, ComponentName, default, AssetKey);
            }
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is AssetStateComponent<TUpdater> assetStateComponent)
            {
                m_AssetKey = assetStateComponent.AssetKey;
            }
        }
    }
}
