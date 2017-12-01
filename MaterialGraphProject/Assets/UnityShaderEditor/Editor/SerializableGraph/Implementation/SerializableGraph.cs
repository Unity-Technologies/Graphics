using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Graphing
{
    [Serializable]
    public class SerializableGraph : IGraph, ISerializationCallbackReceiver
    {
        [NonSerialized]
        List<IEdge> m_Edges = new List<IEdge>();

        [NonSerialized]
        Dictionary<Guid, List<IEdge>> m_NodeEdges = new Dictionary<Guid, List<IEdge>>();

        [NonSerialized]
        Dictionary<Guid, INode> m_Nodes = new Dictionary<Guid, INode>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        List<INode> m_AddedNodes = new List<INode>();

        [NonSerialized]
        List<INode> m_RemovedNodes = new List<INode>();

        [NonSerialized]
        List<IEdge> m_AddedEdges = new List<IEdge>();

        [NonSerialized]
        List<IEdge> m_RemovedEdges = new List<IEdge>();

        public IEnumerable<INode> addedNodes
        {
            get { return m_AddedNodes; }
        }

        public IEnumerable<INode> removedNodes
        {
            get { return m_RemovedNodes; }
        }

        public IEnumerable<IEdge> addedEdges
        {
            get { return m_AddedEdges; }
        }

        public IEnumerable<IEdge> removedEdges
        {
            get { return m_RemovedEdges; }
        }

        public IGraphObject owner { get; set; }

        public virtual void ClearChanges()
        {
            m_AddedNodes.Clear();
            m_RemovedNodes.Clear();
            m_AddedEdges.Clear();
            m_RemovedEdges.Clear();
        }

        public IEnumerable<T> GetNodes<T>() where T : INode
        {
            return m_Nodes.Values.OfType<T>();
        }

        public IEnumerable<IEdge> edges
        {
            get { return m_Edges; }
        }

        public virtual void AddNode(INode node)
        {
            AddNodeNoValidate(node);
            ValidateGraph();
        }

        protected void AddNodeNoValidate(INode node)
        {
            m_Nodes.Add(node.guid, node);
            node.owner = this;
            m_AddedNodes.Add(node);
        }

        public virtual void RemoveNode(INode node)
        {
            if (!node.canDeleteNode)
                return;

            m_Nodes.Remove(node.guid);
            m_RemovedNodes.Add(node);
            ValidateGraph();
        }

        protected void RemoveNodeNoValidate(INode node)
        {
            if (!node.canDeleteNode)
                return;

            m_Nodes.Remove(node.guid);
            m_RemovedNodes.Add(node);
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

        public virtual Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            return new Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo>();
        }

        protected IEdge ConnectNoValidate(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            if (fromSlotRef == null || toSlotRef == null)
                return null;

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

            SlotReference outputSlot = null;
            SlotReference inputSlot = null;

            // output must connect to input
            if (fromSlot.isOutputSlot)
                outputSlot = fromSlotRef;
            else if (fromSlot.isInputSlot)
                inputSlot = fromSlotRef;

            if (toSlot.isOutputSlot)
                outputSlot = toSlotRef;
            else if (toSlot.isInputSlot)
                inputSlot = toSlotRef;

            if (inputSlot == null || outputSlot == null)
                return null;

            var slotEdges = GetEdges(inputSlot).ToList();

            // remove any inputs that exits before adding
            foreach (var edge in slotEdges)
            {
                RemoveEdgeNoValidate(edge);
            }

            var newEdge = new Edge(outputSlot, inputSlot);
            m_Edges.Add(newEdge);
            m_AddedEdges.Add(newEdge);
            AddEdgeToNodeEdges(newEdge);

            Debug.LogFormat("Connected edge: {0} -> {1} ({2} -> {3})\n{4}", newEdge.outputSlot.nodeGuid, newEdge.inputSlot.nodeGuid, fromNode.name, toNode.name, Environment.StackTrace);
            return newEdge;
        }

        public virtual IEdge Connect(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            var newEdge = ConnectNoValidate(fromSlotRef, toSlotRef);
            ValidateGraph();
            return newEdge;
        }

        public virtual void RemoveEdge(IEdge e)
        {
            RemoveEdgeNoValidate(e);
            ValidateGraph();
        }

        public void RemoveElements(IEnumerable<INode> nodes, IEnumerable<IEdge> edges)
        {
            foreach (var edge in edges.ToArray())
                RemoveEdgeNoValidate(edge);

            foreach (var serializableNode in nodes.ToArray())
                RemoveNodeNoValidate(serializableNode);

            ValidateGraph();
        }

        protected void RemoveEdgeNoValidate(IEdge e)
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

        public INode GetNodeFromGuid(Guid guid)
        {
            INode node;
            m_Nodes.TryGetValue(guid, out node);
            return node;
        }

        public bool ContainsNodeGuid(Guid guid)
        {
            return m_Nodes.ContainsKey(guid);
        }

        public T GetNodeFromGuid<T>(Guid guid) where T : INode
        {
            var node = GetNodeFromGuid(guid);
            if (node is T)
                return (T)node;
            return default(T);
        }

        public IEnumerable<IEdge> GetEdges(SlotReference s)
        {
            if (s == null)
                return Enumerable.Empty<IEdge>();

            var node = GetNodeFromGuid(s.nodeGuid);
            if (node == null)
            {
                Debug.LogWarning("Node does not exist");
                return Enumerable.Empty<IEdge>();
            }
            ISlot slot = node.FindSlot<ISlot>(s.slotId);

            List<IEdge> candidateEdges;
            if (!m_NodeEdges.TryGetValue(s.nodeGuid, out candidateEdges))
                return Enumerable.Empty<IEdge>();

            return candidateEdges.Where(candidateEdge =>
                {
                    var cs = slot.isInputSlot ? candidateEdge.inputSlot : candidateEdge.outputSlot;
                    return cs.nodeGuid == s.nodeGuid && cs.slotId == s.slotId;
                });
        }

        public virtual void OnBeforeSerialize()
        {
            m_SerializableNodes = SerializationHelper.Serialize<INode>(m_Nodes.Values);
            m_SerializableEdges = SerializationHelper.Serialize<IEdge>(m_Edges);
        }

        public virtual void OnAfterDeserialize()
        {
            var nodes = SerializationHelper.Deserialize<INode>(m_SerializableNodes, GetLegacyTypeRemapping());
            m_Nodes = new Dictionary<Guid, INode>(nodes.Count);
            foreach (var node in nodes)
            {
                node.owner = this;
                node.UpdateNodeAfterDeserialization();
                m_Nodes.Add(node.guid, node);
            }

            m_SerializableNodes = null;

            m_Edges = SerializationHelper.Deserialize<IEdge>(m_SerializableEdges, null);
            m_SerializableEdges = null;
            foreach (var edge in m_Edges)
                AddEdgeToNodeEdges(edge);
        }

        public virtual void ValidateGraph()
        {
            //First validate edges, remove any
            //orphans. This can happen if a user
            //manually modifies serialized data
            //of if they delete a node in the inspector
            //debug view.
            foreach (var edge in edges.ToArray())
            {
                var outputNode = GetNodeFromGuid(edge.outputSlot.nodeGuid);
                var inputNode = GetNodeFromGuid(edge.inputSlot.nodeGuid);

                if (outputNode == null
                    || inputNode == null
                    || outputNode.FindOutputSlot<ISlot>(edge.outputSlot.slotId) == null
                    || inputNode.FindInputSlot<ISlot>(edge.inputSlot.slotId) == null)
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
                    Debug.LogWarningFormat("Added edge is invalid: {0} -> {1}", edge.outputSlot.nodeGuid, edge.inputSlot.nodeGuid);
                    m_AddedEdges.Remove(edge);
                }
            }
        }

        public virtual void ReplaceWith(IGraph other)
        {
            other.ValidateGraph();
            ValidateGraph();

            // Current tactic is to remove all nodes and edges and then re-add them, such that depending systems
            // will re-initialize with new references.
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
                removedNodeGuids.AddRange(m_Nodes.Keys);
                foreach (var nodeGuid in removedNodeGuids)
                    RemoveNodeNoValidate(m_Nodes[nodeGuid]);
            }

            ValidateGraph();

            foreach (var node in other.GetNodes<INode>())
                AddNodeNoValidate(node);

            foreach (var edge in other.edges)
                ConnectNoValidate(edge.outputSlot, edge.inputSlot);

            ValidateGraph();
        }

        public void OnEnable()
        {
            foreach (var node in GetNodes<INode>().OfType<IOnAssetEnabled>())
            {
                node.OnEnable();
            }
        }
    }
}
