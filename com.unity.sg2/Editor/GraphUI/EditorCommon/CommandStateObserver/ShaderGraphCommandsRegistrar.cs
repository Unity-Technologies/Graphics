using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class ShaderGraphCommandsRegistrar
    {
        public static void RegisterCommandHandlers(
            BaseGraphTool graphTool,
            GraphView graphView,
            BlackboardView blackboardView,
            PreviewManager previewManager,
            ShaderGraphModel shaderGraphModel,
            Dispatcher dispatcher)
        {
            if (dispatcher is not CommandDispatcher commandDispatcher)
                return;

            GraphViewModel graphViewModel = graphView.GraphViewModel;

            // GTF Overrides

            // Unregister the base GraphView command handling for delete as we want to insert our own
            graphView.Dispatcher.UnregisterCommandHandler<DeleteElementsCommand>();
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PreviewManager, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteNodesAndEdges,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                graphViewModel.SelectionState,
                previewManager);

            blackboardView.Dispatcher.UnregisterCommandHandler<DeleteElementsCommand>();
            blackboardView.Dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PreviewManager, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteBlackboardItems,
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

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, UpdateConstantValueCommand>(
                ShaderGraphCommandOverrides.HandleUpdateConstantValue,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                previewManager);

            // Shader Graph commands
            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, AddRedirectNodeCommand>(
                AddRedirectNodeCommand.DefaultHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);

            PreviewCommandsRegistrar.RegisterCommandHandlers(graphTool, previewManager, shaderGraphModel, commandDispatcher, graphViewModel);

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeActiveTargetsCommand>(
                ChangeActiveTargetsCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeTargetSettingsCommand>(
                ChangeTargetSettingsCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState
            );

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

            // Context entry commands
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

            // Variable declaration commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SetVariableSettingCommand>(
                SetVariableSettingCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState);
        }
    }
}
