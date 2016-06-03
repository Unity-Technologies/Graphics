using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Power Node")]
    public class PowerNode : Function2Input
    {
        public PowerNode(IGraph owner) : base(owner)
        {
            name = "PowerNode";
        }
        
        protected override string GetFunctionName() { return "pow"; }
    }
}
