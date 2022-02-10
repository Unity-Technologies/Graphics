using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    abstract class GraphTraversal
    {
        public void VisitGraph(IGraphModel graphModel)
        {
            if (graphModel?.Stencil == null)
                return;

            var visitedNodes = new HashSet<INodeModel>();
            foreach (var entryPoint in graphModel.Stencil.GetEntryPoints())
            {
                VisitNode(entryPoint, visitedNodes);
            }

            // floating nodes
            foreach (var node in graphModel.NodeModels)
            {
                if (node == null || visitedNodes.Contains(node))
                    continue;

                VisitNode(node, visitedNodes);
            }

            foreach (var variableDeclaration in graphModel.VariableDeclarations)
            {
                VisitVariableDeclaration(variableDeclaration);
            }

            foreach (var edgeModel in graphModel.EdgeModels)
            {
                VisitEdge(edgeModel);
            }
        }

        protected virtual void VisitEdge(IEdgeModel edgeModel)
        {
        }

        protected virtual void VisitNode(INodeModel nodeModel, HashSet<INodeModel> visitedNodes)
        {
            if (nodeModel == null)
                return;

            visitedNodes.Add(nodeModel);

            if (nodeModel is IInputOutputPortsNodeModel portHolder)
            {
                foreach (var inputPortModel in portHolder.InputsById.Values)
                {
                    if (inputPortModel.IsConnected())
                        foreach (var connectionPortModel in inputPortModel.GetConnectedPorts())
                        {
                            if (!visitedNodes.Contains(connectionPortModel.NodeModel))
                                VisitNode(connectionPortModel.NodeModel, visitedNodes);
                        }
                }
            }
        }

        protected virtual void VisitVariableDeclaration(IVariableDeclarationModel variableDeclarationModel) {}
    }
}
