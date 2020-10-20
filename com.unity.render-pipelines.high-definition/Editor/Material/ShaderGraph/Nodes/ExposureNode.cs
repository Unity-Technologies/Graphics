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
    [Title("Input", "High Definition Render Pipeline", "Exposure")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.ExposureNode")]
    class ExposureNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public enum ExposureType
        {
            CurrentMultiplier,
            InverseCurrentMultiplier,
            PreviousMultiplier,
            InversePreviousMultiplier,
        }

        static Dictionary<ExposureType, string> exposureFunctions = new Dictionary<ExposureType, string>()
        {
            {ExposureType.CurrentMultiplier, "GetCurrentExposureMultiplier()"},
            {ExposureType.PreviousMultiplier, "GetPreviousExposureMultiplier()"},
            {ExposureType.InverseCurrentMultiplier, "GetInverseCurrentExposureMultiplier()"},
            {ExposureType.InversePreviousMultiplier, "GetInversePreviousExposureMultiplier()"},
        };

        public ExposureNode()
        {
            name = "Exposure";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("SGNode-Exposure");

        [SerializeField]
        ExposureType        m_ExposureType;
        [EnumControl("Type")]
        public ExposureType exposureType
        {
            get => m_ExposureType;
            set
            {
                m_ExposureType = value;
                Dirty(ModificationScope.Node);
            }
        }

        const int kExposureOutputSlotId = 0;
        const string kExposureOutputSlotName = "Output";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ColorRGBMaterialSlot(kExposureOutputSlotId, kExposureOutputSlotName, kExposureOutputSlotName , SlotType.Output, Color.black, ColorMode.Default));

            RemoveSlotsNameNotMatching(new[] {
                kExposureOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("#ifdef SHADERGRAPH_PREVIEW");
            sb.AppendLine($"$precision {GetVariableNameForSlot(kExposureOutputSlotId)} = 1.0;");
            sb.AppendLine("#else");
            sb.AppendLine($"$precision {GetVariableNameForSlot(kExposureOutputSlotId)} = {exposureFunctions[exposureType]};");
            sb.AppendLine("#endif");
        }
    }
}
