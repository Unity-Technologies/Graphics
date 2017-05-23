namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Tan")]
    public class TanNode : Function1Input
    {
        public TanNode()
        {
            name = "Tan";
        }

        protected override string GetFunctionName() { return "tan"; }
    }
}
