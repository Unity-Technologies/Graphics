using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Verbosity of <see cref="GraphModelExtensions.CheckIntegrity"/>.
    /// </summary>
    public enum Verbosity
    {
        Errors,
        Verbose
    }

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
        /// <param name="indexInGroup">THe index of the variable in the group.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <typeparam name="TDeclType">The type of variable declaration to create.</typeparam>
        /// <returns>The newly created variable declaration.</returns>
        public static TDeclType CreateGraphVariableDeclaration<TDeclType>(this IGraphModel self, TypeHandle variableDataType,
            string variableName, ModifierFlags modifierFlags, bool isExposed, IGroupModel group = null, int indexInGroup = -1, IConstant initializationModel = null,
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

        public static IReadOnlyCollection<IGraphElementModel> DeleteVariableDeclaration(this IGraphModel self,
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

        static void FlattenGraphElementList(IEnumerable<IGraphElementModel> graphElementModels, List<IGraphElementModel> result)
        {
            foreach (var element in graphElementModels)
            {
                result.Add(element);
                if (element is IGraphElementContainer container)
                    FlattenGraphElementList(container.GraphElementModels, result);
            }
        }

        struct ElementsByType
        {
            public HashSet<IStickyNoteModel> stickyNoteModels;
            public HashSet<IPlacematModel> placematModels;
            public HashSet<IVariableDeclarationModel> variableDeclarationsModels;
            public HashSet<IGroupModel> variableGroupModels;
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
                    case IGroupModel variableGroupModel:
                        elementsByType.variableGroupModels.Add(variableGroupModel);
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

        public static IEnumerable<IGraphElementModel> DeleteElements(this IGraphModel self,
            IReadOnlyCollection<IGraphElementModel> graphElementModels)
        {
            ElementsByType elementsByType;

            elementsByType.stickyNoteModels = new HashSet<IStickyNoteModel>();
            elementsByType.placematModels = new HashSet<IPlacematModel>();
            elementsByType.variableDeclarationsModels = new HashSet<IVariableDeclarationModel>();
            elementsByType.variableGroupModels = new HashSet<IGroupModel>();
            elementsByType.edgeModels = new HashSet<IEdgeModel>();
            elementsByType.nodeModels = new HashSet<INodeModel>();

            RecursiveSortElements(ref elementsByType, graphElementModels);

            // Add nodes that would be backed by declaration models.
            elementsByType.nodeModels.AddRangeInternal(elementsByType.variableDeclarationsModels.SelectMany(d => self.FindReferencesInGraph<IHasDeclarationModel>(d).OfType<INodeModel>()));

            // Add edges connected to the deleted nodes.
            foreach (var portModel in elementsByType.nodeModels.OfType<IPortNodeModel>().SelectMany(n => n.Ports))
                elementsByType.edgeModels.AddRangeInternal(self.EdgeModels.Where(e => e.ToPort == portModel || e.FromPort == portModel));

            return self.DeleteStickyNotes(elementsByType.stickyNoteModels)
                .Concat(self.DeletePlacemats(elementsByType.placematModels))
                .Concat(self.DeleteEdges(elementsByType.edgeModels))
                .Concat(self.DeleteVariableDeclarations(elementsByType.variableDeclarationsModels, deleteUsages: false))
                .Concat(self.DeleteVariableGroups(elementsByType.variableGroupModels))
                .Concat(self.DeleteNodes(elementsByType.nodeModels, deleteConnections: false)).ToList();
        }

        public static IReadOnlyList<T> GetListOf<T>(this IGraphModel self) where T : IGraphElementModel
        {
            switch (typeof(T))
            {
                case Type x when x == typeof(INodeModel):
                    return (IReadOnlyList<T>)self.NodeModels;

                case Type x when x == typeof(IEdgeModel):
                    return (IReadOnlyList<T>)self.EdgeModels;

                case Type x when x == typeof(IStickyNoteModel):
                    return (IReadOnlyList<T>)self.StickyNoteModels;

                case Type x when x == typeof(IPlacematModel):
                    return (IReadOnlyList<T>)self.PlacematModels;

                case Type x when x == typeof(IVariableDeclarationModel):
                    return (IReadOnlyList<T>)self.VariableDeclarations;

                case Type x when x == typeof(IDeclarationModel):
                    return (IReadOnlyList<T>)self.PortalDeclarations;

                default:
                    throw new ArgumentException($"{typeof(T).Name} isn't a supported type of graph element");
            }
        }

        /// <summary>
        /// Move graph element models forward.
        /// </summary>
        /// <param name="self">The extended GraphModel.</param>
        /// <param name="models">The models to move.</param>
        /// <param name="toTop">Whether to move the model all the way to the top of the list.</param>
        /// <typeparam name="T">The type of graph element model to move.</typeparam>
        /// <remarks>
        /// Trying to move forward an element already at the top of the list will do nothing.
        /// Also note that the order in which the models are passed in <param name="models"/> has no influence in the
        /// order the elements will end up in the graph model. The order in the graph model will be preserved.
        /// e.g. with
        /// `GraphModel.listOfItems = [1, 2, 3, 4, 5]` and `models = [3, 2, 1]`, `MoveForward` will result in `GraphModel.listOfItems = [4, 1, 2, 3, 5]`,
        /// same as if `models = [1, 2, 3]`.
        /// </remarks>
        public static void MoveForward<T>(this IGraphModel self, IReadOnlyList<T> models, bool toTop = false) where T : class, IGraphElementModel
        {
            if (models == null || models.Count == 0)
                return;

            var list = (List<T>)self.GetListOf<T>();

            // Pointless trying to move in list with one element or less.
            if (list.Count <= 1)
                return;

            var nextTopIdx = list.Count - 1;

            for (var i = list.Count - 2; i >= 0; i--)
            {
                if (!models.Contains(list[i]))
                    continue;

                var moveToIdx = i + 1;
                if (toTop)
                {
                    while (models.Contains(list[nextTopIdx]) && nextTopIdx > i)
                        nextTopIdx--;

                    if (nextTopIdx <= i)
                        continue;

                    moveToIdx = nextTopIdx;
                }
                else
                {
                    if (models.Contains(list[moveToIdx]))
                        continue;
                }

                var element = list[i];
                list.RemoveAt(i);
                list.Insert(moveToIdx, element);
            }
        }

        /// <summary>
        /// Move graph element models backward.
        /// </summary>
        /// <param name="self">The extended GraphModel.</param>
        /// <param name="models">The models to move.</param>
        /// <param name="toBottom">Whether to move the model all the way to the bottom of the list.</param>
        /// <typeparam name="T">The type of graph element model to move.</typeparam>
        /// <remarks>
        /// Trying to move backward an element already at the bottom of the list will do nothing.
        /// Also note that the order in which the models are passed in <param name="models"/> has no influence in the
        /// order the elements will end up in the graph model. The order in the graph model will be preserved.
        /// e.g. with
        /// `GraphModel.listOfItems = [1, 2, 3, 4, 5]` and `models = [5, 4, 3]`, `MoveBackward` will result in `GraphModel.listOfItems = [1, 3, 4, 5, 2]`,
        /// same as if `models = [3, 4, 5]`.
        /// </remarks>
        public static void MoveBackward<T>(this IGraphModel self, IReadOnlyList<T> models, bool toBottom = false) where T : class, IGraphElementModel
        {
            if (models == null || models.Count == 0)
                return;

            var list = (List<T>)self.GetListOf<T>();

            // Pointless trying to move in list with one element or less.
            if (list.Count <= 1)
                return;

            var nextBottomIdx = 0;

            for (var i = 1; i < list.Count; i++)
            {
                if (!models.Contains(list[i]))
                    continue;

                var moveToIdx = i - 1;
                if (toBottom)
                {
                    while (models.Contains(list[nextBottomIdx]) && nextBottomIdx < i)
                        nextBottomIdx++;

                    if (nextBottomIdx >= i)
                        continue;

                    moveToIdx = nextBottomIdx;
                }
                else
                {
                    if (models.Contains(list[moveToIdx]))
                        continue;
                }

                var element = list[i];
                list.RemoveAt(i);
                list.Insert(moveToIdx, element);
            }
        }

        public static void MoveBefore<T>(this IGraphModel self, IReadOnlyList<T> models, T insertBefore) where T : class, IGraphElementModel
        {
            var list = (List<T>)self.GetListOf<T>();

            if (insertBefore != null)
            {
                var insertBeforeIndex = list.IndexOf(insertBefore);
                while (insertBeforeIndex < list.Count && models.Contains(list[insertBeforeIndex]))
                {
                    insertBeforeIndex++;
                }

                if (insertBeforeIndex < list.Count)
                    insertBefore = list[insertBeforeIndex];
                else
                    insertBefore = null;
            }

            foreach (var model in models)
            {
                list.Remove(model);
            }

            var insertionIndex = list.Count;
            if (insertBefore != null)
                insertionIndex = list.IndexOf(insertBefore);

            foreach (var model in models)
            {
                list.Insert(insertionIndex++, model);
            }
        }

        public static void MoveAfter<T>(this IGraphModel self, IReadOnlyList<T> models, T insertAfter) where T : class, IGraphElementModel
        {
            var list = (List<T>)self.GetListOf<T>();

            if (insertAfter != null)
            {
                var insertAfterIndex = list.IndexOf(insertAfter);
                while (insertAfterIndex >= 0 && models.Contains(list[insertAfterIndex]))
                {
                    insertAfterIndex--;
                }

                if (insertAfterIndex >= 0)
                    insertAfter = list[insertAfterIndex];
                else
                    insertAfter = null;
            }

            foreach (var model in models)
            {
                list.Remove(model);
            }

            var insertionIndex = 0;
            if (insertAfter != null)
                insertionIndex = list.IndexOf(insertAfter) + 1;

            foreach (var model in models)
            {
                list.Insert(insertionIndex++, model);
            }
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

        /// <summary>
        /// Get the smallest Z order for the placemats in the graph.
        /// </summary>
        /// <returns>The smallest Z order for the placemats in the graph; 0 if the graph has no placemats.</returns>
        [Obsolete("Placemats are always sorted now. This method always returns 0")]
        public static int GetPlacematMinZOrder(this IGraphModel self)
        {
            return 0;
        }

        /// <summary>
        /// Get the largest Z order for the placemats in the graph.
        /// </summary>
        /// <returns>The largest Z order for the placemats in the graph; 0 if the graph has no placemats.</returns>
        [Obsolete("Placemats are always sorted now. This method always returns GraphModel.PlacematModels.Count - 1")]
        public static int GetPlacematMaxZOrder(this IGraphModel self)
        {
            return self.PlacematModels.Count - 1;
        }

        /// <summary>
        /// Get a list of placemats sorted by their Z order.
        /// </summary>
        /// <returns>A list of placemats sorted by their Z order.</returns>
        [Obsolete("Placemats are always sorted now. Always returns GraphModel.PlacematModels")]
        public static IReadOnlyList<IPlacematModel> GetSortedPlacematModels(this IGraphModel self)
        {
            return self.PlacematModels;
        }

        public static void QuickCleanup(this IGraphModel self)
        {
            var toRemove = self.EdgeModels.Where(e => e?.ToPort == null || e.FromPort == null).Cast<IGraphElementModel>()
                .Concat(self.NodeModels.Where(m => m.Destroyed))
                .ToList();
            self.DeleteElements(toRemove);
        }

        public static void Repair(this IGraphModel self)
        {
            var toRemove = self.NodeModels.Where(n => n == null).Cast<IGraphElementModel>()
                .Concat(self.StickyNoteModels.Where(s => s == null))
                .Concat(self.PlacematModels.Where(p => p == null))
                .Concat(self.EdgeModels.Where(e => e?.ToPort == null || e.FromPort == null))
                .ToList();
            self.DeleteElements(toRemove);
        }

        public static bool CheckIntegrity(this IGraphModel self, Verbosity errors)
        {
            Assert.IsTrue((Object)self.AssetModel, "graph asset is invalid");
            bool failed = false;
            for (var i = 0; i < self.EdgeModels.Count; i++)
            {
                var edge = self.EdgeModels[i];
                if (edge.ToPort == null)
                {
                    failed = true;
                    Debug.Log($"Edge {i} toPort is null, output: {edge.FromPort}");
                }

                if (edge.FromPort == null)
                {
                    failed = true;
                    Debug.Log($"Edge {i} output is null, toPort: {edge.ToPort}");
                }
            }

            self.CheckNodeList();
            if (!failed && errors == Verbosity.Verbose)
                Debug.Log("Integrity check succeeded");
            return !failed;
        }

        static void CheckNodeList(this IGraphModel self)
        {
            var existingGuids = new Dictionary<SerializableGUID, int>(self.NodeModels.Count * 4); // wild guess of total number of nodes, including stacked nodes
            for (var i = 0; i < self.NodeModels.Count; i++)
            {
                INodeModel node = self.NodeModels[i];

                Assert.IsTrue(node.GraphModel != null, $"Node {i} {node} graph is null");
                Assert.IsTrue(node.AssetModel != null, $"Node {i} {node} asset is null");
                Assert.IsNotNull(node, $"Node {i} is null");
                Assert.IsTrue(self.AssetModel.GetHashCode() == node.AssetModel?.GetHashCode(), $"Node {i} asset is not matching its actual asset");
                Assert.IsFalse(!node.Guid.Valid, $"Node {i} ({node.GetType()}) has an empty Guid");
                Assert.IsFalse(existingGuids.TryGetValue(node.Guid, out var oldIndex), $"duplicate GUIDs: Node {i} ({node.GetType()}) and Node {oldIndex} have the same guid {node.Guid}");
                existingGuids.Add(node.Guid, i);

                if (node.Destroyed)
                    continue;

                if (node is IInputOutputPortsNodeModel portHolder)
                {
                    CheckNodePorts(portHolder.InputsById);
                    CheckNodePorts(portHolder.OutputsById);
                }

                if (node is IVariableNodeModel variableNode && variableNode.DeclarationModel != null)
                {
                    var originalDeclarations = self.VariableDeclarations.Where(d => d.Guid == variableNode.DeclarationModel.Guid).ToList();
                    Assert.IsTrue(originalDeclarations.Count <= 1);
                    var originalDeclaration = originalDeclarations.SingleOrDefault();
                    Assert.IsNotNull(originalDeclaration, $"Variable Node {i} {variableNode.Title} has a declaration model, but it was not present in the graph's variable declaration list");
                    Assert.IsTrue(ReferenceEquals(originalDeclaration, variableNode.DeclarationModel), $"Variable Node {i} {variableNode.Title} has a declaration model that was not ReferenceEquals() to the matching one in the graph");
                }
            }
        }

        static void CheckNodePorts(IReadOnlyDictionary<string, IPortModel> portsById)
        {
            foreach (var kv in portsById)
            {
                string portId = portsById[kv.Key].UniqueName;
                Assert.AreEqual(kv.Key, portId, $"Node {kv.Key} port and its actual id {portId} mismatch");
            }
        }
    }
}
