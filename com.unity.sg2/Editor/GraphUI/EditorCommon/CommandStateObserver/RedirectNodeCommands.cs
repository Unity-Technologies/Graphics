using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using Unity.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class AddRedirectNodeCommand : UndoableCommand
    {
        static readonly Vector2 k_RedirectSize = new(56, 25);

        public readonly WireModel Edge;
        public readonly Vector2 Position;

        public AddRedirectNodeCommand(WireModel edge, Vector2 position)
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
                undoUpdater.SaveState(graphModelState);
            }

            var graphModel = graphModelState.GraphModel;
            using var updater = graphModelState.UpdateScope;

            var fromPort = command.Edge.FromPort;
            var toPort = command.Edge.ToPort;

            graphModel.DeleteWire(command.Edge);
            updater.MarkDeleted(command.Edge);

            var nodeModel = graphModel.CreateNode<RedirectNodeModel>(position: command.Position - k_RedirectSize / 2);
            nodeModel.UpdateTypeFrom(fromPort);
            updater.MarkNew(nodeModel);

            updater.MarkNew(new[]
            {
                graphModel.CreateWire(nodeModel.InputPort, fromPort),
                graphModel.CreateWire(toPort, nodeModel.OutputPort)
            });
        }
    }
}
