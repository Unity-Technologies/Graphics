using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Power Node")]
    public class PowerNode : Function2Input
    {
        public PowerNode()
        {
            name = "PowerNode";
        }

        protected override string GetFunctionName() { return "pow"; }
    }
}
