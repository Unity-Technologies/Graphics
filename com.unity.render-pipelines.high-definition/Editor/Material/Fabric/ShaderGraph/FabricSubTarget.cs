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
    sealed partial class FabricSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<FabricData>
    {
        public FabricSubTarget() => displayName = "Fabric";

        // TODO: remove this line
        public static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template";

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template";
        protected override string subTargetAssetGuid => "74f1a4749bab90d429ac01d094be0aeb"; // FabricSubTarget.cs
        protected override string customInspector => "Rendering.HighDefinition.FabricGUI";
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Fabric;
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl";
        protected override FieldDescriptor subShaderField => HDFields.SubShader.Fabric;
        protected override bool requireSplitLighting => fabricData.subsurfaceScattering;

        FabricData m_FabricData;

        FabricData IRequiresData<FabricData>.data
        {
            get => m_FabricData;
            set => m_FabricData = value;
        }

        public FabricData fabricData
        {
            get => m_FabricData;
            set => m_FabricData = value;
        }

        protected override SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            var descriptor = base.GetRaytracingSubShaderDescriptor();

            if (fabricData.subsurfaceScattering)
                descriptor.passes.Add(HDShaderPasses.GenerateRaytracingSubsurface());
 
            return descriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Fabric specific properties
            context.AddField(HDFields.CottonWool,                           fabricData.materialType == FabricData.MaterialType.CottonWool);
            context.AddField(HDFields.Silk,                                 fabricData.materialType == FabricData.MaterialType.Silk);
            context.AddField(HDFields.SubsurfaceScattering,                 fabricData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(HDFields.Transmission,                         fabricData.transmission);
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
            context.AddField(HDFields.EnergyConservingSpecular,             fabricData.energyConservingSpecular);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Fabric specific blocks
            context.AddBlock(BlockFields.SurfaceDescription.Specular);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, fabricData.subsurfaceScattering || fabricData.transmission);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,       fabricData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness,            fabricData.transmission);

            // Fabric Silk
            if(fabricData.materialType == FabricData.MaterialType.Silk)
            {
                context.AddBlock(HDBlockFields.SurfaceDescription.Tangent);
                context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy);
            }
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new FabricSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, fabricData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
            => base.ComputeMaterialNeedsUpdateHash() * 23 + fabricData.subsurfaceScattering.GetHashCode();

// #region SubShaders
//         static class SubShaders
//         {
//             public static SubShaderDescriptor Fabric = new SubShaderDescriptor()
//             {
//                 pipelineTag = HDRenderPipeline.k_ShaderTagName,
//                 generatesPreview = true,
//                 passes = new PassCollection
//                 {
//                     { FabricPasses.ShadowCaster },
//                     { FabricPasses.META },
//                     { FabricPasses.SceneSelection },
//                     { FabricPasses.DepthForwardOnly },
//                     { FabricPasses.MotionVectors },
//                     { FabricPasses.TransparentDepthPrepass, new FieldCondition[]{
//                                                             new FieldCondition(HDFields.TransparentDepthPrePass, true),
//                                                             new FieldCondition(HDFields.DisableSSRTransparent, true) }},
//                     { FabricPasses.TransparentDepthPrepass, new FieldCondition[]{
//                                                             new FieldCondition(HDFields.TransparentDepthPrePass, true),
//                                                             new FieldCondition(HDFields.DisableSSRTransparent, false) }},
//                     { FabricPasses.TransparentDepthPrepass, new FieldCondition[]{
//                                                             new FieldCondition(HDFields.TransparentDepthPrePass, false),
//                                                             new FieldCondition(HDFields.DisableSSRTransparent, false) }},
//                     { FabricPasses.ForwardOnly },
//                     { FabricPasses.TransparentDepthPostpass, new FieldCondition(HDFields.TransparentDepthPostPass, true) },
//                 },
//             };

//             public static SubShaderDescriptor FabricRaytracing = new SubShaderDescriptor()
//             {
//                 pipelineTag = HDRenderPipeline.k_ShaderTagName,
//                 generatesPreview = false,
//                 passes = new PassCollection
//                 {
//                     { FabricPasses.RaytracingIndirect, new FieldCondition(Fields.IsPreview, false) },
//                     { FabricPasses.RaytracingVisibility, new FieldCondition(Fields.IsPreview, false) },
//                     { FabricPasses.RaytracingForward, new FieldCondition(Fields.IsPreview, false) },
//                     { FabricPasses.RaytracingGBuffer, new FieldCondition(Fields.IsPreview, false) },
//                     { FabricPasses.RaytracingSubSurface, new FieldCondition(Fields.IsPreview, false) },
//                 },
//             };
//         }
// #endregion

