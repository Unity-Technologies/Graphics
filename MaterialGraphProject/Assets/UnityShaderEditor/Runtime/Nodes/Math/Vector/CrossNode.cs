namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/Cross Product")]
    public class CrossNode : Function2Input
    {
        public CrossNode()
        {
            name = "CrossProduct";
        }

        protected override string GetFunctionName() { return "cross"; }
    }
}
