using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    // Similar in function to the old  EditorGUILayout.HelpBox
    class HelpBoxDeprecated : VisualElement 
    {
        VisualElement m_ContentContainer;
        VisualElement m_LabelContainer;

        public override VisualElement contentContainer
        {
            get { return m_ContentContainer; }
        }

        public HelpBoxDeprecated(string tooltip, UnityEngine.Object target, Action upgrade) 
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/HelpBoxDeprecatedRow"));
            VisualElement container = new VisualElement { name = "container" };
            m_ContentContainer = new VisualElement { name = "content" };
            m_LabelContainer = new VisualElement { name = "label" };

            container.AddToClassList("help-box-deprecated-row-style-warning");
            container.Add(m_LabelContainer);
            container.Add(m_ContentContainer);
            hierarchy.Add(container);
            var button = new Button(upgrade.Invoke) { text = "Update" };
            var label = new Label("DEPRECATED: Hover for info") { tooltip = tooltip };
            Add(label);
            Add(button);
        }
    }
}
