using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;
using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    partial class UniversalTerrainLitSubTarget : UniversalSubTarget, ILegacyTarget
    {
        #region SubShaders
        static class TerrainLitAddSubShaders
        {
            public static SubShaderDescriptor LitComputeDotsSubShader(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = new List<string>() { UniversalTarget.kLitMaterialTypeTag, },renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = false,
                    passes = new PassCollection(),
                    additionalShaderID = "{Name}_AddPass",
                };

                result.passes.Add(TerrainLitAddPasses.Forward(target, blendModePreserveSpecular, TerrainCorePragmas.DOTSForward));
                result.passes.Add(TerrainLitAddPasses.GBuffer(target, blendModePreserveSpecular));

                return result;
            }

            public static SubShaderDescriptor LitGLESSubShader(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = new List<string>() { UniversalTarget.kLitMaterialTypeTag, },
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = false,
                    passes = new PassCollection(),
                    additionalShaderID = "{Name}_AddPass",
                };

                result.passes.Add(TerrainLitAddPasses.Forward(target, blendModePreserveSpecular));

                return result;
            }
        }

        static class TerrainLitBaseMapSubShaders
        {
            public static SubShaderDescriptor LitComputeDotsSubShader(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = new List<string>() { UniversalTarget.kLitMaterialTypeTag, },renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = false,
                    passes = new PassCollection(),
                    usePassList = new List<string>(),
                    additionalShaderID = "{Name}_BaseMap",
                };

                result.passes.Add(TerrainLitPasses.Forward(target, blendModePreserveSpecular, TerrainCorePragmas.DOTSForward));
                result.passes.Add(TerrainLitPasses.GBuffer(target, blendModePreserveSpecular));

                if (target.castShadows || target.allowMaterialOverride)
                    result.passes.Add(PassVariant(TerrainLitPasses.ShadowCaster(target), TerrainCorePragmas.DOTSInstanced));

                if (target.mayWriteDepth)
                    result.passes.Add(PassVariant(TerrainLitPasses.DepthOnly(target), TerrainCorePragmas.DOTSInstanced));

                result.passes.Add(PassVariant(TerrainLitPasses.DepthNormal(target), TerrainCorePragmas.DOTSInstanced));
                result.passes.Add(PassVariant(TerrainLitPasses.Meta(target), TerrainCorePragmas.DOTSInstanced));

                result.usePassList.Add("Hidden/Nature/Terrain/Utilities/PICKING");
                result.usePassList.Add("Universal Render Pipeline/Terrain/Lit/SceneSelectionPass");

                return result;
            }

            public static SubShaderDescriptor LitGLESSubShader(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = new List<string>() { UniversalTarget.kLitMaterialTypeTag, },
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = false,
                    passes = new PassCollection(),
                    usePassList = new List<string>(),
                    additionalShaderID = "{Name}_BaseMap",
                };

                result.passes.Add(TerrainLitAddPasses.Forward(target, blendModePreserveSpecular));

                if (target.castShadows || target.allowMaterialOverride)
                    result.passes.Add(TerrainLitPasses.ShadowCaster(target));

                if (target.mayWriteDepth)
                    result.passes.Add(TerrainLitPasses.DepthOnly(target));

                result.passes.Add(TerrainLitPasses.DepthNormal(target));
                result.passes.Add(TerrainLitPasses.Meta(target));

                result.usePassList.Add("Hidden/Nature/Terrain/Utilities/PICKING");
                result.usePassList.Add("Universal Render Pipeline/Terrain/Lit/SceneSelectionPass");

                return result;
            }
        }

        static class TerrainLitBaseMapGenSubShaders
        {
            public static SubShaderDescriptor LitComputeDotsSubShader(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = new List<string>() { UniversalTarget.kLitMaterialTypeTag, },renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = false,
                    passes = new PassCollection(),
                    additionalShaderID = "{Name}_BaseMapGen",
                };


                return result;
            }

            public static SubShaderDescriptor LitGLESSubShader(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = new List<string>() { UniversalTarget.kLitMaterialTypeTag, },
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = false,
                    passes = new PassCollection(),
                    usePassList = new List<string>(),
                    additionalShaderID = "{Name}_BaseMapGen",
                };

                return result;
            }
        }
        #endregion

        #region Passes
        static class TerrainLitAddPasses
        {
            public static PassDescriptor Forward(UniversalTarget target, bool blendModePreserveSpecular, PragmaCollection pragmas = null)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Universal Forward",
                    referenceName = "SHADERPASS_FORWARD",
                    lightMode = "UniversalForward",
                    useInPreview = true,

                    // Template
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = TerrainBlockMasks.FragmentLit,

                    // Fields
                    structs = TerrainStructCollections.Default,
                    requiredFields = TerrainRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = AdditionalLayersRenderState(),
                    pragmas = pragmas ?? TerrainCorePragmas.Forward,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog, },
                    keywords = new KeywordCollection() { TerrainLitKeywords.Forward },
                    includes = TerrainCoreIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);
                result.defines.Add(TerrainDefines.TerrainSplat01, 1);
                result.defines.Add(TerrainDefines.TerrainSplat23, 1);
                result.defines.Add(TerrainDefines.TerrainAddPass, 1);
                result.keywords.Add(TerrainDefines.TerrainNormalmap);
                result.keywords.Add(TerrainDefines.TerrainMaskmap);
                result.keywords.Add(TerrainDefines.TerrainBlendHeight);
                result.keywords.Add(TerrainDefines.TerrainInstancedPerPixelNormal);
                result.defines.Add(TerrainDefines.MetallicSpecGlossMap, 1);
                result.defines.Add(TerrainDefines.SmoothnessTextureAlbedoChannelA, 1);

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                TerrainLitPasses.AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);

                return result;
            }

            // Deferred only in SM4.5, MRT not supported in GLES2
            public static PassDescriptor GBuffer(UniversalTarget target, bool blendModePreserveSpecular)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "GBuffer",
                    referenceName = "SHADERPASS_GBUFFER",
                    lightMode = "UniversalGBuffer",

                    // Template
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = TerrainBlockMasks.FragmentLit,

                    // Fields
                    structs = TerrainStructCollections.Default,
                    requiredFields = TerrainRequiredFields.GBuffer,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = AdditionalLayersRenderState(),
                    pragmas = TerrainCorePragmas.DOTSGBuffer,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection() { TerrainLitKeywords.GBuffer },
                    includes = TerrainCoreIncludes.GBuffer,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);
                result.defines.Add(TerrainDefines.TerrainSplat01, 1);
                result.defines.Add(TerrainDefines.TerrainSplat23, 1);
                result.defines.Add(TerrainDefines.TerrainAddPass, 1);
                result.keywords.Add(TerrainDefines.TerrainNormalmap);
                result.keywords.Add(TerrainDefines.TerrainMaskmap);
                result.keywords.Add(TerrainDefines.TerrainBlendHeight);
                result.keywords.Add(TerrainDefines.TerrainInstancedPerPixelNormal);
                result.defines.Add(TerrainDefines.MetallicSpecGlossMap, 1);
                result.defines.Add(TerrainDefines.SmoothnessTextureAlbedoChannelA, 1);

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                TerrainLitPasses.AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);

                return result;
            }

            // used by lit/unlit subtargets
            public static RenderStateCollection AdditionalLayersRenderState()
            {
                var result = new RenderStateCollection();

                result.Add(RenderState.Blend(Blend.One, Blend.One));

                return result;
            }
        }
        #endregion
    }
}
