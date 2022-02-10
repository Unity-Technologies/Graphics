using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public struct BlackboardCommandsRegistrar
    {
        public static void RegisterCommands(GraphView graphView, BaseGraphTool graphTool)
        {
            new BlackboardCommandsRegistrar(graphView.Dispatcher, graphView.GraphViewState, graphView.SelectionState,
                graphView.BlackboardViewState, graphTool).RegisterCommandHandlers();
        }

        internal static void RegisterCommands(Dispatcher dispatcher, GraphViewStateComponent graphViewState, SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewState, BaseGraphTool graphTool)
        {
            new BlackboardCommandsRegistrar(dispatcher, graphViewState, selectionState, blackboardViewState, graphTool).RegisterCommandHandlers();
        }

        Dispatcher m_Dispatcher;
        GraphViewStateComponent m_GraphViewState;
        SelectionStateComponent m_SelectionState;
        BlackboardViewStateComponent m_BlackboardViewState;
        BaseGraphTool m_GraphTool;

        BlackboardCommandsRegistrar(Dispatcher dispatcher, GraphViewStateComponent graphViewState, SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewStateComponent, BaseGraphTool graphTool)
        {
            m_Dispatcher = dispatcher;
            m_GraphViewState = graphViewState;
            m_SelectionState = selectionState;
            m_BlackboardViewState = blackboardViewStateComponent;
            m_GraphTool = graphTool;
        }

        void RegisterCommandHandler<TParam3, TCommand>(CommandHandler<UndoStateComponent, GraphViewStateComponent, TParam3, TCommand> commandHandler, TParam3 handlerParam3)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler(commandHandler, m_GraphTool.UndoStateComponent, m_GraphViewState, handlerParam3);
        }

        void RegisterCommandHandlers()
        {
            RegisterCommandHandler<BlackboardViewStateComponent, CreateGraphVariableDeclarationCommand>(
                CreateGraphVariableDeclarationCommand.DefaultCommandHandler, m_BlackboardViewState);

            RegisterCommandHandler<BlackboardViewStateComponent, ReorderGraphVariableDeclarationCommand>(
                ReorderGraphVariableDeclarationCommand.DefaultCommandHandler, m_BlackboardViewState);

            m_Dispatcher.RegisterCommandHandler<UndoStateComponent, BlackboardViewStateComponent, GraphViewStateComponent,
                SelectionStateComponent, BlackboardGroupCreateCommand>(BlackboardGroupCreateCommand.DefaultCommandHandler,
                m_GraphTool.UndoStateComponent, m_BlackboardViewState, m_GraphViewState, m_SelectionState);
        }
    }
}
