using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Main Light")]
    class GetMainLightNode : CodeFunctionNode
    {
        public GetMainLightNode()
        {
            name = "Main Light";
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("GetMainLight", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string GetMainLight(
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
    SHADERGRAPH_GET_MAIN_LIGHT(Direction, Color);
    #endif
}
";
        }
    }
}
