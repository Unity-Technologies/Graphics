using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    partial class UniversalTerrainLitSubTarget
    {
        #region Template
        static class TerrainBaseMapGenTemplate
        {
            public static readonly string kPassTemplate = "Packages/com.unity.render-pipelines.universal/Editor/Terrain/TerrainBaseMapGenPass.template";
            public static readonly string[] kSharedTemplateDirectories;
            static TerrainBaseMapGenTemplate()
            {
                kSharedTemplateDirectories = new string[UniversalTarget.kSharedTemplateDirectories.Length + 1];
                Array.Copy(UniversalTarget.kSharedTemplateDirectories, kSharedTemplateDirectories, UniversalTarget.kSharedTemplateDirectories.Length);
                kSharedTemplateDirectories[^1] = "Packages/com.unity.render-pipelines.universal/Editor/Terrain/";
            }
        }
        #endregion

        #region SubShaders
        static class TerrainLitAddSubShaders
        {
            public static SubShaderDescriptor LitComputeDotsSubShader(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = false,
                    passes = new PassCollection(),
                    additionalShaderID = "Hidden/{Name}_AddPass",
                    shaderCustomEditors = new List<ShaderCustomEditor>(),
                    shaderCustomEditor = "",
                    shaderFallback = "",
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
                    customTags = UniversalTarget.kLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = false,
                    passes = new PassCollection(),
                    additionalShaderID = "Hidden/{Name}_AddPass",
                    shaderCustomEditors = new List<ShaderCustomEditor>(),
                    shaderCustomEditor = "",
                    shaderFallback = "",
                };

                result.passes.Add(TerrainLitAddPasses.Forward(target, blendModePreserveSpecular));

                return result;
            }
        }

        static class TerrainLitBaseMapGenSubShaders
        {
            public static SubShaderDescriptor GenerateBaseMap(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    hideTags = true,
                    generatesPreview = false,
                    passes = new PassCollection(),
                    additionalShaderID = "Hidden/{Name}_BaseMapGen",
                    shaderCustomEditors = new List<ShaderCustomEditor>(),
                    shaderCustomEditor = "",
                    shaderFallback = "",
                };

                result.passes.Add(TerrainBaseGenPasses.MainTex(target));
                result.passes.Add(TerrainBaseGenPasses.MetallicTex(target));

                return result;
            }
        }
        #endregion

        #region Passes
        static class TerrainLitAddPasses
        {
            public static PassDescriptor Forward(UniversalTarget target, bool blendModePreserveSpecular, PragmaCollection pragmas = null)
            {
                var result = TerrainLitPasses.Forward(target, blendModePreserveSpecular, pragmas);
                result.renderStates = AdditionalLayersRenderState();
                result.defines.Add(TerrainDefines.TerrainAddPass, 1);

                return result;
            }

            // Deferred only in SM4.5, MRT not supported in GLES2
            public static PassDescriptor GBuffer(UniversalTarget target, bool blendModePreserveSpecular)
            {
                var result = TerrainLitPasses.GBuffer(target, blendModePreserveSpecular);
                result.renderStates = AdditionalLayersRenderState();
                result.defines.Add(TerrainDefines.TerrainAddPass, 1);

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

        static class TerrainBaseGenPasses
        {
            private static string kMainTexName = "\"Name\" = \"_MainTex\"";
            private static string kMainTexFormat = "\"Format\" = \"ARGB32\"";
            private static string kMainTexSize = "\"Size\" = \"1\"";
            private static string kMainEmptyColor = "\"EmptyColor\" = \"\"";

            private static string kMetallicTexName = "\"Name\" = \"_MetallicTex\"";
            private static string kMetallicTexFormat = "\"Format\" = \"R8\"";
            private static string kMetallicTexSize = "\"Size\" = \"1/4\"";
            private static string kMetallicEmptyColor = "\"EmptyColor\" = \"FF000000\"";

            public static PassDescriptor MainTex(UniversalTarget target)
            {
                var result = new PassDescriptor
                {
                    referenceName = "SHADERPASS_MAINTEX",

                    // Template
                    passTemplatePath = TerrainBaseMapGenTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainBaseMapGenTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = TerrainBlockMasks.FragmentLit,

                    // Fields
                    structs = TerrainStructCollections.Default,
                    requiredFields = TerrainRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = BaseMapGenRenderState,
                    pragmas = BaseMapPragmas,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = BaseGenMainTexIncludes,
                    additionalCommands = BaseMapMainTex,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common,
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);
                result.defines.Add(TerrainDefines.TerrainSplat01, 1);
                result.defines.Add(TerrainDefines.TerrainSplat23, 1);
                result.defines.Add(TerrainDefines.MetallicSpecGlossMap, 1);
                result.defines.Add(TerrainDefines.SmoothnessTextureAlbedoChannelA, 1);
                result.defines.Add(TerrainDefines.TerrainBaseMapGen, 1);
                result.keywords.Add(TerrainDefines.TerrainMaskmap);

                return result;
            }

            public static PassDescriptor MetallicTex(UniversalTarget target)
            {
                var result = new PassDescriptor
                {
                    referenceName = "SHADERPASS_METALLICTEX",

                    // Template
                    passTemplatePath = TerrainBaseMapGenTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainBaseMapGenTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = TerrainBlockMasks.FragmentLit,

                    // Fields
                    structs = TerrainStructCollections.Default,
                    requiredFields = TerrainRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = BaseMapGenRenderState,
                    pragmas = BaseMapPragmas,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = BaseGenMetallicTexIncludes,
                    additionalCommands = BaseMapMetallicTex,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common,
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);
                result.defines.Add(TerrainDefines.TerrainSplat01, 1);
                result.defines.Add(TerrainDefines.TerrainSplat23, 1);
                result.defines.Add(TerrainDefines.MetallicSpecGlossMap, 1);
                result.defines.Add(TerrainDefines.SmoothnessTextureAlbedoChannelA, 1);
                result.defines.Add(TerrainDefines.TerrainBaseMapGen, 1);
                result.keywords.Add(TerrainDefines.TerrainMaskmap);

                return result;
            }

            public static RenderStateCollection BaseMapGenRenderState = new RenderStateCollection()
            {
                { RenderState.ZTest(ZTest.Always) },
                { RenderState.Cull(Cull.Off) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Blend("One", "[_DstBlend]") },
            };

            public static readonly PragmaCollection BaseMapPragmas = new PragmaCollection()
            {
                { Pragma.Target(ShaderModel.Target30) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            public static readonly string kBaseMapPass = "Packages/com.unity.render-pipelines.universal/Editor/Terrain/TerrainBaseMapGenPass.hlsl";

            public static readonly IncludeCollection BaseGenMainTexIncludes = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { TerrainCoreIncludes.kTerrainLitInput, IncludeLocation.Pregraph },
                { TerrainCoreIncludes.kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { TerrainCoreIncludes.CorePostgraph },
                { TerrainBaseGenPasses.kBaseMapPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection BaseGenMetallicTexIncludes = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { TerrainCoreIncludes.kTerrainLitInput, IncludeLocation.Pregraph },
                { TerrainCoreIncludes.kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { TerrainCoreIncludes.CorePostgraph },
                { TerrainBaseGenPasses.kBaseMapPass, IncludeLocation.Postgraph },
            };

            public static readonly AdditionalCommandCollection BaseMapMainTex = new AdditionalCommandCollection()
            {
                new AdditionalCommandDescriptor("BaseGenName", kMainTexName),
                new AdditionalCommandDescriptor("BaseGenTexFormat", kMainTexFormat),
                new AdditionalCommandDescriptor("BaseGenTexSize", kMainTexSize),
                new AdditionalCommandDescriptor("BaseGenEmptyColor", kMainEmptyColor),
            };

            public static readonly AdditionalCommandCollection BaseMapMetallicTex = new AdditionalCommandCollection()
            {
                new AdditionalCommandDescriptor("BaseGenName", kMetallicTexName),
                new AdditionalCommandDescriptor("BaseGenTexFormat", kMetallicTexFormat),
                new AdditionalCommandDescriptor("BaseGenTexSize", kMetallicTexSize),
                new AdditionalCommandDescriptor("BaseGenEmptyColor", kMetallicEmptyColor),
            };
        }
        #endregion
    }
}
