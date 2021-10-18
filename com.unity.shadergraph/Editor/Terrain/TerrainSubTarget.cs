using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    //[InitializeOnLoad]
    internal static class TerrainSubTarget
    {
        public const string kTerrainTag = " \"TerrainCompatible\" = \"True\" ";
        private static string dstBlendValue = "[_DstBlend]";

        public static event Func<AssetImportContext, AssetImportContext> OnImportTerrainShaderGraphAsset;

        public static readonly PragmaDescriptor InstancingOptions = new PragmaDescriptor { value = $"instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap" };

        private static void AddRPKeywords(ref PassDescriptor pd, KeywordCollection rpTerrainKeywords, DefineCollection rpTerrainDefines)
        {
            if (rpTerrainKeywords != null)
                pd.keywords.Add(rpTerrainKeywords);
            if (rpTerrainDefines != null)
                pd.defines.Add(rpTerrainDefines);
        }

        public static void Setup(ref TargetSetupContext context)
        {
            context.AddShaderDependency(GetDependencyName(TerrainSubTarget.TerrainShaders.BasemapGen), "");
            context.AddShaderDependency(GetDependencyName(TerrainSubTarget.TerrainShaders.Basemap), "");
        }

        public static IEnumerable<SubShaderDescriptor> EnumerateSubShaders(PassDescriptor rpForwardPd, SubShaderDescriptor rpSsd)
        {
            yield return GetBaseMapGenSubShader(rpForwardPd);
            yield return GetBasePassSubShader(rpSsd);
            yield return GetAddPassSubShader(rpSsd);
        }

        public static void PostProcessPass(ref PassDescriptor pd, int shaderIdx, string basemapGenTemplate = "", KeywordCollection rpTerrainKeywords = null, DefineCollection rpTerrainDefines = null, FieldCollection rpBasemapGenFields = null)
        {
            pd.includes.Add(Includes.TerrainIncludes);
            switch (shaderIdx)
            {
                case (int)TerrainShaders.Main:
                    pd.keywords.Add(Keywords.MainKeywords);
                    pd.defines.Add(Defines.TerrainShader);
                    AddRPKeywords(ref pd, rpTerrainKeywords, rpTerrainDefines);
                    pd.pragmas.Add(InstancingOptions);
                    break;
                case (int)TerrainShaders.BasemapGen:
                    pd.keywords.Add(Keywords.BasemapGenKeywords);
                    pd.defines.Add(Defines.BasemapGen);
                    AddRPKeywords(ref pd, rpTerrainKeywords, rpTerrainDefines);
                    pd.passTemplatePath = basemapGenTemplate.Equals("") ? kBaseMapGenTemplate : basemapGenTemplate;
                    pd.requiredFields = rpBasemapGenFields ?? new FieldCollection();
                    pd.useInPreview = false;
                    break;
                case (int)TerrainShaders.Basemap:
                    pd.defines.Add(Defines.BasePass);
                    pd.pragmas.Add(InstancingOptions);
                    pd.useInPreview = false;
                    break;
                case (int)TerrainShaders.Add:
                    pd.keywords.Add(Keywords.MainKeywords);
                    AddRPKeywords(ref pd, rpTerrainKeywords, rpTerrainDefines);
                    pd.defines.Add(Defines.AddPass);
                    pd.pragmas.Add(InstancingOptions);
                    pd.useInPreview = false;
                    break;
                default:
                    break;
            }
        }
        public static void PostProcessSubShader(ref SubShaderDescriptor ssd, int shaderIdx, string basemapGenTemplate = "", KeywordCollection rpTerrainKeywords = null, DefineCollection rpTerrainDefines = null, FieldCollection rpBasemapGenFields = null)
        {
            ssd.customTags.Insert(ssd.customTags.Length, kTerrainTag);
            var passes = ssd.passes.ToArray();
            PassCollection finalPasses = new PassCollection();
            for (int i = 0; i < passes.Length; i++)
            {
                var passDescriptor = passes[i].descriptor;
                PostProcessPass(ref passDescriptor, shaderIdx, basemapGenTemplate, rpTerrainKeywords, rpTerrainDefines, rpBasemapGenFields);
            }
        }

        public static class Defines
        {
            public static KeywordDescriptor BasemapGenDesc = new KeywordDescriptor()
            {
                displayName = "Generate Basemap",
                referenceName = "_TERRAIN_BASEMAP_GEN",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor BasemapGenMainDesc = new KeywordDescriptor()
            {
                displayName = "Generate Main Basemap Texture",
                referenceName = "BASEMAPGEN_MAIN",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor BasePassDesc = new KeywordDescriptor()
            {
                displayName = "Base Pass",
                referenceName = "TERRAIN_SPLAT_BASEPASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor AddPassDesc = new KeywordDescriptor()
            {
                displayName = "Add Pass",
                referenceName = "TERRAIN_SPLAT_ADDPASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TerrainDesc = new KeywordDescriptor()
            {
                displayName = "Terrain Shader",
                referenceName = "TERRAIN_ENABLED",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static DefineCollection TerrainShader = new DefineCollection
            {
                { TerrainDesc, 1 }
            };

            public static DefineCollection BasemapGen = new DefineCollection
            {
                { BasemapGenDesc, 1 }
            };

            public static DefineCollection BasemapGenMain = new DefineCollection
            {
                { BasemapGenMainDesc, 1 }
            };

            public static DefineCollection BasePass = new DefineCollection
            {
                { BasePassDesc, 1 }
            };

            public static DefineCollection AddPass = new DefineCollection
            {
                { AddPassDesc, 1 }
            };
        }

        public static class Keywords
        {
            public static KeywordDescriptor NormalMap = new KeywordDescriptor()
            {
                displayName = "Terrain Has Normal Map",
                referenceName = "_NORMALMAP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor MaskMap = new KeywordDescriptor()
            {
                displayName = "Terrain Has Mask Map",
                referenceName = "_MASKMAP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor BlendHeight = new KeywordDescriptor()
            {
                displayName = "Terrain Blend Height",
                referenceName = "_TERRAIN_BLEND_HEIGHT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor InstancedPerPixelNormal = new KeywordDescriptor()
            {
                displayName = "Terrain Instanced Per Pixel Normal",
                referenceName = "_TERRAIN_INSTANCED_PERPIXEL_NORMAL",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordCollection MainKeywords = new KeywordCollection
            {
                { BlendHeight },
                { InstancedPerPixelNormal },
                { MaskMap },
                { NormalMap },
            };

            public static KeywordCollection BasemapGenKeywords = new KeywordCollection
            {
                { NormalMap },
                { MaskMap },
                { BlendHeight },
            };
        }

        public static class Includes
        {
            public static string kTerrainProps = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Terrain/TerrainProps.hlsl";
            public static string kTerrainSplatmap = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Terrain/TerrainLayer_Includes.hlsl";

            public static IncludeCollection TerrainIncludes = new IncludeCollection()
            {
                {   kTerrainProps, IncludeLocation.Pregraph },
                {   kTerrainSplatmap, IncludeLocation.Pregraph },
            };
        }

        public enum TerrainShaders
        {
            Main = 0,
            BasemapGen = 1,
            Basemap = 2,
            Add = 3,
            Count = 4,
        }

        private static string[] TerrainShaderSuffix = new string[4]
        {
            "",
            "_BasemapGen",
            "_Basemap",
            "_Add",
        };

        private static string[] DependencyNames = new string[4]
        {
            "",
            "BaseMapGenShader",
            "BaseMapShader",
            "AddPassShader",
        };

        public static string GetDependencyName(TerrainShaders terrainShader)
        {
            return DependencyNames[Mathf.Clamp((int)terrainShader, 0, 3)];
        }

        public static string GetTerrainShaderName(string shaderName, TerrainShaders terrainShader)
        {
            return shaderName + TerrainShaderSuffix[Mathf.Clamp((int)terrainShader, 0, 3)];
        }

        public static string GetTerrainShaderName(string shaderName, string dependencyName)
        {
            return shaderName + TerrainShaderSuffix[Mathf.Clamp(GetTerrainShaderId(dependencyName), 0, 3)];
        }

        private static int GetTerrainShaderId(string dependencyName)
        {
            for (int i = 0; i < (int)TerrainShaders.Count; i++)
            {
                if (dependencyName == DependencyNames[i])
                    return i;
            }
            return -1;
        }

        public static void SetDstBlendValue(string dstBlendPropName)
        {
            dstBlendValue = dstBlendPropName;
        }

        private static class RenderStates
        {
            public static RenderStateCollection BasemapGen = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.Always) },
                { RenderState.Cull(Cull.Off) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Blend("One", dstBlendValue) },
            };

            public static RenderStateCollection Add = new RenderStateCollection
            {
                { RenderState.Blend(Blend.One, Blend.One) },
            };
        }

        #region Basemap Gen
        private static string kMainTexPassTags = "\"Name\" = \"_MainTex\" \"Format\" = \"ARGB32\" \"Size\" = \"1\"";
        private static string kMetallicTexPassTags = "\"Name\" = \"_MetallicTex\" \"Format\" = \"RG16\" \"Size\" = \"1/4\"";
        private static string kBaseMapGenTemplate = "";

        private static PassDescriptor GenerateMainTexPass(PassDescriptor subTargetForwardPass)
        {
            return new PassDescriptor()
            {
                displayName = "_MainTex",
                referenceName = "TERRAIN_BASEMAPGEN_MAINTEX",
                tags = kMainTexPassTags,
                useInPreview = false,

                validVertexBlocks = new BlockFieldDescriptor[] { BlockFields.VertexDescription.Position },
                validPixelBlocks = new BlockFieldDescriptor[] { BlockFields.SurfaceDescription.BaseColor, BlockFields.SurfaceDescription.Smoothness },

                // Set template in postprocesspass passTemplatePath = kBaseMapGenTemplate,
                renderStates = RenderStates.BasemapGen,
                // Set keywords in postprocesspass keywords = Keywords.BasemapGenKeywords,
                defines = new DefineCollection() { subTargetForwardPass.defines, Defines.BasemapGenMain },
                pragmas = subTargetForwardPass.pragmas,
                structs = subTargetForwardPass.structs,
                includes = subTargetForwardPass.includes,
                sharedTemplateDirectories = subTargetForwardPass.sharedTemplateDirectories,
                customInterpolators = subTargetForwardPass.customInterpolators,
                lightMode = "",
            };
        }

        private static PassDescriptor GenerateMetallicTexPass(PassDescriptor subTargetForwardPass)
        {
            return new PassDescriptor()
            {
                displayName = "_MetallicTex",
                referenceName = "TERRAIN_BASEMAPGEN_METALLICTEX",
                tags = kMetallicTexPassTags,
                useInPreview = false,

                validVertexBlocks = new BlockFieldDescriptor[] { BlockFields.VertexDescription.Position },
                validPixelBlocks = new BlockFieldDescriptor[] { BlockFields.SurfaceDescription.Metallic, BlockFields.SurfaceDescription.Occlusion },

                // Set template in postprocesspass passTemplatePath = kBaseMapGenTemplate,
                renderStates = RenderStates.BasemapGen,
                // Set keywords in postprocesspass keywords = Keywords.BasemapGenKeywords,
                defines = subTargetForwardPass.defines,
                pragmas = subTargetForwardPass.pragmas,
                structs = subTargetForwardPass.structs,
                includes = subTargetForwardPass.includes,
                sharedTemplateDirectories = subTargetForwardPass.sharedTemplateDirectories,
                customInterpolators = subTargetForwardPass.customInterpolators,
                lightMode = "",
            };
        }

        public static SubShaderDescriptor GetBaseMapGenSubShader(PassDescriptor subTargetForwardPass)
        {
            return new SubShaderDescriptor
            {
                generatesPreview = false,
                passes = GetPasses(subTargetForwardPass),
                shaderId = (int)TerrainShaders.BasemapGen
            };

            PassCollection GetPasses(PassDescriptor subTargetForwardPass)
            {
                var passes = new PassCollection
                {
                    GenerateMainTexPass(subTargetForwardPass),
                    GenerateMetallicTexPass(subTargetForwardPass),
                };
                return passes;
            }
        }

        #endregion

        #region Basemap
        private static string kBaseMapTemplate = "";
        public static SubShaderDescriptor GetBasePassSubShader(SubShaderDescriptor mainSubShaderDescriptor)
        {
            mainSubShaderDescriptor.shaderId = (int)TerrainShaders.Basemap;
            return mainSubShaderDescriptor;
        }

        #endregion

        #region AddPass
        public static SubShaderDescriptor GetAddPassSubShader(SubShaderDescriptor mainSubShaderDescriptor)
        {
            mainSubShaderDescriptor.shaderId = (int)TerrainShaders.Add;
            return mainSubShaderDescriptor;
        }

        #endregion

        internal static AssetImportContext ImportTerrainShaderGraphAsset(AssetImportContext ctx)
        {
            return OnImportTerrainShaderGraphAsset?.Invoke(ctx);
        }
    }

    public interface IMaySupportTerrain
    {
        bool TargetsTerrain();
    }

    static class MaySupportTerrainExtensions
    {
        public static bool SupportsTerrain(this Target target)
        {
            var terrainTarget = target as IMaySupportTerrain;
            return terrainTarget != null && terrainTarget.TargetsTerrain();
        }
    }
}
