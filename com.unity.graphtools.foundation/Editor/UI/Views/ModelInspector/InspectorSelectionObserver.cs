using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class InspectorSelectionObserver : StateObserver
    {
        ToolStateComponent m_ToolState;
        GraphModelStateComponent m_GraphModelStateComponent;
        List<SelectionStateComponent> m_SelectionStates;
        ModelInspectorStateComponent m_ModelInspectorState;

        List<IModel> m_LastSelection;

        public InspectorSelectionObserver(ToolStateComponent toolState, GraphModelStateComponent graphModelState,
            IReadOnlyCollection<SelectionStateComponent> selectionStates, ModelInspectorStateComponent modelInspectorState)
            : base(new IStateComponent[] { toolState, graphModelState }.Concat(selectionStates),
                new[] { modelInspectorState })
        {
            m_ToolState = toolState;
            m_GraphModelStateComponent = graphModelState;
            m_SelectionStates = selectionStates.ToList();
            m_ModelInspectorState = modelInspectorState;
        }

        public override void Observe()
        {
            var selectionObservations = this.ObserveStates(m_SelectionStates).ToList();
            try
            {
                using (var toolObservation = this.ObserveState(m_ToolState))
                using (var gvObservation = this.ObserveState(m_GraphModelStateComponent))
                {
                    var selectionUpdateType = UpdateTypeExtensions.Combine(selectionObservations.Select(s => s.UpdateType));
                    var updateType = toolObservation.UpdateType.Combine(selectionUpdateType);

                    if (updateType != UpdateType.None || gvObservation.UpdateType == UpdateType.Complete)
                    {
                        var graphModel = m_GraphModelStateComponent.GraphModel;
                        var selection = m_SelectionStates.SelectMany(s => s.GetSelection(graphModel));
                        var selectedModels = selection.OfType<IModel>().Where(t => t is INodeModel || t is IVariableDeclarationModel).Distinct().ToList();
                        if (selectedModels.Count == 0)
                        {
                            selectedModels.Add(graphModel);
                        }

                        // GTF-EDIT: Caching the view state if the selection hasnt changed
                        if (m_LastSelection?.Count() == 1 && selectedModels.Count() == 1)
                        {
                            var sgModel1 = selectedModels.FirstOrDefault();
                            var sgModel2 = m_LastSelection.FirstOrDefault();
                            if(ReferenceEquals(sgModel1, sgModel2) && sgModel1 is IGraphModel)
                                return;
                        }

                        using (var updater = m_ModelInspectorState.UpdateScope)
                        {
                            updater.SetInspectedModels(selectedModels, graphModel);
                            m_LastSelection = selectedModels;
                        }
                    }
                }
            }
            finally
            {
                foreach (var selectionObservation in selectionObservations)
                {
                    selectionObservation?.Dispose();
                }
            }
        }
    }
}
