using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Main Light Direction")]
    class MainLightDirectionNode : CodeFunctionNode
    {
        public MainLightDirectionNode()
        {
            name = "Main Light Direction";
            synonyms = new string[] { "sun" };
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("MainLightDirection", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string MainLightDirection([Slot(0, Binding.None)] out Vector3 Direction)
        {
            Direction = Vector3.one;
            return
@"
{
    #if SHADERGRAPH_PREVIEW
    Direction = half3(-0.5, -0.5, 0);
    #else
    Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
    #endif
}
";
        }
    }
}
