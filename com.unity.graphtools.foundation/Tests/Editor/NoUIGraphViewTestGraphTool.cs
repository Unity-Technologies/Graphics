using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class NoUIGraphViewTestGraphTool : NoUITestGraphTool
    {
        public GraphViewStateComponent GraphViewState { get; set; }
        public GraphModelStateComponent GraphModelState { get; set; }
        public SelectionStateComponent GraphViewSelectionState { get; set; }

        public NoUIGraphViewTestGraphTool()
        {
            Name = "GraphToolsFoundationTests";
        }

        /// <inheritdoc />
        protected override void InitState()
        {
            WantsTransientPrefs = true;
            base.InitState();

            var assetKey = PersistedState.MakeAssetKey(ToolState.AssetModel);
            GraphViewState = PersistedState.GetOrCreateAssetViewStateComponent<GraphViewStateComponent>(default, Hash128.Compute(0), assetKey);
            State.AddStateComponent(GraphViewState);

            GraphModelState = PersistedState.GetOrCreateAssetStateComponent<GraphModelStateComponent>(default, assetKey);
            State.AddStateComponent(GraphModelState);

            GraphViewSelectionState = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, Hash128.Compute(0), assetKey);
            State.AddStateComponent(GraphViewSelectionState);

            GraphViewCommandsRegistrar.RegisterCommands(Dispatcher, GraphViewState, GraphModelState, GraphViewSelectionState, this);

            IStateObserver observer = new GraphViewStateComponent.GraphAssetLoadedObserver(ToolState, GraphViewState);
            ObserverManager.RegisterObserver(observer);

            observer = new GraphModelStateComponent.GraphAssetLoadedObserver(ToolState, GraphModelState);
            ObserverManager.RegisterObserver(observer);

            observer = new SelectionStateComponent.GraphAssetLoadedObserver(ToolState, GraphViewSelectionState);
            ObserverManager.RegisterObserver(observer);
        }
    }
}
