using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A state component holding data to control the tracing process.
    /// </summary>
    [Serializable]
    public class TracingControlStateComponent : AssetStateComponent<TracingControlStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="TracingControlStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<TracingControlStateComponent>
        {
            /// <inheritdoc  cref="TracingControlStateComponent.CurrentTracingTarget"/>
            public int CurrentTracingTarget
            {
                set
                {
                    if (m_State.m_CurrentTracingTarget != value)
                    {
                        m_State.m_CurrentTracingTarget = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <inheritdoc  cref="TracingControlStateComponent.CurrentTracingFrame"/>
            public int CurrentTracingFrame
            {
                // Getter is here for convenience of using increment and decrement operator.
                get => m_State.m_CurrentTracingFrame;
                set
                {
                    if (m_State.m_CurrentTracingFrame != value)
                    {
                        m_State.m_CurrentTracingFrame = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <inheritdoc  cref="TracingControlStateComponent.CurrentTracingStep"/>
            public int CurrentTracingStep
            {
                // Getter is here for convenience of using increment and decrement operator.
                get => m_State.m_CurrentTracingStep;
                set
                {
                    if (m_State.m_CurrentTracingStep != value)
                    {
                        m_State.m_CurrentTracingStep = value;
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
        int m_CurrentTracingTarget = -1;

        [SerializeField]
        int m_CurrentTracingFrame;

        [SerializeField]
        int m_CurrentTracingStep;

        /// <summary>
        /// The current tracing target index.
        /// </summary>
        public int CurrentTracingTarget => m_CurrentTracingTarget;

        /// <summary>
        /// The current frame index.
        /// </summary>
        public int CurrentTracingFrame => m_CurrentTracingFrame;

        /// <summary>
        /// The current step index.
        /// </summary>
        public int CurrentTracingStep => m_CurrentTracingStep;

        /// <summary>
        /// Initializes a new instance of the <see cref="TracingControlStateComponent" /> class.
        /// </summary>
        public TracingControlStateComponent()
        {
            m_CurrentTracingStep = -1;
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is TracingControlStateComponent tracingControlStateComponent)
            {
                m_CurrentTracingTarget = tracingControlStateComponent.m_CurrentTracingTarget;
                m_CurrentTracingFrame = tracingControlStateComponent.m_CurrentTracingFrame;
                m_CurrentTracingStep = tracingControlStateComponent.m_CurrentTracingStep;
            }
        }
    }
}
