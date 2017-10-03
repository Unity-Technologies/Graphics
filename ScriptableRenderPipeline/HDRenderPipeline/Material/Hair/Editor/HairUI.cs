using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class HairGUI : BaseHairGUI
    {
        protected static class Styles
        {
            public static string InputsText = "Inputs";

            public static GUIContent diffuseColorText = new GUIContent("Diffuse Color + Opacity", "Albedo (RGB) and Opacity (A)");
            public static GUIContent diffuseColorSmoothnessText = new GUIContent("Diffuse Color + Smoothness", "Albedo (RGB) and Smoothness (A)");

            public static GUIContent smoothnessMapChannelText = new GUIContent("Smoothness Source", "Smoothness texture and channel");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent maskMapESText = new GUIContent("Mask Map - M(R), AO(G), E(B), S(A)", "Mask map");
            public static GUIContent maskMapSText = new GUIContent("Mask Map - M(R), AO(G), S(A)", "Mask map");

            public static GUIContent normalMapSpaceText = new GUIContent("Normal/Tangent Map space", "");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (DXT5) - Need to implement BC5");
            public static GUIContent specularOcclusionMapText = new GUIContent("Specular Occlusion Map (RGBA)", "Specular Occlusion Map");
            public static GUIContent horizonFadeText = new GUIContent("Horizon Fade (Spec occlusion)", "horizon fade is use to control specular occlusion");

            public static GUIContent tangentMapText = new GUIContent("Tangent Map", "Tangent Map (BC5) - DXT5 for test");
            public static GUIContent anisotropyText = new GUIContent("Anisotropy", "Anisotropy scale factor");
            public static GUIContent anisotropyMapText = new GUIContent("Anisotropy Map (B)", "Anisotropy");

            public static string textureControlText = "Input textures control";
            public static GUIContent UVBaseMappingText = new GUIContent("Base UV mapping", "");
            public static GUIContent texWorldScaleText = new GUIContent("World scale", "Tiling factor applied to Planar/Trilinear mapping");

            // Details
            public static string detailText = "Inputs Detail";
            public static GUIContent UVDetailMappingText = new GUIContent("Detail UV mapping", "");
            public static GUIContent detailMapNormalText = new GUIContent("Detail Map A(R) Ny(G) S(B) Nx(A)", "Detail Map");
            public static GUIContent detailMaskText = new GUIContent("Detail Mask (G)", "Mask for detailMap");
            public static GUIContent detailAlbedoScaleText = new GUIContent("Detail AlbedoScale", "Detail Albedo Scale factor");
            public static GUIContent detailNormalScaleText = new GUIContent("Detail NormalScale", "Normal Scale factor");
            public static GUIContent detailSmoothnessScaleText = new GUIContent("Detail SmoothnessScale", "Smoothness Scale factor");

            // Emissive
            public static string lightingText = "Inputs Lighting";
            public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive");
            public static GUIContent emissiveIntensityText = new GUIContent("Emissive Intensity", "Emissive");
            public static GUIContent emissiveColorModeText = new GUIContent("Emissive Color Usage", "Use emissive color or emissive mask");

            public static GUIContent normalMapSpaceWarning = new GUIContent("Object space normal can't be use with triplanar mapping.");
        }

        public enum UVBaseMapping
        {
            UV0,
            Planar,
            Triplanar
        }

        public enum NormalMapSpace
        {
            TangentSpace,
            ObjectSpace,
        }

        public enum UVDetailMapping
        {
            UV0,
            UV1,
            UV2,
            UV3
        }

        public enum EmissiveColorMode
        {
            UseEmissiveColor,
            UseEmissiveMask,
        }

        protected MaterialProperty UVBase = null;
        protected const string kUVBase = "_UVBase";
        protected MaterialProperty TexWorldScale = null;
        protected const string kTexWorldScale = "_TexWorldScale";
        protected MaterialProperty UVMappingMask = null;
        protected const string kUVMappingMask = "_UVMappingMask";

        protected MaterialProperty diffuseColor = null;
        protected const string kDiffuseColor = "_DiffuseColor";
        protected MaterialProperty diffuseColorMap = null;
        protected const string kDiffuseColorMap = "_DiffuseColorMap";
        protected MaterialProperty smoothness = null;
        protected const string kSmoothness = "_Smoothness";
        protected MaterialProperty maskMap = null;
        protected const string kMaskMap = "_MaskMap";
        protected MaterialProperty specularOcclusionMap = null;
        protected const string kSpecularOcclusionMap = "_SpecularOcclusionMap";
        protected MaterialProperty horizonFade = null;
        protected const string kHorizonFade = "_HorizonFade";
        protected MaterialProperty normalMap = null;
        protected const string kNormalMap = "_NormalMap";
        protected MaterialProperty normalScale = null;
        protected const string kNormalScale = "_NormalScale";
        protected MaterialProperty normalMapSpace = null;
        protected const string kNormalMapSpace = "_NormalMapSpace";
        protected MaterialProperty tangentMap = null;
        protected const string kTangentMap = "_TangentMap";
        protected MaterialProperty anisotropy = null;
        protected const string kAnisotropy = "_Anisotropy";
        protected MaterialProperty anisotropyMap = null;
        protected const string kAnisotropyMap = "_AnisotropyMap";

        protected MaterialProperty UVDetail = null;
        protected const string kUVDetail = "_UVDetail";
        protected MaterialProperty UVDetailsMappingMask = null;
        protected const string kUVDetailsMappingMask = "_UVDetailsMappingMask";
        protected MaterialProperty detailMap = null;
        protected const string kDetailMap = "_DetailMap";
        protected MaterialProperty detailMask = null;
        protected const string kDetailMask = "_DetailMask";
        protected MaterialProperty detailAlbedoScale = null;
        protected const string kDetailAlbedoScale = "_DetailAlbedoScale";
        protected MaterialProperty detailNormalScale = null;
        protected const string kDetailNormalScale = "_DetailNormalScale";
        protected MaterialProperty detailSmoothnessScale = null;
        protected const string kDetailSmoothnessScale = "_DetailSmoothnessScale";

        protected MaterialProperty emissiveColorMode = null;
        protected const string kEmissiveColorMode = "_EmissiveColorMode";
        protected MaterialProperty emissiveColor = null;
        protected const string kEmissiveColor = "_EmissiveColor";
        protected MaterialProperty emissiveColorMap = null;
        protected const string kEmissiveColorMap = "_EmissiveColorMap";
        protected MaterialProperty emissiveIntensity = null;
        protected const string kEmissiveIntensity = "_EmissiveIntensity";

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            UVBase = FindProperty(kUVBase, props);
            TexWorldScale = FindProperty(kTexWorldScale, props);
            UVMappingMask = FindProperty(kUVMappingMask, props);

            diffuseColor = FindProperty(kDiffuseColor, props);
            diffuseColorMap = FindProperty(kDiffuseColorMap, props);
            smoothness = FindProperty(kSmoothness, props);
            maskMap = FindProperty(kMaskMap, props);
            specularOcclusionMap = FindProperty(kSpecularOcclusionMap, props);
            horizonFade = FindProperty(kHorizonFade, props);
            normalMap = FindProperty(kNormalMap, props);
            normalScale = FindProperty(kNormalScale, props);
            normalMapSpace = FindProperty(kNormalMapSpace, props);
            tangentMap = FindProperty(kTangentMap, props);
            anisotropy = FindProperty(kAnisotropy, props);
            anisotropyMap = FindProperty(kAnisotropyMap, props);

            // Details
            UVDetail = FindProperty(kUVDetail, props);
            UVDetailsMappingMask = FindProperty(kUVDetailsMappingMask, props);
            detailMap = FindProperty(kDetailMap, props);
            detailMask = FindProperty(kDetailMask, props);
            detailAlbedoScale = FindProperty(kDetailAlbedoScale, props);
            detailNormalScale = FindProperty(kDetailNormalScale, props);
            detailSmoothnessScale = FindProperty(kDetailSmoothnessScale, props);

            // Emissive
            emissiveColorMode = FindProperty(kEmissiveColorMode, props);
            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
            emissiveIntensity = FindProperty(kEmissiveIntensity, props);
        }

        protected void ShaderStandardInputGUI()
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.tangentMapText, tangentMap);
            m_MaterialEditor.ShaderProperty(anisotropy, Styles.anisotropyText);
            m_MaterialEditor.TexturePropertySingleLine(Styles.anisotropyMapText, anisotropyMap);
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            bool useEmissiveMask = (EmissiveColorMode)emissiveColorMode.floatValue == EmissiveColorMode.UseEmissiveMask;

            GUILayout.Label(Styles.InputsText, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            m_MaterialEditor.TexturePropertySingleLine(Styles.diffuseColorText, diffuseColorMap, diffuseColor);
            m_MaterialEditor.ShaderProperty(smoothness, Styles.smoothnessText);

            if (useEmissiveMask)
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapESText, maskMap);
            else
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapSText, maskMap);

            m_MaterialEditor.TexturePropertySingleLine(Styles.specularOcclusionMapText, specularOcclusionMap);
            m_MaterialEditor.ShaderProperty(horizonFade, Styles.horizonFadeText);

            m_MaterialEditor.ShaderProperty(normalMapSpace, Styles.normalMapSpaceText);

            // Triplanar only work with tangent space normal
            if ((NormalMapSpace)normalMapSpace.floatValue == NormalMapSpace.ObjectSpace && ((UVBaseMapping)UVBase.floatValue == UVBaseMapping.Triplanar))
            {
                EditorGUILayout.HelpBox(Styles.normalMapSpaceWarning.text, MessageType.Error);
            }

            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap, normalScale);

            ShaderStandardInputGUI();

            EditorGUILayout.Space();
            GUILayout.Label("    " + Styles.textureControlText, EditorStyles.label);
            m_MaterialEditor.ShaderProperty(UVBase, Styles.UVBaseMappingText);
            // UVSet0 is always set, planar and triplanar will override it.
            UVMappingMask.colorValue = new Color(1.0f, 0.0f, 0.0f, 0.0f); // This is override in the shader anyway but just in case.
            if (((UVBaseMapping)UVBase.floatValue == UVBaseMapping.Planar) || ((UVBaseMapping)UVBase.floatValue == UVBaseMapping.Triplanar))
            {
                m_MaterialEditor.ShaderProperty(TexWorldScale, Styles.texWorldScaleText);
            }
            m_MaterialEditor.TextureScaleOffsetProperty(diffuseColorMap);

            EditorGUILayout.Space();
            GUILayout.Label(Styles.detailText, EditorStyles.boldLabel);

            m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
            m_MaterialEditor.TexturePropertySingleLine(Styles.detailMapNormalText, detailMap);

            EditorGUI.indentLevel++;
            // When Planar or Triplanar is enable the UVDetail use the same mode, so we disable the choice on UVDetail
            if ((UVBaseMapping)UVBase.floatValue == UVBaseMapping.UV0)
            {
                m_MaterialEditor.ShaderProperty(UVDetail, Styles.UVDetailMappingText);
            }
            else if ((UVBaseMapping)UVBase.floatValue == UVBaseMapping.Planar)
            {
                GUILayout.Label("       " + Styles.UVDetailMappingText.text + ": Planar");
            }
            else if ((UVBaseMapping)UVBase.floatValue == UVBaseMapping.Triplanar)
            {
                GUILayout.Label("       " + Styles.UVDetailMappingText.text + ": Triplanar");
            }

            // Setup the UVSet for detail, if planar/triplanar is use for base, it will override the mapping of detail (See shader code)
            float X, Y, Z, W;
            X = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV0) ? 1.0f : 0.0f;
            Y = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV1) ? 1.0f : 0.0f;
            Z = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV2) ? 1.0f : 0.0f;
            W = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV3) ? 1.0f : 0.0f;
            UVDetailsMappingMask.colorValue = new Color(X, Y, Z, W);

            m_MaterialEditor.TextureScaleOffsetProperty(detailMap);
            m_MaterialEditor.ShaderProperty(detailAlbedoScale, Styles.detailAlbedoScaleText);
            m_MaterialEditor.ShaderProperty(detailNormalScale, Styles.detailNormalScaleText);
            m_MaterialEditor.ShaderProperty(detailSmoothnessScale, Styles.detailSmoothnessScaleText);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            GUILayout.Label(Styles.lightingText, EditorStyles.boldLabel);
            m_MaterialEditor.ShaderProperty(emissiveColorMode, Styles.emissiveColorModeText);

            if (!useEmissiveMask)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
            }

            m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);

            // The parent Base.ShaderPropertiesGUI will call DoEmissionArea
        }

        protected override bool ShouldEmissionBeEnabled(Material mat)
        {
            return mat.GetFloat(kEmissiveIntensity) > 0.0f;
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseHairKeywords(material);

            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            SetKeyword(material, "_MAPPING_PLANAR", ((UVBaseMapping)material.GetFloat(kUVBase)) == UVBaseMapping.Planar);
            SetKeyword(material, "_MAPPING_TRIPLANAR", ((UVBaseMapping)material.GetFloat(kUVBase)) == UVBaseMapping.Triplanar);
            SetKeyword(material, "_NORMALMAP_TANGENT_SPACE", ((NormalMapSpace)material.GetFloat(kNormalMapSpace)) == NormalMapSpace.TangentSpace);
            SetKeyword(material, "_EMISSIVE_COLOR", ((EmissiveColorMode)material.GetFloat(kEmissiveColorMode)) == EmissiveColorMode.UseEmissiveColor);

            SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap) || material.GetTexture(kDetailMap)); // With details map, we always use a normal map and Unity provide a default (0, 0, 1) normal map for ir
            SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
            SetKeyword(material, "_SPECULAROCCLUSIONMAP", material.GetTexture(kSpecularOcclusionMap));
            SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
            SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMap));
            SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
            SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap));

            bool needUV2 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV2 && (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV0;
            bool needUV3 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV3 && (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV0;

            if (needUV3)
            {
                material.DisableKeyword("_REQUIRE_UV2");
                material.EnableKeyword("_REQUIRE_UV3");
            }
            else if (needUV2)
            {
                material.EnableKeyword("_REQUIRE_UV2");
                material.DisableKeyword("_REQUIRE_UV3");
            }
            else
            {
                material.DisableKeyword("_REQUIRE_UV2");
                material.DisableKeyword("_REQUIRE_UV3");
            }
        }
    }
} // namespace UnityEditor
