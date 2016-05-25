using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public class SerializableGraph : ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<Edge> m_Edges = new List<Edge>();

        [NonSerialized]
        private List<SerializableNode> m_Nodes = new List<SerializableNode>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        public IEnumerable<SerializableNode> nodes
        {
            get { return m_Nodes; }
        }

        public IEnumerable<Edge> edges
        {
            get { return m_Edges; }
        }

        public virtual void AddNode(SerializableNode node)
        {
            m_Nodes.Add(node);
            ValidateGraph();
        }

        public virtual void RemoveNode(SerializableNode node)
        {
            if (!node.canDeleteNode)
                return;

            m_Nodes.Remove(node);
            ValidateGraph();
        }

        private void RemoveNodeNoValidate(SerializableNode node)
        {
            if (!node.canDeleteNode)
                return;

            m_Nodes.Remove(node);
        }

        public virtual Edge Connect(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            SerializableNode fromNode = GetNodeFromGuid(fromSlotRef.nodeGuid);
            SerializableNode toNode = GetNodeFromGuid(toSlotRef.nodeGuid);

            if (fromNode == null || toNode == null)
                return null;

            SerializableSlot fromSlot = fromNode.FindSlot(fromSlotRef.slotName);
            SerializableSlot toSlot = toNode.FindSlot(toSlotRef.slotName);

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
                Debug.Log("Removing existing edge:" + edge);
                // call base here as we DO NOT want to
                // do expensive shader regeneration
                RemoveEdge(edge);
            }

            var newEdge = new Edge(outputSlot, inputSlot);
            m_Edges.Add(newEdge);

            Debug.Log("Connected edge: " + newEdge);
            ValidateGraph();
            return newEdge;
        }

        public virtual void RemoveEdge(Edge e)
        {
            m_Edges.Remove(e);
            ValidateGraph();
        }

        public void RemoveElements(IEnumerable<SerializableNode> nodes, IEnumerable<Edge> edges)
        {
            foreach (var edge in edges)
                RemoveEdgeNoValidate(edge);

            foreach (var serializableNode in nodes)
                RemoveNodeNoValidate(serializableNode);

            ValidateGraph();
        }

        private void RemoveEdgeNoValidate(Edge e)
        {
            m_Edges.Remove(e);
        }

        public SerializableNode GetNodeFromGuid(Guid guid)
        {
            return m_Nodes.FirstOrDefault(x => x.guid == guid);
        }

        public IEnumerable<Edge> GetEdges(SlotReference s)
        {
            return m_Edges.Where(x =>
                (x.outputSlot.nodeGuid == s.nodeGuid && x.outputSlot.slotName == s.slotName)
                || x.inputSlot.nodeGuid == s.nodeGuid && x.inputSlot.slotName == s.slotName);
        }

        public virtual void OnBeforeSerialize()
        {
            m_SerializableNodes = SerializationHelper.Serialize(m_Nodes);
        }

        public virtual void OnAfterDeserialize()
        {
            m_Nodes = SerializationHelper.Deserialize<SerializableNode>(m_SerializableNodes, new object[] {this});
            m_SerializableNodes = null;

            ValidateGraph();
        }

        public virtual void ValidateGraph()
        {
            //First validate edges, remove any
            //orphans. This can happen if a user
            //manually modifies serialized data
            //of if they delete a node in the inspector
            //debug view.
            var allNodeGUIDs = nodes.Select(x => x.guid).ToList();

            foreach (var edge in edges.ToArray())
            {
                if (allNodeGUIDs.Contains(edge.inputSlot.nodeGuid) && allNodeGUIDs.Contains(edge.outputSlot.nodeGuid))
                    continue;

                //orphaned edge
                RemoveEdgeNoValidate(edge);
            }
        }
    }
}
