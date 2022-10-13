using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Input", "High Definition Render Pipeline", "Mask Volume")]
    class MaskVolumeNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public MaskVolumeNode()
        {
            name = "Mask Volume";
        }
        
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_MaskVolume", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_MaskVolume(
           [Slot(2, Binding.WorldSpacePosition)] Vector3 Position,
           [Slot(0, Binding.WorldSpaceNormal)] Vector3 Normal,
           [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
#if defined(__BUILTINGIUTILITIES_HLSL__) && defined(SHADERPASS) && (SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD)
    Out = SampleMaskVolume(Position, Normal);
#else
    Out = 0;
#endif
}
";
        }
    }
}