// #region Passes
//         public static class FabricPasses
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
//                 validPixelBlocks = FabricBlockMasks.FragmentMETA,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 requiredFields = CoreRequiredFields.Meta,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.Meta,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 keywords = CoreKeywords.HDBase,
//                 includes = FabricIncludes.Meta,
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
//                 validPixelBlocks = FabricBlockMasks.FragmentAlphaDepth,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.BlendShadowCaster,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 keywords = CoreKeywords.HDBase,
//                 includes = FabricIncludes.DepthOnly,
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
//                 validPixelBlocks = FabricBlockMasks.FragmentAlphaDepth,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.ShadowCaster,
//                 pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
//                 defines = CoreDefines.SceneSelection,
//                 keywords = CoreKeywords.HDBase,
//                 includes = FabricIncludes.DepthOnly,
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
//                 validPixelBlocks = FabricBlockMasks.FragmentDepthMotionVectors,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 requiredFields = CoreRequiredFields.LitFull,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.DepthOnly,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 defines = CoreDefines.DepthMotionVectors,
//                 keywords = CoreKeywords.DepthMotionVectorsNoNormal,
//                 includes = FabricIncludes.DepthOnly,
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
//                 validPixelBlocks = FabricBlockMasks.FragmentDepthMotionVectors,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 requiredFields = CoreRequiredFields.LitFull,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.MotionVectors,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 defines = CoreDefines.DepthMotionVectors,
//                 keywords = CoreKeywords.DepthMotionVectorsNoNormal,
//                 includes = FabricIncludes.MotionVectors,
//             };

//             public static PassDescriptor TransparentDepthPrepass = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "TransparentDepthPrepass",
//                 referenceName = "SHADERPASS_DEPTH_ONLY",
//                 lightMode = "TransparentDepthPrepass",
//                 useInPreview = true,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = FabricBlockMasks.FragmentTransparentDepthPrepass,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 requiredFields = CoreRequiredFields.LitFull,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.TransparentDepthPrePass,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 defines = CoreDefines.TransparentDepthPrepass,
//                 keywords = CoreKeywords.HDBase,
//                 includes = FabricIncludes.DepthOnly,
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
//                 validPixelBlocks = FabricBlockMasks.FragmentForward,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 requiredFields = CoreRequiredFields.LitFull,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.Forward,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 defines = CoreDefines.Forward,
//                 keywords = CoreKeywords.Forward,
//                 includes = FabricIncludes.ForwardOnly,
//             };

//             public static PassDescriptor TransparentDepthPostpass = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "TransparentDepthPostpass",
//                 referenceName = "SHADERPASS_DEPTH_ONLY",
//                 lightMode = "TransparentDepthPostpass",
//                 useInPreview = true,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = FabricBlockMasks.FragmentTransparentDepthPostpass,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 renderStates = CoreRenderStates.TransparentDepthPostPass,
//                 pragmas = CorePragmas.DotsInstancedInV2Only,
//                 defines = CoreDefines.ShaderGraphRaytracingHigh,
//                 keywords = CoreKeywords.HDBase,
//                 includes = FabricIncludes.DepthOnly,
//             };

//             public static PassDescriptor RaytracingIndirect = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "IndirectDXR",
//                 referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
//                 lightMode = "IndirectDXR",
//                 useInPreview = false,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = FabricBlockMasks.FragmentForward,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 pragmas = CorePragmas.RaytracingBasic,
//                 defines = FabricDefines.RaytracingIndirect,
//                 keywords = CoreKeywords.RaytracingIndirect,
//                 includes = CoreIncludes.Raytracing,
//                 requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingIndirect },
//             };

//             public static PassDescriptor RaytracingVisibility = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "VisibilityDXR",
//                 referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
//                 lightMode = "VisibilityDXR",
//                 useInPreview = false,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = FabricBlockMasks.FragmentForward,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 pragmas = CorePragmas.RaytracingBasic,
//                 defines = FabricDefines.RaytracingVisibility,
//                 keywords = CoreKeywords.RaytracingVisiblity,
//                 includes = CoreIncludes.Raytracing,
//                 requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingVisibility },
//             };

