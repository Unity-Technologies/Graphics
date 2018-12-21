using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    internal sealed class LightweightRenderPipelineMaterialUpgrader
    {
        private LightweightRenderPipelineMaterialUpgrader()
        {
        }

        [MenuItem("Edit/Render Pipeline/Upgrade Project Materials to LightweightRP Materials", priority = CoreUtils.editMenuPriority2)]
        private static void UpgradeProjectMaterials()
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to LightweightRP Materials", MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound);
        }

        [MenuItem("Edit/Render Pipeline/Upgrade Selected Materials to LightweightRP Materials", priority = CoreUtils.editMenuPriority2)]
        private static void UpgradeSelectedMaterials()
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            MaterialUpgrader.UpgradeSelection(upgraders, "Upgrade to LightweightRP Materials", MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound);
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
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Diffuse Detail", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Diffuse Fast", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Bumped Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Parallax Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Parallax Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/VertexLit", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/VertexLit", SupportedUpgradeParams.specularAlphaCutout));

            // Reflective
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped Diffuse", SupportedUpgradeParams.diffuseCubemap));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped Specular", SupportedUpgradeParams.specularCubemap));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped Unlit", SupportedUpgradeParams.diffuseCubemap));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped VertexLit", SupportedUpgradeParams.diffuseCubemap));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Diffuse", SupportedUpgradeParams.diffuseCubemap));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Specular", SupportedUpgradeParams.specularCubemap));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/VertexLit", SupportedUpgradeParams.diffuseCubemap));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Parallax Diffuse", SupportedUpgradeParams.diffuseCubemap));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Parallax Specular", SupportedUpgradeParams.specularCubemap));

            // Self-Illum upgrader
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Parallax Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Bumped Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Parallax Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/VertexLit", SupportedUpgradeParams.specularOpaque));

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

            // Lightmapped
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/VertexLit", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/Bumped Specular", SupportedUpgradeParams.specularOpaque));

            /////////////////////////////////////
            // Sprites Upgraders
            /////////////////////////////////////
            upgraders.Add(new StandardSimpleLightingUpgrader("Sprites/Diffuse", SupportedUpgradeParams.diffuseAlpha));

            /////////////////////////////////////
            // UI Upgraders
            /////////////////////////////////////
            upgraders.Add(new StandardSimpleLightingUpgrader("UI/Lit/Bumped", SupportedUpgradeParams.diffuseAlphaCutout));
            upgraders.Add(new StandardSimpleLightingUpgrader("UI/Lit/Detail", SupportedUpgradeParams.diffuseAlphaCutout));
            upgraders.Add(new StandardSimpleLightingUpgrader("UI/Lit/Refraction", SupportedUpgradeParams.diffuseAlphaCutout));
            upgraders.Add(new StandardSimpleLightingUpgrader("UI/Lit/Refraction Detail", SupportedUpgradeParams.diffuseAlphaCutout));
            upgraders.Add(new StandardSimpleLightingUpgrader("UI/Lit/Transparent", SupportedUpgradeParams.diffuseAlpha));


            /////////////////////////////////////
            // Mobile Upgraders                 /
            /////////////////////////////////////
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Bumped Specular", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Bumped Specular (1 Directional Light)", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Unlit (Supports Lightmap)", SupportedUpgradeParams.diffuseOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/VertexLit", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/VertexLit (Only Directional Lights)", SupportedUpgradeParams.specularOpaque));
            upgraders.Add(new StandardSimpleLightingUpgrader("Mobile/Particles/VertexLit Blended", SupportedUpgradeParams.specularOpaque));

            ////////////////////////////////////
            // Terrain Upgraders              //
            ////////////////////////////////////
            upgraders.Add(new TerrainUpgrader("Nature/Terrain/Standard"));

            ////////////////////////////////////
            // Particle Upgraders             //
            ////////////////////////////////////
            upgraders.Add(new ParticleUpgrader("Particles/Standard Surface"));
            upgraders.Add(new ParticleUpgrader("Particles/Standard Unlit"));
            upgraders.Add(new ParticleUpgrader("Particles/VertexLit Blended"));

            ////////////////////////////////////
            // Autodesk Interactive           //
            ////////////////////////////////////
            upgraders.Add(new AutodeskInteractiveUpgrader("Autodesk Interactive"));
        }
    }

    public static class SupportedUpgradeParams
    {
        static public UpgradeParams diffuseOpaque = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams specularOpaque = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams diffuseAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.SpecularAlpha,
        };

        static public UpgradeParams specularAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.SpecularAlpha,
        };

        static public UpgradeParams diffuseAlphaCutout = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = true,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.SpecularAlpha,
        };

        static public UpgradeParams specularAlphaCutout = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = true,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.SpecularAlpha,
        };

        static public UpgradeParams diffuseCubemap = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams specularCubemap = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams diffuseCubemapAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.BaseAlpha,
        };

        static public UpgradeParams specularCubemapAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.BaseAlpha,
        };
    }

    public class StandardUpgrader : MaterialUpgrader
    {
        public static void UpdateStandardMaterialKeywords(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            material.SetFloat("_WorkflowMode", 1.0f);
            CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
        }

        public static void UpdateStandardSpecularMaterialKeywords(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            material.SetFloat("_WorkflowMode", 0.0f);
            CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
            CoreUtils.SetKeyword(material, "_SPECULAR_SETUP", true);
        }

        public StandardUpgrader(string oldShaderName)
        {
            if (oldShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            string standardShaderPath = ShaderUtils.GetShaderPath(ShaderPathID.PhysicallyBased);

            if (oldShaderName.Contains("Specular"))
                RenameShader(oldShaderName, standardShaderPath, UpdateStandardSpecularMaterialKeywords);
            else
                RenameShader(oldShaderName, standardShaderPath, UpdateStandardMaterialKeywords);
        }
    }

    internal class StandardSimpleLightingUpgrader : MaterialUpgrader
    {
        public StandardSimpleLightingUpgrader(string oldShaderName, UpgradeParams upgradeParams)
        {
            if (oldShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.SimpleLit), UpdateMaterialKeywords);

            SetFloat("_Surface", (float)upgradeParams.surfaceType);
            SetFloat("_Blend", (float)upgradeParams.blendMode);
            SetFloat("_AlphaClip", upgradeParams.alphaClip ? 1 : 0);
            SetFloat("_SpecSource", (float)upgradeParams.specularSource);
            SetFloat("_GlossinessSource", (float)upgradeParams.glosinessSource);

            if (oldShaderName.Contains("Legacy Shaders/Self-Illumin"))
            {
                RenameTexture("_Illum", "_EmissionMap");
                RemoveTexture("_Illum");
                SetColor("_EmissionColor", Color.white);
            }
        }

        public static void UpdateMaterialKeywords(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            material.shaderKeywords = null;
            BaseShaderGUI.SetupMaterialBlendMode(material);
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
            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.TerrainPhysicallyBased));
        }
    }

    public class ParticleUpgrader : MaterialUpgrader
    {
        public ParticleUpgrader(string oldShaderName)
        {
            if (oldShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            if (oldShaderName.Contains("Unlit"))
                RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.ParticlesUnlit));
            else
                RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.ParticlesPhysicallyBased));
        }
    }

    public class AutodeskInteractiveUpgrader : MaterialUpgrader
    {
        public AutodeskInteractiveUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "Lightweight Render Pipeline/Autodesk Interactive/Autodesk Interactive");
        }

        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            base.Convert(srcMaterial, dstMaterial);
            dstMaterial.SetFloat("_UseColorMap", srcMaterial.GetTexture("_MainTex") ? 1.0f : .0f);
            dstMaterial.SetFloat("_UseMetallicMap", srcMaterial.GetTexture("_MetallicGlossMap") ? 1.0f : .0f);
            dstMaterial.SetFloat("_UseNormalMap", srcMaterial.GetTexture("_BumpMap") ? 1.0f : .0f);
            dstMaterial.SetFloat("_UseRoughnessMap", srcMaterial.GetTexture("_SpecGlossMap") ? 1.0f : .0f);
            dstMaterial.SetFloat("_UseEmissiveMap", srcMaterial.GetTexture("_EmissionMap") ? 1.0f : .0f);
            dstMaterial.SetFloat("_UseAoMap", srcMaterial.GetTexture("_OcclusionMap") ? 1.0f : .0f);
            dstMaterial.SetVector("_UvOffset", srcMaterial.GetTextureOffset("_MainTex"));
            dstMaterial.SetVector("_UvTiling", srcMaterial.GetTextureScale("_MainTex"));
        }
    }
}
