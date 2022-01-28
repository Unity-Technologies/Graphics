using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public static class ShaderGraphState
    {
        public static Hash128 m_previewStateWindowGUID = Hash128.Compute(GUID.Generate().ToString());

        public static void RegisterCommandHandlers(BaseGraphTool graphTool, GraphView graphView, Dispatcher dispatcher)
        {
            PreviewManager previewManagerInstance = null;

            if (dispatcher is not CommandDispatcher commandDispatcher)
                return;

            // TODO: Instead of having this be a monolithic list of commands all gathered here, can we can break them up into being registered by individual controllers?
            // Demo commands (TODO: Remove)
            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, AddPortCommand>(
                AddPortCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, RemovePortCommand>(
                RemovePortCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState
            );

            // Shader Graph commands
            commandDispatcher.RegisterCommandHandler<GraphViewStateComponent, AddRedirectNodeCommand>(
                AddRedirectNodeCommand.DefaultHandler,
                graphView.GraphViewState
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, ChangePreviewExpandedCommand>(
                ChangePreviewExpandedCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                previewManagerInstance
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, ChangePreviewModeCommand>(
                ChangePreviewModeCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                previewManagerInstance
            );

            //commandDispatcher.RegisterCommandHandler<GraphViewStateComponent, GraphWindowTickCommand>(
            //    GraphWindowTickCommand.DefaultCommandHandler,
            //    graphView.GraphViewState
            //);

            // Overrides for default GTF commands

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, Preferences, CreateEdgeCommand>(
                ShaderGraphCommandOverrides.HandleCreateEdge,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                previewManagerInstance,
                graphTool.Preferences
            );

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, SelectionStateComponent, PreviewManager, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteElements,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                graphView.SelectionState,
                previewManagerInstance
            );

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, BypassNodesCommand>(
                ShaderGraphCommandOverrides.HandleBypassNodes,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                previewManagerInstance
            );

            dispatcher.RegisterCommandHandler<GraphViewStateComponent, PreviewManager, RenameElementCommand>(
                ShaderGraphCommandOverrides.HandleGraphElementRenamed,
                graphView.GraphViewState,
                previewManagerInstance
            );

            dispatcher.RegisterCommandHandler<GraphViewStateComponent, PreviewManager, UpdateConstantValueCommand>(
                ShaderGraphCommandOverrides.HandleUpdateConstantValue,
                graphView.GraphViewState,
                previewManagerInstance
            );
        }
    }
}
