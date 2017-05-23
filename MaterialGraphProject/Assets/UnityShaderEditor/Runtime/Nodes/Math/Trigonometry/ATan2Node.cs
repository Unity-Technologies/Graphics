namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcTan2")]
    public class ATan2Node : Function2Input
    {
        public ATan2Node()
        {
            name = "ArcTan2";
        }

        protected override string GetFunctionName() { return "atan2"; }
    }
}
