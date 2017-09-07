using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class SurfaceMasterNodeEditorPresenter : AbstractNodeEditorPresenter
    {
        [SerializeField]
        AbstractSurfaceMasterNode m_Node;

        public AbstractSurfaceMasterNode node
        {
            get { return m_Node; }
        }

        public override void Initialize(INode node)
        {
            m_Node = (AbstractSurfaceMasterNode) node;
        }
    }
}
