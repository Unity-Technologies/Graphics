namespace UnityEditor.MaterialGraph
{
    [Title("Math/Sin Node")]
    class SinNode : Function1Input
    {
        public override void OnCreate()
        {
            name = "SinNode";
            base.OnCreate();;
        }

        protected override string GetFunctionName() {return "sin"; }
    }
}
