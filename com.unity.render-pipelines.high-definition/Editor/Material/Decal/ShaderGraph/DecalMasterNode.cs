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
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    [Title("Master", "Decal (HDRP)")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.DecalMasterNode")]
    class DecalMasterNode : AbstractMaterialNode, IMasterNode, IHasSettings, ICanChangeShaderGUI, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string PositionSlotName = "Vertex Position";
        public const string PositionSlotDisplayName = "Vertex Position";
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

        public const string EmissionSlotName = "Emission";
        public const string EmissionDisplaySlotName = "Emission";
        public const int EmissionSlotId = 9;

        public const string VertexNormalSlotName = "Vertex Normal";
         public const int VertexNormalSlotID = 10;

        public const string VertexTangentSlotName = "Vertex Tangent";
        public const int VertexTangentSlotID = 11;

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
                Dirty(ModificationScope.Graph);
            }
        }

        // Just for convenience of doing simple masks. We could run out of bits of course.
        [Flags]
        enum SlotMask
        {
            None = 0,
            Position = 1 << PositionSlotId,
            VertexNormal = 1 << VertexNormalSlotID,
            VertexTangent = 1 << VertexTangentSlotID,
            Albedo = 1 << AlbedoSlotId,
            AlphaAlbedo = 1 << BaseColorOpacitySlotId,
            Normal = 1 << NormalSlotId,
            AlphaNormal = 1 << NormaOpacitySlotId,
            Metallic = 1 << MetallicSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Smoothness = 1 << SmoothnessSlotId,
            AlphaMAOS = 1 << MAOSOpacitySlotId,
            Emission = 1 << EmissionSlotId
        }

        const SlotMask decalParameter = SlotMask.Position | SlotMask.VertexNormal | SlotMask.VertexTangent | SlotMask.Albedo | SlotMask.AlphaAlbedo | SlotMask.Normal | SlotMask.AlphaNormal | SlotMask.Metallic | SlotMask.Occlusion | SlotMask.Smoothness | SlotMask.AlphaMAOS | SlotMask.Emission;


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
                AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotDisplayName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(PositionSlotId);
            }

            //Normal in Vertex
            if (MaterialTypeUsesSlotMask(SlotMask.VertexNormal))
            {
                AddSlot(new NormalMaterialSlot(VertexNormalSlotID, VertexNormalSlotName, VertexNormalSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(VertexNormalSlotID);
            }

            //Tangent in Vertex
            if (MaterialTypeUsesSlotMask(SlotMask.VertexTangent))
            {
                AddSlot(new TangentMaterialSlot(VertexTangentSlotID, VertexTangentSlotName, VertexTangentSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(VertexTangentSlotID);
            }

            // Albedo
            if (MaterialTypeUsesSlotMask(SlotMask.Albedo))
            {
                AddSlot(new ColorRGBMaterialSlot(AlbedoSlotId, AlbedoDisplaySlotName, AlbedoSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
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

            // Alpha MAOS
            if (MaterialTypeUsesSlotMask(SlotMask.Emission))
            {
                AddSlot(new ColorRGBMaterialSlot(EmissionSlotId, EmissionDisplaySlotName, EmissionSlotName, SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
                validSlots.Add(EmissionSlotId);
            }

            RemoveSlotsNameNotMatching(validSlots, true);
        }

        public VisualElement CreateSettingsElement()
        {
            return new DecalSettingsView(this);
        }

        public string renderQueueTag
        {
            get
            {
                return HDRenderQueue.GetShaderTagValue(
                    HDRenderQueue.ChangeType(HDRenderQueue.RenderQueueType.Opaque, drawOrder, false));
            }
        }

        public string renderTypeTag => HDRenderTypeTags.Opaque.ToString();

        public ConditionalField[] GetConditionalFields(PassDescriptor pass)
        {
            return new ConditionalField[]
            {
                // Features
                new ConditionalField(Fields.GraphVertex,            IsSlotConnected(PositionSlotId) ||
                                                                        IsSlotConnected(VertexNormalSlotID) ||
                                                                        IsSlotConnected(VertexTangentSlotID)),
                new ConditionalField(Fields.GraphPixel,             true),

                // Material
                new ConditionalField(HDFields.AffectsAlbedo,        affectsAlbedo.isOn),
                new ConditionalField(HDFields.AffectsNormal,        affectsNormal.isOn),
                new ConditionalField(HDFields.AffectsEmission,      affectsEmission.isOn),
                new ConditionalField(HDFields.AffectsMetal,         affectsMetal.isOn),
                new ConditionalField(HDFields.AffectsAO,            affectsAO.isOn),
                new ConditionalField(HDFields.AffectsSmoothness,    affectsSmoothness.isOn),
                new ConditionalField(HDFields.AffectsMaskMap,       affectsSmoothness.isOn || affectsMetal.isOn || affectsAO.isOn),
                new ConditionalField(HDFields.DecalDefault,         affectsAlbedo.isOn || affectsNormal.isOn || affectsMetal.isOn ||
                                                                        affectsAO.isOn || affectsSmoothness.isOn ),
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
            drawOrder.hidden = true;
            drawOrder.value = 0;
            collector.AddShaderProperty(drawOrder);

            Vector1ShaderProperty decalMeshDepthBias = new Vector1ShaderProperty();
            decalMeshDepthBias.overrideReferenceName = "_DecalMeshDepthBias";
            decalMeshDepthBias.displayName = "DecalMesh DepthBias";
            decalMeshDepthBias.hidden = true;
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
        bool m_AffectsEmission = true;

        public ToggleData affectsEmission
        {
            get { return new ToggleData(m_AffectsEmission); }
            set
            {
                if (m_AffectsEmission == value.isOn)
                    return;
                m_AffectsEmission = value.isOn;
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

        [SerializeField]
        bool m_DOTSInstancing = false;

        public ToggleData dotsInstancing
        {
            get { return new ToggleData(m_DOTSInstancing); }
            set
            {
                if (m_DOTSInstancing == value.isOn)
                    return;

                m_DOTSInstancing = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }
    }
}
