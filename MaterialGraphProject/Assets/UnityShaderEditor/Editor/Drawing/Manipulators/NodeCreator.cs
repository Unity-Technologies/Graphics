using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
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

        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
                return;
            m_ContentViewContainer = graphView.contentViewContainer;
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
