using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;
using Edge = UnityEditor.Graphing.Edge;

using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.MaterialGraph")]
    [FormerName("UnityEditor.ShaderGraph.SubGraph")]
    [FormerName("UnityEditor.ShaderGraph.AbstractMaterialGraph")]
    sealed partial class GraphData : JsonObject
    {
        const int k_CurrentVersion = 2;

        [SerializeField]
        int m_Version;

        public GraphObject owner { get; set; }

        #region Input data

        [SerializeField]
        List<JsonData<AbstractShaderProperty>> m_Properties = new List<JsonData<AbstractShaderProperty>>();

        public DataValueEnumerable<AbstractShaderProperty> properties => m_Properties.SelectValue();

        [SerializeField]
        List<JsonData<ShaderKeyword>> m_Keywords = new List<JsonData<ShaderKeyword>>();

        public DataValueEnumerable<ShaderKeyword> keywords => m_Keywords.SelectValue();

        [NonSerialized]
        List<ShaderInput> m_AddedInputs = new List<ShaderInput>();

        public IEnumerable<ShaderInput> addedInputs
        {
            get { return m_AddedInputs; }
        }

        [NonSerialized]
        List<ShaderInput> m_RemovedInputs = new List<ShaderInput>();

        public IEnumerable<ShaderInput> removedInputs
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

        [SerializeField]
        List<JsonData<AbstractMaterialNode>> m_Nodes = new List<JsonData<AbstractMaterialNode>>();

        [NonSerialized]
        Dictionary<string, AbstractMaterialNode> m_NodeDictionary = new Dictionary<string, AbstractMaterialNode>();

        public IEnumerable<T> GetNodes<T>()
        {
            return m_Nodes.SelectValue().OfType<T>();
        }

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
        List<JsonData<GroupData>> m_GroupDatas = new List<JsonData<GroupData>>();

        public DataValueEnumerable<GroupData> groups
        {
            get { return m_GroupDatas.SelectValue(); }
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
        Dictionary<JsonRef<GroupData>, List<IGroupItem>> m_GroupItems = new Dictionary<JsonRef<GroupData>, List<IGroupItem>>();

        public IEnumerable<IGroupItem> GetItemsInGroup(GroupData groupData)
        {
            if (m_GroupItems.TryGetValue(groupData, out var nodes))
            {
                return nodes;
            }
            return Enumerable.Empty<IGroupItem>();
        }

        #endregion

        #region StickyNote Data
        [SerializeField]
        List<JsonData<StickyNoteData>> m_StickyNoteDatas = new List<JsonData<StickyNoteData>>();

        public DataValueEnumerable<StickyNoteData> stickyNotes => m_StickyNoteDatas.SelectValue();

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

        [SerializeField]
        List<Edge> m_Edges = new List<Edge>();

        public IEnumerable<Edge> edges => m_Edges;

        [NonSerialized]
        Dictionary<string, List<IEdge>> m_NodeEdges = new Dictionary<string, List<IEdge>>();

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

        [SerializeField]
        JsonRef<AbstractMaterialNode> m_OutputNode;

        public AbstractMaterialNode outputNode
        {
            get => m_OutputNode;
            set => m_OutputNode = value;
        }

        internal delegate void SaveGraphDelegate(Shader shader, object context);
        internal static SaveGraphDelegate onSaveGraph;

        #region Targets
        [SerializeField]
        List<JsonData<Target>> m_ActiveTargets = new List<JsonData<Target>>();

        [NonSerialized]
        List<Target> m_ValidTargets = new List<Target>();

        int m_ActiveTargetBitmask;
        public int activeTargetBitmask
        {
            get => m_ActiveTargetBitmask;
            set => m_ActiveTargetBitmask = value;
        }

        public List<Target> validTargets => m_ValidTargets;
        public DataValueEnumerable<Target> activeTargets => m_ActiveTargets.SelectValue();

        // TODO: Need a better way to handle this
        public bool isVFXTarget => !isSubGraph && activeTargets.Count() > 0 && activeTargets.ElementAt(0).GetType() == typeof(VFXTarget);
        #endregion

        public GraphData()
        {
            m_GroupItems[null] = new List<IGroupItem>();
            GetBlockFieldDescriptors();
            GetTargets();
        }

        public void InitializeOutputs(Target[] targets, BlockFieldDescriptor[] blockDescriptors)
        {
            if(targets == null)
                return;

            foreach(var target in targets)
            {
                if(m_ValidTargets.Any(x => x.GetType().Equals(target.GetType())))
                {
                    m_ActiveTargets.Add(target);
                }
            }

            if(blockDescriptors != null)
            {
                foreach(var descriptor in blockDescriptors)
                {
                    var contextData = descriptor.shaderStage == ShaderStage.Fragment ? m_FragmentContext : m_VertexContext;
                    var block = (BlockNode)Activator.CreateInstance(typeof(BlockNode));
                    block.Init(descriptor);
                    AddBlockNoValidate(block, contextData, contextData.blocks.Count);
                }
            }

            ValidateGraph();
            var activeBlocks = GetActiveBlocksForAllActiveTargets();
            UpdateActiveBlocks(activeBlocks);
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

                    var attribute = attrs[0] as GenerateBlocksAttribute;

                    // Get all fields that are BlockFieldDescriptor
                    // If field and context stages match add to list
                    foreach (var fieldInfo in nestedType.GetFields())
                    {
                        if(fieldInfo.GetValue(nestedType) is BlockFieldDescriptor blockFieldDescriptor)
                        {
                            blockFieldDescriptor.path = attribute.path;
                            m_BlockFieldDescriptors.Add(blockFieldDescriptor);
                        }
                    }
                }
            }
        }

        void GetTargets()
        {
            // Find all valid Targets
            var typeCollection = TypeCache.GetTypesDerivedFrom<Target>();
            foreach(var type in typeCollection)
            {
                if(type.IsAbstract || type.IsGenericType || !type.IsClass)
                    continue;

                var target = (Target)Activator.CreateInstance(type);
                if(!target.isHidden)
                {
                    m_ValidTargets.Add(target);
                }
            }
        }

        public void UpdateActiveTargets()
        {
            // Update active TargetImplementation list
            if(m_ActiveTargets != null)
            {
                m_ActiveTargets.Clear();
                var targetCount = m_ValidTargets.Count;
                for(int i = 0; i < targetCount; i++)
                {
                    if(((1 << i) & m_ActiveTargetBitmask) == (1 << i))
                    {
                        m_ActiveTargets.Add(m_ValidTargets[i]);
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
                    subGraphNode.asset.keywords.Any())
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
            if (m_GroupDatas.Contains(groupData))
                return false;

            m_GroupDatas.Add(groupData);
            m_AddedGroups.Add(groupData);
            m_GroupItems.Add(groupData, new List<IGroupItem>());

            return true;
        }

        public void RemoveGroup(GroupData groupData)
        {
            RemoveGroupNoValidate(groupData);
            ValidateGraph();
        }

        void RemoveGroupNoValidate(GroupData group)
        {
            if (!m_GroupDatas.Contains(group))
                throw new InvalidOperationException("Cannot remove a group that doesn't exist.");
            m_GroupDatas.Remove(group);
            m_RemovedGroups.Add(group);

            if (m_GroupItems.TryGetValue(group, out var items))
            {
                foreach (IGroupItem groupItem in items.ToList())
                {
                    SetGroup(groupItem, null);
                }

                m_GroupItems.Remove(group);
            }
        }

        public void AddStickyNote(StickyNoteData stickyNote)
        {
            if (m_StickyNoteDatas.Contains(stickyNote))
            {
                throw new InvalidOperationException("Sticky note has already been added to the graph.");
            }

            if (!m_GroupItems.ContainsKey(stickyNote.group))
            {
                throw new InvalidOperationException("Trying to add sticky note with group that doesn't exist.");
            }

            m_StickyNoteDatas.Add(stickyNote);
            m_AddedStickyNotes.Add(stickyNote);
            m_GroupItems[stickyNote.group].Add(stickyNote);
        }

        void RemoveNoteNoValidate(StickyNoteData stickyNote)
        {
            if (!m_StickyNoteDatas.Contains(stickyNote))
            {
                throw new InvalidOperationException("Cannot remove a note that doesn't exist.");
            }

            m_StickyNoteDatas.Remove(stickyNote);
            m_RemovedNotes.Add(stickyNote);

            if (m_GroupItems.TryGetValue(stickyNote.group, out var groupItems))
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
                oldGroup = node.group,
                // Checking if the groupdata is null. If it is, then it means node has been removed out of a group.
                // If the group data is null, then maybe the old group id should be removed
                newGroup = group,
            };
            node.group = groupChange.newGroup;

            var oldGroupNodes = m_GroupItems[groupChange.oldGroup];
            oldGroupNodes.Remove(node);

            m_GroupItems[groupChange.newGroup].Add(node);
            m_ParentGroupChanges.Add(groupChange);
        }

        public void AddContexts()
        {
            m_VertexContext = new ContextData();
            m_VertexContext.shaderStage = ShaderStage.Vertex;
            m_VertexContext.position = new Vector2(0, 0);
            m_FragmentContext = new ContextData();
            m_FragmentContext.shaderStage = ShaderStage.Fragment;
            m_FragmentContext.position = new Vector2(0, 200);
        }

        public void AddBlock(BlockNode blockNode, ContextData contextData, int index)
        {
            AddBlockNoValidate(blockNode, contextData, index);
            ValidateGraph();

            var activeBlocks = GetActiveBlocksForAllActiveTargets();
            UpdateActiveBlocks(activeBlocks);
        }

        void AddBlockNoValidate(BlockNode blockNode, ContextData contextData, int index)
        {
            // Regular AddNode path
            AddNodeNoValidate(blockNode);

            // Set BlockNode properties
            blockNode.contextData = contextData;

            // Add to ContextData
            if(index == -1 || index >= contextData.blocks.Count())
            {
                contextData.blocks.Add(blockNode);
            }
            else
            {
                contextData.blocks.Insert(index, blockNode);
            }
        }

        public List<BlockFieldDescriptor> GetActiveBlocksForAllActiveTargets()
        {
            // Get list of active Block types
            var currentBlocks = GetNodes<BlockNode>();
            var context = new TargetActiveBlockContext(currentBlocks.Select(x => x.descriptor).ToList());
            foreach(var target in activeTargets)
            {
                target.GetActiveBlocks(ref context);
            }

            return context.activeBlocks;
        }

        public void UpdateActiveBlocks(List<BlockFieldDescriptor> activeBlockDescriptors)
        {
            // Set Blocks as active based on supported Block list
            foreach(var vertexBlock in vertexContext.blocks)
            {
                vertexBlock.value.isActive = activeBlockDescriptors.Contains(vertexBlock.value.descriptor);
            }
            foreach(var fragmentBlock in fragmentContext.blocks)
            {
                fragmentBlock.value.isActive = activeBlockDescriptors.Contains(fragmentBlock.value.descriptor);
            }
        }

        public void AddRemoveBlocksFromActiveList(List<BlockFieldDescriptor> activeBlockDescriptors)
        {
            var blocksToRemove = ListPool<BlockNode>.Get();

            void GetBlocksToRemoveForContext(ContextData contextData)
            {
                for(int i = 0; i < contextData.blocks.Count; i++)
                {
                    var block = contextData.blocks[i];
                    if(!activeBlockDescriptors.Contains(block.value.descriptor))
                    {
                        var slot = block.value.FindSlot<MaterialSlot>(0);
                        if(!slot.isConnected && slot.isDefaultValue) // TODO: How to check default value
                        {
                            blocksToRemove.Add(block);
                        }
                    }
                }
            }

            void TryAddBlockToContext(BlockFieldDescriptor descriptor, ContextData contextData)
            {
                if(descriptor.shaderStage != contextData.shaderStage)
                    return;

                if(contextData.blocks.Any(x => x.value.descriptor.Equals(descriptor)))
                    return;

                var node = (BlockNode)Activator.CreateInstance(typeof(BlockNode));
                node.Init(descriptor);
                AddBlockNoValidate(node, contextData, contextData.blocks.Count);
            }

            // Get inactive Blocks to remove
            GetBlocksToRemoveForContext(vertexContext);
            GetBlocksToRemoveForContext(fragmentContext);

            // Remove blocks
            foreach(var block in blocksToRemove)
            {
                RemoveNodeNoValidate(block);
            }

            // Add active Blocks not currently in Contexts
            foreach(var descriptor in activeBlockDescriptors)
            {
                TryAddBlockToContext(descriptor, vertexContext);
                TryAddBlockToContext(descriptor, fragmentContext);
            }
        }

        void AddNodeNoValidate(AbstractMaterialNode node)
        {
            if (node.group !=null && !m_GroupItems.ContainsKey(node.group))
            {
                throw new InvalidOperationException("Cannot add a node whose group doesn't exist.");
            }
            node.owner = this;
            m_Nodes.Add(node);
            m_NodeDictionary.Add(node.objectId, node);
            m_AddedNodes.Add(node);
            m_GroupItems[node.group].Add(node);
        }

        public void RemoveNode(AbstractMaterialNode node)
        {
            if (!node.canDeleteNode)
            {
                throw new InvalidOperationException($"Node {node.name} ({node.objectId}) cannot be deleted.");
            }
            RemoveNodeNoValidate(node);
            ValidateGraph();

            if(node is BlockNode blockNode)
            {
                var activeBlocks = GetActiveBlocksForAllActiveTargets();
                UpdateActiveBlocks(activeBlocks);
                blockNode.Dirty(ModificationScope.Graph);
            }
        }

        void RemoveNodeNoValidate(AbstractMaterialNode node)
        {
            if (!m_NodeDictionary.ContainsKey(node.objectId))
            {
                throw new InvalidOperationException("Cannot remove a node that doesn't exist.");
            }

            m_Nodes.Remove(node);
            m_NodeDictionary.Remove(node.objectId);
            messageManager?.RemoveNode(node.objectId);
            m_RemovedNodes.Add(node);

            if (m_GroupItems.TryGetValue(node.group, out var groupItems))
            {
                groupItems.Remove(node);
            }

            if(node is BlockNode blockNode && blockNode.contextData != null)
            {
                // Remove from ContextData
                blockNode.contextData.blocks.Remove(blockNode);
            }
        }

        void AddEdgeToNodeEdges(IEdge edge)
        {
            List<IEdge> inputEdges;
            if (!m_NodeEdges.TryGetValue(edge.inputSlot.node.objectId, out inputEdges))
                m_NodeEdges[edge.inputSlot.node.objectId] = inputEdges = new List<IEdge>();
            inputEdges.Add(edge);

            List<IEdge> outputEdges;
            if (!m_NodeEdges.TryGetValue(edge.outputSlot.node.objectId, out outputEdges))
                m_NodeEdges[edge.outputSlot.node.objectId] = outputEdges = new List<IEdge>();
            outputEdges.Add(edge);
        }

        IEdge ConnectNoValidate(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            var fromNode = fromSlotRef.node;
            var toNode = toSlotRef.node;

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

            var fromSlot = fromNode.FindSlot<MaterialSlot>(fromSlotRef.slotId);
            var toSlot = toNode.FindSlot<MaterialSlot>(toSlotRef.slotId);

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
                    throw new InvalidOperationException($"Node {node.name} ({node.objectId}) cannot be deleted.");
                }
            }

            foreach (var edge in edges.ToArray())
            {
                RemoveEdgeNoValidate(edge);
            }

            foreach (var serializableNode in nodes)
            {
                // Check if it is a Redirect Node
                // Get the edges and then re-create all Edges
                // This only works if it has all the edges.
                // If one edge is already deleted then we can not re-create.
                if (serializableNode is RedirectNodeData redirectNode)
                {
                    redirectNode.GetOutputAndInputSlots(out SlotReference outputSlotRef, out var inputSlotRefs);

                    foreach (SlotReference slot in inputSlotRefs)
                    {
                        ConnectNoValidate(outputSlotRef, slot);
                    }
                }

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

            if(nodes.Any(x => x is BlockNode))
            {
                var activeBlocks = GetActiveBlocksForAllActiveTargets();
                UpdateActiveBlocks(activeBlocks);
            }
        }

        void RemoveEdgeNoValidate(IEdge e)
        {
            e = m_Edges.FirstOrDefault(x => x.Equals(e));
            if (e == null)
                throw new ArgumentException("Trying to remove an edge that does not exist.", "e");
            m_Edges.Remove(e as Edge);

            List<IEdge> inputNodeEdges;
            if (m_NodeEdges.TryGetValue(e.inputSlot.node.objectId, out inputNodeEdges))
                inputNodeEdges.Remove(e);

            List<IEdge> outputNodeEdges;
            if (m_NodeEdges.TryGetValue(e.outputSlot.node.objectId, out outputNodeEdges))
                outputNodeEdges.Remove(e);

            m_RemovedEdges.Add(e);
        }

        public AbstractMaterialNode GetNodeFromId(string nodeId)
        {
            m_NodeDictionary.TryGetValue(nodeId, out var node);
            return node;
        }

        public T GetNodeFromId<T>(string nodeId) where T : class
        {
            m_NodeDictionary.TryGetValue(nodeId, out var node);
            return node as T;
        }

        public bool ContainsNode(AbstractMaterialNode node)
        {
            if(node == null)
                return false;

            return m_NodeDictionary.TryGetValue(node.objectId, out var foundNode) && node == foundNode;
        }

        public void GetEdges(SlotReference s, List<IEdge> foundEdges)
        {
            MaterialSlot slot = s.slot;

            List<IEdge> candidateEdges;
            if (!m_NodeEdges.TryGetValue(s.node.objectId, out candidateEdges))
                return;

            foreach (var edge in candidateEdges)
            {
                var cs = slot.isInputSlot ? edge.inputSlot : edge.outputSlot;
                if (cs.node == s.node && cs.slotId == s.slotId)
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

        public void AddGraphInput(ShaderInput input, int index = -1)
        {
            if (input == null)
                return;

            switch(input)
            {
                case AbstractShaderProperty property:
                    if (m_Properties.Contains(property))
                        return;

                    if (index < 0)
                        m_Properties.Add(property);
                    else
                        m_Properties.Insert(index, property);

                    break;
                case ShaderKeyword keyword:
                    if (m_Keywords.Contains(keyword))
                        return;

                    if (index < 0)
                        m_Keywords.Add(keyword);
                    else
                        m_Keywords.Insert(index, keyword);

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
                    input.displayName = GraphUtil.SanitizeName(properties.Where(p => p != input).Select(p => p.displayName), "{0} ({1})", input.displayName);
                    break;
                case ShaderKeyword keyword:
                    input.displayName = GraphUtil.SanitizeName(keywords.Where(p => p != input).Select(p => p.displayName), "{0} ({1})", input.displayName);
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

            if (Regex.IsMatch(name, @"^\d+"))
                name = "_" + name;

            name = Regex.Replace(name, @"(?:[^A-Za-z_0-9])|(?:\s)", "_");
            switch(input)
            {
                case AbstractShaderProperty property:
                    property.overrideReferenceName = GraphUtil.SanitizeName(properties.Where(p => p != property).Select(p => p.referenceName), "{0}_{1}", name);
                    break;
                case ShaderKeyword keyword:
                    keyword.overrideReferenceName = GraphUtil.SanitizeName(keywords.Where(p => p != input).Select(p => p.referenceName), "{0}_{1}", name).ToUpper();
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
                    var propertyNodes = GetNodes<PropertyNode>().Where(x => x.property == input).ToList();
                    foreach (var propertyNode in propertyNodes)
                        ReplacePropertyNodeWithConcreteNodeNoValidate(propertyNode);
                    break;
            }

            RemoveGraphInputNoValidate(input);
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

        void RemoveGraphInputNoValidate(ShaderInput shaderInput)
        {
            if (shaderInput is AbstractShaderProperty property && m_Properties.Remove(property) ||
                shaderInput is ShaderKeyword keyword && m_Keywords.Remove(keyword))
            {
                m_RemovedInputs.Add(shaderInput);
                m_AddedInputs.Remove(shaderInput);
                m_MovedInputs.Remove(shaderInput);
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
            var property = properties.FirstOrDefault(x => x == propertyNode.property);
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
            node.group = propertyNode.group;
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

        public void CleanupGraph()
        {
            //First validate edges, remove any
            //orphans. This can happen if a user
            //manually modifies serialized data
            //of if they delete a node in the inspector
            //debug view.
            foreach (var edge in edges.ToArray())
            {
                var outputNode = edge.outputSlot.node;
                var inputNode = edge.inputSlot.node;

                MaterialSlot outputSlot = null;
                MaterialSlot inputSlot = null;
                if (ContainsNode(outputNode) && ContainsNode(inputNode))
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
        }

        public void ValidateGraph()
        {
            messageManager?.ClearAllFromProvider(this);
            CleanupGraph();
            GraphSetup.SetupGraph(this);
            GraphConcretization.ConcretizeGraph(this);
            GraphValidation.ValidateGraph(this);

            foreach (var edge in m_AddedEdges.ToList())
            {
                if (!ContainsNode(edge.outputSlot.node) || !ContainsNode(edge.inputSlot.node))
                {
                    Debug.LogWarningFormat("Added edge is invalid: {0} -> {1}\n{2}", edge.outputSlot.node.objectId, edge.inputSlot.node.objectId, Environment.StackTrace);
                    m_AddedEdges.Remove(edge);
                }
            }

            foreach (var groupChange in m_ParentGroupChanges.ToList())
            {
                if (groupChange.groupItem is AbstractMaterialNode node && !ContainsNode(node))
                {
                    m_ParentGroupChanges.Remove(groupChange);
                }

                if (groupChange.groupItem is StickyNoteData stickyNote && !m_StickyNoteDatas.Contains(stickyNote))
                {
                    m_ParentGroupChanges.Remove(groupChange);
                }
            }
        }

        public void AddValidationError(string id, string errorMessage,
            ShaderCompilerMessageSeverity severity = ShaderCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new ShaderMessage("Validation: " + errorMessage, severity));
        }

        public void AddSetupError(string id, string errorMessage,
            ShaderCompilerMessageSeverity severity = ShaderCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new ShaderMessage("Setup: " + errorMessage, severity));
        }

        public void AddConcretizationError(string id, string errorMessage,
            ShaderCompilerMessageSeverity severity = ShaderCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new ShaderMessage("Concretization: " + errorMessage, severity));
        }

        public void ClearErrorsForNode(AbstractMaterialNode node)
        {
            messageManager?.ClearNodesFromProvider(this, node.ToEnumerable());
        }

        public void ReplaceWith(GraphData other)
        {
            if (other == null)
                throw new ArgumentException("Can only replace with another AbstractMaterialGraph", "other");

            using (var inputsToRemove = PooledList<ShaderInput>.Get())
            {
                foreach (var property in m_Properties.SelectValue())
                    inputsToRemove.Add(property);
                foreach (var keyword in m_Keywords.SelectValue())
                    inputsToRemove.Add(keyword);
                foreach (var input in inputsToRemove)
                    RemoveGraphInputNoValidate(input);
            }
            foreach (var otherProperty in other.properties)
            {
                AddGraphInput(otherProperty);
            }
            foreach (var otherKeyword in other.keywords)
            {
                AddGraphInput(otherKeyword);
            }

            other.ValidateGraph();
            ValidateGraph();

            // Current tactic is to remove all nodes and edges and then re-add them, such that depending systems
            // will re-initialize with new references.

            using (var removedGroupsPooledObject = ListPool<GroupData>.GetDisposable())
            {
                var removedGroupDatas = removedGroupsPooledObject.value;
                removedGroupDatas.AddRange(m_GroupDatas.SelectValue());
                foreach (var groupData in removedGroupDatas)
                {
                    RemoveGroupNoValidate(groupData);
                }
            }

            using (var removedNotesPooledObject = ListPool<StickyNoteData>.GetDisposable())
            {
                var removedNoteDatas = removedNotesPooledObject.value;
                removedNoteDatas.AddRange(m_StickyNoteDatas.SelectValue());
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

            using (var nodesToRemove = PooledList<AbstractMaterialNode>.Get())
            {
                nodesToRemove.AddRange(m_Nodes.SelectValue());
                foreach (var node in nodesToRemove)
                    RemoveNodeNoValidate(node);
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
                    var contextData = blockNode.descriptor.shaderStage == ShaderStage.Vertex ? vertexContext : fragmentContext;
                    AddBlockNoValidate(blockNode, contextData, blockNode.index);
                }
                else
                {
                    AddNodeNoValidate(node);
                }
            }

            foreach (var edge in other.edges)
            {
                ConnectNoValidate(edge.outputSlot, edge.inputSlot);
            }

            outputNode = other.outputNode;

            // Copy all targets
            m_ActiveTargets.Clear();
            foreach(var target in other.activeTargets)
            {
                // Ensure target inits correctly
                var context = new TargetSetupContext();
                target.Setup(ref context);
                m_ActiveTargets.Add(target);
            }

            // Active blocks
            var activeBlocks = GetActiveBlocksForAllActiveTargets();
            UpdateActiveBlocks(activeBlocks);
            ValidateGraph();

            // TODO: Internal inspector must repaint here
            // TODO: Check with Sai that it does...
            if(onUndoPerformed != null)
                onUndoPerformed();
        }

        // TODO: Temporary hack to repaint inspector on Undo
        // TODO: Make sure this is handled properly by InspectorView then removed
        public delegate void OnUndoPerformed();
        public OnUndoPerformed onUndoPerformed;

        internal void PasteGraph(CopyPasteGraph graphToPaste, List<AbstractMaterialNode> remappedNodes,
            List<Edge> remappedEdges)
        {
            var groupMap = new Dictionary<GroupData, GroupData>();
            foreach (var group in graphToPaste.groups)
            {
                var position = group.position;
                position.x += 30;
                position.y += 30;

                GroupData newGroup = new GroupData(group.title, position);

                groupMap[group] = newGroup;

                AddGroup(newGroup);
                m_PastedGroups.Add(newGroup);
            }

            foreach (var stickyNote in graphToPaste.stickyNotes)
            {
                var position = stickyNote.position;
                position.x += 30;
                position.y += 30;

                StickyNoteData pastedStickyNote = new StickyNoteData(stickyNote.title, stickyNote.content, position);
                if (groupMap.ContainsKey(stickyNote.group))
                {
                    pastedStickyNote.group = groupMap[stickyNote.group];
                }

                AddStickyNote(pastedStickyNote);
                m_PastedStickyNotes.Add(pastedStickyNote);
            }

            var edges = graphToPaste.edges.ToList();
            var nodeList = graphToPaste.GetNodes<AbstractMaterialNode>();
            foreach (var node in nodeList)
            {
                if(node is BlockNode blockNode)
                {
                    continue;
                }

                AbstractMaterialNode pastedNode = node;

                // Check if the property nodes need to be made into a concrete node.
                if (node is PropertyNode propertyNode)
                {
                    // If the property is not in the current graph, do check if the
                    // property can be made into a concrete node.
                    var index = graphToPaste.metaProperties.TakeWhile(x => x != propertyNode.property).Count();
                    var originalId = graphToPaste.metaPropertyIds.ElementAt(index);
                    var property = m_Properties.SelectValue().FirstOrDefault(x => x.objectId == originalId);
                    if (property != null)
                    {
                        propertyNode.property = property;
                    }
                    else
                    {
                        pastedNode = propertyNode.property.ToConcreteNode();
                        pastedNode.drawState = node.drawState;
                        for (var i = 0; i < edges.Count; i++)
                        {
                            var edge = edges[i];
                            if (edge.outputSlot.node == node)
                            {
                                edges[i] = new Edge(new SlotReference(pastedNode, edge.outputSlot.slotId), edge.inputSlot);
                            }
                            else if (edge.inputSlot.node == node)
                            {
                                edges[i] = new Edge(edge.outputSlot, new SlotReference(pastedNode, edge.inputSlot.slotId));
                            }
                        }
                    }
                }

                // If the node has a group guid and no group has been copied, reset the group guid.
                // Check if the node is inside a group
                if (node.group != null)
                {
                    if (groupMap.ContainsKey(node.group))
                    {
                        var absNode = pastedNode;
                        absNode.group = groupMap[node.group];
                        pastedNode = absNode;
                    }
                    else
                    {
                        pastedNode.group = null;
                    }
                }

                remappedNodes.Add(pastedNode);
                AddNode(pastedNode);

                // add the node to the pasted node list
                m_PastedNodes.Add(pastedNode);

                // Check if the keyword nodes need to have their keywords copied.
                if (node is KeywordNode keywordNode)
                {
                    var index = graphToPaste.metaKeywords.TakeWhile(x => x != keywordNode.keyword).Count();
                    var originalId = graphToPaste.metaKeywordIds.ElementAt(index);
                    var keyword = m_Keywords.SelectValue().FirstOrDefault(x => x.objectId == originalId);
                    if (keyword != null)
                    {
                        keywordNode.keyword = keyword;
                    }
                    else
                    {
                        SanitizeGraphInputName(keywordNode.keyword);
                        SanitizeGraphInputReferenceName(keywordNode.keyword, keywordNode.keyword.overrideReferenceName);
                        AddGraphInput(keywordNode.keyword);
                    }

                    // Always update Keyword nodes to handle any collisions resolved on the Keyword
                    keywordNode.UpdateNode();
                }
            }

            foreach (var edge in edges)
            {
                var newEdge = (Edge)Connect(edge.outputSlot, edge.inputSlot);
                if (newEdge != null)
                {
                    remappedEdges.Add(newEdge);
                }
            }

            ValidateGraph();
        }

        public override void OnBeforeSerialize()
        {
            m_Edges.Sort();
            m_Version = k_CurrentVersion;
        }

        static JsonObject DeserializeLegacy(string typeString, string json)
        {
            var value = MultiJsonInternal.CreateInstance(typeString);
            if (value == null)
            {
                Debug.Log($"Cannot create instance for {typeString}");
                return null;
            }

            MultiJsonInternal.Enqueue(value, json);

            return value;
        }

        public override void OnAfterDeserialize(string json)
        {
            if (m_Version == 0)
            {
                var graphData0 = JsonUtility.FromJson<GraphData0>(json);

                var nodeGuidMap = new Dictionary<string, AbstractMaterialNode>();
                var propertyGuidMap = new Dictionary<string, AbstractShaderProperty>();
                var keywordGuidMap = new Dictionary<string, ShaderKeyword>();
                var groupGuidMap = new Dictionary<string, GroupData>();
                var slotsField = typeof(AbstractMaterialNode).GetField("m_Slots", BindingFlags.Instance | BindingFlags.NonPublic);
                var propertyField = typeof(PropertyNode).GetField("m_Property", BindingFlags.Instance | BindingFlags.NonPublic);
                var keywordField = typeof(KeywordNode).GetField("m_Keyword", BindingFlags.Instance | BindingFlags.NonPublic);
                var defaultReferenceNameField = typeof(ShaderInput).GetField("m_DefaultReferenceName", BindingFlags.Instance | BindingFlags.NonPublic);

                m_GroupDatas.Clear();
                m_StickyNoteDatas.Clear();

                foreach (var group0 in graphData0.m_Groups)
                {
                    var group = new GroupData(group0.m_Title, group0.m_Position);
                    m_GroupDatas.Add(group);
                    if (!groupGuidMap.ContainsKey(group0.m_GuidSerialized))
                    {
                        groupGuidMap.Add(group0.m_GuidSerialized, group);
                    }
                    else if (!groupGuidMap[group0.m_GuidSerialized].Equals(group.objectId))
                    {
                        Debug.LogError("Group id mismatch");
                    }
                }

                foreach (var serializedProperty in graphData0.m_SerializedProperties)
                {
                    var property = (AbstractShaderProperty)DeserializeLegacy(serializedProperty.typeInfo.fullName, serializedProperty.JSONnodeData);
                    if (property == null)
                    {
                        continue;
                    }

                    m_Properties.Add(property);

                    var input0 = JsonUtility.FromJson<ShaderInput0>(serializedProperty.JSONnodeData);
                    propertyGuidMap[input0.m_Guid.m_GuidSerialized] = property;

                    // Fix up missing reference names
                    // Properties on Sub Graphs in V0 never have reference names serialized
                    // To maintain Sub Graph node property mapping we force guid based reference names on upgrade
                    if (string.IsNullOrEmpty((string)defaultReferenceNameField.GetValue(property)))
                    {
                        // ColorShaderProperty is the only Property case where `GetDefaultReferenceName` was overriden
                        if (MultiJson.ParseType(serializedProperty.typeInfo.fullName) == typeof(ColorShaderProperty))
                        {
                            defaultReferenceNameField.SetValue(property, $"Color_{GuidEncoder.Encode(Guid.Parse(input0.m_Guid.m_GuidSerialized))}");
                        }
                        else
                        {
                            defaultReferenceNameField.SetValue(property, $"{property.concreteShaderValueType}_{GuidEncoder.Encode(Guid.Parse(input0.m_Guid.m_GuidSerialized))}");
                        }
                    }
                }

                foreach (var serializedKeyword in graphData0.m_SerializedKeywords)
                {
                    var keyword = (ShaderKeyword)DeserializeLegacy(serializedKeyword.typeInfo.fullName, serializedKeyword.JSONnodeData);
                    if (keyword == null)
                    {
                        continue;
                    }

                    m_Keywords.Add(keyword);

                    var input0 = JsonUtility.FromJson<ShaderInput0>(serializedKeyword.JSONnodeData);
                    keywordGuidMap[input0.m_Guid.m_GuidSerialized] = keyword;
                }

                foreach (var serializedNode in graphData0.m_SerializableNodes)
                {
                    var node0 = JsonUtility.FromJson<AbstractMaterialNode0>(serializedNode.JSONnodeData);

                    var node = (AbstractMaterialNode)DeserializeLegacy(serializedNode.typeInfo.fullName, serializedNode.JSONnodeData);
                    if (node == null)
                    {
                        continue;
                    }

                    nodeGuidMap.Add(node0.m_GuidSerialized, node);
                    m_Nodes.Add(node);

                    if (!string.IsNullOrEmpty(node0.m_PropertyGuidSerialized) && propertyGuidMap.TryGetValue(node0.m_PropertyGuidSerialized, out var property))
                    {
                        propertyField.SetValue(node, (JsonRef<AbstractShaderProperty>)property);
                    }

                    if (!string.IsNullOrEmpty(node0.m_KeywordGuidSerialized) && keywordGuidMap.TryGetValue(node0.m_KeywordGuidSerialized, out var keyword))
                    {
                        keywordField.SetValue(node, (JsonRef<ShaderKeyword>)keyword);
                    }

                    var slots = (List<JsonData<MaterialSlot>>)slotsField.GetValue(node);
                    slots.Clear();

                    foreach (var serializedSlot in node0.m_SerializableSlots)
                    {
                        var slot = (MaterialSlot)DeserializeLegacy(serializedSlot.typeInfo.fullName, serializedSlot.JSONnodeData);
                        if (slot == null)
                        {
                            continue;
                        }

                        slots.Add(slot);
                    }

                    if(!String.IsNullOrEmpty(node0.m_GroupGuidSerialized))
                    {
                        if(groupGuidMap.TryGetValue(node0.m_GroupGuidSerialized, out GroupData foundGroup))
                        {
                            node.group = foundGroup;
                        }
                    }
                }

                foreach (var stickyNote0 in graphData0.m_StickyNotes)
                {
                    var stickyNote = new StickyNoteData(stickyNote0.m_Title, stickyNote0.m_Content, stickyNote0.m_Position);
                    if(!String.IsNullOrEmpty(stickyNote0.m_GroupGuidSerialized))
                    {
                        if(groupGuidMap.TryGetValue(stickyNote0.m_GroupGuidSerialized, out GroupData foundGroup))
                        {
                            stickyNote.group = foundGroup;
                        }
                    }
                    stickyNote.theme = stickyNote0.m_Theme;
                    stickyNote.textSize = stickyNote0.m_TextSize;
                    m_StickyNoteDatas.Add(stickyNote);
                }

                var subgraphOuput = GetNodes<SubGraphOutputNode>();
                isSubGraph = subgraphOuput.Any();

                if (isSubGraph)
                {
                    m_OutputNode = subgraphOuput.FirstOrDefault();
                }
                else if (!string.IsNullOrEmpty(graphData0.m_ActiveOutputNodeGuidSerialized))
                {
                    m_OutputNode = nodeGuidMap[graphData0.m_ActiveOutputNodeGuidSerialized];
                }
                else
                {
                    m_OutputNode = (AbstractMaterialNode)GetNodes<IMasterNode1>().FirstOrDefault();
                }

                foreach (var serializedElement in graphData0.m_SerializableEdges)
                {
                    var edge0 = JsonUtility.FromJson<Edge0>(serializedElement.JSONnodeData);
                    m_Edges.Add(new Edge(
                        new SlotReference(
                            nodeGuidMap[edge0.m_OutputSlot.m_NodeGUIDSerialized],
                            edge0.m_OutputSlot.m_SlotId),
                        new SlotReference(
                            nodeGuidMap[edge0.m_InputSlot.m_NodeGUIDSerialized],
                            edge0.m_InputSlot.m_SlotId)));
                }
            }

            // In V2 we need to defer version set to in OnAfterMultiDeserialize
            // This is because we need access to m_OutputNode to convert it to Targets and Stacks
            // The JsonObject will not be fully deserialized until OnAfterMultiDeserialize
            bool deferredUpgrades = m_Version < 2;
            if(!deferredUpgrades)
            {
                m_Version = k_CurrentVersion;
            }
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            // Deferred upgrades
            if(m_Version != k_CurrentVersion)
            {
                if(m_Version < 2)
                {
                    var addedBlocks = ListPool<BlockFieldDescriptor>.Get();

                    void UpgradeFromBlockMap(Dictionary<BlockFieldDescriptor, int> blockMap)
                    {
                        // Map master node ports to blocks
                        if(blockMap != null)
                        {
                            foreach(var blockMapping in blockMap)
                            {
                                // Create a new BlockNode for each unique map entry
                                var descriptor = blockMapping.Key;
                                if(addedBlocks.Contains(descriptor))
                                    continue;

                                addedBlocks.Add(descriptor);

                                var contextData = descriptor.shaderStage == ShaderStage.Fragment ? m_FragmentContext : m_VertexContext;
                                var block = (BlockNode)Activator.CreateInstance(typeof(BlockNode));
                                block.Init(descriptor);
                                AddBlockNoValidate(block, contextData, contextData.blocks.Count);

                                // To avoid having to go around the following deserialization code
                                // We simply run OnBeforeSerialization here to ensure m_SerializedDescriptor is set
                                block.OnBeforeSerialize();

                                // Now remap the incoming edges to blocks
                                var slotId = blockMapping.Value;
                                var oldSlot = m_OutputNode.value.FindSlot<MaterialSlot>(slotId);
                                var newSlot = block.FindSlot<MaterialSlot>(0);
                                if(oldSlot == null)
                                    continue;

                                var oldInputSlotRef = m_OutputNode.value.GetSlotReference(slotId);
                                var newInputSlotRef = block.GetSlotReference(0);

                                // Always copy the value over for convenience
                                newSlot.CopyValuesFrom(oldSlot);

                                for(int i = 0; i < m_Edges.Count; i++)
                                {
                                    // Find all edges connected to the master node using slot ID from the block map
                                    // Remove them and replace them with new edges connected to the block nodes
                                    var edge = m_Edges[i];
                                    if(edge.inputSlot.Equals(oldInputSlotRef))
                                    {
                                        var outputSlot = edge.outputSlot;
                                        m_Edges.Remove(edge);
                                        m_Edges.Add(new Edge(outputSlot, newInputSlotRef));
                                    }
                                }
                            }

                            // We need to call AddBlockNoValidate but this adds to m_AddedNodes resulting in duplicates
                            // Therefore we need to clear this list before the view is created
                            m_AddedNodes.Clear();
                        }
                    }

                    var masterNode = m_OutputNode.value as IMasterNode1;

                    // This is required for edge lookup during Target upgrade
                    m_OutputNode.value.owner = this;
                    foreach (var edge in m_Edges)
                    {
                        AddEdgeToNodeEdges(edge);
                    }

                    // Ensure correct initialization of Contexts
                    AddContexts();

                    // Position Contexts to the match master node
                    var oldPosition = m_OutputNode.value.drawState.position.position;
                    m_VertexContext.position = oldPosition;
                    m_FragmentContext.position = new Vector2(oldPosition.x, oldPosition.y + 200);

                    // Try to upgrade all valid targets from master node
                    // On ShaderGraph side we dont know what Targets exist so make no assumptions
                    if(masterNode != null)
                    {
                        foreach(var target in m_ValidTargets)
                        {
                            if(!(target is ILegacyTarget legacyTarget))
                                continue;

                            if(!legacyTarget.TryUpgradeFromMasterNode(masterNode, out var newBlockMap))
                                continue;

                            m_ActiveTargets.Add(target);
                            UpgradeFromBlockMap(newBlockMap);
                        }
                    }

                    // Clean up after upgrade
                    if(!isSubGraph)
                    {
                        m_OutputNode = null;
                    }

                    var masterNodes = GetNodes<IMasterNode1>().ToArray();
                    for(int i = 0; i < masterNodes.Length; i++)
                    {
                        var node = masterNodes.ElementAt(i) as AbstractMaterialNode;
                        m_Nodes.Remove(node);
                    }

                    m_NodeEdges.Clear();
                }

                m_Version = k_CurrentVersion;
            }

            m_NodeDictionary = new Dictionary<string, AbstractMaterialNode>(m_Nodes.Count);

            foreach (var group in m_GroupDatas.SelectValue())
            {
                m_GroupItems.Add(group, new List<IGroupItem>());
            }

            foreach (var node in m_Nodes.SelectValue())
            {
                node.owner = this;
                node.UpdateNodeAfterDeserialization();
                m_NodeDictionary.Add(node.objectId, node);
                if (m_GroupItems.TryGetValue(node.group, out var groupItems))
                {
                    groupItems.Add(node);
                }
                else
                {
                    node.group = null;
                }
            }

            foreach (var stickyNote in m_StickyNoteDatas.SelectValue())
            {
                if (m_GroupItems.TryGetValue(stickyNote.group, out var groupItems))
                {
                    groupItems.Add(stickyNote);
                }
                else
                {
                    stickyNote.group = null;
                }
            }

            foreach (var edge in m_Edges)
                AddEdgeToNodeEdges(edge);

            // --------------------------------------------------
            // Deserialize Contexts & Blocks

            void DeserializeContextData(ContextData contextData, ShaderStage stage)
            {
                // Because Vertex/Fragment Contexts are serialized explicitly
                // we do not need to serialize the Stage value on the ContextData
                contextData.shaderStage = stage;

                var blocks = contextData.blocks.SelectValue().ToList();
                var blockCount = blocks.Count;
                for(int i = 0; i < blockCount; i++)
                {
                    // Update NonSerialized data on the BlockNode
                    var block = blocks[i];
                    block.descriptor = m_BlockFieldDescriptors.FirstOrDefault(x => $"{x.tag}.{x.name}" == block.serializedDescriptor);
                    block.contextData = contextData;
                }
            }

            // First deserialize the ContextDatas
            DeserializeContextData(m_VertexContext, ShaderStage.Vertex);
            DeserializeContextData(m_FragmentContext, ShaderStage.Fragment);

            foreach(var target in m_ActiveTargets.SelectValue())
            {
                var activeTargetCurrent = m_ValidTargets.FirstOrDefault(x => x.GetType() == target.GetType());
                var targetIndex = m_ValidTargets.IndexOf(activeTargetCurrent);
                m_ActiveTargetBitmask = m_ActiveTargetBitmask | (1 << targetIndex);
                m_ValidTargets[targetIndex] = target;
            }

            UpdateActiveTargets();
        }

        public void OnEnable()
        {
            foreach (var node in GetNodes<AbstractMaterialNode>().OfType<IOnAssetEnabled>())
            {
                node.OnEnable();
            }

            ShaderGraphPreferences.onVariantLimitChanged += OnKeywordChanged;
        }

        public void OnDisable()
        {
            ShaderGraphPreferences.onVariantLimitChanged -= OnKeywordChanged;
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