//             public static PassDescriptor RaytracingForward = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "ForwardDXR",
//                 referenceName = "SHADERPASS_RAYTRACING_FORWARD",
//                 lightMode = "ForwardDXR",
//                 useInPreview = false,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = FabricBlockMasks.FragmentForward,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 pragmas = CorePragmas.RaytracingBasic,
//                 defines = FabricDefines.RaytracingForward,
//                 keywords = CoreKeywords.RaytracingGBufferForward,
//                 includes = CoreIncludes.Raytracing,
//                 requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingForward },
//             };

//             public static PassDescriptor RaytracingGBuffer = new PassDescriptor()
//             {
//                 // Definition
//                 displayName = "GBufferDXR",
//                 referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
//                 lightMode = "GBufferDXR",
//                 useInPreview = false,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 // Port Mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = FabricBlockMasks.FragmentForward,

//                 // Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 pragmas = CorePragmas.RaytracingBasic,
//                 defines = FabricDefines.RaytracingGBuffer,
//                 keywords = CoreKeywords.RaytracingGBufferForward,
//                 includes = CoreIncludes.Raytracing,
//                 requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RayTracingGBuffer },
//             };

//             public static PassDescriptor RaytracingSubSurface = new PassDescriptor()
//             {
//                 //Definition
//                 displayName = "SubSurfaceDXR",
//                 referenceName = "SHADERPASS_RAYTRACING_SUB_SURFACE",
//                 lightMode = "SubSurfaceDXR",
//                 useInPreview = false,

//                 // Template
//                 passTemplatePath = passTemplatePath,
//                 sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

//                 //Port mask
//                 validVertexBlocks = CoreBlockMasks.Vertex,
//                 validPixelBlocks = FabricBlockMasks.FragmentForward,

//                 //Collections
//                 structs = CoreStructCollections.Default,
//                 fieldDependencies = CoreFieldDependencies.Default,
//                 pragmas = CorePragmas.RaytracingBasic,
//                 defines = FabricDefines.RaytracingGBuffer,
//                 keywords = CoreKeywords.RaytracingGBufferForward,
//                 includes = CoreIncludes.Raytracing,
//                 requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingSubSurface },
//             };
//         }
// #endregion

// #region BlockMasks
//         static class FabricBlockMasks
//         {
//             public static BlockFieldDescriptor[] FragmentMETA = new BlockFieldDescriptor[]
//             {
//                 BlockFields.SurfaceDescription.BaseColor,
//                 HDBlockFields.SurfaceDescription.SpecularOcclusion,
//                 BlockFields.SurfaceDescription.NormalTS,
//                 BlockFields.SurfaceDescription.NormalWS,
//                 BlockFields.SurfaceDescription.NormalOS,
//                 BlockFields.SurfaceDescription.Smoothness,
//                 BlockFields.SurfaceDescription.Occlusion,
//                 BlockFields.SurfaceDescription.Specular,
//                 HDBlockFields.SurfaceDescription.DiffusionProfileHash,
//                 HDBlockFields.SurfaceDescription.SubsurfaceMask,
//                 HDBlockFields.SurfaceDescription.Thickness,
//                 HDBlockFields.SurfaceDescription.Tangent,
//                 HDBlockFields.SurfaceDescription.Anisotropy,
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
//                 BlockFields.SurfaceDescription.NormalWS,
//                 BlockFields.SurfaceDescription.NormalOS,
//                 BlockFields.SurfaceDescription.Smoothness,
//                 BlockFields.SurfaceDescription.Alpha,
//                 BlockFields.SurfaceDescription.AlphaClipThreshold,
//                 HDBlockFields.SurfaceDescription.DepthOffset,
//             };

//             public static BlockFieldDescriptor[] FragmentTransparentDepthPrepass = new BlockFieldDescriptor[]
//             {
//                 BlockFields.SurfaceDescription.Alpha,
//                 HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,
//                 HDBlockFields.SurfaceDescription.DepthOffset,
//                 BlockFields.SurfaceDescription.NormalTS,
//                 BlockFields.SurfaceDescription.NormalWS,
//                 BlockFields.SurfaceDescription.NormalOS,
//                 BlockFields.SurfaceDescription.Smoothness,
//             };

