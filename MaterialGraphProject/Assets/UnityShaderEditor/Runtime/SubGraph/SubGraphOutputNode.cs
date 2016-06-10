using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class SubGraphOutputNode : SubGraphIOBaseNode
    {
        public SubGraphOutputNode(IGraph theOwner) : base(theOwner)
        {
            name = "SubGraphOutputs";
        }
    }
}
