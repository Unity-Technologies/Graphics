using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class ShaderGraphCommandsRegistrar
    {
        public static void RegisterCommandHandlers(BaseGraphTool graphTool, GraphView graphView, PreviewManager previewManager, ShaderGraphModel shaderGraphModel, Dispatcher dispatcher)
        {
            if (dispatcher is not CommandDispatcher commandDispatcher)
                return;

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
            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, AddRedirectNodeCommand>(
                AddRedirectNodeCommand.DefaultHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, ChangePreviewExpandedCommand>(
                ChangePreviewExpandedCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                previewManager
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, ChangePreviewModeCommand>(
                ChangePreviewModeCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                previewManager
            );

            // Overrides for default GTF commands

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, Preferences, CreateEdgeCommand>(
                ShaderGraphCommandOverrides.HandleCreateEdge,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                previewManager,
                graphTool.Preferences
            );

            // Unregister the base GraphView command handling for this as we want to insert our own
            graphView.Dispatcher.UnregisterCommandHandler<DeleteElementsCommand>();
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, SelectionStateComponent, PreviewManager, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteElements,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                graphView.SelectionState,
                previewManager
            );

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, BypassNodesCommand>(
                ShaderGraphCommandOverrides.HandleBypassNodes,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                previewManager
            );

            dispatcher.RegisterCommandHandler<GraphViewStateComponent, PreviewManager, RenameElementCommand>(
                ShaderGraphCommandOverrides.HandleGraphElementRenamed,
                graphView.GraphViewState,
                previewManager
            );

            dispatcher.RegisterCommandHandler<GraphViewStateComponent, PreviewManager, UpdateConstantValueCommand>(
                ShaderGraphCommandOverrides.HandleUpdateConstantValue,
                graphView.GraphViewState,
                previewManager
            );

            dispatcher.RegisterCommandHandler<GraphViewStateComponent, PreviewManager, IGraphModel, UpdatePortConstantCommand>(
                ShaderGraphCommandOverrides.HandleUpdatePortValue,
                graphView.GraphViewState,
                previewManager,
                shaderGraphModel
            );

            // Node UI commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, SetGraphTypeValueCommand>(
                SetGraphTypeValueCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                previewManager
            );
        }
    }
}
