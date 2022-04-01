using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class TerrainLitSubTarget
    {
        #region Template
        static class TerrainBaseMapGenTemplate
        {
            public static readonly string kPassTemplate = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/TerrainLit/ShaderGraph/BaseMapGenPass.template";
        }
        #endregion

        private SubShaderDescriptor GetBaseMapGenSubShaderDescriptor()
        {
            return new SubShaderDescriptor()
            {
                hideTags = true,
                generatesPreview = false,
                customTags = GetBaseMapTags(),
                passes = GetPasses(),
                additionalShaderID = "Hidden/{Name}_BaseMapGen",
                shaderCustomEditors = new List<ShaderCustomEditor>(),
                shaderCustomEditor = "",
                shaderFallback = "",
            };

            List<string> GetBaseMapTags()
            {
                var tagList = new List<string>();
                tagList.Add("\"SplatCount\" = \"8\"");

                return tagList;
            }

            PassCollection GetPasses()
            {
                var passes = new PassCollection()
                {
                    GenerateMainTex(systemData.tessellation),
                    GenerateMetallicTex(systemData.tessellation),
                };

                return passes;
            }
        }

        #region Passes
        static public PassDescriptor GenerateMainTex(bool useTessellation)
        {
            var result = new PassDescriptor()
            {
                // Definition
                referenceName = "SHADERPASS_MAINTEX",
                displayName = "MainTex",
                lightMode = "MainTex",
                useInPreview = false,
                passTemplatePath = TerrainBaseMapGenTemplate.kPassTemplate,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = TerrainBaseGen.BaseMapGenRenderState,
                pragmas = TerrainBaseGen.BaseMapPragmas,
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ForwardLit, false, useTessellation),
                includes = TerrainBaseGen.Includes,
                additionalCommands = TerrainBaseGen.BaseMapMainTex,
                virtualTextureFeedback = false,
            };

            return result;
        }

        static public PassDescriptor GenerateMetallicTex(bool useTessellation)
        {
            return new PassDescriptor()
            {
                // Definition
                referenceName = "SHADERPASS_METALLICTEX",
                displayName = "MetallicTex",
                lightMode = "MetallicTex",
                useInPreview = false,
                passTemplatePath = TerrainBaseMapGenTemplate.kPassTemplate,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = TerrainBaseGen.BaseMapGenRenderState,
                pragmas = TerrainBaseGen.BaseMapPragmas,
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ForwardLit, false, useTessellation),
                includes = TerrainBaseGen.Includes,
                additionalCommands = TerrainBaseGen.BaseMapMetallicTex,
                virtualTextureFeedback = false,
            };
        }

        static class TerrainBaseGen
        {
            private static string kMainTexName = "\"Name\" = \"_MainTex\"";
            private static string kMainTexFormat = "\"Format\" = \"ARGB32\"";
            private static string kMainTexSize = "\"Size\" = \"1\"";

            private static string kMetallicTexName = "\"Name\" = \"_MetallicTex\"";
            private static string kMetallicTexFormat = "\"Format\" = \"RG16\"";
            private static string kMetallicTexSize = "\"Size\" = \"1/4\"";

            public static RenderStateCollection BaseMapGenRenderState = new RenderStateCollection()
            {
                { RenderState.ZTest(ZTest.Always) },
                { RenderState.Cull(Cull.Off) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Blend("One", "[_DstBlend]") },
            };

            public static readonly PragmaCollection BaseMapPragmas = new PragmaCollection()
            {
                { Pragma.Target(ShaderModel.Target45) },
                { Pragma.Vertex("Vert") },
                { Pragma.Fragment("Frag") },
            };

            public static IncludeCollection Includes = new IncludeCollection
            {
                { TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph },
                { TerrainIncludes.kSplatmap, IncludeLocation.Pregraph },
            };

            public static readonly AdditionalCommandCollection BaseMapMainTex = new AdditionalCommandCollection()
            {
                new AdditionalCommandDescriptor("BaseGenName", kMainTexName),
                new AdditionalCommandDescriptor("BaseGenTexFormat", kMainTexFormat),
                new AdditionalCommandDescriptor("BaseGenTexSize", kMainTexSize),
            };

            public static readonly AdditionalCommandCollection BaseMapMetallicTex = new AdditionalCommandCollection()
            {
                new AdditionalCommandDescriptor("BaseGenName", kMetallicTexName),
                new AdditionalCommandDescriptor("BaseGenTexFormat", kMetallicTexFormat),
                new AdditionalCommandDescriptor("BaseGenTexSize", kMetallicTexSize),
            };
        }
        #endregion
    }
}
