using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class MaterialRemapAsset : AbstractMaterialGraphAsset
    {
        [SerializeField]
        private MasterRemapGraph m_MasterRemapGraph = new MasterRemapGraph();

        public override IGraph graph
        {
            get { return m_MasterRemapGraph; }
        }

        public MasterRemapGraph masterRemapGraph
        {
            get { return m_MasterRemapGraph; }
        }
    }
}
