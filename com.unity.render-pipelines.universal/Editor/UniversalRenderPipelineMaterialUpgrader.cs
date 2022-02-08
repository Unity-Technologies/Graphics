using System;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal.Converters;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MaterialPostprocessor")]
namespace UnityEditor.Rendering.Universal
{
    internal sealed class UniversalRenderPipelineMaterialUpgrader : RenderPipelineConverter
    {
        public override string name => "Material Upgrade";
        public override string info => "This will upgrade your materials.";
        public override int priority => -1000;
        public override Type container => typeof(BuiltInToURPConverterContainer);

        List<string> m_AssetsToConvert = new List<string>();

        private List<GUID> m_MaterialGUIDs = new();

        static List<MaterialUpgrader> m_Upgraders;
        private static HashSet<string> m_ShaderNamesToIgnore;

        public IReadOnlyList<MaterialUpgrader> upgraders => m_Upgraders;
        static UniversalRenderPipelineMaterialUpgrader()
        {
            m_Upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref m_Upgraders);

            m_ShaderNamesToIgnore = new HashSet<string>();
            GetShaderNamesToIgnore(ref m_ShaderNamesToIgnore);
        }

        private static void UpgradeProjectMaterials()
        {
            m_Upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref m_Upgraders);

            m_ShaderNamesToIgnore = new HashSet<string>();
            GetShaderNamesToIgnore(ref m_ShaderNamesToIgnore);

