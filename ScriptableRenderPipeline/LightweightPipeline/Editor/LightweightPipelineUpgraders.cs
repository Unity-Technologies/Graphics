using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
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

    public class LegacyBlinnPhongUpgrader : MaterialUpgrader
    {
        public LegacyBlinnPhongUpgrader(string oldShaderName, UpgradeParams upgradeParams)
        {
            RenameShader(oldShaderName, LightweightPipelineAsset.m_SimpleLightShaderPath, UpdateMaterialKeywords);
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
            LightweightShaderHelper.SetMaterialBlendMode(material);
            UpdateMaterialSpecularSource(material);
            LightweightShaderHelper.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            LightweightShaderHelper.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);
        }

        private static void UpdateMaterialSpecularSource(Material material)
        {
            SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecSource");
            if (specSource == SpecularSource.NoSpecular)
            {
                LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP", false);
                LightweightShaderHelper.SetKeyword(material, "_SPECULAR_COLOR", false);
                LightweightShaderHelper.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
            }
            else
            {
                GlossinessSource glossSource = (GlossinessSource)material.GetFloat("_GlossinessSource");
                bool hasGlossMap = material.GetTexture("_SpecGlossMap");
                LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP", hasGlossMap);
                LightweightShaderHelper.SetKeyword(material, "_SPECULAR_COLOR", !hasGlossMap);
                LightweightShaderHelper.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", glossSource == GlossinessSource.BaseAlpha);
            }
        }
    }

    public class StandardUpgrader : MaterialUpgrader
    {
        public StandardUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, LightweightPipelineAsset.m_PBSShaderPath);
        }
    }

    public class TerrainUpgrader : MaterialUpgrader
    {
        public TerrainUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, LightweightPipelineAsset.m_PBSShaderPath);
            SetFloat("_Shininess", 1.0f);
        }
    }
}
