namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/DDX")]
    public class DDXNode : Function1Input
    {
        public DDXNode()
        {
            name = "DDX";
        }

        protected override string GetFunctionName() { return "ddx"; }
    }
}


