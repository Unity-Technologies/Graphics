using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.MaterialGraph")]
    [FormerName("UnityEditor.ShaderGraph.SubGraph")]
    [FormerName("UnityEditor.ShaderGraph.AbstractMaterialGraph")]
    sealed class GraphData : IJsonObject, IOnDeserialized
    {
        public GraphObject owner { get; set; }

        #region Input data

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonUpgrade("m_SerializedProperties", typeof(SerializedElementsConverter))]
        List<AbstractShaderProperty> m_Properties = new List<AbstractShaderProperty>();

        public IEnumerable<AbstractShaderProperty> properties
        {
            get { return m_Properties; }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonUpgrade("m_SerializedKeywords", typeof(SerializedElementsConverter))]
        List<ShaderKeyword> m_Keywords = new List<ShaderKeyword>();

        public IEnumerable<ShaderKeyword> keywords
        {
            get { return m_Keywords; }
        }

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

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonUpgrade("m_SerializableNodes", typeof(SerializedElementsConverter))]
        List<AbstractMaterialNode> m_Nodes = new List<AbstractMaterialNode>();

        public IEnumerable<T> GetNodes<T>()
        {
            return m_Nodes.Where(x => x != null).OfType<T>();
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

        GroupData m_NullGroup = new GroupData();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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
        Dictionary<GroupData, List<IGroupItem>> m_GroupItems = new Dictionary<GroupData, List<IGroupItem>>();

        public IEnumerable<IGroupItem> GetItemsInGroup(GroupData groupData)
        {
            if (m_GroupItems.TryGetValue(groupData ?? m_NullGroup, out var nodes))
            {
                return nodes;
            }
            return Enumerable.Empty<IGroupItem>();
        }

        #endregion

        #region StickyNote Data
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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

        [JsonProperty]
        List<Edge> m_Edges = new List<Edge>();

        public IEnumerable<Edge> edges
        {
            get { return m_Edges; }
        }

        [NonSerialized]
        Dictionary<AbstractMaterialNode, List<Edge>> m_NodeEdges = new Dictionary<AbstractMaterialNode, List<Edge>>();

        [NonSerialized]
        List<Edge> m_AddedEdges = new List<Edge>();

        public IEnumerable<Edge> addedEdges
        {
            get { return m_AddedEdges; }
        }

        [NonSerialized]
        List<Edge> m_RemovedEdges = new List<Edge>();

        public IEnumerable<Edge> removedEdges
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

        [JsonProperty]
        AbstractMaterialNode m_OutputNode;

        public AbstractMaterialNode outputNode
        {
            get => m_OutputNode;
            set
            {
                if (m_OutputNode != value)
                {
                    m_OutputNode = value;
                    didActiveOutputNodeChange = true;
                }
            }
        }

        public bool didActiveOutputNodeChange { get; set; }

        internal delegate void SaveGraphDelegate(Shader shader);
        internal static SaveGraphDelegate onSaveGraph;

        public GraphData()
        {
            m_GroupItems[m_NullGroup] = new List<IGroupItem>();
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
            if (!m_Groups.Contains(group))
                throw new InvalidOperationException("Cannot remove a group that doesn't exist.");
            m_Groups.Remove(group);
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
            if (m_StickyNotes.Contains(stickyNote))
            {
                throw new InvalidOperationException("Sticky note has already been added to the graph.");
            }

            if (!m_GroupItems.ContainsKey(stickyNote.group))
            {
                throw new InvalidOperationException("Trying to add sticky note with group that doesn't exist.");
            }

            m_StickyNotes.Add(stickyNote);
            m_AddedStickyNotes.Add(stickyNote);
            m_GroupItems[stickyNote.group ?? m_NullGroup].Add(stickyNote);
        }

        void RemoveNoteNoValidate(StickyNoteData stickyNote)
        {
            if (!m_StickyNotes.Contains(stickyNote))
            {
                throw new InvalidOperationException("Cannot remove a note that doesn't exist.");
            }

            m_StickyNotes.Remove(stickyNote);
            m_RemovedNotes.Add(stickyNote);

            if (m_GroupItems.TryGetValue(stickyNote.group ?? m_NullGroup, out var groupItems))
            {
                groupItems.Remove(stickyNote);
            }
        }

        public void SetGroup(IGroupItem node, GroupData group)
        {
            var groupChange = new ParentGroupChange()
            {
                groupItem = node,
                oldGroup = node.group,
                // Checking if the groupdata is null. If it is, then it means node has been removed out of a group.
                // If the group data is null, then maybe the old group id should be removed
                newGroup = group
            };
            node.group = group;

            var oldGroupNodes = m_GroupItems[groupChange.oldGroup ?? m_NullGroup];
            oldGroupNodes.Remove(node);

            m_GroupItems[groupChange.newGroup].Add(node);
            m_ParentGroupChanges.Add(groupChange);
        }

        void AddNodeNoValidate(AbstractMaterialNode node)
        {
            if (!m_GroupItems.ContainsKey(node.group ?? m_NullGroup))
            {
                throw new InvalidOperationException("Cannot add a node whose group doesn't exist.");
            }
            node.owner = this;
            m_Nodes.Add(node);
            m_AddedNodes.Add(node);
            m_GroupItems[node.group ?? m_NullGroup].Add(node);
        }

        public void RemoveNode(AbstractMaterialNode node)
        {
            if (!node.canDeleteNode)
            {
                throw new InvalidOperationException($"Node {node.name} cannot be deleted.");
            }
            RemoveNodeNoValidate(node);
            ValidateGraph();
        }

        void RemoveNodeNoValidate(AbstractMaterialNode node)
        {
            if (!ContainsNode(node))
            {
                throw new InvalidOperationException("Cannot remove a node that doesn't exist.");
            }

            m_Nodes.Remove(node);
            messageManager?.RemoveNode(node);
            m_RemovedNodes.Add(node);

            if (m_GroupItems.TryGetValue(node.group ?? m_NullGroup, out var groupItems))
            {
                groupItems.Remove(node);
            }
        }

        void AddEdgeToNodeEdges(Edge edge)
        {
            List<Edge> inputEdges;
            if (!m_NodeEdges.TryGetValue(edge.inputSlot.owner, out inputEdges))
                m_NodeEdges[edge.inputSlot.owner] = inputEdges = new List<Edge>();
            inputEdges.Add(edge);

            List<Edge> outputEdges;
            if (!m_NodeEdges.TryGetValue(edge.outputSlot.owner, out outputEdges))
                m_NodeEdges[edge.outputSlot.owner] = outputEdges = new List<Edge>();
            outputEdges.Add(edge);
        }

        Edge ConnectNoValidate(MaterialSlot fromSlot, MaterialSlot toSlot)
        {
            var fromNode = fromSlot.owner;
            var toNode = toSlot.owner;

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

            if (fromSlot.isOutputSlot == toSlot.isOutputSlot)
                throw new InvalidOperationException($"Cannot connect two slots of opposite directions ({fromNode.name}/{fromSlot.displayName} and {toNode.name}/{toSlot.displayName})");

            var outputSlot = fromSlot.isOutputSlot ? fromSlot : toSlot;
            var inputSlot = fromSlot.isInputSlot ? fromSlot : toSlot;

            s_TempEdges.Clear();
            GetEdges(inputSlot, s_TempEdges);

            // remove any inputs that exits before adding
            foreach (var edge in s_TempEdges)
            {
                RemoveEdgeNoValidate((Edge)edge);
            }

            var newEdge = new Edge(outputSlot, inputSlot);
            m_Edges.Add(newEdge);
            m_AddedEdges.Add(newEdge);
            AddEdgeToNodeEdges(newEdge);

            //Debug.LogFormat("Connected edge: {0} -> {1} ({2} -> {3})\n{4}", newEdge.outputSlot.nodeGuid, newEdge.inputSlot.nodeGuid, fromNode.name, toNode.name, Environment.StackTrace);
            return newEdge;
        }

        public Edge Connect(MaterialSlot fromSlot, MaterialSlot toSlot)
        {
            var newEdge = ConnectNoValidate(fromSlot, toSlot);
            ValidateGraph();
            return newEdge;
        }

        public void RemoveEdge(Edge e)
        {
            RemoveEdgeNoValidate((Edge)e);
            ValidateGraph();
        }

        public void RemoveElements(AbstractMaterialNode[] nodes, Edge[] edges, GroupData[] groups, StickyNoteData[] notes)
        {
            foreach (var node in nodes)
            {
                if (!node.canDeleteNode)
                {
                    throw new InvalidOperationException($"Node {node.name} cannot be deleted.");
                }
            }

            foreach (var edge in edges.ToArray())
            {
                RemoveEdgeNoValidate((Edge)edge);
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

        void RemoveEdgeNoValidate(Edge e)
        {
            e = m_Edges.FirstOrDefault(x => x.Equals(e));
            if (e == null)
                throw new ArgumentException("Trying to remove an edge that does not exist.", "e");
            m_Edges.Remove(e);

            List<Edge> inputNodeEdges;
            if (e.inputSlot.owner != null && m_NodeEdges.TryGetValue(e.inputSlot.owner, out inputNodeEdges))
                inputNodeEdges.Remove(e);

            List<Edge> outputNodeEdges;
            if (e.outputSlot.owner != null && m_NodeEdges.TryGetValue(e.outputSlot.owner, out outputNodeEdges))
                outputNodeEdges.Remove(e);

            m_RemovedEdges.Add(e);
        }

//        public AbstractMaterialNode GetNodeFromGuid(Guid guid)
//        {
//            AbstractMaterialNode node;
//            m_NodeDictionary.TryGetValue(guid, out node);
//            return node;
//        }

        public bool ContainsNode(AbstractMaterialNode node)
        {
            if (node == null)
            {
                return false;
            }
            return m_Nodes.Contains(node);
        }

        public void GetEdges(MaterialSlot slot, List<Edge> foundEdges)
        {
            var node = slot.owner;
            if (node == null)
            {
                return;
            }

            List<Edge> candidateEdges;
            if (!m_NodeEdges.TryGetValue(slot.owner, out candidateEdges))
                return;

            foreach (var edge in candidateEdges)
            {
                if ((slot.isInputSlot ? edge.inputSlot : edge.outputSlot) == slot)
                {
                    foundEdges.Add(edge);
                }
            }
        }

        public IEnumerable<Edge> GetEdges(MaterialSlot s)
        {
            var edges = new List<Edge>();
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

        static List<Edge> s_TempEdges = new List<Edge>();

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

            var node = property.ToConcreteNode();
            if (node == null)
                return;

            var slot = propertyNode.FindOutputSlot<MaterialSlot>(PropertyNode.OutputSlotId);
            var newSlot = node.GetOutputSlots<MaterialSlot>().FirstOrDefault(s => s.valueType == slot.valueType);
            if (newSlot == null)
                return;

            node.drawState = propertyNode.drawState;
            node.group = propertyNode.group;
            AddNodeNoValidate(node);

            foreach (var edge in GetEdges(slot))
                ConnectNoValidate(newSlot, edge.inputSlot);

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
            foreach (var edge in m_Edges.ToArray())
            {
                var outputNode = edge.outputSlot.owner;
                var inputNode = edge.inputSlot.owner;

                MaterialSlot outputSlot = null;
                MaterialSlot inputSlot = null;
                if (outputNode != null && inputNode != null)
                {
                    outputSlot = edge.outputSlot;
                    inputSlot = edge.inputSlot;
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

            var temporaryMarks = new HashSet<AbstractMaterialNode>();
            var permanentMarks = new HashSet<AbstractMaterialNode>();
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
                if (permanentMarks.Contains(node))
                {
                    continue;
                }

                if (temporaryMarks.Contains(node))
                {
                    node.ValidateNode();
                    permanentMarks.Add(node);
                }
                else
                {
                    temporaryMarks.Add(node);
                    stack.Push(node);
                    node.GetInputSlots(slots);
                    foreach (var inputSlot in slots)
                    {
                        var nodeEdges = GetEdges(inputSlot);
                        foreach (var edge in nodeEdges)
                        {
                            var fromSlot = edge.outputSlot;
                            var childNode = fromSlot.owner;
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

            foreach (var edge in m_AddedEdges.ToList())
            {
                if (!ContainsNode(edge.outputSlot.owner) || !ContainsNode(edge.inputSlot.owner))
                {
                    Debug.LogWarningFormat("Added edge is invalid: {0} -> {1}\n{2}", edge.outputSlot, edge.inputSlot, Environment.StackTrace);
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

        public void AddValidationError(AbstractMaterialNode node, string errorMessage,
            ShaderCompilerMessageSeverity severity = ShaderCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, node, new ShaderMessage(errorMessage, severity));
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

            using (var pooledList = ListPool<Edge>.GetDisposable())
            {
                var removedNodeEdges = pooledList.value;
                removedNodeEdges.AddRange(m_Edges);
                foreach (var edge in removedNodeEdges)
                    RemoveEdgeNoValidate(edge);
            }

            using (var removedNodesPooledObject = ListPool<AbstractMaterialNode>.GetDisposable())
            {
                var removedNodes = removedNodesPooledObject.value;
                removedNodes.AddRange(m_Nodes.Where(n => n != null));
                foreach (var node in removedNodes)
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
                ConnectNoValidate(edge.outputSlot, edge.inputSlot);

            outputNode = other.outputNode;

            ValidateGraph();
        }

        // TODO: FIX COPY PASTE
        internal void PasteGraph(CopyPasteGraph graphToPaste, List<AbstractMaterialNode> remappedNodes, List<Edge> remappedEdges)
        {
//            var groupGuidMap = new Dictionary<Guid, Guid>();
//            foreach (var group in graphToPaste.groups)
//            {
//                var position = group.position;
//                position.x += 30;
//                position.y += 30;
//
//                GroupData newGroup = new GroupData(group.title, position);
//
//                var oldGuid = group.legacyGuid;
//                var newGuid = newGroup.legacyGuid;
//                groupGuidMap[oldGuid] = newGuid;
//
//                AddGroup(newGroup);
//                m_PastedGroups.Add(newGroup);
//            }
//
//            foreach (var stickyNote in graphToPaste.stickyNotes)
//            {
//                var position = stickyNote.position;
//                position.x += 30;
//                position.y += 30;
//
//                StickyNoteData pastedStickyNote = new StickyNoteData(stickyNote.title, stickyNote.content, position);
////                if (groupGuidMap.ContainsKey(stickyNote.legacyGroupGuid))
////                {
////                    pastedStickyNote.legacyGroupGuid = groupGuidMap[stickyNote.legacyGroupGuid];
////                }
//
//                AddStickyNote(pastedStickyNote);
//                m_PastedStickyNotes.Add(pastedStickyNote);
//            }
//
//            var nodeGuidMap = new Dictionary<Guid, Guid>();
//            foreach (var node in graphToPaste.GetNodes<AbstractMaterialNode>())
//            {
//                AbstractMaterialNode pastedNode = node;
//
//                var oldGuid = node.legacyGuid;
//                var newGuid = node.RewriteGuid();
//                nodeGuidMap[oldGuid] = newGuid;
//
//                // Check if the property nodes need to be made into a concrete node.
//                if (node is PropertyNode propertyNode)
//                {
//                    // If the property is not in the current graph, do check if the
//                    // property can be made into a concrete node.
//                    if (!m_Properties.Select(x => x.guid).Contains(propertyNode.propertyGuid))
//                    {
//                        // If the property is in the serialized paste graph, make the property node into a property node.
//                        var pastedGraphMetaProperties = graphToPaste.metaProperties.Where(x => x.guid == propertyNode.propertyGuid);
//                        if (pastedGraphMetaProperties.Any())
//                        {
//                            pastedNode = pastedGraphMetaProperties.FirstOrDefault().ToConcreteNode();
//                            pastedNode.drawState = node.drawState;
//                            nodeGuidMap[oldGuid] = pastedNode.legacyGuid;
//                        }
//                    }
//                }
//
//                AbstractMaterialNode abstractMaterialNode = (AbstractMaterialNode)node;
//                // Check if the node is inside a group
////                if (groupGuidMap.ContainsKey(abstractMaterialNode.legacyGroupGuid))
////                {
////                    var absNode = pastedNode as AbstractMaterialNode;
////                    absNode.legacyGroupGuid = groupGuidMap[abstractMaterialNode.legacyGroupGuid];
////                    pastedNode = absNode;
////                }
//
//                var drawState = node.drawState;
//                var position = drawState.position;
//                position.x += 30;
//                position.y += 30;
//                drawState.position = position;
//                node.drawState = drawState;
//                remappedNodes.Add(pastedNode);
//                AddNode(pastedNode);
//
//                // add the node to the pasted node list
//                m_PastedNodes.Add(pastedNode);
//
//                // Check if the keyword nodes need to have their keywords copied.
//                if (node is KeywordNode keywordNode)
//                {
//                    // If the keyword is not in the current graph and is in the serialized paste graph copy it.
//                    if (!keywords.Select(x => x.guid).Contains(keywordNode.keywordGuid))
//                    {
//                        var pastedGraphMetaKeywords = graphToPaste.metaKeywords.Where(x => x.guid == keywordNode.keywordGuid);
//                        if (pastedGraphMetaKeywords.Any())
//                        {
//                            var keyword = pastedGraphMetaKeywords.FirstOrDefault(x => x.guid == keywordNode.keywordGuid);
//                            SanitizeGraphInputName(keyword);
//                            SanitizeGraphInputReferenceName(keyword, keyword.overrideReferenceName);
//                            AddGraphInput(keyword);
//                        }
//                    }
//
//                    // Always update Keyword nodes to handle any collisions resolved on the Keyword
//                    keywordNode.UpdateNode();
//                }
//            }
//
//            // only connect edges within pasted elements, discard
//            // external edges.
//            foreach (var edge in graphToPaste.edges)
//            {
//                var outputSlot = edge.outputSlot;
//                var inputSlot = edge.inputSlot;
//
////                Guid remappedOutputNodeGuid;
////                Guid remappedInputNodeGuid;
////                if (nodeGuidMap.TryGetValue(outputSlot.owner.guid, out remappedOutputNodeGuid)
////                    && nodeGuidMap.TryGetValue(inputSlot.owner.guid, out remappedInputNodeGuid))
//                {
//                    remappedEdges.Add(Connect(outputSlot, inputSlot));
//                }
//            }
//
//            ValidateGraph();
        }

        [JsonExtensionData]
        Dictionary<string, JToken> m_AdditionalData = null;

        MaterialSlot GetSlotFromLegacyJToken(JToken jToken, List<DeserializationPair> jObjects)
        {
            var nodeGuid = jToken.Value<string>("m_NodeGUIDSerialized");
            var slotId = jToken.Value<int>("m_SlotId");
            foreach (var (instance, jObject) in jObjects)
            {
                    if (instance is AbstractMaterialNode node &&
                        jObject.TryGetValue("m_GuidSerialized", out var nodeGuidJToken) &&
                        nodeGuidJToken.Value<string>() == nodeGuid)
                {
                    return node.FindSlot(slotId);
                }
            }

            throw new InvalidOperationException($"Could not find legacy slot {nodeGuid}.{slotId}");
        }

        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
        }

        public void OnDeserialized(JObject jObject, List<DeserializationPair> jObjects)
        {
            Dictionary<string, GroupData> legacyGroupMap = null;
            foreach (var (instance, instanceJObject) in jObjects)
            {
                if (instance is GroupData group && instanceJObject.TryGetValue("m_GuidSerialized", out var groupGuidJToken))
                {
                    var guid = groupGuidJToken.Value<string>();
                    if (legacyGroupMap == null)
                    {
                        legacyGroupMap = new Dictionary<string, GroupData>();
                    }

                    legacyGroupMap[guid] = group;
                }
            }

            foreach (var (instance, groupItemJObject) in jObjects)
            {
                if (instance is IGroupItem groupItem && groupItemJObject.TryGetValue("m_GroupGuidSerialized", out var groupGuidJToken))
                {
                    var guid = groupGuidJToken.Value<string>();
                    groupItem.group = legacyGroupMap?[guid];
                }
            }

            if (m_AdditionalData.TryGetValue("m_SerializableEdges", out var serializableEdges))
            {
                foreach (var jToken in serializableEdges)
                {
                    var element = jToken.ToObject<SerializationHelper.JSONSerializedElement>();
                    var edgeJToken = JToken.Parse(element.JSONnodeData);
                    var outputSlot = GetSlotFromLegacyJToken(edgeJToken["m_OutputSlot"], jObjects);
                    var inputSlot = GetSlotFromLegacyJToken(edgeJToken["m_InputSlot"], jObjects);
                    m_Edges.Add(new Edge(outputSlot, inputSlot));
                }
            }

            foreach (var node in m_Nodes)
            {
                m_GroupItems[node.group ?? m_NullGroup].Add(node);
                node.OnAfterDeserialize();
            }

            foreach (var edge in m_Edges)
            {
                AddEdgeToNodeEdges(edge);
            }

            foreach (var stickyNote in m_StickyNotes)
            {
                m_GroupItems[stickyNote.group ?? m_NullGroup].Add(stickyNote);
            }

            if (m_AdditionalData.TryGetValue("m_ActiveOutputNodeGuidSerialized", out var guidJToken))
            {
                var guid = guidJToken.Value<string>();
                foreach (var (instance, nodeJObject) in jObjects)
                {
                    if (instance is AbstractMaterialNode node &&
                        nodeJObject.TryGetValue("m_GuidSerialized", out var nodeGuidJToken) &&
                        nodeGuidJToken.Value<string>() == guid)
                    {
                        outputNode = node;
                        break;
                    }
                }
            }

            if (m_OutputNode == null)
            {
                m_OutputNode = GetNodes<AbstractMaterialNode>().FirstOrDefault(x => x is IMasterNode)
                    ?? GetNodes<SubGraphOutputNode>().FirstOrDefault();
            }

            m_AdditionalData.Clear();
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            foreach (var group in m_Groups)
            {
                m_GroupItems.Add(group, new List<IGroupItem>());
            }

            foreach (var node in m_Nodes)
            {
                node.owner = this;
                m_NodeEdges[node] = new List<Edge>();
            }
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
