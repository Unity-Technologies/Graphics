using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Matrix", "Matrix Determinant")]
    class MatrixDeterminantNode : CodeFunctionNode
    {
        public MatrixDeterminantNode()
        {
            name = "Matrix Determinant";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_MatrixDeterminant", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_MatrixDeterminant(
            [Slot(0, Binding.None)] DynamicDimensionMatrix In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = determinant(In);
}
";
        }
    }
}
