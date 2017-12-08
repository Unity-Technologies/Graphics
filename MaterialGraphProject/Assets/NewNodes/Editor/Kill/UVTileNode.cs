using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "UV Tile")]
    public class UVTileNode : CodeFunctionNode
    {
        public UVTileNode()
        {
            name = "UVTile";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_UVTile", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_UVTile(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None)] Vector2 tileFactor,
            [Slot(2, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;
            return
                @"
{
    result = uv * tileFactor;
}";
        }
    }
}*/
