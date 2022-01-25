//using UnityEditor.GraphToolsFoundation.Overdrive;
//using UnityEditor.ShaderGraph.GraphUI.DataModel;
//using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
//using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;
//using UnityEngine;


// TODO(Brett): Identify why our command handler would not satisfy the syntax requirements for being registered with the command dispatcher.

//namespace UnityEditor.ShaderGraph.GraphUI.Controllers
//{
//    class PreviewController : GraphSubWindowController<Preview, PreviewOverlay>
//    {
//        GraphPreviewStateObserver m_PreviewStateObserver;

//        protected override string OverlayID => PreviewOverlay.k_OverlayID;

//        public PreviewController(CommandDispatcher dispatcher, GraphView graphView, BaseGraphTool graphTool, EditorWindow parentWindow) : base(dispatcher, graphView, graphTool, parentWindow)
//        {
//            View = new Preview();

//            GraphPreviewStateComponent graphPreviewState = PersistedState.GetOrCreateViewStateComponent<GraphPreviewStateComponent>("Graph Preview State", ShaderGraphState.m_previewStateWindowGUID);

//            m_PreviewStateObserver = new GraphPreviewStateObserver();
//            graphView.GraphTool.ObserverManager.RegisterObserver(m_PreviewStateObserver);
//            dispatcher.RegisterCommandHandler<UndoStateComponent, GraphViewStateComponent, GraphPreviewStateComponent, UpdatePortConstantCommand>
//                (
//                (CommandHandler<UpdatePortConstantCommand, UndoStateComponent, GraphViewStateComponent, GraphPreviewStateComponent>)
//                    HandleUpdatePortConstant,
//                    graphTool.UndoStateComponent,
//                    graphView.GraphViewState,
//                    graphPreviewState
//                );
//        }

//        static void HandleUpdatePortConstant(UndoStateComponent undoState, GraphViewStateComponent graphViewState, GraphPreviewStateComponent graphPreviewState, UpdatePortConstantCommand command)
//        {
//            UpdatePortConstantCommand.DefaultCommandHandler(undoState, graphViewState, command);

//            {
//                using var previewUpdater = graphPreviewState.UpdateScope;
//                {
//                    if (command.PortModel.NodeModel is GraphDataNodeModel graphDataNodeModel)
//                    {
//                        previewUpdater.UpdateNodePortConstantValue(command.PortModel.Guid.ToString(), command.NewValue, graphDataNodeModel);
//                    }
//                }
//            }
//        }
//    }
//}
