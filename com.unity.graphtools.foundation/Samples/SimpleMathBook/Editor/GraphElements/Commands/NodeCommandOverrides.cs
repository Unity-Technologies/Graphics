using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class NodeCommandOverrides
    {
        public static void HandleRenameNode(UndoStateComponent undoState, GraphModelStateComponent graphModelState, RenameElementCommand command)
        {
            NodeEdgeDiff edgeDiff = null;

            if (command.Model is MathExpressionNode expressionNode)
                edgeDiff = new NodeEdgeDiff(expressionNode, PortDirection.Input);

            RenameElementCommand.DefaultCommandHandler(undoState, graphModelState, command);

            var deletedEdges = edgeDiff?.GetDeletedEdges().ToList();
            if (deletedEdges != null && deletedEdges.Count > 0)
            {
                using (var graphUpdater = graphModelState.UpdateScope)
                {
                    graphUpdater.MarkDeleted(deletedEdges);
                }
            }
        }
    }
}
