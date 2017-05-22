namespace UnityEngine.MaterialGraph
{
    [Title("Math/Saturate")]
    class SaturateNode : Function1Input
    {
        public SaturateNode()
        {
            name = "SaturateNode";
        }

        protected override string GetFunctionName() { return "saturate"; }
    }
}