using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public abstract class BaseMaterialGraph : Graph
    {
        private PreviewRenderUtility m_PreviewUtility;

        public PreviewRenderUtility previewUtility
        {
            get
            {
                if (m_PreviewUtility == null)
                {
                    m_PreviewUtility = new PreviewRenderUtility();
                    EditorUtility.SetCameraAnimateMaterials(m_PreviewUtility.m_Camera, true);
                }

                return m_PreviewUtility;
            }
        }

        public bool requiresRepaint
        {
            get { return isAwake && nodes.Any(x => x is IRequiresTime); }
        }
        
        public override void RemoveEdge(Edge e)
        {
            base.RemoveEdge(e);

            var toNode = e.toSlot.node as BaseMaterialNode;
            if (toNode == null)
                return;
            
            RevalidateGraph();
        }

        public override Edge Connect(Slot fromSlot, Slot toSlot)
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
            foreach (var edge in inputSlot.edges.ToArray())
            {
                Debug.Log("Removing existing edge:" + edge);
                // call base here as we DO NOT want to
                // do expensive shader regeneration
                base.RemoveEdge(edge);
            }
            
            var newEdge = base.Connect(outputSlot, inputSlot);
            
            Debug.Log("Connected edge: " + newEdge);
            var toNode = inputSlot.node as BaseMaterialNode;
            var fromNode = outputSlot.node as BaseMaterialNode;

            if (fromNode == null || toNode == null)
                return newEdge;
            
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

        public override void AddNode(Node node)
        {
            base.AddNode(node);
            AssetDatabase.AddObjectToAsset(node, this);
            RevalidateGraph();
        }

        protected void AddMasterNodeNoAddToAsset(Node node)
        {
            base.AddNode(node);
        }
    }
}
