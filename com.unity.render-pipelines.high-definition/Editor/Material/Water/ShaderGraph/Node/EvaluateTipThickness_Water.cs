using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateTipThickness_Water")]
    class EvaluateTipThickness_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireViewDirection
    {
        public EvaluateTipThickness_Water()
        {
            name = "Evaluate Tip Thickness Water";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateTipThickness_Water");

        const int kLowFrequencyNormalWSInputSlotId = 0;
        const string kLowFrequencyNormalWSInputSlotName = "LowFrequencyNormalWS";

        const int kLowFrequencyHeightInputSlotId = 1;
        const string kLowFrequencyHeightInputSlotName = "LowFrequencyHeight";

        const int kTipThicknessOutputSlotId = 2;
        const string kTipThicknessOutputSlotName = "TipThickness";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kLowFrequencyNormalWSInputSlotId, kLowFrequencyNormalWSInputSlotName, kLowFrequencyNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightInputSlotId, kLowFrequencyHeightInputSlotName, kLowFrequencyHeightInputSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector1MaterialSlot(kTipThicknessOutputSlotId, kTipThicknessOutputSlotName, kTipThicknessOutputSlotName, SlotType.Output, 0.0f));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kLowFrequencyNormalWSInputSlotId,
                kLowFrequencyHeightInputSlotId,

                // Output
                kTipThicknessOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                // Evaluate the refraction parameters
                string viewWS = $"IN.{CoordinateSpace.World.ToVariableName(InterpolatorType.ViewDirection)}";
                string lfNormalWS = GetSlotValue(kLowFrequencyNormalWSInputSlotId, generationMode);
                string lfHeight = GetSlotValue(kLowFrequencyHeightInputSlotId, generationMode);

                sb.AppendLine("$precision {3} = EvaluateTipThickness({0}, {1}, {2});",
                    viewWS,
                    lfNormalWS,
                    lfHeight,
                    GetVariableNameForSlot(kTipThicknessOutputSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision {0} = 0.0;", GetVariableNameForSlot(kTipThicknessOutputSlotId));
            }
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }
    }
}
