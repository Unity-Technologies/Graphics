using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Sin Node")]
    class SinNode : Function1Input
    {
        public SinNode(IGraph owner) : base(owner)
        {
            name = "SinNode";
        }

        protected override string GetFunctionName() {return "sin"; }
    }
}
