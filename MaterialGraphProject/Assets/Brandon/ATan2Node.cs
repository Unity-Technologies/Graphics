namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcTan2 Node")]
    public class ATan2Node : Function2Input
    {
        public ATan2Node()
        {
            name = "ATan2Node";
        }

        protected override string GetFunctionName() { return "atan2"; }
    }
}
