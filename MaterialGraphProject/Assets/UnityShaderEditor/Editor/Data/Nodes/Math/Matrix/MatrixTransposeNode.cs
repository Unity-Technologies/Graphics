using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Matrix", "TransposeMatrix")]
    public class MatrixTransposeNode : CodeFunctionNode
    {
        public MatrixTransposeNode()
        {
            name = "TransposeMatrix";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("unity_MatrixTranspose_", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string unity_MatrixTranspose_(
            [Slot(0, Binding.None)] Matrix4x4 inMatrix,
            [Slot(1, Binding.None)] out Matrix4x4 outMatrix)
        {
            outMatrix = Matrix4x4.identity;
            return
                @"
{
    outMatrix = transpose(inMatrix);
}
";
        }
    }
}
