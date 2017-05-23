namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcSin")]
    public class ASinNode : Function1Input
    {
        public ASinNode()
        {
            name = "ArcSin";
        }

        protected override string GetFunctionName() { return "asin"; }
    }
}
