using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.Rendering.HDPipeline.Drawing;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Experimental.Rendering.HDPipeline;


namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [Serializable]
    [Title("Master", "HDRP/Decal")]
    class DecalMasterNode : MasterNode<IDecalSubShader>, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string PositionSlotName = "Position";
        public const int PositionSlotId = 0;

        public const string AlbedoSlotName = "Albedo";
        public const string AlbedoDisplaySlotName = "BaseColor";
        public const int AlbedoSlotId = 1;

        public const string BaseColorOpacitySlotName = "AlphaAlbedo";
        public const string BaseColorOpacityDisplaySlotName = "BaseColor Opacity";
        public const int BaseColorOpacitySlotId = 2;

        public const string NormalSlotName = "Normal";
        public const int NormalSlotId = 3;

        public const string NormaOpacitySlotName = "AlphaNormal";
        public const string NormaOpacityDisplaySlotName = "Normal Opacity";
        public const int NormaOpacitySlotId = 4;

        public const string MetallicSlotName = "Metallic";
        public const int MetallicSlotId = 5;

        public const string AmbientOcclusionSlotName = "Occlusion";
        public const string AmbientOcclusionDisplaySlotName = "Ambient Occlusion";
        public const int AmbientOcclusionSlotId = 6;

        public const string SmoothnessSlotName = "Smoothness";
        public const int SmoothnessSlotId = 7;

        public const string MAOSOpacitySlotName = "MAOSOpacity";
        public const string MAOSOpacityDisplaySlotName = "MAOS Opacity";
        public const int MAOSOpacitySlotId = 8;



        // Just for convenience of doing simple masks. We could run out of bits of course.
        [Flags]
        enum SlotMask
        {
            None = 0,
            Position = 1 << PositionSlotId,
            Albedo = 1 << AlbedoSlotId,
            AlphaAlbedo = 1 << BaseColorOpacitySlotId,
            Normal = 1 << NormalSlotId,
            AlphaNormal = 1 << NormaOpacitySlotId,
            Metallic = 1 << MetallicSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Smoothness = 1 << SmoothnessSlotId,
            AlphaMAOS = 1 << MAOSOpacitySlotId
        }

        const SlotMask decalParameter = SlotMask.Position | SlotMask.Albedo | SlotMask.AlphaAlbedo | SlotMask.Normal | SlotMask.AlphaNormal | SlotMask.Metallic | SlotMask.Occlusion | SlotMask.Smoothness | SlotMask.AlphaMAOS;
        

        // This could also be a simple array. For now, catch any mismatched data.
        SlotMask GetActiveSlotMask()
        {
            return decalParameter;
        }

        bool MaterialTypeUsesSlotMask(SlotMask mask)
        {
            SlotMask activeMask = GetActiveSlotMask();
            return (activeMask & mask) != 0;
        }

        public DecalMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return null; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Decal Master";

            List<int> validSlots = new List<int>();

            // Position
            if (MaterialTypeUsesSlotMask(SlotMask.Position))
            {
                AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(PositionSlotId);
            }

            // Albedo
            if (MaterialTypeUsesSlotMask(SlotMask.Albedo))
            {
                AddSlot(new ColorRGBMaterialSlot(AlbedoSlotId, AlbedoDisplaySlotName, AlbedoSlotName, SlotType.Input, Color.white, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(AlbedoSlotId);
            }

            // AlphaAlbedo
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaAlbedo))
            {
                AddSlot(new Vector1MaterialSlot(BaseColorOpacitySlotId, BaseColorOpacityDisplaySlotName, BaseColorOpacitySlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(BaseColorOpacitySlotId);
            }

            // Normal
            if (MaterialTypeUsesSlotMask(SlotMask.Normal))
            {
                AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                validSlots.Add(NormalSlotId);
            }

            // AlphaNormal
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaNormal))
            {
                AddSlot(new Vector1MaterialSlot(NormaOpacitySlotId, NormaOpacityDisplaySlotName, NormaOpacitySlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(NormaOpacitySlotId);
            }

            // Metal
            if (MaterialTypeUsesSlotMask(SlotMask.Metallic))
            {
                AddSlot(new Vector1MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(MetallicSlotId);
            }


            // Ambient Occlusion
            if (MaterialTypeUsesSlotMask(SlotMask.Occlusion))
            {
                AddSlot(new Vector1MaterialSlot(AmbientOcclusionSlotId, AmbientOcclusionDisplaySlotName, AmbientOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AmbientOcclusionSlotId);
            }

            // Smoothness
            if (MaterialTypeUsesSlotMask(SlotMask.Smoothness))
            {
                AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(SmoothnessSlotId);
            }


            // Alpha MAOS
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaMAOS))
            {
                AddSlot(new Vector1MaterialSlot(MAOSOpacitySlotId, MAOSOpacityDisplaySlotName, MAOSOpacitySlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(MAOSOpacitySlotId);
            }

            RemoveSlotsNameNotMatching(validSlots, true);
        }

        protected override VisualElement CreateCommonSettingsElement()
        {
            return new DecalSettingsView(this);
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
            Vector1ShaderProperty drawOrder = new Vector1ShaderProperty();
            drawOrder.overrideReferenceName = "_DrawOrder";
            drawOrder.displayName = "Draw Order";
            drawOrder.floatType = FloatType.Integer;
            drawOrder.value = 0;
            collector.AddShaderProperty(drawOrder);

            Vector1ShaderProperty decalMeshDepthBias = new Vector1ShaderProperty();
            decalMeshDepthBias.overrideReferenceName = "_DecalMeshDepthBias";
            decalMeshDepthBias.displayName = "DecalMesh DepthBias";
            decalMeshDepthBias.floatType = FloatType.Default;
            decalMeshDepthBias.value = 0;
            collector.AddShaderProperty(decalMeshDepthBias);

            base.CollectShaderProperties(collector, generationMode);
        }

        [SerializeField]
        bool m_AffectsMetal = true;

        public ToggleData affectsMetal
        {
            get { return new ToggleData(m_AffectsMetal); }
            set
            {
                if (m_AffectsMetal == value.isOn)
                    return;
                m_AffectsMetal = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AffectsAO = true;

        public ToggleData affectsAO
        {
            get { return new ToggleData(m_AffectsAO); }
            set
            {
                if (m_AffectsAO == value.isOn)
                    return;
                m_AffectsAO = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AffectsSmoothness = true;

        public ToggleData affectsSmoothness
        {
            get { return new ToggleData(m_AffectsSmoothness); }
            set
            {
                if (m_AffectsSmoothness == value.isOn)
                    return;
                m_AffectsSmoothness = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AffectsAlbedo = true;

        public ToggleData affectsAlbedo
        {
            get { return new ToggleData(m_AffectsAlbedo); }
            set
            {
                if (m_AffectsAlbedo == value.isOn)
                    return;
                m_AffectsAlbedo = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AffectsNormal = true;

        public ToggleData affectsNormal
        {
            get { return new ToggleData(m_AffectsNormal); }
            set
            {
                if (m_AffectsNormal == value.isOn)
                    return;
                m_AffectsNormal = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        int m_DrawOrder;

        public int drawOrder
        {
            get { return m_DrawOrder; }
            set
            {
                if (m_DrawOrder == value)
                    return;
                m_DrawOrder = value;
                Dirty(ModificationScope.Graph);
            }
        }

    }
}
