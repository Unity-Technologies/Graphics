using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class MiniMapViewModel : RootViewModel
    {
        public GraphViewStateComponent GraphViewState => ParentGraphView?.GraphViewModel.GraphViewState;
        public GraphModelStateComponent GraphModelState => ParentGraphView?.GraphViewModel.GraphModelState;
        public SelectionStateComponent SelectionState => ParentGraphView?.GraphViewModel.SelectionState;

        /// <summary>
        /// The <see cref="IGraphModel"/> displayed by the MiniMapView.
        /// </summary>
        public IGraphModel GraphModel => ParentGraphView?.GraphViewModel.GraphModelState.GraphModel;

        /// <summary>
        /// The GraphView linked to this MiniMapView.
        /// </summary>
        public GraphView ParentGraphView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniMapViewModel"/> class.
        /// </summary>
        public MiniMapViewModel(GraphView graphView)
        {
            ParentGraphView = graphView;
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
        }
    }
}
