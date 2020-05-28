using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDShaderPasses
    {
#region Distortion Pass

        public static PassDescriptor GenerateDistortionPass(bool supportsLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                useInPreview = true,

                // Template
                // passTemplatePath = templatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FragmentDistortion,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = new RenderStateCollection
                {
                    { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDFields.DistortionAdd, true) },
                    { RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDFields.DistortionMultiply, true) },
                    { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDFields.DistortionReplace, true) },
                    { RenderState.BlendOp(BlendOp.Add, BlendOp.Add) },
                    { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                    { RenderState.ZWrite(ZWrite.Off) },
                    { RenderState.ZTest(ZTest.Always), new FieldCondition(HDFields.DistortionDepthTest, false) },
                    { RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDFields.DistortionDepthTest, true) },
                    { RenderState.Stencil(new StencilDescriptor() {
                        WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDistortionVec,
                        Ref = CoreRenderStates.Uniforms.stencilRefDistortionVec,
                        Comp = "Always",
                        Pass = "Replace",
                    }) },
                },
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = GenerateDistortionIncludes(),
            };

            IncludeCollection GenerateDistortionIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportsLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportsLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kDisortionVectors, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static BlockFieldDescriptor[] FragmentDistortion = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.Distortion,
            HDBlockFields.SurfaceDescription.DistortionBlur,
        };

#endregion

#region Scene Selection Pass

        public static PassDescriptor GenerateSceneSelectionPass()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FragmentSceneSelection,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV1AndV2EditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = SceneSelectionIncludes,
            };
        }

        public static BlockFieldDescriptor[] FragmentSceneSelection = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.DepthOffset,
        };

        public static IncludeCollection SceneSelectionIncludes = new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
            { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.CoreUtility },
            // We don't need decals for scene selection ?
            // { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
            // { kLitDecalData, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
        };

#endregion

#region Shadow Caster Pass

        static public PassDescriptor GenerateShadowCasterPass(bool supportsLighting)
        {
            return new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FragmentShadowCaster,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.ShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = GenerateShadowCasterIncludes(),
            };

            IncludeCollection GenerateShadowCasterIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportsLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportsLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static BlockFieldDescriptor[] FragmentShadowCaster = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,
            HDBlockFields.SurfaceDescription.DepthOffset,
        };

#endregion

#region META pass

        public static PassDescriptor GenerateMETAPass(bool supportsLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Port Mask
                validPixelBlocks = FragmentMeta,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = GenerateMETAIncludes(),
            };

            IncludeCollection GenerateMETAIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportsLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportsLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static BlockFieldDescriptor[] FragmentMeta = new BlockFieldDescriptor[]
        {
            // TODO: this will probably break with unlit / other target that does not support all of these fields
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.NormalTS,
            BlockFields.SurfaceDescription.NormalWS,
            BlockFields.SurfaceDescription.NormalOS,
            HDBlockFields.SurfaceDescription.BentNormal,
            HDBlockFields.SurfaceDescription.Tangent,
            HDBlockFields.SurfaceDescription.SubsurfaceMask,
            HDBlockFields.SurfaceDescription.Thickness,
            HDBlockFields.SurfaceDescription.DiffusionProfileHash,
            HDBlockFields.SurfaceDescription.IridescenceMask,
            HDBlockFields.SurfaceDescription.IridescenceThickness,
            BlockFields.SurfaceDescription.Specular,
            HDBlockFields.SurfaceDescription.CoatMask,
            BlockFields.SurfaceDescription.Metallic,
            BlockFields.SurfaceDescription.Emission,
            BlockFields.SurfaceDescription.Smoothness,
            BlockFields.SurfaceDescription.Occlusion,
            HDBlockFields.SurfaceDescription.SpecularOcclusion,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.Anisotropy,
            HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
            HDBlockFields.SurfaceDescription.SpecularAAThreshold,
            HDBlockFields.SurfaceDescription.RefractionIndex,
            HDBlockFields.SurfaceDescription.RefractionColor,
            HDBlockFields.SurfaceDescription.RefractionDistance,
        };

#endregion

#region Depth Forward Only

        public static PassDescriptor GenerateDepthForwardOnly(bool supportsLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = GenerateRequiredFields(),
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = GenerateIncludes(),
            };

            FieldCollection GenerateRequiredFields()
            {
                return new FieldCollection()
                {
                    HDStructFields.AttributesMesh.normalOS,
                    HDStructFields.AttributesMesh.tangentOS,
                    HDStructFields.AttributesMesh.uv0,
                    HDStructFields.AttributesMesh.uv1,
                    HDStructFields.AttributesMesh.color,
                    HDStructFields.AttributesMesh.uv2,
                    HDStructFields.AttributesMesh.uv3,
                    HDStructFields.FragInputs.tangentToWorld,
                    HDStructFields.FragInputs.positionRWS,
                    HDStructFields.FragInputs.texCoord1,
                    HDStructFields.FragInputs.texCoord2,
                    HDStructFields.FragInputs.texCoord3,
                    HDStructFields.FragInputs.color,
                };
            }

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportsLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportsLighting)
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static BlockFieldDescriptor[] FragmentDepthMotionVectors = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.NormalTS,
            BlockFields.SurfaceDescription.Smoothness,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.DepthOffset,
        };

#endregion

    }
}