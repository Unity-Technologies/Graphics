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

        public static PassDescriptor GenerateDistortionPass(bool supportLighting)
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
                renderStates = GenerateRenderState(),
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = GenerateIncludes(),
            };

            RenderStateCollection GenerateRenderState()
            {
                return new RenderStateCollection
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
                    }) }
                };
            }

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
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

        public static PassDescriptor GenerateSceneSelection(bool supportLighting)
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
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static BlockFieldDescriptor[] FragmentSceneSelection = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.DepthOffset,
        };

#endregion

#region Shadow Caster Pass

        static public PassDescriptor GenerateShadowCaster(bool supportLighting)
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
                includes = GenerateIncludes(),
                requiredFields = supportLighting ? null : UnlitFieldCollection,
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static FieldCollection UnlitFieldCollection = new FieldCollection
        {
            HDFields.SubShader.Unlit,
        };

        public static BlockFieldDescriptor[] FragmentShadowCaster = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,
            HDBlockFields.SurfaceDescription.DepthOffset,
        };

#endregion

#region META pass

        public static PassDescriptor GenerateMETA(bool supportLighting)
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
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
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
            // TODO: We want to only put common fields here, not target specific.
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
            // Eye fields
            HDBlockFields.SurfaceDescription.IrisNormal,
        };

#endregion

#region Depth Forward Only

        public static PassDescriptor GenerateDepthForwardOnlyPass(bool supportLighting)
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
                validPixelBlocks = FragmentDepthOnlyVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = GenerateRequiredFields(),
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = supportLighting ? CoreDefines.DepthMotionVectors : null,
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
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static BlockFieldDescriptor[] FragmentDepthOnlyVectors = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.NormalTS,
            BlockFields.SurfaceDescription.Smoothness,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.DepthOffset,
        };

#endregion

#region Motion Vectors

        public static PassDescriptor GenerateMotionVectors(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Block Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FragmentMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                renderStates = GenerateRenderState(),
                fieldDependencies = CoreFieldDependencies.Default,
                defines = supportLighting ? CoreDefines.DepthMotionVectors : null,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = GenerateKeywords(),
                includes = GenerateIncludes(),
            };

            RenderStateCollection GenerateRenderState()
            {
                var renderState = CoreRenderStates.MotionVectors;
    
                if (!supportLighting)
                {
                    renderState.Add(RenderState.ColorMask("ColorMask [_ColorMaskNormal] 1"));
                    renderState.Add(RenderState.ColorMask("ColorMask 0 2"));
                }

                return renderState;
            }

            KeywordCollection GenerateKeywords()
            {
                var keywords = new KeywordCollection
                {
                    { CoreKeywords.HDBase },
                    { CoreKeywordDescriptors.WriteMsaaDepth },
                    { CoreKeywordDescriptors.AlphaToMask, new FieldCondition(Fields.AlphaToMask, true) },
                };

                if (supportLighting)
                    keywords.Add(CoreKeywordDescriptors.WriteNormalBuffer);
                
                return keywords;
            }

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static BlockFieldDescriptor[] FragmentMotionVectors = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.NormalTS,
            BlockFields.SurfaceDescription.NormalWS,
            BlockFields.SurfaceDescription.NormalOS,
            BlockFields.SurfaceDescription.Smoothness,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.DepthOffset,
        };

#endregion

