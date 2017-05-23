namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/Maximum")]
    public class MaximumNode : Function2Input
    {
        public MaximumNode()
        {
            name = "Maximum";
        }

        protected override string GetFunctionName() { return "max"; }
    }
}
