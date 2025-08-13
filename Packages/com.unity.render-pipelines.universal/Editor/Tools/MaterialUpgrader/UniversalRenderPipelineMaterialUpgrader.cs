using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[assembly: InternalsVisibleTo("MaterialPostprocessor")]
namespace UnityEditor.Rendering.Universal
{
    internal sealed class UniversalRenderPipelineMaterialUpgrader : RenderPipelineConverter
    {
        public override string name => "Material Upgrade";
        public override string info => $@"This converter upgrades Materials from the Built-in Render Pipeline to URP.
It uses {typeof(MaterialUpgrader).Name}instances that implement {typeof(IMaterialUpgradersProvider).Name}.";

        public override int priority => -1000;
        public override Type container => typeof(BuiltInToURPConverterContainer);

        List<string> m_AssetsToConvert = new List<string>();

        static List<MaterialUpgrader> m_Upgraders;

        public UniversalRenderPipelineMaterialUpgrader()
        {
            m_Upgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));
        }

        /// <inheritdoc/>
        public override void OnInitialize(InitializeConverterContext context, Action callback)
        {
            List<ConverterItemDescriptor> descriptors = new List<ConverterItemDescriptor>();

            var entries = MaterialUpgrader.FetchAllUpgradableMaterialsForPipeline(typeof(UniversalRenderPipelineAsset));
            foreach (var material in entries)
            {
                ConverterItemDescriptor desc = new ConverterItemDescriptor()
                {
                    name = material.name,
                    info = AssetDatabase.GetAssetPath(material),
                    warningMessage = string.Empty,
                    helpLink = string.Empty,
                };

                descriptors.Add(desc);
            }

            descriptors.Sort(delegate (ConverterItemDescriptor a, ConverterItemDescriptor b)
            {
                return string.Compare(a.name, b.name, StringComparison.Ordinal);
            });

            foreach (var desc in descriptors)
            {
                context.AddAssetToConvert(desc);
                m_AssetsToConvert.Add(desc.info);
            }

            callback.Invoke();
        }

        /// <inheritdoc/>
        public override void OnRun(ref RunItemContext context)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(context.item.descriptor.info);
            string message = String.Empty;

            if (!MaterialUpgrader.Upgrade(mat, m_Upgraders, MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound, ref message))
            {
                context.didFail = true;
                context.info = message;
            }
        }

        /// <inheritdoc/>
        public override void OnClicked(int index)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Material>(m_AssetsToConvert[index]));
        }

        internal static void DisableKeywords(Material material)
        {
            // LOD fade is now controlled by the render pipeline, and not the individual material, so disable it.
            material.DisableKeyword("LOD_FADE_CROSSFADE");
        }
    }

    /// <summary>
    /// Upgrade parameters for the supported shaders.
    /// </summary>
    public static class SupportedUpgradeParams
    {
        /// <summary>
        /// Upgrade parameters for diffuse Opaque.
        /// </summary>
        public static UpgradeParams diffuseOpaque = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        /// <summary>
        /// Upgrade parameters for specular opaque.
        /// </summary>
        public static UpgradeParams specularOpaque = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        /// <summary>
        /// Upgrade parameters for diffuse alpha.
        /// </summary>
        public static UpgradeParams diffuseAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            smoothnessSource = SmoothnessSource.SpecularAlpha,
        };

        /// <summary>
        /// Upgrade parameters for specular alpha.
        /// </summary>
        public static UpgradeParams specularAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.SpecularAlpha,
        };

        /// <summary>
        /// Upgrade parameters for diffuse alpha cutout.
        /// </summary>
        public static UpgradeParams diffuseAlphaCutout = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = true,
            specularSource = SpecularSource.NoSpecular,
            smoothnessSource = SmoothnessSource.SpecularAlpha,
        };

        /// <summary>
        /// Upgrade parameters for specular alpha cutout.
        /// </summary>
        public static UpgradeParams specularAlphaCutout = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = true,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.SpecularAlpha,
        };

        /// <summary>
        /// Upgrade parameters for diffuse cubemap.
        /// </summary>
        public static UpgradeParams diffuseCubemap = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        /// <summary>
        /// Upgrade parameters for specular cubemap.
        /// </summary>
        public static UpgradeParams specularCubemap = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        /// <summary>
        /// Upgrade parameters for diffuse cubemap alpha.
        /// </summary>
        public static UpgradeParams diffuseCubemapAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        /// <summary>
        /// Upgrade parameters for specular cubemap alpha.
        /// </summary>
        public static UpgradeParams specularCubemapAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };
    }

    /// <summary>
    /// Upgrader for the standard and standard (specular setup) shaders.
    /// </summary>
    public class StandardUpgrader : MaterialUpgrader
    {
        enum LegacyRenderingMode
        {
            Opaque,
            Cutout,
            Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        /// <summary>
        /// Updates keywords for the standard shader.
        /// </summary>
        /// <param name="material">The material to update.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void UpdateStandardMaterialKeywords(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            if (material.GetTexture("_MetallicGlossMap"))
                material.SetFloat("_Smoothness", material.GetFloat("_GlossMapScale"));
            else
                material.SetFloat("_Smoothness", material.GetFloat("_Glossiness"));

            if (material.IsKeywordEnabled("_ALPHATEST_ON"))
            {
                material.SetFloat("_AlphaClip", 1.0f);
            }

            material.SetFloat("_WorkflowMode", 1.0f);
            CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
            UpdateSurfaceTypeAndBlendMode(material);
            UpdateDetailScaleOffset(material);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            UniversalRenderPipelineMaterialUpgrader.DisableKeywords(material);
        }

        /// <summary>
        /// Updates keywords for the standard specular shader.
        /// </summary>
        /// <param name="material">The material to update.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void UpdateStandardSpecularMaterialKeywords(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            if (material.GetTexture("_SpecGlossMap"))
                material.SetFloat("_Smoothness", material.GetFloat("_GlossMapScale"));
            else
                material.SetFloat("_Smoothness", material.GetFloat("_Glossiness"));

            material.SetFloat("_WorkflowMode", 0.0f);
            CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
            CoreUtils.SetKeyword(material, "_SPECULAR_SETUP", true);
            UpdateSurfaceTypeAndBlendMode(material);
            UpdateDetailScaleOffset(material);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            UniversalRenderPipelineMaterialUpgrader.DisableKeywords(material);
        }

        static void UpdateDetailScaleOffset(Material material)
        {
            // In URP details tile/offset is multipied with base tile/offset, where in builtin is not
            // Basically we setup new tile/offset values that in shader they would result in same values as in builtin
            // This archieved with inverted calculation where scale=detailScale/baseScale and tile=detailOffset-baseOffset*scale
            var baseScale = material.GetTextureScale("_BaseMap");
            var baseOffset = material.GetTextureOffset("_BaseMap");
            var detailScale = material.GetTextureScale("_DetailAlbedoMap");
            var detailOffset = material.GetTextureOffset("_DetailAlbedoMap");
            var scale = new Vector2(baseScale.x == 0 ? 0 : detailScale.x / baseScale.x, baseScale.y == 0 ? 0 : detailScale.y / baseScale.y);
            material.SetTextureScale("_DetailAlbedoMap", scale);
            material.SetTextureOffset("_DetailAlbedoMap", new Vector2((detailOffset.x - baseOffset.x * scale.x), (detailOffset.y - baseOffset.y * scale.y)));
        }

        // Converts from legacy RenderingMode to new SurfaceType and BlendMode
        static void UpdateSurfaceTypeAndBlendMode(Material material)
        {
            // Property _Mode is already renamed to _Surface at this point
            var legacyRenderingMode = (LegacyRenderingMode)material.GetFloat("_Surface");
            if (legacyRenderingMode == LegacyRenderingMode.Transparent)
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Transparent);
                material.SetFloat("_Blend", (float)BaseShaderGUI.BlendMode.Premultiply);
            }
            else if (legacyRenderingMode == LegacyRenderingMode.Fade)
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Transparent);
                material.SetFloat("_Blend", (float)BaseShaderGUI.BlendMode.Alpha);
            }
            else
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Opaque);
            }
        }

        /// <summary>
        /// Constructor for the StandardUpgrader class.
        /// </summary>
        /// <param name="oldShaderName">The name of the old shader.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public StandardUpgrader(string oldShaderName)
        {
            if (oldShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            string standardShaderPath = ShaderUtils.GetShaderPath(ShaderPathID.Lit);

            if (oldShaderName.Contains("Specular"))
            {
                RenameShader(oldShaderName, standardShaderPath, UpdateStandardSpecularMaterialKeywords);
            }
            else
            {
                RenameShader(oldShaderName, standardShaderPath, UpdateStandardMaterialKeywords);
            }

            RenameFloat("_Mode", "_Surface");
            RenameFloat("_Mode", "_AlphaClip", renderingMode => renderingMode == 1.0f);
            RenameTexture("_MainTex", "_BaseMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_GlossyReflections", "_EnvironmentReflections");
        }
    }

    internal class StandardSimpleLightingUpgrader : MaterialUpgrader
    {
        internal StandardSimpleLightingUpgrader(string oldShaderName, UpgradeParams upgradeParams)
        {
            if (oldShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.SimpleLit), UpdateMaterialKeywords);

            SetFloat("_Surface", (float)upgradeParams.surfaceType);
            SetFloat("_Blend", (float)upgradeParams.blendMode);
            SetFloat("_AlphaClip", upgradeParams.alphaClip ? 1 : 0);
            SetFloat("_SpecularHighlights", (float)upgradeParams.specularSource);
            SetFloat("_SmoothnessSource", (float)upgradeParams.smoothnessSource);

            RenameTexture("_MainTex", "_BaseMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_Shininess", "_Smoothness");

            if (oldShaderName.Contains("Legacy Shaders/Self-Illumin"))
            {
                RenameTexture("_Illum", "_EmissionMap");
                RemoveTexture("_Illum");
                SetColor("_EmissionColor", Color.white);
            }
        }

        internal static void UpdateMaterialKeywords(Material material)
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
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive) != 0;
            CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);
            UniversalRenderPipelineMaterialUpgrader.DisableKeywords(material);
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
                SmoothnessSource glossSource = (SmoothnessSource)material.GetFloat("_SmoothnessSource");
                bool hasGlossMap = material.GetTexture("_SpecGlossMap");
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasGlossMap);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", !hasGlossMap);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", glossSource == SmoothnessSource.BaseAlpha);
            }
        }
    }

    /// <summary>
    /// Upgrader for terrain materials.
    /// </summary>
    public class TerrainUpgrader : MaterialUpgrader
    {
        /// <summary>
        /// Constructor for the terrain upgrader.
        /// </summary>
        /// <param name="oldShaderName">The name of the old shader.</param>
        public TerrainUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.TerrainLit), UniversalRenderPipelineMaterialUpgrader.DisableKeywords);
        }

    }

    internal class SpeedTreeUpgrader : MaterialUpgrader
    {
        internal SpeedTreeUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.SpeedTree7), UniversalRenderPipelineMaterialUpgrader.DisableKeywords);
        }
    }
    internal class SpeedTreeBillboardUpgrader : MaterialUpgrader
    {
        internal SpeedTreeBillboardUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.SpeedTree7Billboard), UniversalRenderPipelineMaterialUpgrader.DisableKeywords);
        }
    }

    /// <summary>
    /// Upgrader for particle materials.
    /// </summary>
    public class ParticleUpgrader : MaterialUpgrader
    {
        /// <summary>
        /// Constructor for the particle upgrader.
        /// </summary>
        /// <param name="oldShaderName">The name of the old shader.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ParticleUpgrader(string oldShaderName)
        {
            if (oldShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            RenameFloat("_Mode", "_Surface");

            if (oldShaderName.Contains("Unlit"))
            {
                RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.ParticlesUnlit), UpdateUnlit);
            }
            else
            {
                RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.ParticlesLit),
                    UpdateStandardSurface);
                RenameFloat("_Glossiness", "_Smoothness");
            }

            RenameTexture("_MainTex", "_BaseMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_FlipbookMode", "_FlipbookBlending");
        }

        /// <summary>
        /// Updates the standard shader surface properties.
        /// </summary>
        /// <param name="material"></param>
        public static void UpdateStandardSurface(Material material)
        {
            UpdateSurfaceBlendModes(material);
            UniversalRenderPipelineMaterialUpgrader.DisableKeywords(material);
        }

        /// <summary>
        /// Updates the unlit shader properties.
        /// </summary>
        /// <param name="material"></param>
        public static void UpdateUnlit(Material material)
        {
            UpdateSurfaceBlendModes(material);
            UniversalRenderPipelineMaterialUpgrader.DisableKeywords(material);
        }

        /// <summary>
        /// Updates the blending mode properties.
        /// </summary>
        /// <param name="material"></param>
        public static void UpdateSurfaceBlendModes(Material material)
        {
            switch (material.GetFloat("_Mode"))
            {
                case 0: // opaque
                    material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetFloat("_Surface", (int)UpgradeSurfaceType.Opaque);
                    break;
                case 1: // cutout > alphatest
                    material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetFloat("_Surface", (int)UpgradeSurfaceType.Opaque);
                    material.SetFloat("_AlphaClip", 1);
                    break;
                case 2: // fade > alpha
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetFloat("_Surface", (int)UpgradeSurfaceType.Transparent);
                    material.SetFloat("_Blend", (int)UpgradeBlendMode.Alpha);
                    break;
                case 3: // transparent > premul
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetFloat("_Surface", (int)UpgradeSurfaceType.Transparent);
                    material.SetFloat("_Blend", (int)UpgradeBlendMode.Premultiply);
                    break;
                case 4: // add
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetFloat("_Surface", (int)UpgradeSurfaceType.Transparent);
                    material.SetFloat("_Blend", (int)UpgradeBlendMode.Additive);
                    break;
                case 5: // sub > none
                    break;
                case 6: // mod > multiply
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetFloat("_Surface", (int)UpgradeSurfaceType.Transparent);
                    material.SetFloat("_Blend", (int)UpgradeBlendMode.Multiply);
                    break;
            }
        }
    }

    /// <summary>
    /// Upgrader for the autodesk interactive shaders.
    /// </summary>
    public class AutodeskInteractiveUpgrader : MaterialUpgrader
    {
        enum LegacyRenderingMode
        {
            Opaque,
            Cutout,
            Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        /// <summary>
        /// Constructor for the autodesk interactive upgrader.
        /// </summary>
        /// <param name="oldShaderName">The name of the old shader.</param>
        public AutodeskInteractiveUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive", UniversalRenderPipelineMaterialUpgrader.DisableKeywords);
        }

        /// <inheritdoc/>
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

            var legacyRenderingMode = (LegacyRenderingMode)srcMaterial.GetFloat("_Mode");
            switch (legacyRenderingMode)
            {
                case LegacyRenderingMode.Opaque:
                    RenameShader(OldShaderPath, GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorShaders>().autodeskInteractiveShader.name, UniversalRenderPipelineMaterialUpgrader.DisableKeywords);
                    break;
                case LegacyRenderingMode.Cutout:
                    RenameShader(OldShaderPath, GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorShaders>().autodeskInteractiveMaskedShader.name, UniversalRenderPipelineMaterialUpgrader.DisableKeywords);
                    dstMaterial.SetFloat("_UseOpacityMap", .0f);
                    dstMaterial.SetFloat("_OpacityThreshold", srcMaterial.GetFloat("_Cutoff"));
                    break;
                case LegacyRenderingMode.Fade:
                case LegacyRenderingMode.Transparent:
                    RenameShader(OldShaderPath, GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorShaders>().autodeskInteractiveTransparentShader.name, UniversalRenderPipelineMaterialUpgrader.DisableKeywords);
                    dstMaterial.SetFloat("_UseOpacityMap", .0f);
                    dstMaterial.SetFloat("_Opacity", srcMaterial.GetColor("_Color").a);
                    break;
            }
        }
    }
}
