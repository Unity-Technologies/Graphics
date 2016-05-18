using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public class SlotReference
    {
        [SerializeField]
        private string m_NodeGUIDSerialized;

        [NonSerialized]
        private GUID m_NodeGUID;

        [SerializeField]
        private string m_SlotName;

        public SlotReference(GUID nodeGuid, string slotName)
        {
            m_NodeGUID = nodeGuid;
            m_SlotName = slotName;
        }

        public GUID nodeGuid
        {
            get { return m_NodeGUID; }
        }

        public string slotName
        {
            get { return m_SlotName; }
        }

        public void BeforeSerialize()
        {
            m_NodeGUIDSerialized = m_NodeGUID.ToString();
        }

        public void AfterDeserialize()
        {
            m_NodeGUID = new GUID(m_NodeGUIDSerialized);
        }
    }

    [Serializable]
    public class Edge
    {
        [SerializeField]
        private SlotReference m_OutputSlot;
        [SerializeField]
        private SlotReference m_InputSlot;

        public Edge(SlotReference outputSlot, SlotReference inputSlot)
        {
            m_OutputSlot = outputSlot;
            m_InputSlot = inputSlot;
        }

        public SlotReference outputSlot
        {
            get { return m_OutputSlot; }
        }

        public SlotReference inputSlot
        {
            get { return m_InputSlot; }
        }
    }

    public abstract class BaseMaterialGraph
    {
        private PreviewRenderUtility m_PreviewUtility;
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

        private List<BaseMaterialNode> m_Nodes = new List<BaseMaterialNode>();
        private List<Edge> m_Edges = new List<Edge>();

        protected List<BaseMaterialNode> nodes
        {
            get
            {
                return m_Nodes;
            }
        } 

        public bool requiresRepaint
        {
            get { return nodes.Any(x => x is IRequiresTime); }
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

        public BaseMaterialNode GetNodeFromGUID(GUID guid)
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
            var bmns = nodes.Where(x => x is BaseMaterialNode).Cast<BaseMaterialNode>().ToList();

            foreach (var node in bmns)
                node.InvalidateNode();

            foreach (var node in bmns)
            {
                node.ValidateNode();
            }
        }

        public void AddNode(BaseMaterialNode node)
        {
            m_Nodes.Add(node);
            RevalidateGraph();
        }
    }
}
