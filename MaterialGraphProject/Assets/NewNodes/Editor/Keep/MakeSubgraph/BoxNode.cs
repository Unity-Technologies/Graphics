using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "MakeSubgraph", "Square")]
    public class BoxNode : CodeFunctionNode
    {
        public BoxNode()
        {
            name = "Square";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Boxnode", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Boxnode(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.2f, 0.8f, 0, 0)] Vector2 XMinAndMax,
            [Slot(2, Binding.None, 0.2f, 0.8f, 0, 0)] Vector2 YMinAndMax,
            [Slot(3, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    {precision} x = step( XMinAndMax.x, UV.x ) - step( XMinAndMax.y, UV.x );
    {precision} y = step( YMinAndMax.x, UV.y ) - step( YMinAndMax.y, UV.y );
    Out = x * y;
}";
        }
    }
}
