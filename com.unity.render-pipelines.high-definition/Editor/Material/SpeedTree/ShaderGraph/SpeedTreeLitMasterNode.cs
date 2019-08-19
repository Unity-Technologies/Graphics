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

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    [Title("Master", "HDRP/SpeedTreeLit")]
    class SpeedTreeLitMasterNode : MasterNode<ISpeedTreeLitSubShader>, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string PositionSlotName = "Position";
        public const string AlbedoSlotName = "Albedo";
        public const string AlbedoDisplaySlotName = "BaseColor";
        public const string NormalSlotName = "Normal";
        public const string BentNormalSlotName = "BentNormal";
        public const string TangentSlotName = "Tangent";
        public const string SubsurfaceMaskSlotName = "SubsurfaceMask";
        public const string ThicknessSlotName = "Thickness";
        public const string DiffusionProfileHashSlotName = "DiffusionProfileHash";
        public const string IridescenceMaskSlotName = "IridescenceMask";
        public const string IridescenceThicknessSlotName = "IridescenceThickness";
        public const string SpecularColorSlotName = "Specular";
        public const string SpecularColorDisplaySlotName = "SpecularColor";
        public const string CoatMaskSlotName = "CoatMask";
        public const string EmissionSlotName = "Emission";
        public const string MetallicSlotName = "Metallic";
        public const string SmoothnessSlotName = "Smoothness";
        public const string AmbientOcclusionSlotName = "Occlusion";
        public const string AmbientOcclusionDisplaySlotName = "AmbientOcclusion";
        public const string AlphaSlotName = "Alpha";
        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const string AlphaClipThresholdDepthPrepassSlotName = "AlphaClipThresholdDepthPrepass";
        public const string AlphaClipThresholdDepthPostpassSlotName = "AlphaClipThresholdDepthPostpass";
        public const string AnisotropySlotName = "Anisotropy";
        public const string SpecularAAScreenSpaceVarianceSlotName = "SpecularAAScreenSpaceVariance";
        public const string SpecularAAThresholdSlotName = "SpecularAAThreshold";
        public const string RefractionIndexSlotName = "RefractionIndex";
        public const string RefractionColorSlotName = "RefractionColor";
        public const string RefractionDistanceSlotName = "RefractionDistance";
        public const string DistortionSlotName = "Distortion";
        public const string DistortionBlurSlotName = "DistortionBlur";
        public const string SpecularOcclusionSlotName = "SpecularOcclusion";
        public const string AlphaClipThresholdShadowSlotName = "AlphaClipThresholdShadow";
        public const string BakedGISlotName = "Baked GI";
        public const string BakedBackGISlotName = "Baked Back GI";
        public const string DepthOffsetSlotName = "DepthOffset";

        public const int PositionSlotId = 0;
        public const int AlbedoSlotId = 1;
        public const int NormalSlotId = 2;
        public const int BentNormalSlotId = 3;
        public const int TangentSlotId = 4;
        public const int SubsurfaceMaskSlotId = 5;
        public const int ThicknessSlotId = 6;
        public const int DiffusionProfileHashSlotId = 7;
        public const int IridescenceMaskSlotId = 8;
        public const int IridescenceThicknessSlotId = 9;
        public const int SpecularColorSlotId = 10;
        public const int CoatMaskSlotId = 11;
        public const int MetallicSlotId = 12;
        public const int EmissionSlotId = 13;
        public const int SmoothnessSlotId = 14;
        public const int AmbientOcclusionSlotId = 15;
        public const int AlphaSlotId = 16;
        public const int AlphaThresholdSlotId = 17;
        public const int AlphaThresholdDepthPrepassSlotId = 18;
        public const int AlphaThresholdDepthPostpassSlotId = 19;
        public const int AnisotropySlotId = 20;
        public const int SpecularAAScreenSpaceVarianceSlotId = 21;
        public const int SpecularAAThresholdSlotId = 22;
        public const int RefractionIndexSlotId = 23;
        public const int RefractionColorSlotId = 24;
        public const int RefractionDistanceSlotId = 25;
        public const int DistortionSlotId = 26;
        public const int DistortionBlurSlotId = 27;
        public const int SpecularOcclusionSlotId = 28;
        public const int AlphaThresholdShadowSlotId = 29;
        public const int LightingSlotId = 30;
        public const int BackLightingSlotId = 31;
        public const int DepthOffsetSlotId = 32;

        // And here are the add-ons that are specific to SpeedTree
        public const string HueVariationColorSlotName = "HueVariationColor";
        public const int HueVariationColorSlotId = 33;
        public const string WindQualitySlotName = "WindQuality";
        public const int WindQualitySlotId = 34;
        public const string DepthBiasSlotName = "DepthBias";
        public const int DepthBiasSlotId = 35;

        // SpeedTree modal info
        public enum SpeedTreeVersion
        {
            SpeedTree7,
            SpeedTree8,
        }

        public enum TreeGeomType
        {
            Branch,
            BranchDetail,
            Frond,
            Leaf,
            Mesh,
        }

        public enum WindQuality
        {
            None = 0,
            Fastest,
            Fast,
            Better,
            Best,
            Palm,
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
        HDRenderQueue.RenderQueueType m_RenderingPass = HDRenderQueue.RenderQueueType.Opaque;

        public HDRenderQueue.RenderQueueType renderingPass
        {
            get { return m_RenderingPass; }
            set
            {
                if (m_RenderingPass == value)
                    return;

                m_RenderingPass = value;
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

        [SerializeField]
        bool m_ZWrite = false;
        public ToggleData zWrite
        {
            get { return new ToggleData(m_ZWrite); }
            set
            {
                if (m_ZWrite == value.isOn)
                    return;

                m_ZWrite = value.isOn;
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

        // Specific to SpeedTree Lit
        [SerializeField]
        bool m_Billboard;

        public ToggleData billboard
        {
            get { return new ToggleData(m_Billboard); }
            set
            {
                if (m_Billboard == value.isOn)
                    return;
                m_Billboard = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        SpeedTreeVersion m_SpeedTreeVer;

        public SpeedTreeVersion speedTreeAssetVersion
        {
            get { return m_SpeedTreeVer; }
            set
            {
                if (m_SpeedTreeVer == value)
                    return;

                m_SpeedTreeVer = value;
                Dirty(ModificationScope.Graph);
            }
        }


        [SerializeField]
        TreeGeomType m_TreeGeomType;

        public TreeGeomType speedTreeGeomType
        {
            get { return m_TreeGeomType; }
            set
            {
                if (m_TreeGeomType == value)
                    return;

                m_TreeGeomType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        WindQuality m_WindQuality;

        public WindQuality windQuality
        {
            get { return m_WindQuality; }
            set
            {
                if (m_WindQuality == value)
                    return;

                m_WindQuality = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public SpeedTreeLitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "HD Speedtree Lit";

            List<int> validSlots = new List<int>();
            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            validSlots.Add(PositionSlotId);
            AddSlot(new ColorRGBMaterialSlot(AlbedoSlotId, AlbedoDisplaySlotName, AlbedoSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
            validSlots.Add(AlbedoSlotId);
            AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
            validSlots.Add(NormalSlotId);
            AddSlot(new TangentMaterialSlot(TangentSlotId, TangentSlotName, TangentSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
            validSlots.Add(TangentSlotId);
            AddSlot(new Vector1MaterialSlot(AnisotropySlotId, AnisotropySlotName, AnisotropySlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
            validSlots.Add(AnisotropySlotId);
            AddSlot(new Vector1MaterialSlot(SubsurfaceMaskSlotId, SubsurfaceMaskSlotName, SubsurfaceMaskSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(SubsurfaceMaskSlotId);
            AddSlot(new Vector1MaterialSlot(ThicknessSlotId, ThicknessSlotName, ThicknessSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(ThicknessSlotId);
            AddSlot(new DiffusionProfileInputMaterialSlot(DiffusionProfileHashSlotId, DiffusionProfileHashSlotName, DiffusionProfileHashSlotName, ShaderStageCapability.Fragment));
            validSlots.Add(DiffusionProfileHashSlotId);
            AddSlot(new Vector1MaterialSlot(IridescenceMaskSlotId, IridescenceMaskSlotName, IridescenceMaskSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
            validSlots.Add(IridescenceMaskSlotId);
            AddSlot(new Vector1MaterialSlot(IridescenceThicknessSlotId, IridescenceThicknessSlotName, IridescenceThicknessSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
            validSlots.Add(IridescenceThicknessSlotId);

            AddSlot(new ColorRGBMaterialSlot(SpecularColorSlotId, SpecularColorDisplaySlotName, SpecularColorSlotName, SlotType.Input, Color.white, ColorMode.Default, ShaderStageCapability.Fragment));
            validSlots.Add(SpecularColorSlotId);
            AddSlot(new Vector1MaterialSlot(CoatMaskSlotId, CoatMaskSlotName, CoatMaskSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
            validSlots.Add(CoatMaskSlotId);
            AddSlot(new Vector1MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
            validSlots.Add(MetallicSlotId);
            AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            validSlots.Add(SmoothnessSlotId);
            AddSlot(new Vector1MaterialSlot(AmbientOcclusionSlotId, AmbientOcclusionDisplaySlotName, AmbientOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(AmbientOcclusionSlotId);
            AddSlot(new Vector1MaterialSlot(SpecularOcclusionSlotId, SpecularOcclusionSlotName, SpecularOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(SpecularOcclusionSlotId);
            AddSlot(new ColorRGBMaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
            validSlots.Add(EmissionSlotId);
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(AlphaSlotId);
            AddSlot(new Vector1MaterialSlot(AlphaThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            validSlots.Add(AlphaThresholdSlotId);
            AddSlot(new Vector1MaterialSlot(AlphaThresholdDepthPrepassSlotId, AlphaClipThresholdDepthPrepassSlotName, AlphaClipThresholdDepthPrepassSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            validSlots.Add(AlphaThresholdDepthPrepassSlotId);
            AddSlot(new Vector1MaterialSlot(AlphaThresholdDepthPostpassSlotId, AlphaClipThresholdDepthPostpassSlotName, AlphaClipThresholdDepthPostpassSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            validSlots.Add(AlphaThresholdDepthPostpassSlotId);
            AddSlot(new Vector1MaterialSlot(AlphaThresholdShadowSlotId, AlphaClipThresholdShadowSlotName, AlphaClipThresholdShadowSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            validSlots.Add(AlphaThresholdShadowSlotId);

            // SpeedTree specific stuff
            AddSlot(new ColorRGBMaterialSlot(HueVariationColorSlotId, HueVariationColorSlotName, HueVariationColorSlotName, SlotType.Input, Color.white, ColorMode.Default, ShaderStageCapability.Fragment));
            validSlots.Add(HueVariationColorSlotId);
            AddSlot(new Vector1MaterialSlot(WindQualitySlotId, WindQualitySlotName, WindQualitySlotName, SlotType.Input, 1.0f, ShaderStageCapability.All));
            validSlots.Add(WindQualitySlotId);
            AddSlot(new Vector1MaterialSlot(DepthBiasSlotId, DepthBiasSlotName, DepthBiasSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
            validSlots.Add(DepthBiasSlotId);
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

        public override void ProcessPreviewMaterial(Material previewMaterial)
        {
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
        }
    }
}
