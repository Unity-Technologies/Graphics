using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class EyeSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<EyeData>
    {
        public EyeSubTarget() => displayName = "Eye";

        // TODO: remove this line
        public static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template";

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template";
        protected override string customInspector => "Rendering.HighDefinition.EyeGUI";
        protected override string subTargetAssetGuid => "864e4e09d6293cf4d98457f740bb3301";
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Eye;
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl";
        protected override FieldDescriptor subShaderField => HDFields.SubShader.Eye;

        protected override bool supportRaytracing => false;
        protected override bool requireSplitLighting => eyeData.subsurfaceScattering;

        EyeData m_EyeData;

        EyeData IRequiresData<EyeData>.data
        {
            get => m_EyeData;
            set => m_EyeData = value;
        }

        public EyeData eyeData
        {
            get => m_EyeData;
            set => m_EyeData = value;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Eye specific properties
            context.AddField(HDFields.Eye,                                  eyeData.materialType == EyeData.MaterialType.Eye);
            context.AddField(HDFields.EyeCinematic,                         eyeData.materialType == EyeData.MaterialType.EyeCinematic);
            context.AddField(HDFields.SubsurfaceScattering,                 eyeData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Eye specific blocks
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisNormal);
            context.AddBlock(HDBlockFields.SurfaceDescription.IOR);
            context.AddBlock(HDBlockFields.SurfaceDescription.Mask);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash,     eyeData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,           eyeData.subsurfaceScattering);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new EyeSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, eyeData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
            => base.ComputeMaterialNeedsUpdateHash() * 23 + eyeData.subsurfaceScattering.GetHashCode();

// #region SubShaders
//         static class SubShaders
//         {
//             public static SubShaderDescriptor Eye = new SubShaderDescriptor()
//             {
//                 pipelineTag = HDRenderPipeline.k_ShaderTagName,
//                 generatesPreview = true,
//                 passes = new PassCollection
//                 {
//                     { EyePasses.ShadowCaster },
//                     { EyePasses.META },
//                     { EyePasses.SceneSelection },
//                     { EyePasses.DepthForwardOnly },
//                     { EyePasses.MotionVectors },
//                     { EyePasses.ForwardOnly },
//                 },
//             };
//         }
// #endregion

// #region Passes
//         public static class EyePasses
//         {
//             public static PassDescriptor META = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "META",
//                 referenceName = "SHADERPASS_LIGHT_TRANSPORT",
//                 lightMode = "META",
//                 useInPreview = false,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validPixelBlocks = EyeBlockMasks.FragmentMETA,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 requiredFields = CoreRequiredFields.Meta,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.Meta,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 keywords = CoreKeywords.HDBase,
//                 includes = EyeIncludes.Meta,
//             };

//             public static PassDescriptor ShadowCaster = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "ShadowCaster",
//                 referenceName = "SHADERPASS_SHADOWS",
//                 lightMode = "ShadowCaster",
//                 useInPreview = false,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = EyeBlockMasks.FragmentAlphaDepth,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.BlendShadowCaster,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 keywords = CoreKeywords.HDBase,
//                 includes = EyeIncludes.DepthOnly,
//             };

//             public static PassDescriptor SceneSelection = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "SceneSelectionPass",
//                 referenceName = "SHADERPASS_DEPTH_ONLY",
//                 lightMode = "SceneSelectionPass",
//                 useInPreview = false,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = EyeBlockMasks.FragmentAlphaDepth,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.SceneSelection,
//                 pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
//                 defines = CoreDefines.SceneSelection,
//                 keywords = CoreKeywords.HDBase,
//                 includes = EyeIncludes.DepthOnly,
//             };

//             public static PassDescriptor DepthForwardOnly = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "DepthForwardOnly",
//                 referenceName = "SHADERPASS_DEPTH_ONLY",
//                 lightMode = "DepthForwardOnly",
//                 useInPreview = true,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = EyeBlockMasks.FragmentDepthMotionVectors,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 requiredFields = CoreRequiredFields.LitFull,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.DepthOnly,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 defines = CoreDefines.DepthMotionVectors,
//                 keywords = CoreKeywords.DepthMotionVectorsNoNormal,
//                 includes = EyeIncludes.DepthOnly,
//             };

//             public static PassDescriptor MotionVectors = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "MotionVectors",
//                 referenceName = "SHADERPASS_MOTION_VECTORS",
//                 lightMode = "MotionVectors",
//                 useInPreview = false,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = EyeBlockMasks.FragmentDepthMotionVectors,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 requiredFields = CoreRequiredFields.LitFull,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.MotionVectors,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 defines = CoreDefines.DepthMotionVectors,
//                 keywords = CoreKeywords.DepthMotionVectorsNoNormal,
//                 includes = EyeIncludes.MotionVectors,
//             };

//             public static PassDescriptor ForwardOnly = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "ForwardOnly",
//                 referenceName = "SHADERPASS_FORWARD",
//                 lightMode = "ForwardOnly",
//                 useInPreview = true,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = EyeBlockMasks.FragmentForward,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 requiredFields = CoreRequiredFields.LitFull,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.Forward,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 defines = CoreDefines.Forward,
//                 keywords = CoreKeywords.Forward,
//                 includes = EyeIncludes.ForwardOnly,
//             };
//         }
// #endregion

// #region BlockMasks
//         static class EyeBlockMasks
//         {
//             public static BlockFieldDescriptor[] FragmentMETA = new BlockFieldDescriptor[]
//             {
//                 BlockFields.SurfaceDescription.BaseColor,
//                 HDBlockFields.SurfaceDescription.SpecularOcclusion,
//                 BlockFields.SurfaceDescription.NormalTS,
//                 HDBlockFields.SurfaceDescription.IrisNormal,
//                 BlockFields.SurfaceDescription.Smoothness,
//                 HDBlockFields.SurfaceDescription.IOR,
//                 BlockFields.SurfaceDescription.Occlusion,
//                 HDBlockFields.SurfaceDescription.Mask,
//                 HDBlockFields.SurfaceDescription.DiffusionProfileHash,
//                 HDBlockFields.SurfaceDescription.SubsurfaceMask,
//                 BlockFields.SurfaceDescription.Emission,
//                 BlockFields.SurfaceDescription.Alpha,
//                 BlockFields.SurfaceDescription.AlphaClipThreshold,
//             };

//             public static BlockFieldDescriptor[] FragmentAlphaDepth = new BlockFieldDescriptor[]
//             {
//                 BlockFields.SurfaceDescription.Alpha,
//                 BlockFields.SurfaceDescription.AlphaClipThreshold,
//                 HDBlockFields.SurfaceDescription.DepthOffset,
//             };

//             public static BlockFieldDescriptor[] FragmentDepthMotionVectors = new BlockFieldDescriptor[]
//             {
//                 BlockFields.SurfaceDescription.NormalTS,
//                 BlockFields.SurfaceDescription.Smoothness,
//                 BlockFields.SurfaceDescription.Alpha,
//                 BlockFields.SurfaceDescription.AlphaClipThreshold,
//                 HDBlockFields.SurfaceDescription.DepthOffset,
//             };

//             public static BlockFieldDescriptor[] FragmentForward = new BlockFieldDescriptor[]
//             {
//                 BlockFields.SurfaceDescription.BaseColor,
//                 HDBlockFields.SurfaceDescription.SpecularOcclusion,
//                 BlockFields.SurfaceDescription.NormalTS,
//                 HDBlockFields.SurfaceDescription.IrisNormal,
//                 BlockFields.SurfaceDescription.Smoothness,
//                 HDBlockFields.SurfaceDescription.IOR,
//                 BlockFields.SurfaceDescription.Occlusion,
//                 HDBlockFields.SurfaceDescription.Mask,
//                 HDBlockFields.SurfaceDescription.DiffusionProfileHash,
//                 HDBlockFields.SurfaceDescription.SubsurfaceMask,
//                 BlockFields.SurfaceDescription.Emission,
//                 BlockFields.SurfaceDescription.Alpha,
//                 BlockFields.SurfaceDescription.AlphaClipThreshold,
//                 HDBlockFields.SurfaceDescription.BakedGI,
//                 HDBlockFields.SurfaceDescription.BakedBackGI,
//                 HDBlockFields.SurfaceDescription.DepthOffset,
//             };
//         }
// #endregion

// #region Includes
//         static class EyeIncludes
//         {
//             const string kEye = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl";

//             public static IncludeCollection Common = new IncludeCollection
//             {
//                 { CoreIncludes.CorePregraph },
//                 { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
//                 { kEye, IncludeLocation.Pregraph },
//                 { CoreIncludes.CoreUtility },
//                 { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
//                 { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
//             };

//             public static IncludeCollection Meta = new IncludeCollection
//             {
//                 { Common },
//                 { CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph },
//             };

//             public static IncludeCollection DepthOnly = new IncludeCollection
//             {
//                 { Common },
//                 { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
//             };

//             public static IncludeCollection MotionVectors = new IncludeCollection
//             {
//                 { Common },
//                 { CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph },
//             };

//             public static IncludeCollection ForwardOnly = new IncludeCollection
//             {
//                 { CoreIncludes.CorePregraph },
//                 { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
//                 { CoreIncludes.kLighting, IncludeLocation.Pregraph },
//                 { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
//                 { kEye, IncludeLocation.Pregraph },
//                 { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
//                 { CoreIncludes.CoreUtility },
//                 { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
//                 { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
//                 { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
//             };
//         }
// #endregion
    }
}
