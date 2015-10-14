namespace UnityEditor.MaterialGraph
{
    [Title("Math/Maximum Node")]
    class MaximumNode : Function2Input
    {
        public override void Init()
        {
            name = "MaximumNode";
            base.Init();
        }

        protected override string GetFunctionName() { return "max"; }
    }
}
