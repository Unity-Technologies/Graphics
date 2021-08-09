using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class ShaderGraphModel : GraphModel
    {
        IGraphHandler m_GraphHandler;

        // TODO: This will eventually delegate to real serialized data. For now, it's kept here ephemerally. Until
        //       then, (at least) the following features are broken as a result: reloading assembly, saving, loading.
        public IGraphHandler GraphHandler => m_GraphHandler ??= GraphUtil.CreateGraph();

        bool TestConnection(GraphDataPortModel src, GraphDataPortModel dst)
        {
            return GraphHandler.TestConnection(dst.graphDataNodeModel.graphDataName,
                dst.graphDataName, src.graphDataNodeModel.graphDataName,
                src.graphDataName, ((ShaderGraphStencil) Stencil).GetRegistry());
        }

        public bool TryConnect(GraphDataPortModel src, GraphDataPortModel dst)
        {
            return GraphHandler.TryConnect(
                dst.graphDataNodeModel.graphDataName, dst.graphDataName,
                src.graphDataNodeModel.graphDataName, src.graphDataName,
                ((ShaderGraphStencil) Stencil).GetRegistry());
        }

        static bool PortsFormCycle(IPortModel fromPort, IPortModel toPort)
        {
            var queue = new Queue<IPortNodeModel>();
            queue.Enqueue(fromPort.NodeModel);

            while (queue.Count > 0)
            {
                var checkNode = queue.Dequeue();

                if (checkNode == toPort.NodeModel) return true;

                foreach (var incomingEdge in checkNode.GetIncomingEdges())
                {
                    queue.Enqueue(incomingEdge.FromPort.NodeModel);
                }
            }

            return false;
        }

        protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            if (startPortModel.Direction == compatiblePortModel.Direction) return false;

            var fromPort = startPortModel.Direction == PortDirection.Output ? startPortModel : compatiblePortModel;
            var toPort = startPortModel.Direction == PortDirection.Input ? startPortModel : compatiblePortModel;

            if (PortsFormCycle(fromPort, toPort)) return false;

            if (fromPort.NodeModel is RedirectNodeModel fromRedirect)
            {
                fromPort = fromRedirect.ResolveSource();

                // TODO: figure out how to match "loose" redirect behavior from ShaderGraph
                if (fromPort == null) return true;
            }

            if (toPort.NodeModel is RedirectNodeModel toRedirect)
            {
                // Only connect to a hanging branch if it's valid for every connection.
                // Should not recurse more than once. Resolve{Source,Destinations} return non-redirect nodes.
                return toRedirect.ResolveDestinations().All(testPort => IsCompatiblePort(fromPort, testPort));
            }

            if ((fromPort, toPort) is (GraphDataPortModel fromDataPort, GraphDataPortModel toDataPort))
            {
                return fromDataPort.graphDataNodeModel.existsInGraphData &&
                       toDataPort.graphDataNodeModel.existsInGraphData &&
                       TestConnection(fromDataPort, toDataPort);
            }

            // Don't support connecting GraphDelta-backed ports to UI-only ones.
            if (fromPort is GraphDataPortModel || toPort is GraphDataPortModel)
            {
                return false;
            }

            return base.IsCompatiblePort(startPortModel, compatiblePortModel);
        }
    }
}
