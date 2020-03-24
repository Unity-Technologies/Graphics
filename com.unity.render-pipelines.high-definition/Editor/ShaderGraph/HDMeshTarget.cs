using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
#region Enumerations
    enum ShaderType
    {
        Unlit,
        Lit,
        // Eye,
        // Fabric,
        // Hair,
        // StackLit,
    }

    public enum MaterialType
    {
        Standard,
        SubsurfaceScattering,
        Anisotropy,
        Iridescence,
        SpecularColor,
        Translucent
    }

    enum AlphaMode
    {
        Alpha,
        Premultiply,
        Additive,
    }

    enum DistortionMode
    {
        Add,
        Multiply,
        Replace
    }

    enum DoubleSidedMode
    {
        Disabled,
        Enabled,
        FlippedNormals,
        MirroredNormals,
    }

    enum EmissionGIMode
    {
        Disabled,
        Realtime,
        Baked,
    }

    enum SpecularOcclusionMode
    {
        Off,
        FromAO,
        FromAOAndBentNormal,
        Custom
    }
#endregion

    class HDMeshTarget : ITargetImplementation
    {
#region Serialized Fields
        // --------------------------------------------------
        // Unlit

        [SerializeField]
        ShaderType m_ShaderType;

        [SerializeField]
        SurfaceType m_SurfaceType;

        [SerializeField]
        AlphaMode m_AlphaMode;

        [SerializeField]
        HDRenderQueue.RenderQueueType m_RenderingPass = HDRenderQueue.RenderQueueType.Opaque;

        [SerializeField]
        bool m_TransparencyFog = true;

        [SerializeField]
        bool m_Distortion;

        [SerializeField]
        DistortionMode m_DistortionMode;

        [SerializeField]
        bool m_DistortionOnly = true;

        [SerializeField]
        bool m_DistortionDepthTest = true;

        [SerializeField]
        bool m_AlphaTest;

        [SerializeField]
        int m_SortPriority;

        [SerializeField]
        bool m_ZWrite = true;

        [SerializeField]
        TransparentCullMode m_TransparentCullMode = TransparentCullMode.Back;

        [SerializeField]
        CompareFunction m_ZTest = CompareFunction.LessEqual;

        [SerializeField]
        bool m_AddPrecomputedVelocity = false;

        [SerializeField]
        bool m_EnableShadowMatte = false;

        [SerializeField]
        bool m_DOTSInstancing = false;

        // --------------------------------------------------
        // Lit

        [SerializeField]
        MaterialType m_MaterialType;

        [SerializeField]
        bool m_BlendPreserveSpecular = true;

        [SerializeField]
        ScreenSpaceRefraction.RefractionModel m_RefractionModel;

        [SerializeField]
        bool m_AlphaTestDepthPrepass;

        [SerializeField]
        bool m_AlphaTestDepthPostpass;

        [SerializeField]
        bool m_TransparentWritesMotionVec;

        [SerializeField]
        bool m_AlphaTestShadow;

        [SerializeField]
        bool m_BackThenFrontRendering;

        [SerializeField]
        DoubleSidedMode m_DoubleSidedMode;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace;

        [SerializeField]
        bool m_SSSTransmission = true;

        [SerializeField]
        bool m_ReceiveDecals = true;

        [SerializeField]
        bool m_ReceivesSSR = true;

        [SerializeField]
        bool m_ReceivesSSRTransparent = true;

        [SerializeField]
        bool m_EnergyConservingSpecular = true;

        [SerializeField]
        bool m_SpecularAA;

        [SerializeField]
        float m_SpecularAAScreenSpaceVariance;

        [SerializeField]
        float m_SpecularAAThreshold;

        [SerializeField]
        SpecularOcclusionMode m_SpecularOcclusionMode;

        [SerializeField]
        int m_DiffusionProfile;

        [SerializeField]
        bool m_OverrideBakedGI;

        [SerializeField]
        bool m_DepthOffset;

        [SerializeField]
        bool m_SupportLodCrossFade;

        [SerializeField]
        int m_MaterialNeedsUpdateHash = 0;
#endregion

#region ITargetImplementation Properties
        public Type targetType => typeof(MeshTarget);
        public string displayName => "HDRP";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph/Templates";
        public string renderTypeTag => GetRenderTypeTag();
        public string renderQueueTag => GetRenderQueueTag();
#endregion

#region Data Properties
        // --------------------------------------------------
        // Unlit

        public ShaderType shaderType
        {
            get => m_ShaderType;
            set => m_ShaderType = value;
        }

        public SurfaceType surfaceType
        {
            get => m_SurfaceType;
            set => m_SurfaceType = value;
        }

        public AlphaMode alphaMode
        {
            get => m_AlphaMode;
            set => m_AlphaMode = value;
        }

        public HDRenderQueue.RenderQueueType renderingPass
        {
            get => m_RenderingPass;
            set => m_RenderingPass = value;
        }

        public bool transparencyFog
        {
            get => m_TransparencyFog;
            set => m_TransparencyFog = value;
        }

        public bool distortion
        {
            get => m_Distortion;
            set => m_Distortion = value;
        }

        public DistortionMode distortionMode
        {
            get => m_DistortionMode;
            set => m_DistortionMode = value;
        }

        public bool distortionOnly
        {
            get => m_DistortionOnly;
            set => m_DistortionOnly = value;
        }

        public bool distortionDepthTest
        {
            get => m_DistortionDepthTest;
            set => m_DistortionDepthTest = value;
        }

        public bool alphaTest
        {
            get => m_AlphaTest;
            set => m_AlphaTest = value;
        }

        public int sortPriority
        {
            get => m_SortPriority;
            set => m_SortPriority = value;
        }

        public bool zWrite
        {
            get => m_ZWrite;
            set => m_ZWrite = value;
        }

        public TransparentCullMode transparentCullMode
        {
            get => m_TransparentCullMode;
            set => m_TransparentCullMode = value;
        }

        public CompareFunction zTest
        {
            get => m_ZTest;
            set => m_ZTest = value;
        }

        public bool addPrecomputedVelocity
        {
            get => m_AddPrecomputedVelocity;
            set => m_AddPrecomputedVelocity = value;
        }

        public bool enableShadowMatte
        {
            get => m_EnableShadowMatte;
            set => m_EnableShadowMatte = value;
        }

        public bool dotsInstancing
        {
            get => m_DOTSInstancing;
            set => m_DOTSInstancing = value;
        }

        // --------------------------------------------------
        // Lit

        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        public bool blendPreserveSpecular
        {
            get => m_BlendPreserveSpecular;
            set => m_BlendPreserveSpecular = value;
        }

        public ScreenSpaceRefraction.RefractionModel refractionModel
        {
            get => m_RefractionModel;
            set => m_RefractionModel = value;
        }

        public bool alphaTestDepthPrepass
        {
            get => m_AlphaTestDepthPrepass;
            set => m_AlphaTestDepthPrepass = value;
        }

        public bool alphaTestDepthPostpass
        {
            get => m_AlphaTestDepthPostpass;
            set => m_AlphaTestDepthPostpass = value;
        }

        public bool transparentWritesMotionVec
        {
            get => m_TransparentWritesMotionVec;
            set => m_TransparentWritesMotionVec = value;
        }

        public bool alphaTestShadow
        {
            get => m_AlphaTestShadow;
            set => m_AlphaTestShadow = value;
        }

        public bool backThenFrontRendering
        {
            get => m_BackThenFrontRendering;
            set => m_BackThenFrontRendering = value;
        }

        public DoubleSidedMode doubleSidedMode
        {
            get => m_DoubleSidedMode;
            set => m_DoubleSidedMode = value;
        }

        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }

        public bool sssTransmission
        {
            get => m_SSSTransmission;
            set => m_SSSTransmission = value;
        }

        public bool receiveDecals
        {
            get => m_ReceiveDecals;
            set => m_ReceiveDecals = value;
        }

        public bool receivesSSR
        {
            get => m_ReceivesSSR;
            set => m_ReceivesSSR = value;
        }

        public bool receivesSSRTransparent
        {
            get => m_ReceivesSSRTransparent;
            set => m_ReceivesSSRTransparent = value;
        }

        public bool energyConservingSpecular
        {
            get => m_EnergyConservingSpecular;
            set => m_EnergyConservingSpecular = value;
        }

        public bool specularAA
        {
            get => m_SpecularAA;
            set => m_SpecularAA = value;
        }

        public float specularAAScreenSpaceVariance
        {
            get => m_SpecularAAScreenSpaceVariance;
            set => m_SpecularAAScreenSpaceVariance = value;
        }

        public float specularAAThreshold
        {
            get => m_SpecularAAThreshold;
            set => m_SpecularAAThreshold = value;
        }

        public SpecularOcclusionMode specularOcclusionMode
        {
            get => m_SpecularOcclusionMode;
            set => m_SpecularOcclusionMode = value;
        }

        public int diffusionProfile
        {
            get => m_DiffusionProfile;
            set => m_DiffusionProfile = value;
        }

        public bool overrideBakedGI
        {
            get => m_OverrideBakedGI;
            set => m_OverrideBakedGI = value;
        }

        public bool depthOffset
        {
            get => m_DepthOffset;
            set => m_DepthOffset = value;
        }

        public bool supportLodCrossFade
        {
            get => m_SupportLodCrossFade;
            set => m_SupportLodCrossFade = value;
        }
