namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Degrees To Radians")]
    public class DegreesToRadiansNode : Function1Input
    {
        public DegreesToRadiansNode()
        {
            name = "DegreesToRadians";
        }

        protected override string GetFunctionName() { return "radians"; }
    }
}
