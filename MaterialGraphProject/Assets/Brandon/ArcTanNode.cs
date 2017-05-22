namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcTan Node")]
    public class ATanNode : Function1Input
    {
        public ATanNode()
        {
            name = "ATanNode";
        }

        protected override string GetFunctionName() { return "atan"; }
    }
}
