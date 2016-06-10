using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class MaterialGraphAsset : ScriptableObject, IGraphAsset
    {
        [SerializeField]
        private MaterialGraph m_MaterialGraph;

        public IGraph graph
        {
            get { return m_MaterialGraph.currentGraph; }
        }

        public bool shouldRepaint
        {
            get { return graph.nodes.OfType<IRequiresTime>().Any(); }
        }

        public Material GetMaterial()
        {
            return null;
        }

        public void PostCreate()
        {
            m_MaterialGraph.PostCreate();
        }
    }
}
