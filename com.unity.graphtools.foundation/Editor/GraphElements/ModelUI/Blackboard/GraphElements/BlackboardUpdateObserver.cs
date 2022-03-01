using System;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Observer that updates a <see cref="Blackboard"/>.
    /// </summary>
    public class BlackboardUpdateObserver : StateObserver
    {
        protected Blackboard m_Blackboard;
        GraphViewStateComponent m_GraphViewStateComponent;
        SelectionStateComponent m_SelectionStateComponent;
        BlackboardViewStateComponent m_BlackboardViewStateComponent;
        ToolStateComponent m_ToolStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardUpdateObserver" /> class.
        /// </summary>
        /// <param name="blackboard">The <see cref="Blackboard"/> to update.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="blackboardViewState">The blackboard view state component.</param>
        /// <param name="toolState">The tool state component.</param>
        public BlackboardUpdateObserver(Blackboard blackboard, GraphViewStateComponent graphViewState,
            SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewState, ToolStateComponent toolState) :
            base(graphViewState, selectionState, blackboardViewState, toolState)
        {
            m_Blackboard = blackboard;

            m_GraphViewStateComponent = graphViewState;
            m_SelectionStateComponent = selectionState;
            m_BlackboardViewStateComponent = blackboardViewState;
            m_ToolStateComponent = toolState;
        }

        /// <inheritdoc/>
        public override void Observe()
        {
            // PF TODO be smarter about what needs to be updated.

            if (m_Blackboard?.panel != null)
            {
                using (var winObservation = this.ObserveState(m_ToolStateComponent))
                using (var selObservation = this.ObserveState(m_SelectionStateComponent))
                using (var gvObservation = this.ObserveState(m_GraphViewStateComponent))
                using (var bbObservation = this.ObserveState(m_BlackboardViewStateComponent))
                {
                    if (winObservation.UpdateType != UpdateType.None || gvObservation.UpdateType == UpdateType.Complete)
                    {
                        m_Blackboard.UpdateFromModel();
                    }
                    else if (gvObservation.UpdateType == UpdateType.Partial)
                    {
                        var gvChangeSet = m_GraphViewStateComponent.GetAggregatedChangeset(gvObservation.LastObservedVersion);

                        if (gvChangeSet != null)
                        {
                            if (gvChangeSet.NewModels.OfType<IVariableDeclarationModel>().Any() ||
                                gvChangeSet.ChangedModels.OfType<IVariableDeclarationModel>().Any() ||
                                gvChangeSet.DeletedModels.OfType<IVariableDeclarationModel>().Any())
                            {
                                m_Blackboard?.UpdateFromModel();
                            }
                        }
                    }
                    else if (bbObservation.UpdateType != UpdateType.None)
                    {
                        var groups = m_Blackboard.Query<BlackboardVariableGroup>().Build().ToList();
                        foreach (var group in groups)
                        {
                            group.UpdateFromModel();
                        }
                        var rows = m_Blackboard.Query<BlackboardRow>().Build().ToList();
                        foreach (var row in rows)
                        {
                            row.UpdateFromModel();
                        }
                    }
                    else if (selObservation.UpdateType != UpdateType.None)
                    {
                        var selChangeSet = m_SelectionStateComponent.GetAggregatedChangeset(selObservation.LastObservedVersion);
                        if (selChangeSet != null)
                        {
                            foreach (var changedModel in selChangeSet.ChangedModels)
                            {
                                if (changedModel is IHasDeclarationModel hasDeclarationModel)
                                {
                                    var declaration = hasDeclarationModel.DeclarationModel;
                                    var field = declaration.GetUI<BlackboardField>(m_Blackboard.GraphView, BlackboardCreationContext.VariableCreationContext);
                                    field?.UpdateFromModel();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
