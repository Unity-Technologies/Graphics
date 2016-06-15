using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class SubGraphOutputNode : AbstractSubGraphIONode
    {
        public SubGraphOutputNode(IGraph theOwner) : base(theOwner)
        {
            name = "SubGraphOutputs";
        }
    }
}
