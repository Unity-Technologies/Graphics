using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class MaterialGraphDataSource : ICanvasDataSource
    {
        public MaterialGraph graph { get; set; }

        public CanvasElement[] FetchElements()
        {
            var drawableNodes = new List<DrawableMaterialNode>();
            Debug.Log("trying to convert");
            var pixelGraph = graph.currentGraph;
            foreach (var node in pixelGraph.nodes)
            {
                // add the nodes
                var bmn = node as BaseMaterialNode;
                drawableNodes.Add(new DrawableMaterialNode(bmn, 200.0f, typeof(Vector4), this));
            }

            // Add the edges now
            var drawableEdges = new List<Edge<NodeAnchor>>();
            foreach (var drawableMaterialNode in drawableNodes)
            {
                var baseNode = drawableMaterialNode.m_Node;
                foreach (var slot in baseNode.outputSlots)
                {
                    var sourceAnchor =  (NodeAnchor)drawableMaterialNode.Children().FirstOrDefault(x => x is NodeAnchor && ((NodeAnchor) x).m_Slot == slot);

                    foreach (var edge in slot.edges)
                    {
                        var targetNode = drawableNodes.FirstOrDefault(x => x.m_Node == edge.toSlot.node);
                        var targetAnchor = (NodeAnchor)targetNode.Children().FirstOrDefault(x => x is NodeAnchor && ((NodeAnchor) x).m_Slot == edge.toSlot);
                        drawableEdges.Add(new Edge<NodeAnchor>(this, sourceAnchor, targetAnchor));
                    }
                }
            }
            
            var toReturn = new List<CanvasElement>();
            toReturn.AddRange(drawableNodes.Select(x => (CanvasElement)x));
            toReturn.AddRange(drawableEdges.Select(x => (CanvasElement)x));
            
            Debug.LogFormat("REturning {0} nodes", toReturn.Count);
            return toReturn.ToArray();
        }

        public void DeleteElement(CanvasElement e)
        {
            //m_Elements.Remove(e);
        }

        public void Connect(NodeAnchor a, NodeAnchor b)
        {
            //m_Elements.Add();
        }
    }
}
