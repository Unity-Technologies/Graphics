using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using PreviewRenderMode = HeadlessPreviewManager.PreviewRenderMode;

    public class ChangePreviewModeCommand : ModelCommand<GraphDataNodeModel>
    {
        PreviewRenderMode m_PreviewMode;

        // Needed for the ModelPropertyField used by the SGNodeFieldsInspector
        public ChangePreviewModeCommand(object previewMode, IModel model)
            : base("Change Preview Mode", "Change Preview Modes", new []{ model as GraphDataNodeModel })
        {
            m_PreviewMode = (PreviewRenderMode)previewMode;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            PreviewManager previewManager,
            ChangePreviewModeCommand command
        )
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using(var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var graphDataNodeModel in command.Models)
                {
                    graphDataNodeModel.NodePreviewMode = command.m_PreviewMode;
                    graphUpdater.MarkChanged(command.Models);
                }
            }

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
                previewManager.OnPreviewModeChanged(graphDataNodeModel.graphDataName, command.m_PreviewMode);
            }
        }
    }
}
