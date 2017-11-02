using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV/Cartesian To Polar")]
    public class CartesianToPolarNode : CodeFunctionNode
    {
        public CartesianToPolarNode()
        {
            name = "CartesianToPolar";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_CartesianToPolar", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_CartesianToPolar(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None)] out Vector3 result)
        {
            result = Vector3.zero;
            return
                @"
{
    {precision} radius = length(uv);
    {precision} angle = atan2(uv.x, uv.y);
    result = float2(radius, angle);
}
";
        }
    }
}
