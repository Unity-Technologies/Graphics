namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "All")]
    class AllNode : CodeFunctionNode
    {
        public AllNode()
        {
            name = "All";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        static string Unity_All(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = all(In);
}
";
        }
    }
}
