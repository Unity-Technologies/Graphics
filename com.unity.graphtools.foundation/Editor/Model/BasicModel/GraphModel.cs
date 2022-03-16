using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class GraphModel : IGraphModel, ISerializationCallbackReceiver
    {
        static List<ChangeHint> s_GroupingChangeHint = new List<ChangeHint> { ChangeHint.Grouping };

        [SerializeField, HideInInspector]
        SerializableGUID m_Guid;

        // [SerializeReference]
        GraphAssetModel m_AssetModel;

        [SerializeReference]
        List<INodeModel> m_GraphNodeModels;

        [SerializeField, Obsolete]
        List<EdgeModel> m_EdgeModels;

        [SerializeReference]
        List<IBadgeModel> m_BadgeModels;

        [SerializeReference]
        List<IEdgeModel> m_GraphEdgeModels;

        [SerializeReference, Obsolete]
        List<EdgeModel> m_PolymorphicEdgeModels;

        [SerializeField, Obsolete]
        List<StickyNoteModel> m_StickyNoteModels;

        [SerializeReference]
        List<IStickyNoteModel> m_GraphStickyNoteModels;

        [SerializeField, Obsolete]
        List<PlacematModel> m_PlacematModels;

        [SerializeReference]
        List<IPlacematModel> m_GraphPlacematModels;

        [SerializeReference]
        List<IVariableDeclarationModel> m_GraphVariableModels;

        [SerializeReference]
        List<IDeclarationModel> m_GraphPortalModels;

        [SerializeReference]
        List<ISectionModel> m_SectionModels;

        [SerializeField]
        [FormerlySerializedAs("name")]
        [HideInInspector]
        string m_Name;

        /// <summary>
        /// Holds created variables names to make creation of unique names faster.
        /// </summary>
        HashSet<string> m_ExistingVariableNames;

        [SerializeField]
        [HideInInspector]
        string m_StencilTypeName; // serialized as string, resolved as type by ISerializationCallbackReceiver

        Type m_StencilType;

        // As this field is not serialized, use GetElementsByGuid() to access it.
        Dictionary<SerializableGUID, IGraphElementModel> m_ElementsByGuid;

        PortEdgeIndex m_PortEdgeIndex;

        /// <inheritdoc />
        public virtual SerializableGUID Guid
        {
            get
            {
                if (!m_Guid.Valid)
                    AssignNewGuid();
                return m_Guid;
            }
            set => m_Guid = value;
        }

        /// <inheritdoc />
        public virtual Type DefaultStencilType => null;

        /// <inheritdoc />
        public Type StencilType
        {
            get => m_StencilType;
            set
            {
                if (value == null)
                    value = DefaultStencilType;
                Assert.IsTrue(typeof(IStencil).IsAssignableFrom(value));
                m_StencilType = value;
                Stencil = InstantiateStencil(StencilType);
            }
        }

        /// <inheritdoc />
        public IStencil Stencil { get; private set; }

        /// <inheritdoc />
        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        IEnumerable<IGraphElementModel> IGraphElementContainer.GraphElementModels => GetElementsByGuid().Values.Where(t => t.Container == this);

        /// <inheritdoc />
        public IReadOnlyList<INodeModel> NodeModels => m_GraphNodeModels;

        /// <inheritdoc />
        public IEnumerable<INodeModel> NodeAndBlockModels
        {
            get
            {
                IEnumerable<INodeModel> allModels = NodeModels;
                foreach (var nodeModel in m_GraphNodeModels)
                {
                    if (nodeModel is ContextNodeModel contextModel)
                    {
                        allModels = allModels.Concat(contextModel.GraphElementModels.Cast<IBlockNodeModel>());
                    }
                }

                return allModels;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<IEdgeModel> EdgeModels => m_GraphEdgeModels;
        /// <inheritdoc />
        public IReadOnlyList<IBadgeModel> BadgeModels => m_BadgeModels;

        /// <inheritdoc />
        public IReadOnlyList<IStickyNoteModel> StickyNoteModels => m_GraphStickyNoteModels;

        /// <inheritdoc />
        public IReadOnlyList<IPlacematModel> PlacematModels => m_GraphPlacematModels;

        /// <inheritdoc />
        public IReadOnlyList<IVariableDeclarationModel> VariableDeclarations => m_GraphVariableModels;

        /// <inheritdoc />
        public IReadOnlyList<IDeclarationModel> PortalDeclarations => m_GraphPortalModels;

        /// <inheritdoc />
        public IReadOnlyList<ISectionModel> SectionModels => m_SectionModels;

        /// <inheritdoc />
        public string Name => m_AssetModel ? m_AssetModel.Name : "";


        public virtual Type GetSectionModelType()
        {
            return typeof(SectionModel);
        }

        protected ISectionModel InstantiateSection()
        {
            var section = Instantiate<ISectionModel>(GetSectionModelType());
            return section;
        }

        ISectionModel CreateSection(string sectionName)
        {
            var section = InstantiateSection();
            section.Title = sectionName;
            section.AssetModel = AssetModel;
            return section;
        }

        /// <inheritdoc />
        public ISectionModel GetSectionModel(string sectionName)
        {
            var section = m_SectionModels.Find(t => t.Title == sectionName);

            return section;
        }

        /// <summary>
        /// Checks that all variables are referenced in a group. Otherwise adds the variables in their valid section.
        /// Also cleans up no longer existing sections.
        /// </summary>
        internal void CheckGroupConsistency()
        {
            if (Stencil == null)
                return;
            void RecurseGetReferencedGroupItem<T>(IGroupModel root, HashSet<T> result)
                where T : IGroupItemModel
            {
                foreach (var item in root.Items)
                {
                    if (item is T tItem)
                        result.Add(tItem);
                    if (item is IGroupModel subGroup)
                        RecurseGetReferencedGroupItem(subGroup, result);
                }
            }

            var variablesInGroup = new HashSet<IVariableDeclarationModel>();

            CleanupSections(Stencil.SectionNames);
            foreach (var group in SectionModels)
                RecurseGetReferencedGroupItem(group, variablesInGroup);

            if (VariableDeclarations == null) return;

            foreach (var variable in VariableDeclarations)
            {
                if (!variablesInGroup.Contains(variable))
                    GetSectionModel(Stencil.GetVariableSection(variable)).InsertItem(variable);
            }
        }

        /// <summary>
        /// The index that maps ports to the edges connected to them.
        /// </summary>
        internal PortEdgeIndex PortEdgeIndex => m_PortEdgeIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphModel"/> class.
        /// </summary>
        protected GraphModel()
        {
            AssignNewGuid();

            m_GraphNodeModels = new List<INodeModel>();
            m_GraphEdgeModels = new List<IEdgeModel>();
            m_BadgeModels = new List<IBadgeModel>();
            m_GraphStickyNoteModels = new List<IStickyNoteModel>();
            m_GraphPlacematModels = new List<IPlacematModel>();
            m_GraphVariableModels = new List<IVariableDeclarationModel>();
            m_GraphPortalModels = new List<IDeclarationModel>();
            m_SectionModels = new List<ISectionModel>();
            m_ExistingVariableNames = new HashSet<string>();

            m_PortEdgeIndex = new PortEdgeIndex(this);
        }

        /// <inheritdoc />
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }

        /// <inheritdoc />
        public IReadOnlyList<IEdgeModel> GetEdgesForPort(IPortModel portModel)
        {
            return m_PortEdgeIndex.GetEdgesForPort(portModel);
        }

        /// <summary>
        /// Determines whether two ports can be connected together by an edge.
        /// </summary>
        /// <param name="startPortModel">The port from which the edge would come from.</param>
        /// <param name="compatiblePortModel">The port to which the edge would got to.</param>
        /// <returns>True if the two ports can be connected. False otherwise.</returns>
        protected virtual bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            var startEdgePortalModel = startPortModel.NodeModel as IEdgePortalModel;

            if ((startPortModel.PortDataType == typeof(ExecutionFlow)) != (compatiblePortModel.PortDataType == typeof(ExecutionFlow)))
                return false;

            // No good if ports belong to same node that does not allow self connect
            if (compatiblePortModel == startPortModel ||
                (compatiblePortModel.NodeModel != null || startPortModel.NodeModel != null) &&
                !startPortModel.NodeModel.AllowSelfConnect && compatiblePortModel.NodeModel == startPortModel.NodeModel)
                return false;

            // No good if it's on the same portal either.
            if (compatiblePortModel.NodeModel is IEdgePortalModel edgePortalModel)
            {
                if (edgePortalModel.DeclarationModel.Guid == startEdgePortalModel?.DeclarationModel.Guid)
                    return false;
            }

            // This is true for all ports
            if (compatiblePortModel.Direction == startPortModel.Direction ||
                compatiblePortModel.PortType != startPortModel.PortType)
                return false;

            return Stencil.CanAssignTo(compatiblePortModel.DataTypeHandle, startPortModel.DataTypeHandle);
        }

        /// <inheritdoc />
        public virtual List<IPortModel> GetCompatiblePorts(IReadOnlyList<IPortModel> portModels, IPortModel startPortModel)
        {
            return portModels.Where(pModel =>
            {
                return IsCompatiblePort(startPortModel, pModel);
            })
                .ToList();
        }

        /// <summary>
        /// Returns the dictionary associating a <see cref="IGraphElementModel" /> with its GUID.
        /// </summary>
        /// <returns>the dictionary associating a <see cref="IGraphElementModel" /> with its GUID.</returns>
        protected internal Dictionary<SerializableGUID, IGraphElementModel> GetElementsByGuid()
        {
            if (m_ElementsByGuid == null)
                BuildElementByGuidDictionary();

            return m_ElementsByGuid;
        }

        /// <inheritdoc />
        public void RegisterElement(IGraphElementModel model)
        {
            GetElementsByGuid().Add(model.Guid, model);
        }

        /// <inheritdoc />
        public void UnregisterElement(IGraphElementModel model)
        {
            GetElementsByGuid().Remove(model.Guid);
        }

        /// <inheritdoc />
        public bool TryGetModelFromGuid(SerializableGUID guid, out IGraphElementModel model)
        {
            return GetElementsByGuid().TryGetValue(guid, out model);
        }

        /// <inheritdoc />
        public bool TryGetModelFromGuid<T>(SerializableGUID guid, out T model) where T : class, IGraphElementModel
        {
            var returnValue = GetElementsByGuid().TryGetValue(guid, out var graphElementModel);
            model = graphElementModel as T;
            return returnValue && graphElementModel != null;
        }

        /// <summary>
        /// Adds a node model to the graph.
        /// </summary>
        /// <param name="nodeModel">The node model to add.</param>
        protected void AddNode(INodeModel nodeModel)
        {
            if (nodeModel.NeedsContainer())
                throw new ArgumentException("Can't add a node model which is not AddableToGraph to the graph");
            RegisterElement(nodeModel);
            nodeModel.AssetModel = AssetModel;
            m_GraphNodeModels.Add(nodeModel);
        }

        /// <summary>
        /// Replaces node model at index.
        /// </summary>
        /// <param name="index">Index of the node model in the NodeModels list.</param>
        /// <param name="nodeModel">The new node model.</param>
        protected void ReplaceNode(int index, INodeModel nodeModel)
        {
            GetElementsByGuid().Remove(m_GraphNodeModels[index].Guid);
            GetElementsByGuid().Add(nodeModel.Guid, nodeModel);

            m_GraphNodeModels[index] = nodeModel;
        }

        /// <summary>
        /// Removes a node model from the graph.
        /// </summary>
        /// <param name="nodeModel"></param>
        protected void RemoveNode(INodeModel nodeModel)
        {
            UnregisterElement(nodeModel);

            m_GraphNodeModels.Remove(nodeModel);
        }

        void IGraphElementContainer.RemoveElements(IReadOnlyCollection<IGraphElementModel> elementModels)
        {
            foreach (var element in elementModels)
            {
                switch (element)
                {
                    case IStickyNoteModel stickyNoteModel:
                        RemoveStickyNote(stickyNoteModel);
                        break;
                    case IPlacematModel placematModel:
                        RemovePlacemat(placematModel);
                        break;
                    case IVariableDeclarationModel variableDeclarationModel:
                        RemoveVariableDeclaration(variableDeclarationModel);
                        break;
                    case IEdgeModel edgeModel:
                        RemoveEdge(edgeModel);
                        break;
                    case INodeModel nodeModel:
                        RemoveNode(nodeModel);
                        break;
                    case IBadgeModel badgeModel:
                        RemoveBadge(badgeModel);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds a portal declaration model to the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to add.</param>
        protected void AddPortal(IDeclarationModel declarationModel)
        {
            RegisterElement(declarationModel);
            m_GraphPortalModels.Add(declarationModel);
        }

        /// <summary>
        /// Removes a portal declaration model from the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to remove.</param>
        protected void RemovePortal(IDeclarationModel declarationModel)
        {
            UnregisterElement(declarationModel);
            m_GraphPortalModels.Remove(declarationModel);
        }

        /// <summary>
        /// Adds an edge to the graph.
        /// </summary>
        /// <param name="edgeModel">The edge to add.</param>
        protected void AddEdge(IEdgeModel edgeModel)
        {
            RegisterElement(edgeModel);
            m_GraphEdgeModels.Add(edgeModel);

            m_PortEdgeIndex.AddEdge(edgeModel);
        }

        /// <summary>
        /// Removes an edge from th graph.
        /// </summary>
        /// <param name="edgeModel">The edge to remove.</param>
        protected void RemoveEdge(IEdgeModel edgeModel)
        {
            UnregisterElement(edgeModel);
            m_GraphEdgeModels.Remove(edgeModel);

            m_PortEdgeIndex.RemoveEdge(edgeModel);
        }

        /// <inheritdoc />
        public void AddBadge(IBadgeModel badgeModel)
        {
            RegisterElement(badgeModel);
            m_BadgeModels.Add(badgeModel);
        }

        /// <summary>
        /// Removes a badge from the graph.
        /// </summary>
        /// <param name="badgeModel">The badge to remove.</param>
        public void RemoveBadge(IBadgeModel badgeModel)
        {
            UnregisterElement(badgeModel);
            m_BadgeModels.Remove(badgeModel);
        }

        /// <summary>
        /// Adds a sticky note to the graph.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note to add.</param>
        protected void AddStickyNote(IStickyNoteModel stickyNoteModel)
        {
            RegisterElement(stickyNoteModel);
            m_GraphStickyNoteModels.Add(stickyNoteModel);
        }

        /// <summary>
        /// Removes a sticky note from the graph.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note to remove.</param>
        protected void RemoveStickyNote(IStickyNoteModel stickyNoteModel)
        {
            UnregisterElement(stickyNoteModel);
            m_GraphStickyNoteModels.Remove(stickyNoteModel);
        }

        /// <summary>
        /// Adds a placemat to the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to add.</param>
        protected void AddPlacemat(IPlacematModel placematModel)
        {
            RegisterElement(placematModel);
            m_GraphPlacematModels.Add(placematModel);
        }

        /// <summary>
        /// Removes a placemat from the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to remove.</param>
        protected void RemovePlacemat(IPlacematModel placematModel)
        {
            UnregisterElement(placematModel);
            m_GraphPlacematModels.Remove(placematModel);
        }

        /// <summary>
        /// Adds a variable declaration to the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to add.</param>
        protected void AddVariableDeclaration(IVariableDeclarationModel variableDeclarationModel)
        {
            RegisterElement(variableDeclarationModel);
            m_GraphVariableModels.Add(variableDeclarationModel);
            m_ExistingVariableNames.Add(variableDeclarationModel.Title);
        }

        /// <summary>
        /// Removes a variable declaration from the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to remove.</param>
        protected IGroupItemModel RemoveVariableDeclaration(IVariableDeclarationModel variableDeclarationModel)
        {
            UnregisterElement(variableDeclarationModel);
            m_GraphVariableModels.Remove(variableDeclarationModel);
            m_ExistingVariableNames.Remove(variableDeclarationModel.Title);

            var parent = variableDeclarationModel.ParentGroup;
            parent?.RemoveItem(variableDeclarationModel);
            return parent;
        }

        void RecursiveBuildElementByGuid(IGraphElementModel model)
        {
            m_ElementsByGuid.Add(model.Guid, model);

            if (model is IGraphElementContainer container)
            {
                foreach (var element in container.GraphElementModels)
                    RecursiveBuildElementByGuid(element);
            }
        }

        /// <summary>
        /// Instantiates an object of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to instantiate.</param>
        /// <typeparam name="TInterface">A base type for <paramref name="type"/>.</typeparam>
        /// <returns>A new object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="type"/> does not derive from <typeparamref name="TInterface"/></exception>
        protected TInterface Instantiate<TInterface>(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            TInterface obj;
            if (typeof(TInterface).IsAssignableFrom(type))
                obj = (TInterface)Activator.CreateInstance(type);
            else
                throw new ArgumentOutOfRangeException(nameof(type));

            return obj;
        }

        /// <summary>
        /// Rebuilds the dictionary mapping guids to graph element models.
        /// </summary>
        /// <remarks>
        /// Override this function if your graph models holds new graph elements types.
        /// Ensure that all additional graph element model are added to the guid to model mapping.
        /// </remarks>
        protected virtual void BuildElementByGuidDictionary()
        {
            m_ElementsByGuid = new Dictionary<SerializableGUID, IGraphElementModel>();

            foreach (var model in m_GraphNodeModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_BadgeModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_GraphEdgeModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_GraphStickyNoteModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_GraphPlacematModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_GraphPortalModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_SectionModels)
            {
                RecursiveBuildElementByGuid(model);
            }
        }

        /// <inheritdoc />
        public INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SerializableGUID guid = default, Action<INodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var nodeModel = InstantiateNode(nodeTypeToCreate, nodeName, position, guid, initializationCallback);

            if (!spawnFlags.IsOrphan())
            {
                AddNode(nodeModel);
            }

            return nodeModel;
        }

        /// <summary>
        /// Instantiates a new node.
        /// </summary>
        /// <param name="nodeTypeToCreate">The type of the new node to create.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <returns>The newly created node.</returns>
        protected virtual INodeModel InstantiateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SerializableGUID guid = default, Action<INodeModel> initializationCallback = null)
        {
            if (nodeTypeToCreate == null)
                throw new ArgumentNullException(nameof(nodeTypeToCreate));

            INodeModel nodeModel;
            if (typeof(IConstant).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = new ConstantNodeModel { Value = (IConstant)Activator.CreateInstance(nodeTypeToCreate) };
            else if (typeof(INodeModel).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = (INodeModel)Activator.CreateInstance(nodeTypeToCreate);
            else
                throw new ArgumentOutOfRangeException(nameof(nodeTypeToCreate));

            if (nodeModel is IHasTitle titled)
                titled.Title = nodeName ?? nodeTypeToCreate.Name;

            nodeModel.Position = position;
            if (guid.Valid)
                nodeModel.Guid = guid;
            nodeModel.AssetModel = AssetModel;
            initializationCallback?.Invoke(nodeModel);
            nodeModel.OnCreateNode();

            return nodeModel;
        }

        /// <inheritdoc />
        public virtual IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel,
            Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return this.CreateNode<VariableNodeModel>(declarationModel.DisplayTitle, position, guid, v => v.DeclarationModel = declarationModel, spawnFlags);
        }

        /// <inheritdoc />
        public virtual ISubgraphNodeModel CreateSubgraphNode(IGraphAssetModel referenceGraphAsset, Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            if (referenceGraphAsset.IsContainerGraph())
            {
                Debug.LogWarning("Failed to create the subgraph node. Container graphs cannot be referenced by a subgraph node.");
                return null;
            }
            return this.CreateNode<SubgraphNodeModel>(referenceGraphAsset.Name, position, guid, v => { v.SubgraphAssetModel = referenceGraphAsset; }, spawnFlags);
        }

        /// <inheritdoc />
        public virtual IConstantNodeModel CreateConstantNode(TypeHandle constantTypeHandle, string constantName,
            Vector2 position, SerializableGUID guid = default, Action<IConstantNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var nodeType = Stencil.GetConstantNodeValueType(constantTypeHandle);

            void PreDefineSetup(INodeModel model)
            {
                if (model is IConstantNodeModel constantModel)
                {
                    constantModel.Initialize(constantTypeHandle);
                    initializationCallback?.Invoke(constantModel);
                }
            }

            return (IConstantNodeModel)CreateNode(nodeType, constantName, position, guid, PreDefineSetup, spawnFlags);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteBadges()
        {
            var deletedBadges = new List<IGraphElementModel>(m_BadgeModels);

            foreach (var model in deletedBadges)
            {
                m_ElementsByGuid.Remove(model.Guid);
            }
            m_BadgeModels.Clear();

            return deletedBadges;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteBadgesOfType<T>() where T : IBadgeModel
        {
            var deletedBadges = m_BadgeModels
                .Where(b => b is T)
                .ToList();

            foreach (var model in deletedBadges)
            {
                m_ElementsByGuid.Remove(model.Guid);
            }

            m_BadgeModels = m_BadgeModels
                .Where(b => !(b is T))
                .ToList();

            return deletedBadges;
        }

        /// <inheritdoc />
        public INodeModel DuplicateNode(INodeModel sourceNode, Vector2 delta)
        {
            var pastedNodeModel = sourceNode.Clone();

            // Set graphmodel BEFORE define node as it is commonly use during Define
            pastedNodeModel.AssetModel = AssetModel;
            pastedNodeModel.AssignNewGuid();
            pastedNodeModel.OnDuplicateNode(sourceNode);

            AddNode(pastedNodeModel);
            pastedNodeModel.Position += delta;

            if (pastedNodeModel is IGraphElementContainer container)
            {
                foreach (var element in container.GraphElementModels)
                    RecursivelyRegisterAndAssignNewGuid(element);
            }

            return pastedNodeModel;
        }

        void RecursivelyRegisterAndAssignNewGuid(IGraphElementModel model)
        {
            model.AssignNewGuid();
            GetElementsByGuid()[model.Guid] = model;
            if (model is IGraphElementContainer container)
            {
                foreach (var element in container.GraphElementModels)
                    RecursivelyRegisterAndAssignNewGuid(element);
            }
        }

        public virtual IEdgeModel DuplicateEdge(IEdgeModel sourceEdge, INodeModel targetInputNode, INodeModel targetOutputNode)
        {
            IPortModel inputPortModel = null;
            IPortModel outputPortModel = null;
            if (targetInputNode != null && targetOutputNode != null)
            {
                // Both node were duplicated; create a new edge between the duplicated nodes.
                inputPortModel = (targetInputNode as IInputOutputPortsNodeModel)?.InputsById[sourceEdge.ToPortId];
                outputPortModel = (targetOutputNode as IInputOutputPortsNodeModel)?.OutputsById[sourceEdge.FromPortId];
            }
            else if (targetInputNode != null)
            {
                inputPortModel = (targetInputNode as IInputOutputPortsNodeModel)?.InputsById[sourceEdge.ToPortId];
                outputPortModel = sourceEdge.FromPort.AssetModel == AssetModel ? sourceEdge.FromPort : null;
            }
            else if (targetOutputNode != null)
            {
                inputPortModel = sourceEdge.ToPort.AssetModel == AssetModel ? sourceEdge.ToPort : null;
                outputPortModel = (targetOutputNode as IInputOutputPortsNodeModel)?.OutputsById[sourceEdge.FromPortId];
            }

            if (inputPortModel != null && outputPortModel != null)
            {
                if (inputPortModel.Capacity == PortCapacity.Single && inputPortModel.GetConnectedEdges().Any())
                    return null;
                if (outputPortModel.Capacity == PortCapacity.Single && outputPortModel.GetConnectedEdges().Any())
                    return null;

                return CreateEdge(inputPortModel, outputPortModel);
            }

            return null;
        }

        /// <inheritdoc />
        public IInputOutputPortsNodeModel CreateItemizedNode(int nodeOffset, ref IPortModel outputPortModel)
        {
            if (outputPortModel.IsConnected())
            {
                Vector2 offset = Vector2.up * nodeOffset;
                var nodeToConnect = DuplicateNode(outputPortModel.NodeModel, offset) as IInputOutputPortsNodeModel;
                outputPortModel = nodeToConnect?.OutputsById[outputPortModel.UniqueName];
                return nodeToConnect;
            }
            return null;
        }

        public IReadOnlyCollection<IGraphElementModel> DeleteNodes(IReadOnlyCollection<INodeModel> nodeModels, bool deleteConnections)
        {
            var deletedModels = new List<IGraphElementModel>();

            var nodeByContainer = new Dictionary<IGraphElementContainer, List<IGraphElementModel>>();

            foreach (var nodeModel in nodeModels.Where(n => n.IsDeletable()))
            {
                if (!nodeByContainer.TryGetValue(nodeModel.Container, out List<IGraphElementModel> deletedElements))
                {
                    deletedElements = new List<IGraphElementModel>();
                    nodeByContainer.Add(nodeModel.Container, deletedElements);
                }
                deletedElements.Add(nodeModel);

                deletedModels.Add(nodeModel);

                if (deleteConnections)
                {
                    var connectedEdges = nodeModel.GetConnectedEdges().ToList();
                    deletedModels.AddRange(DeleteEdges(connectedEdges));
                }

                // If this is the last portal with the given declaration, delete the declaration.
                if (nodeModel is EdgePortalModel edgePortalModel &&
                    edgePortalModel.DeclarationModel != null &&
                    this.FindReferencesInGraph<IEdgePortalModel>(edgePortalModel.DeclarationModel).All(n => n == nodeModel))
                {
                    RemovePortal(edgePortalModel.DeclarationModel);
                    deletedModels.Add(edgePortalModel.DeclarationModel);
                }

                nodeModel.Destroy();
            }

            foreach (var container in nodeByContainer)
            {
                container.Key.RemoveElements(container.Value);
            }

            return deletedModels;
        }

        /// <summary>
        /// Returns the type of edge to instantiate between two ports.
        /// </summary>
        /// <param name="toPort">The destination port.</param>
        /// <param name="fromPort">The origin port.</param>
        /// <returns>The edge model type.</returns>
        protected virtual Type GetEdgeType(IPortModel toPort, IPortModel fromPort)
        {
            return typeof(EdgeModel);
        }

        /// <inheritdoc />
        public IEdgeModel CreateEdge(IPortModel toPort, IPortModel fromPort, SerializableGUID guid = default)
        {
            var existing = this.GetEdgeConnectedToPorts(toPort, fromPort);
            if (existing != null)
                return existing;

            var edgeModel = InstantiateEdge(toPort, fromPort, guid);
            AddEdge(edgeModel);
            return edgeModel;
        }

        /// <summary>
        /// Instantiates an edge.
        /// </summary>
        /// <param name="toPort">The port from which the edge originates.</param>
        /// <param name="fromPort">The port to which the edge goes.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created edge</returns>
        protected virtual IEdgeModel InstantiateEdge(IPortModel toPort, IPortModel fromPort, SerializableGUID guid = default)
        {
            var edgeType = GetEdgeType(toPort, fromPort);
            var edgeModel = Instantiate<IEdgeModel>(edgeType);
            edgeModel.AssetModel = AssetModel;
            if (guid.Valid)
                edgeModel.Guid = guid;
            edgeModel.EdgeLabel = "";
            edgeModel.SetPorts(toPort, fromPort);
            return edgeModel;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteEdges(IReadOnlyCollection<IEdgeModel> edgeModels)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var edgeModel in edgeModels.Where(e => e != null && e.IsDeletable()))
            {
                edgeModel.ToPort?.NodeModel?.OnDisconnection(edgeModel.ToPort, edgeModel.FromPort);
                edgeModel.FromPort?.NodeModel?.OnDisconnection(edgeModel.FromPort, edgeModel.ToPort);

                RemoveEdge(edgeModel);
                deletedModels.Add(edgeModel);
            }

            return deletedModels;
        }

        /// <summary>
        /// Returns the type of sticky note to instantiate.
        /// </summary>
        /// <returns>The sticky note model type.</returns>
        protected virtual Type GetStickyNoteType()
        {
            return typeof(StickyNoteModel);
        }

        /// <inheritdoc />
        public IStickyNoteModel CreateStickyNote(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var stickyNoteModel = InstantiateStickyNote(position);
            if (!spawnFlags.IsOrphan())
            {
                AddStickyNote(stickyNoteModel);
            }
            return stickyNoteModel;
        }

        /// <summary>
        /// Instantiates a new sticky note.
        /// </summary>
        /// <param name="position">The position of the sticky note to create.</param>
        /// <returns>The newly created sticky note</returns>
        protected virtual IStickyNoteModel InstantiateStickyNote(Rect position)
        {
            var stickyNoteModelType = GetStickyNoteType();
            var stickyNoteModel = Instantiate<IStickyNoteModel>(stickyNoteModelType);
            stickyNoteModel.PositionAndSize = position;
            stickyNoteModel.AssetModel = AssetModel;
            return stickyNoteModel;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteStickyNotes(IReadOnlyCollection<IStickyNoteModel> stickyNoteModels)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var stickyNoteModel in stickyNoteModels.Where(s => s.IsDeletable()))
            {
                RemoveStickyNote(stickyNoteModel);
                stickyNoteModel.Destroy();
                deletedModels.Add(stickyNoteModel);
            }

            return deletedModels;
        }

        /// <summary>
        /// Returns the type of placemat to instantiate.
        /// </summary>
        /// <returns>The placemat model type.</returns>
        protected virtual Type GetPlacematType()
        {
            return typeof(PlacematModel);
        }

        /// <inheritdoc />
        public IPlacematModel CreatePlacemat(Rect position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var placematModel = InstantiatePlacemat(position, guid);
            if (!spawnFlags.IsOrphan())
            {
                AddPlacemat(placematModel);
            }

            return placematModel;
        }

        /// <summary>
        /// Instantiates a new placemat.
        /// </summary>
        /// <param name="position">The position of the placemat to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created placemat</returns>
        protected virtual IPlacematModel InstantiatePlacemat(Rect position, SerializableGUID guid)
        {
            var placematModelType = GetPlacematType();
            var placematModel = Instantiate<IPlacematModel>(placematModelType);
            placematModel.PositionAndSize = position;
            placematModel.AssetModel = AssetModel;
            if (guid.Valid)
                placematModel.Guid = guid;
            return placematModel;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeletePlacemats(IReadOnlyCollection<IPlacematModel> placematModels)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var placematModel in placematModels.Where(p => p.IsDeletable()))
            {
                RemovePlacemat(placematModel);
                placematModel.Destroy();
                deletedModels.Add(placematModel);
            }

            return deletedModels;
        }

        IStencil InstantiateStencil(Type stencilType)
        {
            Debug.Assert(typeof(IStencil).IsAssignableFrom(stencilType));
            var stencil = (IStencil)Activator.CreateInstance(stencilType);
            Assert.IsNotNull(stencil);
            stencil.GraphModel = this;
            return stencil;
        }

        /// <summary>
        /// Returns the type of variable declaration to instantiate.
        /// </summary>
        /// <returns>The variable declaration model type.</returns>
        protected virtual Type GetDefaultVariableDeclarationType()
        {
            return typeof(VariableDeclarationModel);
        }

        /// <inheritdoc />
        public IVariableDeclarationModel CreateGraphVariableDeclaration(TypeHandle variableDataType, string variableName,
            ModifierFlags modifierFlags, bool isExposed,  IGroupModel group = null, int indexInGroup = int.MaxValue, IConstant initializationModel = null, SerializableGUID guid = default,
            SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return CreateGraphVariableDeclaration(GetDefaultVariableDeclarationType(), variableDataType, variableName,
                modifierFlags, isExposed, group, indexInGroup, initializationModel, guid, InitCallback, spawnFlags);

            void InitCallback(IVariableDeclarationModel variableDeclaration, IConstant initModel)
            {
                if (variableDeclaration is VariableDeclarationModel basicVariableDeclarationModel)
                {
                    basicVariableDeclarationModel.VariableFlags = VariableFlags.None;

                    if (initModel != null) basicVariableDeclarationModel.InitializationModel = initModel;
                }
            }
        }

        /// <inheritdoc />
        public void RenameVariable(IVariableDeclarationModel variable, string expectedNewName)
        {
            m_ExistingVariableNames.Remove(variable.Title);
            var newName = GenerateGraphVariableDeclarationUniqueName(expectedNewName);
            m_ExistingVariableNames.Add(newName);
            variable.Title = newName;
        }

        /// <inheritdoc />
        public IVariableDeclarationModel CreateGraphVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, bool isExposed, IGroupModel group = null, int indexInGroup = int.MaxValue,
            IConstant initializationModel = null, SerializableGUID guid = default, Action<IVariableDeclarationModel, IConstant> initializationCallback = null,
            SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var variableDeclaration = InstantiateVariableDeclaration(variableTypeToCreate, variableDataType,
                variableName, modifierFlags, isExposed, initializationModel, guid, initializationCallback);

            if (initializationModel == null && !spawnFlags.IsOrphan())
                variableDeclaration.CreateInitializationValue();

            if (!spawnFlags.IsOrphan())
                AddVariableDeclaration(variableDeclaration);

            if (group != null)
                group.InsertItem(variableDeclaration, indexInGroup);
            else
            {
                var section = variableDeclaration.GraphModel.GetSectionModel(variableDeclaration.GraphModel.Stencil.GetVariableSection(variableDeclaration));

                section.InsertItem(variableDeclaration, indexInGroup);
            }

            return variableDeclaration;
        }

        /// <inheritdoc />
        public string GenerateGraphVariableDeclarationUniqueName(string originalName)
        {
            originalName = originalName.Trim();

            if (!m_ExistingVariableNames.Contains(originalName))
                return originalName;

            originalName.ExtractBaseNameAndIndex(out var basename, out var _);

#if UNITY_2021_1_OR_NEWER // TODO VladN: remove once we don't support 2020.3 anymore
            // mimicking what GameObject do: don't bother over a certain number
            // (see ObjectNames.GetUniqueName C++ implementation)
            // especially for graph variables using the same name:
            // It's probably not sane to have that much anyway.
            const int maxVarNum = 5000;

            var isParenthesis = EditorSettings.gameObjectNamingScheme == EditorSettings.NamingScheme.SpaceParenthesis;
            var longestVarName = basename.FormatWithNamingScheme(maxVarNum);
            var span = longestVarName.ToArray().AsSpan(); // create a buffer containing the longest variable name
            var intFormat = ("D" + Math.Max(1, EditorSettings.gameObjectNamingDigits)).AsSpan();
            var numIndex = basename.Length + 1 + (isParenthesis ? 1 : 0); // ".", "_" or " ("
            var numSlice = span.Slice(numIndex); // where to blit the number

            // until we find a match, blit the new number in the variable buffer
            // if needed also add the closing parenthesis.
            for (int i = 1; i <= maxVarNum; i++)
            {
                var intStr = i.TryFormat(numSlice, out var intLength, intFormat);
                if (isParenthesis)
                    span[numIndex + intLength] = ')';

                var finalSpan = span.Slice(0, numIndex + intLength + (isParenthesis ? 1 : 0));

                var finalName = finalSpan.ToString();
                if (!m_ExistingVariableNames.Contains(finalName))
                    return finalName;
            }
#else
            var i = 1;
            do
            {
                originalName = originalName.FormatWithNamingScheme(i++);
            } while (m_ExistingVariableNames.Contains(originalName));
#endif
            return originalName;

        }

        /// <summary>
        /// Instantiates a new variable declaration.
        /// </summary>
        /// <param name="variableTypeToCreate">The type of variable to create.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <returns>The newly created variable declaration.</returns>
        protected virtual IVariableDeclarationModel InstantiateVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, bool isExposed,
            IConstant initializationModel, SerializableGUID guid, Action<IVariableDeclarationModel, IConstant> initializationCallback = null)
        {
            var variableDeclaration = Instantiate<IVariableDeclarationModel>(variableTypeToCreate);

            if (guid.Valid)
                variableDeclaration.Guid = guid;
            variableDeclaration.AssetModel = AssetModel;
            variableDeclaration.DataType = variableDataType;
            variableDeclaration.Title = GenerateGraphVariableDeclarationUniqueName(variableName);
            variableDeclaration.IsExposed = isExposed;
            variableDeclaration.Modifiers = modifierFlags;

            initializationCallback?.Invoke(variableDeclaration, initializationModel);

            return variableDeclaration;
        }


        public virtual Type GetGroupModelType()
        {
            return typeof(GroupModel);
        }

        public IGroupModel CreateGroup(string title, IReadOnlyCollection<IGroupItemModel> items = null)
        {
            var group = Instantiate<IGroupModel>(GetGroupModelType());
            group.Title = title;
            group.AssetModel = AssetModel;
            RegisterElement(group);
            if (items != null)
            {
                foreach (var item in items)
                    group.InsertItem(item);
            }

            return group;
        }

        /// <inheritdoc />
        public virtual TDeclType DuplicateGraphVariableDeclaration<TDeclType>(TDeclType sourceModel, bool keepGuid = false)
            where TDeclType : IVariableDeclarationModel
        {
            var uniqueName = sourceModel.Title;
            var copy = sourceModel.Clone();
            if (keepGuid)
                copy.Guid = sourceModel.Guid;
            copy.Title = GenerateGraphVariableDeclarationUniqueName(uniqueName);
            if (copy.InitializationModel != null)
            {
                copy.CreateInitializationValue();
                copy.InitializationModel.ObjectValue = sourceModel.InitializationModel.ObjectValue;
            }

            AddVariableDeclaration(copy);

            if (sourceModel.ParentGroup != null && sourceModel.ParentGroup.GraphModel == this)
                sourceModel.ParentGroup.InsertItem(copy, -1);
            else
            {
                var section = GetSectionModel(Stencil.GetVariableSection(copy));
                section.InsertItem(copy, -1);
            }

            return copy;
        }

        /// <inheritdoc />
        public GraphChangeDescription DeleteVariableDeclarations(IReadOnlyCollection<IVariableDeclarationModel> variableModels, bool deleteUsages = true)
        {
            var changedModelsDict = new Dictionary<IGraphElementModel, IReadOnlyList<ChangeHint>>();
            var deletedModels = new List<IGraphElementModel>();

            foreach (var variableModel in variableModels.Where(v => v.IsDeletable()))
            {
                var parent = RemoveVariableDeclaration(variableModel);

                changedModelsDict[parent] = s_GroupingChangeHint;
                deletedModels.Add(variableModel);

                if (deleteUsages)
                {
                    var nodesToDelete = this.FindReferencesInGraph(variableModel).Cast<INodeModel>().ToList();
                    deletedModels.AddRange(DeleteNodes(nodesToDelete, deleteConnections: true));
                }
            }

            return new GraphChangeDescription(null, changedModelsDict, deletedModels);
        }

        /// <inheritdoc />
        public GraphChangeDescription DeleteGroups(IReadOnlyCollection<IGroupModel> groupModels)
        {
            var changedModelsDict = new Dictionary<IGraphElementModel, IReadOnlyList<ChangeHint>>();
            var deletedModels = new List<IGraphElementModel>();
            var deletedVariables = new List<IVariableDeclarationModel>();

            void RecurseAddVariables(IGroupModel groupModel)
            {
                foreach (var item in groupModel.Items)
                {
                    deletedModels.Add(item);
                    if( item is IVariableDeclarationModel variable)
                        deletedVariables.Add(variable);
                    else if( item is IGroupModel group)
                        RecurseAddVariables(group);
                    item.ParentGroup = null;
                }
            }

            foreach (var groupModel in groupModels.Where(v => v.IsDeletable()))
            {
                if (groupModel.ParentGroup != null)
                {
                    var changedModels = groupModel.ParentGroup.RemoveItem(groupModel);
                    foreach (var changedModel in changedModels)
                    {
                        changedModelsDict[changedModel] = s_GroupingChangeHint;
                    }
                }

                RecurseAddVariables(groupModel);
                deletedModels.Add(groupModel);
            }

            var result = DeleteVariableDeclarations(deletedVariables);
            result.Union(null, changedModelsDict, deletedModels);
            return result;
        }

        /// <summary>
        /// Returns the type of portal to instantiate.
        /// </summary>
        /// <returns>The portal model type.</returns>
        protected virtual Type GetPortalType()
        {
            return typeof(DeclarationModel);
        }

        /// <inheritdoc />
        public IDeclarationModel CreateGraphPortalDeclaration(string portalName, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var decl = InstantiatePortalDeclaration(portalName, guid);

            if (!spawnFlags.IsOrphan())
            {
                AddPortal(decl);
            }

            return decl;
        }

        /// <summary>
        /// Instantiates a new portal model.
        /// </summary>
        /// <param name="portalName">The name of the portal</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created declaration model</returns>
        protected virtual IDeclarationModel InstantiatePortalDeclaration(string portalName, SerializableGUID guid = default)
        {
            var portalModelType = GetPortalType();
            var portalModel = Instantiate<IDeclarationModel>(portalModelType);
            portalModel.Title = portalName;
            if (guid.Valid)
                portalModel.Guid = guid;
            return portalModel;
        }

        /// <inheritdoc />
        public IEdgePortalModel CreateOppositePortal(IEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            EdgePortalModel createdPortal = null;
            Type oppositeType = null;
            switch (edgePortalModel)
            {
                case ExecutionEdgePortalEntryModel _:
                    oppositeType = typeof(ExecutionEdgePortalExitModel);
                    break;
                case ExecutionEdgePortalExitModel _:
                    oppositeType = typeof(ExecutionEdgePortalEntryModel);
                    break;
                case DataEdgePortalEntryModel _:
                    oppositeType = typeof(DataEdgePortalExitModel);
                    break;
                case DataEdgePortalExitModel _:
                    oppositeType = typeof(DataEdgePortalEntryModel);
                    break;
            }

            if (oppositeType != null)
                createdPortal = (EdgePortalModel)CreateNode(oppositeType, edgePortalModel.Title, position, spawnFlags: spawnFlags, initializationCallback: n => ((EdgePortalModel)n).PortDataTypeHandle = edgePortalModel.PortDataTypeHandle);

            if (createdPortal != null)
                createdPortal.DeclarationModel = edgePortalModel.DeclarationModel;

            return createdPortal;
        }

        /// <inheritdoc />
        public IEdgePortalEntryModel CreateEntryPortalFromEdge(IEdgeModel edgeModel)
        {
            var outputPortModel = edgeModel.FromPort;
            if (outputPortModel.PortType == PortType.Execution)
                return this.CreateNode<ExecutionEdgePortalEntryModel>();

            return this.CreateNode<DataEdgePortalEntryModel>(initializationCallback: n => n.PortDataTypeHandle = outputPortModel.DataTypeHandle);
        }

        /// <inheritdoc />
        public IEdgePortalExitModel CreateExitPortalFromEdge(IEdgeModel edgeModel)
        {
            var inputPortModel = edgeModel.ToPort;
            if (inputPortModel.PortType == PortType.Execution)
                return this.CreateNode<ExecutionEdgePortalExitModel>();

            return this.CreateNode<DataEdgePortalExitModel>(initializationCallback: n => n.PortDataTypeHandle = inputPortModel.DataTypeHandle);
        }

        /// <inheritdoc />
        public virtual void OnEnable()
        {
            if (m_GraphEdgeModels == null)
                m_GraphEdgeModels = new List<IEdgeModel>();

            if (m_GraphStickyNoteModels == null)
                m_GraphStickyNoteModels = new List<IStickyNoteModel>();

            if (m_GraphPlacematModels == null)
                m_GraphPlacematModels = new List<IPlacematModel>();

#pragma warning disable 612
            // Serialized data conversion code
            if (m_EdgeModels?.Count > 0)
            {
                m_GraphEdgeModels.AddRange(m_EdgeModels);
                m_EdgeModels = null;

                m_PortEdgeIndex.MarkDirty();
            }

            if (m_PolymorphicEdgeModels?.Count > 0)
            {
                m_GraphEdgeModels.AddRange(m_PolymorphicEdgeModels);
                m_PolymorphicEdgeModels = null;

                m_PortEdgeIndex.MarkDirty();
            }

            // Serialized data conversion code
            if (m_StickyNoteModels != null)
            {
                m_GraphStickyNoteModels.AddRange(m_StickyNoteModels);
                m_StickyNoteModels = null;
            }

            // Serialized data conversion code
            if (m_PlacematModels != null)
            {
                m_GraphPlacematModels.AddRange(m_PlacematModels);
                m_PlacematModels = null;
            }
#pragma warning restore 612

            if (m_GraphNodeModels == null)
                m_GraphNodeModels = new List<INodeModel>();

            foreach (var model in NodeModels)
            {
                if (model is null)
                    continue;
                model.AssetModel = AssetModel;
            }

            foreach (var nodeModel in NodeModels)
            {
                (nodeModel as NodeModel)?.DefineNode();
            }

            foreach (var edge in m_GraphEdgeModels)
            {
                (edge as EdgeModel)?.InitAssetModel(this.AssetModel);
            }

            MigrateNodes();

            CheckGroupConsistency();
        }

        /// <summary>
        /// Callback to migrate nodes from an old asset to the new models.
        /// </summary>
        protected virtual void MigrateNodes()
        {
        }

        /// <inheritdoc />
        public void OnDisable()
        {
        }

        /// <inheritdoc />
        public void UndoRedoPerformed()
        {
            OnEnable();
            foreach (var edgeModel in EdgeModels.OfType<EdgeModel>())
            {
                edgeModel.ResetPortCache();
            }
        }

        /// <inheritdoc />
        public void OnLoadGraphAsset()
        {
            foreach (var nodeModel in NodeModels.OfType<NodeModel>())
                nodeModel.DefineNode();

            foreach (var edgeModel in EdgeModels.OfType<EdgeModel>())
            {
                edgeModel.UpdatePortFromCache();
                edgeModel.ResetPortCache();
            }
        }

        /// <inheritdoc />
        public virtual bool CheckIntegrity(Verbosity errors)
        {
            return GraphModelExtensions.CheckIntegrity(this, errors);
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
            if (StencilType != null)
                m_StencilTypeName = StencilType.AssemblyQualifiedName;
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_StencilTypeName))
                StencilType = Type.GetType(m_StencilTypeName) ?? DefaultStencilType;

