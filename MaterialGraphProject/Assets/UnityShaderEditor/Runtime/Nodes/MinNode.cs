namespace UnityEngine.MaterialGraph
{
    [Title("Math/Minimum Node")]
    public class MinimumNode : Function2Input
    {
        public MinimumNode()
        {
            name = "MinimumNode";
        }

        protected override string GetFunctionName() { return "min"; }
    }
}
