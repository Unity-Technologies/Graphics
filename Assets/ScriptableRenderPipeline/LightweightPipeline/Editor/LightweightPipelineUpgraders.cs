using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    public static class SupportedUpgradeParams
    {
        static public UpgradeParams diffuseOpaque = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Opaque,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.BaseAlpha,
            reflectionSource = ReflectionSource.NoReflection
        };

        static public UpgradeParams specularOpaque = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Opaque,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.BaseAlpha,
            reflectionSource = ReflectionSource.NoReflection
        };

        static public UpgradeParams diffuseAlpha = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Alpha,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.SpecularAlpha,
            reflectionSource = ReflectionSource.NoReflection
        };

        static public UpgradeParams specularAlpha = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Alpha,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.SpecularAlpha,
            reflectionSource = ReflectionSource.NoReflection
        };

        static public UpgradeParams diffuseAlphaCutout = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Cutout,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.SpecularAlpha,
            reflectionSource = ReflectionSource.NoReflection
        };

        static public UpgradeParams specularAlphaCutout = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Cutout,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.SpecularAlpha,
            reflectionSource = ReflectionSource.NoReflection
        };

        static public UpgradeParams diffuseCubemap = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Opaque,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.BaseAlpha,
            reflectionSource = ReflectionSource.Cubemap
        };

        static public UpgradeParams specularCubemap = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Opaque,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.BaseAlpha,
            reflectionSource = ReflectionSource.Cubemap
        };

        static public UpgradeParams diffuseCubemapAlpha = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Alpha,
            specularSource = SpecularSource.NoSpecular,
            glosinessSource = GlossinessSource.BaseAlpha,
            reflectionSource = ReflectionSource.Cubemap
        };

        static public UpgradeParams specularCubemapAlpha = new UpgradeParams()
        {
            blendMode = UpgradeBlendMode.Alpha,
            specularSource = SpecularSource.SpecularTextureAndColor,
            glosinessSource = GlossinessSource.BaseAlpha,
            reflectionSource = ReflectionSource.Cubemap
        };
    }

    public class LegacyBlinnPhongUpgrader : MaterialUpgrader
    {
        public LegacyBlinnPhongUpgrader(string oldShaderName, UpgradeParams upgradeParams)
        {
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LightweightPipeline/NonPBR", UpdateMaterialKeywords);
            SetFloat("_Mode", (float)upgradeParams.blendMode);
            SetFloat("_SpecSource", (float)upgradeParams.specularSource);
            SetFloat("_GlossinessSource", (float)upgradeParams.glosinessSource);
            SetFloat("_ReflectionSource", (float)upgradeParams.reflectionSource);

            if (oldShaderName.Contains("Legacy Shaders/Self-Illumin"))
            {
                RenameTexture("_MainTex", "_EmissionMap");
                RemoveTexture("_MainTex");
                SetColor("_EmissionColor", Color.white);
            }
        }

        public static void UpdateMaterialKeywords(Material material)
        {
            LightweightShaderHelper.SetMaterialBlendMode(material);
            UpdateMaterialSpecularSource(material);
            UpdateMaterialReflectionSource(material);
            LightweightShaderHelper.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
            LightweightShaderHelper.SetKeyword(material, "_CUBEMAP_REFLECTION", material.GetTexture("_Cube"));
            LightweightShaderHelper.SetKeyword(material, "_EMISSION", material.GetTexture("_EmissionMap"));
        }

        private static void UpdateMaterialSpecularSource(Material material)
        {
            SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecSource");
            if (specSource == SpecularSource.NoSpecular)
            {
                LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP", false);
                LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP_BASE_ALPHA", false);
                LightweightShaderHelper.SetKeyword(material, "_SPECULAR_COLOR", false);
            }
            else if (specSource == SpecularSource.SpecularTextureAndColor && material.GetTexture("_SpecGlossMap"))
            {
                GlossinessSource glossSource = (GlossinessSource)material.GetFloat("_GlossinessSource");
                if (glossSource == GlossinessSource.BaseAlpha)
                {
                    LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP", false);
                    LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP_BASE_ALPHA", true);
                }
                else
                {
                    LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP", true);
                    LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP_BASE_ALPHA", false);
                }

                LightweightShaderHelper.SetKeyword(material, "_SPECULAR_COLOR", false);
            }
            else
            {
                LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP", false);
                LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP_BASE_ALPHA", false);
                LightweightShaderHelper.SetKeyword(material, "_SPECULAR_COLOR", true);
            }
        }

        private static void UpdateMaterialReflectionSource(Material material)
        {
            LightweightShaderHelper.SetKeyword(material, "_REFLECTION_CUBEMAP", false);
            LightweightShaderHelper.SetKeyword(material, "_REFLECTION_PROBE", false);

            ReflectionSource reflectionSource = (ReflectionSource)material.GetFloat("_ReflectionSource");
            if (reflectionSource == ReflectionSource.Cubemap && material.GetTexture("_Cube"))
            {
                LightweightShaderHelper.SetKeyword(material, "_REFLECTION_CUBEMAP", true);
            }
            else if (reflectionSource == ReflectionSource.ReflectionProbe)
            {
                LightweightShaderHelper.SetKeyword(material, "_REFLECTION_PROBE", true);
            }
        }
    }

    public class ParticlesMultiplyUpgrader : MaterialUpgrader
    {
        public ParticlesMultiplyUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LightweightPipeline/Particles/Multiply");
        }
    }

    public class ParticlesAdditiveUpgrader : MaterialUpgrader
    {
        public ParticlesAdditiveUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LightweightPipeline/Particles/Additive");
        }
    }

    public class StandardUpgrader : MaterialUpgrader
    {
        public StandardUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LightweightPipeline/NonPBR");
        }
    }

    public class TerrainUpgrader : MaterialUpgrader
    {
        public TerrainUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LightweightPipeline/NonPBR");
            SetFloat("_Shininess", 1.0f);
        }
    }
}
