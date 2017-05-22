namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/Cross Node")]
    public class CrossNode : Function2Input
    {
        public CrossNode()
        {
            name = "CrossNode";
        }

        protected override string GetFunctionName() { return "cross"; }
    }
}