#region Forward Only

        public static PassDescriptor GenereateForwardOnlyPass(bool supportLighting)
        {
            return new PassDescriptor
            { 
                // Definition
                displayName = "ForwardOnly",
                referenceName = supportLighting ? "SHADERPASS_FORWARD" : "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FragmentForwardOnly,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = supportLighting ? CoreDefines.Forward : null,
                keywords = CoreKeywords.Forward,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kPassForward, IncludeLocation.Postgraph);
                else
                    includes.Add(CoreIncludes.kPassForwardUnlit, IncludeLocation.Postgraph);
 
                return includes;
            }
        }

        public static BlockFieldDescriptor[] FragmentForwardOnly = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.BaseColor,
            HDBlockFields.SurfaceDescription.SpecularOcclusion,
            BlockFields.SurfaceDescription.NormalTS,
            BlockFields.SurfaceDescription.NormalOS,
            BlockFields.SurfaceDescription.NormalWS,
            HDBlockFields.SurfaceDescription.BentNormal,
            BlockFields.SurfaceDescription.Smoothness,
            BlockFields.SurfaceDescription.Occlusion,
            BlockFields.SurfaceDescription.Specular,
            HDBlockFields.SurfaceDescription.DiffusionProfileHash,
            HDBlockFields.SurfaceDescription.SubsurfaceMask,
            HDBlockFields.SurfaceDescription.Thickness,
            HDBlockFields.SurfaceDescription.Tangent,
            HDBlockFields.SurfaceDescription.Anisotropy,
            BlockFields.SurfaceDescription.Emission,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
            HDBlockFields.SurfaceDescription.BakedGI,
            HDBlockFields.SurfaceDescription.BakedBackGI,
            HDBlockFields.SurfaceDescription.DepthOffset,
            HDBlockFields.SurfaceDescription.ShadowTint, // Unlit only field
            // Eye fields
            HDBlockFields.SurfaceDescription.IrisNormal,
            HDBlockFields.SurfaceDescription.IOR,
            HDBlockFields.SurfaceDescription.Mask,
            // Hair only
            HDBlockFields.SurfaceDescription.HairStrandDirection,
            HDBlockFields.SurfaceDescription.Transmittance,
            HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
            HDBlockFields.SurfaceDescription.SpecularTint,
            HDBlockFields.SurfaceDescription.SpecularShift,
            HDBlockFields.SurfaceDescription.SecondarySpecularTint,
            HDBlockFields.SurfaceDescription.SecondarySmoothness,
            HDBlockFields.SurfaceDescription.SecondarySpecularShift,
        };

#endregion

