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

                // Collections
                renderStates = GenerateRenderState(),
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingDefault,
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


#endregion

#region Scene Picking Pass

        public static PassDescriptor GenerateScenePicking()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Collections
                renderStates = CoreRenderStates.ScenePicking,
                pragmas = CorePragmas.DotsInstancedInV1AndV2EditorSync,
                defines = CoreDefines.ScenePicking,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPickingSpaceTransforms, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

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

                // Collections
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV1AndV2EditorSync,
                defines = CoreDefines.SceneSelection,
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

                validPixelBlocks  = new BlockFieldDescriptor[]
                {
                    BlockFields.SurfaceDescription.Alpha,
                    BlockFields.SurfaceDescription.AlphaClipThreshold,
                    HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,
                    HDBlockFields.SurfaceDescription.DepthOffset,
                },

                // Collections
                renderStates = CoreRenderStates.ShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
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

                // We don't need any vertex inputs on meta pass:
                validVertexBlocks = new BlockFieldDescriptor[0],

                // Collections
                requiredFields = CoreRequiredFields.Meta,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingDefault,
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

                // Collections
                requiredFields = GenerateRequiredFields(),
                renderStates = GenerateRenderState(),
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = supportLighting ? CoreDefines.DepthForwardOnly : CoreDefines.DepthForwardOnlyUnlit,
                includes = GenerateIncludes(),
            };

            RenderStateCollection GenerateRenderState()
            {
                var renderState = new RenderStateCollection{ CoreRenderStates.DepthOnly };

                if (!supportLighting)
                {
                    // Caution: When using MSAA we have normal and depth buffer bind.
                    // Unlit objects need to NOT write in normal buffer (or write 0) - Disable color mask for this RT
                    // Note: ShaderLab doesn't allow to have a variable on the second parameter of ColorMask
                    // - When MSAA: disable target 1 (normal buffer)
                    // - When no MSAA: disable target 0 (normal buffer) and 1 (unused)
                    renderState.Add(RenderState.ColorMask("ColorMask [_ColorMaskNormal]"));
                    renderState.Add(RenderState.ColorMask("ColorMask 0 1"));
                }

                return renderState;
            }

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

#endregion

#region Motion Vectors

        public static PassDescriptor GenerateMotionVectors(bool supportLighting, bool supportForward)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Collections
                requiredFields = CoreRequiredFields.LitFull,
                renderStates = GenerateRenderState(),
                defines = GenerateDefines(),
                pragmas = CorePragmas.DotsInstancedInV2Only,
                includes = GenerateIncludes(),
            };

            DefineCollection GenerateDefines()
            {
                if (!supportLighting)
                    return null;

                var defines = new DefineCollection { Defines.raytracingDefault };

                //  #define WRITE_NORMAL_BUFFER for motion vector in forward case
                // if (supportForward)
                // {
                //     defines.Add(CoreKeywordDescriptors.WriteNormalBuffer, 1);
                // }                    
                
                return defines;
            }

            RenderStateCollection GenerateRenderState()
            {
                var renderState = new RenderStateCollection();
                renderState.Add(CoreRenderStates.MotionVectors);
    
                if (!supportLighting)
                {
                    // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
                    // Unlit objects need to NOT write in normal buffer (or write 0) - Disable color mask for this RT
                    // Note: ShaderLab doesn't allow to have a variable on the second parameter of ColorMask
                    // - When MSAA: disable target 2 (normal buffer)
                    // - When no MSAA: disable target 1 (normal buffer) and 2 (unused)
                    renderState.Add(RenderState.ColorMask("ColorMask [_ColorMaskNormal] 1"));
                    renderState.Add(RenderState.ColorMask("ColorMask 0 2"));
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
                includes.Add(CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph);

                return includes;
            }
        }


#endregion

