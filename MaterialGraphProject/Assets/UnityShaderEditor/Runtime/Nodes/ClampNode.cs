namespace UnityEngine.MaterialGraph
{
    [Title("Math/Clamp Node")]
    public class ClampNode : Function3Input
    {
        public ClampNode()
        {
            name = "ClampNode";
        }

        protected override string GetFunctionName() {return "clamp"; }
    }
}
