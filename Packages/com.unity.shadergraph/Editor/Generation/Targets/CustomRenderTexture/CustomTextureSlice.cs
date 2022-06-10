using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.CustomRenderTexture.ShaderGraph
{
    [Title("Custom Render Texture", "Slice Index / Cubemap Face")]
    [SubTargetFilter(typeof(CustomTextureSubTarget))]
    class CustomTextureSlice : AbstractMaterialNode, IGeneratesFunction
    {
        private const string kOutputSlotCubeFaceName = "Texture Cube Face";
        private const string kOutputSlot3DSliceName = "Texture Depth Slice";

        public const int OutputSlotCubeFaceId = 3;
        public const int OutputSlot3DSliceId = 4;

        public CustomTextureSlice()
        {
            name = "Slice Index / Cubemap Face";
            UpdateNodeAfterDeserialization();
        }

        protected int[] validSlots => new[] { OutputSlotCubeFaceId, OutputSlot3DSliceId };

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotCubeFaceId, kOutputSlotCubeFaceName, kOutputSlotCubeFaceName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot3DSliceId, kOutputSlot3DSliceName, kOutputSlot3DSliceName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(validSlots);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlotCubeFaceId:
                    return "_CustomRenderTextureCubeFace";
                default:
                case OutputSlot3DSliceId:
                    return "_CustomRenderTexture3DSlice";
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            // For preview only we declare CRT defines
            if (generationMode == GenerationMode.Preview)
            {
                registry.builder.AppendLine("#define _CustomRenderTextureCubeFace 0.0");
                registry.builder.AppendLine("#define _CustomRenderTexture3DSlice 0.0");
            }
        }
    }
}
