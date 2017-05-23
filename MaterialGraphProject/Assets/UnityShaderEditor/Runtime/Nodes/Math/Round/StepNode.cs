namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Step")]
    public class StepNode : Function2Input
    {
        public StepNode()
        {
            name = "Step";
        }

        protected override string GetFunctionName() { return "step"; }
    }
}


