namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Cos Node")]
    public class CosNode : Function1Input
    {
        public CosNode()
        {
            name = "CosNode";
        }

        protected override string GetFunctionName() { return "cos"; }
    }
}
