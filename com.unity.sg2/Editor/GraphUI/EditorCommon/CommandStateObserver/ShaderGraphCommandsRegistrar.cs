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

            GraphViewModel graphViewModel = graphView.GraphViewModel;

            // Demo commands (TODO: Remove)
            //commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, AddPortCommand>(
            //    AddPortCommand.DefaultCommandHandler,
            //    graphTool.UndoStateComponent,
            //    graphView.GraphViewState
            //);

            //commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, RemovePortCommand>(
            //    RemovePortCommand.DefaultCommandHandler,
            //    graphTool.UndoStateComponent,
            //    graphView.GraphModelState
            //);

            // Shader Graph commands
            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, AddRedirectNodeCommand>(
                AddRedirectNodeCommand.DefaultHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, ChangePreviewExpandedCommand>(
                ChangePreviewExpandedCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                previewManager
            );

            commandDispatcher.RegisterCommandHandler<ShaderGraphModel, PreviewManager, ChangePreviewMeshCommand>(
                ChangePreviewMeshCommand.DefaultCommandHandler,
                shaderGraphModel,
                previewManager
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, ChangePreviewModeCommand>(
                ChangePreviewModeCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                previewManager
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, ChangeActiveTargetsCommand>(
                ChangeActiveTargetsCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, ChangeTargetSettingsCommand>(
                ChangeTargetSettingsCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent
            );

            //commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, PreviewManager, ChangePreviewModeCommand>(
            //    ChangePreviewModeCommand.DefaultCommandHandler,
            //    graphTool.UndoStateComponent,
            //    graphView.GraphViewState,
            //    previewManager
            //);

            // Unregister the base GraphView command handling for this as we want to insert our own
            graphView.Dispatcher.UnregisterCommandHandler<DeleteElementsCommand>();
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PreviewManager, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteElements,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                graphViewModel.SelectionState,
                previewManager);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PreviewManager, BypassNodesCommand>(
                ShaderGraphCommandOverrides.HandleBypassNodes,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                graphViewModel.SelectionState,
                previewManager);

            dispatcher.RegisterCommandHandler<GraphViewStateComponent, PreviewManager, RenameElementCommand>(
                ShaderGraphCommandOverrides.HandleGraphElementRenamed,
                graphViewModel.GraphViewState,
                previewManager);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, UpdateConstantValueCommand>(
                ShaderGraphCommandOverrides.HandleUpdateConstantValue,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                previewManager);

            // Node UI commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, SetGraphTypeValueCommand>(
                SetGraphTypeValueCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                previewManager);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, SetGradientTypeValueCommand>(
                SetGradientTypeValueCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                previewManager);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeNodeFunctionCommand>(
                ChangeNodeFunctionCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);

            // Node upgrade commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, DismissNodeUpgradeCommand>(
                DismissNodeUpgradeCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, UpgradeNodeCommand>(
                UpgradeNodeCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, AddContextEntryCommand>(
                AddContextEntryCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, RemoveContextEntryCommand>(
                RemoveContextEntryCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, RenameContextEntryCommand>(
                RenameContextEntryCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeContextEntryTypeCommand>(
                ChangeContextEntryTypeCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);
        }
    }
}
