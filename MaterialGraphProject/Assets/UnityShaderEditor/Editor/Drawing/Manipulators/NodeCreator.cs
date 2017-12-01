using System;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class NodeCreator : MouseManipulator
    {
        VisualElement m_ContentViewContainer;
        IGraph m_Graph;
        Vector2 m_MouseUpPosition;
        GenericMenu m_Menu;

        public NodeCreator(IGraph graph)
        {
            m_Graph = graph;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            m_Menu = new GenericMenu();
            foreach (var type in Assembly.GetAssembly(typeof(AbstractMaterialNode)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(AbstractMaterialNode))))
                {
                    var attrs = type.GetCustomAttributes(typeof(TitleAttribute), false) as TitleAttribute[];
                    if (attrs != null && attrs.Length > 0)
                        m_Menu.AddItem(new GUIContent(attrs[0].m_Title), false, OnAddNode, type);
                }
            }

            m_Menu.AddSeparator("");
            m_Menu.AddItem(new GUIContent("Convert to Property"), false, OnConvertToProperty);
            m_Menu.AddItem(new GUIContent("Convert to Inline node"), false, OnConvertToInlineNode);
        }

        private void OnConvertToInlineNode()
        {
            if (m_GraphView == null)
                return;

            var slected = m_GraphView.selection.OfType<MaterialNodeView>()
                .Select(x => x.node)
                .OfType<PropertyNode>();

            foreach (var propNode in slected)
                ((AbstractMaterialGraph)propNode.owner).ReplacePropertyNodeWithConcreteNode(propNode);
        }

        private void OnConvertToProperty()
        {
            if (m_GraphView == null)
                return;

            var graph = m_Graph as AbstractMaterialGraph;
            if (graph == null)
                return;

            var slected = m_GraphView.selection.OfType<MaterialNodeView>().Select(x => x.node);

            foreach (var node in slected.ToArray())
            {
                if (!(node is IPropertyFromNode))
                    continue;

                var converter = node as IPropertyFromNode;
                var prop = converter.AsShaderProperty();
                graph.AddShaderProperty(prop);

                var propNode = new PropertyNode();
                propNode.drawState = node.drawState;
                graph.AddNode(propNode);
                propNode.propertyGuid = prop.guid;

                var oldSlot = node.FindSlot<MaterialSlot>(converter.outputSlotId);
                var newSlot = propNode.FindSlot<MaterialSlot>(PropertyNode.OutputSlotId);

                var edges = graph.GetEdges(oldSlot.slotReference).ToArray();
                foreach (var edge in edges)
                    graph.Connect(newSlot.slotReference, edge.inputSlot);

                graph.RemoveNode(node);
            }
        }

        void OnAddNode(object userData)
        {
            var type = userData as Type;
            var node = Activator.CreateInstance(type) as INode;
            if (node == null)
                return;

            var drawState = node.drawState;
            drawState.position = new Rect(m_MouseUpPosition.x, m_MouseUpPosition.y, 0, 0);
            node.drawState = drawState;

            m_Graph.owner.RegisterCompleteObjectUndo("Add " + node.name);
            m_Graph.AddNode(node);
        }

        private GraphView m_GraphView;
        protected override void RegisterCallbacksOnTarget()
        {
            m_GraphView = target as GraphView;
            if (m_GraphView == null)
                return;

            m_ContentViewContainer = m_GraphView.contentViewContainer;
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                m_MouseUpPosition = m_ContentViewContainer.transform.matrix.inverse.MultiplyPoint3x4(evt.localMousePosition);
                m_Menu.ShowAsContext();
                evt.StopPropagation();
            }
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }
    }
}
