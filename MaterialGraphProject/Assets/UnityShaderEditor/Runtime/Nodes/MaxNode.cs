using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Maximum Node")]
    public class MaximumNode : Function2Input
    {
        public MaximumNode(IGraph owner) : base(owner)
        {
            name = "MaximumNode";
        }

        protected override string GetFunctionName() { return "max"; }
    }
}
