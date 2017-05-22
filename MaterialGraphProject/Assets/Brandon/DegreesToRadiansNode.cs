namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Degrees To Radians Node")]
    public class DegreesToRadiansNode : Function1Input
    {
        public DegreesToRadiansNode()
        {
            name = "DegreesToRadians";
        }

        protected override string GetFunctionName() { return "radians"; }
    }
}
