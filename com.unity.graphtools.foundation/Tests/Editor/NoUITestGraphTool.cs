using System;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class TestCommandDispatcher : CommandDispatcher
    {
        public Func<bool> CheckIntegrity { get; set; }

        protected override void PostDispatchCommand(ICommand command)
        {
            base.PostDispatchCommand(command);

            if (CheckIntegrity != null)
                Assert.IsTrue(CheckIntegrity());
        }
    }

    public class NoUITestGraphTool : BaseGraphTool
    {
        public new TestCommandDispatcher Dispatcher => base.Dispatcher as TestCommandDispatcher;

        public GraphViewStateComponent GraphViewState { get; set; }
        public SelectionStateComponent SelectionState { get; set; }
        public BlackboardViewStateComponent BlackboardViewState { get; set; }

        public NoUITestGraphTool()
        {
            Name = "GraphToolsFoundationTests";
        }

        /// <inheritdoc />
        protected override void InitDispatcher()
        {
            base.Dispatcher = new TestCommandDispatcher();
        }

        /// <inheritdoc />
        protected override void InitState()
        {
            WantsTransientPrefs = true;
            base.InitState();

            var assetKey = PersistedState.MakeAssetKey(ToolState.AssetModel);
            GraphViewState = PersistedState.GetOrCreateAssetViewStateComponent<GraphViewStateComponent>(default, default, assetKey);
            SelectionState = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, default, assetKey);
            BlackboardViewState = PersistedState.GetOrCreateAssetViewStateComponent<BlackboardViewStateComponent>(default, default, assetKey);

            State.AddStateComponent(GraphViewState);
            State.AddStateComponent(SelectionState);

            // Register the graph view commands on the tool's dispatcher.
            GraphViewCommandsRegistrar.RegisterCommands(Dispatcher, GraphViewState, SelectionState, this);
            BlackboardCommandsRegistrar.RegisterCommands(Dispatcher, GraphViewState, SelectionState, BlackboardViewState, this);

            IStateObserver observer = new GraphViewStateComponent.GraphAssetLoadedObserver(ToolState, GraphViewState);
            ObserverManager.RegisterObserver(observer);

            observer = new SelectionStateComponent.GraphAssetLoadedObserver(ToolState, SelectionState);
            ObserverManager.RegisterObserver(observer);
        }
    }
}
