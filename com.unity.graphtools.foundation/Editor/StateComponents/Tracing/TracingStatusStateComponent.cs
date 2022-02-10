using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A state component holding data to control the tracing process.
    /// </summary>
    [Serializable]
    public class TracingStatusStateComponent : AssetStateComponent<TracingStatusStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="TracingStatusStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<TracingStatusStateComponent>
        {
            /// <inheritdoc  cref="TracingStatusStateComponent.TracingEnabled"/>
            public bool TracingEnabled
            {
                set
                {
                    if (m_State.m_TracingEnabled != value)
                    {
                        m_State.m_TracingEnabled = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <summary>
            /// Saves the state component and replaces it by the state component associated with <paramref name="assetModel"/>.
            /// </summary>
            /// <param name="assetModel">The asset model for which we want to load a state component.</param>
            public void SaveAndLoadStateForAsset(IGraphAssetModel assetModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadAssetStateForAsset(m_State, this, assetModel);
            }
        }

        [SerializeField]
        bool m_TracingEnabled;

        /// <summary>
        /// Whether tracing is enabled or not.
        /// </summary>
        public bool TracingEnabled => m_TracingEnabled;

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is TracingStatusStateComponent tracingStatusStateComponent)
            {
                m_TracingEnabled = tracingStatusStateComponent.m_TracingEnabled;
            }
        }
    }
}
