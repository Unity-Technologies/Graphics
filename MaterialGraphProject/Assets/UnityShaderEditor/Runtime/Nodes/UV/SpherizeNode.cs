using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/Spherize")]
    public class SpherizeNode : CodeFunctionNode
    {
        public SpherizeNode()
        {
            name = "Spherize";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Spherize", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Spherize(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None)] Vector2 position,
            [Slot(2, Binding.None)] Vector2 radiusAndStrength,
            [Slot(3, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;
            return
                @"
{
     {precision}2 fromUVToPoint = position - uv;
     {precision} dist = length(fromUVToPoint);
     {precision} mag = ((1.0 - (dist / radiusAndStrength.x)) * radiusAndStrength.y) * step(dist, radiusAndStrength.x);
     result = uv + (mag * fromUVToPoint);
}";
        }
    }
}
