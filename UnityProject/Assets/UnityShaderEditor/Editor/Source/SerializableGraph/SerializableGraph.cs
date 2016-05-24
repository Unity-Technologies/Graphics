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

        public virtual Edge Connect(SerializableSlot fromSlot, SerializableSlot toSlot)
        {
            SerializableSlot outputSlot = null;
            SerializableSlot inputSlot = null;

            // output must connect to input
            if (fromSlot.isOutputSlot)
                outputSlot = fromSlot;
            else if (fromSlot.isInputSlot)
                inputSlot = fromSlot;

            if (toSlot.isOutputSlot)
                outputSlot = toSlot;
            else if (toSlot.isInputSlot)
                inputSlot = toSlot;

            if (inputSlot == null || outputSlot == null)
                return null;

            var edges = GetEdges(inputSlot).ToList();
            // remove any inputs that exits before adding
            foreach (var edge in edges)
            {
                Debug.Log("Removing existing edge:" + edge);
                // call base here as we DO NOT want to
                // do expensive shader regeneration
                RemoveEdge(edge);
            }

            var newEdge = new Edge(new SlotReference(outputSlot.owner.guid, outputSlot.name), new SlotReference(inputSlot.owner.guid, inputSlot.name));
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

        public IEnumerable<Edge> GetEdges(SerializableSlot s)
        {
            return m_Edges.Where(x =>
                (x.outputSlot.nodeGuid == s.owner.guid && x.outputSlot.slotName == s.name)
                || x.inputSlot.nodeGuid == s.owner.guid && x.inputSlot.slotName == s.name);
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
