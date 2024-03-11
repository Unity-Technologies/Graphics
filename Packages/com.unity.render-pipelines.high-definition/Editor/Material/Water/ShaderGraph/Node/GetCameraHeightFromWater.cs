using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Input", "High Definition Render Pipeline", "Water", "GetCameraHeightFromWater")]
    sealed class GetCameraHeightFromWater : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        const int kHeightOutputSlotId = 0;
        const string kHeightOutputSlotName = "Height";

        public override bool hasPreview { get { return false; } }

        public GetCameraHeightFromWater()
        {
            name = "Camera Height From Water";
            synonyms = new string[] { "surface", "underwater" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Output
            AddSlot(new Vector1MaterialSlot(kHeightOutputSlotId, kHeightOutputSlotName, kHeightOutputSlotName, SlotType.Output, 0.0f));

            RemoveSlotsNameNotMatching(new[]
            {
                // Output
                kHeightOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("$precision {0} = GetWaterCameraHeight();",
                    GetVariableNameForSlot(kHeightOutputSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision {0} = 0.0;", GetVariableNameForSlot(kHeightOutputSlotId));
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
                registry.RequiresIncludePath("Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/UnderWaterUtilities.hlsl");
        }
    }
}
