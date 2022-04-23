using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Helper methods to create a subgraph.
    /// </summary>
    static class SubgraphCreationHelpers
    {
        /// <summary>
        /// The data needed to recreate graph elements in a subgraph.
        /// </summary>
        internal struct GraphElementsToAddToSubgraph
        {
            public HashSet<IStickyNoteModel> StickyNoteModels;
            public HashSet<IPlacematModel> PlacematModels;
            public HashSet<IEdgeModel> EdgeModels;
            public HashSet<INodeModel> NodeModels;

            internal static GraphElementsToAddToSubgraph ConvertToGraphElementsToAdd(IEnumerable<IGraphElementModel> sourceElements)
            {
                GraphElementsToAddToSubgraph elementsToAdd;

                elementsToAdd.StickyNoteModels = new HashSet<IStickyNoteModel>();
                elementsToAdd.PlacematModels = new HashSet<IPlacematModel>();
                elementsToAdd.EdgeModels = new HashSet<IEdgeModel>();
                elementsToAdd.NodeModels = new HashSet<INodeModel>();

                SortElementsByType(elementsToAdd, sourceElements);

                return elementsToAdd;
            }

            static void SortElementsByType(GraphElementsToAddToSubgraph elementsToAdd, IEnumerable<IGraphElementModel> sourceElements)
            {
                foreach (var element in sourceElements)
                {
                    if (element is IGraphElementContainer container)
                        SortContainer(elementsToAdd, container);

                    switch (element)
                    {
                        case IStickyNoteModel stickyNoteModel:
                            elementsToAdd.StickyNoteModels.Add(stickyNoteModel);
                            break;
                        case IPlacematModel placematModel:
                            elementsToAdd.PlacematModels.Add(placematModel);
                            break;
                        case IEdgeModel edgeModel:
                            elementsToAdd.EdgeModels.Add(edgeModel);
                            break;
                        case INodeModel nodeModel:
                            if (nodeModel.HasCapability(Capabilities.NeedsContainer))
                                SortContainer(elementsToAdd, nodeModel.Container);
                            elementsToAdd.NodeModels.Add(nodeModel);
                            break;
                    }
                }
            }

            static void SortContainer(GraphElementsToAddToSubgraph elementsToAdd, IGraphElementContainer container)
            {
                if (container is INodeModel containerNodeModel)
                {
                    if (elementsToAdd.NodeModels.Contains(containerNodeModel))
                        return;

                    elementsToAdd.NodeModels.Add(containerNodeModel);
                    SortElementsByType(elementsToAdd, container.GraphElementModels);
                }
                else
                {
                    // TODO: IGraphElementContainer might not be INodeModel in the future, it should be added to the right type HashSet
                    SortElementsByType(elementsToAdd, container.GraphElementModels);
                }
            }
        }

        /// <summary>
        /// Populates the subgraph with variable declarations and graph elements.
        /// </summary>
        /// <param name="graphModel">The subgraph.</param>
        /// <param name="sourceElementsToAdd">The selected graph elements to be recreated in the subgraph.</param>
        /// <param name="allEdges">The edge models to be recreated in the subgraph.</param>
        /// <param name="inputEdgeConnections">A dictionary of input edge connections to their corresponding subgraph node port's unique name.</param>
        /// <param name="outputEdgeConnections">A dictionary of output edge connections to their corresponding subgraph node port's unique name.</param>
        internal static void PopulateSubgraph(IGraphModel graphModel,
            GraphElementsToAddToSubgraph sourceElementsToAdd, IEnumerable<IEdgeModel> allEdges,
            Dictionary<IEdgeModel, string> inputEdgeConnections, Dictionary<IEdgeModel, string> outputEdgeConnections)
        {
            // Add input and output variable declarations to the subgraph
            CreateVariableDeclaration(graphModel, inputEdgeConnections, true);
            CreateVariableDeclaration(graphModel, outputEdgeConnections, false);

            // Add the graph elements to the subgraph
            AddGraphElementsToSubgraph(graphModel, sourceElementsToAdd, allEdges, inputEdgeConnections, outputEdgeConnections);
        }

        /// <summary>
        /// Creates all the edges connected to the subgraph node.
        /// </summary>
        /// <param name="graphModel">The graph asset of the subgraph.</param>
        /// <param name="subgraphNode">The selected graph elements to be recreated in the subgraph.</param>
        /// <param name="inputEdgeConnections">A dictionary of input edge connections to their corresponding subgraph node port's unique name.</param>
        /// <param name="outputEdgeConnections">A dictionary of output edge connections to their corresponding subgraph node port's unique name.</param>
        internal static IEnumerable<IEdgeModel> CreateEdgesConnectedToSubgraphNode(IGraphModel graphModel, ISubgraphNodeModel subgraphNode, Dictionary<IEdgeModel, string> inputEdgeConnections, Dictionary<IEdgeModel, string> outputEdgeConnections)
        {
            var newEdges = new List<IEdgeModel>();

            var subgraphNodeInputPorts = subgraphNode.DataInputPortToVariableDeclarationDictionary.Keys.Concat(subgraphNode.ExecutionInputPortToVariableDeclarationDictionary.Keys).ToList();
            var subgraphNodeOutputPorts = subgraphNode.DataOutputPortToVariableDeclarationDictionary.Keys.Concat(subgraphNode.ExecutionOutputPortToVariableDeclarationDictionary.Keys).ToList();

            CreateEdgesConnectedToSubgraphNode(newEdges, graphModel, subgraphNodeInputPorts, inputEdgeConnections, true);
            CreateEdgesConnectedToSubgraphNode(newEdges, graphModel, subgraphNodeOutputPorts, outputEdgeConnections, false);

            return newEdges;
        }

        static void CreateVariableDeclaration(IGraphModel graphModel, Dictionary<IEdgeModel, string> edgeConnections, bool isInput)
        {
            foreach (var edge in edgeConnections.Keys.ToList())
            {
                var portToSubgraph = isInput ? edge.ToPort : edge.FromPort;
                var newGuid = portToSubgraph.Guid;
                var variable = graphModel.VariableDeclarations.FirstOrDefault(v => v.Guid == newGuid);
                if (variable == null)
                {
                    var otherPort = isInput ? edge.FromPort : edge.ToPort;
                    variable = graphModel.CreateGraphVariableDeclaration(otherPort.DataTypeHandle, (portToSubgraph as IHasTitle)?.Title, isInput ? ModifierFlags.Read : ModifierFlags.Write, true, guid: newGuid);
                }

                // Used to keep track of the edge connections to the right subgraph node ports. The subgraph node ports will have the corresponding variable's guid as port id.
                edgeConnections[edge] = variable.Guid.ToString();
            }
        }

        static void AddGraphElementsToSubgraph(IGraphModel graphModel,
            GraphElementsToAddToSubgraph sourceGraphElementToAdd, IEnumerable<IEdgeModel> allEdges,
            Dictionary<IEdgeModel, string> inputEdgeConnections,
            Dictionary<IEdgeModel, string> outputEdgeConnections)
        {
            var elementMapping = new Dictionary<string, IGraphElementModel>();

            if (sourceGraphElementToAdd.NodeModels != null)
            {
                foreach (var sourceNode in sourceGraphElementToAdd.NodeModels)
                {
                    // Ignore Blocks. They are duplicated when their Context is duplicated.
                    if (sourceNode.HasCapability(Capabilities.NeedsContainer))
                        continue;

                    var pastedNode = graphModel.DuplicateNode(sourceNode, Vector2.zero);
                    elementMapping.Add(sourceNode.Guid.ToString(), pastedNode);

                    if (sourceNode is IGraphElementContainer sourceContainer && pastedNode is IGraphElementContainer pastedContainer)
                    {
                        using (var pastedIter = pastedContainer.GraphElementModels.GetEnumerator())
                        using (var sourceIter = sourceContainer.GraphElementModels.GetEnumerator())
                        {
                            while (pastedIter.MoveNext() && sourceIter.MoveNext())
                            {
                                if (sourceIter.Current is INodeModel sourceElement && pastedIter.Current is INodeModel pastedElement)
                                {
                                    elementMapping.Add(sourceElement.Guid.ToString(), pastedElement);
                                }
                            }
                        }
                    }
                }
            }

            if (allEdges != null)
            {
                const float offset = 20;
                var existingPositions = new List<Vector2>();

                foreach (var sourceEdge in allEdges)
                {
                    elementMapping.TryGetValue(sourceEdge.ToNodeGuid.ToString(), out var newInput);
                    elementMapping.TryGetValue(sourceEdge.FromNodeGuid.ToString(), out var newOutput);

                    if (newOutput == null)
                    {
                        var toPortId = inputEdgeConnections[sourceEdge];

                        // input data
                        var declarationModel = graphModel.VariableDeclarations.FirstOrDefault(v => v.Guid.ToString() == toPortId);
                        if (declarationModel != null)
                        {
                            var inputPortModel = (newInput as IInputOutputPortsNodeModel)?.InputsById[sourceEdge.ToPortId];
                            // If the port is already connected to a variable node with the same declaration, do not create a new variable node
                            if (inputPortModel != null && !inputPortModel.GetConnectedPorts().Any(p => p.NodeModel is IVariableNodeModel variableNode && variableNode.VariableDeclarationModel.Guid == declarationModel.Guid))
                            {
                                var variableNodeModel = graphModel.CreateVariableNode(declarationModel, GetNewVariablePosition(existingPositions, sourceEdge.FromPort.NodeModel.Position, offset));
                                graphModel.CreateEdge(inputPortModel, variableNodeModel.OutputPort);
                            }
                        }
                    }
                    else if (newInput == null)
                    {
                        var fromPortId = outputEdgeConnections[sourceEdge];

                        // output data
                        var declarationModel = graphModel.VariableDeclarations.FirstOrDefault(v => v.Guid.ToString() == fromPortId);
                        if (declarationModel != null)
                        {
                            var outputPortModel = (newOutput as IInputOutputPortsNodeModel)?.OutputsById[sourceEdge.FromPortId];
                            // If the port is already connected to a variable node with the same declaration, do not create a new variable node
                            if (outputPortModel != null && !outputPortModel.GetConnectedPorts().Any(p => p.NodeModel is IVariableNodeModel variableNode && variableNode.VariableDeclarationModel.Guid == declarationModel.Guid))
                            {
                                var variableNodeModel = graphModel.CreateVariableNode(declarationModel, GetNewVariablePosition(existingPositions, sourceEdge.ToPort.NodeModel.Position, offset));
                                graphModel.CreateEdge(variableNodeModel.InputPort, outputPortModel);
                            }
                        }
                    }
                    else
                    {
                        graphModel.DuplicateEdge(sourceEdge, newInput as INodeModel, newOutput as INodeModel);
                    }
                    elementMapping.Add(sourceEdge.Guid.ToString(), sourceEdge);
                }
            }

            if (sourceGraphElementToAdd.NodeModels != null)
            {
                foreach (var sourceVariableNode in sourceGraphElementToAdd.NodeModels.Where(model => model is IVariableNodeModel))
                {
                    elementMapping.TryGetValue(sourceVariableNode.Guid.ToString(), out var newNode);
                    var variableDeclarationModel = graphModel.DuplicateGraphVariableDeclaration(((IVariableNodeModel)sourceVariableNode).VariableDeclarationModel);

                    if (newNode != null)
                        ((IVariableNodeModel)newNode).DeclarationModel = variableDeclarationModel;
                }
            }

            if (sourceGraphElementToAdd.StickyNoteModels != null)
            {
                foreach (var stickyNote in sourceGraphElementToAdd.StickyNoteModels)
                {
                    var newPosition = new Rect(stickyNote.PositionAndSize.position, stickyNote.PositionAndSize.size);
                    var pastedStickyNote = graphModel.CreateStickyNote(newPosition);
                    pastedStickyNote.Title = stickyNote.Title;
                    pastedStickyNote.Contents = stickyNote.Contents;
                    pastedStickyNote.Theme = stickyNote.Theme;
                    pastedStickyNote.TextSize = stickyNote.TextSize;
                    elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
                }
            }

            if (sourceGraphElementToAdd.PlacematModels != null)
            {
                var pastedPlacemats = new List<IPlacematModel>();

                foreach (var placemat in sourceGraphElementToAdd.PlacematModels)
                {
                    var newPosition = new Rect(placemat.PositionAndSize.position, placemat.PositionAndSize.size);
                    var pastedPlacemat = graphModel.CreatePlacemat(newPosition);
                    pastedPlacemat.Title = placemat.Title;
                    pastedPlacemat.Color = placemat.Color;
                    pastedPlacemat.Collapsed = placemat.Collapsed;
                    pastedPlacemat.HiddenElements = (placemat).HiddenElements;
                    pastedPlacemats.Add(pastedPlacemat);
                    elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
                }
                // Update hidden content to new node ids.
                foreach (var pastedPlacemat in pastedPlacemats)
                {
                    if (pastedPlacemat.Collapsed)
                    {
                        foreach (var hiddenElement in pastedPlacemat.HiddenElements)
                        {
                            if (elementMapping.TryGetValue(hiddenElement.Guid.ToString(), out var pastedElement))
                            {
                                hiddenElement.Guid = pastedElement.Guid;
                            }
                        }
                    }
                }
            }
        }

        static Vector2 GetNewVariablePosition(List<Vector2> existingPositions, Vector2 position, float offset)
        {
            while (existingPositions.Any(p => (p - position).sqrMagnitude < offset * offset))
            {
                position.x += offset;
                position.y += offset;
            }
            existingPositions.Add(position);

            return position;
        }

        static void CreateEdgesConnectedToSubgraphNode(List<IEdgeModel> newEdges, IGraphModel graphModel, List<IPortModel> portsOnSubgraphNode, Dictionary<IEdgeModel, string> edgeConnections, bool isInput)
        {
            foreach (var edgeConnection in edgeConnections)
            {
                var portOnSubgraphNode = portsOnSubgraphNode.FirstOrDefault(p => p.UniqueName == edgeConnection.Value);
                var edge = edgeConnection.Key;

                newEdges.Add(isInput ? graphModel.CreateEdge(portOnSubgraphNode, edge.FromPort) : graphModel.CreateEdge(edge.ToPort, portOnSubgraphNode));
            }
        }
    }
}
