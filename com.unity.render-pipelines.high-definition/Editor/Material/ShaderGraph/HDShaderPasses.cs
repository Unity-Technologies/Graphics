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

                // Collections
                renderStates = CoreRenderStates.ShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
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

                // Collections
                requiredFields = CoreRequiredFields.Meta,
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

                // Collections
                requiredFields = CoreRequiredFields.LitFull,
                renderStates = GenerateRenderState(),
                defines = supportLighting ? CoreDefines.DepthMotionVectors : null,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = GenerateKeywords(),
                includes = GenerateIncludes(),
            };

            RenderStateCollection GenerateRenderState()
            {
                var renderState = new RenderStateCollection();
                renderState.Add(CoreRenderStates.MotionVectors);
    
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

                // Collections
                requiredFields = supportLighting ? CoreRequiredFields.LitFull : null,
                renderStates = CoreRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = supportLighting ? CoreDefines.Forward : null,
                keywords = supportLighting ? CoreKeywords.Forward : UnlitForwardKeywords,
                includes = GenerateIncludes(),

                virtualTextureFeedback = supportLighting ? false : true,
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

        public static KeywordCollection UnlitForwardKeywords = new KeywordCollection
        {
            { CoreKeywords.HDBase },
            { CoreKeywordDescriptors.DebugDisplay },
            { CoreKeywordDescriptors.Shadow, new FieldCondition(HDFields.EnableShadowMatte, true) },
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

                // Collections
                requiredFields = CoreRequiredFields.LitMinimal,
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

                // Collections
                requiredFields = TransparentDepthPrepassFields,
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

                // Collections
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
                defines = CoreDefines.ShaderGraphRaytracingHigh,
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
            { CoreKeywords.HDBase },
            { CoreKeywordDescriptors.WriteMsaaDepth },
            { CoreKeywordDescriptors.WriteNormalBuffer },
            { CoreKeywordDescriptors.AlphaToMask, new FieldCondition(Fields.AlphaToMask, true) },
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
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = GBufferKeywords,
                includes = GBufferIncludes,

                virtualTextureFeedback = true,
            };
        }

        public static KeywordCollection GBufferKeywords = new KeywordCollection
        {
            { CoreKeywords.HDBase },
            { CoreKeywordDescriptors.DebugDisplay },
            { CoreKeywords.Lightmaps },
            { CoreKeywordDescriptors.ShadowsShadowmask },
            { CoreKeywordDescriptors.LightLayers },
            { CoreKeywordDescriptors.Decals },
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
                renderStates = CoreRenderStates.ForwardColorMask,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
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
                pragmas = LitRaytracingPrepassPragmas,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = RayTracingPrepassIncludes,
            };
        }

        public static PragmaCollection LitRaytracingPrepassPragmas = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.Vertex("Vert") },
            { Pragma.Fragment("Frag") },
            { Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.Playstation, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch}) },
        };

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
                defines = supportLighting ? GenerateDefines() : null,
                keywords = CoreKeywords.RaytracingIndirect,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RaytracingIndirect },
            };

            DefineCollection GenerateDefines()
            {
                return new DefineCollection
                {
                    { Defines.shadowLow },
                    { Defines.raytracingLow },
                    { CoreKeywordDescriptors.HasLightloop, 1 },
                };
            }
        }

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
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RaytracingVisibility },
                includes = CoreIncludes.Raytracing,
            };
        }

        public static DefineCollection RaytracingVisibilityDefines = new DefineCollection
        {
            { Defines.raytracingLow },
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
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RaytracingForward },
            };
        }

        public static DefineCollection RaytracingForwardDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { RayTracingNode.GetRayTracingKeyword(), 0 },
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
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RayTracingGBuffer },
            };
        }

        public static DefineCollection RaytracingGBufferDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { Defines.raytracingLow },
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
                keywords = CoreKeywords.HDBaseNoCrossFade,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RaytracingPathTracing },
            };
        }

        public static DefineCollection RaytracingPathTracingDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { RayTracingNode.GetRayTracingKeyword(), 0 },
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
                // sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // //Port mask
                // validVertexBlocks = CoreBlockMasks.Vertex,
                // validPixelBlocks = LitBlockMasks.FragmentDefault,

                //Collections
                pragmas = CorePragmas.RaytracingBasic,
                defines = RaytracingSubsurfaceDefines,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.ShaderPass.RaytracingSubSurface },
            };
        }

        public static DefineCollection RaytracingSubsurfaceDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { Defines.raytracingLow },
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
            public static DefineCollection raytracingLow = new DefineCollection { {RayTracingNode.GetRayTracingKeyword(), 1} };
            public static DefineCollection raytracingHigh = new DefineCollection { {RayTracingNode.GetRayTracingKeyword(), 0} };
        }

#endregion

    }
}
