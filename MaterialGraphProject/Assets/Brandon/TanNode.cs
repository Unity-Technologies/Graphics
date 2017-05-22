namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Tan Node")]
    public class TanNode : Function1Input
    {
        public TanNode()
        {
            name = "TanNode";
        }

        protected override string GetFunctionName() { return "tan"; }
    }
}
