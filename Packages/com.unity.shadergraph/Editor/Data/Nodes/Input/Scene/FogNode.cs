using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Fog")]
    class FogNode : CodeFunctionNode, IMayRequireTransform
    {
        public FogNode()
        {
            name = "Fog";
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Fog", BindingFlags.Static | BindingFlags.NonPublic);
        }

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

        public NeededTransform[] RequiresTransform(ShaderStageCapability stageCapability = ShaderStageCapability.All) => new[] { NeededTransform.ObjectToWorld };
    }
}
