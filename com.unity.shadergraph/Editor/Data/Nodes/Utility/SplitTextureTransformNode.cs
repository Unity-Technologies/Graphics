using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Split Texture Transform")]
    class SplitTextureTransformNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }
        public SplitTextureTransformNode()
        {
            name = "Split Texture Transform";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SplitTextureTransform", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SplitTextureTransform(
            [Slot(0, Binding.None)] Texture2D In,
            [Slot(1, Binding.None)] out Vector2 Tiling,
            [Slot(2, Binding.None)] out Vector2 Offset,
            [Slot(3, Binding.None)] out Texture2D TextureOnly)
        {
            TextureOnly = default;
            Tiling = default;
            Offset = default;
            return
@"
{
    TextureOnly = In;
    TextureOnly.scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f);
    Tiling = In.scaleTranslate.xy;
    Offset = In.scaleTranslate.zw;
}
";
        }
    }
}
