using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.LowendMobile
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
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/NonPBR", UpdateMaterialKeywords);
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
            UpdateMaterialBlendMode(material);
            UpdateMaterialSpecularSource(material);
            UpdateMaterialReflectionSource(material);
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
            SetKeyword(material, "_CUBEMAP_REFLECTION", material.GetTexture("_Cube"));
            SetKeyword(material, "_EMISSION", material.GetTexture("_EmissionMap"));
        }

        private static void UpdateMaterialBlendMode(Material material)
        {
            UpgradeBlendMode mode = (UpgradeBlendMode)material.GetFloat("_Mode");
            switch (mode)
            {
                case UpgradeBlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    SetKeyword(material, "_ALPHATEST_ON", false);
                    SetKeyword(material, "_ALPHABLEND_ON", false);
                    material.renderQueue = -1;
                    break;

                case UpgradeBlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    SetKeyword(material, "_ALPHATEST_ON", true);
                    SetKeyword(material, "_ALPHABLEND_ON", false);
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;

                case UpgradeBlendMode.Alpha:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    SetKeyword(material, "_ALPHATEST_ON", false);
                    SetKeyword(material, "_ALPHABLEND_ON", true);
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
            }
        }

        private static void UpdateMaterialSpecularSource(Material material)
        {
            SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecSource");
            if (specSource == SpecularSource.NoSpecular)
            {
                SetKeyword(material, "_SPECGLOSSMAP", false);
                SetKeyword(material, "_SPECGLOSSMAP_BASE_ALPHA", false);
                SetKeyword(material, "_SPECULAR_COLOR", false);
            }
            else if (specSource == SpecularSource.SpecularTextureAndColor && material.GetTexture("_SpecGlossMap"))
            {
                GlossinessSource glossSource = (GlossinessSource)material.GetFloat("_GlossinessSource");
                if (glossSource == GlossinessSource.BaseAlpha)
                {
                    SetKeyword(material, "_SPECGLOSSMAP", false);
                    SetKeyword(material, "_SPECGLOSSMAP_BASE_ALPHA", true);
                }
                else
                {
                    SetKeyword(material, "_SPECGLOSSMAP", true);
                    SetKeyword(material, "_SPECGLOSSMAP_BASE_ALPHA", false);
                }

                SetKeyword(material, "_SPECULAR_COLOR", false);
            }
            else
            {
                SetKeyword(material, "_SPECGLOSSMAP", false);
                SetKeyword(material, "_SPECGLOSSMAP_BASE_ALPHA", false);
                SetKeyword(material, "_SPECULAR_COLOR", true);
            }
        }

        private static void UpdateMaterialReflectionSource(Material material)
        {
            SetKeyword(material, "_REFLECTION_CUBEMAP", false);
            SetKeyword(material, "_REFLECTION_PROBE", false);

            ReflectionSource reflectionSource = (ReflectionSource)material.GetFloat("_ReflectionSource");
            if (reflectionSource == ReflectionSource.Cubemap && material.GetTexture("_Cube"))
            {
                SetKeyword(material, "_REFLECTION_CUBEMAP", true);
            }
            else if (reflectionSource == ReflectionSource.ReflectionProbe)
            {
                SetKeyword(material, "_REFLECTION_PROBE", true);
            }
        }

        private static void SetKeyword(Material material, string keyword, bool enable)
        {
            if (enable)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }

    public class ParticlesMultiplyUpgrader : MaterialUpgrader
    {
        public ParticlesMultiplyUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/Particles/Multiply");
        }
    }

    public class ParticlesAdditiveUpgrader : MaterialUpgrader
    {
        public ParticlesAdditiveUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/Particles/Additive");
        }
    }

    public class StandardUpgrader : MaterialUpgrader
    {
        public StandardUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/NonPBR");
        }
    }

    public class TerrainUpgrader : MaterialUpgrader
    {
        public TerrainUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/NonPBR");
            SetFloat("_Shininess", 1.0f);
        }
    }
}
