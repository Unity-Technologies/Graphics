using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Importers;

namespace UnityEditor.ShaderGraph
{
    [DataContract]
    [JsonVersioned(typeof(GraphDataV0))]
    public sealed class GraphData : IGenerateProperties
    {
        public GraphObject owner { get; set; }

        public bool isSubGraph { get; internal set; }

        SubGraphOutputNode m_OutputNode;

        public SubGraphOutputNode outputNode
        {
            get
            {
                if (!isSubGraph)
                    return null;
                // find existing node
                if (m_OutputNode == null)
                    m_OutputNode = GetNodes<SubGraphOutputNode>().FirstOrDefault();

                return m_OutputNode;
            }
        }

        public IMasterNode masterNode
        {
            get { return isSubGraph ? null : GetNodes<INode>().OfType<IMasterNode>().FirstOrDefault(); }
        }

        #region Property data

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        List<IShaderProperty> m_Properties = new List<IShaderProperty>();

        public IEnumerable<IShaderProperty> properties
        {
            get { return m_Properties; }
        }

        List<IShaderProperty> m_AddedProperties = new List<IShaderProperty>();

        public IEnumerable<IShaderProperty> addedProperties
        {
            get { return m_AddedProperties; }
        }

        List<Guid> m_RemovedProperties = new List<Guid>();

        public IEnumerable<Guid> removedProperties
        {
            get { return m_RemovedProperties; }
        }

        List<IShaderProperty> m_MovedProperties = new List<IShaderProperty>();

        public IEnumerable<IShaderProperty> movedProperties
        {
            get { return m_MovedProperties; }
        }

        #endregion

        [DataMember]
        public Guid guid { get; private set; }

        #region Node data

        Stack<Identifier> m_FreeNodeTempIds = new Stack<Identifier>();

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        List<AbstractMaterialNode> m_Nodes = new List<AbstractMaterialNode>();

        Dictionary<Guid, INode> m_NodeDictionary = new Dictionary<Guid, INode>();

        public IEnumerable<T> GetNodes<T>() where T : INode
        {
            return m_Nodes.Where(x => x != null).OfType<T>();
        }

        List<INode> m_AddedNodes = new List<INode>();

        public IEnumerable<INode> addedNodes
        {
            get { return m_AddedNodes; }
        }

        List<INode> m_RemovedNodes = new List<INode>();

        public IEnumerable<INode> removedNodes
        {
            get { return m_RemovedNodes; }
        }

        List<INode> m_PastedNodes = new List<INode>();

        public IEnumerable<INode> pastedNodes
        {
            get { return m_PastedNodes; }
        }

        #endregion

        #region Edge data

        [DataMember]
        List<EdgeData> m_Edges = new List<EdgeData>();

        public IEnumerable<EdgeData> edges
        {
            get { return m_Edges; }
        }

        Dictionary<Guid, List<EdgeData>> m_NodeEdges = new Dictionary<Guid, List<EdgeData>>();

        List<EdgeData> m_AddedEdges = new List<EdgeData>();

        public IEnumerable<EdgeData> addedEdges
        {
            get { return m_AddedEdges; }
        }

        List<EdgeData> m_RemovedEdges = new List<EdgeData>();

        public IEnumerable<EdgeData> removedEdges
        {
            get { return m_RemovedEdges; }
        }

        #endregion

        [DataMember]
        InspectorPreviewData m_Preview = new InspectorPreviewData();

        public InspectorPreviewData preview
        {
            get { return m_Preview; }
            set { m_Preview = value; }
        }

        public string name { get; set; }

        [DataMember]
        string m_Path;

        public string path
        {
            get { return m_Path; }
            set
            {
                if (m_Path == value)
                    return;
                m_Path = value;
                owner.RegisterCompleteObjectUndo("Change Path");
            }
        }

        public void ClearChanges()
        {
            m_AddedNodes.Clear();
            m_RemovedNodes.Clear();
            m_PastedNodes.Clear();
            m_AddedEdges.Clear();
            m_RemovedEdges.Clear();
            m_AddedProperties.Clear();
            m_RemovedProperties.Clear();
            m_MovedProperties.Clear();
        }

