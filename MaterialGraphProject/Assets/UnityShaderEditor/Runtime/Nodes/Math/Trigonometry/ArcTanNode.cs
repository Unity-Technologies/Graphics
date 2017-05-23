namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcTan")]
    public class ATanNode : Function1Input
    {
        public ATanNode()
        {
            name = "ArcTan";
        }

        protected override string GetFunctionName() { return "atan"; }
    }
}
