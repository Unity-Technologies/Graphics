using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public struct GraphViewCommandsRegistrar
    {
        public static void RegisterCommands(GraphView graphView, BaseGraphTool graphTool)
        {
            new GraphViewCommandsRegistrar(graphView.Dispatcher, graphView.GraphViewModel.GraphViewState, graphView.GraphViewModel.GraphModelState, graphView.GraphViewModel.SelectionState, graphTool).RegisterCommandHandlers();
        }

        internal static void RegisterCommands(Dispatcher dispatcher, GraphViewStateComponent graphViewState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BaseGraphTool graphTool)
        {
            new GraphViewCommandsRegistrar(dispatcher, graphViewState, graphModelState, selectionState, graphTool).RegisterCommandHandlers();
        }

        Dispatcher m_Dispatcher;
        GraphViewStateComponent m_GraphViewState;
        GraphModelStateComponent m_GraphModelState;
        SelectionStateComponent m_SelectionState;
        BaseGraphTool m_GraphTool;

        GraphViewCommandsRegistrar(Dispatcher dispatcher, GraphViewStateComponent graphViewState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BaseGraphTool graphTool)
        {
            m_Dispatcher = dispatcher;
            m_GraphViewState = graphViewState;
            m_GraphModelState = graphModelState;
            m_SelectionState = selectionState;
            m_GraphTool = graphTool;
        }

        void RegisterCommandHandler<TCommand>(CommandHandler<UndoStateComponent, GraphModelStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState);
        }

        void RegisterCommandHandler<TCommand>(CommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState, m_SelectionState);
        }

        void RegisterCommandHandler<TParam3, TCommand>(CommandHandler<UndoStateComponent, GraphModelStateComponent, TParam3, TCommand> commandHandler, TParam3 handlerParam3)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState, handlerParam3);
        }

        void RegisterCommandHandler<TParam3, TCommand>(CommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, TParam3, TCommand> commandHandler, TParam3 handlerParam3)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState, m_SelectionState, handlerParam3);
        }

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

            RegisterCommandHandler<MoveEdgeCommand>(MoveEdgeCommand.DefaultCommandHandler);
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
            RegisterCommandHandler<SetInspectedGraphElementModelFieldCommand>(SetInspectedGraphElementModelFieldCommand.DefaultCommandHandler);

            RegisterCommandHandler<ItemizeNodeCommand>(ItemizeNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<LockConstantNodeCommand>(LockConstantNodeCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeVariableDeclarationCommand>(ChangeVariableDeclarationCommand.DefaultCommandHandler);

            m_Dispatcher.RegisterCommandHandler<GraphViewStateComponent, GraphModelStateComponent, SelectionStateComponent, ReframeGraphViewCommand>(
                ReframeGraphViewCommand.DefaultCommandHandler, m_GraphViewState, m_GraphModelState, m_SelectionState);

            RegisterCommandHandler<PasteSerializedDataCommand>(PasteSerializedDataCommand.DefaultCommandHandler);
            RegisterCommandHandler<DeleteElementsCommand>(DeleteElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ConvertConstantNodesAndVariableNodesCommand>(
                ConvertConstantNodesAndVariableNodesCommand.DefaultCommandHandler);

            RegisterCommandHandler<SelectElementsCommand>(SelectElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ClearSelectionCommand>(ClearSelectionCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateBlockFromSearcherCommand>(CreateBlockFromSearcherCommand.DefaultCommandHandler);
            RegisterCommandHandler<InsertBlocksInContextCommand>(InsertBlocksInContextCommand.DefaultCommandHandler);
            RegisterCommandHandler<CreateSubgraphCommand>(CreateSubgraphCommand.DefaultCommandHandler);
        }
    }
}