#endregion

#region Helper Properties
        // --------------------------------------------------
        // Unlit

        bool activeDistortion => m_SurfaceType == SurfaceType.Transparent && m_Distortion;
        bool activeAlpha => m_SurfaceType == SurfaceType.Transparent || m_AlphaTest;
        bool activeAlphaTest => m_AlphaTest;
        bool activeShadowTint => m_ShaderType == ShaderType.Unlit && m_EnableShadowMatte;

        // --------------------------------------------------
        // Lit

        bool activeRefraction => m_ShaderType == ShaderType.Lit && m_SurfaceType == SurfaceType.Transparent && m_RenderingPass != HDRenderQueue.RenderQueueType.PreRefraction && m_RefractionModel != ScreenSpaceRefraction.RefractionModel.None;
        bool activeSplitLighting => m_ShaderType == ShaderType.Lit && m_MaterialType == MaterialType.SubsurfaceScattering;
        bool activeReceiveDecals => m_ShaderType == ShaderType.Lit && m_ReceiveDecals;
        bool activeReceiveSSR => m_ShaderType == ShaderType.Lit && m_ReceivesSSR;
        bool activeReceiveSSRTransparent => m_ShaderType == ShaderType.Lit && m_ReceivesSSRTransparent;
        bool activeAlphaTestShadow => m_ShaderType == ShaderType.Lit && m_AlphaTest && m_AlphaTestShadow;
        bool activeLodCrossFade => m_ShaderType == ShaderType.Lit && m_SupportLodCrossFade;
        bool activeTransmission => m_ShaderType == ShaderType.Lit && ((m_MaterialType == MaterialType.SubsurfaceScattering && m_SSSTransmission) || m_MaterialType == MaterialType.Translucent);
        bool activeBlendPreserveSpecular => m_ShaderType == ShaderType.Lit && m_SurfaceType != SurfaceType.Opaque && m_BlendPreserveSpecular;
        bool activeTransparentWritesMotionVec => m_ShaderType == ShaderType.Lit && m_SurfaceType != SurfaceType.Opaque && m_TransparentWritesMotionVec;
        bool activeVelocityPrecomputed => m_ShaderType == ShaderType.Lit && m_AddPrecomputedVelocity;
        bool activeSpecularAA => m_ShaderType == ShaderType.Lit && m_SpecularAA;
        bool activeDepthOffset => m_ShaderType == ShaderType.Lit && m_DepthOffset;
        bool activeBackThenFrontRendering => m_ShaderType == ShaderType.Lit && m_SurfaceType != SurfaceType.Opaque && m_BackThenFrontRendering;
        bool activeTransparentDepthPrepass => m_ShaderType == ShaderType.Lit && m_SurfaceType != SurfaceType.Opaque && m_AlphaTestDepthPrepass;
        bool activeTransparentDepthPostpass => m_ShaderType == ShaderType.Lit && m_SurfaceType != SurfaceType.Opaque && m_AlphaTestDepthPostpass;
        bool activeEnergyConservingSpecular => m_ShaderType == ShaderType.Lit && m_EnergyConservingSpecular;
