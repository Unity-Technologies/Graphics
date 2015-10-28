namespace UnityEditor.MaterialGraph
{
    [Title("Math/Length Node")]
    class LengthNode : Function1Input
    {
        public override void OnCreate()
        {
            name = "LengthNode";
            base.OnCreate();;
        }

        protected override string GetFunctionName() { return "length"; }
    }
}
