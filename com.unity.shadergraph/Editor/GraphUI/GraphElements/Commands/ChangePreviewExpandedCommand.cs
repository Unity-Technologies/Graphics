using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch
{
    public class ChangePreviewExpandedCommand : ModelCommand<GraphDataNodeModel>
    {
        bool m_IsPreviewExpanded;
        public ChangePreviewExpandedCommand(bool isPreviewExpanded, IReadOnlyList<GraphDataNodeModel> models)
            : base("Change Preview Expansion", "Change Previews Expansion", models)
        {
            m_IsPreviewExpanded = isPreviewExpanded;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangePreviewExpandedCommand command)
        {
            graphToolState.PushUndo(command);
            using var graphUpdater = graphToolState.GraphViewState.UpdateScope;
            {
                foreach (var graphDataNodeModel in command.Models)
                {
                    graphDataNodeModel.IsPreviewVisible = command.m_IsPreviewExpanded;
                    graphUpdater.MarkChanged(command.Models);
                }
            }

            if (graphToolState is ShaderGraphState shaderGraphState)
            {
                using var previewUpdater = shaderGraphState.GraphPreviewState.UpdateScope;
                {
                    foreach (var graphDataNodeModel in command.Models)
                    {
                        previewUpdater.ChangePreviewExpansionState(graphDataNodeModel.Guid.ToString(), command.m_IsPreviewExpanded);
                    }
                }
            }
        }
    }
}
