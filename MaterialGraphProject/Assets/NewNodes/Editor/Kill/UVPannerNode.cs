using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "UV Panner")]
    public class PannerNode : CodeFunctionNode
    {
        public PannerNode()
        {
            name = "UVPanner";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_UVPanner", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_UVPanner(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None)] Vector1 horizontalOffset,
            [Slot(2, Binding.None)] Vector1 verticalOffset,
            [Slot(3, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;
            return
                @"
{
     result = float2(uv.x + horizontalOffset, uv.y + verticalOffset);
}";
        }
    }
}*/
