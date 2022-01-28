using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview;
using UnityEditor.ShaderGraph.GraphDelta;
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
