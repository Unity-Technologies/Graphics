using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.Controllers
{
    class PreviewController : GraphSubWindowController<Preview, PreviewOverlay>
    {
        GraphPreviewStateObserver m_PreviewStateObserver;

        protected override string OverlayID => PreviewOverlay.k_OverlayID;

        public PreviewController(CommandDispatcher dispatcher, GraphView parentGraphView, EditorWindow parentWindow) : base(dispatcher, parentGraphView, parentWindow)
        {
            View = new Preview();

            m_PreviewStateObserver = new GraphPreviewStateObserver();
            dispatcher.RegisterObserver(m_PreviewStateObserver);

            dispatcher.RegisterCommandHandler<UpdatePortConstantCommand>(HandleUpdatePortConstant);
        }

        static void HandleUpdatePortConstant(GraphToolState graphToolState, UpdatePortConstantCommand command)
        {
            UpdatePortConstantCommand.DefaultCommandHandler(graphToolState, command);

            if (graphToolState is ShaderGraphState shaderGraphState)
            {
                using var previewUpdater = shaderGraphState.GraphPreviewState.UpdateScope;
                {
                    if (command.PortModel.NodeModel is GraphDataNodeModel graphDataNodeModel)
                    {
                        previewUpdater.UpdateNodePortConstantValue(command.PortModel.Guid.ToString(), command.NewValue, graphDataNodeModel);
                    }
                }
            }
        }
    }
}
