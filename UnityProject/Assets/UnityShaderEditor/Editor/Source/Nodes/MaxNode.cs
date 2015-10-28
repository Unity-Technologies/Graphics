namespace UnityEditor.MaterialGraph
{
    [Title("Math/Maximum Node")]
    class MaximumNode : Function2Input
    {
        public override void OnCreate()
        {
            name = "MaximumNode";
            base.OnCreate();;
        }

        protected override string GetFunctionName() { return "max"; }
    }
}
