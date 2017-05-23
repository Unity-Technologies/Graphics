namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/DDY")]
    public class DDYNode : Function1Input
    {
        public DDYNode()
        {
            name = "DDY";
        }

        protected override string GetFunctionName() { return "ddy"; }
    }
}