#endregion

#region Setup
        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("326a52113ee5a7d46bf9145976dcb7f6")); // HDRPMeshTarget

            switch(m_ShaderType)
            {
                case ShaderType.Unlit:
                    context.AddSubShader(HDSubShaders.HDUnlit);
                    break;
                case ShaderType.Lit:
                    context.AddSubShader(HDSubShaders.HDLit);
                    break;
                // case ShaderType.Eye:
                //     context.AddSubShader(HDSubShaders.Eye);
                //     break;
                // case ShaderType.Fabric:
                //     context.AddSubShader(HDSubShaders.Fabric);
                //     break;
                // case ShaderType.Hair:
                //     context.AddSubShader(HDSubShaders.Hair);
                //     break;
                // case ShaderType.StackLit:
                //     context.AddSubShader(HDSubShaders.StackLit);
                //     break;
            }
        }
#endregion

#region Active Blocks
        public void SetActiveBlocks(ref List<BlockFieldDescriptor> activeBlocks)
        {
            // Always supported Blocks
            activeBlocks.Add(BlockFields.VertexDescription.Position);
            activeBlocks.Add(BlockFields.VertexDescription.Normal);
            activeBlocks.Add(BlockFields.VertexDescription.Tangent);
            activeBlocks.Add(BlockFields.SurfaceDescription.BaseColor);
            activeBlocks.Add(BlockFields.SurfaceDescription.Emission);

            // --------------------------------------------------
            // Unlit

            if(activeAlpha)
                activeBlocks.Add(BlockFields.SurfaceDescription.Alpha);

            if(activeAlphaTest)
                activeBlocks.Add(BlockFields.SurfaceDescription.AlphaClipThreshold);
            
            if(activeShadowTint)
                activeBlocks.Add(HDBlockFields.SurfaceDescription.ShadowTint);

            if(activeDistortion)
            {
                activeBlocks.Add(HDBlockFields.SurfaceDescription.Distortion);
                activeBlocks.Add(HDBlockFields.SurfaceDescription.DistortionBlur);
            }

            // --------------------------------------------------
            // Lit

            if(m_ShaderType == ShaderType.Lit)
            {
                activeBlocks.Add(HDBlockFields.SurfaceDescription.BentNormal);
                activeBlocks.Add(HDBlockFields.SurfaceDescription.CoatMask);
                activeBlocks.Add(BlockFields.SurfaceDescription.Smoothness);
                activeBlocks.Add(BlockFields.SurfaceDescription.Occlusion);

                switch (m_NormalDropOffSpace)
                {
                    case NormalDropOffSpace.Tangent:
                        activeBlocks.Add(BlockFields.SurfaceDescription.NormalTS);
                        break;
                    case NormalDropOffSpace.World:
                        activeBlocks.Add(BlockFields.SurfaceDescription.NormalWS);
                        break;
                    case NormalDropOffSpace.Object:
                        activeBlocks.Add(BlockFields.SurfaceDescription.NormalOS);
                        break;
                }

                switch (m_MaterialType)
                {
                    case MaterialType.Standard:
                        activeBlocks.Add(BlockFields.SurfaceDescription.Metallic);
                        break;
                    case MaterialType.SpecularColor:
                        activeBlocks.Add(BlockFields.SurfaceDescription.Specular);
                        break;
                    case MaterialType.SubsurfaceScattering:
                        activeBlocks.Add(HDBlockFields.SurfaceDescription.SubsurfaceMask);
                        activeBlocks.Add(HDBlockFields.SurfaceDescription.DiffusionProfileHash);
                        break;
                    case MaterialType.Translucent:
                        activeBlocks.Add(HDBlockFields.SurfaceDescription.DiffusionProfileHash);
                        break;
                    case MaterialType.Anisotropy:
                        activeBlocks.Add(BlockFields.SurfaceDescription.Metallic);
                        activeBlocks.Add(HDBlockFields.SurfaceDescription.Anisotropy);
                        activeBlocks.Add(HDBlockFields.SurfaceDescription.Tangent);
                        break;
                    case MaterialType.Iridescence:
                        activeBlocks.Add(BlockFields.SurfaceDescription.Metallic);
                        activeBlocks.Add(HDBlockFields.SurfaceDescription.IridescenceMask);
                        activeBlocks.Add(HDBlockFields.SurfaceDescription.IridescenceThickness);
                        break;
                }

                bool hasThickness = (m_MaterialType == MaterialType.SubsurfaceScattering && m_SSSTransmission) || m_MaterialType == MaterialType.Translucent || activeRefraction;
                if(hasThickness)
                {
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.Thickness);
                }

                if(specularOcclusionMode == SpecularOcclusionMode.Custom)
                {
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.SpecularOcclusion);
                }
                if(activeTransparentDepthPrepass && activeAlphaTest)
                {
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass);
                }
                if(activeTransparentDepthPostpass && activeAlphaTest)
                {
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass);
                }
                if(activeAlphaTestShadow)
                {
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow);
                }
                if(m_SpecularAA)
                {
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance);
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.SpecularAAThreshold);
                }
                if(m_OverrideBakedGI)
                {
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.BakedGI);
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.BakedBackGI);
                }
                if(m_DepthOffset)
                {
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.DepthOffset);
                }
                if(activeRefraction)
                {
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.RefractionIndex);
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.RefractionColor);
                    activeBlocks.Add(HDBlockFields.SurfaceDescription.RefractionDistance);
                }
            }
        }
