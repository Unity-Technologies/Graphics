using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public static class ShaderGraphState
    {
        public static Hash128 m_previewStateWindowGUID = Hash128.Compute(GUID.Generate().ToString());

        public static void RegisterCommandHandlers(BaseGraphTool graphTool, GraphView graphView, Dispatcher dispatcher)
        {
            // TODO (Brett) This assumes that the window for preview exists.
            // TODO We should get preview state from somewhere else.
            GraphPreviewStateComponent graphPreviewState = PersistedState.GetOrCreateViewStateComponent<GraphPreviewStateComponent>("Graph Preview State", m_previewStateWindowGUID);

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

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, GraphPreviewStateComponent, ChangePreviewExpandedCommand>(
                ChangePreviewExpandedCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                graphPreviewState
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, GraphPreviewStateComponent, ChangePreviewModeCommand>(
                ChangePreviewModeCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                graphPreviewState
            );

            // Overrides for default GTF commands

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, Preferences, CreateEdgeCommand>(
                ShaderGraphCommandOverrides.HandleCreateEdge,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                graphTool.Preferences
            );

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, SelectionStateComponent, GraphPreviewStateComponent, DeleteElementsCommand>(
                ShaderGraphCommandOverrides.HandleDeleteElements,
                graphTool.UndoStateComponent,
                graphView.GraphViewState,
                graphView.SelectionState,
                graphPreviewState
            );

            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, BypassNodesCommand>(
                ShaderGraphCommandOverrides.HandleBypassNodes,
                graphTool.UndoStateComponent,
                graphView.GraphViewState
            );

            dispatcher.RegisterCommandHandler<GraphViewStateComponent, GraphPreviewStateComponent, RenameElementCommand>(
                ShaderGraphCommandOverrides.HandleGraphElementRenamed,
                graphView.GraphViewState,
                graphPreviewState
            );

            dispatcher.RegisterCommandHandler<GraphPreviewStateComponent, ChangeNodeStateCommand>(
                ShaderGraphCommandOverrides.HandleNodeStateChanged,
                graphPreviewState
            );

            dispatcher.RegisterCommandHandler<GraphViewStateComponent, GraphPreviewStateComponent, UpdateConstantValueCommand>(
                ShaderGraphCommandOverrides.HandleUpdateConstantValue,
                graphView.GraphViewState,
                graphPreviewState
            );
        }
    }
}
