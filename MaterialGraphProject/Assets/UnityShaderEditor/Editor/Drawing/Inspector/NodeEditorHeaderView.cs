using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public class NodeEditorHeaderView : VisualElement
    {
        Label m_Title;
        Label m_Type;

        public NodeEditorHeaderView()
        {
            m_Title = new Label("") { name = "title" };
            Add(m_Title);
            Add(new Label("(") { name = "preType" });
            m_Type = new Label("") { name = "type" };
            Add(m_Type);
            Add(new Label(")") { name = "postType" });
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
