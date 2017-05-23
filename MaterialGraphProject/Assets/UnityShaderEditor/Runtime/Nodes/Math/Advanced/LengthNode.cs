namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Length")]
    public class LengthNode : Function1Input
    {
        public LengthNode()
        {
            name = "Length";
        }

        protected override string GetFunctionName() { return "length"; }
    }
}
