using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch
{
    public class ChangePreviewModeCommand : ModelCommand<GraphDataNodeModel>
    {
        PreviewMode m_PreviewMode;

        public ChangePreviewModeCommand(PreviewMode previewMode, IReadOnlyList<GraphDataNodeModel> models)
            : base("Change Preview Mode", "Change Preview Modes", models)
        {
            m_PreviewMode = previewMode;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphViewStateComponent graphViewState,
            GraphPreviewStateComponent graphPreviewState,
            ChangePreviewModeCommand command
        )
        {
            undoState.UpdateScope.SaveSingleState(graphViewState, command);
            using var graphUpdater = graphViewState.UpdateScope;
            {
                foreach (var graphDataNodeModel in command.Models)
                {
                    graphDataNodeModel.NodePreviewMode = command.m_PreviewMode;
                    graphUpdater.MarkChanged(command.Models);
                }
            }
            using var previewUpdater = graphPreviewState.UpdateScope;
            {
                // Because every nodes preview mode can affect the modes of those downstream of it
                // we first want to set the preview mode of all the nodes that are being modified
                foreach (var graphDataNodeModel in command.Models)
                {
                    graphDataNodeModel.NodePreviewMode = command.m_PreviewMode;
                }

                // After all the nodes preview modes are set, go through the nodes again
                // and concretize the preview modes that are set to inherit for preview data
                foreach (var graphDataNodeModel in command.Models)
                {
                    previewUpdater.ChangeNodePreviewMode(graphDataNodeModel.Guid.ToString(), graphDataNodeModel, command.m_PreviewMode);
                }
            }
        }
    }
}
