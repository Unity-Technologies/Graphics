using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class PropertyRow : VisualElement
    {
        VisualElement m_ContentContainer;
        VisualElement m_LabelContainer;

        public override VisualElement contentContainer
        {
            get { return m_ContentContainer; }
        }

        public VisualElement label
        {
            get { return (m_LabelContainer.childCount > 0) ? m_LabelContainer[0] : null; }
            set
            {
                if (m_LabelContainer.childCount > 0)
                {
                    m_LabelContainer.Clear();
                }
                m_LabelContainer.Add(value);
            }
        }

        public PropertyRow(VisualElement label = null)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            VisualElement container = new VisualElement { name = "container" };
            m_ContentContainer = new VisualElement { name = "content" };
            m_LabelContainer = new VisualElement { name = "label" };
            m_LabelContainer.Add(label);

            container.Add(m_LabelContainer);
            container.Add(m_ContentContainer);

            hierarchy.Add(container);
        }
    }
}
