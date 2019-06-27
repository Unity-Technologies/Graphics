namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Not")]
    class NotNode : CodeFunctionNode
    {
        public NotNode()
        {
            name = "Not";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        static string Unity_Not(
            [Slot(0, Binding.None)] Boolean In,
            [Slot(1, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = !In;
}
";
        }
    }
}
