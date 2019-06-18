using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.Rendering.HDPipeline.Drawing;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [Serializable]
    [Title("Master", "HDRP/Full Screen Pass")]
    class FullScreenPassMasterNode : MasterNode<IFullScreenPassSubShader>, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string ColorSlotName = "Color";
        public const string DepthSlotName = "Depth";

        public const int ColorSlotId = 1;
        public const int DepthSlotId = 2;

        // Just for convenience of doing simple masks. We could run out of bits of course.
        [Flags]
        enum SlotMask
        {
            None = 0,
            Color = 1 << ColorSlotId,
            Depth = 1 << DepthSlotId,
        }

        const SlotMask ColorSlotMask = SlotMask.Color;
        const SlotMask ColorDepthSlotMask = SlotMask.Color | SlotMask.Depth;

        [SerializeField]
        SurfaceType m_SurfaceType;

        public SurfaceType surfaceType
        {
            get { return m_SurfaceType; }
            set
            {
                if (m_SurfaceType == value)
                    return;

                m_SurfaceType = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_ModifyDepth = false;
        public ToggleData modifyDepth
        {
            get { return new ToggleData(m_ModifyDepth); }
            set
            {
                if (m_ModifyDepth == value.isOn)
                    return;
                
                m_ModifyDepth = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        AlphaMode m_AlphaMode;

        public AlphaMode alphaMode
        {
            get { return m_AlphaMode; }
            set
            {
                if (m_AlphaMode == value)
                    return;

                m_AlphaMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        SlotMask GetActiveSlotMask()
        {
            if (modifyDepth.isOn)
                return ColorDepthSlotMask;
            else
                return ColorSlotMask;
        }

        bool MaterialTypeUsesSlotMask(SlotMask mask)
        {
            SlotMask activeMask = GetActiveSlotMask();
            return (activeMask & mask) != 0;
        }

        public FullScreenPassMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/HD-FullScreen-Pass-Node"; }
        }
        
        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "FullScreen Pass Master";

            List<int> validSlots = new List<int>();
            if (MaterialTypeUsesSlotMask(SlotMask.Color))
            {
                AddSlot(new ColorRGBMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(ColorSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Depth))
            {
                AddSlot(new Vector1MaterialSlot(DepthSlotId, DepthSlotName, DepthSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(DepthSlotId);
            }

            RemoveSlotsNameNotMatching(validSlots, true);
        }

        protected override VisualElement CreateCommonSettingsElement()
        {
            return new FullScreenPassSettingsView(this);
        }

        // TODO: something about these functions, i don't think we'll allow to fetch these inside the PP graph
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
    }
}
