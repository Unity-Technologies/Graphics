using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    public class LightweightMaterialUpgrader
    {
        [MenuItem("Edit/Render Pipeline/Upgrade/Lightweight/Upgrade Project Materials", priority = CoreUtils.editMenuPriority2)]
        private static void UpgradeProjectMaterials()
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to Lightweight Pipeline Materials", MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound);
        }

        [MenuItem("Edit/Render Pipeline/Upgrade/Lightweight/Upgrade Selected Materials", priority = CoreUtils.editMenuPriority2)]
        private static void UpgradeSelectedMaterials()
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            MaterialUpgrader.UpgradeSelection(upgraders, "Upgrade to Lightweight Pipeline Materials", MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound);
        }

        private static void GetUpgraders(ref List<MaterialUpgrader> upgraders)
        {
            /////////////////////////////////////
            //     Unity Standard Upgraders    //
            /////////////////////////////////////
            upgraders.Add(new StandardUpgrader("Standard"));
            upgraders.Add(new StandardUpgrader("Standard (Specular setup)"));

            /////////////////////////////////////
            // Legacy Shaders upgraders         /
            /////////////////////////////////////
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Bumped Specular", SupportedUpgradeParams.specularOpaque));

            // TODO:
            //upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped Diffuse", SupportedUpgradeParams.diffuseCubemap));
            //upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped Specular", SupportedUpgradeParams.specularOpaque));
            //upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Diffuse", SupportedUpgradeParams.diffuseCubemap));
            //upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Specular", SupportedUpgradeParams.specularOpaque));

            // Self-Illum upgrader
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Bumped Specular", SupportedUpgradeParams.specularOpaque));

            // Alpha Blended
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Diffuse", SupportedUpgradeParams.diffuseAlpha));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Specular", SupportedUpgradeParams.specularAlpha));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Bumped Diffuse", SupportedUpgradeParams.diffuseAlpha));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Bumped Specular", SupportedUpgradeParams.specularAlpha));

            // Cutout
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/Diffuse", SupportedUpgradeParams.diffuseAlphaCutout));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/Specular", SupportedUpgradeParams.specularAlphaCutout));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/Bumped Diffuse", SupportedUpgradeParams.diffuseAlphaCutout));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/Bumped Specular", SupportedUpgradeParams.specularAlphaCutout));

            /////////////////////////////////////
            // Mobile Upgraders                 /
            /////////////////////////////////////
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Bumped Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Bumped Specular (1 Directional Light)", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Unlit (Supports Lightmap)", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/VertexLit", SupportedUpgradeParams.specularOpaque));

            ////////////////////////////////////
            // Terrain Upgraders              //
            ////////////////////////////////////
            upgraders.Add(new TerrainUpgrader("Nature/Terrain/Standard"));

            ////////////////////////////////////
            // Particle Upgraders             //
            ////////////////////////////////////
            upgraders.Add(new ParticleUpgrader("Particles/Standard Surface"));
            upgraders.Add(new ParticleUpgrader("Particles/Standard Unlit"));
        }
    }

    public static class SupportedUpgradeParams
    {
        static public UpgradeParams diffuseOpaque = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Opaque,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams specularOpaque = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Opaque,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams diffuseAlpha = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Alpha,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.SpecularAlpha,
        };

        static public UpgradeParams specularAlpha = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Alpha,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.SpecularAlpha,
        };

        static public UpgradeParams diffuseAlphaCutout = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Cutout,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.SpecularAlpha,
        };

        static public UpgradeParams specularAlphaCutout = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Cutout,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.SpecularAlpha,
        };

        static public UpgradeParams diffuseCubemap = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Opaque,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams specularCubemap = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Opaque,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams diffuseCubemapAlpha = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Alpha,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams specularCubemapAlpha = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Alpha,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.BaseAlpha,
        };
    }

    public class StandardUpgrader : MaterialUpgrader
    {
        public static void UpdateStandardMaterialKeywords(Material material)
        {
            material.SetFloat("_WorkflowMode", 1.0f);
            CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
        }

        public static void UpdateStandardSpecularMaterialKeywords(Material material)
        {
            material.SetFloat("_WorkflowMode", 0.0f);
            CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
            CoreUtils.SetKeyword(material, "_SPECULAR_SETUP", true);
        }

        public StandardUpgrader(string oldShaderName)
        {
            string standardShaderPath = LightweightShaderUtils.GetShaderPath(ShaderPathID.STANDARD_PBS);
            if (oldShaderName.Contains("Specular"))
                RenameShader(oldShaderName, standardShaderPath, UpdateStandardSpecularMaterialKeywords);
            else
                RenameShader(oldShaderName, standardShaderPath, UpdateStandardMaterialKeywords);
        }
    }

    public class StandardSimpleLightingUpgrader : MaterialUpgrader
    {
        public StandardSimpleLightingUpgrader(string oldShaderName, UpgradeParams upgradeParams)
        {
            RenameShader(oldShaderName, LightweightShaderUtils.GetShaderPath(ShaderPathID.STANDARD_SIMPLE_LIGHTING), UpdateMaterialKeywords);
            SetFloat("_Mode", (float)upgradeParams.blendMode);
            SetFloat("_SpecSource", (float)upgradeParams.specularSource);
            SetFloat("_GlossinessSource", (float)upgradeParams.glosinessSource);

            if (oldShaderName.Contains("Legacy Shaders/Self-Illumin"))
            {
                RenameTexture("_MainTex", "_EmissionMap");
                RemoveTexture("_MainTex");
                SetColor("_EmissionColor", Color.white);
            }
        }

        public static void UpdateMaterialKeywords(Material material)
        {
            material.shaderKeywords = null;
            LightweightShaderGUI.SetupMaterialBlendMode(material);
            UpdateMaterialSpecularSource(material);
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);
        }

        private static void UpdateMaterialSpecularSource(Material material)
        {
            SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecSource");
            if (specSource == SpecularSource.NoSpecular)
            {
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", false);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", false);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
            }
            else
            {
                GlossinessSource glossSource = (GlossinessSource)material.GetFloat("_GlossinessSource");
                bool hasGlossMap = material.GetTexture("_SpecGlossMap");
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasGlossMap);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", !hasGlossMap);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", glossSource == GlossinessSource.BaseAlpha);
            }
        }
    }

    public class TerrainUpgrader : MaterialUpgrader
    {
        public TerrainUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, LightweightShaderUtils.GetShaderPath(ShaderPathID.STANDARD_TERRAIN));
        }
    }

    public class ParticleUpgrader : MaterialUpgrader
    {
        public ParticleUpgrader(string oldShaderName)
        {
            if (oldShaderName.Contains("Unlit"))
                RenameShader(oldShaderName, LightweightShaderUtils.GetShaderPath(ShaderPathID.STANDARD_PARTICLES_UNLIT));
            else
                RenameShader(oldShaderName, LightweightShaderUtils.GetShaderPath(ShaderPathID.STANDARD_PARTICLES_LIT));
        }
    }
}
