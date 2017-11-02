using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural/Box")]
    public class BoxNode : CodeFunctionNode
    {
        public BoxNode()
        {
            name = "Box";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Boxnode", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Boxnode(
            [Slot(0, Binding.None)] Vector2 xy,
            [Slot(1, Binding.None)] Vector2 xMinAndMax,
            [Slot(2, Binding.None)] Vector2 yMinAndMax,
            [Slot(3, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    {precision} x = step( xMinAndMax.x, xy.x ) - step( xMinAndMax.y, xy.x );
    {precision} y = step( yMinAndMax.x, xy.y ) - step( yMinAndMax.y, xy.y );
    result = x * y;
}";
        }
    }
}
