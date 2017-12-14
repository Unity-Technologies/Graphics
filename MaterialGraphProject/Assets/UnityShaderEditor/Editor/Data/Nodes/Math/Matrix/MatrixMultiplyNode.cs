using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Matrix", "Matrix Multiply")]
    public class MatrixMultiplyNode : CodeFunctionNode
    {
        public MatrixMultiplyNode()
        {
            name = "Matrix Multiply";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_MatrixMultiply", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_MatrixMultiply(
            [Slot(0, Binding.None)] DynamicDimensionMatrix A,
            [Slot(1, Binding.None)] DynamicDimensionMatrix B,
            [Slot(2, Binding.None)] out DynamicDimensionMatrix Out)
        {
            return
                @"
{
    Out = A * B;
}
";
        }
    }
}
