using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Tiling And Offset")]
    class TilingAndOffsetNode : CodeFunctionNode
    {
        public TilingAndOffsetNode()
        {
            name = "Tiling And Offset";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_TilingAndOffset", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void Unity_TilingAndOffset(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(1, Binding.None, 1f, 1f, 1f, 1f)] Float2 Tiling,
            [Slot(2, Binding.None, 0f, 0f, 0f, 0f)] Float2 Offset,
            [Slot(3, Binding.None)] out Float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
    }
}
