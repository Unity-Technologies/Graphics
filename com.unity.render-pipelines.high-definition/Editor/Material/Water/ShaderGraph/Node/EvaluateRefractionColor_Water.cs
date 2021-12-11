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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateRefractionColor_Water (Preview)")]
    class EvaluateRefractionColor_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateRefractionColor_Water()
        {
            name = "Evaluate Refraction Color Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateRefractionColor_Water");

        const int kAbsoprtionTintInputSlotId = 0;
        const string kAbsoprtionTintInputSlotName = "AbsoprtionTint";

        const int kCausticsOutputSlotId = 1;
        const string kCausticsOutputSlotName = "Caustics";

        const int kRefractionColorOutputSlotId = 2;
        const string kRefractionColorOutputSlotName = "RefractionColor";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kAbsoprtionTintInputSlotId, kAbsoprtionTintInputSlotName, kAbsoprtionTintInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kCausticsOutputSlotId, kCausticsOutputSlotName, kCausticsOutputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kRefractionColorOutputSlotId, kRefractionColorOutputSlotName, kRefractionColorOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kAbsoprtionTintInputSlotId,
                kCausticsOutputSlotId,

                // Output
                kRefractionColorOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                // Evaluate the data
                sb.AppendLine("$precision3 {2} = EvaluateRefractionColor({0}, {1});",
                    GetSlotValue(kAbsoprtionTintInputSlotId, generationMode),
                    GetSlotValue(kCausticsOutputSlotId, generationMode),
                    GetVariableNameForSlot(kRefractionColorOutputSlotId));
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0", GetVariableNameForSlot(kRefractionColorOutputSlotId));
            }
        }
    }
}
