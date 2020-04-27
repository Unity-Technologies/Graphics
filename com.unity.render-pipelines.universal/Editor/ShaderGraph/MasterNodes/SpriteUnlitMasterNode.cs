using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [Serializable]
    [Title("Master", "Sprite Unlit (Experimental)")]
    [FormerName("UnityEditor.Experimental.Rendering.LWRP.SpriteUnlitMasterNode")]
    class SpriteUnlitMasterNode : AbstractMaterialNode, IMasterNode, IHasSettings, ICanChangeShaderGUI, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string PositionName = "Vertex Position";
        public const string NormalName = "Vertex Normal";
        public const string TangentName = "Vertex Tangent";
        public const string ColorSlotName = "Color";


        public const int PositionSlotId = 9;
        public const int ColorSlotId = 0;
        public const int VertNormalSlotId = 10;
        public const int VertTangentSlotId = 11;

        [SerializeField] private string m_ShaderGUIOverride;
        public string ShaderGUIOverride
        {
            get => m_ShaderGUIOverride;
            set => m_ShaderGUIOverride = value;
        }

        [SerializeField] private bool m_OverrideEnabled;
        public bool OverrideEnabled
        {
            get => m_OverrideEnabled;
            set => m_OverrideEnabled = value;
        }

        public SpriteUnlitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Sprite Unlit Master";

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionName, PositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new NormalMaterialSlot(VertNormalSlotId, NormalName, NormalName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new TangentMaterialSlot(VertTangentSlotId, TangentName, TangentName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new ColorRGBAMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.white, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(
                new[]
            {
                PositionSlotId,
                VertNormalSlotId,
                VertTangentSlotId,
                ColorSlotId,
            });
        }

        public VisualElement CreateSettingsElement()
        {
            return new SpriteSettingsView(this);
        }

        public string renderQueueTag => $"{RenderQueue.Transparent}";
        public string renderTypeTag => $"{RenderType.Transparent}";

        public ConditionalField[] GetConditionalFields(PassDescriptor pass)
        {
            return new ConditionalField[]
            {
                // Features
                new ConditionalField(Fields.GraphVertex,         IsSlotConnected(PBRMasterNode.PositionSlotId) ||
                                                                        IsSlotConnected(PBRMasterNode.VertNormalSlotId) ||
                                                                        IsSlotConnected(PBRMasterNode.VertTangentSlotId)),
                new ConditionalField(Fields.GraphPixel,          true),

                // Surface Type
                new ConditionalField(Fields.SurfaceTransparent,  true),

                // Blend Mode
                new ConditionalField(Fields.BlendAlpha,          true),

                // Culling
                new ConditionalField(Fields.DoubleSided,         true),
            };
        }

        public void ProcessPreviewMaterial(Material material)
        {
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
