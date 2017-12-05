using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "Line")]
    public class LineNode : CodeFunctionNode
    {
        public LineNode()
        {
            name = "Line";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Linenode", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Linenode(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None)] Vector2 startPoint,
            [Slot(2, Binding.None)] Vector2 endPoint,
            [Slot(3, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    {precision}2 aTob = endPoint - startPoint;
    {precision}2 aTop = uv - startPoint;
    {precision} t = dot(aTop, aTob) / dot(aTob, aTob);
    t = clamp(t, 0.0, 1.0);
    {precision} d = 1.0 / length(uv - (startPoint + aTob * t));
    result = clamp(d, 0.0, 1.0);
}";
        }
    }
}*/
