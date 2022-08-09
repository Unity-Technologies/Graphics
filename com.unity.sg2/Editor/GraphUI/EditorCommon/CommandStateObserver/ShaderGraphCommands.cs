using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Class for adding commands that are unique to Shader Graph user interactions
    /// </summary>
    public static class ShaderGraphCommands
    {
        public static void RegisterCommandHandlers(BaseGraphTool graphTool, PreviewManager previewManager)
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

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewManager, ChangeTargetSettingsCommand>(
                ChangeTargetSettingsCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewManager
            );

            // Node commands
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
