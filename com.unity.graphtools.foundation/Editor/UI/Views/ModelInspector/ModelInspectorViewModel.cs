using System;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

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
        public GraphModelStateComponent GraphModelState => ParentGraphView?.GraphViewModel.GraphModelState;

        /// <summary>
        /// The parent graph view.
        /// </summary>
        public GraphView ParentGraphView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorViewModel"/> class.
        /// </summary>
        public ModelInspectorViewModel(GraphView graphView)
        {
            m_Guid = new SerializableGUID(GetType().FullName + graphView.GraphViewModel.Guid);

            ParentGraphView = graphView;

            var graphModel = GraphModelState?.GraphModel;
            var lastSelectedNode = graphView.GraphViewModel.SelectionState.GetSelection(graphModel).LastOrDefault(t => t is INodeModel || t is IVariableDeclarationModel);

            var key = PersistedState.MakeGraphKey(graphModel);
            ModelInspectorState = PersistedState.GetOrCreatePersistedStateComponent(default, Guid, key,
                () => new ModelInspectorStateComponent(new[] { lastSelectedNode }, graphModel));
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