        public /*virtual*/ void AddNode(INode node)
        {
            var materialNode = node as AbstractMaterialNode;
            if (materialNode != null)
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

        void AddNodeNoValidate(AbstractMaterialNode materialNode)
        {
            materialNode.owner = this;
            if (m_FreeNodeTempIds.Any())
            {
                var id = m_FreeNodeTempIds.Pop();
                id.IncrementVersion();
                materialNode.tempId = id;
                m_Nodes[id.index] = materialNode;
            }
            else
            {
                var id = new Identifier(m_Nodes.Count);
                materialNode.tempId = id;
                m_Nodes.Add(materialNode);
            }
            m_NodeDictionary.Add(materialNode.guid, materialNode);
            m_AddedNodes.Add(materialNode);
        }

        public void RemoveNode(INode node)
        {
            if (!node.canDeleteNode)
                return;
            RemoveNodeNoValidate(node);
            ValidateGraph();
        }

        void RemoveNodeNoValidate(INode node)
        {
            var materialNode = (AbstractMaterialNode)node;
            if (!materialNode.canDeleteNode)
                return;

            m_Nodes[materialNode.tempId.index] = null;
            m_FreeNodeTempIds.Push(materialNode.tempId);
            m_NodeDictionary.Remove(materialNode.guid);
            m_RemovedNodes.Add(materialNode);
        }

        void AddEdgeToNodeEdges(EdgeData edge)
        {
            List<EdgeData> inputEdges;
            if (!m_NodeEdges.TryGetValue(edge.inputSlot.nodeGuid, out inputEdges))
                m_NodeEdges[edge.inputSlot.nodeGuid] = inputEdges = new List<EdgeData>();
            inputEdges.Add(edge);

            List<EdgeData> outputEdges;
            if (!m_NodeEdges.TryGetValue(edge.outputSlot.nodeGuid, out outputEdges))
                m_NodeEdges[edge.outputSlot.nodeGuid] = outputEdges = new List<EdgeData>();
            outputEdges.Add(edge);
        }

        EdgeData ConnectNoValidate(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            var fromNode = GetNodeFromGuid(fromSlotRef.nodeGuid);
            var toNode = GetNodeFromGuid(toSlotRef.nodeGuid);

            if (fromNode == null || toNode == null)
                return null;

            // if fromNode is already connected to toNode
            // do now allow a connection as toNode will then
            // have an edge to fromNode creating a cycle.
            // if this is parsed it will lead to an infinite loop.
            var dependentNodes = new List<INode>();
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

            var newEdge = new EdgeData(outputSlot, inputSlot);
            m_Edges.Add(newEdge);
            m_AddedEdges.Add(newEdge);
            AddEdgeToNodeEdges(newEdge);

            //Debug.LogFormat("Connected edge: {0} -> {1} ({2} -> {3})\n{4}", newEdge.outputSlot.nodeGuid, newEdge.inputSlot.nodeGuid, fromNode.name, toNode.name, Environment.StackTrace);
            return newEdge;
        }

        public /*virtual*/ EdgeData Connect(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            var newEdge = ConnectNoValidate(fromSlotRef, toSlotRef);
            ValidateGraph();
            return newEdge;
        }

        public /*virtual*/ void RemoveEdge(EdgeData e)
        {
            RemoveEdgeNoValidate(e);
            ValidateGraph();
        }

        public void RemoveElements(IEnumerable<INode> nodes, IEnumerable<EdgeData> edges)
        {
            foreach (var edge in edges.ToArray())
                RemoveEdgeNoValidate(edge);

            foreach (var serializableNode in nodes.ToArray())
                RemoveNodeNoValidate(serializableNode);

            ValidateGraph();
        }

        void RemoveEdgeNoValidate(EdgeData e)
        {
            e = m_Edges.FirstOrDefault(x => x.Equals(e));
            if (e == null)
                throw new ArgumentException("Trying to remove an edge that does not exist.", "e");
            m_Edges.Remove(e);

            List<EdgeData> inputNodeEdges;
            if (m_NodeEdges.TryGetValue(e.inputSlot.nodeGuid, out inputNodeEdges))
                inputNodeEdges.Remove(e);

            List<EdgeData> outputNodeEdges;
            if (m_NodeEdges.TryGetValue(e.outputSlot.nodeGuid, out outputNodeEdges))
                outputNodeEdges.Remove(e);

            m_RemovedEdges.Add(e);
        }

        public INode GetNodeFromGuid(Guid guid)
        {
            INode node;
            m_NodeDictionary.TryGetValue(guid, out node);
            return node;
        }

        public INode GetNodeFromTempId(Identifier tempId)
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

        public T GetNodeFromGuid<T>(Guid guid) where T : INode
        {
            var node = GetNodeFromGuid(guid);
            if (node is T)
                return (T)node;
            return default(T);
        }

        public IEnumerable<EdgeData> GetEdges(SlotReference s)
        {
            var foundEdges = new List<EdgeData>();
            GetEdges(s, foundEdges);
            return foundEdges;
        }

        public void GetEdges(SlotReference s, List<EdgeData> foundEdges)
        {
            var node = GetNodeFromGuid(s.nodeGuid);
            if (node == null)
            {
                Debug.LogWarning("Node does not exist");
                return;
            }
            ISlot slot = node.FindSlot<ISlot>(s.slotId);

            List<EdgeData> candidateEdges;
            if (!m_NodeEdges.TryGetValue(s.nodeGuid, out candidateEdges))
                return;

            foreach (var edge in candidateEdges)
            {
                var cs = slot.isInputSlot ? edge.inputSlot : edge.outputSlot;
                if (cs.nodeGuid == s.nodeGuid && cs.slotId == s.slotId)
                    foundEdges.Add(edge);
            }
        }

        public /*virtual*/ void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (isSubGraph)
            {
                var activeNodes = new List<AbstractMaterialNode>();
                NodeUtils.DepthFirstCollectNodesFromNode(activeNodes, outputNode);
                foreach (var node in activeNodes)
                    node.CollectShaderProperties(collector, generationMode);
            }

            // if we are previewing the graph we need to
            // export 'exposed props' if we are 'for real'
            // then we are outputting the graph in the
            // nested context and the needed values will
            // be copied into scope.
            if (!isSubGraph || generationMode == GenerationMode.Preview)
            {
                foreach (var prop in properties)
                    collector.AddShaderProperty(prop);
            }

        }

