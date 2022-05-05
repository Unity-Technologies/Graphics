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
            ShaderGraphAssetModel graphAsset,
            PreviewManager previewManager,
            ChangePreviewMeshCommand command
        )
        {
            graphAsset.SetPreviewMesh(command.m_NewPreviewMesh);
            // Lets the preview manager know to re-render the main preview output
            previewManager.OnPreviewMeshChanged();
        }
    }
}
