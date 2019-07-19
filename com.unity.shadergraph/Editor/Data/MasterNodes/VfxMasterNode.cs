using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    sealed class VfxMasterNode : MasterNode, IMayRequirePosition
    {
        public const string PositionName = "Position";
        public const int PositionSlotId = 0;
        
        public const string ColorSlotName = "Color";
        public const int ColorSlotId = 1;
        
        public VfxMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            
            name = "Visual Effect Master";
            
            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionName, PositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new ColorRGBMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(new[]
            {
                PositionSlotId,
                ColorSlotId
            });
        }
        
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
