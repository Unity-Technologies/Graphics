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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateScatteringColor_Water")]
    class EvaluateScatteringColor_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateScatteringColor_Water()
        {
            name = "Evaluate Scattering Color Water";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateScatteringColor_Water");

        const int kAbsorptionTintInputSlotId = 0;
        const string kAbsorptionTintInputSlotName = "AbsorptionTint";

        const int kLowFrequencyHeightInputSlotId = 1;
        const string kLowFrequencyHeightInputSlotName = "LowFrequencyHeight";

        const int kHorizontalDisplacementInputSlotId = 2;
        const string kHorizontalDisplacementInputSlotName = "HorizontalDisplacement";

        const int kDeepFoamInputSlotId = 3;
        const string kDeepFoamInputSlotName = "DeepFoam";

        const int kScatteringColorOutputSlotId = 4;
        const string kScatteringColorOutputSlotName = "BaseColor";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kAbsorptionTintInputSlotId, kAbsorptionTintInputSlotName, kAbsorptionTintInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightInputSlotId, kLowFrequencyHeightInputSlotName, kLowFrequencyHeightInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kHorizontalDisplacementInputSlotId, kHorizontalDisplacementInputSlotName, kHorizontalDisplacementInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kDeepFoamInputSlotId, kDeepFoamInputSlotName, kDeepFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kScatteringColorOutputSlotId, kScatteringColorOutputSlotName, kScatteringColorOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kAbsorptionTintInputSlotId,
                kLowFrequencyHeightInputSlotId,
                kHorizontalDisplacementInputSlotId,
                kDeepFoamInputSlotId,

                // Output
                kScatteringColorOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                // Evaluate the data
                sb.AppendLine("$precision3 {5} = EvaluateScatteringColor(IN.{0}.xzy, {1}, {2}, {3}, {4});",
                    ShaderGeneratorNames.GetUVName(UVChannel.UV0),
                    GetSlotValue(kLowFrequencyHeightInputSlotId, generationMode),
                    GetSlotValue(kHorizontalDisplacementInputSlotId, generationMode),
                    GetSlotValue(kAbsorptionTintInputSlotId, generationMode),
                    GetSlotValue(kDeepFoamInputSlotId, generationMode),
                    GetVariableNameForSlot(kScatteringColorOutputSlotId));
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kScatteringColorOutputSlotId));
            }
        }
    }
}
