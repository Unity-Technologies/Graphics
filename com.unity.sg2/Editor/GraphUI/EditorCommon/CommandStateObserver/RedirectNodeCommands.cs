using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class AddRedirectNodeCommand : UndoableCommand
    {
        static readonly Vector2 k_RedirectSize = new(56, 25);

        public readonly IEdgeModel Edge;
        public readonly Vector2 Position;

        public AddRedirectNodeCommand(IEdgeModel edge, Vector2 position)
        {
            Edge = edge;
            Position = position;
        }

        public static void DefaultHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            AddRedirectNodeCommand command
        )
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveSingleState(graphModelState, command);
            }

            var graphModel = graphModelState.GraphModel;
            using var updater = graphModelState.UpdateScope;

            var fromPort = command.Edge.FromPort;
            var toPort = command.Edge.ToPort;

            graphModel.DeleteEdge(command.Edge);
            updater.MarkDeleted(command.Edge);

            var nodeModel = graphModel.CreateNode<RedirectNodeModel>(position: command.Position - k_RedirectSize / 2);
            nodeModel.UpdateTypeFrom(fromPort);
            updater.MarkNew(nodeModel);

            updater.MarkNew(new[]
            {
                graphModel.CreateEdge(nodeModel.InputPort, fromPort),
                graphModel.CreateEdge(toPort, nodeModel.OutputPort)
            });
        }
    }
}