#region Forward Only

        public static PassDescriptor GenerateForwardOnlyPass(bool supportLighting)
        {
            return new PassDescriptor
            { 
                // Definition
                displayName = "ForwardOnly",
                referenceName = supportLighting ? "SHADERPASS_FORWARD" : "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Collections
                requiredFields = GenerateRequiredFields(),
                renderStates = CoreRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = supportLighting ? CoreDefines.Forward : CoreDefines.ForwardUnlit,
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
            };

            FieldCollection GenerateRequiredFields()
            {
                if (supportLighting)
                    return CoreRequiredFields.LitFull;
                else
                {
                    return new FieldCollection
                    {
                        // TODO: add preprocessor protection for this interpolator: _TRANSPARENT_WRITES_MOTION_VEC
                        HDStructFields.FragInputs.positionRWS,
                    };
                }
            }

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

                // Collections
                requiredFields = CoreRequiredFields.LitMinimal,
                renderStates = CoreRenderStates.TransparentBackface,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.BackThenFront,
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

#endregion

#region Transparent Depth Prepass

        public static PassDescriptor GenerateTransparentDepthPrepass(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_TRANSPARENT_DEPTH_PREPASS",
                lightMode = "TransparentDepthPrepass",
                useInPreview = true,

                validPixelBlocks = supportLighting ?
                    new BlockFieldDescriptor[]
                    {
                        BlockFields.SurfaceDescription.Alpha,
                        HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,
                        BlockFields.SurfaceDescription.AlphaClipThreshold,
                        HDBlockFields.SurfaceDescription.DepthOffset,
                        BlockFields.SurfaceDescription.NormalTS,
                        BlockFields.SurfaceDescription.NormalWS,
                        BlockFields.SurfaceDescription.NormalOS,
                        BlockFields.SurfaceDescription.Smoothness,
                    } :
                    new BlockFieldDescriptor[]
                    {
                        BlockFields.SurfaceDescription.Alpha,
                        HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,
                        BlockFields.SurfaceDescription.AlphaClipThreshold,
                        HDBlockFields.SurfaceDescription.DepthOffset,
                    },

                // Collections
                requiredFields = TransparentDepthPrepassFields,
                renderStates = GenerateRenderState(),
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.TransparentDepthPrepass,
                includes = GenerateIncludes(),
            };

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
                    // Caution: When using MSAA we have normal and depth buffer bind.
                    // Unlit objects need to NOT write in normal buffer (or write 0) - Disable color mask for this RT
                    // Note: ShaderLab doesn't allow to have a variable on the second parameter of ColorMask
                    // - When MSAA: disable target 1 (normal buffer)
                    // - When no MSAA: disable target 0 (normal buffer) and 1 (unused)
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

#endregion

#region Transparent Depth Postpass

        public static PassDescriptor GenerateTransparentDepthPostpass(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_TRANSPARENT_DEPTH_POSTPASS",
                lightMode = "TransparentDepthPostpass",
                useInPreview = true,

                validPixelBlocks = new BlockFieldDescriptor[]
                {
                    BlockFields.SurfaceDescription.Alpha,
                    HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,
                    HDBlockFields.SurfaceDescription.DepthOffset,
                    BlockFields.SurfaceDescription.AlphaClipThreshold,
                },

                // Collections
                renderStates = GenerateRenderState(),
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.TransparentDepthPostpass,
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

#endregion

#region Lit DepthOnly

        public static PassDescriptor GenerateLitDepthOnly()
        {
            return new PassDescriptor
            {
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Collections
                requiredFields = CoreRequiredFields.LitFull,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingDefault,
                keywords = LitDepthOnlyKeywords,
                includes = DepthOnlyIncludes,
            };
        }

        public static IncludeCollection DepthOnlyIncludes = new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
            { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.CoreUtility },
            { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
            { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
        };

        public static KeywordCollection LitDepthOnlyKeywords = new KeywordCollection
        {
            { CoreKeywordDescriptors.WriteNormalBuffer },
        };

#endregion

#region GBuffer

        public static PassDescriptor GenerateGBuffer()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                useInPreview = true,

                // Collections
                requiredFields = CoreRequiredFields.LitMinimal,
                renderStates = GBufferRenderState,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingDefault,
                keywords = GBufferKeywords,
                includes = GBufferIncludes,

                virtualTextureFeedback = true,
            };
        }

        public static KeywordCollection GBufferKeywords = new KeywordCollection
        {
            { CoreKeywordDescriptors.LightLayers },
        };

        public static IncludeCollection GBufferIncludes = new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
            { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.CoreUtility },
            { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
            { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            { CoreIncludes.kPassGBuffer, IncludeLocation.Postgraph },
        };

        public static RenderStateCollection GBufferRenderState = new RenderStateCollection
        {
            { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
            { RenderState.ZTest(CoreRenderStates.Uniforms.zTestGBuffer) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskGBuffer,
                Ref = CoreRenderStates.Uniforms.stencilRefGBuffer,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

#endregion

#region Lit Forward

        public static PassDescriptor GenerateLitForward()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                useInPreview = true,

                // Collections
                requiredFields = CoreRequiredFields.LitMinimal,
                renderStates = CoreRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.Forward,
                includes = ForwardIncludes,

                virtualTextureFeedback = true,
            };
        }

        public static IncludeCollection ForwardIncludes = new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
            { CoreIncludes.kLighting, IncludeLocation.Pregraph },
            { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
            { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
            { CoreIncludes.CoreUtility },
            { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
            { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
        };

#endregion

#region Lit Raytracing Prepass

        public static PassDescriptor GenerateLitRaytracingPrepass()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "RayTracingPrepass",
                referenceName = "SHADERPASS_CONSTANT",
                lightMode = "RayTracingPrepass",
                useInPreview = false,

                // Collections
                renderStates = RayTracingPrepassRenderState,
                pragmas = CorePragmas.Basic,
                defines = CoreDefines.ShaderGraphRaytracingDefault,
                includes = RayTracingPrepassIncludes,
            };
        }

        public static IncludeCollection RayTracingPrepassIncludes = new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
            { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.CoreUtility },
            { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
            { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            { CoreIncludes.kPassConstant, IncludeLocation.Postgraph },
        };

        public static RenderStateCollection RayTracingPrepassRenderState = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.Zero) },
            { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            // Note: we use default ZTest LEqual so if the object have already been render in depth prepass, it will re-render to tag stencil
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

                // Collections
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? RaytracingIndirectDefines : null,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection { CoreIncludes.RaytracingCorePregraph };

                includes.Add(CoreIncludes.kRaytracingIntersection, IncludeLocation.Pregraph);

                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                }

                // Each material has a specific hlsl file that should be included pre-graph and holds the lighting model
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                // We need to then include the ray tracing missing bits for the lighting models (based on which lighting model)
                includes.Add(CoreIncludes.kRaytracingPlaceholder, IncludeLocation.Pregraph);
                // We want to have the ray tracing light loop if this is an indirect sub-shader or a forward one and it is not the unlit shader
                if (supportLighting)
                    includes.Add(CoreIncludes.kRaytracingLightLoop, IncludeLocation.Pregraph);

                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kRaytracingCommon, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);

                // post graph includes
                includes.Add(CoreIncludes.kPassRaytracingIndirect, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static DefineCollection RaytracingIndirectDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { Defines.raytracingRaytraced },
            { CoreKeywordDescriptors.HasLightloop, 1 },
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
                // validVertexBlocks = CoreBlockMasks.Vertex,
                // validPixelBlocks = RaytracingVisibilityFragment,

                // Collections
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? RaytracingVisibilityDefines : null,
                keywords = CoreKeywords.RaytracingVisiblity,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection { CoreIncludes.RaytracingCorePregraph };

                // We want the generic payload if this is not a gbuffer or a subsurface subshader
                includes.Add(CoreIncludes.kRaytracingIntersection, IncludeLocation.Pregraph);

                // Each material has a specific hlsl file that should be included pre-graph and holds the lighting model
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                // We need to then include the ray tracing missing bits for the lighting models (based on which lighting model)
                includes.Add(CoreIncludes.kRaytracingPlaceholder, IncludeLocation.Pregraph);

                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kRaytracingCommon, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);

                // post graph includes
                includes.Add(CoreIncludes.kPassRaytracingVisbility, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static DefineCollection RaytracingVisibilityDefines = new DefineCollection
        {
            { Defines.raytracingRaytraced },
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
                // validVertexBlocks = CoreBlockMasks.Vertex,
                // validPixelBlocks = RaytracingForwardFragment,

                // Collections
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? RaytracingForwardDefines : null,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection { CoreIncludes.RaytracingCorePregraph };

                // We want the generic payload if this is not a gbuffer or a subsurface subshader
                includes.Add(CoreIncludes.kRaytracingIntersection, IncludeLocation.Pregraph);

                // We want to have the lighting include if this is an indirect sub-shader, a forward one or the path tracing (and this is not an unlit)
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                }

                // Each material has a specific hlsl file that should be included pre-graph and holds the lighting model
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                // We need to then include the ray tracing missing bits for the lighting models (based on which lighting model)
                includes.Add(CoreIncludes.kRaytracingPlaceholder, IncludeLocation.Pregraph);

                // We want to have the ray tracing light loop if this is an indirect sub-shader or a forward one and it is not the unlit shader
                if (supportLighting)
                    includes.Add(CoreIncludes.kRaytracingLightLoop, IncludeLocation.Pregraph);

                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kRaytracingCommon, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);

                // post graph includes
                includes.Add(CoreIncludes.kPassRaytracingForward, IncludeLocation.Postgraph);

                return includes;
            }
        }


        public static DefineCollection RaytracingForwardDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { Defines.raytracingRaytraced },
            { CoreKeywordDescriptors.HasLightloop, 1 },
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
                // validVertexBlocks = CoreBlockMasks.Vertex,
                // validPixelBlocks = RaytracingGBufferFragment,

                // Collections
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? RaytracingGBufferDefines : null,
                keywords = supportLighting ? CoreKeywords.RaytracingGBuffer : null,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection { CoreIncludes.RaytracingCorePregraph };

                includes.Add(CoreIncludes.kRaytracingIntersectionGBuffer, IncludeLocation.Pregraph);

                // Each material has a specific hlsl file that should be included pre-graph and holds the lighting model
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);

                // We want to have the normal buffer include if this is a gbuffer and unlit shader
                if (!supportLighting)
                    includes.Add(CoreIncludes.kNormalBuffer, IncludeLocation.Pregraph);
                    
                // If this is the gbuffer sub-shader, we want the standard lit data
                includes.Add(CoreIncludes.kStandardLit, IncludeLocation.Pregraph);

                // We need to then include the ray tracing missing bits for the lighting models (based on which lighting model)
                includes.Add(CoreIncludes.kRaytracingPlaceholder, IncludeLocation.Pregraph);

                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kRaytracingCommon, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);

                // post graph includes
                includes.Add(CoreIncludes.kPassRaytracingGBuffer, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static DefineCollection RaytracingGBufferDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { Defines.raytracingRaytraced },
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
                // validVertexBlocks = CoreBlockMasks.Vertex,
                // validPixelBlocks = PathTracingFragment,

                //Collections
                pragmas = CorePragmas.RaytracingBasic,
                defines = supportLighting ? RaytracingPathTracingDefines : null,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection { CoreIncludes.RaytracingCorePregraph };

                // We want the generic payload if this is not a gbuffer or a subsurface subshader
                includes.Add(CoreIncludes.kRaytracingIntersection, IncludeLocation.Pregraph);

                // We want to have the lighting include if this is an indirect sub-shader, a forward one or the path tracing (and this is not an unlit)
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                }

                // Each material has a specific hlsl file that should be included pre-graph and holds the lighting model
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                // We need to then include path tracing support for the material
                includes.Add(CoreIncludes.kPathtracingPlaceholder, IncludeLocation.Pregraph);

                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kRaytracingCommon, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);

                // post graph includes
                includes.Add(CoreIncludes.kPassPathTracing, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static DefineCollection RaytracingPathTracingDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { Defines.raytracingDefault },
            { CoreKeywordDescriptors.HasLightloop, 1 },
        };

