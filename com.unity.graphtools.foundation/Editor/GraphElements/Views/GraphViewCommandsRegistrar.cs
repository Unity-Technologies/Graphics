using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public struct GraphViewCommandsRegistrar
    {
        public static void RegisterCommands(GraphView graphView, BaseGraphTool graphTool)
        {
            new GraphViewCommandsRegistrar(graphView.Dispatcher, graphView.GraphViewState, graphView.SelectionState, graphTool).RegisterCommandHandlers();
        }

        internal static void RegisterCommands(Dispatcher dispatcher, GraphViewStateComponent graphViewState, SelectionStateComponent selectionState, BaseGraphTool graphTool)
        {
            new GraphViewCommandsRegistrar(dispatcher, graphViewState, selectionState, graphTool).RegisterCommandHandlers();
        }

        Dispatcher m_Dispatcher;
        GraphViewStateComponent m_GraphViewState;
        SelectionStateComponent m_SelectionState;
        BaseGraphTool m_GraphTool;

        GraphViewCommandsRegistrar(Dispatcher dispatcher, GraphViewStateComponent graphViewState, SelectionStateComponent selectionState, BaseGraphTool graphTool)
        {
            m_Dispatcher = dispatcher;
            m_GraphViewState = graphViewState;
            m_SelectionState = selectionState;
            m_GraphTool = graphTool;
        }

        void RegisterCommandHandler<TCommand>(CommandHandler<UndoStateComponent, GraphViewStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphViewState);
        }

        void RegisterCommandHandler<TCommand>(CommandHandler<UndoStateComponent, GraphViewStateComponent, SelectionStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphViewState, m_SelectionState);
        }

        void RegisterCommandHandler<TCommand>(CommandHandler<GraphViewStateComponent, SelectionStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphViewState, m_SelectionState);
        }

        void RegisterCommandHandler<TParam3, TCommand>(CommandHandler<UndoStateComponent, GraphViewStateComponent, TParam3, TCommand> commandHandler, TParam3 handlerParam3)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphViewState, handlerParam3);
        }

        //CommandHandler<UndoStateComponent, GraphViewStateComponent, TParam3, TCommand>

        void RegisterCommandHandlers()
        {
            RegisterCommandHandler<Preferences, CreateEdgeCommand>(
                CreateEdgeCommand.DefaultCommandHandler, m_GraphTool.Preferences);
            RegisterCommandHandler<Preferences, CreateNodeCommand>(CreateNodeCommand.DefaultCommandHandler,
                m_GraphTool.Preferences);
#pragma warning disable 618
            RegisterCommandHandler<Preferences, CreateNodeFromPortCommand>(
                CreateNodeFromPortCommand.DefaultCommandHandler, m_GraphTool.Preferences);
            RegisterCommandHandler<CreateNodeOnEdgeCommand>(CreateNodeOnEdgeCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateVariableNodesCommand>(CreateVariableNodesCommand.DefaultCommandHandler);
#pragma warning restore 618

            RegisterCommandHandler<ReorderEdgeCommand>(ReorderEdgeCommand.DefaultCommandHandler);
            RegisterCommandHandler<SplitEdgeAndInsertExistingNodeCommand>(SplitEdgeAndInsertExistingNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<ConvertEdgesToPortalsCommand>(ConvertEdgesToPortalsCommand.DefaultCommandHandler);
            RegisterCommandHandler<DisconnectNodeCommand>(DisconnectNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeNodeStateCommand>(ChangeNodeStateCommand.DefaultCommandHandler);
            RegisterCommandHandler<CollapseNodeCommand>(CollapseNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateConstantValueCommand>(UpdateConstantValueCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateOppositePortalCommand>(CreateOppositePortalCommand.DefaultCommandHandler);
            RegisterCommandHandler<DeleteEdgeCommand>(DeleteEdgeCommand.DefaultCommandHandler);

            m_Dispatcher.RegisterCommandHandler<BuildAllEditorCommand>(BuildAllEditorCommand.DefaultCommandHandler);

            RegisterCommandHandler<AlignNodesCommand>(AlignNodesCommand.DefaultCommandHandler);
            RegisterCommandHandler<RenameElementCommand>(RenameElementCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdatePortConstantCommand>(UpdatePortConstantCommand.DefaultCommandHandler);
            RegisterCommandHandler<BypassNodesCommand>(BypassNodesCommand.DefaultCommandHandler);
            RegisterCommandHandler<MoveElementsCommand>(MoveElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<AutoPlaceElementsCommand>(AutoPlaceElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeElementColorCommand>(ChangeElementColorCommand.DefaultCommandHandler);
            RegisterCommandHandler<ResetElementColorCommand>(ResetElementColorCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeElementLayoutCommand>(ChangeElementLayoutCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreatePlacematCommand>(CreatePlacematCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangePlacematOrderCommand>(ChangePlacematOrderCommand.DefaultCommandHandler);
            RegisterCommandHandler<CollapsePlacematCommand>(CollapsePlacematCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateStickyNoteCommand>(CreateStickyNoteCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateStickyNoteCommand>(UpdateStickyNoteCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateStickyNoteThemeCommand>(UpdateStickyNoteThemeCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateStickyNoteTextSizeCommand>(UpdateStickyNoteTextSizeCommand.DefaultCommandHandler);

            RegisterCommandHandler<ItemizeNodeCommand>(ItemizeNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<LockConstantNodeCommand>(LockConstantNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<InitializeVariableCommand>(InitializeVariableCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeVariableTypeCommand>(ChangeVariableTypeCommand.DefaultCommandHandler);
            RegisterCommandHandler<ExposeVariableCommand>(ExposeVariableCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateTooltipCommand>(UpdateTooltipCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeVariableDeclarationCommand>(ChangeVariableDeclarationCommand.DefaultCommandHandler);

            RegisterCommandHandler<PasteSerializedDataCommand>(PasteSerializedDataCommand.DefaultCommandHandler);
            RegisterCommandHandler<ReframeGraphViewCommand>(ReframeGraphViewCommand.DefaultCommandHandler);
            RegisterCommandHandler<DeleteElementsCommand>(DeleteElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ConvertConstantNodesAndVariableNodesCommand>(
                ConvertConstantNodesAndVariableNodesCommand.DefaultCommandHandler);
            RegisterCommandHandler<SelectElementsCommand>(SelectElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ClearSelectionCommand>(ClearSelectionCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateBlockFromSearcherCommand>(CreateBlockFromSearcherCommand.DefaultCommandHandler);
            RegisterCommandHandler<InsertBlocksInContextCommand>(InsertBlocksInContextCommand.DefaultCommandHandler);
        }
    }
}
