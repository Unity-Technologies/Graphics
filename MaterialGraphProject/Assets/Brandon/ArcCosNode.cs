namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcCos Node")]
    public class ACosNode : Function1Input
    {
        public ACosNode()
        {
            name = "ACosNode";
        }

        protected override string GetFunctionName() { return "acos"; }
    }
}
