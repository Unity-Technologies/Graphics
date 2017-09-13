using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class SurfaceMasterNodeEditorPresenter : AbstractNodeEditorPresenter
    {
        [SerializeField]
        AbstractSurfaceMasterNode m_Node;

        public override INode node
        {
            get { return m_Node; }
            set { m_Node = (AbstractSurfaceMasterNode)value; }
        }

        public AbstractSurfaceMasterNode materialNode
        {
            get { return m_Node; }
        }
    }
}
