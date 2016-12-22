using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class MaterialRemapAsset : ScriptableObject, IMaterialGraphAsset
    {
        [SerializeField]
        private MasterRemapGraph m_MasterRemapGraph = new MasterRemapGraph();

        public IGraph graph
        {
            get { return m_MasterRemapGraph; }
        }

        public MasterRemapGraph masterRemapGraph
        {
            get { return m_MasterRemapGraph; }
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

        [SerializeField]
        private GraphDrawingData m_DrawingData = new GraphDrawingData();

        public GraphDrawingData drawingData
        {
            get { return m_DrawingData; }
        }
    }
}
