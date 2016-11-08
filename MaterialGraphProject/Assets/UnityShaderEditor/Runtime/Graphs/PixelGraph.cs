using System;
using System.Linq;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class PixelGraph : AbstractMaterialGraph
    {
        public AbstractMasterNode masterNode
        {
            get { return GetNodes<AbstractMasterNode>().FirstOrDefault(); }
        }

        public string name
        {
            get { return "Graph_ " + masterNode.GetVariableNameForNode(); }
        }
    }
}
