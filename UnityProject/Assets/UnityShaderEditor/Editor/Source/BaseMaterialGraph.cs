using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public abstract class BaseMaterialGraph : ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<Edge> m_Edges = new List<Edge>();

        [SerializeField]
        private List<BaseMaterialNode> m_Nodes = new List<BaseMaterialNode>();

        private PreviewRenderUtility m_PreviewUtility;

        private MaterialGraph m_Owner;

        public BaseMaterialGraph(MaterialGraph owner)
        {
            m_Owner = owner;
        }

        public IEnumerable<BaseMaterialNode> nodes { get { return m_Nodes; } } 
        public IEnumerable<Edge> edges { get { return m_Edges; } } 

        public PreviewRenderUtility previewUtility
        {
            get
            {
                if (m_PreviewUtility == null)
                {
                    m_PreviewUtility = new PreviewRenderUtility();
                    // EditorUtility.SetCameraAnimateMaterials(m_PreviewUtility.m_Camera, true);
                }

                return m_PreviewUtility;
            }
        }

        public bool requiresRepaint
        {
            get { return m_Nodes.Any(x => x is IRequiresTime); }
        }

        public MaterialGraph owner
        {
            get { return m_Owner; }
        }

        public void RemoveEdge(Edge e)
        {
            m_Edges.Remove(e);
            RevalidateGraph();
        }

        public void RemoveEdgeNoRevalidate(Edge e)
        {
            m_Edges.Remove(e);
        }

        public void RemoveNode(BaseMaterialNode node)
        {
            if (!node.canDeleteNode)
                return;

            m_Nodes.Remove(node);
        }

        public BaseMaterialNode GetNodeFromGUID(Guid guid)
        {
            return m_Nodes.FirstOrDefault(x => x.guid == guid);
        }

        public IEnumerable<Edge> GetEdges(Slot s)
        {
            return m_Edges.Where(x =>
                (x.outputSlot.nodeGuid == s.nodeGuid && x.outputSlot.slotName == s.name)
                || x.inputSlot.nodeGuid == s.nodeGuid && x.inputSlot.slotName == s.name);
        }

        public Edge Connect(Slot fromSlot, Slot toSlot)
        {
            Slot outputSlot = null;
            Slot inputSlot = null;

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

            // remove any inputs that exits before adding
            foreach (var edge in GetEdges(inputSlot))
            {
                Debug.Log("Removing existing edge:" + edge);
                // call base here as we DO NOT want to
                // do expensive shader regeneration
                RemoveEdge(edge);
            }

            var newEdge = new Edge(new SlotReference(outputSlot.nodeGuid, outputSlot.name), new SlotReference(inputSlot.nodeGuid, inputSlot.name));
            m_Edges.Add(newEdge);

            Debug.Log("Connected edge: " + newEdge);
            RevalidateGraph();
            return newEdge;
        }

        public virtual void RevalidateGraph()
        {
            var bmns = m_Nodes.Where(x => x is BaseMaterialNode).Cast<BaseMaterialNode>().ToList();

            foreach (var node in bmns)
                node.InvalidateNode();

            foreach (var node in bmns)
                node.ValidateNode();
        }

        public void AddNode(BaseMaterialNode node)
        {
            m_Nodes.Add(node);
            RevalidateGraph();
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            foreach (var node in nodes)
            {
                node.owner = this;
            }
        }
    }
}