#endregion

#region Conditional Fields
        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks)
        {
            // TODO: Figure this out...
            // We need this to know if there are any Dots properties active
            // Ideally we do this another way but HDLit needs this for conditional pragmas
            // var shaderProperties = new PropertyCollector();
            // owner.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
            bool hasDotsProperties = false; //shaderProperties.GetDotsInstancingPropertiesCount(GenerationMode.ForReals) > 0;

            return new ConditionalField[]
            {
                // --------------------------------------------------
                // Unlit

                // Features
                new ConditionalField(Fields.GraphVertex,                    blocks.Contains(BlockFields.VertexDescription.Position) ||
                                                                                blocks.Contains(BlockFields.VertexDescription.Normal) ||
                                                                                blocks.Contains(BlockFields.VertexDescription.Tangent)),
                new ConditionalField(Fields.GraphPixel,                     true),

                // Distortion
                new ConditionalField(HDFields.DistortionDepthTest,          m_DistortionDepthTest),
                new ConditionalField(HDFields.DistortionAdd,                m_DistortionMode == DistortionMode.Add),
                new ConditionalField(HDFields.DistortionMultiply,           m_DistortionMode == DistortionMode.Multiply),
                new ConditionalField(HDFields.DistortionReplace,            m_DistortionMode == DistortionMode.Replace),
                new ConditionalField(HDFields.TransparentDistortion,        activeDistortion),
                
                // Misc
                new ConditionalField(Fields.AlphaTest,                      m_AlphaTest && pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold)),
                new ConditionalField(HDFields.AlphaFog,                     m_SurfaceType != SurfaceType.Opaque && m_TransparencyFog),
                new ConditionalField(Fields.VelocityPrecomputed,            m_AddPrecomputedVelocity),
                new ConditionalField(HDFields.EnableShadowMatte,            activeShadowTint),

                // --------------------------------------------------
                // Lit

                // Structs
                new ConditionalField(HDStructFields.FragInputs.IsFrontFace,         m_ShaderType == ShaderType.Lit && doubleSidedMode != DoubleSidedMode.Disabled && !pass.Equals(HDPasses.HDLit.MotionVectors)),

                // Material
                new ConditionalField(HDFields.Anisotropy,                           m_ShaderType == ShaderType.Lit && m_MaterialType == MaterialType.Anisotropy),
                new ConditionalField(HDFields.Iridescence,                          m_ShaderType == ShaderType.Lit && m_MaterialType == MaterialType.Iridescence),
                new ConditionalField(HDFields.SpecularColor,                        m_ShaderType == ShaderType.Lit && m_MaterialType == MaterialType.SpecularColor),
                new ConditionalField(HDFields.Standard,                             m_ShaderType == ShaderType.Lit && m_MaterialType == MaterialType.Standard),
                new ConditionalField(HDFields.SubsurfaceScattering,                 m_ShaderType == ShaderType.Lit && m_MaterialType == MaterialType.SubsurfaceScattering && m_SurfaceType != SurfaceType.Transparent),
                new ConditionalField(HDFields.Translucent,                          m_ShaderType == ShaderType.Lit && m_MaterialType == MaterialType.Translucent),
                new ConditionalField(HDFields.Transmission,                         activeTransmission),

                // Specular Occlusion
                new ConditionalField(HDFields.SpecularOcclusionFromAO,              m_ShaderType == ShaderType.Lit && m_SpecularOcclusionMode == SpecularOcclusionMode.FromAO),
                new ConditionalField(HDFields.SpecularOcclusionFromAOBentNormal,    m_ShaderType == ShaderType.Lit && m_SpecularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal),
                new ConditionalField(HDFields.SpecularOcclusionCustom,              m_ShaderType == ShaderType.Lit && m_SpecularOcclusionMode == SpecularOcclusionMode.Custom),

                // Refraction
                new ConditionalField(HDFields.Refraction,                           activeRefraction),
                new ConditionalField(HDFields.RefractionBox,                        activeRefraction && m_RefractionModel == ScreenSpaceRefraction.RefractionModel.Box),
                new ConditionalField(HDFields.RefractionSphere,                     activeRefraction && m_RefractionModel == ScreenSpaceRefraction.RefractionModel.Sphere),

                // Normal Drop Off Space
                new ConditionalField(Fields.NormalDropOffOS,                        m_NormalDropOffSpace == NormalDropOffSpace.Object),
                new ConditionalField(Fields.NormalDropOffTS,                        m_NormalDropOffSpace == NormalDropOffSpace.Tangent),
                new ConditionalField(Fields.NormalDropOffWS,                        m_NormalDropOffSpace == NormalDropOffSpace.World),

                // Dots
                new ConditionalField(HDFields.DotsInstancing,                       m_ShaderType == ShaderType.Lit && m_DOTSInstancing), // TODO: Why doesnt Unlit have this?
                new ConditionalField(HDFields.DotsProperties,                       hasDotsProperties), // TODO: Why doesnt Unlit have this?

                // Misc
                new ConditionalField(Fields.LodCrossFade,                           activeLodCrossFade),
                new ConditionalField(HDFields.AlphaTestShadow,                      activeAlphaTestShadow && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow)),
                new ConditionalField(HDFields.AlphaTestPrepass,                     m_ShaderType == ShaderType.Lit && activeAlphaTest && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass)),
                new ConditionalField(HDFields.AlphaTestPostpass,                    m_ShaderType == ShaderType.Lit && activeAlphaTest && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass)),
                new ConditionalField(HDFields.BlendPreserveSpecular,                activeBlendPreserveSpecular),
                new ConditionalField(HDFields.TransparentWritesMotionVec,           activeTransparentWritesMotionVec),
                new ConditionalField(HDFields.DisableDecals,                        !activeReceiveDecals),
                new ConditionalField(HDFields.DisableSSR,                           !activeReceiveSSR),
                new ConditionalField(HDFields.DisableSSRTransparent,                !activeReceiveSSRTransparent),
                new ConditionalField(HDFields.SpecularAA,                           activeSpecularAA && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance)),
                new ConditionalField(HDFields.DepthOffset,                          activeDepthOffset && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.DepthOffset)),
                new ConditionalField(HDFields.TransparentBackFace,                  activeBackThenFrontRendering),
                new ConditionalField(HDFields.TransparentDepthPrePass,              activeTransparentDepthPrepass),
                new ConditionalField(HDFields.TransparentDepthPostPass,             activeTransparentDepthPostpass),
                new ConditionalField(HDFields.EnergyConservingSpecular,             activeEnergyConservingSpecular),
                new ConditionalField(HDFields.BentNormal,                           blocks.Contains(HDBlockFields.SurfaceDescription.BentNormal) && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.BentNormal)),
                new ConditionalField(HDFields.AmbientOcclusion,                     blocks.Contains(BlockFields.SurfaceDescription.Occlusion) && pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.Occlusion)),
                new ConditionalField(HDFields.CoatMask,                             blocks.Contains(HDBlockFields.SurfaceDescription.CoatMask) && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.CoatMask)),
                new ConditionalField(HDFields.Tangent,                              blocks.Contains(HDBlockFields.SurfaceDescription.Tangent) && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.Tangent)),
                new ConditionalField(HDFields.LightingGI,                           blocks.Contains(HDBlockFields.SurfaceDescription.BakedGI) && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedGI)),
                new ConditionalField(HDFields.BackLightingGI,                       blocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI) && pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI)),

                // --------------------------------------------------
                // Legacy?
                // TODO: It seems that these are no longer needed. Confirm then remove.

                // Surface Type
                new ConditionalField(Fields.SurfaceOpaque,                          m_ShaderType == ShaderType.Lit && m_SurfaceType == SurfaceType.Opaque),
                new ConditionalField(Fields.SurfaceTransparent,                     m_ShaderType == ShaderType.Lit && m_SurfaceType != SurfaceType.Opaque),

                // Blend Mode
                new ConditionalField(Fields.BlendAdd,                               m_ShaderType == ShaderType.Lit && m_SurfaceType != SurfaceType.Opaque && m_AlphaMode == AlphaMode.Additive),
                new ConditionalField(Fields.BlendAlpha,                             m_ShaderType == ShaderType.Lit && m_SurfaceType != SurfaceType.Opaque && m_AlphaMode == AlphaMode.Alpha),
                new ConditionalField(Fields.BlendPremultiply,                       m_ShaderType == ShaderType.Lit && m_SurfaceType != SurfaceType.Opaque && m_AlphaMode == AlphaMode.Premultiply),

                // Double Sided
                new ConditionalField(HDFields.DoubleSided,                          m_ShaderType == ShaderType.Lit && m_DoubleSidedMode != DoubleSidedMode.Disabled),
                new ConditionalField(HDFields.DoubleSidedFlip,                      m_ShaderType == ShaderType.Lit && m_DoubleSidedMode == DoubleSidedMode.FlippedNormals && !pass.Equals(HDPasses.HDLit.MotionVectors)),
                new ConditionalField(HDFields.DoubleSidedMirror,                    m_ShaderType == ShaderType.Lit && m_DoubleSidedMode == DoubleSidedMode.MirroredNormals && !pass.Equals(HDPasses.HDLit.MotionVectors)),

                // Misc
                new ConditionalField(Fields.VelocityPrecomputed,                    activeVelocityPrecomputed),
            };
        }
