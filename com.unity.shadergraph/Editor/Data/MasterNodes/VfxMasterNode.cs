using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    sealed class VfxMasterNode : MasterNode, IMayRequirePosition
    {
        public const string PositionName = "Position";
        public const int PositionSlotId = 0;

        public const string BaseColorSlotName = "Base Color";
        public const int BaseColorSlotId = 1;

        public const string MetallicSlotName = "Metallic";
        public const int MetallicSlotId = 2;

        public const string SmoothnessSlotName = "Smoothness";
        public const int SmoothnessSlotId = 3;

        public const string AlphaSlotName = "Alpha";
        public const int AlphaSlotId = 4;

        public VfxMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();

            name = "Visual Effect Master";

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionName, PositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new ColorRGBMaterialSlot(BaseColorSlotId, BaseColorSlotName, NodeUtils.GetHLSLSafeName(BaseColorSlotName), SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(new[]
            {
                PositionSlotId,
                BaseColorSlotId,
                MetallicSlotId,
                SmoothnessSlotId,
                AlphaSlotId
            });
        }

        public override bool hasPreview => false;

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetSlots(slots);

            List<MaterialSlot> validSlots = new List<MaterialSlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].stageCapability != ShaderStageCapability.All && slots[i].stageCapability != stageCapability)
                    continue;

                validSlots.Add(slots[i]);
            }
            return validSlots.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition(stageCapability));
        }

        public override string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return false;
        }

        public override int GetPreviewPassIndex()
        {
            return 0;
        }
    }
}
