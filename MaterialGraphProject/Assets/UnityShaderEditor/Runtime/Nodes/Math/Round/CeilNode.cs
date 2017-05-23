namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Ceil")]
    public class CeilNode : Function1Input
    {
        public CeilNode()
        {
            name = "Ceil";
        }

        protected override string GetFunctionName() { return "ceil"; }
    }
}
