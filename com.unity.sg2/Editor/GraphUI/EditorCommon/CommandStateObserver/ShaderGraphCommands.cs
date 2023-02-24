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

            var undoStateComponent = ShaderGraphEditorWindow.GetStateComponentOfType<UndoStateComponent>(stateStore);
            var graphModelStateComponent = ShaderGraphEditorWindow.GetStateComponentOfType<GraphModelStateComponent>(stateStore);

            // Target setting commands
            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeActiveTargetsCommand>(
                ChangeActiveTargetsCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent
            );

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, ChangeTargetSettingsCommand>(
                ChangeTargetSettingsCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher
            );

            // Node commands
            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, SetGraphTypeValueCommand>(
                SetGraphTypeValueCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, SetGradientTypeValueCommand>(
                SetGradientTypeValueCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, SetSwizzleMaskCommand>(
                SetSwizzleMaskCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, SetCoordinateSpaceCommand>(
                SetCoordinateSpaceCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, SetConversionTypeCommand>(
                SetConversionTypeCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, AddRedirectNodeCommand>(
                AddRedirectNodeCommand.DefaultHandler,
                undoStateComponent,
                graphModelStateComponent);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, SetPortOptionCommand>(
                SetPortOptionCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent,
                previewUpdateDispatcher);

            // Node upgrade commands
            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, DismissNodeUpgradeCommand>(
                DismissNodeUpgradeCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, UpgradeNodeCommand>(
                UpgradeNodeCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            // Context entry commands
            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, AddContextEntryCommand>(
                AddContextEntryCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, RemoveContextEntryCommand>(
                RemoveContextEntryCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, RenameContextEntryCommand>(
                RenameContextEntryCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeContextEntryTypeCommand>(
                ChangeContextEntryTypeCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);

            // Variable declaration commands
            graphTool.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SetVariableSettingCommand>(
                SetVariableSettingCommand.DefaultCommandHandler,
                undoStateComponent,
                graphModelStateComponent);
        }
    }
}
