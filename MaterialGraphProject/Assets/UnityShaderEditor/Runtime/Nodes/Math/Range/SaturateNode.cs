namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/Saturate")]
    class SaturateNode : Function1Input
    {
        public SaturateNode()
        {
            name = "Saturate";
        }

        protected override string GetFunctionName() { return "saturate"; }
    }
}