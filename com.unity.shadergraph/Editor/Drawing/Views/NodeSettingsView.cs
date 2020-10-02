using System;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class NodeSettingsView : VisualElement
    {
        VisualElement m_ContentContainer;

        public NodeSettingsView()
        {
            pickingMode = PickingMode.Ignore;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/NodeSettings"));
            var uxml = Resources.Load<VisualTreeAsset>("UXML/NodeSettings");
            {
                // CloneTree now respect the target provided by contentContainer
                // The following lines preserves the old behavior of tpl.instantiate(this).
                var ve = uxml.Instantiate();
                while (ve.hierarchy.childCount != 0)
                    this.hierarchy.Add(ve.hierarchy[0]);
            }

            // Get the element we want to use as content container
            m_ContentContainer = this.Q("contentContainer");
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            evt.StopPropagation();
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            evt.StopPropagation();
        }

        public override VisualElement contentContainer
        {
            get { return m_ContentContainer; }
        }
    }
}
