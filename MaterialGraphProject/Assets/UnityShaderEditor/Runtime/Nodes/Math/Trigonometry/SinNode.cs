namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Sin")]
    class SinNode : Function1Input
    {
        public SinNode()
        {
            name = "Sin";
        }

        protected override string GetFunctionName() {return "sin"; }
    }
}
