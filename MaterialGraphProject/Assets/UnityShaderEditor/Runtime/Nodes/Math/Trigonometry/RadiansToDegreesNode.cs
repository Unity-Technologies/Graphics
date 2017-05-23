namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Radians To Degrees")]
    public class RadiansToDegreesNode : Function1Input
    {
        public RadiansToDegreesNode()
        {
            name = "RadiansToDegrees";
        }

        protected override string GetFunctionName() { return "degrees"; }
    }
}
