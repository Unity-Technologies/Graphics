namespace UnityEngine.MaterialGraph
{
    [Title("Math/SmoothStep Node")]
    class SmoothStepNode : Function3Input
    {
        public SmoothStepNode()
        {
            name = "SmoothStepNode";
        }

        protected override string GetFunctionName() {return "smoothstep"; }
    }
}
