using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlackboardViewModel : RootViewModel, IDisposable
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
        public BlackboardViewModel(GraphView graphView, DeclarationHighlighterStateComponent highlighterState, IState state)
        {
            m_Guid = new SerializableGUID("Blackboard");

            ParentGraphView = graphView;
            HighlighterState = highlighterState;

            var assetKey = PersistedState.MakeAssetKey(GraphModelState?.GraphModel?.AssetModel);

            ViewState = PersistedState.GetOrCreateAssetViewStateComponent<BlackboardViewStateComponent>(default, Guid, assetKey);

            SelectionState = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, Guid, assetKey);
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(); false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ViewState?.Dispose();
                SelectionState?.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
