using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Maximum Node")]
    public class MaximumNode : Function2Input
    {
        public MaximumNode()
        {
            name = "MaximumNode";
        }

        protected override string GetFunctionName() { return "max"; }
    }
}