#region Back then front pass

        public static PassDescriptor GenerateBackThenFront(bool supportLighting)
        {
            return new PassDescriptor
            { 
                // Definition
                displayName = "TransparentBackface",
                referenceName = supportLighting ? "SHADERPASS_FORWARD" : "SHADERPASS_FORWARD_UNLIT",
                lightMode = "TransparentBackface",
                useInPreview = true,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FragmentBackThenFront,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitMinimal,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentBackface,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kPassForward, IncludeLocation.Postgraph);
                else
                    includes.Add(CoreIncludes.kPassForwardUnlit, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static BlockFieldDescriptor[] FragmentBackThenFront = new BlockFieldDescriptor[]
        {
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
            HDBlockFields.SurfaceDescription.DepthOffset,
            // Eye fields
            HDBlockFields.SurfaceDescription.IrisNormal,
            HDBlockFields.SurfaceDescription.IOR,
            HDBlockFields.SurfaceDescription.Mask,
            // Hair only
            HDBlockFields.SurfaceDescription.HairStrandDirection,
            HDBlockFields.SurfaceDescription.Transmittance,
            HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
            HDBlockFields.SurfaceDescription.SpecularTint,
            HDBlockFields.SurfaceDescription.SpecularShift,
            HDBlockFields.SurfaceDescription.SecondarySpecularTint,
            HDBlockFields.SurfaceDescription.SecondarySmoothness,
            HDBlockFields.SurfaceDescription.SecondarySpecularShift,
        };

#endregion

#region Transparent Depth Prepass

        public static PassDescriptor GenerateTransparentDepthPrepass(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                useInPreview = true,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FragmentTransparentDepthPrepass,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = TransparentDepthPrepassFields,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = GenerateRenderState(),
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = GenerateDefines(),
                keywords = CoreKeywords.HDBase,
                includes = GenerateIncludes(),
            };

            DefineCollection GenerateDefines()
            {
                var defines = new DefineCollection{ { RayTracingNode.GetRayTracingKeyword(), 0 } };

                if (supportLighting)
                    defines.Add(CoreKeywordDescriptors.WriteNormalBufferDefine, 1, new FieldCondition(HDFields.DisableSSRTransparent, false));

                return defines;
            }

            RenderStateCollection GenerateRenderState()
            {
                var renderState = new RenderStateCollection
                {
                    { RenderState.Blend(Blend.One, Blend.Zero) },
                    { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                    { RenderState.ZWrite(ZWrite.On) },
                    { RenderState.Stencil(new StencilDescriptor()
                    {
                        WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDepth,
                        Ref = CoreRenderStates.Uniforms.stencilRefDepth,
                        Comp = "Always",
                        Pass = "Replace",
                    }) },
                };

                if (!supportLighting)
                {
                    renderState.Add(RenderState.ColorMask("ColorMask [_ColorMaskNormal]"));
                    renderState.Add(RenderState.ColorMask("ColorMask 0 1"));
                }

                return renderState;
            }

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static FieldCollection TransparentDepthPrepassFields = new FieldCollection()
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

#endregion

#region Transparent Depth Postpass

        public static PassDescriptor GenerateTransparentDepthPostpass(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                useInPreview = true,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FragmentTransparentDepthPostpass,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = GenerateRenderState(),
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }

            RenderStateCollection GenerateRenderState()
            {
                var renderState = new RenderStateCollection
                {
                    { RenderState.Blend(Blend.One, Blend.Zero) },
                    { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                    { RenderState.ZWrite(ZWrite.On) },
                    { RenderState.ColorMask("ColorMask 0") },
                };

                return renderState;
            }
        }

        public static BlockFieldDescriptor[] FragmentTransparentDepthPostpass = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.Alpha,
            HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,
            HDBlockFields.SurfaceDescription.DepthOffset,
        };

#endregion

#region Raytracing Indirect

        public static PassDescriptor GenerateRaytracingIndirect(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = RaytracingIndirectFragment,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? GenerateDefines() : null,
                keywords = CoreKeywords.RaytracingIndirect,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RaytracingIndirect },
            };

            DefineCollection GenerateDefines()
            {
                return new DefineCollection
                {
                    { CoreKeywordDescriptors.Shadow, 0 },
                    { RayTracingNode.GetRayTracingKeyword(), 1 },
                    { CoreKeywordDescriptors.HasLightloop, 1 },
                };
            }
        }

        public static BlockFieldDescriptor[] RaytracingIndirectFragment = new BlockFieldDescriptor[]
        {
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
            HDBlockFields.SurfaceDescription.BakedGI,
            HDBlockFields.SurfaceDescription.BakedBackGI,
            HDBlockFields.SurfaceDescription.DepthOffset,
            //Hair blocks:
            HDBlockFields.SurfaceDescription.SpecularTint,
            HDBlockFields.SurfaceDescription.SpecularShift,
            HDBlockFields.SurfaceDescription.SecondarySpecularTint,
            HDBlockFields.SurfaceDescription.SecondarySmoothness,
            HDBlockFields.SurfaceDescription.SecondarySpecularShift,
            HDBlockFields.SurfaceDescription.HairStrandDirection,
            HDBlockFields.SurfaceDescription.Transmittance,
            HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
        };

#endregion

#region Raytracing Visibility

        public static PassDescriptor GenerateRaytracingVisibility(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = RaytracingVisibilityFragment,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? RaytracingVisibilityDefines : null,
                keywords = CoreKeywords.RaytracingVisiblity,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RaytracingVisibility },
                includes = CoreIncludes.Raytracing,
            };
        }

        public static DefineCollection RaytracingVisibilityDefines = new DefineCollection
        {
            { RayTracingNode.GetRayTracingKeyword(), 1 },
        };

        // TODO: we might want to share this with all ray tracing passes
        public static BlockFieldDescriptor[] RaytracingVisibilityFragment = new BlockFieldDescriptor[]
        {
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
            HDBlockFields.SurfaceDescription.BakedGI,
            HDBlockFields.SurfaceDescription.BakedBackGI,
            HDBlockFields.SurfaceDescription.DepthOffset,
            //Hair blocks:
            HDBlockFields.SurfaceDescription.SpecularTint,
            HDBlockFields.SurfaceDescription.SpecularShift,
            HDBlockFields.SurfaceDescription.SecondarySpecularTint,
            HDBlockFields.SurfaceDescription.SecondarySmoothness,
            HDBlockFields.SurfaceDescription.SecondarySpecularShift,
            HDBlockFields.SurfaceDescription.HairStrandDirection,
            HDBlockFields.SurfaceDescription.Transmittance,
            HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
        };

