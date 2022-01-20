using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateTipThickness_Water (Preview)")]
    class EvaluateTipThickness_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateTipThickness_Water()
        {
            name = "Evaluate Tip Thickness Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateTipThickness_Water");

        const int kViewWSInputSlotId = 0;
        const string kViewWSInputSlotName = "ViewWS";

        const int kLowFrequencyNormalWSInputSlotId = 1;
        const string kLowFrequencyNormalWSInputSlotName = "LowFrequencyNormalWS";

        const int kLowFrequencyHeightInputSlotId = 2;
        const string kLowFrequencyHeightInputSlotName = "LowFrequencyHeight";

        const int kTipThicknessOutputSlotId = 3;
        const string kTipThicknessOutputSlotName = "TipThickness";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kViewWSInputSlotId, kViewWSInputSlotName, kViewWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kLowFrequencyNormalWSInputSlotId, kLowFrequencyNormalWSInputSlotName, kLowFrequencyNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightInputSlotId, kLowFrequencyHeightInputSlotName, kLowFrequencyHeightInputSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector1MaterialSlot(kTipThicknessOutputSlotId, kTipThicknessOutputSlotName, kTipThicknessOutputSlotName, SlotType.Output, 0.0f));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kViewWSInputSlotId,
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
                string viewWS = GetSlotValue(kViewWSInputSlotId, generationMode);
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
    }
}
