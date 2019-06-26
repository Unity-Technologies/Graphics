namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Matrix", "Matrix Transpose")]
    class MatrixTransposeNode : CodeFunctionNode
    {
        public MatrixTransposeNode()
        {
            name = "Matrix Transpose";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        static string Unity_MatrixTranspose(
            [Slot(0, Binding.None)] DynamicDimensionMatrix In,
            [Slot(1, Binding.None)] out DynamicDimensionMatrix Out)
        {
            return
                @"
{
    Out = transpose(In);
}
";
        }
    }
}