#endregion

#region Shader Properties
        public void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
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
                value = (int)m_RenderingPass,
            });

            // See SG-ADDITIONALVELOCITY-NOTE
            if (m_AddPrecomputedVelocity)
            {
                collector.AddShaderProperty(new BooleanShaderProperty
                {
                    value  = true,
                    hidden = true,
                    overrideReferenceName = kAddPrecomputedVelocity,
                });
            }

            if (activeShadowTint)
            {
                uint mantissa = ((uint)LightFeatureFlags.Punctual | (uint)LightFeatureFlags.Directional | (uint)LightFeatureFlags.Area) & 0x007FFFFFu;
                uint exponent = 0b10000000u; // 0 as exponent
                collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    hidden = true,
                    value = HDShadowUtils.Asfloat((exponent << 23) | mantissa),
                    overrideReferenceName = HDMaterialProperties.kShadowMatteFilter
                });
            }

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, activeSplitLighting, activeReceiveSSR, activeReceiveSSRTransparent);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                m_SurfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(m_AlphaMode),
                m_SortPriority,
                m_ZWrite,
                m_TransparentCullMode,
                m_ZTest,
                activeBackThenFrontRendering,
                m_TransparencyFog
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, m_AlphaTest, activeAlphaTestShadow);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, m_DoubleSidedMode);
        }
