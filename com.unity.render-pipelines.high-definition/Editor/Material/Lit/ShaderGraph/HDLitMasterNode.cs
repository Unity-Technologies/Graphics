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
    [Title("Master", "Lit (HDRP)")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDLitMasterNode")]
    [FormerName("UnityEditor.ShaderGraph.HDLitMasterNode")]
    class HDLitMasterNode : AbstractMaterialNode, IMasterNode, IHasSettings, ICanChangeShaderGUI, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string AlbedoSlotName = "Albedo";
        public const string AlbedoDisplaySlotName = "BaseColor";
        public const string NormalSlotName = "Normal";
        public const string BentNormalSlotName = "BentNormal";
        public const string TangentSlotName = "Tangent";
        public const string SubsurfaceMaskSlotName = "SubsurfaceMask";
        public const string ThicknessSlotName = "Thickness";
        public const string DiffusionProfileHashSlotName = "DiffusionProfileHash";
        public const string DiffusionProfileHashSlotDisplayName = "Diffusion Profile";
        public const string IridescenceMaskSlotName = "IridescenceMask";
        public const string IridescenceThicknessSlotName = "IridescenceThickness";
        public const string IridescenceThicknessSlotDisplayName = "Iridescence Layer Thickness";
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
        public const string PositionSlotName = "Vertex Position";
        public const string PositionSlotDisplayName = "Vertex Position";
        public const string VertexNormalSlotName = "Vertex Normal";
        public const string VertexTangentSlotName = "Vertex Tangent";
        public const string SpecularAAScreenSpaceVarianceSlotName = "SpecularAAScreenSpaceVariance";
        public const string SpecularAAThresholdSlotName = "SpecularAAThreshold";
        public const string RefractionIndexSlotName = "RefractionIndex";
        public const string RefractionColorSlotName = "RefractionColor";
        public const string RefractionColorSlotDisplayName = "Transmittance Color";
        public const string RefractionDistanceSlotName = "RefractionDistance";
        public const string RefractionDistanceSlotDisplayName = "Transmittance Absorption Distance";
        public const string DistortionSlotName = "Distortion";
        public const string DistortionSlotDisplayName = "Distortion Vector";
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
        public const int VertexNormalSlotID = 33;
        public const int VertexTangentSlotID = 34;

        public enum MaterialType
        {
            Standard,
            SubsurfaceScattering,
            Anisotropy,
            Iridescence,
            SpecularColor,
            Translucent
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
            BentNormal = 1 << BentNormalSlotId,
            Tangent = 1 << TangentSlotId,
            SubsurfaceMask = 1 << SubsurfaceMaskSlotId,
            Thickness = 1 << ThicknessSlotId,
            DiffusionProfile = 1 << DiffusionProfileHashSlotId,
            IridescenceMask = 1 << IridescenceMaskSlotId,
            IridescenceLayerThickness = 1 << IridescenceThicknessSlotId,
            Specular = 1 << SpecularColorSlotId,
            CoatMask = 1 << CoatMaskSlotId,
            Metallic = 1 << MetallicSlotId,
            Emission = 1 << EmissionSlotId,
            Smoothness = 1 << SmoothnessSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Alpha = 1 << AlphaSlotId,
            AlphaThreshold = 1 << AlphaThresholdSlotId,
            AlphaThresholdDepthPrepass = 1 << AlphaThresholdDepthPrepassSlotId,
            AlphaThresholdDepthPostpass = 1 << AlphaThresholdDepthPostpassSlotId,
            Anisotropy = 1 << AnisotropySlotId,
            SpecularOcclusion = 1 << SpecularOcclusionSlotId,
            AlphaThresholdShadow = 1 << AlphaThresholdShadowSlotId,
            Lighting = 1 << LightingSlotId,
            BackLighting = 1 << BackLightingSlotId,
            DepthOffset = 1 << DepthOffsetSlotId,
            VertexNormal = 1 << VertexNormalSlotID,
            VertexTangent = 1 << VertexTangentSlotID
        }

        const SlotMask StandardSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Metallic | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask SubsurfaceScatteringSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.SubsurfaceMask | SlotMask.Thickness | SlotMask.DiffusionProfile | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask AnisotropySlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.Tangent | SlotMask.Anisotropy | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Metallic | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask IridescenceSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.IridescenceMask | SlotMask.IridescenceLayerThickness | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Metallic | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask SpecularColorSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.Specular | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask TranslucentSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.Thickness | SlotMask.DiffusionProfile | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;

        // This could also be a simple array. For now, catch any mismatched data.
        SlotMask GetActiveSlotMask()
        {
            switch (materialType)
            {
                case MaterialType.Standard:
                    return StandardSlotMask;

                case MaterialType.SubsurfaceScattering:
                    return SubsurfaceScatteringSlotMask;

                case MaterialType.Anisotropy:
                    return AnisotropySlotMask;

                case MaterialType.Iridescence:
                    return IridescenceSlotMask;

                case MaterialType.SpecularColor:
                    return SpecularColorSlotMask;

                case MaterialType.Translucent:
                    return TranslucentSlotMask;

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
        bool m_RayTracing;

        public ToggleData rayTracing
        {
            get { return new ToggleData(m_RayTracing); }
            set
            {
                if (m_RayTracing == value.isOn)
                    return;

                m_RayTracing = value.isOn;
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

        [SerializeField, Obsolete("Kept for data migration")]
        internal bool m_DrawBeforeRefraction;

        [SerializeField]
        ScreenSpaceRefraction.RefractionModel m_RefractionModel;

        public ScreenSpaceRefraction.RefractionModel refractionModel
        {
            get { return m_RefractionModel; }
            set
            {
                if (m_RefractionModel == value)
                    return;

                m_RefractionModel = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_Distortion;

        public ToggleData distortion
        {
            get { return new ToggleData(m_Distortion); }
            set
            {
                if (m_Distortion == value.isOn)
                    return;
                m_Distortion = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        DistortionMode m_DistortionMode;

        public DistortionMode distortionMode
        {
            get { return m_DistortionMode; }
            set
            {
                if (m_DistortionMode == value)
                    return;

                m_DistortionMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_DistortionDepthTest = true;

        public ToggleData distortionDepthTest
        {
            get { return new ToggleData(m_DistortionDepthTest); }
            set
            {
                if (m_DistortionDepthTest == value.isOn)
                    return;
                m_DistortionDepthTest = value.isOn;
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
        NormalDropOffSpace m_NormalDropOffSpace;
        public NormalDropOffSpace normalDropOffSpace
        {
            get { return m_NormalDropOffSpace; }
            set
            {
                if (m_NormalDropOffSpace == value)
                    return;

                m_NormalDropOffSpace = value;
                if (!IsSlotConnected(NormalSlotId))
                    updateNormalSlot = true;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }
        bool updateNormalSlot;


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
        bool m_SSSTransmission = true;

        public ToggleData sssTransmission
        {
            get { return new ToggleData(m_SSSTransmission); }
            set
            {
                m_SSSTransmission = value.isOn;
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
        bool m_ReceivesSSRTransparent = true;
        public ToggleData receiveSSRTransparent
        {
            get { return new ToggleData(m_ReceivesSSRTransparent); }
            set
            {
                if (m_ReceivesSSRTransparent == value.isOn)
                    return;
                m_ReceivesSSRTransparent = value.isOn;
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
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        int m_DiffusionProfile;

        public int diffusionProfile
        {
            get { return m_DiffusionProfile; }
            set
            {
                if (m_DiffusionProfile == value)
                    return;

                m_DiffusionProfile = value;
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
        int m_MaterialNeedsUpdateHash = 0;

        int ComputeMaterialNeedsUpdateHash()
        {
            int hash = 0;

            hash |= (alphaTest.isOn ? 0 : 1) << 0;
            hash |= (alphaTestShadow.isOn ? 0 : 1) << 1;
            hash |= (receiveSSR.isOn ? 0 : 1) << 2;
            hash |= (receiveSSRTransparent.isOn ? 0 : 1) << 3;
            hash |= (RequiresSplitLighting() ? 0 : 1) << 4;

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

        public HDLitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return null; }
        }

        public bool HasRefraction()
        {
            return (surfaceType == SurfaceType.Transparent && renderingPass != HDRenderQueue.RenderQueueType.PreRefraction && refractionModel != ScreenSpaceRefraction.RefractionModel.None);
        }

        public bool HasDistortion()
        {
            return (surfaceType == SurfaceType.Transparent && distortion.isOn);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Lit Master";

            List<int> validSlots = new List<int>();
            if (MaterialTypeUsesSlotMask(SlotMask.Position))
            {
                AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotDisplayName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(PositionSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.VertexNormal))
            {
                AddSlot(new NormalMaterialSlot(VertexNormalSlotID, VertexNormalSlotName, VertexNormalSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(VertexNormalSlotID);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.VertexTangent))
            {
                AddSlot(new TangentMaterialSlot(VertexTangentSlotID, VertexTangentSlotName, VertexTangentSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(VertexTangentSlotID);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Albedo))
            {
                AddSlot(new ColorRGBMaterialSlot(AlbedoSlotId, AlbedoDisplaySlotName, AlbedoSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(AlbedoSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Normal))
            {
                var coordSpace = CoordinateSpace.Tangent;
                if (updateNormalSlot)
                {
                    RemoveSlot(NormalSlotId);
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
                    updateNormalSlot = false;
                }
                AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, coordSpace, ShaderStageCapability.Fragment));
                validSlots.Add(NormalSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.BentNormal))
            {
                AddSlot(new NormalMaterialSlot(BentNormalSlotId, BentNormalSlotName, BentNormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                validSlots.Add(BentNormalSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Tangent))
            {
                AddSlot(new TangentMaterialSlot(TangentSlotId, TangentSlotName, TangentSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                validSlots.Add(TangentSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Anisotropy))
            {
                AddSlot(new Vector1MaterialSlot(AnisotropySlotId, AnisotropySlotName, AnisotropySlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AnisotropySlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.SubsurfaceMask))
            {
                AddSlot(new Vector1MaterialSlot(SubsurfaceMaskSlotId, SubsurfaceMaskSlotName, SubsurfaceMaskSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SubsurfaceMaskSlotId);
            }
            if ((MaterialTypeUsesSlotMask(SlotMask.Thickness) && (sssTransmission.isOn || materialType == MaterialType.Translucent)) || HasRefraction())
            {
                AddSlot(new Vector1MaterialSlot(ThicknessSlotId, ThicknessSlotName, ThicknessSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(ThicknessSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.DiffusionProfile))
            {
                AddSlot(new DiffusionProfileInputMaterialSlot(DiffusionProfileHashSlotId, DiffusionProfileHashSlotDisplayName, DiffusionProfileHashSlotName, ShaderStageCapability.Fragment));
                validSlots.Add(DiffusionProfileHashSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.IridescenceMask))
            {
                AddSlot(new Vector1MaterialSlot(IridescenceMaskSlotId, IridescenceMaskSlotName, IridescenceMaskSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(IridescenceMaskSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.IridescenceLayerThickness))
            {
                AddSlot(new Vector1MaterialSlot(IridescenceThicknessSlotId, IridescenceThicknessSlotDisplayName, IridescenceThicknessSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(IridescenceThicknessSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Specular))
            {
                AddSlot(new ColorRGBMaterialSlot(SpecularColorSlotId, SpecularColorDisplaySlotName, SpecularColorSlotName, SlotType.Input, Color.white, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularColorSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.CoatMask))
            {
                AddSlot(new Vector1MaterialSlot(CoatMaskSlotId, CoatMaskSlotName, CoatMaskSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(CoatMaskSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Metallic))
            {
                AddSlot(new Vector1MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(MetallicSlotId);
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
            if (MaterialTypeUsesSlotMask(SlotMask.SpecularOcclusion) && specularOcclusionMode == SpecularOcclusionMode.Custom)
            {
                AddSlot(new Vector1MaterialSlot(SpecularOcclusionSlotId, SpecularOcclusionSlotName, SpecularOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularOcclusionSlotId);
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
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaThreshold) && alphaTest.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaThresholdSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaThresholdDepthPrepass) && surfaceType == SurfaceType.Transparent && alphaTest.isOn && alphaTestDepthPrepass.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaThresholdDepthPrepassSlotId, AlphaClipThresholdDepthPrepassSlotName, AlphaClipThresholdDepthPrepassSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaThresholdDepthPrepassSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaThresholdDepthPostpass) && surfaceType == SurfaceType.Transparent && alphaTest.isOn && alphaTestDepthPostpass.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaThresholdDepthPostpassSlotId, AlphaClipThresholdDepthPostpassSlotName, AlphaClipThresholdDepthPostpassSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaThresholdDepthPostpassSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.AlphaThresholdShadow) && alphaTest.isOn && alphaTestShadow.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaThresholdShadowSlotId, AlphaClipThresholdShadowSlotName, AlphaClipThresholdShadowSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaThresholdShadowSlotId);
            }
            if (specularAA.isOn)
            {
                AddSlot(new Vector1MaterialSlot(SpecularAAScreenSpaceVarianceSlotId, SpecularAAScreenSpaceVarianceSlotName, SpecularAAScreenSpaceVarianceSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularAAScreenSpaceVarianceSlotId);

                AddSlot(new Vector1MaterialSlot(SpecularAAThresholdSlotId, SpecularAAThresholdSlotName, SpecularAAThresholdSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularAAThresholdSlotId);
            }
            if (HasRefraction())
            {
                AddSlot(new Vector1MaterialSlot(RefractionIndexSlotId, RefractionIndexSlotName, RefractionIndexSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(RefractionIndexSlotId);

                AddSlot(new ColorRGBMaterialSlot(RefractionColorSlotId, RefractionColorSlotDisplayName, RefractionColorSlotName, SlotType.Input, Color.white, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(RefractionColorSlotId);

                AddSlot(new Vector1MaterialSlot(RefractionDistanceSlotId, RefractionDistanceSlotDisplayName, RefractionDistanceSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(RefractionDistanceSlotId);
            }
            if (HasDistortion())
            {
                AddSlot(new Vector2MaterialSlot(DistortionSlotId, DistortionSlotDisplayName, DistortionSlotName, SlotType.Input, new Vector2(2.0f, -1.0f), ShaderStageCapability.Fragment));
                validSlots.Add(DistortionSlotId);

                AddSlot(new Vector1MaterialSlot(DistortionBlurSlotId, DistortionBlurSlotName, DistortionBlurSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(DistortionBlurSlotId);
            }
            if (MaterialTypeUsesSlotMask(SlotMask.Lighting) && overrideBakedGI.isOn)
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
            return new HDLitSettingsView(this);
        }

        public string renderQueueTag
        {
            get
            {
                if(renderingPass == HDRenderQueue.RenderQueueType.Unknown)
                {
                    switch(surfaceType)
                    {
                        case SurfaceType.Opaque:
                            renderingPass = HDRenderQueue.RenderQueueType.Opaque;
                            break;
                        case SurfaceType.Transparent:
                        #pragma warning disable CS0618 // Type or member is obsolete
                            if (m_DrawBeforeRefraction)
                            {
                                m_DrawBeforeRefraction = false;
                        #pragma warning restore CS0618 // Type or member is obsolete
                                renderingPass = HDRenderQueue.RenderQueueType.PreRefraction;
                            }
                            else
                            {
                                renderingPass = HDRenderQueue.RenderQueueType.Transparent;
                            }
                            break;
                    }
                }
                int queue = HDRenderQueue.ChangeType(renderingPass, sortPriority, alphaTest.isOn);
                return HDRenderQueue.GetShaderTagValue(queue);
            }
        }

        public string renderTypeTag => HDRenderTypeTags.HDLitShader.ToString();

        public ConditionalField[] GetConditionalFields(PassDescriptor pass)
        {
            var ambientOcclusionSlot = FindSlot<Vector1MaterialSlot>(AmbientOcclusionSlotId);
            var coatMaskSlot = FindSlot<Vector1MaterialSlot>(CoatMaskSlotId);

            // We need this to know if there are any Dots properties active
            // Ideally we do this another way but HDLit needs this for conditional pragmas
            var shaderProperties = new PropertyCollector();
            owner.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
            bool hasDotsProperties = shaderProperties.GetDotsInstancingPropertiesCount(GenerationMode.ForReals) > 0;

            return new ConditionalField[]
            {
                // Features
                new ConditionalField(Fields.GraphVertex,                    IsSlotConnected(PositionSlotId) ||
                                                                                IsSlotConnected(VertexNormalSlotID) ||
                                                                                IsSlotConnected(VertexTangentSlotID)),
                new ConditionalField(Fields.GraphPixel,                     true),
                new ConditionalField(Fields.LodCrossFade,                   supportLodCrossFade.isOn),

                // Structs
                new ConditionalField(HDStructFields.FragInputs.IsFrontFace,doubleSidedMode != DoubleSidedMode.Disabled &&
                                                                                !pass.Equals(HDLitSubTarget.LitPasses.MotionVectors)),

                // Dots
                new ConditionalField(HDFields.DotsInstancing,               dotsInstancing.isOn),
                new ConditionalField(HDFields.DotsProperties,               hasDotsProperties),

                // Material
                new ConditionalField(HDFields.Anisotropy,                   materialType == MaterialType.Anisotropy),
                new ConditionalField(HDFields.Iridescence,                  materialType == MaterialType.Iridescence),
                new ConditionalField(HDFields.SpecularColor,                materialType == MaterialType.SpecularColor),
                new ConditionalField(HDFields.Standard,                     materialType == MaterialType.Standard),
                new ConditionalField(HDFields.SubsurfaceScattering,         materialType == MaterialType.SubsurfaceScattering &&
                                                                                surfaceType != SurfaceType.Transparent),
                new ConditionalField(HDFields.Transmission,                (materialType == MaterialType.SubsurfaceScattering && sssTransmission.isOn) ||
                                                                                (materialType == MaterialType.Translucent)),
                new ConditionalField(HDFields.Translucent,                 materialType == MaterialType.Translucent),

                // Surface Type
                new ConditionalField(Fields.SurfaceOpaque,                  surfaceType == SurfaceType.Opaque),
                new ConditionalField(Fields.SurfaceTransparent,             surfaceType != SurfaceType.Opaque),

                // Blend Mode
                new ConditionalField(Fields.BlendAdd,                       surfaceType != SurfaceType.Opaque && alphaMode == AlphaMode.Additive),
                new ConditionalField(Fields.BlendAlpha,                     surfaceType != SurfaceType.Opaque && alphaMode == AlphaMode.Alpha),
                new ConditionalField(Fields.BlendMultiply,                  surfaceType != SurfaceType.Opaque && alphaMode == AlphaMode.Multiply),
                new ConditionalField(Fields.BlendPremultiply,               surfaceType != SurfaceType.Opaque && alphaMode == AlphaMode.Premultiply),

                // Double Sided
                new ConditionalField(HDFields.DoubleSided,                  doubleSidedMode != DoubleSidedMode.Disabled),
                new ConditionalField(HDFields.DoubleSidedFlip,              doubleSidedMode == DoubleSidedMode.FlippedNormals &&
                                                                                !pass.Equals(HDLitSubTarget.LitPasses.MotionVectors)),
                new ConditionalField(HDFields.DoubleSidedMirror,            doubleSidedMode == DoubleSidedMode.MirroredNormals &&
                                                                                !pass.Equals(HDLitSubTarget.LitPasses.MotionVectors)),

                // Specular Occlusion
                new ConditionalField(HDFields.SpecularOcclusionFromAO,      specularOcclusionMode == SpecularOcclusionMode.FromAO),
                new ConditionalField(HDFields.SpecularOcclusionFromAOBentNormal, specularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal),
                new ConditionalField(HDFields.SpecularOcclusionCustom,      specularOcclusionMode == SpecularOcclusionMode.Custom),

                //Distortion
                new ConditionalField(HDFields.DistortionDepthTest,          distortionDepthTest.isOn),
                new ConditionalField(HDFields.DistortionAdd,                distortionMode == DistortionMode.Add),
                new ConditionalField(HDFields.DistortionMultiply,           distortionMode == DistortionMode.Multiply),
                new ConditionalField(HDFields.DistortionReplace,            distortionMode == DistortionMode.Replace),
                new ConditionalField(HDFields.TransparentDistortion,        surfaceType != SurfaceType.Opaque && distortion.isOn),

                // Refraction
                new ConditionalField(HDFields.Refraction,                   HasRefraction()),
                new ConditionalField(HDFields.RefractionBox,                HasRefraction() && refractionModel == ScreenSpaceRefraction.RefractionModel.Box),
                new ConditionalField(HDFields.RefractionSphere,             HasRefraction() && refractionModel == ScreenSpaceRefraction.RefractionModel.Sphere),

                //Normal Drop Off Space
                new ConditionalField(Fields.NormalDropOffOS,                normalDropOffSpace == NormalDropOffSpace.Object),
                new ConditionalField(Fields.NormalDropOffTS,                normalDropOffSpace == NormalDropOffSpace.Tangent),
                new ConditionalField(Fields.NormalDropOffWS,                normalDropOffSpace == NormalDropOffSpace.World),

                // Misc
                // We always generate the keyword ALPHATEST_ON
                new ConditionalField(Fields.AlphaTest,                      alphaTest.isOn && (pass.pixelPorts.Contains(AlphaThresholdSlotId) || pass.pixelPorts.Contains(AlphaThresholdShadowSlotId) ||
                                                                                 pass.pixelPorts.Contains(AlphaThresholdDepthPrepassSlotId) || pass.pixelPorts.Contains(AlphaThresholdDepthPostpassSlotId))),
                // All the DoAlphaXXX field drive the generation of which code to use for alpha test in the template
                // Do alpha test only if we aren't using the TestShadow one
                // Note: we always generate the code for DoAlphaTestXXX as it is then the keyword _ALPHATEST_ON that will define if it is enabled or not
                new ConditionalField(HDFields.DoAlphaTest,                  alphaTest.isOn && (pass.pixelPorts.Contains(AlphaThresholdSlotId) &&
                                                                                !(alphaTestShadow.isOn && pass.pixelPorts.Contains(AlphaThresholdShadowSlotId)))),
                new ConditionalField(HDFields.DoAlphaTestShadow,            alphaTest.isOn && alphaTestShadow.isOn && pass.pixelPorts.Contains(AlphaThresholdShadowSlotId)),
                new ConditionalField(HDFields.DoAlphaTestPrepass,           alphaTest.isOn && alphaTestDepthPrepass.isOn && pass.pixelPorts.Contains(AlphaThresholdDepthPrepassSlotId)),
                new ConditionalField(HDFields.DoAlphaTestPostpass,          alphaTest.isOn && alphaTestDepthPostpass.isOn && pass.pixelPorts.Contains(AlphaThresholdDepthPostpassSlotId)),
                new ConditionalField(Fields.AlphaToMask,                    alphaTest.isOn && pass.pixelPorts.Contains(AlphaThresholdSlotId) && alphaToMask.isOn),
                new ConditionalField(HDFields.AlphaFog,                     surfaceType != SurfaceType.Opaque && transparencyFog.isOn),
                new ConditionalField(HDFields.BlendPreserveSpecular,        surfaceType != SurfaceType.Opaque && blendPreserveSpecular.isOn),
                new ConditionalField(HDFields.TransparentWritesMotionVec,   surfaceType != SurfaceType.Opaque && transparentWritesMotionVec.isOn),
                new ConditionalField(HDFields.DisableDecals,                !receiveDecals.isOn),
                new ConditionalField(HDFields.DisableSSR,                   !receiveSSR.isOn),
                new ConditionalField(HDFields.DisableSSRTransparent,        !receiveSSRTransparent.isOn),
                new ConditionalField(Fields.VelocityPrecomputed,                addPrecomputedVelocity.isOn),
                new ConditionalField(HDFields.SpecularAA,                   specularAA.isOn &&
                                                                                pass.pixelPorts.Contains(SpecularAAThresholdSlotId) &&
                                                                                pass.pixelPorts.Contains(SpecularAAScreenSpaceVarianceSlotId)),
                new ConditionalField(HDFields.EnergyConservingSpecular,     energyConservingSpecular.isOn),
                new ConditionalField(HDFields.BentNormal,                   IsSlotConnected(BentNormalSlotId) &&
                                                                                pass.pixelPorts.Contains(BentNormalSlotId)),
                new ConditionalField(HDFields.AmbientOcclusion,             pass.pixelPorts.Contains(AmbientOcclusionSlotId) &&
                                                                                (IsSlotConnected(AmbientOcclusionSlotId) ||
                                                                                ambientOcclusionSlot.value != ambientOcclusionSlot.defaultValue)),
                new ConditionalField(HDFields.CoatMask,                     pass.pixelPorts.Contains(CoatMaskSlotId) &&
                                                                                (IsSlotConnected(CoatMaskSlotId) || coatMaskSlot.value > 0.0f)),
                new ConditionalField(HDFields.Tangent,                      IsSlotConnected(TangentSlotId) &&
                                                                                pass.pixelPorts.Contains(TangentSlotId)),
                new ConditionalField(HDFields.LightingGI,                   IsSlotConnected(LightingSlotId) &&
                                                                                pass.pixelPorts.Contains(LightingSlotId)),
                new ConditionalField(HDFields.BackLightingGI,               IsSlotConnected(BackLightingSlotId) &&
                                                                                pass.pixelPorts.Contains(BackLightingSlotId)),
                new ConditionalField(HDFields.DepthOffset,                  depthOffset.isOn && pass.pixelPorts.Contains(DepthOffsetSlotId)),
                new ConditionalField(HDFields.TransparentBackFace,          surfaceType != SurfaceType.Opaque && backThenFrontRendering.isOn),
                new ConditionalField(HDFields.TransparentDepthPrePass,      surfaceType != SurfaceType.Opaque && alphaTestDepthPrepass.isOn),
                new ConditionalField(HDFields.TransparentDepthPostPass,     surfaceType != SurfaceType.Opaque && alphaTestDepthPostpass.isOn),
                new ConditionalField(HDFields.RayTracing,                   rayTracing.isOn),
            };
        }

        public void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)(SurfaceType)surfaceType);
            material.SetFloat(kDoubleSidedNormalMode, (int)doubleSidedMode);
            material.SetFloat(kAlphaCutoffEnabled, alphaTest.isOn ? 1 : 0);
            material.SetFloat(kBlendMode, (int)HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode));
            material.SetFloat(kEnableFogOnTransparent, transparencyFog.isOn ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)zTest);
            material.SetFloat(kTransparentCullMode, (int)transparentCullMode);
            material.SetFloat(kZWrite, zWrite.isOn ? 1.0f : 0.0f);
            // No sorting priority for shader graph preview
            material.renderQueue = (int)HDRenderQueue.ChangeType(renderingPass, offset: 0, alphaTest: alphaTest.isOn);

            HDLitGUI.SetupMaterialKeywordsAndPass(material);
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
            return materialType == HDLitMasterNode.MaterialType.SubsurfaceScattering;
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
            // ShaderGraph only property used to send the RenderQueueType to the material
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "_RenderQueueType",
                hidden = true,
                value = (int)renderingPass,
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
            HDSubShaderUtilities.AddStencilShaderProperties(collector, RequiresSplitLighting(), receiveSSR.isOn, receiveSSRTransparent.isOn);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                surfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode),
                sortPriority,
                alphaToMask.isOn,
                zWrite.isOn,
                transparentCullMode,
                zTest,
                backThenFrontRendering.isOn,
                transparencyFog.isOn
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, alphaTest.isOn, alphaTestShadow.isOn);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, doubleSidedMode);
            HDSubShaderUtilities.AddRayTracingProperty(collector, rayTracing.isOn);

            base.CollectShaderProperties(collector, generationMode);
        }

        public override void Setup()
        {
            base.Setup();
            var hdPipelineAsset = HDRenderPipeline.currentAsset;

            if (hdPipelineAsset == null)
                return;

            var diffusionProfileSlot = FindSlot<DiffusionProfileInputMaterialSlot>(DiffusionProfileHashSlotId);

            if (diffusionProfileSlot == null)
                return;

            if ((diffusionProfileSlot.diffusionProfile) != null && !hdPipelineAsset.diffusionProfileSettingsList.Any(d => d == diffusionProfileSlot.diffusionProfile))
            {
                //owner.AddSetupError(tempId, $"Diffusion profile '{diffusionProfileSlot.diffusionProfile.name}' is not referenced in the current HDRP asset", ShaderCompilerMessageSeverity.Warning);
            }

        }
    }
}
