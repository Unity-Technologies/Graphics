using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.BakedGAbstractMaterialNode")]
    [FormerName("UnityEditor.ShaderGraph.LightProbeNode")]
    [Title("Input", "Lighting", "Baked GI")]
    class BakedGINode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePixelPosition, IMayRequirePosition, IMayRequireNormal, IMayRequireMeshUV
    {
        public override bool hasPreview { get { return false; } }

        public BakedGINode()
        {
            name = "Baked GI";
            synonyms = new string[] { "global illumination" };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private bool m_ApplyScaling = true;

        [ToggleControl("Apply Lightmap Scaling")]
        public ToggleData applyScaling
        {
            get { return new ToggleData(m_ApplyScaling); }
            set
            {
                if (m_ApplyScaling == value.isOn)
                    return;
                m_ApplyScaling = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        const int kNormalWSInputSlotId = 0;
        const string kNormalWSInputSlotName = "NormalWS";

        const int kOutputSlotId = 1;
        const string kOutputSlotName = "Out";

        const int kPositionWSInputSlotId = 2;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kStaticUVInputSlotId = 3;
        const string kStaticUVInputSlotName = "StaticUV";

        const int kDynamicUVInputSlotId = 4;
        const string kDynamicUVInputSlotName = "DynamicUV";
        
        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new NormalMaterialSlot(kNormalWSInputSlotId, kNormalWSInputSlotName, kNormalWSInputSlotName, CoordinateSpace.World));
            AddSlot(new PositionMaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, CoordinateSpace.World));
            AddSlot(new UVMaterialSlot(kStaticUVInputSlotId, kStaticUVInputSlotName, kStaticUVInputSlotName, UVChannel.UV1));
            AddSlot(new UVMaterialSlot(kDynamicUVInputSlotId, kDynamicUVInputSlotName, kDynamicUVInputSlotName, UVChannel.UV2));

            // Output
            AddSlot(new Vector3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kNormalWSInputSlotId,
                kPositionWSInputSlotId,
                kStaticUVInputSlotId,
                kDynamicUVInputSlotId,

                // Output
                kOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("$precision3 {6} = SHADERGRAPH_BAKED_GI({0}, {1}, IN.{2}.xy, {3}, {4}, {5});",
                    GetSlotValue(kPositionWSInputSlotId, generationMode),
                    GetSlotValue(kNormalWSInputSlotId, generationMode),
                    ShaderGeneratorNames.PixelPosition,
                    GetSlotValue(kStaticUVInputSlotId, generationMode),
                    GetSlotValue(kDynamicUVInputSlotId, generationMode),
                    applyScaling.isOn ? "true" : "false",
                    GetVariableNameForSlot(kOutputSlotId));
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kOutputSlotId));
            }
        }

        public bool RequiresPixelPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true; // needed for APV sampling noise when TAA is used
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<PositionMaterialSlot>(kPositionWSInputSlotId).RequiresPosition();
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<NormalMaterialSlot>(kNormalWSInputSlotId).RequiresNormal();
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<UVMaterialSlot>(kStaticUVInputSlotId).RequiresMeshUV(channel) ||
                   FindSlot<UVMaterialSlot>(kDynamicUVInputSlotId).RequiresMeshUV(channel);
        }
    }
}
