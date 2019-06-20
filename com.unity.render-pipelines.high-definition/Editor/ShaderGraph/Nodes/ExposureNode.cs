using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [Title("Input", "High Definition Render Pipeline", "Exposure")]
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

        public override string documentationURL
        {
            // TODO: write the doc
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Exposure-Node"; }
        }

        [SerializeField]
        ExposureType        m_ExposureType;
        [EnumControl]
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

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            string exposure = generationMode.IsPreview() ? "1.0" : exposureFunctions[exposureType];

            sb.AppendLine("$precision {0} = {1};",
                GetVariableNameForSlot(kExposureOutputSlotId),
                exposure);
        }
    }
}
