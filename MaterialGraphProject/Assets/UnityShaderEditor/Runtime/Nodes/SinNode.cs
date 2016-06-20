using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Sin Node")]
    class SinNode : Function1Input
    {
        public SinNode()
        {
            name = "SinNode";
        }

        protected override string GetFunctionName() {return "sin"; }
    }
}
