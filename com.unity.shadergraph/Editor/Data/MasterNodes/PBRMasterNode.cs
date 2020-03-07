using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master", "PBR")]
    class PBRMasterNode : AbstractMaterialNode, IMasterNode, IHasSettings, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string AlbedoSlotName = "Albedo";
        public const string NormalSlotName = "Normal";
        public const string EmissionSlotName = "Emission";
        public const string MetallicSlotName = "Metallic";
        public const string SpecularSlotName = "Specular";
        public const string SmoothnessSlotName = "Smoothness";
        public const string OcclusionSlotName = "Occlusion";
        public const string AlphaSlotName = "Alpha";
        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const string PositionName = "Vertex Position";
        public const string NormalName = "Vertex Normal";
        public const string TangentName = "Vertex Tangent";

        public const int AlbedoSlotId = 0;
        public const int NormalSlotId = 1;
        public const int MetallicSlotId = 2;
        public const int SpecularSlotId = 3;
        public const int EmissionSlotId = 4;
        public const int SmoothnessSlotId = 5;
        public const int OcclusionSlotId = 6;
        public const int AlphaSlotId = 7;
        public const int AlphaThresholdSlotId = 8;
        public const int PositionSlotId = 9;
        public const int VertNormalSlotId = 10;
        public const int VertTangentSlotId = 11;

        public enum Model
        {
            Specular,
            Metallic
        }

        [SerializeField]
        Model m_Model = Model.Metallic;

        public Model model
        {
            get { return m_Model; }
            set
            {
                if (m_Model == value)
                    return;

                m_Model = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

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

        [SerializeField]
        bool m_TwoSided;

        public ToggleData twoSided
        {
            get { return new ToggleData(m_TwoSided); }
            set
            {
                if (m_TwoSided == value.isOn)
                    return;
                m_TwoSided = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace;
        public NormalDropOffSpace normalDropOffSpace
        {
            get { return m_NormalDropOffSpace; }
            set
            {
                if (m_NormalDropOffSpace == value)
                    return;

                m_NormalDropOffSpace = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
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

        public PBRMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "PBR Master";
            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionName, PositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new NormalMaterialSlot(VertNormalSlotId, NormalName, NormalName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new TangentMaterialSlot(VertTangentSlotId, TangentName, TangentName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new ColorRGBMaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
            //switch drop off delivery space for normal values
            var coordSpace = CoordinateSpace.Tangent;
            switch (m_NormalDropOffSpace)
            {
                case NormalDropOffSpace.Tangent:
                    coordSpace = CoordinateSpace.Tangent;
                    break;
                case NormalDropOffSpace.World:
                    coordSpace = CoordinateSpace.World;
                    break;
                case NormalDropOffSpace.Object:
                    coordSpace = CoordinateSpace.Object;
                    break;
            }
            RemoveSlot(NormalSlotId);
            AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, coordSpace, ShaderStageCapability.Fragment));
            AddSlot(new ColorRGBMaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, Color.black, ColorMode.Default, ShaderStageCapability.Fragment));
            if (model == Model.Metallic)
                AddSlot(new Vector1MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            else
                AddSlot(new ColorRGBMaterialSlot(SpecularSlotId, SpecularSlotName, SpecularSlotName, SlotType.Input, Color.grey, ColorMode.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, 1f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
            {
                PositionSlotId,
                VertNormalSlotId,
                VertTangentSlotId,
                AlbedoSlotId,
                NormalSlotId,
                EmissionSlotId,
                model == Model.Metallic ? MetallicSlotId : SpecularSlotId,
                SmoothnessSlotId,
                OcclusionSlotId,
                AlphaSlotId,
                AlphaThresholdSlotId
            }, true);
        }

        public VisualElement CreateSettingsElement()
        {
            return new PBRSettingsView(this);
        }

        public string renderQueueTag
        {
            get
            {
                if(surfaceType == SurfaceType.Transparent)
                    return $"{RenderQueue.Transparent}";
                else if(IsSlotConnected(UnlitMasterNode.AlphaThresholdSlotId) || FindSlot<Vector1MaterialSlot>(AlphaThresholdSlotId).value > 0.0f)
                    return $"{RenderQueue.AlphaTest}";
                else
                    return $"{RenderQueue.Geometry}";
            }
        }

        public string renderTypeTag
        {
            get
            {
                if(surfaceType == SurfaceType.Transparent)
                    return $"{RenderType.Transparent}";
                else
                    return $"{RenderType.Opaque}";
            }
        }

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
                new ConditionalField(Fields.SurfaceOpaque,       surfaceType == ShaderGraph.SurfaceType.Opaque),
                new ConditionalField(Fields.SurfaceTransparent,  surfaceType != ShaderGraph.SurfaceType.Opaque),
                
                // Blend Mode
                new ConditionalField(Fields.BlendAdd,            surfaceType != ShaderGraph.SurfaceType.Opaque && alphaMode == AlphaMode.Additive),
                new ConditionalField(Fields.BlendAlpha,          surfaceType != ShaderGraph.SurfaceType.Opaque && alphaMode == AlphaMode.Alpha),
                new ConditionalField(Fields.BlendMultiply,       surfaceType != ShaderGraph.SurfaceType.Opaque && alphaMode == AlphaMode.Multiply),
                new ConditionalField(Fields.BlendPremultiply,    surfaceType != ShaderGraph.SurfaceType.Opaque && alphaMode == AlphaMode.Premultiply),

                // Normal Drop Off Space
                new ConditionalField(Fields.NormalDropOffOS,     normalDropOffSpace == NormalDropOffSpace.Object),
                new ConditionalField(Fields.NormalDropOffTS,     normalDropOffSpace == NormalDropOffSpace.Tangent),
                new ConditionalField(Fields.NormalDropOffWS,     normalDropOffSpace == NormalDropOffSpace.World),

                // Misc
                new ConditionalField(Fields.AlphaClip,           IsSlotConnected(UnlitMasterNode.AlphaThresholdSlotId) ||
                                                                        FindSlot<Vector1MaterialSlot>(AlphaThresholdSlotId).value > 0.0f),
                new ConditionalField(Fields.AlphaTest,           IsSlotConnected(UnlitMasterNode.AlphaThresholdSlotId) ||
                                                                        FindSlot<Vector1MaterialSlot>(AlphaThresholdSlotId).value > 0.0f),
                new ConditionalField(Fields.SpecularSetup,       model == PBRMasterNode.Model.Specular),
                new ConditionalField(Fields.Normal,              IsSlotConnected(PBRMasterNode.NormalSlotId)),
                new ConditionalField(Fields.DoubleSided,         twoSided.isOn),
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
