using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public class StandardNodeEditorView : AbstractNodeEditorView
    {
        NodeEditorHeaderView m_HeaderView;
        AbstractMaterialNode m_Node;

        public override INode node
        {
            get { return m_Node; }
            set
            {
                if (value == m_Node)
                    return;
                if (m_Node != null)
                    m_Node.onModified -= OnModified;
                m_Node = value as AbstractMaterialNode;
                OnModified(m_Node, ModificationScope.Node);
                if (m_Node != null)
                    m_Node.onModified += OnModified;
            }
        }

        public override void Dispose()
        {
            if (m_Node != null)
                m_Node.onModified -= OnModified;
        }

        public StandardNodeEditorView()
        {
            AddToClassList("nodeEditor");

            m_HeaderView = new NodeEditorHeaderView() { type = "node" };
            Add(m_HeaderView);
        }

        void OnModified(INode changedNode, ModificationScope scope)
        {
            if (node == null)
                return;

            m_HeaderView.title = node.name;
        }
    }
}
