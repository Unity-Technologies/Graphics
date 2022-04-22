using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for <see cref="IGraphModel"/>.
    /// </summary>
    public static class GraphModelExtensions
    {
        static readonly Vector2 k_PortalOffset = Vector2.right * 150;

        public static IEnumerable<IHasDeclarationModel> FindReferencesInGraph(this IGraphModel self, IDeclarationModel variableDeclarationModel)
        {
            return self.NodeModels.OfType<IHasDeclarationModel>().Where(v => v.DeclarationModel != null && variableDeclarationModel.Guid == v.DeclarationModel.Guid);
        }

        public static IEnumerable<T> FindReferencesInGraph<T>(this IGraphModel self, IDeclarationModel variableDeclarationModel) where T : IHasDeclarationModel
        {
            return self.FindReferencesInGraph(variableDeclarationModel).OfType<T>();
        }

        public static IEnumerable<IPortModel> GetPortModels(this IGraphModel self)
        {
            IEnumerable<IPortModel> result = Enumerable.Empty<IPortModel>();

            foreach (var element in self.GraphElementModels)
            {
                result = result.Concat(RecursivelyGetPortModels(element));
            }

            return result;
        }

        static IEnumerable<IPortModel> RecursivelyGetPortModels(IGraphElementModel model)
        {
            IEnumerable<IPortModel> result;

            if (model is IPortNodeModel portNode)
                result = portNode.Ports;
            else
                result = Enumerable.Empty<IPortModel>();

            if (model is IGraphElementContainer container)
            {
                foreach (var element in container.GraphElementModels)
                    result = result.Concat(RecursivelyGetPortModels(element));
            }

            return result;
        }

        /// <summary>
        /// Creates a new node in a graph.
        /// </summary>
        /// <param name="self">The graph to add a node to.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <typeparam name="TNodeType">The type of the new node to create.</typeparam>
        /// <returns>The newly created node.</returns>
        public static TNodeType CreateNode<TNodeType>(this IGraphModel self, string nodeName = "", Vector2 position = default,
            SerializableGUID guid = default, Action<TNodeType> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
            where TNodeType : class, INodeModel
        {
            Action<INodeModel> setupWrapper = null;
            if (initializationCallback != null)
            {
                setupWrapper = n => initializationCallback.Invoke(n as TNodeType);
            }

            return (TNodeType)self.CreateNode(typeof(TNodeType), nodeName, position, guid, setupWrapper, spawnFlags);
        }

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="self">The graph to add a variable declaration to.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">THe index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <typeparam name="TDeclType">The type of variable declaration to create.</typeparam>
        /// <returns>The newly created variable declaration.</returns>
        public static TDeclType CreateGraphVariableDeclaration<TDeclType>(this IGraphModel self, TypeHandle variableDataType,
            string variableName, ModifierFlags modifierFlags, bool isExposed, IGroupModel group = null, int indexInGroup = int.MaxValue, IConstant initializationModel = null,
            SerializableGUID guid = default, Action<TDeclType, IConstant> initializationCallback = null,
            SpawnFlags spawnFlags = SpawnFlags.Default)
            where TDeclType : class, IVariableDeclarationModel
        {
            return (TDeclType)self.CreateGraphVariableDeclaration(typeof(TDeclType), variableDataType, variableName,
                modifierFlags, isExposed, group, indexInGroup, initializationModel, guid, (d, c) => initializationCallback?.Invoke((TDeclType)d, c), spawnFlags);
        }

        public static IEdgePortalModel CreateOppositePortal(this IGraphModel self, IEdgePortalModel edgePortalModel, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var offset = Vector2.zero;
            switch (edgePortalModel)
            {
                case IEdgePortalEntryModel _:
                    offset = k_PortalOffset;
                    break;
                case IEdgePortalExitModel _:
                    offset = -k_PortalOffset;
                    break;
            }
            var currentPos = edgePortalModel.Position;
            return self.CreateOppositePortal(edgePortalModel, currentPos + offset, spawnFlags);
        }

        public static GraphChangeDescription DeleteVariableDeclaration(this IGraphModel self,
            IVariableDeclarationModel variableDeclarationToDelete, bool deleteUsages)
        {
            return self.DeleteVariableDeclarations(new[] { variableDeclarationToDelete }, deleteUsages);
        }

        public static IReadOnlyCollection<IGraphElementModel> DeleteNode(this IGraphModel self, INodeModel nodeToDelete, bool deleteConnections)
        {
            return self.DeleteNodes(new[] { nodeToDelete }, deleteConnections);
        }

        public static IReadOnlyCollection<IGraphElementModel> DeleteEdge(this IGraphModel self, IEdgeModel edgeToDelete)
        {
            return self.DeleteEdges(new[] { edgeToDelete });
        }

        public static IReadOnlyCollection<IGraphElementModel> DeleteStickyNote(this IGraphModel self, IStickyNoteModel stickyNoteToDelete)
        {
            return self.DeleteStickyNotes(new[] { stickyNoteToDelete });
        }

        public static IReadOnlyCollection<IGraphElementModel> DeletePlacemat(this IGraphModel self, IPlacematModel placematToDelete)
        {
            return self.DeletePlacemats(new[] { placematToDelete });
        }

        struct ElementsByType
        {
            public HashSet<IStickyNoteModel> stickyNoteModels;
            public HashSet<IPlacematModel> placematModels;
            public HashSet<IVariableDeclarationModel> variableDeclarationsModels;
            public HashSet<IGroupModel> groupModels;
            public HashSet<IEdgeModel> edgeModels;
            public HashSet<INodeModel> nodeModels;
        }

        static void RecursiveSortElements(ref ElementsByType elementsByType, IEnumerable<IGraphElementModel> graphElementModels)
        {
            foreach (var element in graphElementModels)
            {
                if (element is IGraphElementContainer container)
                    RecursiveSortElements(ref elementsByType, container.GraphElementModels);
                switch (element)
                {
                    case IStickyNoteModel stickyNoteModel:
                        elementsByType.stickyNoteModels.Add(stickyNoteModel);
                        break;
                    case IPlacematModel placematModel:
                        elementsByType.placematModels.Add(placematModel);
                        break;
                    case IVariableDeclarationModel variableDeclarationModel:
                        elementsByType.variableDeclarationsModels.Add(variableDeclarationModel);
                        break;
                    case IGroupModel groupModel:
                        elementsByType.groupModels.Add(groupModel);
                        break;
                    case IEdgeModel edgeModel:
                        elementsByType.edgeModels.Add(edgeModel);
                        break;
                    case INodeModel nodeModel:
                        elementsByType.nodeModels.Add(nodeModel);
                        break;
                }
            }
        }

        public static GraphChangeDescription DeleteElements(this IGraphModel self,
            IReadOnlyCollection<IGraphElementModel> graphElementModels)
        {
            ElementsByType elementsByType;

            elementsByType.stickyNoteModels = new HashSet<IStickyNoteModel>();
            elementsByType.placematModels = new HashSet<IPlacematModel>();
            elementsByType.variableDeclarationsModels = new HashSet<IVariableDeclarationModel>();
            elementsByType.groupModels = new HashSet<IGroupModel>();
            elementsByType.edgeModels = new HashSet<IEdgeModel>();
            elementsByType.nodeModels = new HashSet<INodeModel>();

            RecursiveSortElements(ref elementsByType, graphElementModels);

            // Add nodes that would be backed by declaration models.
            elementsByType.nodeModels.UnionWith(elementsByType.variableDeclarationsModels.SelectMany(d => self.FindReferencesInGraph<IHasDeclarationModel>(d).OfType<INodeModel>()));

            // Add edges connected to the deleted nodes.
            foreach (var portModel in elementsByType.nodeModels.OfType<IPortNodeModel>().SelectMany(n => n.Ports))
                elementsByType.edgeModels.UnionWith(self.EdgeModels.Where(e => e.ToPort == portModel || e.FromPort == portModel));

            var deletedModels = self.DeleteStickyNotes(elementsByType.stickyNoteModels)
                .Concat(self.DeletePlacemats(elementsByType.placematModels))
                .Concat(self.DeleteEdges(elementsByType.edgeModels))
                .Concat(self.DeleteNodes(elementsByType.nodeModels, deleteConnections: false)).ToList();

            var changeDescription = self.DeleteVariableDeclarations(elementsByType.variableDeclarationsModels, deleteUsages: false);
            changeDescription.Union(self.DeleteGroups(elementsByType.groupModels));
            changeDescription.Union(null, null, deletedModels);
            return changeDescription;
        }

        /// <summary>
        /// Find the single edge that is connected to both port.
        /// </summary>
        /// <param name="self">The graph model.</param>
        /// <param name="toPort">The port the edge is going to.</param>
        /// <param name="fromPort">The port the edge is coming from.</param>
        /// <returns>The edge that connects the two ports, or null.</returns>
        public static IEdgeModel GetEdgeConnectedToPorts(this IGraphModel self, IPortModel toPort, IPortModel fromPort)
        {
            var edges = self?.GetEdgesForPort(toPort);
            if (edges != null)
                foreach (var e in edges)
                {
                    if (e.ToPort == toPort && e.FromPort == fromPort)
                        return e;
                }

            return null;
        }

        public static void QuickCleanup(this IGraphModel self)
        {
            var toRemove = self.EdgeModels.Where(e => e?.ToPort == null || e.FromPort == null).Cast<IGraphElementModel>()
                .Concat(self.NodeModels.Where(m => m.Destroyed))
                .ToList();
            self.DeleteElements(toRemove);
        }

        /// <summary>
        /// Gets a list of subgraph nodes on the current graph that reference the current graph.
        /// </summary>
        /// <returns>A list of subgraph nodes on the current graph that reference the current graph.</returns>
        public static IEnumerable<ISubgraphNodeModel> GetRecursiveSubgraphNodes(this IGraphModel self)
        {
            var recursiveSubgraphNodeModels = new List<ISubgraphNodeModel>();

            var subgraphNodeModels = self.NodeModels.OfType<ISubgraphNodeModel>().ToList();
            if (subgraphNodeModels.Any())
            {
                recursiveSubgraphNodeModels.AddRange(subgraphNodeModels.Where(subgraphNodeModel => subgraphNodeModel.SubgraphModel == self));
            }

            return recursiveSubgraphNodeModels;
        }

        /// <summary>
        /// A version of <see cref="IGraphModel.Name"/> usable in C# scripts.
        /// </summary>
        public static string GetFriendlyScriptName(this IGraphModel graphModel) => graphModel?.Name?.CodifyStringInternal() ?? "";
    }
}
