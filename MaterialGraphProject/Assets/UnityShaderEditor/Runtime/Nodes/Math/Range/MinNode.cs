namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/Minimum")]
    public class MinimumNode : Function2Input
    {
        public MinimumNode()
        {
            name = "Minimum";
        }

        protected override string GetFunctionName() { return "min"; }
    }
}
