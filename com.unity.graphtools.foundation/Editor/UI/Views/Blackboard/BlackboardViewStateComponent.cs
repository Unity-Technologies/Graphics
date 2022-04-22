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
    public class BlackboardViewStateComponent : PersistedStateComponent<BlackboardViewStateComponent.StateUpdater>
    {
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
                    m_State.CurrentChangeset.ChangedModels.Add(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Partial);
                }
                else if (!isExpanded && expanded)
                {
                    m_State.m_BlackboardExpandedRowStates?.Add(model.Guid.ToString());
                    m_State.CurrentChangeset.ChangedModels.Add(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Sets the expanded state of the group model in the blackboard.
            /// </summary>
            /// <param name="model">The model for which to set the state.</param>
            /// <param name="expanded">True if the group should be expanded, false otherwise.</param>
            public void SetGroupModelExpanded(IGroupModel model, bool expanded)
            {
                bool isExpanded = m_State.GetGroupExpanded(model);
                if (!isExpanded && expanded)
                {
                    m_State.m_BlackboardCollapsedGroupStates?.Remove(model.Guid.ToString());
                    m_State.CurrentChangeset.ChangedModels.Add(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Partial);
                }
                else if (isExpanded && !expanded)
                {
                    m_State.m_BlackboardCollapsedGroupStates?.Add(model.Guid.ToString());
                    m_State.CurrentChangeset.ChangedModels.Add(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Sets the Blackboard ScrollView scroll offset.
            /// </summary>
            /// <param name="scrollOffset">The horizontal and vertical offsets for the ScrollView.</param>
            public void SetScrollOffset(Vector2 scrollOffset)
            {
                m_State.m_ScrollOffset = scrollOffset;
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Saves the state component and replaces it by the state component associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The asset for which we want to load a state component.</param>
            public void SaveAndLoadStateForGraph(IGraphModel graphModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph(m_State, this, graphModel);
            }
        }

        ChangesetManager<SimpleChangeset<string>> m_ChangesetManager = new ChangesetManager<SimpleChangeset<string>>();

        /// <inheritdoc />
        protected override IChangesetManager ChangesetManager => m_ChangesetManager;

        SimpleChangeset<string> CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        [SerializeField]
        List<string> m_BlackboardExpandedRowStates;

        [SerializeField]
        List<string> m_BlackboardCollapsedGroupStates;

        [SerializeField]
        Vector2 m_ScrollOffset;

        /// <summary>
        /// The scroll offset of the blackboard scroll view.
        /// </summary>
        public Vector2 ScrollOffset => m_ScrollOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardViewStateComponent" /> class.
        /// </summary>
        public BlackboardViewStateComponent()
        {
            m_BlackboardExpandedRowStates = new List<string>();
            m_BlackboardCollapsedGroupStates = new List<string>();
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public SimpleChangeset<string> GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
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
        public bool GetGroupExpanded(IGroupModel model)
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

                m_ScrollOffset = blackboardViewStateComponent.m_ScrollOffset;
            }
        }
    }
}
