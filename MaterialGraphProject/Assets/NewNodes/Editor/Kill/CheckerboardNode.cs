/*
using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "Checkerboard")]
    public class CheckerboardNode : CodeFunctionNode
    {
        public CheckerboardNode()
        {
            name = "Checkerboard";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Checkerboard", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Checkerboard(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None)] Vector1 horizontalTileScale,
            [Slot(2, Binding.None)] Vector1 verticalTileScale,
            [Slot(3, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    result = abs(floor(fmod(floor(uv.x * horizontalTileScale) + floor(uv.y * verticalTileScale), 2.0)));
}";
        }
    }
}
*/
