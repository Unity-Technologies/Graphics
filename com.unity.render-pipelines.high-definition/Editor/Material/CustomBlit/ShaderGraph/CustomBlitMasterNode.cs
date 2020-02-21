using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Rendering.HighDefinition.Drawing;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    [Title("Master", "HDRP/CustomBlit")]
    class CustomBlitMasterNode : MasterNode<ICustomBlitSubShader>
    {     
        public const string BaseColorSlotName = "BaseColor";
        public const string BaseColorDisplaySlotName = "BaseColor";
        public const int BaseColorSlotId = 0;
     
        // Just for convenience of doing simple masks. We could run out of bits of course.
        [Flags]
        enum SlotMask
        {
            None = 0,          
            BaseColor = 1 << BaseColorSlotId
        }

        const SlotMask CustomBlitParameter = SlotMask.BaseColor;
        

        // This could also be a simple array. For now, catch any mismatched data.
        SlotMask GetActiveSlotMask()
        {
            return CustomBlitParameter;
        }

        bool MaterialTypeUsesSlotMask(SlotMask mask)
        {
            SlotMask activeMask = GetActiveSlotMask();
            return (activeMask & mask) != 0;
        }

        public CustomBlitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return null; }
        }

        public enum BlendType
        {
            None,
            Add,
            Blend
        }

        [SerializeField]
        BlendType m_BlendType;

        public BlendType blendType
        {
            get { return m_BlendType; }
            set
            {
                if (m_BlendType == value)
                    return;

                m_BlendType = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "CustomBlit Master";

            List<int> validSlots = new List<int>();

            // Position
            if (MaterialTypeUsesSlotMask(SlotMask.BaseColor))
            {
                AddSlot(new ColorRGBAMaterialSlot(BaseColorSlotId, BaseColorDisplaySlotName, BaseColorSlotName, SlotType.Input, Color.grey.gamma, ShaderStageCapability.Fragment));
                validSlots.Add(BaseColorSlotId);
            }

            RemoveSlotsNameNotMatching(validSlots, true);
        }

        protected override VisualElement CreateCommonSettingsElement()
        {
            return new CustomBlitSettingsView(this);
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

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {           
            base.CollectShaderProperties(collector, generationMode);
        }
    }
}
