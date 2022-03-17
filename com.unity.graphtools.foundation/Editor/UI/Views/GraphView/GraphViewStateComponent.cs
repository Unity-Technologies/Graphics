using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A component to hold the editor state of the <see cref="GraphView"/> for a graph asset.
    /// </summary>
    [Serializable]
    public class GraphViewStateComponent : AssetViewStateComponent<GraphViewStateComponent.StateUpdater>
    {
        /// <summary>
        /// An observer that updates the <see cref="GraphViewStateComponent"/> when a graph is loaded.
        /// </summary>
        public class GraphAssetLoadedObserver : StateObserver
        {
            ToolStateComponent m_ToolStateComponent;
            GraphViewStateComponent m_GraphViewStateComponent;

            /// <summary>
            /// Initializes a new instance of the <see cref="GraphAssetLoadedObserver"/> class.
            /// </summary>
            public GraphAssetLoadedObserver(ToolStateComponent toolStateComponent, GraphViewStateComponent graphViewStateComponent)
                : base(new [] { toolStateComponent},
                    new IStateComponent[] { graphViewStateComponent })
            {
                m_ToolStateComponent = toolStateComponent;
                m_GraphViewStateComponent = graphViewStateComponent;
            }

            /// <inheritdoc />
            public override void Observe()
            {
                using (var obs = this.ObserveState(m_ToolStateComponent))
                {
                    if (obs.UpdateType != UpdateType.None)
                    {
                        using (var updater = m_GraphViewStateComponent.UpdateScope)
                        {
                            updater.SaveAndLoadStateForAsset(m_ToolStateComponent.AssetModel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updater for the <see cref="GraphViewStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<GraphViewStateComponent>
        {
            /// <summary>
            /// Saves the current state and loads the state associated with <paramref name="assetModel"/>.
            /// </summary>
            /// <param name="assetModel">The graph asset for which to load the state component.</param>
            public void SaveAndLoadStateForAsset(IGraphAssetModel assetModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadAssetViewStateForAsset(m_State, this, assetModel);
            }

            /// <summary>
            /// The scale factor of the <see cref="GraphView"/>.
            /// </summary>
            public Vector3 Scale
            {
                set
                {
                    if (m_State.m_Scale != value)
                    {
                        m_State.m_Scale = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <summary>
            /// The position of the <see cref="GraphView"/>.
            /// </summary>
            public Vector3 Position
            {
                set
                {
                    if (m_State.m_Position != value)
                    {
                        m_State.m_Position = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <summary>
            /// Sets the index of the current error; used by the error toolbar.
            /// </summary>
            /// <param name="index">The index of the error.</param>
            public void SetCurrentErrorIndex(int index)
            {
                m_State.ErrorIndex = index;
            }

            /// <summary>
            /// Marks the state component as changed. Used to communicate UI-only changes
            /// that occurs in <see cref="SelectionDragger"/> and <see cref="ContentDragger"/>
            /// to the <see cref="MiniMap"/>.
            /// </summary>
            internal void MarkContentUpdated()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        [SerializeField]
        Vector3 m_Scale = Vector3.one;

        [SerializeField]
        Vector3 m_Position = Vector3.zero;

        /// <summary>
        /// The scale (zoom factor) of the graph view.
        /// </summary>
        public Vector3 Scale => m_Scale;

        /// <summary>
        /// The position of the graph view.
        /// </summary>
        public Vector3 Position => m_Position;

        /// <summary>
        /// The index of the current error.
        /// </summary>
        public int ErrorIndex { get; private set; }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is GraphViewStateComponent graphViewStateComponent)
            {
                m_Scale = graphViewStateComponent.m_Scale;
                m_Position = graphViewStateComponent.m_Position;
                ErrorIndex = graphViewStateComponent.ErrorIndex;
            }
        }
    }
}
