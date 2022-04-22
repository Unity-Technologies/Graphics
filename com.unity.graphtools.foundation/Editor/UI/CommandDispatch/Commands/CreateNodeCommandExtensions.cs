using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Utility/Factory methods to generate a <see cref="CreateNodeCommand"/>.
    /// </summary>
    public static class CreateNodeCommandExtensions
    {
        /// <summary>
        /// Adds a graph node from a searcher item to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="searcherItem">The searcher item to create a node from.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnGraph(this CreateNodeCommand command, GraphNodeModelSearcherItem searcherItem, Vector2 position, SerializableGUID guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                SearcherItem = searcherItem,
                Position = position,
                Guid = guid
            });
        }

        /// <summary>
        /// Adds a variable to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="variableDeclaration">The variable to create.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnGraph(this CreateNodeCommand command, IVariableDeclarationModel variableDeclaration, Vector2 position, SerializableGUID guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                VariableDeclaration = variableDeclaration,
                Position = position,
                Guid = guid
            });
        }

        /// <summary>
        /// Adds a node from a searcher item inserted in the middle of an edge to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="searcherItem">The searcher item to create a node from.</param>
        /// <param name="edgeModel">The edge on which to insert the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnEdge(this CreateNodeCommand command, GraphNodeModelSearcherItem searcherItem, IEdgeModel edgeModel, Vector2 position, SerializableGUID guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                SearcherItem = searcherItem,
                EdgeToInsertOn = edgeModel,
                Position = position,
                Guid = guid
            });
        }

        /// <summary>
        /// Adds a node from a searcher item inserted on an edge to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="searcherItem">The searcher item to create a node from.</param>
        /// <param name="edges">The edges on which to insert the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnEdges(this CreateNodeCommand command, GraphNodeModelSearcherItem searcherItem, IEnumerable<(IEdgeModel, EdgeSide)> edges, Vector2 position, SerializableGUID guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                SearcherItem = searcherItem,
                EdgesToConnect = edges,
                Position = position,
                Guid = guid
            });
        }

        /// <summary>
        /// Adds a node from a searcher item connected to a port to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="searcherItem">The searcher item to create a node from.</param>
        /// <param name="portModel">The port on which to connect the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="autoAlign">If true, the node will try to align automatically with the port after creation.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnPort(this CreateNodeCommand command, GraphNodeModelSearcherItem searcherItem, IPortModel portModel, Vector2 position, bool autoAlign = false, SerializableGUID guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                SearcherItem = searcherItem,
                PortModel = portModel,
                Position = position,
                Guid = guid,
                AutoAlign = autoAlign 
            });
        }

        /// <summary>
        /// Adds variable connected to a port to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="variableDeclaration">The variable to create.</param>
        /// <param name="portModel">The port on which to connect the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="autoAlign">If true, the node will try to align automatically with the port after creation.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnPort(this CreateNodeCommand command, IVariableDeclarationModel variableDeclaration, IPortModel portModel, Vector2 position, bool autoAlign = false, SerializableGUID guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                VariableDeclaration = variableDeclaration,
                PortModel = portModel,
                Position = position,
                Guid = guid,
                AutoAlign = autoAlign
            });
        }

        static CreateNodeCommand WithNode(this CreateNodeCommand command, CreateNodeCommand.NodeData data)
        {
            command.CreationData.Add(data);
            return command;
        }
    }
}
