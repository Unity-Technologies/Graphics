namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Sign")]
    public class SignNode : Function1Input
    {
        public SignNode()
        {
            name = "Sign";
        }

        protected override string GetFunctionName() { return "sign"; }
    }
}
