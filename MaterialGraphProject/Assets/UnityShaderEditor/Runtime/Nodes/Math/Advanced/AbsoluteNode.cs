namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Absolute")]
    public class AbsoluteNode : Function1Input
    {
        public AbsoluteNode()
        {
            name = "Absolute";
        }

        protected override string GetFunctionName() {return "abs"; }
    }
}
