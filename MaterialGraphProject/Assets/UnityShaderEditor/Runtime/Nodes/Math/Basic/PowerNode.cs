namespace UnityEngine.MaterialGraph
{
    [Title("Math/Basic/Power")]
    public class PowerNode : Function2Input
    {
        public PowerNode()
        {
            name = "Power";
        }

        protected override string GetFunctionName() { return "pow"; }
    }
}
