using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public class NodeEditorHeaderView : VisualElement
    {
        VisualElement m_Title;
        VisualElement m_Type;

        public NodeEditorHeaderView()
        {
            m_Title = new VisualElement { name = "title", text = "" };
            Add(m_Title);
            Add(new VisualElement { name = "preType", text = "(" });
            m_Type = new VisualElement { name = "type", text = "" };
            Add(m_Type);
            Add(new VisualElement { name = "postType", text = ")" });
        }

        public string title
        {
            get { return m_Title.text; }
            set { m_Title.text = value; }
        }

        public string type
        {
            get { return m_Type.text; }
            set { m_Type.text = value; }
        }
    }
}
