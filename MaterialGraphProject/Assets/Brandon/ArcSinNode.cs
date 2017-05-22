namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcSin Node")]
    public class ASinNode : Function1Input
    {
        public ASinNode()
        {
            name = "ASinNode";
        }

        protected override string GetFunctionName() { return "asin"; }
    }
}
