namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Log")]
    public class LogNode : Function1Input
    {
        public LogNode()
        {
            name = "Log";
        }

        protected override string GetFunctionName() { return "log"; }
    }
}

