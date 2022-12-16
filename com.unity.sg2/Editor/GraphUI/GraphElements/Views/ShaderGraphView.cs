using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.CommandStateObserver;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphView : GraphView
    {
        GraphModelStateObserver m_GraphModelStateObserver;
        ShaderGraphLoadedObserver m_ShaderGraphLoadedObserver;

        PreviewStateObserver m_PreviewStateObserver;
        PreviewUpdateDispatcher m_PreviewUpdateDispatcher;

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
            PreviewUpdateDispatcher previewUpdateDispatcher,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, displayMode)
        {
            // This can be called by the searcher and if so, all of these dependencies will be null, need to guard against that
            if (window != null)
            {
                m_PreviewUpdateDispatcher = previewUpdateDispatcher;

                ViewSelection = new ShaderGraphViewSelection(this, GraphViewModel.GraphModelState, GraphViewModel.SelectionState);

                RegisterGraphViewOverrideCommandHandlers(this, graphTool.State, previewUpdateDispatcher);
            }
        }

        /// <summary>
        /// Place to register any commands that are overrides of base GTF commands for the graph view
        /// </summary>
        static void RegisterGraphViewOverrideCommandHandlers(
            ShaderGraphView graphView,
            IState stateStore,
            PreviewUpdateDispatcher previewUpdateDispatcher)
        {
            var dispatcher = graphView.Dispatcher;
            var undoStateComponent = ShaderGraphEditorWindow.GetStateComponentOfType<UndoStateComponent>(stateStore);
            var selectionStateComponent = graphView.GraphViewModel.SelectionState;
            var graphModelStateComponent = graphView.GraphViewModel.GraphModelState;

            // Unregister the base GraphView command handling for delete as we want to insert our own
            graphView.Dispatcher.UnregisterCommandHandler<DeleteElementsCommand>();
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteNodesAndEdges,
                undoStateComponent,
                graphModelStateComponent,
                selectionStateComponent);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PreviewUpdateDispatcher, BypassNodesCommand>(
                ShaderGraphCommandOverrides.HandleBypassNodes,
                undoStateComponent,
                graphModelStateComponent,
                selectionStateComponent,
                previewUpdateDispatcher);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PasteSerializedDataCommand>(
                ShaderGraphCommandOverrides.HandlePasteSerializedData,
                undoStateComponent,
                graphModelStateComponent,
                selectionStateComponent);
        }

        protected override void RegisterObservers()
        {
            base.RegisterObservers();

            // Handling for when the searcher is opened
            if (GraphTool == null)
                return;

            var previewStateComponent = ShaderGraphEditorWindow.GetStateComponentOfType<PreviewStateComponent>(GraphTool.State);
            Assert.IsNotNull(previewStateComponent);

            m_ShaderGraphLoadedObserver = new ShaderGraphLoadedObserver(GraphTool.ToolState, GraphViewModel.GraphModelState, previewStateComponent, Window as ShaderGraphEditorWindow);
            GraphTool.ObserverManager.RegisterObserver(m_ShaderGraphLoadedObserver);

            m_GraphModelStateObserver = new GraphModelStateObserver(GraphViewModel.GraphModelState, previewStateComponent, m_PreviewUpdateDispatcher);
            GraphTool.ObserverManager.RegisterObserver(m_GraphModelStateObserver);

            m_PreviewStateObserver = new PreviewStateObserver(previewStateComponent, this);
            GraphTool.ObserverManager.RegisterObserver(m_PreviewStateObserver);
        }

        /// <inheritdoc />
        protected override void UnregisterObservers()
        {
            base.UnregisterObservers();

            if (GraphTool?.ObserverManager == null)
                return;

            if (m_ShaderGraphLoadedObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_ShaderGraphLoadedObserver);
                m_ShaderGraphLoadedObserver = null;
            }

            if (m_GraphModelStateObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_GraphModelStateObserver);
                m_GraphModelStateObserver = null;
            }

            if (m_PreviewStateObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_PreviewStateObserver);
                m_PreviewStateObserver = null;
            }
        }

        public void HandlePreviewUpdates(IEnumerable<SerializableGUID> changedModels)
        {
            if (!changedModels.Any())
                return;

            UpdateChangedModels(changedModels.ToHashSet(), false, new List<GraphElement>());
        }

        /// <inheritdoc />
        public override void BuildOptionMenu(GenericMenu menu)
        {
            if (Unsupported.IsDeveloperMode())
            {
                menu.AddItem(new GUIContent("Check Blackboard Sanity"), false, () =>
                {
                    (GraphModel as SGGraphModel).CheckBlackboardSanity();
                });

                menu.AddSeparator("");
            }

            base.BuildOptionMenu(menu);
        }
    }
}
