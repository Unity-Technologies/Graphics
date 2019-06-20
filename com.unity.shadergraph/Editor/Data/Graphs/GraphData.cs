using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
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

        #region Property data

        [NonSerialized]
        List<AbstractShaderProperty> m_Properties = new List<AbstractShaderProperty>();

        public IEnumerable<AbstractShaderProperty> properties
        {
            get { return m_Properties; }
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        List<AbstractShaderProperty> m_AddedProperties = new List<AbstractShaderProperty>();

        public IEnumerable<AbstractShaderProperty> addedProperties
        {
            get { return m_AddedProperties; }
        }

        [NonSerialized]
        List<Guid> m_RemovedProperties = new List<Guid>();

        public IEnumerable<Guid> removedProperties
        {
            get { return m_RemovedProperties; }
        }

        [NonSerialized]
        List<AbstractShaderProperty> m_MovedProperties = new List<AbstractShaderProperty>();

        public IEnumerable<AbstractShaderProperty> movedProperties
        {
            get { return m_MovedProperties; }
        }

        [SerializeField]
        SerializableGuid m_GUID = new SerializableGuid();

        public Guid guid
        {
            get { return m_GUID.guid; }
        }

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
        List<NodeGroupChange> m_NodeGroupChanges = new List<NodeGroupChange>();

        public IEnumerable<NodeGroupChange> nodeGroupChanges
        {
            get { return m_NodeGroupChanges; }
        }

        [NonSerialized]
        Dictionary<Guid, List<AbstractMaterialNode>> m_GroupNodes = new Dictionary<Guid, List<AbstractMaterialNode>>();

        public IEnumerable<AbstractMaterialNode> GetNodesInGroup(GroupData groupData)
        {
            if (m_GroupNodes.TryGetValue(groupData.guid, out var nodes))
            {
                return nodes;
            }
            return Enumerable.Empty<AbstractMaterialNode>();
        }

        #endregion


        #region Edge data

        [NonSerialized]
        List<IEdge> m_Edges = new List<IEdge>();

        public IEnumerable<IEdge> edges
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
                        m_OutputNode = (AbstractMaterialNode)GetNodes<IMasterNode>().FirstOrDefault();
                    }
                }
                

                return m_OutputNode;
            }
        }

        public GraphData()
        {
            m_GroupNodes[Guid.Empty] = new List<AbstractMaterialNode>();
        }

        public void ClearChanges()
        {
            m_AddedNodes.Clear();
            m_RemovedNodes.Clear();
            m_PastedNodes.Clear();
            m_NodeGroupChanges.Clear();
            m_AddedGroups.Clear();
            m_RemovedGroups.Clear();
            m_PastedGroups.Clear();
            m_AddedEdges.Clear();
            m_RemovedEdges.Clear();
            m_AddedProperties.Clear();
            m_RemovedProperties.Clear();
            m_MovedProperties.Clear();
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
                ValidateGraph();
            }
            else
            {
                Debug.LogWarningFormat("Trying to add node {0} to Material graph, but it is not a {1}", node, typeof(AbstractMaterialNode));
            }
        }

        public void AddGroupData(GroupData groupData)
        {
            if (m_Groups.Contains(groupData))
                return;
            m_Groups.Add(groupData);
            m_AddedGroups.Add(groupData);
            m_GroupNodes.Add(groupData.guid, new List<AbstractMaterialNode>());
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

            if (m_GroupNodes.TryGetValue(group.guid, out var nodes))
            {
                m_GroupNodes.Remove(group.guid);
                foreach (AbstractMaterialNode node in nodes)
                {
                    RemoveNodeNoValidate(node);
                }
            }
        }

        public void SetNodeGroup(AbstractMaterialNode node, GroupData group)
        {
            var groupChange = new NodeGroupChange()
            {
                nodeGuid = node.guid,
                oldGroupGuid = node.groupGuid,
                // Checking if the groupdata is null. If it is, then it means node has been removed out of a group.
                // If the group data is null, then maybe the old group id should be removed
                newGroupGuid = group?.guid ?? Guid.Empty
            };
            node.groupGuid = groupChange.newGroupGuid;

            var oldGroupNodes = m_GroupNodes[groupChange.oldGroupGuid];
            oldGroupNodes.Remove(node);

            if (groupChange.oldGroupGuid != Guid.Empty && !oldGroupNodes.Any())
            {
                RemoveGroupNoValidate(m_Groups.First(x => x.guid == groupChange.oldGroupGuid));
            }

            m_GroupNodes[groupChange.newGroupGuid].Add(node);
            m_NodeGroupChanges.Add(groupChange);
        }

        void AddNodeNoValidate(AbstractMaterialNode node)
        {
            if (node.groupGuid != Guid.Empty && !m_GroupNodes.ContainsKey(node.groupGuid))
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
            m_GroupNodes[node.groupGuid].Add(node);
        }

        public void RemoveNode(AbstractMaterialNode node)
        {
            if (!node.canDeleteNode)
                return;
            RemoveNodeNoValidate(node);
            ValidateGraph();
        }

        void RemoveNodeNoValidate(AbstractMaterialNode node)
        {
            if (!m_NodeDictionary.ContainsKey(node.guid))
            {
                throw new InvalidOperationException("Cannot remove a node that doesn't exist.");
            }

            var materialNode = (AbstractMaterialNode)node;
            if (!materialNode.canDeleteNode)
                return;

            m_Nodes[materialNode.tempId.index] = null;
            m_FreeNodeTempIds.Push(materialNode.tempId);
            m_NodeDictionary.Remove(materialNode.guid);
            messageManager?.RemoveNode(materialNode.tempId);
            m_RemovedNodes.Add(materialNode);

            if (m_GroupNodes.TryGetValue(materialNode.groupGuid, out var nodes))
            {
                nodes.Remove(materialNode);
                if (materialNode.groupGuid != Guid.Empty && !nodes.Any())
                {
                    RemoveGroupNoValidate(m_Groups.First(x => x.guid == materialNode.groupGuid));
                }
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

        public void RemoveElements(IEnumerable<AbstractMaterialNode> nodes, IEnumerable<IEdge> edges, IEnumerable<GroupData> groups)
        {
            foreach (var edge in edges.ToArray())
                RemoveEdgeNoValidate(edge);

            foreach (var serializableNode in nodes.ToArray())
                RemoveNodeNoValidate(serializableNode);

            foreach (var groupData in groups)
            {
                if (m_GroupNodes.ContainsKey(groupData.guid))
                    RemoveGroupNoValidate(groupData);
            }

            ValidateGraph();
        }

        void RemoveEdgeNoValidate(IEdge e)
        {
            e = m_Edges.FirstOrDefault(x => x.Equals(e));
            if (e == null)
                throw new ArgumentException("Trying to remove an edge that does not exist.", "e");
            m_Edges.Remove(e);

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
                Debug.LogWarning("Node does not exist");
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
            if (!isSubGraph || generationMode == GenerationMode.Preview)
        {
            foreach (var prop in properties)
                collector.AddShaderProperty(prop);
        }

            if (isSubGraph)
            {
                List<AbstractMaterialNode> activeNodes = new List<AbstractMaterialNode>();
                NodeUtils.DepthFirstCollectNodesFromNode(activeNodes, outputNode);
                foreach (var node in activeNodes)
                {
                    node.CollectShaderProperties(collector, generationMode);
                }
            }
        }

        public void AddShaderProperty(AbstractShaderProperty property)
        {
            if (property == null)
                return;

            if (m_Properties.Contains(property))
                return;

            m_Properties.Add(property);
            m_AddedProperties.Add(property);
        }

        public string SanitizePropertyName(string displayName, Guid guid = default(Guid))
        {
            displayName = displayName.Trim();
            return GraphUtil.SanitizeName(m_Properties.Where(p => p.guid != guid).Select(p => p.displayName), "{0} ({1})", displayName);
        }

        public string SanitizePropertyReferenceName(string referenceName, Guid guid = default(Guid))
        {
            referenceName = referenceName.Trim();

            if (string.IsNullOrEmpty(referenceName))
                return null;

            if (!referenceName.StartsWith("_"))
                referenceName = "_" + referenceName;

            referenceName = Regex.Replace(referenceName, @"(?:[^A-Za-z_0-9])|(?:\s)", "_");

            return GraphUtil.SanitizeName(m_Properties.Where(p => p.guid != guid).Select(p => p.referenceName), "{0}_{1}", referenceName);
        }

        public void RemoveShaderProperty(Guid guid)
        {
            var propertyNodes = GetNodes<PropertyNode>().Where(x => x.propertyGuid == guid).ToList();
            foreach (var propNode in propertyNodes)
                ReplacePropertyNodeWithConcreteNodeNoValidate(propNode);

            RemoveShaderPropertyNoValidate(guid);

            ValidateGraph();
        }

        public void MoveShaderProperty(AbstractShaderProperty property, int newIndex)
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
            if (!m_MovedProperties.Contains(property))
                m_MovedProperties.Add(property);
        }

        public int GetShaderPropertyIndex(AbstractShaderProperty property)
        {
            return m_Properties.IndexOf(property);
        }

        void RemoveShaderPropertyNoValidate(Guid guid)
        {
            if (m_Properties.RemoveAll(x => x.guid == guid) > 0)
            {
                m_RemovedProperties.Add(guid);
                m_AddedProperties.RemoveAll(x => x.guid == guid);
                m_MovedProperties.RemoveAll(x => x.guid == guid);
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
                    || inputSlot == null
                    || !outputSlot.IsCompatibleWith(inputSlot))
                {
                    //orphaned edge
                    RemoveEdgeNoValidate(edge);
                }
            }

            foreach (var node in GetNodes<AbstractMaterialNode>())
                node.ValidateNode();

            foreach (var edge in m_AddedEdges.ToList())
            {
                if (!ContainsNodeGuid(edge.outputSlot.nodeGuid) || !ContainsNodeGuid(edge.inputSlot.nodeGuid))
                {
                    Debug.LogWarningFormat("Added edge is invalid: {0} -> {1}\n{2}", edge.outputSlot.nodeGuid, edge.inputSlot.nodeGuid, Environment.StackTrace);
                    m_AddedEdges.Remove(edge);
                }
            }
        }

        public void AddValidationError(Identifier id, string errorMessage)
        {
            messageManager?.AddOrAppendError(this, id, new ShaderMessage(errorMessage));;
        }
        
        public void ReplaceWith(GraphData other)
        {
            if (other == null)
                throw new ArgumentException("Can only replace with another AbstractMaterialGraph", "other");

            using (var removedPropertiesPooledObject = ListPool<Guid>.GetDisposable())
            {
                var removedPropertyGuids = removedPropertiesPooledObject.value;
                foreach (var property in m_Properties)
                    removedPropertyGuids.Add(property.guid);
                foreach (var propertyGuid in removedPropertyGuids)
                    RemoveShaderPropertyNoValidate(propertyGuid);
            }
            foreach (var otherProperty in other.properties)
            {
                if (!properties.Any(p => p.guid == otherProperty.guid))
                    AddShaderProperty(otherProperty);
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
                AddGroupData(groupData);

            foreach (var node in other.GetNodes<AbstractMaterialNode>())
                AddNodeNoValidate(node);

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

                AddGroupData(newGroup);
                m_PastedGroups.Add(newGroup);
            }

            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in graphToPaste.GetNodes<AbstractMaterialNode>())
            {
                AbstractMaterialNode pastedNode = node;

                var oldGuid = node.guid;
                var newGuid = node.RewriteGuid();
                nodeGuidMap[oldGuid] = newGuid;

                // Check if the property nodes need to be made into a concrete node.
                if (node is PropertyNode)
                {
                    PropertyNode propertyNode = (PropertyNode)node;

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
                // Check if the node is inside a group
                if (groupGuidMap.ContainsKey(abstractMaterialNode.groupGuid))
                {
                    var absNode = pastedNode as AbstractMaterialNode;
                    absNode.groupGuid = groupGuidMap[abstractMaterialNode.groupGuid];
                    pastedNode = absNode;
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
            m_SerializableNodes = SerializationHelper.Serialize(GetNodes<AbstractMaterialNode>());
            m_SerializableEdges = SerializationHelper.Serialize<IEdge>(m_Edges);
            m_SerializedProperties = SerializationHelper.Serialize<AbstractShaderProperty>(m_Properties);
        }

        public void OnAfterDeserialize()
        {
            // have to deserialize 'globals' before nodes
            m_Properties = SerializationHelper.Deserialize<AbstractShaderProperty>(m_SerializedProperties, GraphUtil.GetLegacyTypeRemapping());

            var nodes = SerializationHelper.Deserialize<AbstractMaterialNode>(m_SerializableNodes, GraphUtil.GetLegacyTypeRemapping());
            m_Nodes = new List<AbstractMaterialNode>(nodes.Count);
            m_NodeDictionary = new Dictionary<Guid, AbstractMaterialNode>(nodes.Count);
            foreach (var node in nodes.OfType<AbstractMaterialNode>())
            {
                node.owner = this;
                node.UpdateNodeAfterDeserialization();
                node.tempId = new Identifier(m_Nodes.Count);
                m_Nodes.Add(node);
                m_NodeDictionary.Add(node.guid, node);

                if(m_GroupNodes.ContainsKey(node.groupGuid))
                    m_GroupNodes[node.groupGuid].Add(node);
                else
                    m_GroupNodes.Add(node.groupGuid, new List<AbstractMaterialNode>(){node});
            }

            m_SerializableNodes = null;

            m_Edges = SerializationHelper.Deserialize<IEdge>(m_SerializableEdges, GraphUtil.GetLegacyTypeRemapping());
            m_SerializableEdges = null;
            foreach (var edge in m_Edges)
                AddEdgeToNodeEdges(edge);

            m_OutputNode = null;
        }

        public void OnEnable()
        {
            foreach (var node in GetNodes<AbstractMaterialNode>().OfType<IOnAssetEnabled>())
            {
                node.OnEnable();
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
