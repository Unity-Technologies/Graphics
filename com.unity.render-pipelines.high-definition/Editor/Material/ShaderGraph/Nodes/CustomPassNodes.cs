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
    [Title("Input", "High Definition Render Pipeline", "Custom Color Buffer")]
    class CustomColorBufferNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireScreenPosition
    {
        public CustomColorBufferNode()
        {
            name = "Custom Color Buffer";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("SGNode-HD-Custom-Color-Node");

        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "UV";

        const int kColorOutputSlotId = 1;
        const string kColorOutputSlotName = "Output";

        public override bool hasPreview => false;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default));
            AddSlot(new Vector4MaterialSlot(kColorOutputSlotId, kColorOutputSlotName, kColorOutputSlotName, SlotType.Output, Vector4.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kUvInputSlotId,
                kColorOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string uv = GetSlotValue(kUvInputSlotId, generationMode);

            if (generationMode.IsPreview())
                sb.AppendLine($"$precision4 {GetVariableNameForSlot(kColorOutputSlotId)} = 1;");
            else
                sb.AppendLine($"$precision4 {GetVariableNameForSlot(kColorOutputSlotId)} = SampleCustomColor({uv}.xy);");
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) => true;
    }

    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Input", "High Definition Render Pipeline", "Custom Depth Buffer")]
    class CustomDepthBufferNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireScreenPosition
    {
        public CustomDepthBufferNode()
        {
            name = "Custom Depth Buffer";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private DepthSamplingMode m_DepthSamplingMode = DepthSamplingMode.Linear01;

        [EnumControl("Sampling Mode")]
        public DepthSamplingMode depthSamplingMode
        {
            get { return m_DepthSamplingMode; }
            set
            {
                if (m_DepthSamplingMode == value)
                    return;

                m_DepthSamplingMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override string documentationURL => Documentation.GetPageLink("SGNode-HD-Custom-Depth-Node");

        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "UV";

        const int kDepthOutputSlotId = 1;
        const string kDepthOutputSlotName = "Output";

        public override bool hasPreview => false;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default));
            AddSlot(new Vector1MaterialSlot(kDepthOutputSlotId, kDepthOutputSlotName, kDepthOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                kUvInputSlotId,
                kDepthOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string uv = GetSlotValue(kUvInputSlotId, generationMode);

            string depthValue = $"SampleCustomDepth({uv}.xy)";

            if (depthSamplingMode == DepthSamplingMode.Eye)
                depthValue = $"LinearEyeDepth({depthValue}, _ZBufferParams)";
            if (depthSamplingMode == DepthSamplingMode.Linear01)
                depthValue = $"Linear01Depth({depthValue}, _ZBufferParams)";

            if (generationMode.IsPreview())
                sb.AppendLine($"$precision {GetVariableNameForSlot(kDepthOutputSlotId)} = 0;");
            else
                sb.AppendLine($"$precision {GetVariableNameForSlot(kDepthOutputSlotId)} = {depthValue};");
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) => true;
    }
}
