namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/Fraction")]
    public class FractionNode : Function1Input
    {
        public FractionNode()
        {
            name = "Fraction";
        }

        protected override string GetFunctionName() { return "frac"; }
    }
}


