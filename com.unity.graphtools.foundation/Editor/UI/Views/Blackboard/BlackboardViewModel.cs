using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlackboardViewModel : RootViewModel
    {
        /// <summary>
        /// The blackboard state component.
        /// </summary>
        public BlackboardViewStateComponent ViewState { get; }

        /// <summary>
        /// The <see cref="SelectionStateComponent"/>. Holds the blackboard selection.
        /// </summary>
        public SelectionStateComponent SelectionState { get; }

        /// <summary>
        /// The <see cref="GraphModelStateComponent"/> of the <see cref="GraphView"/> linked to this blackboard.
        /// </summary>
        public GraphModelStateComponent GraphModelState => ParentGraphView?.GraphViewModel.GraphModelState;

        /// <summary>
        /// The highlighter state.
        /// </summary>
        public DeclarationHighlighterStateComponent HighlighterState { get; }

        /// <summary>
        /// The parent graph view.
        /// </summary>
        public GraphView ParentGraphView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardViewModel"/> class.
        /// </summary>
        public BlackboardViewModel(GraphView graphView, DeclarationHighlighterStateComponent highlighterState)
        {
            m_Guid = new SerializableGUID(GetType().FullName + graphView.GraphViewModel.Guid);

            ParentGraphView = graphView;
            HighlighterState = highlighterState;

            var key = PersistedState.MakeGraphKey(GraphModelState?.GraphModel);
            ViewState = PersistedState.GetOrCreatePersistedStateComponent<BlackboardViewStateComponent>(default, Guid, key);
            SelectionState = PersistedState.GetOrCreatePersistedStateComponent<SelectionStateComponent>(default, Guid, key);
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
            state?.AddStateComponent(ViewState);
            state?.AddStateComponent(SelectionState);
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
            state?.RemoveStateComponent(ViewState);
            state?.RemoveStateComponent(SelectionState);
        }
    }
}
