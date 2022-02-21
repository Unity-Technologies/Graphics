using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.CustomRenderTexture.ShaderGraph
{
    [Title("Custom Render Texture", "Size")]
    [SubTargetFilter(typeof(CustomTextureSubTarget))]
    class CustomTextureSize : AbstractMaterialNode, IGeneratesFunction
    {
        private const string kOutputSlotWidthName = "Texture Width";
        private const string kOutputSlotHeightName = "Texture Height";
        private const string kOutputSlotDepthName = "Texture Depth";

        public const int OutputSlotWidthId = 0;
        public const int OutputSlotHeightId = 1;
        public const int OutputSlotDepthId = 2;

        public CustomTextureSize()
        {
            name = "Custom Render Texture Size";
            UpdateNodeAfterDeserialization();
        }

        protected int[] validSlots => new[] { OutputSlotWidthId, OutputSlotHeightId, OutputSlotDepthId };

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotWidthId, kOutputSlotWidthName, kOutputSlotWidthName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotHeightId, kOutputSlotHeightName, kOutputSlotHeightName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotDepthId, kOutputSlotDepthName, kOutputSlotDepthName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(validSlots);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlotHeightId:
                    return "_CustomRenderTextureHeight";
                case OutputSlotDepthId:
                    return "_CustomRenderTextureDepth";
                default:
                    return "_CustomRenderTextureWidth";
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            // For preview only we declare CRT defines
            if (generationMode == GenerationMode.Preview)
            {
                registry.builder.AppendLine("#ifndef _CustomRenderTextureHeight");
                registry.builder.AppendLine("#define _CustomRenderTextureHeight 1.0");
                registry.builder.AppendLine("#endif");
                registry.builder.AppendLine("#ifndef _CustomRenderTextureWidth");
                registry.builder.AppendLine("#define _CustomRenderTextureWidth 1.0");
                registry.builder.AppendLine("#endif");
                registry.builder.AppendLine("#ifndef _CustomRenderTextureDepth");
                registry.builder.AppendLine("#define _CustomRenderTextureDepth 1.0");
                registry.builder.AppendLine("#endif");
            }
        }
    }
}
