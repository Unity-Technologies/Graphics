namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Truncate")]
    public class TruncateNode : Function1Input
    {
        public TruncateNode()
        {
            name = "Truncate";
        }

        protected override string GetFunctionName() { return "truncate"; }
    }
}


