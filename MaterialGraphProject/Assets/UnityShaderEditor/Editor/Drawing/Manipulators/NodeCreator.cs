using System;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class NodeCreator : MouseManipulator
    {
        SearchWindowProvider m_SearchWindowProvider;

        public NodeCreator(SearchWindowProvider searchWindowProvider)
        {
            m_SearchWindowProvider = searchWindowProvider;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(evt.mousePosition)), m_SearchWindowProvider);
                evt.StopPropagation();
            }
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }
    }
}
