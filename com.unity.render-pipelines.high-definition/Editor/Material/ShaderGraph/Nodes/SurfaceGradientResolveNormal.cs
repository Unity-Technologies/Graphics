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
    [Title("Utility", "High Definition Render Pipeline", "Surface Gradient", "SurfaceGradientResolveNormal")]
    class SurfaceGradientResolveNormal : AbstractMaterialNode, IGeneratesBodyCode
    {
        public SurfaceGradientResolveNormal()
        {
            name = "Surface Gradient Resolve Normal";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("SurfaceGradientResolveNormal");

        const int kNormalInputSlotId = 0;
        const string kNormalInputSlotName = "Normal";

        const int kSurfaceGradientInputSlotId = 1;
        const string kSurfaceGradientInputSlotName = "SurfaceGradient";

        const int kNormalOutputSlotId = 2;
        const string kNormalOutputSlotName = "NormalOutput";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kNormalInputSlotId, kNormalInputSlotName, kNormalInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.All));
            AddSlot(new Vector3MaterialSlot(kSurfaceGradientInputSlotId, kSurfaceGradientInputSlotName, kSurfaceGradientInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.All));

            AddSlot(new Vector3MaterialSlot(kNormalOutputSlotId, kNormalOutputSlotName, kNormalOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kNormalInputSlotId,
                kSurfaceGradientInputSlotId,

                kNormalOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string normal = GetSlotValue(kNormalInputSlotId, generationMode);
            string surfaceGradient = GetSlotValue(kSurfaceGradientInputSlotId, generationMode);

            sb.AppendLine("$precision3 {0} = SafeNormalize({1} - {2});",
                GetVariableNameForSlot(kNormalOutputSlotId),
                normal,
                surfaceGradient
            );
        }
    }
}