#if UNITY_2021_1_OR_NEWER // TODO VladN: remove once we don't support 2020.3 anymore
            m_ExistingVariableNames = new HashSet<string>(VariableDeclarations.Count);
#else
            m_ExistingVariableNames = new HashSet<string>();
#endif
            foreach (var declarationModel in VariableDeclarations)
            {
                if (declarationModel != null) // in case of bad serialized graph - breaks a test if not tested
                    m_ExistingVariableNames.Add(declarationModel.Title);
            }
        }

        /// <summary>
        /// Cleans the stored sections based on the the given section names.
        /// </summary>
        /// <param name="sectionNames">The section that should exist.</param>
        internal virtual void CleanupSections(IEnumerable<string> sectionNames)
        {
            if (m_SectionModels == null)
                m_SectionModels = new List<ISectionModel>();
            HashSet<string> sectionHash = new HashSet<string>(sectionNames);
            foreach (var section in m_SectionModels.ToList())
            {
                if (!sectionHash.Contains(section.Title))
                    m_SectionModels.Remove(section);
            }

            foreach (var sectionName in sectionNames)
            {
                if (m_SectionModels.All(t => t.Title != sectionName))
                {
                    var section = CreateSection(sectionName);
                    section.AssetModel = AssetModel;
                    m_SectionModels.Add(section);
                }
            }
        }

        /// <inheritdoc />
        public virtual void CloneGraph(IGraphModel sourceGraphModel)
        {
            m_GraphNodeModels = new List<INodeModel>();
            m_GraphEdgeModels = new List<IEdgeModel>();
            m_GraphStickyNoteModels = new List<IStickyNoteModel>();
            m_GraphPlacematModels = new List<IPlacematModel>();
            m_GraphVariableModels = new List<IVariableDeclarationModel>();
            m_GraphPortalModels = new List<IDeclarationModel>();

            var elementMapping = new Dictionary<string, IGraphElementModel>();
            var nodeMapping = new Dictionary<INodeModel, INodeModel>();
            var variableMapping = new Dictionary<IVariableDeclarationModel, IVariableDeclarationModel>();

            if (sourceGraphModel.VariableDeclarations.Any())
            {
                List<IVariableDeclarationModel> variableDeclarationModels =
                    sourceGraphModel.VariableDeclarations.ToList();

                foreach (var sourceModel in variableDeclarationModels)
                {
                    var copy = DuplicateGraphVariableDeclaration(sourceModel);
                    variableMapping.Add(sourceModel, copy);
                }
            }

            foreach (var sourceNode in sourceGraphModel.NodeModels)
            {
                var pastedNode = DuplicateNode(sourceNode, Vector2.zero);
                nodeMapping[sourceNode] = pastedNode;
            }

            foreach (var nodeModel in nodeMapping)
            {
                elementMapping.Add(nodeModel.Key.Guid.ToString(), nodeModel.Value);
            }

            foreach (var sourceEdge in sourceGraphModel.EdgeModels)
            {
                elementMapping.TryGetValue(sourceEdge.ToNodeGuid.ToString(), out var newInput);
                elementMapping.TryGetValue(sourceEdge.FromNodeGuid.ToString(), out var newOutput);

                DuplicateEdge(sourceEdge, newInput as INodeModel, newOutput as INodeModel);
                elementMapping.Add(sourceEdge.Guid.ToString(), sourceEdge);
            }

            foreach (var sourceVariableNode in sourceGraphModel.NodeModels.Where(model => model is VariableNodeModel))
            {
                elementMapping.TryGetValue(sourceVariableNode.Guid.ToString(), out var newNode);

                if (newNode != null)
                    ((VariableNodeModel)newNode).DeclarationModel =
                        variableMapping[((VariableNodeModel)sourceVariableNode).VariableDeclarationModel];
            }

            foreach (var stickyNote in sourceGraphModel.StickyNoteModels)
            {
                var newPosition = new Rect(stickyNote.PositionAndSize.position, stickyNote.PositionAndSize.size);
                var pastedStickyNote = (StickyNoteModel)CreateStickyNote(newPosition);
                pastedStickyNote.Title = stickyNote.Title;
                pastedStickyNote.Contents = stickyNote.Contents;
                pastedStickyNote.Theme = stickyNote.Theme;
                pastedStickyNote.TextSize = stickyNote.TextSize;
                elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
            }

            List<PlacematModel> pastedPlacemats = new List<PlacematModel>();
            foreach (var placemat in sourceGraphModel.PlacematModels)
            {
                var newPosition = new Rect(placemat.PositionAndSize.position, placemat.PositionAndSize.size);
                var pastedPlacemat = (PlacematModel)CreatePlacemat(newPosition);
                pastedPlacemat.Title = placemat.Title;
                pastedPlacemat.Color = placemat.Color;
                pastedPlacemat.Collapsed = placemat.Collapsed;
                pastedPlacemat.HiddenElementsGuid = ((PlacematModel)placemat).HiddenElementsGuid;
                pastedPlacemats.Add(pastedPlacemat);
                elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
            }

            // Update hidden content to new node ids.
            foreach (var pastedPlacemat in pastedPlacemats)
            {
                if (pastedPlacemat.Collapsed)
                {
                    List<string> pastedHiddenContent = new List<string>();
                    foreach (var guid in pastedPlacemat.HiddenElementsGuid)
                    {
                        if (elementMapping.TryGetValue(guid, out IGraphElementModel pastedElement))
                        {
                            pastedHiddenContent.Add(pastedElement.Guid.ToString());
                        }
                    }

                    pastedPlacemat.HiddenElementsGuid = pastedHiddenContent;
                }
            }
        }

        /// <inheritdoc />
        public virtual void Repair()
        {
            m_GraphNodeModels.RemoveAll(t => t == null);
            m_GraphNodeModels.RemoveAll(t => t is IVariableNodeModel variable && variable.DeclarationModel == null);

            foreach (var container in m_GraphNodeModels.OfType<IGraphElementContainer>())
            {
                container.Repair();
            }

            var validGuids = new HashSet<SerializableGUID>(m_GraphNodeModels.Select(t=>t.Guid));

            m_BadgeModels.RemoveAll(t => t == null);
            m_BadgeModels.RemoveAll(t => t.ParentModel == null);
            m_GraphEdgeModels.RemoveAll(t => t == null);
            m_GraphEdgeModels.RemoveAll(t => ! validGuids.Contains(t.FromNodeGuid) || ! validGuids.Contains(t.ToNodeGuid));
            m_GraphStickyNoteModels.RemoveAll(t => t == null);
            m_GraphPlacematModels.RemoveAll(t => t == null);
            m_GraphVariableModels.RemoveAll(t=>t == null);
            m_GraphPortalModels.RemoveAll(t => t == null);
            m_SectionModels.ForEach(t => t.Repair());
        }
    }
}
