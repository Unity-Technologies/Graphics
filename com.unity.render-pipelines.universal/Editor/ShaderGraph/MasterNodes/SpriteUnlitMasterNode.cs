using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [Serializable]
    [Title("Master", "Sprite Unlit (Experimental)")]
    [FormerName("UnityEditor.Experimental.Rendering.LWRP.SpriteUnlitMasterNode")]
    class SpriteUnlitMasterNode : MasterNode<ISpriteUnlitSubShader>, IMayRequirePosition
    {
        public const string PositionName = "Position";
        public const string ColorSlotName = "Color";


        public const int PositionSlotId = 9;
        public const int ColorSlotId = 0;

        public SpriteUnlitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Sprite Unlit Master";

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionName, PositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new ColorRGBAMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.white, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(
                new[]
            {
                PositionSlotId,
                ColorSlotId,
            });
        }

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
