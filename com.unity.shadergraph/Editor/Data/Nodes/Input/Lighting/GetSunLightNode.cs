using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Sun Light")]
    class GetSunLightNode : CodeFunctionNode
    {
        public GetSunLightNode()
        {
            name = "GetSunLight";
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("GetSunLight", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string GetSunLight(
            [Slot(0, Binding.None)] out Vector3 Direction,
            [Slot(1, Binding.None)] out Vector3 Color)
        {
            Direction = Vector3.one;
            Color = Vector3.one;
            return
@"
{
    #if SHADERGRAPH_PREVIEW
    Direction = half3(0.5, 0.5, 0);
    Color = 1;
    #else
    SHADERGRAPH_GET_SUN_LIGHT(Direction, Color);
    #endif
}
";
        }
    }
}
