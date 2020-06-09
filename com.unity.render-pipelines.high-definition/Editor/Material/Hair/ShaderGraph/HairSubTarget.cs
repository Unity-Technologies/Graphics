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
    sealed partial class HairSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<HairData>
    {
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template";
        protected override string customInspector => "Rendering.HighDefinition.HairGUI";
        protected override string subTargetAssetGuid => "7e681cc79dd8e6c46ba1e8412d519e26"; // HairSubTarget.cs
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Hair;

        public HairSubTarget() => displayName = "Hair";

        HairData m_HairData;

        HairData IRequiresData<HairData>.data
        {
            get => m_HairData;
            set => m_HairData = value;
        }

        public HairData hairData
        {
            get => m_HairData;
            set => m_HairData = value;
        }

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            yield return SubShaders.Hair;
            yield return SubShaders.HairRaytracing;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // TODO: move this elsewhere:
            // Make sure that we don't end up in an unsupported configuration
            lightingData.subsurfaceScattering = false;

            base.GetFields(ref context);

            var descs = context.blocks.Select(x => x.descriptor);
            // Hair specific properties:
            context.AddField(HDStructFields.FragInputs.IsFrontFace,         systemData.doubleSidedMode != DoubleSidedMode.Disabled && !context.pass.Equals(HairSubTarget.HairPasses.MotionVectors));
            context.AddField(HDFields.KajiyaKay,                            hairData.materialType == HairData.MaterialType.KajiyaKay);
            context.AddField(HDFields.HairStrandDirection,                  descs.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection));
            context.AddField(HDFields.RimTransmissionIntensity,             descs.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity));
            context.AddField(HDFields.UseLightFacingNormal,                 hairData.useLightFacingNormal);
            context.AddField(HDFields.Transmittance,                        descs.Contains(HDBlockFields.SurfaceDescription.Transmittance) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.Transmittance));

            // All the DoAlphaXXX field drive the generation of which code to use for alpha test in the template
            // Do alpha test only if we aren't using the TestShadow one
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && (context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) &&
                                                                                !(builtinData.alphaTestShadow && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow))));

            // Misc
            context.AddField(HDFields.SpecularAA,                           lightingData.specularAA &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Hair specific blocks
            context.AddBlock(HDBlockFields.SurfaceDescription.BentNormal);
            context.AddBlock(HDBlockFields.SurfaceDescription.Transmittance);
            context.AddBlock(HDBlockFields.SurfaceDescription.RimTransmissionIntensity);
            context.AddBlock(HDBlockFields.SurfaceDescription.HairStrandDirection);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularTint);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularShift);
            context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySpecularTint);
            context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySmoothness);
            context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySpecularShift);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new SurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit));
            blockList.AddPropertyBlock(new HairAdvancedOptionsPropertyBlock(hairData));
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Hair = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { HairPasses.ShadowCaster },
                    { HairPasses.META },
                    { HairPasses.SceneSelection },
                    { HairPasses.DepthForwardOnly },
                    { HairPasses.MotionVectors },
                    { HairPasses.TransparentBackface,       new FieldCondition(HDFields.TransparentBackFace, true) },
                    { HairPasses.TransparentDepthPrepass,   new FieldCondition[]{
                                                            new FieldCondition(HDFields.TransparentDepthPrePass, true),
                                                            new FieldCondition(HDFields.DisableSSRTransparent, true) }},
                    { HairPasses.TransparentDepthPrepass,   new FieldCondition[]{
                                                            new FieldCondition(HDFields.TransparentDepthPrePass, true),
                                                            new FieldCondition(HDFields.DisableSSRTransparent, false) }},
                    { HairPasses.TransparentDepthPrepass,   new FieldCondition[]{
                                                            new FieldCondition(HDFields.TransparentDepthPrePass, false),
                                                            new FieldCondition(HDFields.DisableSSRTransparent, false) }},
                    { HairPasses.ForwardOnly },
                    { HairPasses.TransparentDepthPostpass, new FieldCondition(HDFields.TransparentDepthPostPass, true) },
                },
            };

            public static SubShaderDescriptor HairRaytracing = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = false,
                passes = new PassCollection
                {
                    { HairPasses.RaytracingIndirect, new FieldCondition(Fields.IsPreview, false) },
                    { HairPasses.RaytracingVisibility, new FieldCondition(Fields.IsPreview, false) },
                    { HairPasses.RaytracingForward, new FieldCondition(Fields.IsPreview, false) },
                    { HairPasses.RaytracingGBuffer, new FieldCondition(Fields.IsPreview, false) },
                    { HairPasses.RaytracingSubSurface, new FieldCondition(Fields.IsPreview, false) },
                },
            };
        }
#endregion

