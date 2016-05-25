using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class MaterialGraphAsset : ScriptableObject
    {
        [SerializeField]
        private MaterialGraph m_MaterialGraph;

        public MaterialGraph graph
        {
            get { return m_MaterialGraph; }
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
