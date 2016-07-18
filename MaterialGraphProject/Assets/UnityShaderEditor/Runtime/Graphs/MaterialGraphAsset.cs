using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class MaterialGraphAsset : ScriptableObject, IMaterialGraphAsset
    {
        [SerializeField]
        private MaterialGraph m_MaterialGraph = new MaterialGraph();

        public IGraph graph
        {
            get { return m_MaterialGraph.currentGraph; }
        }

        public bool shouldRepaint 
        {
            get { return graph.GetNodes<AbstractMaterialNode>().OfType<IRequiresTime>().Any(); }
        }

        public ScriptableObject GetScriptableObject()
        {
            return this;
        }

        public void OnEnable()
        {
            graph.OnEnable();
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
