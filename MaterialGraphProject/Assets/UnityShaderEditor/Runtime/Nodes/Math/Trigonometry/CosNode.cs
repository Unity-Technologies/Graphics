namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Cos")]
    public class CosNode : Function1Input
    {
        public CosNode()
        {
            name = "Cos";
        }

        protected override string GetFunctionName() { return "cos"; }
    }
}
