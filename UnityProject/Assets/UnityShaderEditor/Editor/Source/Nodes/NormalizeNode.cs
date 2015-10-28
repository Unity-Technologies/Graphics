namespace UnityEditor.MaterialGraph
{
    [Title("Math/Normalize Node")]
    class NormalizeNode : Function1Input
    {
        public override void OnCreate()
        {
            name = "NormalizeNode";
            base.OnCreate();;
        }

        protected override string GetFunctionName() { return "normalize"; }
    }
}
