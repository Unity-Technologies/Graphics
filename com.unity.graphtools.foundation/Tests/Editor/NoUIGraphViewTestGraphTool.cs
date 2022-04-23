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

            var assetKey = PersistedState.MakeGraphKey(ToolState.GraphModel);
            GraphViewState = PersistedState.GetOrCreatePersistedStateComponent<GraphViewStateComponent>(default, Hash128.Compute(0), assetKey);
            State.AddStateComponent(GraphViewState);

            GraphModelState = new GraphModelStateComponent();
            State.AddStateComponent(GraphModelState);

            GraphViewSelectionState = PersistedState.GetOrCreatePersistedStateComponent<SelectionStateComponent>(default, Hash128.Compute(0), assetKey);
            State.AddStateComponent(GraphViewSelectionState);

            GraphViewCommandsRegistrar.RegisterCommands(Dispatcher, GraphViewState, GraphModelState, GraphViewSelectionState, this);

            IStateObserver observer = new GraphViewStateComponent.GraphLoadedObserver(ToolState, GraphViewState);
            ObserverManager.RegisterObserver(observer);

            observer = new GraphModelStateComponent.GraphAssetLoadedObserver(ToolState, GraphModelState);
            ObserverManager.RegisterObserver(observer);

            observer = new SelectionStateComponent.GraphLoadedObserver(ToolState, GraphViewSelectionState);
            ObserverManager.RegisterObserver(observer);
        }
    }
}
