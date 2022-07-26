using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class ShaderGraphCommandsRegistrar
    {
        public static void RegisterCommandHandlers(
            GraphView graphView,
            BlackboardView blackboardView,
            PreviewManager previewManager,
            ShaderGraphModel shaderGraphModel,
            Dispatcher dispatcher,
            GraphModelStateComponent graphModelStateComponent,
            GraphViewStateComponent graphViewStateComponent,
            SelectionStateComponent selectionStateComponent,
            UndoStateComponent undoStateComponent)
        {
            if (dispatcher is not CommandDispatcher commandDispatcher)
                return;

            GraphViewModel graphViewModel = graphView.GraphViewModel;

            // GTF Overrides

            // Unregister the base GraphView command handling for delete as we want to insert our own
            graphView.Dispatcher.UnregisterCommandHandler<DeleteElementsCommand>();
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PreviewManager, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteNodesAndEdges,
                undoStateComponent,
                graphModelStateComponent,
                selectionStateComponent,
                previewManager);

            blackboardView.Dispatcher.UnregisterCommandHandler<DeleteElementsCommand>();
            blackboardView.Dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, PreviewManager, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteBlackboardItems,
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

            // Shader Graph commands
            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, AddRedirectNodeCommand>(
                AddRedirectNodeCommand.DefaultHandler,
                undoStateComponent,
                graphViewModel.GraphModelState);

            PreviewCommandsRegistrar.RegisterCommandHandlers(undoStateComponent, graphModelStateComponent, previewManager, shaderGraphModel, commandDispatcher);

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeActiveTargetsCommand>(
                ChangeActiveTargetsCommand.DefaultCommandHandler,
                undoStateComponent,
                graphViewModel.GraphModelState
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeTargetSettingsCommand>(
                ChangeTargetSettingsCommand.DefaultCommandHandler,
                undoStateComponent,
                graphViewModel.GraphModelState
            );

            // Node UI commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, SetGraphTypeValueCommand>(
                SetGraphTypeValueCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewManager);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, SetGradientTypeValueCommand>(
                SetGradientTypeValueCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewManager);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeNodeFunctionCommand>(
                ChangeNodeFunctionCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            // Node upgrade commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, DismissNodeUpgradeCommand>(
                DismissNodeUpgradeCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, UpgradeNodeCommand>(
                UpgradeNodeCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            // Context entry commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, AddContextEntryCommand>(
                AddContextEntryCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, RemoveContextEntryCommand>(
                RemoveContextEntryCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, RenameContextEntryCommand>(
                RenameContextEntryCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeContextEntryTypeCommand>(
                ChangeContextEntryTypeCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            // Variable declaration commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SetShaderDeclarationCommand>(
                SetShaderDeclarationCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);
        }
    }
}
