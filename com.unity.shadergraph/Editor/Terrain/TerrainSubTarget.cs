using System;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    //[InitializeOnLoad]
    internal static class TerrainSubTarget
    {
        public const string kTerrainTag = "\"TerrainCompatible\" = \"True\"";
        private static string dstBlendValue = "[_DstBlend]";

        public static event Func<AssetImportContext, AssetImportContext> OnImportTerrainShaderGraphAsset;

        public static readonly PragmaDescriptor InstancingOptions = new PragmaDescriptor { value = $"instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap" };

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

            public static DefineCollection BasemapGen = new DefineCollection
            {
                { BasemapGenDesc, 1 }
            };

            public static DefineCollection BasemapGenMain = new DefineCollection
            {
                { BasemapGenMainDesc, 1 }
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

        private static PassDescriptor GenerateMainTexPass()
        {
            return new PassDescriptor()
            {
                displayName = "_MainTex",
                referenceName = "TERRAIN_BASEMAPGEN_MAINTEX",
                tags = kMainTexPassTags,
                useInPreview = false,

                validVertexBlocks = new BlockFieldDescriptor[] { BlockFields.VertexDescription.Position },
                validPixelBlocks = new BlockFieldDescriptor[] { BlockFields.SurfaceDescription.BaseColor, BlockFields.SurfaceDescription.Smoothness },

                passTemplatePath = kBaseMapGenTemplate,
                renderStates = RenderStates.BasemapGen,
                keywords = Keywords.BasemapGenKeywords,
                defines = new DefineCollection()
                {
                    Defines.BasemapGen,
                    Defines.BasemapGenMain,
                },
                pragmas = new PragmaCollection(),
                structs = new StructCollection(),
                includes = new IncludeCollection(),
                sharedTemplateDirectories = new string[0],
                lightMode = "",
            };
        }

        private static PassDescriptor GenerateMetallicTexPass()
        {
            return new PassDescriptor()
            {
                displayName = "_MetallicTex",
                referenceName = "TERRAIN_BASEMAPGEN_METALLICTEX",
                tags = kMetallicTexPassTags,
                useInPreview = false,

                validVertexBlocks = new BlockFieldDescriptor[] { BlockFields.VertexDescription.Position },
                validPixelBlocks = new BlockFieldDescriptor[] { BlockFields.SurfaceDescription.Metallic },

                passTemplatePath = kBaseMapGenTemplate,
                renderStates = RenderStates.BasemapGen,
                keywords = Keywords.BasemapGenKeywords,
                defines = new DefineCollection { Defines.BasemapGen },
                pragmas = new PragmaCollection(),
                structs = new StructCollection(),
                includes = new IncludeCollection(),
                sharedTemplateDirectories = new string[0],
                lightMode = "",
            };
        }

        public static SubShaderDescriptor GetBaseMapGenSubShader()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = false,
                passes = GetPasses(),
                shaderId = (int)TerrainShaders.BasemapGen
            };

            PassCollection GetPasses()
            {
                var passes = new PassCollection
                {
                    GenerateMainTexPass(),
                    GenerateMetallicTexPass(),
                };
                return passes;
            }
        }

        #endregion

        #region Basemap
        private static string kBaseMapTemplate = "";
        public static SubShaderDescriptor GetBasemapSubShader(SubShaderDescriptor mainSubShaderDescriptor)
        {
            return mainSubShaderDescriptor;
        }

        #endregion

        internal static AssetImportContext CreateBasemapGenShader(AssetImportContext ctx)
        {
            // Fullscreen triangle VS, PS outputs to _MainTex and _MetallicTex
            string basemapShaderText = "";
            // Find subshadergraph for terrain layer blend

#if UNITY_2021_1_OR_NEWER
            Shader basemapGenShader = ShaderUtil.CreateShaderAsset(ctx, basemapShaderText, false);
#else
            // earlier builds of Unity may or may not have it
            // here we try to invoke the new version via reflection
            var createShaderAssetMethod = typeof(ShaderUtil).GetMethod(
                "CreateShaderAsset",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.ExactBinding,
                null,
                new Type[] { typeof(AssetImportContext), typeof(string), typeof(bool) },
                null);

            if (createShaderAssetMethod != null)
            {
                shader = createShaderAssetMethod.Invoke(null, new Object[] { ctx, basemapShaderText, false }) as Shader;
            }
            else
            {
                // method doesn't exist in this version of Unity, call old version
                // this doesn't create dependencies properly, but is the best that we can do
                shader = ShaderUtil.CreateShaderAsset(basemapShaderText, false);
            }
#endif

            ctx.AddObjectToAsset("BasemapGen", basemapGenShader);
            return ctx;
        }

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
