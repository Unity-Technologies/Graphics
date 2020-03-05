using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Internal;
using Edge = UnityEditor.Graphing.Edge;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.MaterialGraph")]
    [FormerName("UnityEditor.ShaderGraph.SubGraph")]
    [FormerName("UnityEditor.ShaderGraph.AbstractMaterialGraph")]
    sealed class GraphData : ISerializationCallbackReceiver
    {
        public GraphObject owner { get; set; }

        #region Input data

        [NonSerialized]
        List<AbstractShaderProperty> m_Properties = new List<AbstractShaderProperty>();

        public IEnumerable<AbstractShaderProperty> properties
        {
            get { return m_Properties; }
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        List<ShaderKeyword> m_Keywords = new List<ShaderKeyword>();

        public IEnumerable<ShaderKeyword> keywords
        {
            get { return m_Keywords; }
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedKeywords = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        List<ShaderInput> m_AddedInputs = new List<ShaderInput>();

        public IEnumerable<ShaderInput> addedInputs
        {
            get { return m_AddedInputs; }
        }

        [NonSerialized]
        List<Guid> m_RemovedInputs = new List<Guid>();

        public IEnumerable<Guid> removedInputs
        {
            get { return m_RemovedInputs; }
        }

        [NonSerialized]
        List<ShaderInput> m_MovedInputs = new List<ShaderInput>();

        public IEnumerable<ShaderInput> movedInputs
        {
            get { return m_MovedInputs; }
        }

        public string assetGuid { get; set; }

        #endregion

        #region Node data

        [NonSerialized]
        Stack<Identifier> m_FreeNodeTempIds = new Stack<Identifier>();

        [NonSerialized]
        List<AbstractMaterialNode> m_Nodes = new List<AbstractMaterialNode>();

        [NonSerialized]
        Dictionary<Guid, AbstractMaterialNode> m_NodeDictionary = new Dictionary<Guid, AbstractMaterialNode>();

        public IEnumerable<T> GetNodes<T>()
        {
            return m_Nodes.Where(x => x != null).OfType<T>();
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        List<AbstractMaterialNode> m_AddedNodes = new List<AbstractMaterialNode>();

        public IEnumerable<AbstractMaterialNode> addedNodes
        {
            get { return m_AddedNodes; }
        }

        [NonSerialized]
        List<AbstractMaterialNode> m_RemovedNodes = new List<AbstractMaterialNode>();

        public IEnumerable<AbstractMaterialNode> removedNodes
        {
            get { return m_RemovedNodes; }
        }

        [NonSerialized]
        List<AbstractMaterialNode> m_PastedNodes = new List<AbstractMaterialNode>();

        public IEnumerable<AbstractMaterialNode> pastedNodes
        {
            get { return m_PastedNodes; }
        }
        #endregion

        #region Group Data

        [SerializeField]
        List<GroupData> m_Groups = new List<GroupData>();

        public IEnumerable<GroupData> groups
        {
            get { return m_Groups; }
        }

        [NonSerialized]
        List<GroupData> m_AddedGroups = new List<GroupData>();

        public IEnumerable<GroupData> addedGroups
        {
            get { return m_AddedGroups; }
        }

        [NonSerialized]
        List<GroupData> m_RemovedGroups = new List<GroupData>();

        public IEnumerable<GroupData> removedGroups
        {
            get { return m_RemovedGroups; }
        }

        [NonSerialized]
        List<GroupData> m_PastedGroups = new List<GroupData>();

        public IEnumerable<GroupData> pastedGroups
        {
            get { return m_PastedGroups; }
        }

        [NonSerialized]
        List<ParentGroupChange> m_ParentGroupChanges = new List<ParentGroupChange>();

        public IEnumerable<ParentGroupChange> parentGroupChanges
        {
            get { return m_ParentGroupChanges; }
        }

        [NonSerialized]
        GroupData m_MostRecentlyCreatedGroup;

        public GroupData mostRecentlyCreatedGroup => m_MostRecentlyCreatedGroup;

        [NonSerialized]
        Dictionary<Guid, List<IGroupItem>> m_GroupItems = new Dictionary<Guid, List<IGroupItem>>();

        public IEnumerable<IGroupItem> GetItemsInGroup(GroupData groupData)
        {
            if (m_GroupItems.TryGetValue(groupData.guid, out var nodes))
            {
                return nodes;
            }
            return Enumerable.Empty<IGroupItem>();
        }

        #endregion

        #region StickyNote Data
        [SerializeField]
        List<StickyNoteData> m_StickyNotes = new List<StickyNoteData>();

        public IEnumerable<StickyNoteData> stickyNotes => m_StickyNotes;

        [NonSerialized]
        List<StickyNoteData> m_AddedStickyNotes = new List<StickyNoteData>();

        public List<StickyNoteData> addedStickyNotes => m_AddedStickyNotes;

        [NonSerialized]
        List<StickyNoteData> m_RemovedNotes = new List<StickyNoteData>();

        public IEnumerable<StickyNoteData> removedNotes => m_RemovedNotes;

        [NonSerialized]
        List<StickyNoteData> m_PastedStickyNotes = new List<StickyNoteData>();

        public IEnumerable<StickyNoteData> pastedStickyNotes => m_PastedStickyNotes;

        #endregion

        #region Edge data

        [NonSerialized]
        List<Edge> m_Edges = new List<Edge>();

        public IEnumerable<Edge> edges
        {
            get { return m_Edges; }
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        Dictionary<Guid, List<IEdge>> m_NodeEdges = new Dictionary<Guid, List<IEdge>>();

        [NonSerialized]
        List<IEdge> m_AddedEdges = new List<IEdge>();

        public IEnumerable<IEdge> addedEdges
        {
            get { return m_AddedEdges; }
        }

        [NonSerialized]
        List<IEdge> m_RemovedEdges = new List<IEdge>();

        public IEnumerable<IEdge> removedEdges
        {
            get { return m_RemovedEdges; }
        }

        #endregion

        #region Context Data

        [SerializeField]
        ContextData m_VertexContext;

        [SerializeField]
        ContextData m_FragmentContext;

        // We build this once and cache it as it uses reflection
        // This list is used to build the Create Node menu entries for Blocks
        // as well as when deserializing descriptor fields on serialized Blocks
        [NonSerialized]
        List<BlockFieldDescriptor> m_BlockFieldDescriptors;

        public ContextData vertexContext => m_VertexContext;
        public ContextData fragmentContext => m_FragmentContext;
        public List<BlockFieldDescriptor> blockFieldDescriptors => m_BlockFieldDescriptors;

        #endregion

        [SerializeField]
        InspectorPreviewData m_PreviewData = new InspectorPreviewData();

        public InspectorPreviewData previewData
        {
            get { return m_PreviewData; }
            set { m_PreviewData = value; }
        }

        [SerializeField]
        string m_Path;

        public string path
        {
            get { return m_Path; }
            set
            {
                if (m_Path == value)
                    return;
                m_Path = value;
                if(owner != null)
                    owner.RegisterCompleteObjectUndo("Change Path");
            }
        }

        public MessageManager messageManager { get; set; }
        public bool isSubGraph { get; set; }

        [SerializeField]
        private ConcretePrecision m_ConcretePrecision = ConcretePrecision.Float;

        public ConcretePrecision concretePrecision
        {
            get => m_ConcretePrecision;
            set => m_ConcretePrecision = value;
        }

        [NonSerialized]
        Guid m_ActiveOutputNodeGuid;

        public Guid activeOutputNodeGuid
        {
            get { return m_ActiveOutputNodeGuid; }
            set
            {
                if (value != m_ActiveOutputNodeGuid)
                {
                    m_ActiveOutputNodeGuid = value;
                    m_OutputNode = null;
                    didActiveOutputNodeChange = true;
                    UpdateTargets();
                }
            }
        }

        [SerializeField]
        string m_ActiveOutputNodeGuidSerialized;

        [NonSerialized]
        private AbstractMaterialNode m_OutputNode;

        public AbstractMaterialNode outputNode
        {
            get
            {
                // find existing node
                if (m_OutputNode == null)
                {
                    if (isSubGraph)
                    {
                        m_OutputNode = GetNodes<SubGraphOutputNode>().FirstOrDefault();
                    }
                    else
                    {
                        m_OutputNode = GetNodeFromGuid(m_ActiveOutputNodeGuid);
                    }
                }

                return m_OutputNode;
            }
        }

        #region Targets
        [NonSerialized]
        List<ITargetImplementation> m_AllImplementations;

        [NonSerialized]
        List<ITarget> m_ValidTargets = new List<ITarget>();

        public List<ITarget> validTargets => m_ValidTargets;

        [SerializeField]
        int m_ActiveTargetIndex;

        public int activeTargetIndex
        {
            get => m_ActiveTargetIndex;
            set => m_ActiveTargetIndex = value;
        }

        public ITarget activeTarget => m_ValidTargets[m_ActiveTargetIndex];

        [NonSerialized]
        List<ITargetImplementation> m_ValidImplementations = new List<ITargetImplementation>();

        public List<ITargetImplementation> validImplementations => m_ValidImplementations;

        [SerializeField]
        int m_ActiveTargetImplementationBitmask = -1;

        public int activeTargetImplementationBitmask
        {
            get => m_ActiveTargetImplementationBitmask;
            set => m_ActiveTargetImplementationBitmask = value;
        }

        public List<ITargetImplementation> activeTargetImplementations
        {
            get
            {
                // Return a list of all valid TargetImplementations enabled in the bitmask
                return m_ValidImplementations.Where(s => ((1 << m_ValidImplementations.IndexOf(s)) & 
                    m_ActiveTargetImplementationBitmask) == (1 << m_ValidImplementations.IndexOf(s))).ToList();
            }
        }

        // TODO: Temporary. Remove.
        // TODO: For now simply serialize the target datas on GraphData
        // TODO: These will need to be moved to metadata objects later to allow
        // TODO: HDRP to strip shaders effectively
        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableTargetImplementationDatas = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        List<TargetImplementationData> m_TargetImplementationDatas = new List<TargetImplementationData>();

        // Used to return all target datas that have a currently active matching implementation
        public IEnumerable<TargetImplementationData> activeTargetImplementationDatas
        {
            get
            {
                return m_TargetImplementationDatas.Where(s => activeTargetImplementations.Contains(s.implementation));
            }
        }
        #endregion

        public bool didActiveOutputNodeChange { get; set; }

        internal delegate void SaveGraphDelegate(Shader shader, object context);
        internal static SaveGraphDelegate onSaveGraph;

        public GraphData()
        {
            m_GroupItems[Guid.Empty] = new List<IGroupItem>();
            GetBlockFieldDescriptors();
            GetTargetImplementations();
        }

        // We need to cache TargetImplementations
        // This is for numerous reasons:
        // - Currently we redo this reflection every target update which is wasteful
        // - We need to have matching implementation instances here and per target data object
        void GetTargetImplementations()
        {
            m_AllImplementations = new List<ITargetImplementation>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypesOrNothing())
                {
                    if (!type.IsAbstract && !type.IsGenericType && type.IsClass && typeof(ITargetImplementation).IsAssignableFrom(type))
                    {
                        var implementation = (ITargetImplementation)Activator.CreateInstance(type);
                        m_AllImplementations.Add(implementation);
                    }
                }
            }
        }

        void GetBlockFieldDescriptors()
        {
            m_BlockFieldDescriptors = new List<BlockFieldDescriptor>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var nestedType in assembly.GetTypes().SelectMany(t => t.GetNestedTypes()))
                {
                    var attrs = nestedType.GetCustomAttributes(typeof(GenerateBlocksAttribute), false);
                    if (attrs == null || attrs.Length <= 0)
                        continue;

                    // Get all fields that are BlockFieldDescriptor
                    // If field and context stages match add to list
                    foreach (var fieldInfo in nestedType.GetFields())
                    {
                        if(fieldInfo.GetValue(nestedType) is BlockFieldDescriptor blockFieldDescriptor)
                        {
                            m_BlockFieldDescriptors.Add(blockFieldDescriptor);
                        }
                    }
                }
            }
        }

        public void ClearChanges()
        {
            m_AddedNodes.Clear();
            m_RemovedNodes.Clear();
            m_PastedNodes.Clear();
            m_ParentGroupChanges.Clear();
            m_AddedGroups.Clear();
            m_RemovedGroups.Clear();
            m_PastedGroups.Clear();
            m_AddedEdges.Clear();
            m_RemovedEdges.Clear();
            m_AddedInputs.Clear();
            m_RemovedInputs.Clear();
            m_MovedInputs.Clear();
            m_AddedStickyNotes.Clear();
            m_RemovedNotes.Clear();
            m_PastedStickyNotes.Clear();
            m_MostRecentlyCreatedGroup = null;
            didActiveOutputNodeChange = false;
        }

        public void AddNode(AbstractMaterialNode node)
        {
            if (node is AbstractMaterialNode materialNode)
            {
                if (isSubGraph && !materialNode.allowedInSubGraph)
                {
                    Debug.LogWarningFormat("Attempting to add {0} to Sub Graph. This is not allowed.", materialNode.GetType());
                    return;
                }

                AddNodeNoValidate(materialNode);

                // If adding a Sub Graph node whose asset contains Keywords
                // Need to restest Keywords against the variant limit
                if(node is SubGraphNode subGraphNode &&
                    subGraphNode.asset != null &&
                    subGraphNode.asset.keywords.Count > 0)
                {
                    OnKeywordChangedNoValidate();
                }

                ValidateGraph();
            }
            else
            {
                Debug.LogWarningFormat("Trying to add node {0} to Material graph, but it is not a {1}", node, typeof(AbstractMaterialNode));
            }
        }

        public void CreateGroup(GroupData groupData)
        {
            if (AddGroup(groupData))
            {
                m_MostRecentlyCreatedGroup = groupData;
            }
        }

        bool AddGroup(GroupData groupData)
        {
            if (m_Groups.Contains(groupData))
                return false;

            m_Groups.Add(groupData);
            m_AddedGroups.Add(groupData);
            m_GroupItems.Add(groupData.guid, new List<IGroupItem>());

            return true;
        }

        public void RemoveGroup(GroupData groupData)
        {
            RemoveGroupNoValidate(groupData);
            ValidateGraph();
        }

        void RemoveGroupNoValidate(GroupData group)
        {
            if (!m_Groups.Contains(group))
                throw new InvalidOperationException("Cannot remove a group that doesn't exist.");
            m_Groups.Remove(group);
            m_RemovedGroups.Add(group);

            if (m_GroupItems.TryGetValue(group.guid, out var items))
            {
                foreach (IGroupItem groupItem in items.ToList())
                {
                    SetGroup(groupItem, null);
                }

                m_GroupItems.Remove(group.guid);
            }
        }

        public void AddStickyNote(StickyNoteData stickyNote)
        {
            if (m_StickyNotes.Contains(stickyNote))
            {
                throw new InvalidOperationException("Sticky note has already been added to the graph.");
            }

            if (!m_GroupItems.ContainsKey(stickyNote.groupGuid))
            {
                throw new InvalidOperationException("Trying to add sticky note with group that doesn't exist.");
            }

            m_StickyNotes.Add(stickyNote);
            m_AddedStickyNotes.Add(stickyNote);
            m_GroupItems[stickyNote.groupGuid].Add(stickyNote);
        }

        void RemoveNoteNoValidate(StickyNoteData stickyNote)
        {
            if (!m_StickyNotes.Contains(stickyNote))
            {
                throw new InvalidOperationException("Cannot remove a note that doesn't exist.");
            }

            m_StickyNotes.Remove(stickyNote);
            m_RemovedNotes.Add(stickyNote);

            if (m_GroupItems.TryGetValue(stickyNote.groupGuid, out var groupItems))
            {
                groupItems.Remove(stickyNote);
            }
        }

        public void RemoveStickyNote(StickyNoteData stickyNote)
        {
            RemoveNoteNoValidate(stickyNote);
            ValidateGraph();
        }

        public void SetGroup(IGroupItem node, GroupData group)
        {
            var groupChange = new ParentGroupChange()
            {
                groupItem = node,
                oldGroupGuid = node.groupGuid,
                // Checking if the groupdata is null. If it is, then it means node has been removed out of a group.
                // If the group data is null, then maybe the old group id should be removed
                newGroupGuid = group?.guid ?? Guid.Empty
            };
            node.groupGuid = groupChange.newGroupGuid;

            var oldGroupNodes = m_GroupItems[groupChange.oldGroupGuid];
            oldGroupNodes.Remove(node);

            m_GroupItems[groupChange.newGroupGuid].Add(node);
            m_ParentGroupChanges.Add(groupChange);
        }

        public void AddContexts()
        {
            m_VertexContext = new ContextData();
            m_VertexContext.contextStage = ContextStage.Vertex;
            m_VertexContext.position = new Vector2(0, 0);
            m_FragmentContext = new ContextData();
            m_FragmentContext.contextStage = ContextStage.Fragment;
            m_FragmentContext.position = new Vector2(0, 200);
        }

        public void AddBlock(BlockNode blockNode, ContextData contextData, int index)
        {
            AddBlockNoValidate(blockNode, contextData, index);
            ValidateGraph();
        }

        void AddBlockNoValidate(BlockNode blockNode, ContextData contextData, int index)
        {
            // Regular AddNode path
            AddNodeNoValidate(blockNode);

            // Set BlockNode properties
            blockNode.index = index;
            blockNode.contextData = contextData;
            
            // Add to ContextData
            if(index == -1 || index >= contextData.blockGuids.Count)
            {
                contextData.blockGuids.Add(blockNode.guid);
            }
            else
            {
                contextData.blockGuids.Insert(index, blockNode.guid);
            }

            // Update support Blocks
            UpdateSupportedBlocks();
        }

        public void UpdateSupportedBlocks()
        {
            // Get list of supported Block types
            // TODO: This should be calculated by Settings object for the Target
            var supportedBlockTypes = ListPool<string>.Get();
            var masterNode = GetNodeFromGuid<AbstractMaterialNode>(activeOutputNodeGuid) as IMasterNode;
            foreach(var implementation in validImplementations)
            {
                supportedBlockTypes.AddRange(implementation.GetSupportedBlocks(masterNode).Select(x => $"{x.tag}.{x.name}"));
            }

            // Set Blocks as active based on supported Block list
            foreach(var vertexBlockGuid in vertexContext.blockGuids)
            {
                var block = GetNodeFromGuid<BlockNode>(vertexBlockGuid);
                if(block == null)
                    continue;

                block.isActive = supportedBlockTypes.Contains(block.name);
            }
            foreach(var fragmentBlockGuid in fragmentContext.blockGuids)
            {
                var block = GetNodeFromGuid<BlockNode>(fragmentBlockGuid);
                if(block == null)
                    continue;

                block.isActive = supportedBlockTypes.Contains(block.name);
            }
        }

        void AddNodeNoValidate(AbstractMaterialNode node)
        {
            if (node.groupGuid != Guid.Empty && !m_GroupItems.ContainsKey(node.groupGuid))
            {
                throw new InvalidOperationException("Cannot add a node whose group doesn't exist.");
            }
            node.owner = this;
            if (m_FreeNodeTempIds.Any())
            {
                var id = m_FreeNodeTempIds.Pop();
                id.IncrementVersion();
                node.tempId = id;
                m_Nodes[id.index] = node;
            }
            else
            {
                var id = new Identifier(m_Nodes.Count);
                node.tempId = id;
                m_Nodes.Add(node);
            }
            m_NodeDictionary.Add(node.guid, node);
            m_AddedNodes.Add(node);
            m_GroupItems[node.groupGuid].Add(node);
        }

        public void RemoveNode(AbstractMaterialNode node)
        {
            if (!node.canDeleteNode)
            {
                throw new InvalidOperationException($"Node {node.name} ({node.guid}) cannot be deleted.");
            }
            RemoveNodeNoValidate(node);
            ValidateGraph();
        }

        void RemoveNodeNoValidate(AbstractMaterialNode node)
        {
            if (!m_NodeDictionary.ContainsKey(node.guid))
            {
                throw new InvalidOperationException("Cannot remove a node that doesn't exist.");
            }

            m_Nodes[node.tempId.index] = null;
            m_FreeNodeTempIds.Push(node.tempId);
            m_NodeDictionary.Remove(node.guid);
            messageManager?.RemoveNode(node.tempId);
            m_RemovedNodes.Add(node);

            if (m_GroupItems.TryGetValue(node.groupGuid, out var groupItems))
            {
                groupItems.Remove(node);
            }

            if(node is BlockNode blockNode && blockNode.contextData != null)
            {
                // Remove from ContextData
                blockNode.contextData.blockGuids.Remove(blockNode.guid);
            }
        }

        void AddEdgeToNodeEdges(IEdge edge)
        {
            List<IEdge> inputEdges;
            if (!m_NodeEdges.TryGetValue(edge.inputSlot.nodeGuid, out inputEdges))
                m_NodeEdges[edge.inputSlot.nodeGuid] = inputEdges = new List<IEdge>();
            inputEdges.Add(edge);

            List<IEdge> outputEdges;
            if (!m_NodeEdges.TryGetValue(edge.outputSlot.nodeGuid, out outputEdges))
                m_NodeEdges[edge.outputSlot.nodeGuid] = outputEdges = new List<IEdge>();
            outputEdges.Add(edge);
        }

        IEdge ConnectNoValidate(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            var fromNode = GetNodeFromGuid(fromSlotRef.nodeGuid);
            var toNode = GetNodeFromGuid(toSlotRef.nodeGuid);

            if (fromNode == null || toNode == null)
                return null;

            // if fromNode is already connected to toNode
            // do now allow a connection as toNode will then
            // have an edge to fromNode creating a cycle.
            // if this is parsed it will lead to an infinite loop.
            var dependentNodes = new List<AbstractMaterialNode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, toNode);
            if (dependentNodes.Contains(fromNode))
                return null;

            var fromSlot = fromNode.FindSlot<ISlot>(fromSlotRef.slotId);
            var toSlot = toNode.FindSlot<ISlot>(toSlotRef.slotId);

            if (fromSlot == null || toSlot == null)
                return null;

            if (fromSlot.isOutputSlot == toSlot.isOutputSlot)
                return null;

            var outputSlot = fromSlot.isOutputSlot ? fromSlotRef : toSlotRef;
            var inputSlot = fromSlot.isInputSlot ? fromSlotRef : toSlotRef;

            s_TempEdges.Clear();
            GetEdges(inputSlot, s_TempEdges);

            // remove any inputs that exits before adding
            foreach (var edge in s_TempEdges)
            {
                RemoveEdgeNoValidate(edge);
            }

            var newEdge = new Edge(outputSlot, inputSlot);
            m_Edges.Add(newEdge);
            m_AddedEdges.Add(newEdge);
            AddEdgeToNodeEdges(newEdge);

            //Debug.LogFormat("Connected edge: {0} -> {1} ({2} -> {3})\n{4}", newEdge.outputSlot.nodeGuid, newEdge.inputSlot.nodeGuid, fromNode.name, toNode.name, Environment.StackTrace);
            return newEdge;
        }

        public IEdge Connect(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            var newEdge = ConnectNoValidate(fromSlotRef, toSlotRef);
            ValidateGraph();
            return newEdge;
        }

        public void RemoveEdge(IEdge e)
        {
            RemoveEdgeNoValidate(e);
            ValidateGraph();
        }

        public void RemoveElements(AbstractMaterialNode[] nodes, IEdge[] edges, GroupData[] groups, StickyNoteData[] notes)
        {
            foreach (var node in nodes)
            {
                if (!node.canDeleteNode)
                {
                    throw new InvalidOperationException($"Node {node.name} ({node.guid}) cannot be deleted.");
                }
            }

            foreach (var edge in edges.ToArray())
            {
                RemoveEdgeNoValidate(edge);
            }

            foreach (var serializableNode in nodes)
            {
                RemoveNodeNoValidate(serializableNode);
            }

            foreach (var noteData in notes)
            {
                RemoveNoteNoValidate(noteData);
            }

            foreach (var groupData in groups)
            {
                RemoveGroupNoValidate(groupData);
            }

            ValidateGraph();
        }

        void RemoveEdgeNoValidate(IEdge e)
        {
            e = m_Edges.FirstOrDefault(x => x.Equals(e));
            if (e == null)
                throw new ArgumentException("Trying to remove an edge that does not exist.", "e");
            m_Edges.Remove(e as Edge);

            List<IEdge> inputNodeEdges;
            if (m_NodeEdges.TryGetValue(e.inputSlot.nodeGuid, out inputNodeEdges))
                inputNodeEdges.Remove(e);

            List<IEdge> outputNodeEdges;
            if (m_NodeEdges.TryGetValue(e.outputSlot.nodeGuid, out outputNodeEdges))
                outputNodeEdges.Remove(e);

            m_RemovedEdges.Add(e);
        }

        public AbstractMaterialNode GetNodeFromGuid(Guid guid)
        {
            AbstractMaterialNode node;
            m_NodeDictionary.TryGetValue(guid, out node);
            return node;
        }

        public AbstractMaterialNode GetNodeFromTempId(Identifier tempId)
        {
            if (tempId.index > m_Nodes.Count)
                throw new ArgumentException("Trying to retrieve a node using an identifier that does not exist.");
            var node = m_Nodes[tempId.index];
            if (node == null)
                throw new Exception("Trying to retrieve a node using an identifier that does not exist.");
            if (node.tempId.version != tempId.version)
                throw new Exception("Trying to retrieve a node that was removed from the graph.");
            return node;
        }

        public bool ContainsNodeGuid(Guid guid)
        {
            return m_NodeDictionary.ContainsKey(guid);
        }

        public T GetNodeFromGuid<T>(Guid guid) where T : AbstractMaterialNode
        {
            var node = GetNodeFromGuid(guid);
            if (node is T)
                return (T)node;
            return default(T);
        }

        public void GetEdges(SlotReference s, List<IEdge> foundEdges)
        {
            var node = GetNodeFromGuid(s.nodeGuid);
            if (node == null)
            {
                return;
            }
            ISlot slot = node.FindSlot<ISlot>(s.slotId);

            List<IEdge> candidateEdges;
            if (!m_NodeEdges.TryGetValue(s.nodeGuid, out candidateEdges))
                return;

            foreach (var edge in candidateEdges)
            {
                var cs = slot.isInputSlot ? edge.inputSlot : edge.outputSlot;
                if (cs.nodeGuid == s.nodeGuid && cs.slotId == s.slotId)
                    foundEdges.Add(edge);
            }
        }

        public IEnumerable<IEdge> GetEdges(SlotReference s)
        {
            var edges = new List<IEdge>();
            GetEdges(s, edges);
            return edges;
        }

        public void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            foreach (var prop in properties)
            {
                if(prop is GradientShaderProperty gradientProp && generationMode == GenerationMode.Preview)
                {
                    GradientUtil.GetGradientPropertiesForPreview(collector, gradientProp.referenceName, gradientProp.value);
                    continue;
                }

                collector.AddShaderProperty(prop);
            }
        }

        public void CollectShaderKeywords(KeywordCollector collector, GenerationMode generationMode)
        {
            foreach (var keyword in keywords)
            {
                collector.AddShaderKeyword(keyword);
            }

            // Alwways calculate permutations when collecting
            collector.CalculateKeywordPermutations();
        }

        public void AddGraphInput(ShaderInput input)
        {
            if (input == null)
                return;

            switch(input)
            {
                case AbstractShaderProperty property:
                    if (m_Properties.Contains(property))
                        return;
                    m_Properties.Add(property);
                    break;
                case ShaderKeyword keyword:
                    if (m_Keywords.Contains(keyword))
                        return;
                    m_Keywords.Add(keyword);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            m_AddedInputs.Add(input);
        }

        public void SanitizeGraphInputName(ShaderInput input)
        {
            input.displayName = input.displayName.Trim();
            switch(input)
            {
                case AbstractShaderProperty property:
                    input.displayName = GraphUtil.SanitizeName(properties.Where(p => p.guid != input.guid).Select(p => p.displayName), "{0} ({1})", input.displayName);
                    break;
                case ShaderKeyword keyword:
                    input.displayName = GraphUtil.SanitizeName(keywords.Where(p => p.guid != input.guid).Select(p => p.displayName), "{0} ({1})", input.displayName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SanitizeGraphInputReferenceName(ShaderInput input, string newName)
        {
            if (string.IsNullOrEmpty(newName))
                return;

            string name = newName.Trim();
            if (string.IsNullOrEmpty(name))
                return;

            name = Regex.Replace(name, @"(?:[^A-Za-z_0-9])|(?:\s)", "_");
            switch(input)
            {
                case AbstractShaderProperty property:
                    property.overrideReferenceName = GraphUtil.SanitizeName(properties.Where(p => p.guid != property.guid).Select(p => p.referenceName), "{0}_{1}", name);
                    break;
                case ShaderKeyword keyword:
                    keyword.overrideReferenceName = GraphUtil.SanitizeName(keywords.Where(p => p.guid != input.guid).Select(p => p.referenceName), "{0}_{1}", name).ToUpper();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void RemoveGraphInput(ShaderInput input)
        {
            switch(input)
            {
                case AbstractShaderProperty property:
                    var propetyNodes = GetNodes<PropertyNode>().Where(x => x.propertyGuid == input.guid).ToList();
                    foreach (var propNode in propetyNodes)
                        ReplacePropertyNodeWithConcreteNodeNoValidate(propNode);
                    break;
            }

            RemoveGraphInputNoValidate(input.guid);
            ValidateGraph();
        }

        public void MoveProperty(AbstractShaderProperty property, int newIndex)
        {
            if (newIndex > m_Properties.Count || newIndex < 0)
                throw new ArgumentException("New index is not within properties list.");
            var currentIndex = m_Properties.IndexOf(property);
            if (currentIndex == -1)
                throw new ArgumentException("Property is not in graph.");
            if (newIndex == currentIndex)
                return;
            m_Properties.RemoveAt(currentIndex);
            if (newIndex > currentIndex)
                newIndex--;
            var isLast = newIndex == m_Properties.Count;
            if (isLast)
                m_Properties.Add(property);
            else
                m_Properties.Insert(newIndex, property);
            if (!m_MovedInputs.Contains(property))
                m_MovedInputs.Add(property);
        }

        public void MoveKeyword(ShaderKeyword keyword, int newIndex)
        {
            if (newIndex > m_Keywords.Count || newIndex < 0)
                throw new ArgumentException("New index is not within keywords list.");
            var currentIndex = m_Keywords.IndexOf(keyword);
            if (currentIndex == -1)
                throw new ArgumentException("Keyword is not in graph.");
            if (newIndex == currentIndex)
                return;
            m_Keywords.RemoveAt(currentIndex);
            if (newIndex > currentIndex)
                newIndex--;
            var isLast = newIndex == m_Keywords.Count;
            if (isLast)
                m_Keywords.Add(keyword);
            else
                m_Keywords.Insert(newIndex, keyword);
            if (!m_MovedInputs.Contains(keyword))
                m_MovedInputs.Add(keyword);
        }

        public int GetGraphInputIndex(ShaderInput input)
        {
            switch(input)
            {
                case AbstractShaderProperty property:
                    return m_Properties.IndexOf(property);
                case ShaderKeyword keyword:
                    return m_Keywords.IndexOf(keyword);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void RemoveGraphInputNoValidate(Guid guid)
        {
            if (m_Properties.RemoveAll(x => x.guid == guid) > 0 ||
                m_Keywords.RemoveAll(x => x.guid == guid) > 0)
            {
                m_RemovedInputs.Add(guid);
                m_AddedInputs.RemoveAll(x => x.guid == guid);
                m_MovedInputs.RemoveAll(x => x.guid == guid);
            }
        }

        static List<IEdge> s_TempEdges = new List<IEdge>();

        public void ReplacePropertyNodeWithConcreteNode(PropertyNode propertyNode)
        {
            ReplacePropertyNodeWithConcreteNodeNoValidate(propertyNode);
            ValidateGraph();
        }

        void ReplacePropertyNodeWithConcreteNodeNoValidate(PropertyNode propertyNode)
        {
            var property = properties.FirstOrDefault(x => x.guid == propertyNode.propertyGuid);
            if (property == null)
                return;

            var node = property.ToConcreteNode() as AbstractMaterialNode;
            if (node == null)
                return;

            var slot = propertyNode.FindOutputSlot<MaterialSlot>(PropertyNode.OutputSlotId);
            var newSlot = node.GetOutputSlots<MaterialSlot>().FirstOrDefault(s => s.valueType == slot.valueType);
            if (newSlot == null)
                return;

            node.drawState = propertyNode.drawState;
            node.groupGuid = propertyNode.groupGuid;
            AddNodeNoValidate(node);

            foreach (var edge in this.GetEdges(slot.slotReference))
                ConnectNoValidate(newSlot.slotReference, edge.inputSlot);

            RemoveNodeNoValidate(propertyNode);
        }

        public void OnKeywordChanged()
        {
            OnKeywordChangedNoValidate();
            ValidateGraph();
        }

        public void OnKeywordChangedNoValidate()
        {
            var allNodes = GetNodes<AbstractMaterialNode>();
            foreach(AbstractMaterialNode node in allNodes)
            {
                node.Dirty(ModificationScope.Topological);
                node.ValidateNode();
            }
        }

        public void ValidateGraph()
        {
            var propertyNodes = GetNodes<PropertyNode>().Where(n => !m_Properties.Any(p => p.guid == n.propertyGuid)).ToArray();
            foreach (var pNode in propertyNodes)
                ReplacePropertyNodeWithConcreteNodeNoValidate(pNode);

            messageManager?.ClearAllFromProvider(this);
            //First validate edges, remove any
            //orphans. This can happen if a user
            //manually modifies serialized data
            //of if they delete a node in the inspector
            //debug view.
            foreach (var edge in edges.ToArray())
            {
                var outputNode = GetNodeFromGuid(edge.outputSlot.nodeGuid);
                var inputNode = GetNodeFromGuid(edge.inputSlot.nodeGuid);

                MaterialSlot outputSlot = null;
                MaterialSlot inputSlot = null;
                if (outputNode != null && inputNode != null)
                {
                    outputSlot = outputNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId);
                    inputSlot = inputNode.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId);
                }

                if (outputNode == null
                    || inputNode == null
                    || outputSlot == null
                    || inputSlot == null)
                {
                    //orphaned edge
                    RemoveEdgeNoValidate(edge);
                }
            }

            var temporaryMarks = IndexSetPool.Get();
            var permanentMarks = IndexSetPool.Get();
            var slots = ListPool<MaterialSlot>.Get();

            // Make sure we process a node's children before the node itself.
            var stack = StackPool<AbstractMaterialNode>.Get();
            foreach (var node in GetNodes<AbstractMaterialNode>())
            {
                stack.Push(node);
            }
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (permanentMarks.Contains(node.tempId.index))
                {
                    continue;
                }

                if (temporaryMarks.Contains(node.tempId.index))
                {
                    node.ValidateNode();
                    permanentMarks.Add(node.tempId.index);
                }
                else
                {
                    temporaryMarks.Add(node.tempId.index);
                    stack.Push(node);
                    node.GetInputSlots(slots);
                    foreach (var inputSlot in slots)
                    {
                        var nodeEdges = GetEdges(inputSlot.slotReference);
                        foreach (var edge in nodeEdges)
                        {
                            var fromSocketRef = edge.outputSlot;
                            var childNode = GetNodeFromGuid(fromSocketRef.nodeGuid);
                            if (childNode != null)
                            {
                                stack.Push(childNode);
                            }
                        }
                    }
                    slots.Clear();
                }
            }

            StackPool<AbstractMaterialNode>.Release(stack);
            ListPool<MaterialSlot>.Release(slots);
            IndexSetPool.Release(temporaryMarks);
            IndexSetPool.Release(permanentMarks);

            foreach (var edge in m_AddedEdges.ToList())
            {
                if (!ContainsNodeGuid(edge.outputSlot.nodeGuid) || !ContainsNodeGuid(edge.inputSlot.nodeGuid))
                {
                    Debug.LogWarningFormat("Added edge is invalid: {0} -> {1}\n{2}", edge.outputSlot.nodeGuid, edge.inputSlot.nodeGuid, Environment.StackTrace);
                    m_AddedEdges.Remove(edge);
                }
            }

            foreach (var groupChange in m_ParentGroupChanges.ToList())
            {
                if (groupChange.groupItem is AbstractMaterialNode node && !ContainsNodeGuid(node.guid))
                {
                    m_ParentGroupChanges.Remove(groupChange);
                }

                if (groupChange.groupItem is StickyNoteData stickyNote && !m_StickyNotes.Contains(stickyNote))
                {
                    m_ParentGroupChanges.Remove(groupChange);
                }
            }
        }

        public void AddValidationError(Identifier id, string errorMessage,
            ShaderCompilerMessageSeverity severity = ShaderCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new ShaderMessage(errorMessage, severity));
        }

        public void ClearErrorsForNode(AbstractMaterialNode node)
        {
            messageManager?.ClearNodesFromProvider(this, node.ToEnumerable());
        }

        public void ReplaceWith(GraphData other)
        {
            if (other == null)
                throw new ArgumentException("Can only replace with another AbstractMaterialGraph", "other");

            using (var removedInputsPooledObject = ListPool<Guid>.GetDisposable())
            {
                var removedInputGuids = removedInputsPooledObject.value;
                foreach (var property in m_Properties)
                    removedInputGuids.Add(property.guid);
                foreach (var keyword in m_Keywords)
                    removedInputGuids.Add(keyword.guid);
                foreach (var inputGuid in removedInputGuids)
                    RemoveGraphInputNoValidate(inputGuid);
            }
            foreach (var otherProperty in other.properties)
            {
                if (!properties.Any(p => p.guid == otherProperty.guid))
                    AddGraphInput(otherProperty);
            }
            foreach (var otherKeyword in other.keywords)
            {
                if (!keywords.Any(p => p.guid == otherKeyword.guid))
                    AddGraphInput(otherKeyword);
            }

            other.ValidateGraph();
            ValidateGraph();

            // Current tactic is to remove all nodes and edges and then re-add them, such that depending systems
            // will re-initialize with new references.

            using (var removedGroupsPooledObject = ListPool<GroupData>.GetDisposable())
            {
                var removedGroupDatas = removedGroupsPooledObject.value;
                removedGroupDatas.AddRange(m_Groups);
                foreach (var groupData in removedGroupDatas)
                {
                    RemoveGroupNoValidate(groupData);
                }
            }

            using (var removedNotesPooledObject = ListPool<StickyNoteData>.GetDisposable())
            {
                var removedNoteDatas = removedNotesPooledObject.value;
                removedNoteDatas.AddRange(m_StickyNotes);
                foreach (var groupData in removedNoteDatas)
                {
                    RemoveNoteNoValidate(groupData);
                }
            }

            using (var pooledList = ListPool<IEdge>.GetDisposable())
            {
                var removedNodeEdges = pooledList.value;
                removedNodeEdges.AddRange(m_Edges);
                foreach (var edge in removedNodeEdges)
                    RemoveEdgeNoValidate(edge);
            }

            using (var removedNodesPooledObject = ListPool<Guid>.GetDisposable())
            {
                var removedNodeGuids = removedNodesPooledObject.value;
                removedNodeGuids.AddRange(m_Nodes.Where(n => n != null).Select(n => n.guid));
                foreach (var nodeGuid in removedNodeGuids)
                    RemoveNodeNoValidate(m_NodeDictionary[nodeGuid]);
            }

            ValidateGraph();

            foreach (GroupData groupData in other.groups)
                AddGroup(groupData);

            foreach (var stickyNote in other.stickyNotes)
            {
                AddStickyNote(stickyNote);
            }

            foreach (var node in other.GetNodes<AbstractMaterialNode>())
            {
                if(node is BlockNode blockNode)
                {
                    var contextData = blockNode.descriptor.contextStage == ContextStage.Vertex ? vertexContext : fragmentContext;
                    AddBlockNoValidate(blockNode, contextData, blockNode.index);
                }
                else
                {
                    AddNodeNoValidate(node);
                }
            }

            foreach (var edge in other.edges)
                ConnectNoValidate(edge.outputSlot, edge.inputSlot);

            ValidateGraph();
        }

        internal void PasteGraph(CopyPasteGraph graphToPaste, List<AbstractMaterialNode> remappedNodes, List<IEdge> remappedEdges)
        {
            var groupGuidMap = new Dictionary<Guid, Guid>();
            foreach (var group in graphToPaste.groups)
            {
                var position = group.position;
                position.x += 30;
                position.y += 30;

                GroupData newGroup = new GroupData(group.title, position);

                var oldGuid = group.guid;
                var newGuid = newGroup.guid;
                groupGuidMap[oldGuid] = newGuid;

                AddGroup(newGroup);
                m_PastedGroups.Add(newGroup);
            }

            foreach (var stickyNote in graphToPaste.stickyNotes)
            {
                var position = stickyNote.position;
                position.x += 30;
                position.y += 30;

                StickyNoteData pastedStickyNote = new StickyNoteData(stickyNote.title, stickyNote.content, position);
                if (groupGuidMap.ContainsKey(stickyNote.groupGuid))
                {
                    pastedStickyNote.groupGuid = groupGuidMap[stickyNote.groupGuid];
                }

                AddStickyNote(pastedStickyNote);
                m_PastedStickyNotes.Add(pastedStickyNote);
            }

            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in graphToPaste.GetNodes<AbstractMaterialNode>())
            {
                AbstractMaterialNode pastedNode = node;

                var oldGuid = node.guid;
                var newGuid = node.RewriteGuid();
                nodeGuidMap[oldGuid] = newGuid;

                // Check if the property nodes need to be made into a concrete node.
                if (node is PropertyNode propertyNode)
                {
                    // If the property is not in the current graph, do check if the
                    // property can be made into a concrete node.
                    if (!m_Properties.Select(x => x.guid).Contains(propertyNode.propertyGuid))
                    {
                        // If the property is in the serialized paste graph, make the property node into a property node.
                        var pastedGraphMetaProperties = graphToPaste.metaProperties.Where(x => x.guid == propertyNode.propertyGuid);
                        if (pastedGraphMetaProperties.Any())
                        {
                            pastedNode = pastedGraphMetaProperties.FirstOrDefault().ToConcreteNode();
                            pastedNode.drawState = node.drawState;
                            nodeGuidMap[oldGuid] = pastedNode.guid;
                        }
                    }
                }

                AbstractMaterialNode abstractMaterialNode = (AbstractMaterialNode)node;

                // If the node has a group guid and no group has been copied, reset the group guid.
                // Check if the node is inside a group
                if (abstractMaterialNode.groupGuid != Guid.Empty)
                {
                    if (groupGuidMap.ContainsKey(abstractMaterialNode.groupGuid))
                    {
                        var absNode = pastedNode as AbstractMaterialNode;
                        absNode.groupGuid = groupGuidMap[abstractMaterialNode.groupGuid];
                        pastedNode = absNode;
                    }
                    else
                    {
                        pastedNode.groupGuid = Guid.Empty;
                    }
                }

                var drawState = node.drawState;
                var position = drawState.position;
                position.x += 30;
                position.y += 30;
                drawState.position = position;
                node.drawState = drawState;
                remappedNodes.Add(pastedNode);
                AddNode(pastedNode);

                // add the node to the pasted node list
                m_PastedNodes.Add(pastedNode);

                // Check if the keyword nodes need to have their keywords copied.
                if (node is KeywordNode keywordNode)
                {
                    // If the keyword is not in the current graph and is in the serialized paste graph copy it.
                    if (!keywords.Select(x => x.guid).Contains(keywordNode.keywordGuid))
                    {
                        var pastedGraphMetaKeywords = graphToPaste.metaKeywords.Where(x => x.guid == keywordNode.keywordGuid);
                        if (pastedGraphMetaKeywords.Any())
                        {
                            var keyword = pastedGraphMetaKeywords.FirstOrDefault(x => x.guid == keywordNode.keywordGuid);
                            SanitizeGraphInputName(keyword);
                            SanitizeGraphInputReferenceName(keyword, keyword.overrideReferenceName);
                            AddGraphInput(keyword);
                        }
                    }

                    // Always update Keyword nodes to handle any collisions resolved on the Keyword
                    keywordNode.UpdateNode();
                }
            }

            // only connect edges within pasted elements, discard
            // external edges.
            foreach (var edge in graphToPaste.edges)
            {
                var outputSlot = edge.outputSlot;
                var inputSlot = edge.inputSlot;

                Guid remappedOutputNodeGuid;
                Guid remappedInputNodeGuid;
                if (nodeGuidMap.TryGetValue(outputSlot.nodeGuid, out remappedOutputNodeGuid)
                    && nodeGuidMap.TryGetValue(inputSlot.nodeGuid, out remappedInputNodeGuid))
                {
                    var outputSlotRef = new SlotReference(remappedOutputNodeGuid, outputSlot.slotId);
                    var inputSlotRef = new SlotReference(remappedInputNodeGuid, inputSlot.slotId);
                    remappedEdges.Add(Connect(outputSlotRef, inputSlotRef));
                }
            }

            ValidateGraph();
        }

        public void OnBeforeSerialize()
        {
            var nodes = GetNodes<AbstractMaterialNode>().ToList();
            nodes.Sort((x1, x2) => x1.guid.CompareTo(x2.guid));
            m_SerializableNodes = SerializationHelper.Serialize(nodes.AsEnumerable());
            m_Edges.Sort();
            m_SerializableEdges = SerializationHelper.Serialize<Edge>(m_Edges);
            m_SerializedProperties = SerializationHelper.Serialize<AbstractShaderProperty>(m_Properties);
            m_SerializedKeywords = SerializationHelper.Serialize<ShaderKeyword>(m_Keywords);
            m_ActiveOutputNodeGuidSerialized = m_ActiveOutputNodeGuid == Guid.Empty ? null : m_ActiveOutputNodeGuid.ToString();

            // Serialize implementation datas
            // We also serialize their implementation reference here (see OnAfterDeserialize)
            foreach(var implementationData in m_TargetImplementationDatas)
            {
                implementationData.serializedImplementation = implementationData.implementation.GetType().FullName;
            }
            m_SerializableTargetImplementationDatas = SerializationHelper.Serialize<TargetImplementationData>(m_TargetImplementationDatas);
        }

        public void OnAfterDeserialize()
        {
            // have to deserialize 'globals' before nodes
            m_Properties = SerializationHelper.Deserialize<AbstractShaderProperty>(m_SerializedProperties, GraphUtil.GetLegacyTypeRemapping());
            m_Keywords = SerializationHelper.Deserialize<ShaderKeyword>(m_SerializedKeywords, GraphUtil.GetLegacyTypeRemapping());

            var nodes = SerializationHelper.Deserialize<AbstractMaterialNode>(m_SerializableNodes, GraphUtil.GetLegacyTypeRemapping());

            m_Nodes = new List<AbstractMaterialNode>(nodes.Count);
            m_NodeDictionary = new Dictionary<Guid, AbstractMaterialNode>(nodes.Count);

            foreach (var group in m_Groups)
            {
                m_GroupItems.Add(group.guid, new List<IGroupItem>());
            }

            foreach (var node in nodes)
            {
                node.owner = this;
                node.UpdateNodeAfterDeserialization();
                node.tempId = new Identifier(m_Nodes.Count);
                m_Nodes.Add(node);
                m_NodeDictionary.Add(node.guid, node);
                m_GroupItems[node.groupGuid].Add(node);
            }

            foreach (var stickyNote in m_StickyNotes)
            {
                m_GroupItems[stickyNote.groupGuid].Add(stickyNote);
            }

            m_SerializableNodes = null;

            m_Edges = SerializationHelper.Deserialize<Edge>(m_SerializableEdges, GraphUtil.GetLegacyTypeRemapping());
            m_SerializableEdges = null;
            foreach (var edge in m_Edges)
                AddEdgeToNodeEdges(edge);

            // Process Blocks
            m_VertexContext.contextStage = ContextStage.Vertex;
            var vertexBlockCount = m_VertexContext.blockGuids.Count;
            for(int i = 0; i < vertexBlockCount; i++)
            {
                var vertexBlock = GetNodeFromGuid<BlockNode>(m_VertexContext.blockGuids[i]);
                vertexBlock.descriptor = m_BlockFieldDescriptors.FirstOrDefault(x => $"{x.tag}.{x.name}" == vertexBlock.serializedDescriptor);
                vertexBlock.contextData = m_VertexContext;
                vertexBlock.index = i;
            }
            m_FragmentContext.contextStage = ContextStage.Fragment;
            var fragmentBlockCount = m_FragmentContext.blockGuids.Count;
            for(int i = 0; i < fragmentBlockCount; i++)
            {
                var fragmentBlock = GetNodeFromGuid<BlockNode>(m_FragmentContext.blockGuids[i]);
                fragmentBlock.descriptor = m_BlockFieldDescriptors.FirstOrDefault(x => $"{x.tag}.{x.name}" == fragmentBlock.serializedDescriptor);
                fragmentBlock.contextData = m_FragmentContext;
                fragmentBlock.index = i;
            }

            m_OutputNode = null;

            if (!isSubGraph)
            {
                if (string.IsNullOrEmpty(m_ActiveOutputNodeGuidSerialized))
                {
                    var node = (AbstractMaterialNode)GetNodes<IMasterNode>().FirstOrDefault();
                    if (node != null)
                    {
                        m_ActiveOutputNodeGuid = node.guid;
                    }
                }
                else
                {
                    m_ActiveOutputNodeGuid = new Guid(m_ActiveOutputNodeGuidSerialized);
                }
            }

            // Deserialize implementation datas
            // Because deserialization of their implementation references requires the implementation list stored here
            // We simply deserialize them here rather than implementing ISerializationCallbackReceiver on the data object
            // and handling the GraphData reference there somehow (deserialization order issues)
            m_TargetImplementationDatas = SerializationHelper.Deserialize<TargetImplementationData>(m_SerializableTargetImplementationDatas, GraphUtil.GetLegacyTypeRemapping());
            foreach(var implementationData in m_TargetImplementationDatas)
            {
                implementationData.implementation = m_AllImplementations.FirstOrDefault(x => x.GetType().FullName == implementationData.serializedImplementation);
            }
        }

        public void OnEnable()
        {
            foreach (var node in GetNodes<AbstractMaterialNode>().OfType<IOnAssetEnabled>())
            {
                node.OnEnable();
            }

            UpdateTargets();

            ShaderGraphPreferences.onVariantLimitChanged += OnKeywordChanged;
        }

        public void OnDisable()
        {
            ShaderGraphPreferences.onVariantLimitChanged -= OnKeywordChanged;
        }

        public void UpdateTargets()
        {
            if(outputNode == null)
                return;

            // First get all valid TargetImplementations that are valid with the current graph
            List<ITargetImplementation> foundImplementations = new List<ITargetImplementation>();
            foreach(var implementation in m_AllImplementations)
            {
                // dataType property must be of type TargetImplementationData
                // but we have no way of constraining this so we have to simply return warnings
                if(!implementation.dataType.IsSubclassOf(typeof(TargetImplementationData)))
                {
                    Debug.LogWarning($"{implementation.GetType().Name} dataType does not derive from Type TargetImplementationData. Will be ignored.");
                    continue;
                }

                // TODO: This can probably be optimised. After moving to caching the implementation on ctor
                // TODO: this section allocs GC just to either remove PreviewTarget or return only PreviewTarget depending on if this is a Subgraph
                if (outputNode is SubGraphOutputNode && typeof(DefaultPreviewTarget).IsAssignableFrom(implementation.GetType()))
                {
                    foundImplementations.Add(implementation);
                }
                else if (!foundImplementations.Contains(implementation) && !typeof(DefaultPreviewTarget).IsAssignableFrom(implementation.GetType()))
                {
                    foundImplementations.Add(implementation);
                }
            }

            // Next we get all Targets that have valid TargetImplementations
            List<ITarget> foundTargets = new List<ITarget>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypesOrNothing())
                {
                    var isTarget = !type.IsAbstract && !type.IsGenericType && type.IsClass && typeof(ITarget).IsAssignableFrom(type);
                    if (isTarget && !foundTargets.Any(s => s.GetType() == type))
                    {
                        var target = (ITarget)Activator.CreateInstance(type);
                        if(foundImplementations.Where(s => s.targetType == type).Any())
                            foundTargets.Add(target);
                    }
                }
            }

            // Assembly reload, just rebuild the non-serialized lists
            if(m_ValidTargets.Count == 0)
            {
                m_ValidTargets = foundTargets;
                m_ValidImplementations = foundImplementations.Where(s => s.targetType == foundTargets[0].GetType()).ToList();
            }

            // Active Target index has changed
            // Still need to validate TargetImplementation bitmask
            if(foundTargets[m_ActiveTargetIndex].GetType() != activeTarget.GetType())
            {
                var activeTargetInFoundList = foundTargets.Where(s => s.GetType() == activeTarget.GetType()).FirstOrDefault();
                m_ActiveTargetIndex = foundTargets.IndexOf(activeTargetInFoundList);
            }

            m_ValidTargets = foundTargets;
            m_ValidImplementations = foundImplementations.Where(s => s.targetType == activeTarget.GetType()).ToList();

            // Nothing or Everything. No need to update bitmask.
            if(m_ActiveTargetImplementationBitmask != 0 && m_ActiveTargetImplementationBitmask != -1)
            {
                // Current ITargetImplementation bitmask is set to Mixed...
                // We need to build a new bitmask from the indicies in the new Implementation list
                int newBitmask = 0;
                foreach(ITargetImplementation implementation in activeTargetImplementations)
                {
                    var implementationInFound = foundImplementations.Where(s => s.GetType() == implementation.GetType()).FirstOrDefault();
                    if(implementationInFound != null)
                    {
                        // If the new Implementation list contains this Implementation
                        // add its new index to the bitmask
                        newBitmask = newBitmask | (1 << foundImplementations.IndexOf(implementationInFound));
                    }
                }
                m_ActiveTargetImplementationBitmask = newBitmask;
            }
            
            UpdateSupportedBlocks();
            UpdateTargetDatas();
        }

        void UpdateTargetDatas()
        {
            // Ensure that all active TargetImplementations have a matching data object
            // Currently we never remove serialized data objects
            foreach(var implementation in activeTargetImplementations)
            {
                if(!m_TargetImplementationDatas.Any(s => s.implementation == implementation))
                {
                    var implementationData = Activator.CreateInstance(implementation.dataType) as TargetImplementationData;
                    implementationData.Init(implementation);
                    m_TargetImplementationDatas.Add(implementationData);
                }
            }
        }
    }

    [Serializable]
    class InspectorPreviewData
    {
        public SerializableMesh serializedMesh = new SerializableMesh();

        [NonSerialized]
        public Quaternion rotation = Quaternion.identity;

        [NonSerialized]
        public float scale = 1f;
    }
}
