using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class MaterialSubGraphAsset : ScriptableObject
    {
        [SerializeField]
        private GraphData m_MaterialSubGraph = new GraphData()
        {
            isSubGraph
                = true
        };

        public GraphData subGraph
        {
            get { return m_MaterialSubGraph; }
            set { m_MaterialSubGraph = value; }
        }
    }
}
