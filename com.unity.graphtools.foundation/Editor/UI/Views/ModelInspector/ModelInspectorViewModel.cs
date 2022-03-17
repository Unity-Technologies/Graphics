using System;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ModelInspectorViewModel: RootViewModel
    {
        /// <summary>
        /// The model inspector state component.
        /// </summary>
        public ModelInspectorStateComponent ModelInspectorState { get; }

        /// <summary>
        /// The graph model state from the parent graph view.
        /// </summary>
        public GraphModelStateComponent GraphModelState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorViewModel"/> class.
        /// </summary>
        public ModelInspectorViewModel(GraphView graphView)
        {
            GraphModelState = graphView?.GraphViewModel.GraphModelState;
            var graphModel = GraphModelState?.GraphModel;
            var lastSelectedNode = graphView?.GraphViewModel.SelectionState.GetSelection(graphModel).LastOrDefault(t => t is INodeModel || t is IVariableDeclarationModel);

            ModelInspectorState = new ModelInspectorStateComponent(new[] { lastSelectedNode }, graphModel);
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
            state?.AddStateComponent(ModelInspectorState);
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
            state?.RemoveStateComponent(ModelInspectorState);
        }
    }
}
