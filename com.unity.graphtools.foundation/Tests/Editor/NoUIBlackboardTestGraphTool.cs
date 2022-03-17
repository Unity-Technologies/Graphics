using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class NoUIBlackboardTestGraphTool : NoUITestGraphTool
    {
        public GraphModelStateComponent GraphModelState { get; set; }
        public BlackboardViewStateComponent BlackboardViewState { get; set; }
        public SelectionStateComponent BlackboardSelectionState { get; set; }

        public SelectionStateComponent GraphViewSelectionState { get; set; }

        public NoUIBlackboardTestGraphTool()
        {
            Name = "GraphToolsFoundationTests";
        }

        /// <inheritdoc />
        protected override void InitState()
        {
            WantsTransientPrefs = true;
            base.InitState();

            var assetKey = PersistedState.MakeAssetKey(ToolState.AssetModel);

            GraphModelState = PersistedState.GetOrCreateAssetStateComponent<GraphModelStateComponent>(default, assetKey);
            State.AddStateComponent(GraphModelState);

            BlackboardViewState = PersistedState.GetOrCreateAssetViewStateComponent<BlackboardViewStateComponent>(default, Hash128.Compute(1), assetKey);
            State.AddStateComponent(BlackboardViewState);

            BlackboardSelectionState = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, Hash128.Compute(1), assetKey);
            State.AddStateComponent(BlackboardSelectionState);

            GraphViewSelectionState = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, Hash128.Compute(0), assetKey);
            State.AddStateComponent(GraphViewSelectionState);

            // Register the graph view commands on the tool's dispatcher.
            BlackboardCommandsRegistrar.RegisterCommands(Dispatcher, GraphModelState, BlackboardSelectionState, BlackboardViewState, this);

            IStateObserver observer = new GraphModelStateComponent.GraphAssetLoadedObserver(ToolState, GraphModelState);
            ObserverManager.RegisterObserver(observer);

            observer = new BlackboardGraphAssetLoadedObserver(ToolState, BlackboardViewState, BlackboardSelectionState);
            ObserverManager.RegisterObserver(observer);

            observer = new SelectionStateComponent.GraphAssetLoadedObserver(ToolState, GraphViewSelectionState);
            ObserverManager.RegisterObserver(observer);
        }
    }
}
