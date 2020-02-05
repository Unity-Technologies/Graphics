using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
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
    [Title("Master", "Eye (HDRP)(Preview)")]
    class EyeMasterNode : MasterNode<IEyeSubShader>, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
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

        public const string IrisNormalSlotName = "IrisNormal";
        public const int IrisNormalSlotId = 4;

        public const string SmoothnessSlotName = "Smoothness";
        public const int SmoothnessSlotId = 5;

        public const string IORSlotName = "IOR";
        public const int IORSlotId = 6;

        public const string AmbientOcclusionSlotName = "Occlusion";
        public const string AmbientOcclusionDisplaySlotName = "AmbientOcclusion";
        public const int AmbientOcclusionSlotId = 7;

        public const string MaskSlotName = "Mask";
        public const int MaskSlotId = 8;

        public const string DiffusionProfileHashSlotName = "DiffusionProfileHash";
        public const string DiffusionProfileHashSlotDisplayName = "Diffusion Profile";
        public const int DiffusionProfileHashSlotId = 9;

        public const string SubsurfaceMaskSlotName = "SubsurfaceMask";
        public const int SubsurfaceMaskSlotId = 10;

        public const string EmissionSlotName = "Emission";
        public const int EmissionSlotId = 11;

        public const string AlphaSlotName = "Alpha";
        public const int AlphaSlotId = 12;

        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const int AlphaClipThresholdSlotId = 13;

        public const string BentNormalSlotName = "BentNormal";
        public const int BentNormalSlotId = 14;

        public const int LightingSlotId = 15;
        public const string BakedGISlotName = "BakedGI";

        public const int BackLightingSlotId = 16;
        public const string BakedBackGISlotName = "BakedBackGI";

        public const int DepthOffsetSlotId = 17;
        public const string DepthOffsetSlotName = "DepthOffset";

        public const string VertexNormalSlotName = "Vertex Normal";
         public const int VertexNormalSlotID = 18;

        public const string VertexTangentSlotName = "Vertex Tangent";
        public const int VertexTangentSlotID = 19;

        public enum MaterialType
        {
            Eye,
            EyeCinematic
        }

        // Don't support Multiply
        public enum AlphaModeEye
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
            VertexNormal = 1 << VertexNormalSlotID,
            VertexTangent = 1 << VertexTangentSlotID,
            Albedo = 1 << AlbedoSlotId,
            SpecularOcclusion = 1 << SpecularOcclusionSlotId,
            Normal = 1 << NormalSlotId,
            IrisNormal = 1 << IrisNormalSlotId,
            Smoothness = 1 << SmoothnessSlotId,
            IOR = 1 << IORSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Mask = 1 << MaskSlotId,
            DiffusionProfile = 1 << DiffusionProfileHashSlotId,
            SubsurfaceMask = 1 << SubsurfaceMaskSlotId,
            Emission = 1 << EmissionSlotId,
            Alpha = 1 << AlphaSlotId,
            AlphaClipThreshold = 1 << AlphaClipThresholdSlotId,
            BentNormal = 1 << BentNormalSlotId,
            BakedGI = 1 << LightingSlotId,
            BakedBackGI = 1 << BackLightingSlotId,
            DepthOffset = 1 << DepthOffsetSlotId,
        }

        const SlotMask EyeSlotMask = SlotMask.Position | SlotMask.VertexNormal | SlotMask.VertexTangent | SlotMask.Albedo | SlotMask.SpecularOcclusion | SlotMask.Normal | SlotMask.IrisNormal | SlotMask.Smoothness | SlotMask.IOR | SlotMask.Occlusion | SlotMask.Mask | SlotMask.DiffusionProfile | SlotMask.SubsurfaceMask | SlotMask.Emission | SlotMask.Alpha | SlotMask.AlphaClipThreshold | SlotMask.BentNormal | SlotMask.BakedGI | SlotMask.DepthOffset;
        const SlotMask EyeCinematicSlotMask = EyeSlotMask;

        // This could also be a simple array. For now, catch any mismatched data.
        SlotMask GetActiveSlotMask()
        {
            switch (materialType)
            {
                case MaterialType.Eye:
                    return EyeSlotMask;

                case MaterialType.EyeCinematic:
                    return EyeCinematicSlotMask;

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
        int m_MaterialNeedsUpdateHash = 0;

        int ComputeMaterialNeedsUpdateHash()
        {
            int hash = 0;

            hash |= (alphaTest.isOn ? 0 : 1) << 0;
            hash |= (receiveSSR.isOn ? 0 : 1) << 1;
            hash |= (RequiresSplitLighting() ? 0 : 1) << 2;

            return hash;
        }

        public EyeMasterNode()
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
            name = "Eye Master";

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

            // IrisNormal
            if (MaterialTypeUsesSlotMask(SlotMask.IrisNormal))
            {
                AddSlot(new NormalMaterialSlot(IrisNormalSlotId, IrisNormalSlotName, IrisNormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                validSlots.Add(IrisNormalSlotId);
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
                AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.9f, ShaderStageCapability.Fragment));
                validSlots.Add(SmoothnessSlotId);
            }

            // IOR
            if (MaterialTypeUsesSlotMask(SlotMask.IOR))
            {
                AddSlot(new Vector1MaterialSlot(IORSlotId, IORSlotName, IORSlotName, SlotType.Input, 1.4f, ShaderStageCapability.Fragment));
                validSlots.Add(IORSlotId);
            }

            // Ambient Occlusion
            if (MaterialTypeUsesSlotMask(SlotMask.Occlusion))
            {
                AddSlot(new Vector1MaterialSlot(AmbientOcclusionSlotId, AmbientOcclusionDisplaySlotName, AmbientOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AmbientOcclusionSlotId);
            }

            // Mask
            if (MaterialTypeUsesSlotMask(SlotMask.Mask))
            {
                AddSlot(new Vector2MaterialSlot(MaskSlotId, MaskSlotName, MaskSlotName, SlotType.Input, new Vector2(1.0f, 0.0f), ShaderStageCapability.Fragment));
                validSlots.Add(MaskSlotId);
            }

            // Diffusion Profile
            if (MaterialTypeUsesSlotMask(SlotMask.DiffusionProfile) && subsurfaceScattering.isOn)
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

        protected override VisualElement CreateCommonSettingsElement()
        {
            return new EyeSettingsView(this);
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

        public override void ProcessPreviewMaterial(Material previewMaterial)
        {
            // Fixup the material settings:
            previewMaterial.SetFloat(kSurfaceType, (int)(SurfaceType)surfaceType);
            previewMaterial.SetFloat(kDoubleSidedNormalMode, (int)doubleSidedMode);
            previewMaterial.SetFloat(kUseSplitLighting, RequiresSplitLighting() ? 1.0f : 0.0f);
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

            EyeGUI.SetupMaterialKeywordsAndPass(previewMaterial);
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
