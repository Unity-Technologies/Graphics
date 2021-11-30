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
        public static StructCollection GenerateStructs(StructCollection input, bool useVFX, bool useTessellation)
        {
            StructCollection structs = input == null ? new StructCollection() : new StructCollection { input };

            if (useVFX) // Do nothing the struct will be replace in PostProcessSubShader of VFXHDRPSubTargets
                return structs;
            else
                structs.Add(useTessellation ? CoreStructCollections.BasicTessellation : CoreStructCollections.Basic);

            return structs;
        }

        public static PragmaCollection GeneratePragmas(PragmaCollection input, bool useVFX, bool useTessellation)
        {
            PragmaCollection pragmas = input == null ? new PragmaCollection() : new PragmaCollection { input };

            if (useVFX)
                pragmas.Add(CorePragmas.BasicVFX);
            else
                pragmas.Add(useTessellation ? CorePragmas.BasicTessellation : CorePragmas.Basic);

            return pragmas;
        }

        public static DefineCollection GenerateDefines(DefineCollection input, bool useVFX, bool useTessellation)
        {
            DefineCollection defines = input == null ? new DefineCollection() : new DefineCollection { input };

            if (useTessellation && !useVFX)
                defines.Add(CoreDefines.Tessellation);

            return defines;
        }

        #region Distortion Pass

        public static PassDescriptor GenerateDistortionPass(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                useInPreview = true,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = CoreRequiredFields.Basic,
                renderStates = GenerateRenderState(),
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV2Only, useVFX, useTessellation),
                defines = GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
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

        public static PassDescriptor GenerateScenePicking(bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = CoreRenderStates.ScenePicking,
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV1AndV2EditorSync, useVFX, useTessellation),
                defines = GenerateDefines(CoreDefines.ScenePicking, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
            };

            FieldCollection GenerateRequiredFields()
            {
                var fieldCollection = new FieldCollection();

                fieldCollection.Add(CoreRequiredFields.Basic);
                fieldCollection.Add(CoreRequiredFields.AddWriteNormalBuffer);

                return fieldCollection;
            }

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.kPickingSpaceTransforms, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CorePregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        #endregion

        #region Scene Selection Pass

        public static PassDescriptor GenerateSceneSelection(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = CoreRequiredFields.Basic,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV1AndV2EditorSync, useVFX, useTessellation),
                defines = GenerateDefines(CoreDefines.SceneSelection, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.kPickingSpaceTransforms, IncludeLocation.Pregraph);
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

        static public PassDescriptor GenerateShadowCaster(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                validPixelBlocks = new BlockFieldDescriptor[]
                {
                    BlockFields.SurfaceDescription.Alpha,
                    BlockFields.SurfaceDescription.AlphaClipThreshold,
                    HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,
                    HDBlockFields.SurfaceDescription.DepthOffset,
                    HDBlockFields.SurfaceDescription.DiffusionProfileHash   // not used, but keeps the UnityPerMaterial cbuffer identical
                },

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = CoreRequiredFields.Basic,
                renderStates = CoreRenderStates.ShadowCaster,
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV2Only, useVFX, useTessellation),
                defines = GenerateDefines(null, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
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

        public static PassDescriptor GenerateMETA(bool supportLighting, bool useVFX)
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
                structs = GenerateStructs(null, useVFX, false),
                requiredFields = CoreRequiredFields.Meta,
                renderStates = CoreRenderStates.Meta,
                // Note: no tessellation for meta pass
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV1AndV2, useVFX, false),
                defines = GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, useVFX, false),
                keywords = new KeywordCollection() { CoreKeywordDescriptors.EditorVisualization },
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

        public static PassDescriptor GenerateDepthForwardOnlyPass(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = GenerateRenderState(),
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV2Only, useVFX, useTessellation),
                defines = GenerateDefines(supportLighting ? CoreDefines.DepthForwardOnly : CoreDefines.DepthForwardOnlyUnlit, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
            };

            FieldCollection GenerateRequiredFields()
            {
                var fieldCollection = new FieldCollection();

                fieldCollection.Add(supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.Basic);
                fieldCollection.Add(CoreRequiredFields.AddWriteNormalBuffer);

                return fieldCollection;
            }

            RenderStateCollection GenerateRenderState()
            {
                var renderState = new RenderStateCollection { CoreRenderStates.DepthOnly };
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
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        #endregion

        #region Motion Vectors

        public static PassDescriptor GenerateMotionVectors(bool supportLighting, bool supportForward, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = GenerateRenderState(),
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV2Only, useVFX, useTessellation),
                // For shadow matte (unlit SG only) we need to enable write normal buffer
                // For lighting case: we want to use WRITE_NORMAL_BUFFER as a define in forward only case and WRITE_NORMAL_BUFFER as a keyword in Lit case.
                // This is handled in CollectPassKeywords() function in SurfaceSubTarget.cs so we don't add it here.
                defines = GenerateDefines(supportLighting ? Defines.raytracingDefault : CoreDefines.MotionVectorUnlit, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
            };

            FieldCollection GenerateRequiredFields()
            {
                var fieldCollection = new FieldCollection();

                fieldCollection.Add(supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.BasicMotionVector);
                fieldCollection.Add(CoreRequiredFields.AddWriteNormalBuffer);

                return fieldCollection;
            }

            RenderStateCollection GenerateRenderState()
            {
                var renderState = new RenderStateCollection();
                renderState.Add(CoreRenderStates.MotionVectors);
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

        public static PassDescriptor GenerateForwardOnlyPass(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = supportLighting ? "SHADERPASS_FORWARD" : "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.BasicMotionVector,
                renderStates = CoreRenderStates.Forward,
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV2Only, useVFX, useTessellation),
                defines = GenerateDefines(supportLighting ? CoreDefines.Forward : CoreDefines.ForwardUnlit, useVFX, useTessellation),
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common
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

        #region Back then front pass

        public static PassDescriptor GenerateBackThenFront(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = supportLighting ? "SHADERPASS_FORWARD" : "SHADERPASS_FORWARD_UNLIT",
                lightMode = "TransparentBackface",
                useInPreview = true,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                // BackThenFront is a forward pass and thus require same settings
                requiredFields = supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.BasicMotionVector,
                renderStates = CoreRenderStates.TransparentBackface,
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV1AndV2, useVFX, useTessellation),
                defines = GenerateDefines(CoreDefines.BackThenFront, useVFX, useTessellation),
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common
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

        public static PassDescriptor GenerateTransparentDepthPrepass(bool supportLighting, bool useVFX, bool useTessellation)
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
                    HDBlockFields.SurfaceDescription.DiffusionProfileHash   // not used, but keeps the UnityPerMaterial cbuffer identical
                } :
                new BlockFieldDescriptor[]
                {
                    BlockFields.SurfaceDescription.Alpha,
                    HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,
                    BlockFields.SurfaceDescription.AlphaClipThreshold,
                    HDBlockFields.SurfaceDescription.DepthOffset,
                    HDBlockFields.SurfaceDescription.DiffusionProfileHash   // not used, but keeps the UnityPerMaterial cbuffer identical
                },

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = GenerateRenderState(),
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV1AndV2, useVFX, useTessellation),
                // For TransparentDepthPrepass, WRITE_NORMAL_BUFFER is define in the ShaderPass.template directly as it rely on other define
                defines = GenerateDefines(CoreDefines.TransparentDepthPrepass, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
            };

            FieldCollection GenerateRequiredFields()
            {
                var fieldCollection = new FieldCollection();

                fieldCollection.Add(CoreRequiredFields.Basic);
                fieldCollection.Add(CoreRequiredFields.AddWriteNormalBuffer); // Always define as we can't test condition for it

                return fieldCollection;
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

        #endregion

        #region Transparent Depth Postpass

        public static PassDescriptor GenerateTransparentDepthPostpass(bool supportLighting, bool useVFX, bool useTessellation)
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
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = CoreRequiredFields.Basic,
                renderStates = GenerateRenderState(),
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV1AndV2, useVFX, useTessellation),
                defines = GenerateDefines(CoreDefines.TransparentDepthPostpass, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
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

        public static PassDescriptor GenerateLitDepthOnly(bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV1AndV2, useVFX, useTessellation),
                defines = GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, useVFX, useTessellation),
                keywords = LitDepthOnlyKeywords,
                includes = DepthOnlyIncludes,
                customInterpolators = CoreCustomInterpolators.Common,
            };

            FieldCollection GenerateRequiredFields()
            {
                var fieldCollection = new FieldCollection();

                fieldCollection.Add(CoreRequiredFields.Basic);
                fieldCollection.Add(CoreRequiredFields.AddWriteNormalBuffer);

                return fieldCollection;
            }
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

        public static PassDescriptor GenerateGBuffer(bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                useInPreview = true,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = GBufferRenderState,
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV1AndV2, useVFX, useTessellation),
                defines = GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, useVFX, useTessellation),
                keywords = GBufferKeywords,
                includes = GBufferIncludes,
                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common,
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

        public static PassDescriptor GenerateLitForward(bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                useInPreview = true,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = CoreRenderStates.Forward,
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV1AndV2, useVFX, useTessellation),
                defines = GenerateDefines(CoreDefines.ForwardLit, useVFX, useTessellation),
                includes = ForwardIncludes,
                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common,
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
                structs = GenerateStructs(null, false, false),
                requiredFields = CoreRequiredFields.Basic,
                renderStates = RayTracingPrepassRenderState,
                // no tessellation for raytracing
                pragmas = GeneratePragmas(null, false, false),
                defines = GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, false, false),
                includes = RayTracingPrepassIncludes,
                customInterpolators = CoreCustomInterpolators.Common,
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

        public static KeywordCollection IndirectDiffuseKeywordCollection = new KeywordCollection
        {
            { CoreKeywordDescriptors.multiBounceIndirect },
        };

        public static PassDescriptor GenerateRaytracingIndirect(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,
                requiredFields = supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.Basic,

                // Collections
                structs = CoreStructCollections.BasicRaytracing,
                pragmas = CorePragmas.BasicRaytracing,
                defines = supportLighting ? RaytracingIndirectDefines : RaytracingIndirectUnlitDefines,
                keywords = supportLighting ? IndirectDiffuseKeywordCollection : null,
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

        public static DefineCollection RaytracingIndirectUnlitDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { Defines.raytracingRaytraced },
        };

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
                structs = CoreStructCollections.BasicRaytracing,
                pragmas = CorePragmas.BasicRaytracing,
                defines = supportLighting ? RaytracingVisibilityDefines : null,
                requiredFields = CoreRequiredFields.Basic,
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
                requiredFields = supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.Basic,

                // Port Mask
                // validVertexBlocks = CoreBlockMasks.Vertex,
                // validPixelBlocks = RaytracingForwardFragment,

                // Collections
                structs = CoreStructCollections.BasicRaytracing,
                pragmas = CorePragmas.BasicRaytracing,
                defines = supportLighting ? RaytracingForwardDefines : RaytracingForwardUnlitDefines,
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

        public static DefineCollection RaytracingForwardUnlitDefines = new DefineCollection
        {
            { Defines.shadowLow },
            { Defines.raytracingRaytraced },
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
                requiredFields = supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.Basic,

                // Port Mask
                // validVertexBlocks = CoreBlockMasks.Vertex,
                // validPixelBlocks = RaytracingGBufferFragment,

                // Collections
                structs = CoreStructCollections.BasicRaytracing,
                pragmas = CorePragmas.BasicRaytracing,
                defines = RaytracingGBufferDefines,
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
                structs = CoreStructCollections.BasicRaytracing,
                pragmas = CorePragmas.BasicRaytracing,
                defines = supportLighting ? RaytracingPathTracingDefines : null,
                requiredFields = supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.Basic,
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
                requiredFields = CoreRequiredFields.BasicLighting,

                // //Port mask
                // validVertexBlocks = CoreBlockMasks.Vertex,
                // validPixelBlocks = LitBlockMasks.FragmentDefault,

                //Collections
                structs = CoreStructCollections.BasicRaytracing,
                pragmas = CorePragmas.BasicRaytracing,
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

        public static PassDescriptor GenerateFullScreenDebug(bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "FullScreenDebug",
                referenceName = "SHADERPASS_FULL_SCREEN_DEBUG",
                lightMode = "FullScreenDebug",
                useInPreview = false,

                // Collections
                structs = GenerateStructs(null, useVFX, useTessellation),
                requiredFields = CoreRequiredFields.Basic,
                pragmas = GeneratePragmas(CorePragmas.DotsInstancedInV2Only, useVFX, useTessellation),
                defines = GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, useVFX, useTessellation),
                renderStates = FullScreenDebugRenderState,
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
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
            public static DefineCollection shadowLow = new DefineCollection { { CoreKeywordDescriptors.Shadow, 0 } };
            public static DefineCollection shadowMedium = new DefineCollection { { CoreKeywordDescriptors.Shadow, 1 } };
            public static DefineCollection shadowHigh = new DefineCollection { { CoreKeywordDescriptors.Shadow, 2 } };

            // Raytracing Quality
            public static DefineCollection raytracingDefault = new DefineCollection { { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 } };
            public static DefineCollection raytracingRaytraced = new DefineCollection { { RayTracingQualityNode.GetRayTracingQualityKeyword(), 1 } };
        }

        #endregion
    }
}
