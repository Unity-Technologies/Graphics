namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Normalize")]
    class NormalizeNode : Function1Input
    {
        public NormalizeNode()
        {
            name = "Normalize";
        }

        protected override string GetFunctionName() { return "normalize"; }
    }
}
