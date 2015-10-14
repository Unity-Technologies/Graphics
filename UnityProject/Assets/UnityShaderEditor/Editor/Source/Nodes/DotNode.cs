namespace UnityEditor.MaterialGraph
{
    [Title("Math/Dot Node")]
    class DotNode : Function2Input
    {
        public override void Init()
        {
            name = "DotNode";
            base.Init();
        }

        protected override string GetFunctionName() { return "dot"; }
    }
}
