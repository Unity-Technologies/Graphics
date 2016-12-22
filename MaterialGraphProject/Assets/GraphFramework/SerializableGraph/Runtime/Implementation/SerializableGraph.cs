using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Graphing
{
    [Serializable]
    public class SerializableGraph : IGraph, ISerializationCallbackReceiver
    {
        [NonSerialized]
        private List<IEdge> m_Edges = new List<IEdge>();

        [NonSerialized]
        private List<INode> m_Nodes = new List<INode>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = new List<SerializationHelper.JSONSerializedElement>();

        public IEnumerable<T> GetNodes<T>() where T : INode
        {
            return m_Nodes.OfType<T>();
        }

        public IEnumerable<IEdge> edges
        {
            get { return m_Edges; }
        }

        public virtual void AddNode(INode node)
        {
            m_Nodes.Add(node);
            node.owner = this;
            ValidateGraph();
        }

        public virtual void RemoveNode(INode node)
        {
            if (!node.canDeleteNode)
                return;

            m_Nodes.Remove(node);
            ValidateGraph();
        }

        private void RemoveNodeNoValidate(INode node)
        {
            if (!node.canDeleteNode)
                return;

            m_Nodes.Remove(node);
        }

        public virtual Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            return new Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo>();
        }

        public virtual IEdge Connect(SlotReference fromSlotRef, SlotReference toSlotRef)
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

            Debug.Log("Connected edge: " + newEdge);
            ValidateGraph();
            return newEdge;
        }

        public virtual void RemoveEdge(IEdge e)
        {
            m_Edges.Remove(e);
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

        private void RemoveEdgeNoValidate(IEdge e)
        {
            m_Edges.Remove(e);
        }

        public INode GetNodeFromGuid(Guid guid)
        {
            return m_Nodes.FirstOrDefault(x => x.guid == guid);
        }

        public T GetNodeFromGuid<T>(Guid guid) where T : INode
        {
            return m_Nodes.Where(x => x.guid == guid).OfType<T>().FirstOrDefault();
        }

        public IEnumerable<IEdge> GetEdges(SlotReference s)
        {
            if (s == null)
                return new Edge[0];

            return m_Edges.Where(x =>
                (x.outputSlot.nodeGuid == s.nodeGuid && x.outputSlot.slotId == s.slotId)
                || x.inputSlot.nodeGuid == s.nodeGuid && x.inputSlot.slotId == s.slotId).ToList();
        }

        public virtual void OnBeforeSerialize()
        {
            m_SerializableNodes = SerializationHelper.Serialize<INode>(m_Nodes);
            m_SerializableEdges = SerializationHelper.Serialize<IEdge>(m_Edges);
        }

        public virtual void OnAfterDeserialize()
        {
            m_Nodes = SerializationHelper.Deserialize<INode>(m_SerializableNodes, GetLegacyTypeRemapping());
            foreach (var node in m_Nodes)
                node.owner = this;

            m_SerializableNodes = null;

            m_Edges = SerializationHelper.Deserialize<IEdge>(m_SerializableEdges, null);
            m_SerializableEdges = null;

            ValidateGraph();
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
