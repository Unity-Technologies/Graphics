using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class MaterialGraphDataSource : ICanvasDataSource
    {
        readonly List<DrawableMaterialNode> m_DrawableNodes = new List<DrawableMaterialNode>();
        
        public MaterialGraph graph { get; set; }

        public ICollection<DrawableMaterialNode> lastGeneratedNodes
        {
            get { return m_DrawableNodes; }
        }

        public CanvasElement[] FetchElements()
        {
            m_DrawableNodes.Clear();
            Debug.Log("trying to convert");
            var pixelGraph = graph.currentGraph;
            foreach (var node in pixelGraph.nodes)
            {
                // add the nodes
                var bmn = node as BaseMaterialNode;
                m_DrawableNodes.Add(new DrawableMaterialNode(bmn, (bmn is PixelShaderNode) ? 600.0f : 200.0f, this));
            }

            // Add the edges now
            var drawableEdges = new List<Edge<NodeAnchor>>();
            foreach (var drawableMaterialNode in m_DrawableNodes)
            {
                var baseNode = drawableMaterialNode.m_Node;
                foreach (var slot in baseNode.outputSlots)
                {
                    var sourceAnchor =  (NodeAnchor)drawableMaterialNode.Children().FirstOrDefault(x => x is NodeAnchor && ((NodeAnchor) x).m_Slot == slot);

                    foreach (var edge in slot.edges)
                    {
                        var targetNode = m_DrawableNodes.FirstOrDefault(x => x.m_Node == edge.toSlot.node);
                        var targetAnchor = (NodeAnchor)targetNode.Children().FirstOrDefault(x => x is NodeAnchor && ((NodeAnchor) x).m_Slot == edge.toSlot);
                        drawableEdges.Add(new Edge<NodeAnchor>(this, sourceAnchor, targetAnchor));
                    }
                }
            }
            
            // Add proxy inputs for when edges are not connect
            var nullInputSlots = new List<NullInputProxy>();
            foreach (var drawableMaterialNode in m_DrawableNodes)
            {
                var baseNode = drawableMaterialNode.m_Node;
                // grab the input slots where there are no edges
                foreach (var slot in baseNode.GetDrawableInputProxies())
                {
                    // if there is no anchor, continue
                    // this can happen if we are in collapsed mode
                    var sourceAnchor = (NodeAnchor)drawableMaterialNode.Children().FirstOrDefault(x => x is NodeAnchor && ((NodeAnchor)x).m_Slot == slot);
                    if (sourceAnchor == null)
                        continue;

                    nullInputSlots.Add(new NullInputProxy(slot, sourceAnchor));
                }
            }
            var toReturn = new List<CanvasElement>();
            toReturn.AddRange(m_DrawableNodes.Select(x => (CanvasElement)x));
            toReturn.AddRange(drawableEdges.Select(x => (CanvasElement)x));
            toReturn.AddRange(nullInputSlots.Select(x => (CanvasElement)x));
            
            Debug.LogFormat("REturning {0} nodes", toReturn.Count);
            return toReturn.ToArray();
        }

        public void DeleteElement(CanvasElement e)
        {
            // do nothing here, we want to use the 'correct' 
            // delete elements.
        }

        public void DeleteElements(List<CanvasElement> elements)
        {
            // delete selected edges first
            foreach (var e in elements.Where(x => x is Edge<NodeAnchor>))
            {
                //find the edge
                var localEdge = (Edge<NodeAnchor>) e;
                var edge = graph.currentGraph.edges.FirstOrDefault(x => x.fromSlot == localEdge.Left.m_Slot && x.toSlot == localEdge.Right.m_Slot);

                Debug.Log("Deleting edge " + edge);
                graph.currentGraph.RemoveEdgeNoRevalidate(edge);
            }

            // now delete edges that the selected nodes use
            foreach (var e in elements.Where(x => x is DrawableMaterialNode))
            {
                var node = ((DrawableMaterialNode) e).m_Node;

                foreach (var slot in node.slots)
                {
                    for (int index = slot.edges.Count -1; index >= 0; --index)
                    {
                        var edge = slot.edges[index];
                        Debug.Log("Deleting edge " + edge);
                        graph.currentGraph.RemoveEdgeNoRevalidate(edge);
                    }
                }
            }

            // now delete the nodes
            foreach (var e in elements.Where(x => x is DrawableMaterialNode))
            {
                Debug.Log("Deleting node " + e + " " + ((DrawableMaterialNode) e).m_Node);
                graph.currentGraph.RemoveNode(((DrawableMaterialNode) e).m_Node);
            }
            graph.currentGraph.RevalidateGraph();
        }

        public void Connect(NodeAnchor a, NodeAnchor b)
        {
            var pixelGraph = graph.currentGraph;
            pixelGraph.Connect(a.m_Slot, b.m_Slot);
        }

        private string m_LastPath;
        public void Export(bool quickExport)
        {
            var path = quickExport ? m_LastPath : EditorUtility.SaveFilePanelInProject("Export shader to file...", "shader.shader", "shader", "Enter file name");
            m_LastPath = path; // For quick exporting
            if (!string.IsNullOrEmpty(path))
                graph.ExportShader(path);
            else
                EditorUtility.DisplayDialog("Export Shader Error", "Cannot export shader", "Ok");
        }
    }
}
