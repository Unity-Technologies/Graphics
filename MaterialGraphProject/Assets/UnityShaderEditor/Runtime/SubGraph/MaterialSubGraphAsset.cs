using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class MaterialSubGraphAsset : ScriptableObject, IMaterialGraphAsset
    {
        [SerializeField]
        private SubGraph m_MaterialSubGraph;

        public IGraph graph
        {
            get { return m_MaterialSubGraph; }
        }

        public bool shouldRepaint 
        {
            get { return graph.GetNodes<AbstractMaterialNode>().OfType<IRequiresTime>().Any(); }
        }

        public ScriptableObject GetScriptableObject()
        {
            return this;
        }

        public void PostCreate()
        {
            m_MaterialSubGraph.PostCreate();
        }
    }
}
