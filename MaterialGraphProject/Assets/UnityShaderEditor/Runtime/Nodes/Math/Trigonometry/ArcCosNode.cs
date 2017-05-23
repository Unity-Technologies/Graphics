namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcCos")]
    public class ACosNode : Function1Input
    {
        public ACosNode()
        {
            name = "ArcCos";
        }

        protected override string GetFunctionName() { return "acos"; }
    }
}
