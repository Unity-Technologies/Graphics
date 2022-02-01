using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A state component holding data about the tracing process.
    /// </summary>
    [Serializable]
    public class TracingDataStateComponent : AssetStateComponent<TracingDataStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="TracingControlStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<TracingDataStateComponent>
        {
            /// <inheritdoc  cref="TracingDataStateComponent.MaxTracingStep"/>
            public int MaxTracingStep
            {
                set
                {
                    m_State.m_MaxTracingStep = value;
                    m_State.SetUpdateType(UpdateType.Complete);
                }
            }

            /// <inheritdoc  cref="TracingDataStateComponent.DebuggingData"/>
            public IReadOnlyList<TracingStep> DebuggingData
            {
                set
                {
                    m_State.DebuggingData = value;
                    m_State.SetUpdateType(UpdateType.Complete);
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
        int m_MaxTracingStep;

        /// <summary>
        /// The last step index.
        /// </summary>
        public int MaxTracingStep => m_MaxTracingStep;

        /// <summary>
        /// Stores the list of steps for the current graph, frame and target tuple
        /// </summary>
        public IReadOnlyList<TracingStep> DebuggingData { get; private set; }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                DebuggingData = null;
            }
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is TracingDataStateComponent tracingDataStateComponent)
            {
                m_MaxTracingStep = tracingDataStateComponent.m_MaxTracingStep;
            }
        }
    }
}
