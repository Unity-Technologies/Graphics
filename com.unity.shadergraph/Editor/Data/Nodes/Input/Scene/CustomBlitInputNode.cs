using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Custom Blit")]
    sealed class CustomBlitInputNode : CodeFunctionNode, IMayRequireCameraOpaqueTexture
    {
        public CustomBlitInputNode()
        {
            name = "Custom Blit";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_CustomBlitInput", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_CustomBlitInput(
            [Slot(0, Binding.ScreenPosition)] Vector4 UV,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
        {
            Out = Vector4.one;
            return
                @"{
                    Out = SHADERGRAPH_LOAD_CUSTOM_BLIT_INPUT(UV.xy);
                }
                ";
        }

        public bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}

