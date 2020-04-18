using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Rendering.HighDefinition.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    [Title("Master", "Fabric (HDRP)")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.FabricMasterNode")]
    [FormerName("UnityEditor.ShaderGraph.FabricMasterNode")]
    class FabricMasterNode : AbstractMaterialNode, IMasterNode, IHasSettings, ICanChangeShaderGUI, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string PositionSlotName = "Vertex Position";
        public const string PositionSlotDisplayName = "Vertex Position";
        public const int PositionSlotId = 0;

        public const string AlbedoSlotName = "Albedo";
        public const string AlbedoDisplaySlotName = "BaseColor";
        public const int AlbedoSlotId = 1;

        public const string SpecularOcclusionSlotName = "SpecularOcclusion";
        public const int SpecularOcclusionSlotId = 2;

        public const string NormalSlotName = "Normal";
        public const int NormalSlotId = 3;

        public const string SmoothnessSlotName = "Smoothness";
        public const int SmoothnessSlotId = 4;

        public const string AmbientOcclusionSlotName = "Occlusion";
        public const string AmbientOcclusionDisplaySlotName = "AmbientOcclusion";
        public const int AmbientOcclusionSlotId = 5;

        public const string SpecularColorSlotName = "Specular";
        public const string SpecularColorDisplaySlotName = "SpecularColor";
        public const int SpecularColorSlotId = 6;

        public const string DiffusionProfileHashSlotName = "DiffusionProfileHash";
        public const string DiffusionProfileHashSlotDisplayName = "Diffusion Profile";
        public const int DiffusionProfileHashSlotId = 7;

        public const string SubsurfaceMaskSlotName = "SubsurfaceMask";
        public const int SubsurfaceMaskSlotId = 8;

        public const string ThicknessSlotName = "Thickness";
        public const int ThicknessSlotId = 9;

        public const string TangentSlotName = "Tangent";
        public const int TangentSlotId = 10;

        public const string AnisotropySlotName = "Anisotropy";
        public const int AnisotropySlotId = 11;

        public const string EmissionSlotName = "Emission";
        public const int EmissionSlotId = 12;

        public const string AlphaSlotName = "Alpha";
        public const int AlphaSlotId = 13;

        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const int AlphaClipThresholdSlotId = 14;

        public const string BentNormalSlotName = "BentNormal";
        public const int BentNormalSlotId = 15;

        public const int LightingSlotId = 16;
        public const string BakedGISlotName = "BakedGI";

        public const int BackLightingSlotId = 17;
        public const string BakedBackGISlotName = "BakedBackGI";

        public const int DepthOffsetSlotId = 18;
        public const string DepthOffsetSlotName = "DepthOffset";
        public const int VertexNormalSlotId = 19;
        public const string VertexNormalSlotName = "Vertex Normal";
        public const int VertexTangentSlotId = 20;
        public const string VertexTangentSlotName = "Vertex Tangent";

        public enum MaterialType
        {
            CottonWool,
            Silk
        }

        // Don't support Multiply
        public enum AlphaModeFabric
        {
            Alpha,
            Premultiply,
            Additive,
        }

        // Just for convenience of doing simple masks. We could run out of bits of course.
        [Flags]
        enum SlotMask
        {
            None = 0,
            Position = 1 << PositionSlotId,
            Albedo = 1 << AlbedoSlotId,
            SpecularOcclusion = 1 << SpecularOcclusionSlotId,
            Normal = 1 << NormalSlotId,
            Smoothness = 1 << SmoothnessSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Specular = 1 << SpecularColorSlotId,
            DiffusionProfile = 1 << DiffusionProfileHashSlotId,
            SubsurfaceMask = 1 << SubsurfaceMaskSlotId,
            Thickness = 1 << ThicknessSlotId,
            Tangent = 1 << TangentSlotId,
            Anisotropy = 1 << AnisotropySlotId,
            Emission = 1 << EmissionSlotId,
            Alpha = 1 << AlphaSlotId,
            AlphaClipThreshold = 1 << AlphaClipThresholdSlotId,
            BentNormal = 1 << BentNormalSlotId,
            BakedGI = 1 << LightingSlotId,
            BakedBackGI = 1 << BackLightingSlotId,
            DepthOffset = 1 << DepthOffsetSlotId,
            VertexNormal = 1 << VertexNormalSlotId,
            VertexTangent = 1 << VertexTangentSlotId,
        }

        const SlotMask CottonWoolSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.SpecularOcclusion | SlotMask.Normal | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.Specular | SlotMask.DiffusionProfile | SlotMask.SubsurfaceMask | SlotMask.Thickness | SlotMask.Emission | SlotMask.Alpha | SlotMask.AlphaClipThreshold | SlotMask.BentNormal | SlotMask.BakedGI | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask SilkSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.SpecularOcclusion | SlotMask.Normal | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.Specular | SlotMask.DiffusionProfile | SlotMask.SubsurfaceMask | SlotMask.Thickness | SlotMask.Tangent | SlotMask.Anisotropy | SlotMask.Emission | SlotMask.Alpha | SlotMask.AlphaClipThreshold | SlotMask.BentNormal | SlotMask.BakedGI | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;

        // This could also be a simple array. For now, catch any mismatched data.
        SlotMask GetActiveSlotMask()
        {
            switch (materialType)
            {
                case MaterialType.CottonWool:
                    return CottonWoolSlotMask;

                case MaterialType.Silk:
                    return SilkSlotMask;

                default:
                    return SlotMask.None;
            }
        }

        bool MaterialTypeUsesSlotMask(SlotMask mask)
        {
            SlotMask activeMask = GetActiveSlotMask();
            return (activeMask & mask) != 0;
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

        [SerializeField]
        bool m_BlendPreserveSpecular = true;

        public ToggleData blendPreserveSpecular
        {
            get { return new ToggleData(m_BlendPreserveSpecular); }
            set
            {
                if (m_BlendPreserveSpecular == value.isOn)
                    return;
                m_BlendPreserveSpecular = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_TransparencyFog = true;

        public ToggleData transparencyFog
        {
            get { return new ToggleData(m_TransparencyFog); }
            set
            {
                if (m_TransparencyFog == value.isOn)
                    return;
                m_TransparencyFog = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AlphaTest;

        public ToggleData alphaTest
        {
            get { return new ToggleData(m_AlphaTest); }
            set
            {
                if (m_AlphaTest == value.isOn)
                    return;
                m_AlphaTest = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }
        
        [SerializeField]
        bool m_AlphaToMask = false;

        public ToggleData alphaToMask
        {
            get { return new ToggleData(m_AlphaToMask); }
            set
            {
                if (m_AlphaToMask == value.isOn)
                    return;
                m_AlphaToMask = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        int m_SortPriority;

        public int sortPriority
        {
            get { return m_SortPriority; }
            set
            {
                if (m_SortPriority == value)
                    return;
                m_SortPriority = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        DoubleSidedMode m_DoubleSidedMode;

        public DoubleSidedMode doubleSidedMode
        {
            get { return m_DoubleSidedMode; }
            set
            {
                if (m_DoubleSidedMode == value)
                    return;

                m_DoubleSidedMode = value;
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        MaterialType m_MaterialType;

        public MaterialType materialType
        {
            get { return m_MaterialType; }
            set
            {
                if (m_MaterialType == value)
                    return;

                m_MaterialType = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_ReceiveDecals = true;

        public ToggleData receiveDecals
        {
            get { return new ToggleData(m_ReceiveDecals); }
            set
            {
                if (m_ReceiveDecals == value.isOn)
                    return;
                m_ReceiveDecals = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_ReceivesSSR = true;
        public ToggleData receiveSSR
        {
            get { return new ToggleData(m_ReceivesSSR); }
            set
            {
                if (m_ReceivesSSR == value.isOn)
                    return;
                m_ReceivesSSR = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AddPrecomputedVelocity = false;

        public ToggleData addPrecomputedVelocity
        {
            get { return new ToggleData(m_AddPrecomputedVelocity); }
            set
            {
                if (m_AddPrecomputedVelocity == value.isOn)
                    return;
                m_AddPrecomputedVelocity = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_EnergyConservingSpecular = true;

        public ToggleData energyConservingSpecular
        {
            get { return new ToggleData(m_EnergyConservingSpecular); }
            set
            {
                if (m_EnergyConservingSpecular == value.isOn)
                    return;
                m_EnergyConservingSpecular = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_Transmission = false;

        public ToggleData transmission
        {
            get { return new ToggleData(m_Transmission); }
            set
            {
                if (m_Transmission == value.isOn)
                    return;
                m_Transmission = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_SubsurfaceScattering = false;

        public ToggleData subsurfaceScattering
        {
            get { return new ToggleData(m_SubsurfaceScattering); }
            set
            {
                if (m_SubsurfaceScattering == value.isOn)
                    return;
                m_SubsurfaceScattering = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        SpecularOcclusionMode m_SpecularOcclusionMode;

        public SpecularOcclusionMode specularOcclusionMode
        {
            get { return m_SpecularOcclusionMode; }
            set
            {
                if (m_SpecularOcclusionMode == value)
                    return;

                m_SpecularOcclusionMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_overrideBakedGI;

        public ToggleData overrideBakedGI
        {
            get { return new ToggleData(m_overrideBakedGI); }
            set
            {
                if (m_overrideBakedGI == value.isOn)
                    return;
                m_overrideBakedGI = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_depthOffset;

        public ToggleData depthOffset
        {
            get { return new ToggleData(m_depthOffset); }
            set
            {
                if (m_depthOffset == value.isOn)
                    return;
                m_depthOffset = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_ZWrite;

        public ToggleData zWrite
        {
            get { return new ToggleData(m_ZWrite); }
            set
            {
                if (m_ZWrite == value.isOn)
                    return;
                m_ZWrite = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        TransparentCullMode m_transparentCullMode = TransparentCullMode.Back;
        public TransparentCullMode transparentCullMode
        {
            get => m_transparentCullMode;
            set
            {
                if (m_transparentCullMode == value)
                    return;

                m_transparentCullMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        CompareFunction m_ZTest = CompareFunction.LessEqual;
        public CompareFunction zTest
        {
            get => m_ZTest;
            set
            {
                if (m_ZTest == value)
                    return;

                m_ZTest = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_SupportLodCrossFade;

        public ToggleData supportLodCrossFade
        {
            get { return new ToggleData(m_SupportLodCrossFade); }
            set
            {
                if (m_SupportLodCrossFade == value.isOn)
                    return;
                m_SupportLodCrossFade = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Node);
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

        [SerializeField]
        int m_MaterialNeedsUpdateHash = 0;

        int ComputeMaterialNeedsUpdateHash()
        {
            int hash = 0;

            hash |= (alphaTest.isOn ? 0 : 1) << 0;
            hash |= (receiveSSR.isOn ? 0 : 1) << 1;
            hash |= (RequiresSplitLighting() ? 0 : 1) << 2;

            return hash;
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

        public FabricMasterNode()
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
            name = "Fabric Master";

            List<int> validSlots = new List<int>();

            // Position
            if (MaterialTypeUsesSlotMask(SlotMask.Position))
            {
                AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotDisplayName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(PositionSlotId);
            }

            // Normal in Vertex
            if (MaterialTypeUsesSlotMask(SlotMask.VertexNormal))
            {
                AddSlot(new NormalMaterialSlot(VertexNormalSlotId, VertexNormalSlotName, VertexNormalSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(VertexNormalSlotId);
            }

            // tangent in Vertex
            if (MaterialTypeUsesSlotMask(SlotMask.VertexTangent))
            {
                AddSlot(new TangentMaterialSlot(VertexTangentSlotId, VertexTangentSlotName, VertexTangentSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(VertexTangentSlotId);
            }

            // Albedo
            if (MaterialTypeUsesSlotMask(SlotMask.Albedo))
            {
                AddSlot(new ColorRGBMaterialSlot(AlbedoSlotId, AlbedoDisplaySlotName, AlbedoSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(AlbedoSlotId);
            }

            // Specular Occlusion
            if (MaterialTypeUsesSlotMask(SlotMask.SpecularOcclusion) && specularOcclusionMode == SpecularOcclusionMode.Custom)
            {
                AddSlot(new Vector1MaterialSlot(SpecularOcclusionSlotId, SpecularOcclusionSlotName, SpecularOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularOcclusionSlotId);
            }

            // Normal
            if (MaterialTypeUsesSlotMask(SlotMask.Normal))
            {
                AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                validSlots.Add(NormalSlotId);
            }

            // BentNormal
            if (MaterialTypeUsesSlotMask(SlotMask.BentNormal))
            {
                AddSlot(new NormalMaterialSlot(BentNormalSlotId, BentNormalSlotName, BentNormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                validSlots.Add(BentNormalSlotId);
            }

            // Smoothness
            if (MaterialTypeUsesSlotMask(SlotMask.Smoothness))
            {
                AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(SmoothnessSlotId);
            }

            // Ambient Occlusion
            if (MaterialTypeUsesSlotMask(SlotMask.Occlusion))
            {
                AddSlot(new Vector1MaterialSlot(AmbientOcclusionSlotId, AmbientOcclusionDisplaySlotName, AmbientOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AmbientOcclusionSlotId);
            }

            // Specular Color
            if (MaterialTypeUsesSlotMask(SlotMask.Specular))
            {
                AddSlot(new ColorRGBMaterialSlot(SpecularColorSlotId, SpecularColorDisplaySlotName, SpecularColorSlotName, SlotType.Input, new Color(0.2f,0.2f,0.2f,1.0f), ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularColorSlotId);
            }

            // Diffusion Profile
            if (MaterialTypeUsesSlotMask(SlotMask.DiffusionProfile) && (subsurfaceScattering.isOn || transmission.isOn))
            {
                AddSlot(new DiffusionProfileInputMaterialSlot(DiffusionProfileHashSlotId, DiffusionProfileHashSlotDisplayName, DiffusionProfileHashSlotName, ShaderStageCapability.Fragment));
                validSlots.Add(DiffusionProfileHashSlotId);
            }

            // Subsurface mask
            if (MaterialTypeUsesSlotMask(SlotMask.SubsurfaceMask) && subsurfaceScattering.isOn)
            {
                AddSlot(new Vector1MaterialSlot(SubsurfaceMaskSlotId, SubsurfaceMaskSlotName, SubsurfaceMaskSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SubsurfaceMaskSlotId);
            }

            // Thickness
            if (MaterialTypeUsesSlotMask(SlotMask.Thickness) &&  transmission.isOn)
            {
                AddSlot(new Vector1MaterialSlot(ThicknessSlotId, ThicknessSlotName, ThicknessSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(ThicknessSlotId);
            }

            // Tangent
            if (MaterialTypeUsesSlotMask(SlotMask.Tangent))
            {
                AddSlot(new TangentMaterialSlot(TangentSlotId, TangentSlotName, TangentSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                validSlots.Add(TangentSlotId);
            }

            // Anisotropy
            if (MaterialTypeUsesSlotMask(SlotMask.Anisotropy))
            {
                AddSlot(new Vector1MaterialSlot(AnisotropySlotId, AnisotropySlotName, AnisotropySlotName, SlotType.Input, 0.8f, ShaderStageCapability.Fragment));
                validSlots.Add(AnisotropySlotId);
            }

            // Emission Normal
            if (MaterialTypeUsesSlotMask(SlotMask.Emission))
            {
                AddSlot(new ColorRGBMaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
                validSlots.Add(EmissionSlotId);
            }

            // Alpha
            if (MaterialTypeUsesSlotMask(SlotMask.Alpha))
            {
                AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaSlotId);
            }

            // Alpha threshold
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaClipThreshold) && alphaTest.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaClipThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaClipThresholdSlotId);
            }

            if (MaterialTypeUsesSlotMask(SlotMask.BakedGI) && overrideBakedGI.isOn)
            {
                AddSlot(new DefaultMaterialSlot(LightingSlotId, BakedGISlotName, BakedGISlotName, ShaderStageCapability.Fragment));
                validSlots.Add(LightingSlotId);
                AddSlot(new DefaultMaterialSlot(BackLightingSlotId, BakedBackGISlotName, BakedBackGISlotName, ShaderStageCapability.Fragment));
                validSlots.Add(BackLightingSlotId);
            }

            if (depthOffset.isOn)
            {
                AddSlot(new Vector1MaterialSlot(DepthOffsetSlotId, DepthOffsetSlotName, DepthOffsetSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(DepthOffsetSlotId);
            }

            RemoveSlotsNameNotMatching(validSlots, true);
        }

        public VisualElement CreateSettingsElement()
        {
            return new FabricSettingsView(this);
        }

        public string renderQueueTag
        {
            get
            {
                var renderingPass = surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, sortPriority, alphaTest.isOn);
                return HDRenderQueue.GetShaderTagValue(queue);
            }
        }

        public string renderTypeTag => HDRenderTypeTags.HDLitShader.ToString();

        public ConditionalField[] GetConditionalFields(PassDescriptor pass)
        {
            var ambientOcclusionSlot = FindSlot<Vector1MaterialSlot>(AmbientOcclusionSlotId);

            return new ConditionalField[]
            {
                // Features
                new ConditionalField(Fields.GraphVertex,                            IsSlotConnected(PositionSlotId) ||
                                                                                        IsSlotConnected(VertexNormalSlotId) ||
                                                                                        IsSlotConnected(VertexTangentSlotId)),
                new ConditionalField(Fields.GraphPixel,                             true),
                new ConditionalField(Fields.LodCrossFade,                           supportLodCrossFade.isOn),

                // Surface Type
                new ConditionalField(Fields.SurfaceOpaque,                          surfaceType == SurfaceType.Opaque),
                new ConditionalField(Fields.SurfaceTransparent,                     surfaceType != SurfaceType.Opaque),

                // Structs
                new ConditionalField(HDStructFields.FragInputs.IsFrontFace,doubleSidedMode != DoubleSidedMode.Disabled &&
                                                                                        !pass.Equals(HDFabricSubTarget.FabricPasses.MotionVectors)),
                // Material
                new ConditionalField(HDFields.CottonWool,                           materialType == MaterialType.CottonWool),
                new ConditionalField(HDFields.Silk,                                 materialType == MaterialType.Silk),
                new ConditionalField(HDFields.SubsurfaceScattering,                 subsurfaceScattering.isOn && surfaceType != SurfaceType.Transparent),
                new ConditionalField(HDFields.Transmission,                         transmission.isOn),

                // Specular Occlusion
                new ConditionalField(HDFields.SpecularOcclusionFromAO,              specularOcclusionMode == SpecularOcclusionMode.FromAO),
                new ConditionalField(HDFields.SpecularOcclusionFromAOBentNormal,    specularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal),
                new ConditionalField(HDFields.SpecularOcclusionCustom,              specularOcclusionMode == SpecularOcclusionMode.Custom),

                // Misc
                new ConditionalField(Fields.AlphaTest,                              alphaTest.isOn && pass.pixelPorts.Contains(AlphaClipThresholdSlotId)),
                new ConditionalField(HDFields.DoAlphaTest,                          alphaTest.isOn && pass.pixelPorts.Contains(AlphaClipThresholdSlotId)),
                new ConditionalField(Fields.AlphaToMask,                            alphaTest.isOn && pass.pixelPorts.Contains(AlphaClipThresholdSlotId) && alphaToMask.isOn),
                new ConditionalField(HDFields.AlphaFog,                             surfaceType != SurfaceType.Opaque && transparencyFog.isOn),
                new ConditionalField(HDFields.BlendPreserveSpecular,                surfaceType != SurfaceType.Opaque && blendPreserveSpecular.isOn),
                new ConditionalField(HDFields.DisableDecals,                        !receiveDecals.isOn),
                new ConditionalField(HDFields.DisableSSR,                           !receiveSSR.isOn),
                new ConditionalField(Fields.VelocityPrecomputed,                    addPrecomputedVelocity.isOn),
                new ConditionalField(HDFields.BentNormal,                           IsSlotConnected(BentNormalSlotId) &&
                                                                                        pass.pixelPorts.Contains(BentNormalSlotId)),
                new ConditionalField(HDFields.AmbientOcclusion,                     pass.pixelPorts.Contains(AmbientOcclusionSlotId) &&
                                                                                        (IsSlotConnected(AmbientOcclusionSlotId) ||
                                                                                        ambientOcclusionSlot.value != ambientOcclusionSlot.defaultValue)),
                new ConditionalField(HDFields.Tangent,                              IsSlotConnected(TangentSlotId) &&
                                                                                        pass.pixelPorts.Contains(TangentSlotId)),
                new ConditionalField(HDFields.LightingGI,                           IsSlotConnected(LightingSlotId) &&
                                                                                        pass.pixelPorts.Contains(LightingSlotId)),
                new ConditionalField(HDFields.BackLightingGI,                       IsSlotConnected(BackLightingSlotId) &&
                                                                                        pass.pixelPorts.Contains(BackLightingSlotId)),
                new ConditionalField(HDFields.DepthOffset,                          depthOffset.isOn && pass.pixelPorts.Contains(DepthOffsetSlotId)),
                new ConditionalField(HDFields.EnergyConservingSpecular,             energyConservingSpecular.isOn),

            };
        }

        public void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)(SurfaceType)surfaceType);
            material.SetFloat(kDoubleSidedNormalMode, (int)doubleSidedMode);
            material.SetFloat(kDoubleSidedEnable, doubleSidedMode != DoubleSidedMode.Disabled ? 1.0f : 0.0f);
            material.SetFloat(kAlphaCutoffEnabled, alphaTest.isOn ? 1 : 0);
            material.SetFloat(kBlendMode, (int)HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode));
            material.SetFloat(kEnableFogOnTransparent, transparencyFog.isOn ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)zTest);
            material.SetFloat(kTransparentCullMode, (int)transparentCullMode);
            material.SetFloat(kZWrite, zWrite.isOn ? 1.0f : 0.0f);
            // No sorting priority for shader graph preview
            var renderingPass = surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            material.renderQueue = (int)HDRenderQueue.ChangeType(renderingPass, offset: 0, alphaTest: alphaTest.isOn);

            FabricGUI.SetupMaterialKeywordsAndPass(material);
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

        public bool RequiresSplitLighting()
        {
            return subsurfaceScattering.isOn;
        }
        public override object saveContext
        {
            get
            {
                int hash = ComputeMaterialNeedsUpdateHash();

                bool needsUpdate = hash != m_MaterialNeedsUpdateHash;

                if (needsUpdate)
                    m_MaterialNeedsUpdateHash = hash;

                return new HDSaveContext{ updateMaterials = needsUpdate };
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // Trunk currently relies on checking material property "_EmissionColor" to allow emissive GI. If it doesn't find that property, or it is black, GI is forced off.
            // ShaderGraph doesn't use this property, so currently it inserts a dummy color (white). This dummy color may be removed entirely once the following PR has been merged in trunk: Pull request #74105
            // The user will then need to explicitly disable emissive GI if it is not needed.
            // To be able to automatically disable emission based on the ShaderGraph config when emission is black,
            // we will need a more general way to communicate this to the engine (not directly tied to a material property).
            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = "_EmissionColor",
                hidden = true,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });

            //See SG-ADDITIONALVELOCITY-NOTE
            if (addPrecomputedVelocity.isOn)
            {
                collector.AddShaderProperty(new BooleanShaderProperty
                {
                    value = true,
                    hidden = true,
                    overrideReferenceName = kAddPrecomputedVelocity,
                });
            }

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, RequiresSplitLighting(), receiveSSR.isOn);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                surfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode),
                sortPriority,
                alphaToMask.isOn,
                zWrite.isOn,
                transparentCullMode,
                zTest,
                false,
                transparencyFog.isOn
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, alphaTest.isOn, false);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, doubleSidedMode);

            base.CollectShaderProperties(collector, generationMode);
        }
    }
}
