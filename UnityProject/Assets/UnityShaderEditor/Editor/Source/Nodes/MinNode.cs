namespace UnityEditor.MaterialGraph
{
    [Title("Math/Minimum Node")]
    class MinimumNode : Function2Input
    {
        public override void OnCreate()
        {
            name = "MinimumNode";
            base.OnCreate();;
        }

        protected override string GetFunctionName() { return "min"; }
    }
}
