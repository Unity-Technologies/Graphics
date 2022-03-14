using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.CustomRenderTexture.ShaderGraph
{
    [Title("Custom Render Texture", "Self")]
    [SubTargetFilter(typeof(CustomTextureSubTarget))]
    class CustomTextureSelf : AbstractMaterialNode, IGeneratesFunction
    {
        private const string kOutputSlotSelf2DName = "Self Texture 2D";
        private const string kOutputSlotSelfCubeName = "Self Texture Cube";
        private const string kOutputSlotSelf3DName = "Self Texture 3D";

        public const int OutputSlotSelf2DId = 5;
        public const int OutputSlotSelfCubeId = 6;
        public const int OutputSlotSelf3DId = 7;

        public CustomTextureSelf()
        {
            name = "Custom Render Texture Self";
            UpdateNodeAfterDeserialization();
        }

        protected int[] validSlots => new[] { OutputSlotSelf2DId, OutputSlotSelfCubeId, OutputSlotSelf3DId };

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DMaterialSlot(OutputSlotSelf2DId, kOutputSlotSelf2DName, kOutputSlotSelf2DName, SlotType.Output, ShaderStageCapability.Fragment, false) { bareResource = true });
            AddSlot(new CubemapMaterialSlot(OutputSlotSelfCubeId, kOutputSlotSelfCubeName, kOutputSlotSelfCubeName, SlotType.Output, ShaderStageCapability.Fragment, false) { bareResource = true });
            AddSlot(new Texture3DMaterialSlot(OutputSlotSelf3DId, kOutputSlotSelf3DName, kOutputSlotSelf3DName, SlotType.Output, ShaderStageCapability.Fragment, false) { bareResource = true });
            RemoveSlotsNameNotMatching(validSlots);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlotSelf2DId:
                    return "UnityBuildTexture2DStructNoScale(_SelfTexture2D)";
                case OutputSlotSelfCubeId:
                    return "UnityBuildTextureCubeStruct(_SelfTextureCube)";
                default:
                    return "UnityBuildTexture3DStruct(_SelfTexture3D)";
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            // For preview only we declare CRT defines
            if (generationMode == GenerationMode.Preview)
            {
                registry.builder.AppendLine("#if !defined(UNITY_CRT_PREVIEW_TEXTURE) && !defined(UNITY_CUSTOM_TEXTURE_INCLUDED)");
                registry.builder.AppendLine("#define UNITY_CRT_PREVIEW_TEXTURE");
                registry.builder.AppendLine("TEXTURE2D(_SelfTexture2D);");
                registry.builder.AppendLine("SAMPLER(sampler_SelfTexture2D);");
                registry.builder.AppendLine("float4 _SelfTexture2D_TexelSize;");
                registry.builder.AppendLine("TEXTURECUBE(_SelfTextureCube);");
                registry.builder.AppendLine("SAMPLER(sampler_SelfTextureCube);");
                registry.builder.AppendLine("float4 _SelfTextureCube_TexelSize;");
                registry.builder.AppendLine("TEXTURE3D(_SelfTexture3D);");
                registry.builder.AppendLine("SAMPLER(sampler_SelfTexture3D);");
                registry.builder.AppendLine("float4 sampler_SelfTexture3D_TexelSize;");
                registry.builder.AppendLine("#endif");
            }
        }
    }
}