        public void AddShaderProperty(IShaderProperty property)
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

        public void MoveShaderProperty(IShaderProperty property, int newIndex)
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

        public int GetShaderPropertyIndex(IShaderProperty property)
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

        static List<EdgeData> s_TempEdges = new List<EdgeData>();

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
            var materialNode = node as AbstractMaterialNode;
            if (materialNode == null)
                return;

            var slot = propertyNode.FindOutputSlot<MaterialSlot>(PropertyNode.OutputSlotId);
            var newSlot = materialNode.GetOutputSlots<MaterialSlot>().FirstOrDefault(s => s.valueType == slot.valueType);
            if (newSlot == null)
                return;

            materialNode.drawState = propertyNode.drawState;
            AddNodeNoValidate(materialNode);

            foreach (var edge in this.GetEdges(slot.slotReference))
                ConnectNoValidate(newSlot.slotReference, edge.inputSlot);

            RemoveNodeNoValidate(propertyNode);
        }

        public void ValidateGraph()
        {
            var propertyNodes = GetNodes<PropertyNode>().Where(n => !m_Properties.Any(p => p.guid == n.propertyGuid)).ToArray();
            foreach (var pNode in propertyNodes)
                ReplacePropertyNodeWithConcreteNodeNoValidate(pNode);

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

            foreach (var node in GetNodes<INode>())
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

        public void ReplaceWith(GraphData other)
        {
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
            using (var pooledList = ListPool<EdgeData>.GetDisposable())
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

            foreach (var node in other.GetNodes<AbstractMaterialNode>())
                AddNodeNoValidate(node);

            foreach (var edge in other.edges)
                ConnectNoValidate(edge.outputSlot, edge.inputSlot);

            ValidateGraph();
        }

        internal void PasteGraph(CopyPasteGraph graphToPaste, List<INode> remappedNodes, List<EdgeData> remappedEdges)
        {
            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in graphToPaste.GetNodes<INode>())
            {
                INode pastedNode = node;

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

        public void OnEnable()
        {
            foreach (var node in GetNodes<INode>().OfType<IOnAssetEnabled>())
            {
                node.OnEnable();
            }
        }

        public void LoadedFromDisk()
        {
            OnEnable();
            ValidateGraph();
        }
    }

    [Serializable]
    sealed class GraphDataV0 : IUpgradableTo<GraphData>
    {
        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        SerializableGuid m_GUID = new SerializableGuid();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        InspectorPreviewData m_PreviewData = new InspectorPreviewData();

        [SerializeField]
        string m_Path;

        public GraphData Upgrade()
        {
            var graphData = new GraphData
            {

            };
            return graphData;
        }

//        public void OnBeforeSerialize()
//        {
//            m_SerializableNodes = SerializationHelper.Serialize(GetNodes<INode>());
//            m_SerializableEdges = SerializationHelper.Serialize<EdgeData>(m_Edges);
//            m_SerializedProperties = SerializationHelper.Serialize<IShaderProperty>(m_Properties);
//        }
//
//        public /*virtual*/ void OnAfterDeserialize()
//        {
//            m_OutputNode = null;
//            // have to deserialize 'globals' before nodes
//            m_Properties = SerializationHelper.Deserialize<IShaderProperty>(m_SerializedProperties, GraphUtil.GetLegacyTypeRemapping());
//            var nodes = SerializationHelper.Deserialize<INode>(m_SerializableNodes, GraphUtil.GetLegacyTypeRemapping());
//            m_Nodes = new List<AbstractMaterialNode>(nodes.Count);
//            m_NodeDictionary = new Dictionary<Guid, INode>(nodes.Count);
//            foreach (var node in nodes.OfType<AbstractMaterialNode>())
//            {
//                node.owner = this;
//                node.UpdateNodeAfterDeserialization();
//                node.tempId = new Identifier(m_Nodes.Count);
//                m_Nodes.Add(node);
//                m_NodeDictionary.Add(node.guid, node);
//            }
//
//            m_SerializableNodes = null;
//
//            m_Edges = SerializationHelper.Deserialize<EdgeData>(m_SerializableEdges, GraphUtil.GetLegacyTypeRemapping());
//            m_SerializableEdges = null;
//            foreach (var edge in m_Edges)
//                AddEdgeToNodeEdges(edge);
//        }
    }
}
