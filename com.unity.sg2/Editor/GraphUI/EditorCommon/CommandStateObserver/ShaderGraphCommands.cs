using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using Unity.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Class for adding commands that are unique to Shader Graph user interactions
    /// </summary>
    static class ShaderGraphCommands
    {
        public static void RegisterCommandHandlers(BaseGraphTool graphTool, PreviewUpdateDispatcher previewUpdateDispatcher)
        {
            var stateStore = graphTool.State;
            var dispatcher = graphTool.Dispatcher;

            var undoStateComponent = ShaderGraphEditorWindow.GetStateComponentOfType<UndoStateComponent>(stateStore);
            var graphModelStateComponent = ShaderGraphEditorWindow.GetStateComponentOfType<GraphModelStateComponent>(stateStore);

            // Target setting commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeActiveTargetsCommand>(
                ChangeActiveTargetsCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent
            );

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, ChangeTargetSettingsCommand>(
                ChangeTargetSettingsCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher
            );

            // Node commands
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, SetGraphTypeValueCommand>(
                SetGraphTypeValueCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, SetGradientTypeValueCommand>(
                SetGradientTypeValueCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher);

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, AddRedirectNodeCommand>(
                AddRedirectNodeCommand.DefaultHandler,
                undoStateComponent,
                graphModelStateComponent);

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
            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SetVariableSettingCommand>(
                SetVariableSettingCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);
        }
    }
}
