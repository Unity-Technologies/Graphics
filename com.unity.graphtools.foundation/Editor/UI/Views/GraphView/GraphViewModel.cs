using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphViewModel : RootViewModel
    {
        /// <summary>
        /// The graph view state component.
        /// </summary>
        public GraphViewStateComponent GraphViewState { get; }

        /// <summary>
        /// The graph model state component.
        /// </summary>
        public GraphModelStateComponent GraphModelState { get; }

        /// <summary>
        /// The selection state component.
        /// </summary>
        public SelectionStateComponent SelectionState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphViewModel"/> class.
        /// </summary>
        public GraphViewModel(string graphViewName, IGraphAssetModel assetModel, IState state)
        {
            m_Guid = new SerializableGUID(graphViewName);

            var assetKey = PersistedState.MakeAssetKey(assetModel);

            GraphViewState = PersistedState.GetOrCreateAssetViewStateComponent<GraphViewStateComponent>(default, Guid, assetKey);

            GraphModelState = PersistedState.GetOrCreateAssetStateComponent<GraphModelStateComponent>(default, assetKey);

            SelectionState = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, Guid, assetKey);
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
            state?.AddStateComponent(GraphViewState);
            state?.AddStateComponent(GraphModelState);
            state?.AddStateComponent(SelectionState);
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
            state?.RemoveStateComponent(GraphViewState);
            state?.RemoveStateComponent(GraphModelState);
            state?.RemoveStateComponent(SelectionState);
        }
    }
}
