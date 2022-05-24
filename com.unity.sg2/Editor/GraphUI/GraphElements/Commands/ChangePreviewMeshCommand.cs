using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ChangePreviewMeshCommand : ICommand
    {
        Mesh m_NewPreviewMesh;
        public ChangePreviewMeshCommand(Mesh newPreviewMesh)
        {
            m_NewPreviewMesh = newPreviewMesh;
        }

        public static void DefaultCommandHandler(
            ShaderGraphModel graphModel,
            PreviewManager previewManager,
            ChangePreviewMeshCommand command
        )
        {
            graphModel.MainPreviewData.mesh = command.m_NewPreviewMesh;
            // Lets the preview manager know to re-render the main preview output
            previewManager.OnPreviewMeshChanged();
        }
    }
}