#endregion

#region Raytracing Forward

        public static PassDescriptor GenerateRaytracingForward(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = RaytracingForwardFragment,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? RaytracingForwardDefines : null,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RaytracingForward },
            };
        }

        public static DefineCollection RaytracingForwardDefines = new DefineCollection
        {
            { CoreKeywordDescriptors.Shadow, 0 },
            { RayTracingNode.GetRayTracingKeyword(), 0 },
            { CoreKeywordDescriptors.HasLightloop, 1 },
        };

        public static BlockFieldDescriptor[] RaytracingForwardFragment = new BlockFieldDescriptor[]
        {
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
            HDBlockFields.SurfaceDescription.BakedGI,
            HDBlockFields.SurfaceDescription.BakedBackGI,
            HDBlockFields.SurfaceDescription.DepthOffset,
            //Hair blocks:
            HDBlockFields.SurfaceDescription.SpecularTint,
            HDBlockFields.SurfaceDescription.SpecularShift,
            HDBlockFields.SurfaceDescription.SecondarySpecularTint,
            HDBlockFields.SurfaceDescription.SecondarySmoothness,
            HDBlockFields.SurfaceDescription.SecondarySpecularShift,
            HDBlockFields.SurfaceDescription.HairStrandDirection,
            HDBlockFields.SurfaceDescription.Transmittance,
            HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
        };

#endregion

#region Raytracing GBuffer

        public static PassDescriptor GenerateRaytracingGBuffer(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = RaytracingGBufferFragment,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? RaytracingGBufferDefines : null,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RayTracingGBuffer },
            };
        }

        public static DefineCollection RaytracingGBufferDefines = new DefineCollection
        {
            { CoreKeywordDescriptors.Shadow, 0 },
            { RayTracingNode.GetRayTracingKeyword(), 1 },
        };

        public static BlockFieldDescriptor[] RaytracingGBufferFragment = new BlockFieldDescriptor[]
        {
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
            HDBlockFields.SurfaceDescription.BakedGI,
            HDBlockFields.SurfaceDescription.BakedBackGI,
            HDBlockFields.SurfaceDescription.DepthOffset,
            //Hair blocks:
            HDBlockFields.SurfaceDescription.SpecularTint,
            HDBlockFields.SurfaceDescription.SpecularShift,
            HDBlockFields.SurfaceDescription.SecondarySpecularTint,
            HDBlockFields.SurfaceDescription.SecondarySmoothness,
            HDBlockFields.SurfaceDescription.SecondarySpecularShift,
            HDBlockFields.SurfaceDescription.HairStrandDirection,
            HDBlockFields.SurfaceDescription.Transmittance,
            HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
        };

#endregion

#region Path Tracing

        public static PassDescriptor GeneratePathTracing(bool supportLighting)
        {
            return new PassDescriptor
            {
                //Definition
                displayName = "PathTracingDXR",
                referenceName = "SHADERPASS_PATH_TRACING",
                lightMode = "PathTracingDXR",
                useInPreview = false,

                //Port mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = PathTracingFragment,

                //Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? RaytracingPathTracingDefines : null,
                keywords = CoreKeywords.HDBaseNoCrossFade,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RaytracingPathTracing },
            };
        }

        public static DefineCollection RaytracingPathTracingDefines = new DefineCollection
        {
            { CoreKeywordDescriptors.Shadow, 0 },
            { RayTracingNode.GetRayTracingKeyword(), 0 },
            { CoreKeywordDescriptors.HasLightloop, 1 },
        };

        public static BlockFieldDescriptor[] PathTracingFragment = new BlockFieldDescriptor[]
        {
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
            HDBlockFields.SurfaceDescription.BakedGI,
            HDBlockFields.SurfaceDescription.BakedBackGI,
            HDBlockFields.SurfaceDescription.DepthOffset,
        };

#endregion

    }
}