#endregion

#region Raytracing Subsurface

        public static PassDescriptor GenerateRaytracingSubsurface()
        {
            return new PassDescriptor
            {
                //Definition
                displayName = "SubSurfaceDXR",
                referenceName = "SHADERPASS_RAYTRACING_SUB_SURFACE",
                lightMode = "SubSurfaceDXR",
                useInPreview = false,

                // Template
                // passTemplatePath = passTemplatePath,
                // sharedTemplateDirectories = passTemplateMaterialDirectories,

                // //Port mask
                // validVertexBlocks = CoreBlockMasks.Vertex,
                // validPixelBlocks = LitBlockMasks.FragmentDefault,

                //Collections
                pragmas = CorePragmas.RaytracingBasic,
                defines = RaytracingSubsurfaceDefines,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection { CoreIncludes.RaytracingCorePregraph };

                // We want the sub-surface payload if we are in the subsurface sub shader
                includes.Add(CoreIncludes.kRaytracingIntersectionSubSurface, IncludeLocation.Pregraph);

                // Each material has a specific hlsl file that should be included pre-graph and holds the lighting model
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                // We need to then include the ray tracing missing bits for the lighting models (based on which lighting model)
                includes.Add(CoreIncludes.kRaytracingPlaceholder, IncludeLocation.Pregraph);

                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kRaytracingCommon, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);

                // post graph includes
                includes.Add(CoreIncludes.kPassRaytracingSubSurface, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static DefineCollection RaytracingSubsurfaceDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { Defines.raytracingRaytraced },
        };

#endregion

#region FullScreen Debug

        public static PassDescriptor GenerateFullScreenDebug()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "FullScreenDebug",
                referenceName = "SHADERPASS_FULL_SCREEN_DEBUG",
                lightMode = "FullScreenDebug",
                useInPreview = false,

                // Collections
                pragmas = CorePragmas.Basic,
                renderStates = FullScreenDebugRenderState,
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                return new IncludeCollection
                {
                    { CoreIncludes.CorePregraph },
                    { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                    { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
                    { CoreIncludes.CoreUtility },
                    { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                    { CoreIncludes.kPassFullScreenDebug, IncludeLocation.Postgraph },
                };
            }
        }

        public static RenderStateCollection FullScreenDebugRenderState = new RenderStateCollection
        {
            { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.Off) },
            { RenderState.ZTest(ZTest.LEqual) },
        };

#endregion

#region Define Utility

        public static class Defines
        {
            // Shadows
            public static DefineCollection shadowLow = new DefineCollection { {CoreKeywordDescriptors.Shadow, 0} };
            public static DefineCollection shadowMedium = new DefineCollection { {CoreKeywordDescriptors.Shadow, 1} };
            public static DefineCollection shadowHigh = new DefineCollection { {CoreKeywordDescriptors.Shadow, 2} };

            // Raytracing Quality
            public static DefineCollection raytracingDefault = new DefineCollection { { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0} };
            public static DefineCollection raytracingRaytraced = new DefineCollection { { RayTracingQualityNode.GetRayTracingQualityKeyword(), 1} };
        }

#endregion

    }
}
