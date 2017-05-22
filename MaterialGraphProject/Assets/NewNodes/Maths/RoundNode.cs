namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Round")]
    public class RoundNode : Function1Input
    {
        public RoundNode()
        {
            name = "Round";
        }

        protected override string GetFunctionName() { return "round"; }
    }
}
