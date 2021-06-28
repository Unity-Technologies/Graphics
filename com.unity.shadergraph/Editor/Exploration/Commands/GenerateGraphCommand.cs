using System;
using System.Collections.Generic;
using GtfPlayground.DataModel;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using Random = UnityEngine.Random;

namespace GtfPlayground.Commands
{
    /// <summary>
    /// A command that generates a random graph of a single node type. Used to demonstrate creating and deleting graph
    /// elements in an undoable command.
    /// </summary>
    public class GenerateGraphCommand : UndoableCommand
    {
        Type m_NodeModel;
        Vector2 m_Origin;
        int m_MaxNodes;

        public GenerateGraphCommand()
        {
            UndoString = "Generate Graph";

            m_NodeModel = typeof(NoOpTwoFloatNodeModel);
            m_Origin = Vector2.zero;
            m_MaxNodes = 15;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, GenerateGraphCommand command)
        {
            var nodesAvailable = command.m_MaxNodes;
            var portsToConnect = new Queue<(IPortModel, int)>();

            graphToolState.PushUndo(command);

            using var graphUpdater = graphToolState.GraphViewState.UpdateScope;
            var graphModel = graphToolState.GraphViewState.GraphModel;

            NodeModel AddNodeToGeneratedGraph(Vector2 pos, int layer, string name)
            {
                var node = (NodeModel) graphModel.CreateNode(command.m_NodeModel, name, pos);
                graphUpdater.MarkNew(node);

                foreach (var outputPort in node.GetOutputPorts())
                {
                    portsToConnect.Enqueue((outputPort, layer));
                }

                return node;
            }

            var nodeNumber = 1;
            var layerSizes = new int[command.m_MaxNodes];
            AddNodeToGeneratedGraph(command.m_Origin, 0, $"Node {nodeNumber++}");

            while (portsToConnect.Count > 0)
            {
                var (from, layer) = Random.value > 0.2 ? portsToConnect.Dequeue() : portsToConnect.Peek();

                var pos = from.NodeModel.Position;
                pos.x += 250;
                pos.y = layerSizes[layer + 1]++ * 150;

                if (nodesAvailable <= 0) continue;

                var to = AddNodeToGeneratedGraph(pos, layer + 1, $"Node {nodeNumber++}");
                nodesAvailable--;

                var edge = graphModel.CreateEdge(to.GetPortFitToConnectTo(from), from);
                graphUpdater.MarkNew(edge);
            }
        }
    }
}
