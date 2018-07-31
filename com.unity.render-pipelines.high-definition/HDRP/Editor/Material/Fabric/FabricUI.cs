using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class FabricGUI : BaseLitGUI
    {
        protected static class Styles
        {
            public static string InputsText = "Inputs";

            public static GUIContent fabricTypeText = new GUIContent("Fabric Type", "");

            public static GUIContent baseColorText = new GUIContent("Base Color + Opacity", "Albedo (RGB) and Opacity (A)");

            public static GUIContent fuzzTintText = new GUIContent("Fuzz Tint", "");

            public static GUIContent smoothnessMapChannelText = new GUIContent("Smoothness Source", "Smoothness texture and channel");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent smoothnessRemappingText = new GUIContent("Smoothness Remapping", "Smoothness remapping");
            public static GUIContent aoRemappingText = new GUIContent("AmbientOcclusion Remapping", "AmbientOcclusion remapping");
            public static GUIContent maskMapSText = new GUIContent("Mask Map - X, AO(G), D(B), S(A)", "Mask map");
            public static GUIContent maskMapSpecularText = new GUIContent("Mask Map - AO(G), D(B), S(A)", "Mask map");

            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC7/BC5/DXT5(nm))");
            public static GUIContent bentNormalMapText = new GUIContent("Bent normal map", "Use only with indirect diffuse lighting (Lightmap/lightprobe) - Cosine weighted Bent Normal Map (average unoccluded direction) (BC7/BC5/DXT5(nm))");

            public static GUIContent tangentMapText = new GUIContent("Tangent Map", "Tangent Map (BC7/BC5/DXT5(nm))");
            public static GUIContent anisotropyText = new GUIContent("Anisotropy", "Anisotropy scale factor");
            public static GUIContent anisotropyMapText = new GUIContent("Anisotropy Map (R)", "Anisotropy");

            public static GUIContent UVBaseMappingText = new GUIContent("Base UV mapping", "");

            // Details
            public static string detailText = "Detail Inputs";
            public static GUIContent UVDetailMappingText = new GUIContent("Detail UV mapping", "");
            public static GUIContent detailMapNormalText = new GUIContent("Detail Map AO(R) Ny(G) S(B) Nx(A)", "Detail Map");
            public static GUIContent detailMaskText = new GUIContent("Fuzz Detail (RG)", "Fuzz Detail");
            public static GUIContent detailFuzz1Text = new GUIContent("Fuzz Detail 1", "Fuzz Detail factor");
            public static GUIContent detailAOScaleText = new GUIContent("Detail AO", "Detail AO Scale factor");
            public static GUIContent detailNormalScaleText = new GUIContent("Detail NormalScale", "Normal Scale factor");
            public static GUIContent detailSmoothnessScaleText = new GUIContent("Detail SmoothnessScale", "Smoothness Scale factor");
            public static GUIContent linkDetailsWithBaseText = new GUIContent("Lock to Base Tiling/Offset", "Lock details Tiling/Offset to Base Tiling/Offset");

            // Subsurface
            public static GUIContent diffusionProfileText = new GUIContent("Diffusion profile", "A profile determines the shape of the SSS/transmission filter.");
            public static GUIContent subsurfaceMaskText = new GUIContent("Subsurface mask", "Determines the strength of the subsurface scattering effect.");
            public static GUIContent subsurfaceMaskMapText = new GUIContent("Subsurface mask map (R)", "Determines the strength of the subsurface scattering effect.");
            public static GUIContent thicknessText = new GUIContent("Thickness", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.");
            public static GUIContent thicknessMapText = new GUIContent("Thickness map (R)", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.");
            public static GUIContent thicknessRemapText = new GUIContent("Thickness Remap", "Remaps values of the thickness map from [0, 1] to the specified range.");

            // Specular occlusion
            public static GUIContent enableSpecularOcclusionText = new GUIContent("Enable Specular Occlusion from Bent normal", "Require cosine weighted bent normal and cosine weighted ambient occlusion. Specular occlusion for reflection probe");
            public static GUIContent specularOcclusionWarning = new GUIContent("Require a cosine weighted bent normal and ambient occlusion maps");
        }

        public enum FabricType
        {
            Silk,
            CottonWool,
        }

        public enum UVBaseMapping
        {
            UV0,
            UV1,
            UV2,
            UV3
        }

        public enum UVDetailMapping
        {
            UV0,
            UV1,
            UV2,
            UV3
        }

        protected MaterialProperty UVBase = null;
        protected const string kUVBase = "_UVBase";
        protected MaterialProperty UVMappingMask = null;
        protected const string kUVMappingMask = "_UVMappingMask";

        protected MaterialProperty baseColor = null;
        protected const string kBaseColor = "_BaseColor";
        protected MaterialProperty baseColorMap = null;
        protected const string kBaseColorMap = "_BaseColorMap";
        protected MaterialProperty smoothness = null;
        protected const string kSmoothness = "_Smoothness";
        protected MaterialProperty smoothnessRemapMin = null;
        protected const string kSmoothnessRemapMin = "_SmoothnessRemapMin";
        protected MaterialProperty smoothnessRemapMax = null;
        protected const string kSmoothnessRemapMax = "_SmoothnessRemapMax";
        protected MaterialProperty aoRemapMin = null;
        protected const string kAORemapMin = "_AORemapMin";
        protected MaterialProperty aoRemapMax = null;
        protected const string kAORemapMax = "_AORemapMax";
        protected MaterialProperty maskMap = null;
        protected const string kMaskMap = "_MaskMap";
        protected MaterialProperty normalScale = null;
        protected const string kNormalScale = "_NormalScale";
        protected MaterialProperty normalMap = null;
        protected const string kNormalMap = "_NormalMap";
        protected MaterialProperty bentNormalMap = null;
        protected const string kBentNormalMap = "_BentNormalMap";

        protected MaterialProperty fuzzTint = null;
        protected const string kFuzzTint = "_FuzzTint";
        protected MaterialProperty fabricType = null;
        protected const string kFabricType = "_FabricType";

        protected MaterialProperty diffusionProfileID = null;
        protected const string kDiffusionProfileID = "_DiffusionProfile";
        protected MaterialProperty subsurfaceMask = null;
        protected const string kSubsurfaceMask = "_SubsurfaceMask";
        protected MaterialProperty subsurfaceMaskMap = null;
        protected const string kSubsurfaceMaskMap = "_SubsurfaceMaskMap";
        protected MaterialProperty thickness = null;
        protected const string kThickness = "_Thickness";
        protected MaterialProperty thicknessMap = null;
        protected const string kThicknessMap = "_ThicknessMap";
        protected MaterialProperty thicknessRemap = null;
        protected const string kThicknessRemap = "_ThicknessRemap";

        protected MaterialProperty UVDetail = null;
        protected const string kUVDetail = "_UVDetail";
        protected MaterialProperty UVDetailsMappingMask = null;
        protected const string kUVDetailsMappingMask = "_UVDetailsMappingMask";
        
        protected MaterialProperty detailMap = null;
        protected const string kDetailMap = "_DetailMap";
        protected MaterialProperty detailMask = null;
        protected const string kDetailMask = "_DetailMask";
        protected MaterialProperty linkDetailsWithBase = null;
        protected const string kLinkDetailsWithBase = "_LinkDetailsWithBase";     

        protected MaterialProperty detailFuzz1 = null;
        protected const string kDetailFuzz1 = "_DetailFuzz1";
        protected MaterialProperty detailAOScale = null;
        protected const string kDetailAOScale = "_DetailAOScale";
        protected MaterialProperty detailNormalScale = null;
        protected const string kDetailNormalScale = "_DetailNormalScale";
        protected MaterialProperty detailSmoothnessScale = null;
        protected const string kDetailSmoothnessScale = "_DetailSmoothnessScale";

        protected MaterialProperty tangentMap = null;
        protected const string kTangentMap = "_TangentMap";
        protected MaterialProperty anisotropy = null;
        protected const string kAnisotropy = "_Anisotropy";
        protected MaterialProperty anisotropyMap = null;
        protected const string kAnisotropyMap = "_AnisotropyMap";

        protected MaterialProperty enableSpecularOcclusion = null;
        protected const string kEnableSpecularOcclusion = "_EnableSpecularOcclusion";

        protected MaterialProperty enableSubsurfaceScattering = null;
        protected const string kEnableSubsurfaceScattering = "_EnableSubsurfaceScattering";
        protected MaterialProperty enableTransmission = null;
        protected const string kEnableTransmission = "_EnableTransmission";

        override protected void FindMaterialProperties(MaterialProperty[] props)
        {
            UVBase = FindProperty(kUVBase, props);
            UVMappingMask = FindProperty(kUVMappingMask, props);

            baseColor = FindProperty(kBaseColor, props);
            baseColorMap = FindProperty(kBaseColorMap, props);
            smoothness = FindProperty(kSmoothness, props);
            smoothnessRemapMin = FindProperty(kSmoothnessRemapMin, props);
            smoothnessRemapMax = FindProperty(kSmoothnessRemapMax, props);
            aoRemapMin = FindProperty(kAORemapMin, props);
            aoRemapMax = FindProperty(kAORemapMax, props);
            maskMap = FindProperty(kMaskMap, props);
            normalMap = FindProperty(kNormalMap, props);
            normalScale = FindProperty(kNormalScale, props);
            bentNormalMap = FindProperty(kBentNormalMap, props);

            fuzzTint = FindProperty(kFuzzTint, props);
            fabricType = FindProperty(kFabricType, props);           

            // Sub surface
            diffusionProfileID = FindProperty(kDiffusionProfileID, props);
            subsurfaceMask = FindProperty(kSubsurfaceMask, props);
            subsurfaceMaskMap = FindProperty(kSubsurfaceMaskMap, props);
            thickness = FindProperty(kThickness, props);
            thicknessMap = FindProperty(kThicknessMap, props);
            thicknessRemap = FindProperty(kThicknessRemap, props);

            // Details
            UVDetail = FindProperty(kUVDetail, props);
            UVDetailsMappingMask = FindProperty(kUVDetailsMappingMask, props);
            linkDetailsWithBase = FindProperty(kLinkDetailsWithBase, props);
            
            detailMap = FindProperty(kDetailMap, props);
            detailMask = FindProperty(kDetailMask, props);
            detailFuzz1 = FindProperty(kDetailFuzz1, props);
            detailAOScale = FindProperty(kDetailAOScale, props);
            detailNormalScale = FindProperty(kDetailNormalScale, props);
            detailSmoothnessScale = FindProperty(kDetailSmoothnessScale, props);

            // Anisotropy
            tangentMap = FindProperty(kTangentMap, props);
            anisotropy = FindProperty(kAnisotropy, props);
            anisotropyMap = FindProperty(kAnisotropyMap, props);

            // toggle
            enableSubsurfaceScattering = FindProperty(kEnableSubsurfaceScattering, props);
            enableTransmission = FindProperty(kEnableTransmission, props);
        }

        protected void ShaderSSSAndTransmissionInputGUI(Material material)
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (hdPipeline == null)
                return;

            var diffusionProfileSettings = hdPipeline.diffusionProfileSettings;

            if (hdPipeline.IsInternalDiffusionProfile(diffusionProfileSettings))
            {
                EditorGUILayout.HelpBox("No diffusion profile Settings have been assigned to the render pipeline asset.", MessageType.Warning);
                return;
            }

            // TODO: Optimize me
            var profiles = diffusionProfileSettings.profiles;
            var names = new GUIContent[profiles.Length + 1];
            names[0] = new GUIContent("None");

            var values = new int[names.Length];
            values[0] = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

            for (int i = 0; i < profiles.Length; i++)
            {
                names[i + 1] = new GUIContent(profiles[i].name);
                values[i + 1] = i + 1;
            }

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                int profileID = (int)diffusionProfileID.floatValue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(Styles.diffusionProfileText);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        profileID = EditorGUILayout.IntPopup(profileID, names, values);

                        if (GUILayout.Button("Goto", EditorStyles.miniButton, GUILayout.Width(50f)))
                            Selection.activeObject = diffusionProfileSettings;
                    }
                }

                if (scope.changed)
                    diffusionProfileID.floatValue = profileID;
            }

            /*
            if ((int)materialID.floatValue == (int)MaterialId.LitSSS)
            {
                m_MaterialEditor.ShaderProperty(subsurfaceMask[layerIndex], Styles.subsurfaceMaskText);
                m_MaterialEditor.TexturePropertySingleLine(Styles.subsurfaceMaskMapText, subsurfaceMaskMap[layerIndex]);
            }

            if ((int)materialID.floatValue == (int)BaseLitGUI.MaterialId.LitTranslucent ||
                ((int)materialID.floatValue == (int)BaseLitGUI.MaterialId.LitSSS && transmissionEnable.floatValue > 0.0f))
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.thicknessMapText, thicknessMap[layerIndex]);
                if (thicknessMap[layerIndex].textureValue != null)
                {
                    // Display the remap of texture values.
                    Vector2 remap = thicknessRemap[layerIndex].vectorValue;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(Styles.thicknessRemapText, ref remap.x, ref remap.y, 0.0f, 1.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        thicknessRemap[layerIndex].vectorValue = remap;
                    }
                }
                else
                {
                    // Allow the user to set the constant value of thickness if no thickness map is provided.
                    m_MaterialEditor.ShaderProperty(thickness[layerIndex], Styles.thicknessText);
                }
            }
         */   
        }

        protected void ShaderAnisoInputGUI()
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.tangentMapText, tangentMap);
            m_MaterialEditor.ShaderProperty(anisotropy, Styles.anisotropyText);
            m_MaterialEditor.TexturePropertySingleLine(Styles.anisotropyMapText, anisotropyMap);
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            GUILayout.Label("Fabric Options", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            m_MaterialEditor.ShaderProperty(fabricType, Styles.fabricTypeText);
            EditorGUI.indentLevel--;
            m_MaterialEditor.ShaderProperty(fuzzTint, Styles.fuzzTintText);

            EditorGUILayout.LabelField(Styles.InputsText, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            m_MaterialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap, baseColor);

            if (maskMap.textureValue == null)
            {
                m_MaterialEditor.ShaderProperty(smoothness, Styles.smoothnessText);
            }
            else
            {
                float remapMin = smoothnessRemapMin.floatValue;
                float remapMax = smoothnessRemapMax.floatValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(Styles.smoothnessRemappingText, ref remapMin, ref remapMax, 0.0f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    smoothnessRemapMin.floatValue = remapMin;
                    smoothnessRemapMax.floatValue = remapMax;
                }

                float aoMin = aoRemapMin.floatValue;
                float aoMax = aoRemapMax.floatValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(Styles.aoRemappingText, ref aoMin, ref aoMax, 0.0f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    aoRemapMin.floatValue = aoMin;
                    aoRemapMax.floatValue = aoMax;
                }
            }

            m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapSpecularText, maskMap);

            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap, normalScale);
            m_MaterialEditor.TexturePropertySingleLine(Styles.bentNormalMapText, bentNormalMap);

            ShaderSSSAndTransmissionInputGUI(material);
            ShaderAnisoInputGUI();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            m_MaterialEditor.ShaderProperty(UVBase, Styles.UVBaseMappingText);

            UVBaseMapping uvBaseMapping = (UVBaseMapping)UVBase.floatValue;

            float X, Y, Z, W;
            X = (uvBaseMapping == UVBaseMapping.UV0) ? 1.0f : 0.0f;
            Y = (uvBaseMapping == UVBaseMapping.UV1) ? 1.0f : 0.0f;
            Z = (uvBaseMapping == UVBaseMapping.UV2) ? 1.0f : 0.0f;
            W = (uvBaseMapping == UVBaseMapping.UV3) ? 1.0f : 0.0f;

            UVMappingMask.colorValue = new Color(X, Y, Z, W);

            m_MaterialEditor.TextureScaleOffsetProperty(baseColorMap);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.detailText, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
            m_MaterialEditor.TexturePropertySingleLine(Styles.detailMapNormalText, detailMap);

            if (material.GetTexture(kDetailMap))
            {
                EditorGUI.indentLevel++;

                m_MaterialEditor.ShaderProperty(UVDetail, Styles.UVDetailMappingText);

                // Setup the UVSet for detail, if planar/triplanar is use for base, it will override the mapping of detail (See shader code)
                X = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV0) ? 1.0f : 0.0f;
                Y = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV1) ? 1.0f : 0.0f;
                Z = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV2) ? 1.0f : 0.0f;
                W = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV3) ? 1.0f : 0.0f;
                UVDetailsMappingMask.colorValue = new Color(X, Y, Z, W);

                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(linkDetailsWithBase, Styles.linkDetailsWithBaseText);
                EditorGUI.indentLevel--;

                m_MaterialEditor.TextureScaleOffsetProperty(detailMap);
                m_MaterialEditor.ShaderProperty(detailFuzz1, Styles.detailFuzz1Text);
                m_MaterialEditor.ShaderProperty(detailAOScale, Styles.detailAOScaleText);
                m_MaterialEditor.ShaderProperty(detailNormalScale, Styles.detailNormalScaleText);
                m_MaterialEditor.ShaderProperty(detailSmoothnessScale, Styles.detailSmoothnessScaleText);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            if (enableSpecularOcclusion != null)
            {
                m_MaterialEditor.ShaderProperty(enableSpecularOcclusion, Styles.enableSpecularOcclusionText);
            }            
        }

        protected override void VertexAnimationPropertiesGUI()
        {

        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseLitKeywords(material);
            SetupBaseLitMaterialPass(material);

            // With details map, we always use a normal map and Unity provide a default (0, 0, 1) normal map for it
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap) || material.GetTexture(kDetailMap));
            CoreUtils.SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMap));
            CoreUtils.SetKeyword(material, "_BENTNORMALMAP", material.GetTexture(kBentNormalMap));

            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));

            CoreUtils.SetKeyword(material, "_ENABLESPECULAROCCLUSION", material.GetFloat(kEnableSpecularOcclusion) > 0.0f);
            CoreUtils.SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
            CoreUtils.SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap));
            CoreUtils.SetKeyword(material, "_SUBSURFACE_MASK_MAP", material.GetTexture(kSubsurfaceMaskMap));
            CoreUtils.SetKeyword(material, "_THICKNESSMAP", material.GetTexture(kThicknessMap));

            bool needUV2 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV2 || (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV2;
            bool needUV3 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV3 || (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV3;

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

            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", material.GetFloat(kEnableSubsurfaceScattering) > 0.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", material.GetFloat(kEnableTransmission) > 0.0f);
            FabricType fabricType = (FabricType)material.GetFloat(kFabricType);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_ANISOTROPY", fabricType == FabricType.Silk);
        }
    }
} // namespace UnityEditor