//             public static BlockFieldDescriptor[] FragmentForward = new BlockFieldDescriptor[]
//             {
//                 BlockFields.SurfaceDescription.BaseColor,
//                 HDBlockFields.SurfaceDescription.SpecularOcclusion,
//                 BlockFields.SurfaceDescription.NormalTS,
//                 BlockFields.SurfaceDescription.NormalWS,
//                 BlockFields.SurfaceDescription.NormalOS,
//                 HDBlockFields.SurfaceDescription.BentNormal,
//                 BlockFields.SurfaceDescription.Smoothness,
//                 BlockFields.SurfaceDescription.Occlusion,
//                 BlockFields.SurfaceDescription.Specular,
//                 HDBlockFields.SurfaceDescription.DiffusionProfileHash,
//                 HDBlockFields.SurfaceDescription.SubsurfaceMask,
//                 HDBlockFields.SurfaceDescription.Thickness,
//                 HDBlockFields.SurfaceDescription.Tangent,
//                 HDBlockFields.SurfaceDescription.Anisotropy,
//                 BlockFields.SurfaceDescription.Emission,
//                 BlockFields.SurfaceDescription.Alpha,
//                 BlockFields.SurfaceDescription.AlphaClipThreshold,
//                 HDBlockFields.SurfaceDescription.BakedGI,
//                 HDBlockFields.SurfaceDescription.BakedBackGI,
//                 HDBlockFields.SurfaceDescription.DepthOffset,
//             };

//             public static BlockFieldDescriptor[] FragmentTransparentDepthPostpass = new BlockFieldDescriptor[]
//             {
//                 BlockFields.SurfaceDescription.Alpha,
//                 HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,
//                 HDBlockFields.SurfaceDescription.DepthOffset,
//             };
//         }
// #endregion

// #region Defines
//         static class FabricDefines
//         {
//             public static DefineCollection RaytracingForward = new DefineCollection
//             {
//                 { CoreKeywordDescriptors.Shadow, 0 },
//                 { RayTracingNode.GetRayTracingKeyword(), 0 },
//                 { CoreKeywordDescriptors.HasLightloop, 1 },
//             };

//             public static DefineCollection RaytracingIndirect = new DefineCollection
//             {
//                 { CoreKeywordDescriptors.Shadow, 0 },
//                 { RayTracingNode.GetRayTracingKeyword(), 1 },
//                 { CoreKeywordDescriptors.HasLightloop, 1 },
//             };

//             public static DefineCollection RaytracingVisibility = new DefineCollection
//             {
//                 { RayTracingNode.GetRayTracingKeyword(), 1 },
//             };

//             public static DefineCollection RaytracingGBuffer = new DefineCollection
//             {
//                 { CoreKeywordDescriptors.Shadow, 0 },
//                 { RayTracingNode.GetRayTracingKeyword(), 1 },
//             };

//             public static DefineCollection RaytracingPathTracing = new DefineCollection
//             {
//                 { CoreKeywordDescriptors.Shadow, 0 },
//                 { RayTracingNode.GetRayTracingKeyword(), 0 },
//                 { CoreKeywordDescriptors.HasLightloop, 1 },
//             };
//         }
// #endregion

// #region Includes
        // static class FabricIncludes
        // {
        //     const string kFabric = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl";

        //     public static IncludeCollection Common = new IncludeCollection
        //     {
        //         { CoreIncludes.CorePregraph },
        //         { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
        //         { kFabric, IncludeLocation.Pregraph },
        //         { CoreIncludes.CoreUtility },
        //         { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
        //         { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
        //     };

        //     public static IncludeCollection Meta = new IncludeCollection
        //     {
        //         { Common },
        //         { CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph },
        //     };

        //     public static IncludeCollection DepthOnly = new IncludeCollection
        //     {
        //         { Common },
        //         { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
        //     };

        //     public static IncludeCollection MotionVectors = new IncludeCollection
        //     {
        //         { Common },
        //         { CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph },
        //     };

        //     public static IncludeCollection ForwardOnly = new IncludeCollection
        //     {
        //         { CoreIncludes.CorePregraph },
        //         { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
        //         { CoreIncludes.kLighting, IncludeLocation.Pregraph },
        //         { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
        //         { kFabric, IncludeLocation.Pregraph },
        //         { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
        //         { CoreIncludes.CoreUtility },
        //         { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
        //         { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
        //         { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
        //     };
        // }
// #endregion
    }
}
