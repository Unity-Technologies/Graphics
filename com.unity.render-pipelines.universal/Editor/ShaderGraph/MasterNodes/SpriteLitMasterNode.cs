using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [Serializable]
    [Title("Master", "Sprite Lit (Experimental)")]
    [FormerName("UnityEditor.Experimental.Rendering.LWRP.SpriteLitMasterNode")]
    class SpriteLitMasterNode : MasterNode<ISpriteLitSubShader>, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string PositionName = "Vertex Position";
        public const string NormalName = "Vertex Normal";
        public const string TangentName = "Vertex Tangent";
        public const string ColorSlotName = "Color";
        public const string MaskSlotName =  "Mask";
        public const string NormalSlotName = "Normal";

        public const int PositionSlotId = 9;
        public const int ColorSlotId = 0;
        public const int MaskSlotId = 1;
        public const int NormalSlotId = 2;
        public const int VertNormalSlotId = 10;
        public const int VertTangentSlotId = 11;

        public SpriteLitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Sprite Lit Master";

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionName, PositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new NormalMaterialSlot(VertNormalSlotId, NormalName, NormalName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new TangentMaterialSlot(VertTangentSlotId, TangentName, TangentName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new ColorRGBAMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.white, ShaderStageCapability.Fragment));
            AddSlot(new ColorRGBAMaterialSlot(MaskSlotId, MaskSlotName, MaskSlotName, SlotType.Input, Color.white, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, new Vector3(0.0f, 0.0f, 1.0f), ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(
                new[]
            {
                PositionSlotId,
                VertNormalSlotId,
                VertTangentSlotId,
                ColorSlotId,
                MaskSlotId,
                NormalSlotId,
            });
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
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
            return validSlots.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal(stageCapability));
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

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
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
            return validSlots.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresTangent(stageCapability));
        }
    }
}
