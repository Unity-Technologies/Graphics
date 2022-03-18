using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Helper to register command handler on the <see cref="BlackboardView"/>.
    /// </summary>
    public struct BlackboardCommandsRegistrar
    {
        /// <summary>
        /// Registers command handler on the <paramref name="view"/>.
        /// </summary>
        /// <param name="view">The view to register command handlers on.</param>
        /// <param name="graphTool">The graph tool.</param>
        public static void RegisterCommands(BlackboardView view, BaseGraphTool graphTool)
        {
            new BlackboardCommandsRegistrar(view.Dispatcher, view.BlackboardViewModel.GraphModelState, view.BlackboardViewModel.SelectionState,
                view.BlackboardViewModel.ViewState, graphTool).RegisterCommandHandlers();
        }

        internal static void RegisterCommands(Dispatcher dispatcher, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewState, BaseGraphTool graphTool)
        {
            new BlackboardCommandsRegistrar(dispatcher, graphModelState, selectionState, blackboardViewState, graphTool).RegisterCommandHandlers();
        }

        Dispatcher m_Dispatcher;
        GraphModelStateComponent m_GraphModelState;
        SelectionStateComponent m_SelectionState;
        BlackboardViewStateComponent m_BlackboardViewState;
        BaseGraphTool m_GraphTool;

        BlackboardCommandsRegistrar(Dispatcher dispatcher, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewStateComponent, BaseGraphTool graphTool)
        {
            m_Dispatcher = dispatcher;
            m_GraphModelState = graphModelState;
            m_SelectionState = selectionState;
            m_BlackboardViewState = blackboardViewStateComponent;
            m_GraphTool = graphTool;
        }

        void RegisterCommandHandler<TCommand>(CommandHandler<UndoStateComponent, GraphModelStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState);
        }

        void RegisterCommandHandler<TCommand>(
            CommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState, m_SelectionState);
        }

        void RegisterCommandHandler<TParam3, TCommand>(
            CommandHandler<UndoStateComponent, GraphModelStateComponent, TParam3, TCommand> commandHandler, TParam3 handlerParam3)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphModelState, handlerParam3);
        }

        void RegisterCommandHandlers()
        {
            m_Dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent,
                BlackboardViewStateComponent, SelectionStateComponent, CreateGraphVariableDeclarationCommand>(
                CreateGraphVariableDeclarationCommand.DefaultCommandHandler,
                m_GraphTool.UndoStateComponent, m_GraphModelState, m_BlackboardViewState, m_SelectionState);

            RegisterCommandHandler<BlackboardViewStateComponent, ReorderGroupItemsCommand>(
                ReorderGroupItemsCommand.DefaultCommandHandler, m_BlackboardViewState);

            m_Dispatcher.RegisterCommandHandler<UndoStateComponent, BlackboardViewStateComponent,
                GraphModelStateComponent, SelectionStateComponent, BlackboardGroupCreateCommand>(
                BlackboardGroupCreateCommand.DefaultCommandHandler,
                m_GraphTool.UndoStateComponent, m_BlackboardViewState, m_GraphModelState, m_SelectionState);

            m_Dispatcher.RegisterCommandHandler<BlackboardViewStateComponent, ExpandVariableDeclarationCommand>(
                ExpandVariableDeclarationCommand.DefaultCommandHandler, m_BlackboardViewState);

            m_Dispatcher.RegisterCommandHandler<BlackboardViewStateComponent, ExpandVariableGroupCommand>(
                ExpandVariableGroupCommand.DefaultCommandHandler, m_BlackboardViewState);

            RegisterCommandHandler<InitializeVariableCommand>(InitializeVariableCommand.DefaultCommandHandler);
            RegisterCommandHandler<ChangeVariableTypeCommand>(ChangeVariableTypeCommand.DefaultCommandHandler);
            RegisterCommandHandler<ExposeVariableCommand>(ExposeVariableCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateTooltipCommand>(UpdateTooltipCommand.DefaultCommandHandler);

            m_Dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SelectionStateComponent,
                BlackboardViewStateComponent, PasteSerializedDataCommand>(
                BlackboardViewCommandHandlers.PasteSerializedDataCommandHandler,
                m_GraphTool.UndoStateComponent, m_GraphModelState, m_SelectionState, m_BlackboardViewState);

            RegisterCommandHandler<DeleteElementsCommand>(DeleteElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<SelectElementsCommand>(SelectElementsCommand.DefaultCommandHandler);
            RegisterCommandHandler<ClearSelectionCommand>(ClearSelectionCommand.DefaultCommandHandler);

            RegisterCommandHandler<RenameElementCommand>(RenameElementCommand.DefaultCommandHandler);
            RegisterCommandHandler<UpdateConstantValueCommand>(UpdateConstantValueCommand.DefaultCommandHandler);
        }
    }
}
