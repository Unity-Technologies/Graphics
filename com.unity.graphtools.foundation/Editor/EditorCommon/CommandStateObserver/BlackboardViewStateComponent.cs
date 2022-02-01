using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// State component holding blackboard view related data.
    /// </summary>
    [Serializable]
    public class BlackboardViewStateComponent : AssetViewStateComponent<BlackboardViewStateComponent.StateUpdater>
    {
        /// <summary>
        /// An observer that updates the <see cref="BlackboardViewStateComponent"/> when a graph is loaded.
        /// </summary>
        public class GraphAssetLoadedObserver : StateObserver
        {
            ToolStateComponent m_ToolStateComponent;
            BlackboardViewStateComponent m_BlackboardViewStateComponent;

            /// <summary>
            /// Initializes a new instance of the <see cref="GraphAssetLoadedObserver"/> class.
            /// </summary>
            public GraphAssetLoadedObserver(ToolStateComponent toolStateComponent, BlackboardViewStateComponent blackboardViewStateComponent)
                : base(new [] { toolStateComponent},
                    new [] { blackboardViewStateComponent })
            {
                m_ToolStateComponent = toolStateComponent;
                m_BlackboardViewStateComponent = blackboardViewStateComponent;
            }

            /// <inheritdoc />
            public override void Observe()
            {
                using (var obs = this.ObserveState(m_ToolStateComponent))
                {
                    if (obs.UpdateType != UpdateType.None)
                    {
                        using (var updater = m_BlackboardViewStateComponent.UpdateScope)
                        {
                            updater.SaveAndLoadStateForAsset(m_ToolStateComponent.AssetModel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The updater for the <see cref="BlackboardViewStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<BlackboardViewStateComponent>
        {
            /// <summary>
            /// Sets the expanded state of the variable declaration model in the blackboard.
            /// </summary>
            /// <param name="model">The model for which to set the state.</param>
            /// <param name="expanded">True if the variable should be expanded, false otherwise.</param>
            public void SetVariableDeclarationModelExpanded(IVariableDeclarationModel model, bool expanded)
            {
                bool isExpanded = m_State.GetVariableDeclarationModelExpanded(model);
                if (isExpanded && !expanded)
                {
                    m_State.m_BlackboardExpandedRowStates?.Remove(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Complete);
                }
                else if (!isExpanded && expanded)
                {
                    m_State.m_BlackboardExpandedRowStates?.Add(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Complete);
                }
            }

            /// <summary>
            /// Sets the expanded state of the variable group model in the blackboard.
            /// </summary>
            /// <param name="model">The model for which to set the state.</param>
            /// <param name="expanded">True if the group should be expanded, false otherwise.</param>
            public void SetVariableGroupModelExpanded(IGroupModel model, bool expanded)
            {
                bool isExpanded = m_State.GetVariableGroupExpanded(model);
                if (!isExpanded && expanded)
                {
                    m_State.m_BlackboardCollapsedGroupStates?.Remove(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Complete);
                }
                else if (isExpanded && !expanded)
                {
                    m_State.m_BlackboardCollapsedGroupStates?.Add(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Complete);
                }
            }

            /// <summary>
            /// Saves the state component and replaces it by the state component associated with <paramref name="assetModel"/>.
            /// </summary>
            /// <param name="assetModel">The asset model for which we want to load a state component.</param>
            public void SaveAndLoadStateForAsset(IGraphAssetModel assetModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadAssetViewStateForAsset(m_State, this, assetModel);
            }
        }

        [SerializeField]
        List<string> m_BlackboardExpandedRowStates;

        [SerializeField]
        List<string> m_BlackboardCollapsedGroupStates;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardViewStateComponent" /> class.
        /// </summary>
        public BlackboardViewStateComponent()
        {
            m_BlackboardExpandedRowStates = new List<string>();
            m_BlackboardCollapsedGroupStates = new List<string>();
        }

        /// <summary>
        /// Gets the expanded state of a variable declaration model.
        /// </summary>
        /// <param name="model">The variable declaration model.</param>
        /// <returns>True is the UI for the model should be expanded. False otherwise.</returns>
        public bool GetVariableDeclarationModelExpanded(IVariableDeclarationModel model)
        {
            return m_BlackboardExpandedRowStates?.Contains(model.Guid.ToString()) ?? false;
        }

        /// <summary>
        /// Gets the expanded state of a variable declaration model.
        /// </summary>
        /// <param name="model">The variable declaration model.</param>
        /// <returns>True is the UI for the model should be expanded. False otherwise.</returns>
        public bool GetVariableGroupExpanded(IGroupModel model)
        {
            return !(m_BlackboardCollapsedGroupStates?.Contains(model.Guid.ToString()) ?? false);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is BlackboardViewStateComponent blackboardViewStateComponent)
            {
                m_BlackboardExpandedRowStates = blackboardViewStateComponent.m_BlackboardExpandedRowStates;
                blackboardViewStateComponent.m_BlackboardExpandedRowStates = null;

                m_BlackboardCollapsedGroupStates = blackboardViewStateComponent.m_BlackboardCollapsedGroupStates;
                blackboardViewStateComponent.m_BlackboardCollapsedGroupStates = null;
            }
        }
    }
}
