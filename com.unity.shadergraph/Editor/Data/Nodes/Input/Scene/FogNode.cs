using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Fog")]
    class FogNode : CodeFunctionNode
    {
        public FogNode()
        {
            name = "Fog";
        }


        public override bool hasPreview { get { return false; } }

        static string Unity_Fog(
            [Slot(2, Binding.ObjectSpacePosition)] Vector3 Position,
            [Slot(0, Binding.None)] out Vector4 Color,
            [Slot(1, Binding.None)] out Vector1 Density)
        {
            Color = Vector4.zero;
            return
                @"
{
    SHADERGRAPH_FOG(Position, Color, Density);
}
";
        }
    }
}
