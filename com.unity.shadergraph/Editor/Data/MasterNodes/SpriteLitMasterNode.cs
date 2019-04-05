using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master", "Sprite Unlit")]
    class SpriteLitMasterNode : MasterNode<ISpriteLitSubShader>, IMayRequirePosition
    {
        public const string PositionName = "Position";
        public const string ColorSlotName = "Color";
        public const string MaskSlotName =  "Mask";
        public const string NormalSlotName = "Normal";


        public const int PositionSlotId = 9;
        public const int ColorSlotId = 0;
        public const int MaskSlotId = 1;
        public const int NormalSlotId = 2;

        public SpriteLitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Lit Sprite Master";

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionName, PositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new ColorRGBAMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.white, ShaderStageCapability.Fragment));
            AddSlot(new ColorRGBAMaterialSlot(MaskSlotId, MaskSlotName, MaskSlotName, SlotType.Input, Color.white, ShaderStageCapability.Fragment));
            AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(
                new[]
            {
                PositionSlotId,
                ColorSlotId,
                MaskSlotId,
                NormalSlotId,
            });
        }

        //public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        //{
        //    List<MaterialSlot> slots = new List<MaterialSlot>();
        //    GetSlots(slots);

        //    List<MaterialSlot> validSlots = new List<MaterialSlot>();
        //    for (int i = 0; i < slots.Count; i++)
        //    {
        //        if (slots[i].stageCapability != ShaderStageCapability.All && slots[i].stageCapability != stageCapability)
        //            continue;

        //        validSlots.Add(slots[i]);
        //    }
        //    return validSlots.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal(stageCapability));
        //}

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
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
    }
}
