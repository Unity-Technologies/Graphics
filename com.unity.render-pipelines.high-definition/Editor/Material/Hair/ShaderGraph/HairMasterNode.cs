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

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    [Title("Master", "Hair (HDRP)")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HairMasterNode")]
    class HairMasterNode : MaterialMasterNode<IHairSubShader>, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string PositionSlotName = "Vertex Position";
        public const string PositionSlotDisplayName = "Vertex Position";
        public const int PositionSlotId = 0;

        public const string AlbedoSlotName = "Albedo";
        public const string AlbedoDisplaySlotName = "DiffuseColor";
        public const int AlbedoSlotId = 1;

        public const string NormalSlotName = "Normal";
        public const int NormalSlotId = 2;

        public const string SpecularOcclusionSlotName = "SpecularOcclusion";
        public const int SpecularOcclusionSlotId = 3;

        public const string BentNormalSlotName = "BentNormal";
        public const int BentNormalSlotId = 4;

        public const string HairStrandDirectionSlotName = "HairStrandDirection";
        public const int HairStrandDirectionSlotId = 5;

        public const int UnusedSlot6 = 6;

        public const string TransmittanceSlotName = "Transmittance";
        public const int TransmittanceSlotId = 7;

        public const string RimTransmissionIntensitySlotName = "RimTransmissionIntensity";
        public const int RimTransmissionIntensitySlotId = 8;

        public const string SmoothnessSlotName = "Smoothness";
        public const int SmoothnessSlotId = 9;

        public const string AmbientOcclusionSlotName = "Occlusion";
        public const string AmbientOcclusionDisplaySlotName = "AmbientOcclusion";
        public const int AmbientOcclusionSlotId = 10;

        public const string EmissionSlotName = "Emission";
        public const int EmissionSlotId = 11;

        public const string AlphaSlotName = "Alpha";
        public const int AlphaSlotId = 12;

        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const int AlphaClipThresholdSlotId = 13;

        public const string AlphaClipThresholdDepthPrepassSlotName = "AlphaClipThresholdDepthPrepass";
        public const int AlphaClipThresholdDepthPrepassSlotId = 14;

        public const string AlphaClipThresholdDepthPostpassSlotName = "AlphaClipThresholdDepthPostpass";
        public const int AlphaClipThresholdDepthPostpassSlotId = 15;

        public const string SpecularAAScreenSpaceVarianceSlotName = "SpecularAAScreenSpaceVariance";
        public const int SpecularAAScreenSpaceVarianceSlotId = 16;

        public const string SpecularAAThresholdSlotName = "SpecularAAThreshold";
        public const int SpecularAAThresholdSlotId = 17;

        //Hair Specific
        public const string SpecularTintSlotName = "SpecularTint";
        public const int SpecularTintSlotId = 18;

        public const string SpecularShiftSlotName = "SpecularShift";
        public const int SpecularShiftSlotId = 19;

		public const string SecondarySpecularTintSlotName = "SecondarySpecularTint";
        public const int SecondarySpecularTintSlotId = 20;

        public const string SecondarySmoothnessSlotName = "SecondarySmoothness";
        public const int SecondarySmoothnessSlotId = 21;

        public const string SecondarySpecularShiftSlotName = "SecondarySpecularShift";
        public const int SecondarySpecularShiftSlotId = 22;

        public const string AlphaClipThresholdShadowSlotName = "AlphaClipThresholdShadow";
        public const int AlphaClipThresholdShadowSlotId = 23;

        public const string BakedGISlotName = "BakedGI";
        public const int LightingSlotId = 24;

        public const string BakedBackGISlotName = "BakedBackGI";
        public const int BackLightingSlotId = 25;

        public const string DepthOffsetSlotName = "DepthOffset";
        public const int DepthOffsetSlotId = 26;

        public const int VertexNormalSlotId = 27;
        public const string VertexNormalSlotName = "Vertex Normal";

        public const int VertexTangentSlotId = 28;
        public const string VertexTangentSlotName = "Vertex Tangent";

        public enum MaterialType
        {
            KajiyaKay
        }

        // Don't support Multiply
        public enum AlphaModeLit
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
            Normal = 1 << NormalSlotId,
            SpecularOcclusion = 1 << SpecularOcclusionSlotId,
            BentNormal = 1 << BentNormalSlotId,
            HairStrandDirection = 1 << HairStrandDirectionSlotId,
            Slot6 = 1 << UnusedSlot6,
            Transmittance = 1 << TransmittanceSlotId,
            RimTransmissionIntensity = 1 << RimTransmissionIntensitySlotId,
            Smoothness = 1 << SmoothnessSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Emission = 1 << EmissionSlotId,
            Alpha = 1 << AlphaSlotId,
            AlphaClipThreshold = 1 << AlphaClipThresholdSlotId,
            AlphaClipThresholdDepthPrepass = 1 << AlphaClipThresholdDepthPrepassSlotId,
            AlphaClipThresholdDepthPostpass = 1 << AlphaClipThresholdDepthPostpassSlotId,
            SpecularTint = 1 << SpecularTintSlotId,
            SpecularShift = 1 << SpecularShiftSlotId,
            SecondarySpecularTint = 1 << SecondarySpecularTintSlotId,
            SecondarySmoothness = 1 << SecondarySmoothnessSlotId,
            SecondarySpecularShift = 1 << SecondarySpecularShiftSlotId,
            AlphaClipThresholdShadow = 1 << AlphaClipThresholdShadowSlotId,
            BakedGI = 1 << LightingSlotId,
            BakedBackGI = 1 << BackLightingSlotId,
            DepthOffset = 1 << DepthOffsetSlotId,
            VertexNormal = 1 << VertexNormalSlotId,
            VertexTangent = 1 << VertexTangentSlotId,
        }

        const SlotMask KajiyaKaySlotMask = SlotMask.Position | SlotMask.VertexNormal | SlotMask.VertexTangent | SlotMask.Albedo | SlotMask.Normal | SlotMask.SpecularOcclusion | SlotMask.BentNormal | SlotMask.HairStrandDirection | SlotMask.Slot6
                                            | SlotMask.Transmittance | SlotMask.RimTransmissionIntensity | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.Alpha | SlotMask.AlphaClipThreshold | SlotMask.AlphaClipThresholdDepthPrepass
                                                | SlotMask.AlphaClipThresholdDepthPostpass | SlotMask.SpecularTint | SlotMask.SpecularShift | SlotMask.SecondarySpecularTint | SlotMask.SecondarySmoothness | SlotMask.SecondarySpecularShift | SlotMask.AlphaClipThresholdShadow | SlotMask.BakedGI | SlotMask.DepthOffset;

        // This could also be a simple array. For now, catch any mismatched data.
        SlotMask GetActiveSlotMask()
        {
            switch (materialType)
            {
                case MaterialType.KajiyaKay:
                    return KajiyaKaySlotMask;
                default:
                    return KajiyaKaySlotMask;
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
        bool m_AlphaTestDepthPrepass;

        public ToggleData alphaTestDepthPrepass
        {
            get { return new ToggleData(m_AlphaTestDepthPrepass); }
            set
            {
                if (m_AlphaTestDepthPrepass == value.isOn)
                    return;
                m_AlphaTestDepthPrepass = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_AlphaTestDepthPostpass;

        public ToggleData alphaTestDepthPostpass
        {
            get { return new ToggleData(m_AlphaTestDepthPostpass); }
            set
            {
                if (m_AlphaTestDepthPostpass == value.isOn)
                    return;
                m_AlphaTestDepthPostpass = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_TransparentWritesMotionVec;

        public ToggleData transparentWritesMotionVec
        {
            get { return new ToggleData(m_TransparentWritesMotionVec); }
            set
            {
                if (m_TransparentWritesMotionVec == value.isOn)
                    return;
                m_TransparentWritesMotionVec = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_AlphaTestShadow;

        public ToggleData alphaTestShadow
        {
            get { return new ToggleData(m_AlphaTestShadow); }
            set
            {
                if (m_AlphaTestShadow == value.isOn)
                    return;
                m_AlphaTestShadow = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_BackThenFrontRendering;

        public ToggleData backThenFrontRendering
        {
            get { return new ToggleData(m_BackThenFrontRendering); }
            set
            {
                if (m_BackThenFrontRendering == value.isOn)
                    return;
                m_BackThenFrontRendering = value.isOn;
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
        bool m_UseLightFacingNormal = false;
        public ToggleData useLightFacingNormal
        {
            get { return new ToggleData(m_UseLightFacingNormal); }
            set
            {
                if (m_UseLightFacingNormal == value.isOn)
                    return;
                m_UseLightFacingNormal = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_SpecularAA;

        public ToggleData specularAA
        {
            get { return new ToggleData(m_SpecularAA); }
            set
            {
                if (m_SpecularAA == value.isOn)
                    return;
                m_SpecularAA = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        float m_SpecularAAScreenSpaceVariance;

        public float specularAAScreenSpaceVariance
        {
            get { return m_SpecularAAScreenSpaceVariance; }
            set
            {
                if (m_SpecularAAScreenSpaceVariance == value)
                    return;
                m_SpecularAAScreenSpaceVariance = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        float m_SpecularAAThreshold;

        public float specularAAThreshold
        {
            get { return m_SpecularAAThreshold; }
            set
            {
                if (m_SpecularAAThreshold == value)
                    return;
                m_SpecularAAThreshold = value;
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
                Dirty(ModificationScope.Graph);
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
        int m_MaterialNeedsUpdateHash = 0;

        int ComputeMaterialNeedsUpdateHash()
        {
            int hash = 0;

            hash |= (alphaTest.isOn ? 0 : 1) << 0;
            hash |= (alphaTestShadow.isOn ? 0 : 1) << 1;
            hash |= (receiveSSR.isOn ? 0 : 1) << 2;

            return hash;
        }

        public HairMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("Master-Node-Hair");

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Hair Master";

            List<int> validSlots = new List<int>();

            if (MaterialTypeUsesSlotMask(SlotMask.Position))
            {
                AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotDisplayName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(PositionSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.VertexNormal))
            {
                AddSlot(new NormalMaterialSlot(VertexNormalSlotId, VertexNormalSlotName, VertexNormalSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(VertexNormalSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.VertexTangent))
            {
                AddSlot(new TangentMaterialSlot(VertexTangentSlotId, VertexTangentSlotName, VertexTangentSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(VertexTangentSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Albedo))
            {
                AddSlot(new ColorRGBMaterialSlot(AlbedoSlotId, AlbedoDisplaySlotName, AlbedoSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(AlbedoSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.SpecularOcclusion) && specularOcclusionMode == SpecularOcclusionMode.Custom)
            {
                AddSlot(new Vector1MaterialSlot(SpecularOcclusionSlotId, SpecularOcclusionSlotName, SpecularOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularOcclusionSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Normal))
            {
                AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                validSlots.Add(NormalSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.BentNormal))
            {
                AddSlot(new NormalMaterialSlot(BentNormalSlotId, BentNormalSlotName, BentNormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                validSlots.Add(BentNormalSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Smoothness))
            {
                AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(SmoothnessSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Occlusion))
            {
                AddSlot(new Vector1MaterialSlot(AmbientOcclusionSlotId, AmbientOcclusionDisplaySlotName, AmbientOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AmbientOcclusionSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Transmittance))
            {
                AddSlot(new Vector3MaterialSlot(TransmittanceSlotId, TransmittanceSlotName, TransmittanceSlotName, SlotType.Input, 0.3f * new Vector3(1.0f, 0.65f, 0.3f), ShaderStageCapability.Fragment));
                validSlots.Add(TransmittanceSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.RimTransmissionIntensity))
            {
                AddSlot(new Vector1MaterialSlot(RimTransmissionIntensitySlotId, RimTransmissionIntensitySlotName, RimTransmissionIntensitySlotName, SlotType.Input, 0.2f, ShaderStageCapability.Fragment));
                validSlots.Add(RimTransmissionIntensitySlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.HairStrandDirection))
            {
                AddSlot(new Vector3MaterialSlot(HairStrandDirectionSlotId, HairStrandDirectionSlotName, HairStrandDirectionSlotName, SlotType.Input, new Vector3(0, -1, 0), ShaderStageCapability.Fragment));
                validSlots.Add(HairStrandDirectionSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Emission))
            {
                AddSlot(new ColorRGBMaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
                validSlots.Add(EmissionSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Alpha))
            {
                AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaClipThreshold) && alphaTest.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaClipThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaClipThresholdSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaClipThresholdDepthPrepass) && surfaceType == SurfaceType.Transparent && alphaTest.isOn && alphaTestDepthPrepass.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaClipThresholdDepthPrepassSlotId, AlphaClipThresholdDepthPrepassSlotName, AlphaClipThresholdDepthPrepassSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaClipThresholdDepthPrepassSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaClipThresholdDepthPostpass) && surfaceType == SurfaceType.Transparent && alphaTest.isOn && alphaTestDepthPostpass.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaClipThresholdDepthPostpassSlotId, AlphaClipThresholdDepthPostpassSlotName, AlphaClipThresholdDepthPostpassSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaClipThresholdDepthPostpassSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaClipThresholdShadow) && alphaTest.isOn && alphaTestShadow.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaClipThresholdShadowSlotId, AlphaClipThresholdShadowSlotName, AlphaClipThresholdShadowSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaClipThresholdShadowSlotId);
            }
            if (specularAA.isOn)
            {
                AddSlot(new Vector1MaterialSlot(SpecularAAScreenSpaceVarianceSlotId, SpecularAAScreenSpaceVarianceSlotName, SpecularAAScreenSpaceVarianceSlotName, SlotType.Input, 0.1f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularAAScreenSpaceVarianceSlotId);

                AddSlot(new Vector1MaterialSlot(SpecularAAThresholdSlotId, SpecularAAThresholdSlotName, SpecularAAThresholdSlotName, SlotType.Input, 0.2f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularAAThresholdSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.SpecularTint))
            {
                AddSlot(new ColorRGBMaterialSlot(SpecularTintSlotId, SpecularTintSlotName, SpecularTintSlotName, SlotType.Input, Color.white, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularTintSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.SpecularShift))
            {
                AddSlot(new Vector1MaterialSlot(SpecularShiftSlotId, SpecularShiftSlotName, SpecularShiftSlotName, SlotType.Input, 0.1f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularShiftSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.SecondarySpecularTint))
            {
                AddSlot(new ColorRGBMaterialSlot(SecondarySpecularTintSlotId, SecondarySpecularTintSlotName, SecondarySpecularTintSlotName, SlotType.Input, Color.grey, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(SecondarySpecularTintSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.SecondarySmoothness))
            {
                AddSlot(new Vector1MaterialSlot(SecondarySmoothnessSlotId, SecondarySmoothnessSlotName, SecondarySmoothnessSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(SecondarySmoothnessSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.SecondarySpecularShift))
            {
                AddSlot(new Vector1MaterialSlot(SecondarySpecularShiftSlotId, SecondarySpecularShiftSlotName, SecondarySpecularShiftSlotName, SlotType.Input, -0.1f, ShaderStageCapability.Fragment));
                validSlots.Add(SecondarySpecularShiftSlotId);
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

        protected override VisualElement CreateCommonSettingsElement()
        {
            return new HairSettingsView(this);
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

        public override void ProcessPreviewMaterial(Material previewMaterial)
        {
            // Fixup the material settings:
            previewMaterial.SetFloat(kSurfaceType, (int)(SurfaceType)surfaceType);
            previewMaterial.SetFloat(kDoubleSidedNormalMode, (int)doubleSidedMode);
            previewMaterial.SetFloat(kDoubleSidedEnable, doubleSidedMode != DoubleSidedMode.Disabled ? 1.0f : 0.0f);
            previewMaterial.SetFloat(kAlphaCutoffEnabled, alphaTest.isOn ? 1 : 0);
            previewMaterial.SetFloat(kBlendMode, (int)HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode));
            previewMaterial.SetFloat(kEnableFogOnTransparent, transparencyFog.isOn ? 1.0f : 0.0f);
            previewMaterial.SetFloat(kZTestTransparent, (int)zTest);
            previewMaterial.SetFloat(kTransparentCullMode, (int)transparentCullMode);
            previewMaterial.SetFloat(kZWrite, zWrite.isOn ? 1.0f : 0.0f);
            // No sorting priority for shader graph preview
            var renderingPass = surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            previewMaterial.renderQueue = (int)HDRenderQueue.ChangeType(renderingPass, offset: 0, alphaTest: alphaTest.isOn);

            HairGUI.SetupMaterialKeywordsAndPass(previewMaterial);
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
            HDSubShaderUtilities.AddStencilShaderProperties(collector, false, receiveSSR.isOn);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                surfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode),
                sortPriority,
                zWrite.isOn,
                transparentCullMode,
                zTest,
                backThenFrontRendering.isOn,
                transparencyFog.isOn
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, alphaTest.isOn, alphaTestShadow.isOn);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, doubleSidedMode);
            HDSubShaderUtilities.AddPrePostPassProperties(collector, alphaTestDepthPrepass.isOn, alphaTestDepthPostpass.isOn);

            base.CollectShaderProperties(collector, generationMode);
        }
    }
}
