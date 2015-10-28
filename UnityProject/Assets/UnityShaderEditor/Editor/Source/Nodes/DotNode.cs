namespace UnityEditor.MaterialGraph
{
    [Title("Math/Dot Node")]
    class DotNode : Function2Input
    {
        public override void OnCreate()
        {
            name = "DotNode";
            base.OnCreate();;
        }

        protected override string GetFunctionName() { return "dot"; }
    }
}
