namespace UnityEngine.MaterialGraph
{
	[Title("Math/Trigonometry/SinCos")]
    public class SinCosNode : Function1Input
    {
        public SinCosNode()
        {
            name = "SinCos";
        }

        protected override string GetFunctionName() { return "cos"; }
    }
}
