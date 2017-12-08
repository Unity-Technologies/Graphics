using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Flipbook")]
    public class FlipbookNode : CodeFunctionNode
    {
        public FlipbookNode()
        {
            name = "Flipbook";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Flipbook", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Flipbook(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1, 1, 1, 1)] Vector1 Width,
            [Slot(2, Binding.None, 1, 1, 1, 1)] Vector1 Height,
            [Slot(3, Binding.None)] Vector1 Tile,
            [Slot(4, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;

            return
                @"
{
    {precision}2 tileCount = {precision}2(1.0, 1.0) / {precision}2(Width, Height);
    {precision} tileY = floor(Tile * tileCount.x);
    {precision} tileX = Tile - Width * tileY;
    Out = (UV + {precision}2(tileX, tileY)) * tileCount;
}
";
        }
    }
}
