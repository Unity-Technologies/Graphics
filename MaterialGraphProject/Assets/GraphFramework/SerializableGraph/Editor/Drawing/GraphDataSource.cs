using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class CustomNodeUI : Attribute
    {
        private Type m_ModeToDrawFor;

        public CustomNodeUI(Type nodeToDrawFor)
        {
            m_ModeToDrawFor = nodeToDrawFor;
        }

        public Type nodeToDrawFor
        {
            get { return m_ModeToDrawFor; } }
    }

    public class GraphDataSource : ICanvasDataSource
    {
        readonly List<DrawableNode> m_DrawableNodes = new List<DrawableNode>();

        public IGraphAsset graphAsset { get; set; }

        public ICollection<DrawableNode> lastGeneratedNodes
        {
            get { return m_DrawableNodes; }
        }

        private static Type[] GetTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return new Type[] {};
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return new Type[] {};
            }
        }

        private static Dictionary<Type, Type> s_DrawerUI;

        private static Dictionary<Type, Type> drawerUI
        {
            get
            {
                if (s_DrawerUI == null)
                {
                    s_DrawerUI = new Dictionary<Type, Type>();
                    var loadedTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => GetTypesFromAssembly(x));

                    foreach (var type in loadedTypes)
                    {
                        var attribute = type.GetCustomAttributes(true).OfType<CustomNodeUI>().FirstOrDefault();
                        if (attribute != null && typeof(ICustomNodeUi).IsAssignableFrom(type))
                            s_DrawerUI.Add(attribute.nodeToDrawFor, type);
                    }
                }
                return s_DrawerUI;
            }
        }

        public CanvasElement[] FetchElements()
        {
            m_DrawableNodes.Clear();
            var graph = graphAsset.graph;
            Debug.LogFormat("Trying to convert: {0}", graphAsset.graph);
            foreach (var node in graph.GetNodes<INode>())
            {
                var nodeType = node.GetType();
                Type draweruiType = null;

                while (draweruiType == null && nodeType != null)
                {
                    draweruiType = drawerUI.FirstOrDefault(x => x.Key == nodeType).Value;
                    nodeType = nodeType.BaseType;
                }

                ICustomNodeUi customUI = null;
                if (draweruiType != null)
                {
                    try
                    {
                        customUI = Activator.CreateInstance(draweruiType) as ICustomNodeUi;
                        customUI.node = node;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarningFormat("Could not construct instance of: {0} - {1}", draweruiType, e);
                    }
                }

                // add the nodes
                m_DrawableNodes.Add(new DrawableNode(node, customUI, this));
            }

            // Add the edges now
            var drawableEdges = new List<DrawableEdge<NodeAnchor>>();
            foreach (var drawableMaterialNode in m_DrawableNodes)
            {
                var baseNode = drawableMaterialNode.m_Node;
                foreach (var slot in baseNode.GetOutputSlots<ISlot>())
                {
                    var sourceAnchor =  (NodeAnchor)drawableMaterialNode.Children().FirstOrDefault(x => x is NodeAnchor && ((NodeAnchor)x).m_Slot == slot);

                    var edges = baseNode.owner.GetEdges(new SlotReference(baseNode.guid, slot.id));
                    foreach (var edge in edges)
                    {
                        var toNode = baseNode.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                        var toSlot = toNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                        var targetNode = m_DrawableNodes.FirstOrDefault(x => x.m_Node == toNode);
                        var targetAnchor = (NodeAnchor)targetNode.Children().FirstOrDefault(x => x is NodeAnchor && ((NodeAnchor)x).m_Slot == toSlot);
                        drawableEdges.Add(new DrawableEdge<NodeAnchor>(edge, this, sourceAnchor, targetAnchor));
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
            var graph = graphAsset.graph;
            var toRemoveEdge = new List<IEdge>();
            // delete selected edges first
            foreach (var e in elements.OfType<Edge<NodeAnchor>>())
            {
                //find the edge
                var edge = graph.edges.FirstOrDefault(x => graph.GetNodeFromGuid(x.outputSlot.nodeGuid).FindOutputSlot<ISlot>(x.outputSlot.slotId) == e.Left.m_Slot
                        && graph.GetNodeFromGuid(x.inputSlot.nodeGuid).FindInputSlot<ISlot>(x.inputSlot.slotId) == e.Right.m_Slot);

                toRemoveEdge.Add(edge);
            }

            var toRemoveNode = new List<INode>();
            // now delete the nodes
            foreach (var e in elements.OfType<DrawableNode>())
            {
                if (!e.m_Node.canDeleteNode)
                    continue;
                toRemoveNode.Add(e.m_Node);
            }
            graph.RemoveElements(toRemoveNode, toRemoveEdge);
            MarkDirty();
        }

        public void Connect(NodeAnchor a, NodeAnchor b)
        {
            var graph = graphAsset.graph;
            graph.Connect(a.m_Node.GetSlotReference(a.m_Slot.id), b.m_Node.GetSlotReference(b.m_Slot.id));
            MarkDirty();
        }

        public void Addnode(INode node)
        {
            var graph = graphAsset.graph;
            graph.AddNode(node);
            MarkDirty();
        }

        public void MarkDirty()
        {
            EditorUtility.SetDirty(graphAsset.GetScriptableObject());
        }

        /*      
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
                }*/
    }
}
