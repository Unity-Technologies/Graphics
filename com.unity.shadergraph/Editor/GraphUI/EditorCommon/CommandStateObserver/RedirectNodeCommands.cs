using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public class AddRedirectNodeCommand : UndoableCommand
    {
        public IEdgeModel Edge;
        public Vector2 Position;

        public AddRedirectNodeCommand(IEdgeModel edge, Vector2 position)
        {
            Edge = edge;
            Position = position;
        }

        public static void DefaultHandler(GraphToolState state, AddRedirectNodeCommand command)
        {
            var graphModel = state.GraphViewState.GraphModel;
            using var updater = state.GraphViewState.UpdateScope;

            var fromPort = command.Edge.FromPort;
            var toPort = command.Edge.ToPort;

            graphModel.DeleteEdge(command.Edge);
            updater.MarkDeleted(command.Edge);

            var redirectNode = graphModel.CreateNode<RedirectNodeModel>(position: command.Position);
            redirectNode.UpdateTypeFrom(fromPort);
            updater.MarkNew(redirectNode);

            updater.MarkNew(new[]
            {
                graphModel.CreateEdge(redirectNode.InputPort, fromPort),
                graphModel.CreateEdge(toPort, redirectNode.OutputPort)
            });
        }
    }
}
