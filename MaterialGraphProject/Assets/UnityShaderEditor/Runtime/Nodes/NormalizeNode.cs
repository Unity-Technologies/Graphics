namespace UnityEngine.MaterialGraph
{
    [Title("Math/Normalize Node")]
    class NormalizeNode : Function1Input
    {
        public NormalizeNode()
        {
            name = "NormalizeNode";
        }

        protected override string GetFunctionName() { return "normalize"; }
    }
}