            MaterialUpgrader.UpgradeProjectFolder(m_Upgraders, m_ShaderNamesToIgnore, "Upgrade to URP Materials", MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound);
            // TODO: return upgrade paths and pass to AnimationClipUpgrader
            AnimationClipUpgrader.DoUpgradeAllClipsMenuItem(m_Upgraders, "Upgrade Animation Clips to URP Materials");
        }

        [MenuItem("Edit/Rendering/Materials/Convert Selected Built-in Materials to URP", true)]
        static bool MaterialValidate(MenuCommand command)
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is not Material) return false;
            }

            return true;
        }

        [MenuItem("Edit/Rendering/Materials/Convert Selected Built-in Materials to URP", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.editMenuPriority + 1)]
        private static void UpgradeSelectedMaterialsMenuItem()
        {
            UpgradeSelectedMaterials(false);
        }

        // Added bool variable in case this method was used by anyone.
        // Doing this, since the menuitem should behave as it did before,
        // and then we didn't have the Animation clips upgrader
        private static void UpgradeSelectedMaterials(bool UpgradeAnimationClips = true)
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            HashSet<string> shaderNamesToIgnore = new HashSet<string>();
            GetShaderNamesToIgnore(ref shaderNamesToIgnore);

            MaterialUpgrader.UpgradeSelection(upgraders, shaderNamesToIgnore, "Upgrade to URP Materials", MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound);
            if (UpgradeAnimationClips)
            {
                // TODO: return upgrade paths and pass to AnimationClipUpgrader
                AnimationClipUpgrader.DoUpgradeAllClipsMenuItem(upgraders, "Upgrade Animation Clips to URP Materials");
            }
        }

        private static void GetShaderNamesToIgnore(ref HashSet<string> shadersToIgnore)
        {
            shadersToIgnore.Add("Universal Render Pipeline/Baked Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Particles/Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Particles/Simple Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Particles/Unlit");
            shadersToIgnore.Add("Universal Render Pipeline/Simple Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Nature/SpeedTree7");
            shadersToIgnore.Add("Universal Render Pipeline/Nature/SpeedTree7 Billboard");
            shadersToIgnore.Add("Universal Render Pipeline/Nature/SpeedTree8");
            shadersToIgnore.Add("Universal Render Pipeline/Nature/SpeedTree8_PBRLit");
            shadersToIgnore.Add("Universal Render Pipeline/2D/Sprite-Lit-Default");
            shadersToIgnore.Add("Universal Render Pipeline/Terrain/Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Unlit");
            shadersToIgnore.Add("Sprites/Default");
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
            upgraders.Add(new SpeedTreeUpgrader("Nature/SpeedTree"));
            upgraders.Add(new SpeedTreeBillboardUpgrader("Nature/SpeedTree Billboard"));
            upgraders.Add(new UniversalSpeedTree8Upgrader("Nature/SpeedTree8"));

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

        bool IsMaterialPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            return path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase);
        }

        bool ShouldUpgradeShader(Material material, HashSet<string> shaderNamesToIgnore)
        {
            if (material == null)
                return false;

            if (material.shader == null)
                return false;

            return !shaderNamesToIgnore.Contains(material.shader.name);
        }

        public override void OnInitialize(InitializeConverterContext context, Action callback)
        {
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (IsMaterialPath(path))
                {
                    Material m = AssetDatabase.LoadMainAssetAtPath(path) as Material;

                    if (!ShouldUpgradeShader(m, m_ShaderNamesToIgnore))
                        continue;

                    GUID guid = AssetDatabase.GUIDFromAssetPath(path);
                    m_MaterialGUIDs.Add(guid);

                    m_AssetsToConvert.Add(path);

                    ConverterItemDescriptor desc = new ConverterItemDescriptor()
                    {
                        name = m.name,
                        info = path,
                        warningMessage = String.Empty,
                        helpLink = String.Empty,
                    };
                    // Each converter needs to add this info using this API.
                    context.AddAssetToConvert(desc);
                }
            }
            callback.Invoke();
        }

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

        public override void OnClicked(int index)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Material>(m_AssetsToConvert[index]));
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
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        static public UpgradeParams specularOpaque = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        static public UpgradeParams diffuseAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            smoothnessSource = SmoothnessSource.SpecularAlpha,
        };

        static public UpgradeParams specularAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.SpecularAlpha,
        };

        static public UpgradeParams diffuseAlphaCutout = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = true,
            specularSource = SpecularSource.NoSpecular,
            smoothnessSource = SmoothnessSource.SpecularAlpha,
        };

        static public UpgradeParams specularAlphaCutout = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = true,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.SpecularAlpha,
        };

        static public UpgradeParams diffuseCubemap = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        static public UpgradeParams specularCubemap = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Opaque,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        static public UpgradeParams diffuseCubemapAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.NoSpecular,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };

        static public UpgradeParams specularCubemapAlpha = new UpgradeParams()
        {
            surfaceType = UpgradeSurfaceType.Transparent,
            blendMode = UpgradeBlendMode.Alpha,
            alphaClip = false,
            specularSource = SpecularSource.SpecularTextureAndColor,
            smoothnessSource = SmoothnessSource.BaseAlpha,
        };
    }

    public class StandardUpgrader : MaterialUpgrader
    {
        enum LegacyRenderingMode
        {
            Opaque,
            Cutout,
            Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

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
        }

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
            RenameTexture("_MainTex", "_BaseMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_GlossyReflections", "_EnvironmentReflections");
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
                SmoothnessSource glossSource = (SmoothnessSource)material.GetFloat("_SmoothnessSource");
                bool hasGlossMap = material.GetTexture("_SpecGlossMap");
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasGlossMap);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", !hasGlossMap);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", glossSource == SmoothnessSource.BaseAlpha);
            }
        }
    }

    public class TerrainUpgrader : MaterialUpgrader
    {
        public TerrainUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.TerrainLit));
        }
    }

    internal class SpeedTreeUpgrader : MaterialUpgrader
    {
        internal SpeedTreeUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.SpeedTree7));
        }
    }
    internal class SpeedTreeBillboardUpgrader : MaterialUpgrader
    {
        internal SpeedTreeBillboardUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.SpeedTree7Billboard));
        }
    }

    public class ParticleUpgrader : MaterialUpgrader
    {
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

        public static void UpdateStandardSurface(Material material)
        {
            UpdateSurfaceBlendModes(material);
        }

        public static void UpdateUnlit(Material material)
        {
            UpdateSurfaceBlendModes(material);
        }

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

    public class AutodeskInteractiveUpgrader : MaterialUpgrader
    {
        public AutodeskInteractiveUpgrader(string oldShaderName)
        {
            RenameShader(oldShaderName, "Universal Render Pipeline/Autodesk Interactive/Autodesk Interactive");
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
