using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class MaterialSubGraphAsset : AbstractMaterialGraphAsset
    {
        [SerializeField]
        private SubGraph m_MaterialSubGraph = new SubGraph();

        public override IGraph graph
        {
            get { return m_MaterialSubGraph; }
        }

        public SubGraph subGraph
        {
            get { return m_MaterialSubGraph; }
        }

        public void PostCreate()
        {
            m_MaterialSubGraph.PostCreate();
        }
    }
}
