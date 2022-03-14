using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ChangePreviewExpandedCommand : ModelCommand<GraphDataNodeModel>
    {
        bool m_IsPreviewExpanded;
        public ChangePreviewExpandedCommand(bool isPreviewExpanded, IReadOnlyList<GraphDataNodeModel> models)
            : base("Change Preview Expansion", "Change Previews Expansion", models)
        {
            m_IsPreviewExpanded = isPreviewExpanded;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphViewStateComponent graphViewState,
            PreviewManager previewManager,
            ChangePreviewExpandedCommand command
        )
        {
            undoState.UpdateScope.SaveSingleState(graphViewState, command);
            using var graphUpdater = graphViewState.UpdateScope;
            {
                foreach (var graphDataNodeModel in command.Models)
                {
                    graphDataNodeModel.IsPreviewVisible = command.m_IsPreviewExpanded;
                    graphUpdater.MarkChanged(command.Models);
                }
            }

            foreach (var graphDataNodeModel in command.Models)
            {
                previewManager.OnPreviewExpansionChanged(graphDataNodeModel.graphDataName, command.m_IsPreviewExpanded);
            }
        }
    }
}
