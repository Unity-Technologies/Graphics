namespace UnityEditor.MaterialGraph
{
    [Title("Math/Sin Node")]
    class SinNode : Function1Input
    {
        public override void Init()
        {
            name = "SinNode";
            base.Init();
        }

        protected override string GetFunctionName() {return "sin"; }
    }
}