#region Passes
        public static class HairPasses
        {
            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validPixelBlocks = HairBlockMasks.FragmentMETA,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.Meta,
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentShadowCaster,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.BlendShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = HairIncludes.DepthOnly,
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = HairRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = HairIncludes.MotionVectors,
            };

            public static PassDescriptor TransparentDepthPrepass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentTransparentDepthPrepass,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentDepthPrePass,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.TransparentDepthPrepass,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
            };

            public static PassDescriptor TransparentBackface = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "TransparentBackface",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentTransparentBackface,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitMinimal,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentBackface,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = HairIncludes.ForwardOnly,
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.ForwardColorMask,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = HairIncludes.ForwardOnly,
            };

            public static PassDescriptor TransparentDepthPostpass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentTransparentDepthPostpass,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentDepthPostPass,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
            };

            public static PassDescriptor RaytracingIndirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = HairDefines.RaytracingIndirect,
                keywords = CoreKeywords.RaytracingIndirect,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RaytracingIndirect },
            };

            public static PassDescriptor RaytracingVisibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.RaytracingVisiblity,
                defines = HairDefines.RaytracingVisibility,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RaytracingVisibility },
            };

            public static PassDescriptor RaytracingForward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = HairDefines.RaytracingForward,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RaytracingForward },
            };

            public static PassDescriptor RaytracingGBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = HairDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RayTracingGBuffer },
            };

            public static PassDescriptor RaytracingSubSurface = new PassDescriptor()
            {
                //Definition
                displayName = "SubSurfaceDXR",
                referenceName = "SHADERPASS_RAYTRACING_SUB_SURFACE",
                lightMode = "SubSurfaceDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = HairBlockMasks.FragmentForward,

                //Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = HairDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RaytracingSubSurface },
            };
        }
#endregion

#region BlockMasks
        static class HairBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentMETA = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.HairStrandDirection,
                HDBlockFields.SurfaceDescription.Transmittance,
                HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.SpecularTint,
                HDBlockFields.SurfaceDescription.SpecularShift,
                HDBlockFields.SurfaceDescription.SecondarySpecularTint,
                HDBlockFields.SurfaceDescription.SecondarySmoothness,
                HDBlockFields.SurfaceDescription.SecondarySpecularShift,
            };

            public static BlockFieldDescriptor[] FragmentShadowCaster = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentAlphaDepth = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentDepthMotionVectors = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentTransparentDepthPrepass = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,
                HDBlockFields.SurfaceDescription.DepthOffset,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.Smoothness,
            };

            public static BlockFieldDescriptor[] FragmentTransparentBackface = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.HairStrandDirection,
                HDBlockFields.SurfaceDescription.Transmittance,
                HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.SpecularTint,
                HDBlockFields.SurfaceDescription.SpecularShift,
                HDBlockFields.SurfaceDescription.SecondarySpecularTint,
                HDBlockFields.SurfaceDescription.SecondarySmoothness,
                HDBlockFields.SurfaceDescription.SecondarySpecularShift,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentForward = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.HairStrandDirection,
                HDBlockFields.SurfaceDescription.Transmittance,
                HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.SpecularTint,
                HDBlockFields.SurfaceDescription.SpecularShift,
                HDBlockFields.SurfaceDescription.SecondarySpecularTint,
                HDBlockFields.SurfaceDescription.SecondarySmoothness,
                HDBlockFields.SurfaceDescription.SecondarySpecularShift,
                HDBlockFields.SurfaceDescription.BakedGI,
                HDBlockFields.SurfaceDescription.BakedBackGI,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentTransparentDepthPostpass = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };
        }
#endregion

#region RenderStates
        static class HairRenderStates
        {
            public static RenderStateCollection MotionVectors = new RenderStateCollection
            {
                { RenderState.AlphaToMask(CoreRenderStates.Uniforms.alphaToMask), new FieldCondition(Fields.AlphaToMask, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskMV,
                    Ref = CoreRenderStates.Uniforms.stencilRefMV,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };
        }
#endregion

#region Defines
        static class HairDefines
        {
            public static DefineCollection RaytracingForward = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 0 },
                { CoreKeywordDescriptors.HasLightloop, 1 },
            };

            public static DefineCollection RaytracingIndirect = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 1 },
                { CoreKeywordDescriptors.HasLightloop, 1 },
            };

            public static DefineCollection RaytracingVisibility = new DefineCollection
            {
                { RayTracingNode.GetRayTracingKeyword(), 1 },
            };

            public static DefineCollection RaytracingGBuffer = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 1 },
            };

            public static DefineCollection RaytracingPathTracing = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 0 },
                { CoreKeywordDescriptors.HasLightloop, 1 },
            };
        }
#endregion

#region Includes
        static class HairIncludes
        {
            public static IncludeCollection Common = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kHair, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            };

            public static IncludeCollection Meta = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph },
            };

            public static IncludeCollection DepthOnly = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
            };

            public static IncludeCollection MotionVectors = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection ForwardOnly = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kLighting, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
                { CoreIncludes.kHair, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
