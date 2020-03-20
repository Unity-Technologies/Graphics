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

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.MaterialGraph")]
    [FormerName("UnityEditor.ShaderGraph.SubGraph")]
    [FormerName("UnityEditor.ShaderGraph.AbstractMaterialGraph")]
    sealed class GraphData : JsonObject
    {
        const int k_CurrentVersion = 1;

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

        #region Targets
        [NonSerialized]
        List<ITarget> m_ValidTargets = new List<ITarget>();

        [NonSerialized]
        List<ITargetImplementation> m_ValidImplementations = new List<ITargetImplementation>();

        public List<ITargetImplementation> validImplementations => m_ValidImplementations;
        #endregion

        public bool didActiveOutputNodeChange { get; set; }

        internal delegate void SaveGraphDelegate(Shader shader, object context);
        internal static SaveGraphDelegate onSaveGraph;

        public GraphData()
        {
            m_GroupItems[Guid.Empty] = new List<IGroupItem>();
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

        void AddNodeNoValidate(AbstractMaterialNode node)
        {
            if (node.groupGuid != Guid.Empty && !m_GroupItems.ContainsKey(node.groupGuid))
            {
                throw new InvalidOperationException("Cannot add a node whose group doesn't exist.");
            }
            node.owner = this;
            m_Nodes.Add(node);
            m_NodeDictionary.Add(node.objectId, node);
            m_AddedNodes.Add(node);
            m_GroupItems[node.groupGuid].Add(node);
        }

        public void RemoveNode(AbstractMaterialNode node)
        {
            if (!node.canDeleteNode)
            {
                throw new InvalidOperationException($"Node {node.name} ({node.objectId}) cannot be deleted.");
            }
            RemoveNodeNoValidate(node);
            ValidateGraph();
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

            if (m_GroupItems.TryGetValue(node.groupGuid, out var groupItems))
            {
                groupItems.Remove(node);
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
            if (m_NodeEdges.TryGetValue(e.inputSlot.node.objectId, out inputNodeEdges))
                inputNodeEdges.Remove(e);

            List<IEdge> outputNodeEdges;
            if (m_NodeEdges.TryGetValue(e.outputSlot.node.objectId, out outputNodeEdges))
                outputNodeEdges.Remove(e);

            m_RemovedEdges.Add(e);
        }

        public AbstractMaterialNode GetNodeFromId(string nodeId)
        {
            if (m_NodeDictionary.TryGetValue(nodeId, out var node))
            {
                return node;
            }
            return null;
        }

        public T GetNodeFromId<T>(string nodeId) where T : class
        {
            return m_NodeDictionary[nodeId] as T;
        }

        public bool ContainsNode(AbstractMaterialNode node)
        {
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
            if (m_Properties.RemoveAll(x => x.value.guid == guid) > 0 ||
                m_Keywords.RemoveAll(x => x.value.guid == guid) > 0)
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
            var propertyNodes = GetNodes<PropertyNode>().Where(n => !m_Properties.Any(p => p.value.guid == n.propertyGuid)).ToArray();
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

            var temporaryMarks = PooledHashSet<string>.Get();
            var permanentMarks = PooledHashSet<string>.Get();
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
                if (permanentMarks.Contains(node.objectId))
                {
                    continue;
                }

                if (temporaryMarks.Contains(node.objectId))
                {
                    node.ValidateNode();
                    permanentMarks.Add(node.objectId);
                }
                else
                {
                    temporaryMarks.Add(node.objectId);
                    stack.Push(node);
                    node.GetInputSlots(slots);
                    foreach (var inputSlot in slots)
                    {
                        var nodeEdges = GetEdges(inputSlot.slotReference);
                        foreach (var edge in nodeEdges)
                        {
                            var fromSocketRef = edge.outputSlot;
                            var childNode = fromSocketRef.node;
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
            temporaryMarks.Dispose();
            permanentMarks.Dispose();

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

                if (groupChange.groupItem is StickyNoteData stickyNote && !m_StickyNotes.Contains(stickyNote))
                {
                    m_ParentGroupChanges.Remove(groupChange);
                }
            }
        }

        public void AddValidationError(string id, string errorMessage,
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
                foreach (var property in m_Properties.SelectValue())
                    removedInputGuids.Add(property.guid);
                foreach (var keyword in m_Keywords.SelectValue())
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
                AddNodeNoValidate(node);

            foreach (var edge in other.edges)
            {
                ConnectNoValidate(edge.outputSlot, edge.inputSlot);
            }

            outputNode = other.outputNode;

            ValidateGraph();
        }

        internal void PasteGraph(CopyPasteGraph graphToPaste, List<AbstractMaterialNode> remappedNodes, List<IEdge> remappedEdges)
        {
            // var groupGuidMap = new Dictionary<Guid, Guid>();
            // foreach (var group in graphToPaste.groups)
            // {
            //     var position = group.position;
            //     position.x += 30;
            //     position.y += 30;
            //
            //     GroupData newGroup = new GroupData(group.title, position);
            //
            //     var oldGuid = group.guid;
            //     var newGuid = newGroup.guid;
            //     groupGuidMap[oldGuid] = newGuid;
            //
            //     AddGroup(newGroup);
            //     m_PastedGroups.Add(newGroup);
            // }
            //
            // foreach (var stickyNote in graphToPaste.stickyNotes)
            // {
            //     var position = stickyNote.position;
            //     position.x += 30;
            //     position.y += 30;
            //
            //     StickyNoteData pastedStickyNote = new StickyNoteData(stickyNote.title, stickyNote.content, position);
            //     if (groupGuidMap.ContainsKey(stickyNote.groupGuid))
            //     {
            //         pastedStickyNote.groupGuid = groupGuidMap[stickyNote.groupGuid];
            //     }
            //
            //     AddStickyNote(pastedStickyNote);
            //     m_PastedStickyNotes.Add(pastedStickyNote);
            // }
            //
            // var nodeGuidMap = new Dictionary<Guid, Guid>();
            // foreach (var node in graphToPaste.GetNodes<AbstractMaterialNode>())
            // {
            //     AbstractMaterialNode pastedNode = node;
            //
            //     var oldGuid = node.guid;
            //     var newGuid = node.RewriteGuid();
            //     nodeGuidMap[oldGuid] = newGuid;
            //
            //     // Check if the property nodes need to be made into a concrete node.
            //     if (node is PropertyNode propertyNode)
            //     {
            //         // If the property is not in the current graph, do check if the
            //         // property can be made into a concrete node.
            //         if (!m_Properties.Select(x => x.guid).Contains(propertyNode.propertyGuid))
            //         {
            //             // If the property is in the serialized paste graph, make the property node into a property node.
            //             var pastedGraphMetaProperties = graphToPaste.metaProperties.Where(x => x.guid == propertyNode.propertyGuid);
            //             if (pastedGraphMetaProperties.Any())
            //             {
            //                 pastedNode = pastedGraphMetaProperties.FirstOrDefault().ToConcreteNode();
            //                 pastedNode.drawState = node.drawState;
            //                 nodeGuidMap[oldGuid] = pastedNode.guid;
            //             }
            //         }
            //     }
            //
            //     AbstractMaterialNode abstractMaterialNode = (AbstractMaterialNode)node;
            //
            //     // If the node has a group guid and no group has been copied, reset the group guid.
            //     // Check if the node is inside a group
            //     if (abstractMaterialNode.groupGuid != Guid.Empty)
            //     {
            //         if (groupGuidMap.ContainsKey(abstractMaterialNode.groupGuid))
            //         {
            //             var absNode = pastedNode as AbstractMaterialNode;
            //             absNode.groupGuid = groupGuidMap[abstractMaterialNode.groupGuid];
            //             pastedNode = absNode;
            //         }
            //         else
            //         {
            //             pastedNode.groupGuid = Guid.Empty;
            //         }
            //     }
            //
            //     var drawState = node.drawState;
            //     var position = drawState.position;
            //     position.x += 30;
            //     position.y += 30;
            //     drawState.position = position;
            //     node.drawState = drawState;
            //     remappedNodes.Add(pastedNode);
            //     AddNode(pastedNode);
            //
            //     // add the node to the pasted node list
            //     m_PastedNodes.Add(pastedNode);
            //
            //     // Check if the keyword nodes need to have their keywords copied.
            //     if (node is KeywordNode keywordNode)
            //     {
            //         // If the keyword is not in the current graph and is in the serialized paste graph copy it.
            //         if (!keywords.Select(x => x.guid).Contains(keywordNode.keywordGuid))
            //         {
            //             var pastedGraphMetaKeywords = graphToPaste.metaKeywords.Where(x => x.guid == keywordNode.keywordGuid);
            //             if (pastedGraphMetaKeywords.Any())
            //             {
            //                 var keyword = pastedGraphMetaKeywords.FirstOrDefault(x => x.guid == keywordNode.keywordGuid);
            //                 SanitizeGraphInputName(keyword);
            //                 SanitizeGraphInputReferenceName(keyword, keyword.overrideReferenceName);
            //                 AddGraphInput(keyword);
            //             }
            //         }
            //
            //         // Always update Keyword nodes to handle any collisions resolved on the Keyword
            //         keywordNode.UpdateNode();
            //     }
            // }
            //
            // // only connect edges within pasted elements, discard
            // // external edges.
            // foreach (var edge in graphToPaste.edges)
            // {
            //     var outputSlot = edge.outputSlot;
            //     var inputSlot = edge.inputSlot;
            //
            //     Guid remappedOutputNodeGuid;
            //     Guid remappedInputNodeGuid;
            //     if (nodeGuidMap.TryGetValue(outputSlot.nodeGuid, out remappedOutputNodeGuid)
            //         && nodeGuidMap.TryGetValue(inputSlot.nodeGuid, out remappedInputNodeGuid))
            //     {
            //         var outputSlotRef = new SlotReference(remappedOutputNodeGuid, outputSlot.slotId);
            //         var inputSlotRef = new SlotReference(remappedInputNodeGuid, inputSlot.slotId);
            //         remappedEdges.Add(Connect(outputSlotRef, inputSlotRef));
            //     }
            // }
            //
            // ValidateGraph();
        }

        public override void OnBeforeSerialize()
        {
            m_Edges.Sort();
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
                var slotsField = typeof(AbstractMaterialNode).GetField("m_Slots", BindingFlags.Instance | BindingFlags.NonPublic);

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
                }

                foreach (var serializedProperty in graphData0.m_SerializedProperties)
                {
                    var property = (AbstractShaderProperty)DeserializeLegacy(serializedProperty.typeInfo.fullName, serializedProperty.JSONnodeData);
                    if (property == null)
                    {
                        continue;
                    }

                    m_Properties.Add(property);
                }

                foreach (var serializedKeyword in graphData0.m_SerializedKeywords)
                {
                    var keyword = (ShaderKeyword)DeserializeLegacy(serializedKeyword.typeInfo.fullName, serializedKeyword.JSONnodeData);
                    if (keyword == null)
                    {
                        continue;
                    }

                    m_Keywords.Add(keyword);
                }

                if (isSubGraph)
                {
                    m_OutputNode = GetNodes<SubGraphOutputNode>().FirstOrDefault();
                }
                else if (!string.IsNullOrEmpty(graphData0.m_ActiveOutputNodeGuidSerialized))
                {
                    m_OutputNode = nodeGuidMap[graphData0.m_ActiveOutputNodeGuidSerialized];
                }
                else
                {
                    m_OutputNode = (AbstractMaterialNode)GetNodes<IMasterNode>().FirstOrDefault();
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

            m_Version = k_CurrentVersion;
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            m_NodeDictionary = new Dictionary<string, AbstractMaterialNode>(m_Nodes.Count);

            foreach (var group in m_Groups)
            {
                m_GroupItems.Add(group.guid, new List<IGroupItem>());
            }

            foreach (var node in m_Nodes.SelectValue())
            {
                node.owner = this;
                node.UpdateNodeAfterDeserialization();
                m_NodeDictionary.Add(node.objectId, node);
                m_GroupItems[node.groupGuid].Add(node);
            }

            foreach (var stickyNote in m_StickyNotes)
            {
                m_GroupItems[stickyNote.groupGuid].Add(stickyNote);
            }

            foreach (var edge in m_Edges)
                AddEdgeToNodeEdges(edge);
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
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypesOrNothing())
                {
                    var isImplementation = !type.IsAbstract && !type.IsGenericType && type.IsClass && typeof(ITargetImplementation).IsAssignableFrom(type);
                    //for subgraph output nodes, preview target is the only valid target
                    if (outputNode is SubGraphOutputNode && isImplementation && typeof(DefaultPreviewTarget).IsAssignableFrom(type))
                    {
                        var implementation = (DefaultPreviewTarget)Activator.CreateInstance(type);
                        foundImplementations.Add(implementation);
                    }
                    else if (isImplementation && !foundImplementations.Any(s => s.GetType() == type))
                    {
                        var masterNode = outputNode as IMasterNode;
                        var implementation = (ITargetImplementation)Activator.CreateInstance(type);
                        if(implementation.IsValid(masterNode))
                        {
                            foundImplementations.Add(implementation);
                        }
                    }
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

            m_ValidTargets = foundTargets;
            m_ValidImplementations = foundImplementations.Where(s => s.targetType == foundTargets[0].GetType()).ToList();

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