#endregion

#region Preview Material
        public void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)m_SurfaceType);
            material.SetFloat(kDoubleSidedEnable, m_DoubleSidedMode != DoubleSidedMode.Disabled ? 1.0f : 0.0f);
            material.SetFloat(kDoubleSidedNormalMode, (int)m_DoubleSidedMode);
            material.SetFloat(kAlphaCutoffEnabled, m_AlphaTest ? 1 : 0);
            material.SetFloat(kBlendMode, (int)HDSubShaderUtilities.ConvertAlphaModeToBlendMode(m_AlphaMode));
            material.SetFloat(kEnableFogOnTransparent, m_TransparencyFog ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)m_ZTest);
            material.SetFloat(kTransparentCullMode, (int)m_TransparentCullMode);
            material.SetFloat(kZWrite, m_ZWrite ? 1.0f : 0.0f);

            // No sorting priority for shader graph preview
            material.renderQueue = (int)HDRenderQueue.ChangeType(m_RenderingPass, offset: 0, alphaTest: m_AlphaTest);

            HDUnlitGUI.SetupMaterialKeywordsAndPass(material);
        }
#endregion

#region Settings View
        public VisualElement GetSettings(Action onChange)
        {
            return new HDMeshTargetSettingsView(this, onChange);
        }
#endregion

