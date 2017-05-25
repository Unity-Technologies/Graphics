namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Reciprocal Square Root")]
    public class ReciprocalSqrtNode : Function1Input
    {
        public ReciprocalSqrtNode()
        {
            name = "ReciprocalSquareRoot";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override string GetFunctionName() { return "rsqrt"; }
    }
}


