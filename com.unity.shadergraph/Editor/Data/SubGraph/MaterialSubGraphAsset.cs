using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public class MaterialSubGraphAsset : ScriptableObject
    {
        [SerializeField] private GraphData m_MaterialSubGraphData = new GraphData();

        public GraphData subGraph
        {
            get { return m_MaterialSubGraphData; }
            set
            {
                if (!value.isSubGraph)
                    throw new ArgumentException("value must be a sub-graph.");
                m_MaterialSubGraphData = value;
            }
        }
    }
}
