namespace UnityEditor.MaterialGraph
{
    [Title("Math/Normalize Node")]
    class NormalizeNode : Function1Input
    {
        public override void Init()
        {
            name = "NormalizeNode";
            base.Init();
        }

        protected override string GetFunctionName() { return "normalize"; }
    }
}
