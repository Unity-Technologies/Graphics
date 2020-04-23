using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class PropertySheet : VisualElement
    {
        VisualElement m_ContentContainer;
        VisualElement m_HeaderContainer;
        VisualElement m_ErrorContainer;
        Label m_Header;
        public override VisualElement contentContainer
        {
            get { return m_ContentContainer; }
        }

        public VisualElement errorContainer => m_ErrorContainer;

        public VisualElement headerContainer
        {
            get { return m_HeaderContainer.Children().FirstOrDefault(); }
            set
            {
                var first = m_HeaderContainer.Children().FirstOrDefault();
                if (first != null)
                    first.RemoveFromHierarchy();

                m_HeaderContainer.Add(value);
            }
        }

        public PropertySheet(Label header = null)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertySheet"));
            m_ContentContainer = new VisualElement { name = "content" };
            m_HeaderContainer = new VisualElement { name = "header" };
            m_ErrorContainer = new VisualElement {name = "error"};
            if (header != null)
                m_HeaderContainer.Add(header);

            m_ContentContainer.Add(m_HeaderContainer);
            m_ContentContainer.Add(m_ErrorContainer);
            hierarchy.Add(m_ContentContainer);
        }
    }
}
