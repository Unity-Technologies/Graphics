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
                var bmn = node as AbstractMaterialNode;
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

                    var edges = baseNode.owner.GetEdges(new SlotReference(baseNode.guid, slot.name));
                    foreach (var edge in edges)
                    {
                        var toNode = baseNode.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                        var toSlot = toNode.FindInputSlot(edge.inputSlot.slotName);
                        var targetNode = m_DrawableNodes.FirstOrDefault(x => x.m_Node == toNode);
                        var targetAnchor = (NodeAnchor)targetNode.Children().FirstOrDefault(x => x is NodeAnchor && ((NodeAnchor) x).m_Slot == toSlot);
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
                foreach (var slot in baseNode.GetInputsWithNoConnection())
                {
                    // if there is no anchor, continue
                    // this can happen if we are in collapsed mode
                    var sourceAnchor = (NodeAnchor)drawableMaterialNode.Children().FirstOrDefault(x => x is NodeAnchor && ((NodeAnchor)x).m_Slot == slot);
                    if (sourceAnchor == null)
                        continue;

                    nullInputSlots.Add(new NullInputProxy(baseNode, slot, sourceAnchor));
                }
            }
            var toReturn = new List<CanvasElement>();
            toReturn.AddRange(m_DrawableNodes.Select(x => (CanvasElement)x));
            toReturn.AddRange(drawableEdges.Select(x => (CanvasElement)x));
            toReturn.AddRange(nullInputSlots.Select(x => (CanvasElement)x));
            
            //toReturn.Add(new FloatingPreview(new Rect(Screen.width - 300, Screen.height - 300, 300, 300), pixelGraph.nodes.FirstOrDefault(x => x is PixelShaderNode)));

            Debug.LogFormat("Returning {0} nodes", m_DrawableNodes.Count);
            Debug.LogFormat("Returning {0} drawableEdges", drawableEdges.Count);
            Debug.LogFormat("Returning {0} nullInputSlots", nullInputSlots.Count);
            return toReturn.ToArray();
        }

        public void DeleteElement(CanvasElement e)
        {
            // do nothing here, we want to use delete elements.
            // delete elements ensures that edges are deleted before nodes.
        }

        public void DeleteElements(List<CanvasElement> elements)
        {
            var toRemoveEdge = new List<Edge>();
            // delete selected edges first
            foreach (var e in elements.Where(x => x is Edge<NodeAnchor>))
            {
                //find the edge
                var localEdge = (Edge<NodeAnchor>) e;
                var edge = graph.currentGraph.edges.FirstOrDefault(x => graph.currentGraph.GetNodeFromGuid(x.outputSlot.nodeGuid).FindOutputSlot(x.outputSlot.slotName) == localEdge.Left.m_Slot 
                    && graph.currentGraph.GetNodeFromGuid(x.inputSlot.nodeGuid).FindInputSlot(x.inputSlot.slotName) == localEdge.Right.m_Slot);

                toRemoveEdge.Add(edge);
            }

            var toRemoveNode = new List<SerializableNode>();
            // now delete the nodes
            foreach (var e in elements.Where(x => x is DrawableMaterialNode))
            {
                var node = ((DrawableMaterialNode) e).m_Node;
                if (!node.canDeleteNode)
                    continue;
                toRemoveNode.Add(node);
            }
            graph.currentGraph.RemoveElements(toRemoveNode, toRemoveEdge);
        }

        public void Connect(NodeAnchor a, NodeAnchor b)
        {
            var pixelGraph = graph.currentGraph;
            pixelGraph.Connect(a.m_Node.GetSlotReference(a.m_Slot.name), b.m_Node.GetSlotReference(b.m_Slot.name));
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

    public class FloatingPreview : CanvasElement
    {
        private AbstractMaterialNode m_Node;

        public FloatingPreview(Rect position, AbstractMaterialNode node)
        {
            m_Node = node as AbstractMaterialNode;
            m_Translation = new Vector2(position.x, position.y);
            m_Scale = new Vector3(position.width, position.height, 1);
            m_Caps |= Capabilities.Floating | Capabilities.Unselectable;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            var drawArea = new Rect(0, 0, scale.x, scale.y);
            Color backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
            EditorGUI.DrawRect(drawArea, backgroundColor);

            drawArea.width -= 10;
            drawArea.height -= 10;
            drawArea.x += 5;
            drawArea.y += 5;

            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GUI.DrawTexture(drawArea, m_Node.RenderPreview(new Rect(0, 0, drawArea.width, drawArea.height)), ScaleMode.StretchToFill, false);
            GL.sRGBWrite = false;

            Invalidate();
            canvas.Repaint();
        }
    }
}
