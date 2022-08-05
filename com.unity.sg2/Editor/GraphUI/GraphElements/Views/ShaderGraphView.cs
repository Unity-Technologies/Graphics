using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphView : GraphView
    {
        GraphModelStateObserver m_GraphModelStateObserver;
        ShaderGraphLoadedObserver m_ShaderGraphLoadedObserver;
        PreviewManager m_PreviewManager;

        public ShaderGraphView(
            GraphViewEditorWindow window,
            BaseGraphTool graphTool,
            string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, displayMode)
        {
            if(window != null)
                ViewSelection = new ShaderGraphViewSelection(this, GraphViewModel.GraphModelState, GraphViewModel.SelectionState);
        }


        public ShaderGraphView(
            GraphViewEditorWindow window,
            BaseGraphTool graphTool,
            string graphViewName,
            PreviewManager previewManager,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, displayMode)
        {
            m_PreviewManager = previewManager;
            // This can be called by the searcher and if so, all of these dependencies will be null, need to guard against that
            if(window != null)
                ViewSelection = new ShaderGraphViewSelection(this, GraphViewModel.GraphModelState, GraphViewModel.SelectionState);

            GraphViewCommandOverridesRegistrar(this, graphTool.State, m_PreviewManager);

            GraphViewNodeCommandsRegistrar(this, graphTool.State, m_PreviewManager);
        }

        /// <summary>
        /// Place to register any commands that are overrides of base GTF commands
        /// </summary>
        static void GraphViewCommandOverridesRegistrar(ShaderGraphView graphView, IState stateStore, PreviewManager previewManager)
        {
            var dispatcher = graphView.Dispatcher;
            var undoStateComponent = ShaderGraphEditorWindow.GetStateComponentOfType<UndoStateComponent>(stateStore);
            var selectionStateComponent = graphView.GraphViewModel.SelectionState;
            var graphModelStateComponent = graphView.GraphViewModel.GraphModelState;

            // Unregister the base GraphView command handling for delete as we want to insert our own
            graphView.Dispatcher.UnregisterCommandHandler<DeleteElementsCommand>();
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PreviewManager, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteNodesAndEdges,
                undoStateComponent,
                graphModelStateComponent,
                selectionStateComponent,
                previewManager);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PreviewManager, BypassNodesCommand>(
                ShaderGraphCommandOverrides.HandleBypassNodes,
                undoStateComponent,
                graphModelStateComponent,
                selectionStateComponent,
                previewManager);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, UpdateConstantValueCommand>(
                ShaderGraphCommandOverrides.HandleUpdateConstantValue,
                undoStateComponent,
                graphModelStateComponent,
                previewManager);
        }

        protected override void RegisterObservers()
        {
            base.RegisterObservers();

            // Handling for when the searcher is opened
            if (GraphTool == null)
                return;

            m_GraphModelStateObserver = new GraphModelStateObserver(GraphViewModel.GraphModelState, m_PreviewManager);
            GraphTool.ObserverManager.RegisterObserver(m_GraphModelStateObserver);

            m_ShaderGraphLoadedObserver = new ShaderGraphLoadedObserver(GraphTool.ToolState, GraphViewModel.GraphModelState, this);
            GraphTool.ObserverManager.RegisterObserver(m_ShaderGraphLoadedObserver);
        }

        // Entry point for initializing any systems that depend on the graph model
        public void HandleGraphLoad(ShaderGraphModel shaderGraphModel)
        {
            var shaderGraphEditorWindow = Window as ShaderGraphEditorWindow;
            Assert.IsNotNull(shaderGraphEditorWindow);

            m_PreviewManager.Initialize(shaderGraphModel, shaderGraphEditorWindow.MainPreviewView, shaderGraphEditorWindow.WasWindowClosedInDirtyState);

            PreviewCommandsRegistrar.RegisterCommandHandlers(GraphTool, m_PreviewManager, shaderGraphModel, Dispatcher, GraphViewModel);
        }
    }
}
