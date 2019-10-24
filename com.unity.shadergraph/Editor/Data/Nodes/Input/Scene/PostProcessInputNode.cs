using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Post Process")]
    sealed class PostProcessInputNode : CodeFunctionNode, IMayRequireCameraOpaqueTexture
    {
        const string kScreenPositionSlotName = "UV";
        const string kOutputSlotName = "Out";

        public const int ScreenPositionSlotId = 0;
        public const int OutputSlotId = 1;

        public PostProcessInputNode()
        {
            name = "Post Process";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_PostProcessInput", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_PostProcessInput(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
        {
            Out = Vector4.one;
            return
                @"
{
    Out = SHADERGRAPH_LOAD_POST_PROCESS_INPUT(UV.xy);
}
";
        }

        public bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}

