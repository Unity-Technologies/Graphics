using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Tiling And Offset")]
    class TilingAndOffsetNode : CodeFunctionNode, IDifferentiable
    {
        public TilingAndOffsetNode()
        {
            name = "Tiling And Offset";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_TilingAndOffset", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_TilingAndOffset(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 1f, 1f)] Vector2 Tiling,
            [Slot(2, Binding.None, 0f, 0f, 0f, 0f)] Vector2 Offset,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;
            return
                @"
{
    Out = UV * Tiling + Offset;
}
";
        }

        public Derivative GetDerivative(int outputSlotId)
        {
            if (outputSlotId != 3)
                throw new System.ArgumentException("outputSlotId");

            return new Derivative()
            {
                FuncVariableInputSlotIds = new[] { 0, 1, 2 },
                Function = genMode => $"{GetSlotValue(1, genMode)} * {{0}} + {GetSlotValue(0, genMode)} * {{1}} + {{2}}",
            };
        }
    }
}
