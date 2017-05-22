namespace UnityEngine.MaterialGraph
{
    [Title("Math/SinCos Node")]
    public class SinCosNode : Function1Input
    {
        public SinCosNode()
        {
            name = "SinCosNode";
        }

        protected override string GetFunctionName() { return "cos"; }
    }
}
