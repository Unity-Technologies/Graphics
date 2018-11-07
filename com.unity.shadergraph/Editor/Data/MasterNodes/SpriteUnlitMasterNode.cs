using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master", "Sprite Unlit")]
    public class SpriteUnlitMasterNode : MasterNode<ISpriteUnlitSubShader>, IMayRequirePosition
    {
        public const string ColorSlotName = "Color";
        public const string AlphaSlotName = "Alpha";
        public const string PositionName = "Position";


        public const int ColorSlotId = 0;
        public const int AlphaSlotId = 7;
        public const int PositionSlotId = 9;


        // [SerializeField]
        // SurfaceType m_SurfaceType;

        // public SurfaceType surfaceType
        // {
        //     get { return m_SurfaceType; }
        //     set
        //     {
        //         if (m_SurfaceType == value)
        //             return;

        //         m_SurfaceType = value;
        //         Dirty(ModificationScope.Graph);
        //     }
        // }

        public SpriteUnlitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Sprite Unlit Master";

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionName, PositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new ColorRGBMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.grey, ColorMode.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(
                new[]
            {
                PositionSlotId,
                ColorSlotId,
                AlphaSlotId
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