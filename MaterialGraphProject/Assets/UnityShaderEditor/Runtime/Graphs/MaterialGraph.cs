using System;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class MaterialGraph
    {
        [SerializeField]
        private PixelGraph m_PixelGraph;

        [SerializeField]
        private string m_Name;

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public MaterialGraph()
        {
            m_PixelGraph = new PixelGraph();
        }
        
        public AbstractMaterialGraph currentGraph
        {
            get { return m_PixelGraph; }
        }

        /*
        public Material GetMaterial()
        {
            if (m_PixelGraph == null)
                return null;

            return m_PixelGraph.GetMaterial();
        }*/

        public void PostCreate()
        {
            //m_PixelGraph.AddNode(new PixelShaderNode());
        }
    }
}