#region Helpers
        string GetRenderTypeTag()
        {
            switch(m_ShaderType)
            {
                case ShaderType.Unlit:
                    return HDRenderTypeTags.HDUnlitShader.ToString();
                default:
                    return HDRenderTypeTags.HDLitShader.ToString();
            }
        }

        string GetRenderQueueTag()
        {
            if(m_RenderingPass == HDRenderQueue.RenderQueueType.Unknown)
            {
                switch(m_SurfaceType)
                {
                    case SurfaceType.Opaque:
                        m_RenderingPass = HDRenderQueue.RenderQueueType.Opaque;
                        break;
                    case SurfaceType.Transparent:
                        m_RenderingPass = HDRenderQueue.RenderQueueType.Transparent;
                        break;
                }
            }

            int queue = HDRenderQueue.ChangeType(m_RenderingPass, m_SortPriority, m_AlphaTest);
            return HDRenderQueue.GetShaderTagValue(queue);
        }

        int ComputeMaterialNeedsUpdateHash()
        {
            int hash = 0;

            hash |= (m_AlphaTest ? 0 : 1) << 0;
            hash |= (m_AlphaTestShadow ? 0 : 1) << 1;
            hash |= (m_ReceivesSSR ? 0 : 1) << 2;
            hash |= (m_ReceivesSSRTransparent ? 0 : 1) << 3;
            hash |= (activeSplitLighting ? 0 : 1) << 4;

            return hash;
        }
#endregion
    }
}
