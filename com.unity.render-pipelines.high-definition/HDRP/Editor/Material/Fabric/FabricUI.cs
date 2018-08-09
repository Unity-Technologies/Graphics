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
            // Fields
            public static GUIContent fabricTypeText = new GUIContent("Fabric Type", "");
            public static string InputsText = "Inputs";
            public static string emissiveLabelText = "Emissive Inputs";

            // Primary UV mapping
            public static GUIContent UVBaseMappingText = new GUIContent("Base UV mapping", "");

            // Base Color
            public static GUIContent baseColorText = new GUIContent("Base Color + Opacity", "Albedo (RGB) and Opacity (A)");

            // Fuzz Tint
            public static GUIContent fuzzTintText = new GUIContent("Fuzz Tint", "");

            // Smoothness
            public static GUIContent smoothnessMapChannelText = new GUIContent("Smoothness Source", "Smoothness texture and channel");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent smoothnessRemappingText = new GUIContent("Smoothness Remapping", "Smoothness remapping");
            
            // AO
            public static GUIContent aoRemappingText = new GUIContent("AmbientOcclusion Remapping", "AmbientOcclusion remapping");
            
            // Mask
            public static GUIContent maskMapSText = new GUIContent("Mask Map - X, AO(G), DM(B), S(A)", "Mask map");
            public static GUIContent maskMapSpecularText = new GUIContent("Mask Map - AO(G), DM(B), S(A)", "Mask map");

            // Normal map
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC7/BC5/DXT5(nm))");
            // public static GUIContent bentNormalMapText = new GUIContent("Bent normal map", "Use only with indirect diffuse lighting (Lightmap/light-probe) - Cosine weighted Bent Normal Map (average un-occluded direction) (BC7/BC5/DXT5(nm))");

            // Tangent map
            public static GUIContent tangentMapText = new GUIContent("Tangent Map", "Tangent Map (BC7/BC5/DXT5(nm))");
           
            // Anisotropy
            public static GUIContent anisotropyText = new GUIContent("Anisotropy", "Anisotropy scale factor");
            public static GUIContent anisotropyMapText = new GUIContent("Anisotropy Map (R)", "Anisotropy");

            // Detail map
            public static string detailText = "Detail Inputs";
            public static GUIContent UVDetailMappingText = new GUIContent("Detail UV mapping", "");
            public static GUIContent detailMapNormalText = new GUIContent("Detail Map AO(R) Ny(G) S(B) Nx(A)", "Detail Map");
            public static GUIContent FuzzDetailText = new GUIContent("Fuzz Detail", "Fuzz Detail factor");
            public static GUIContent detailAOScaleText = new GUIContent("Detail AO", "Detail AO Scale factor");
            public static GUIContent detailNormalScaleText = new GUIContent("Detail NormalScale", "Normal Scale factor");
            public static GUIContent detailSmoothnessScaleText = new GUIContent("Detail SmoothnessScale", "Smoothness Scale factor");
            public static GUIContent linkDetailsWithBaseText = new GUIContent("Lock to Base Tiling/Offset", "Lock details Tiling/Offset to Base Tiling/Offset");

            // Diffusion
            public static GUIContent diffusionProfileText = new GUIContent("Diffusion profile", "A profile determines the shape of the SSS/transmission filter.");

            // Transmission
            public static GUIContent transmissionToggleText = new GUIContent("Transmission Enabled", "Enable/Disable the transmission");

            // Subsurface scattering
            public static GUIContent subsurfaceToggleText = new GUIContent("Subsurface Enabled", "Enable/Disable the subsurface");
            public static GUIContent subsurfaceMaskText = new GUIContent("Subsurface mask", "Determines the strength of the subsurface scattering effect.");
            public static GUIContent subsurfaceMaskMapText = new GUIContent("Subsurface mask map (R)", "Determines the strength of the subsurface scattering effect.");

            // Thickness
            public static GUIContent thicknessText = new GUIContent("Thickness", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.");
            public static GUIContent thicknessMapText = new GUIContent("Thickness map (R)", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.");
            public static GUIContent thicknessRemapText = new GUIContent("Thickness Remap", "Remaps values of the thickness map from [0, 1] to the specified range.");

            // Emissive
            public static GUIContent UVMappingEmissiveText = new GUIContent("Emissive UV mapping", "");
            public static GUIContent emissiveText = new GUIContent("Emissive Map + Color", "Emissive Map + Color (linear RGB) in nits unit");
            public static GUIContent albedoAffectEmissiveText = new GUIContent("Albedo Affect Emissive", "Specifies whether or not the emissive color is multiplied by the albedo.");

            // Specular occlusion
            // public static GUIContent enableSpecularOcclusionText = new GUIContent("Enable Specular Occlusion from Bent normal", "Require cosine weighted bent normal and cosine weighted ambient occlusion. Specular occlusion for reflection probe");
            // public static GUIContent specularOcclusionWarning = new GUIContent("Require a cosine weighted bent normal and ambient occlusion maps");
        }

        // Fabric Type
        protected MaterialProperty fabricType = null;
        protected const string kFabricType = "_FabricType";

        // Base UV set & mask
        protected MaterialProperty UVBase = null;
        protected const string kUVBase = "_UVBase";
        protected MaterialProperty UVMappingMask = null;
        protected const string kUVMappingMask = "_UVMappingMask";

        // Base color
        protected MaterialProperty baseColor = null;
        protected const string kBaseColor = "_BaseColor";

        // Base color map
        protected MaterialProperty baseColorMap = null;
        protected const string kBaseColorMap = "_BaseColorMap";
        protected MaterialProperty smoothness = null;

        // Smoothness
        protected const string kSmoothness = "_Smoothness";

        // Mask map
        protected MaterialProperty maskMap = null;
        protected const string kMaskMap = "_MaskMap";

        // Smoothness remapping
        protected MaterialProperty smoothnessRemapMin = null;
        protected const string kSmoothnessRemapMin = "_SmoothnessRemapMin";
        protected MaterialProperty smoothnessRemapMax = null;
        protected const string kSmoothnessRemapMax = "_SmoothnessRemapMax";

        // AO remapping
        protected MaterialProperty aoRemapMin = null;
        protected const string kAORemapMin = "_AORemapMin";
        protected MaterialProperty aoRemapMax = null;
        protected const string kAORemapMax = "_AORemapMax";

        // Normal Scale & Map
        protected MaterialProperty normalScale = null;
        protected const string kNormalScale = "_NormalScale";
        protected MaterialProperty normalMap = null;
        protected const string kNormalMap = "_NormalMap";
        // protected MaterialProperty bentNormalMap = null;
        // protected const string kBentNormalMap = "_BentNormalMap";

        // Tangent Map
        protected MaterialProperty tangentMap = null;
        protected const string kTangentMap = "_TangentMap";

        // Fuzz Tint
        protected MaterialProperty fuzzTint = null;
        protected const string kFuzzTint = "_FuzzTint";

        // Diffusion profile
        protected MaterialProperty diffusionProfileID = null;
        protected const string kDiffusionProfileID = "_DiffusionProfile";

        // Transmission
        protected MaterialProperty enableTransmission = null;
        protected const string kEnableTransmission = "_EnableTransmission";

        // Subsurface scattering
        protected MaterialProperty enableSubsurfaceScattering = null;
        protected const string kEnableSubsurfaceScattering = "_EnableSubsurfaceScattering";
        protected MaterialProperty subsurfaceMask = null;
        protected const string kSubsurfaceMask = "_SubsurfaceMask";
        protected MaterialProperty subsurfaceMaskMap = null;
        protected const string kSubsurfaceMaskMap = "_SubsurfaceMaskMap";

        // Thickness
        protected MaterialProperty thickness = null;
        protected const string kThickness = "_Thickness";
        protected MaterialProperty thicknessMap = null;
        protected const string kThicknessMap = "_ThicknessMap";
        protected MaterialProperty thicknessRemap = null;
        protected const string kThicknessRemap = "_ThicknessRemap";

        // UV Detail Set & Mask
        protected MaterialProperty UVDetail = null;
        protected const string kUVDetail = "_UVDetail";
        protected MaterialProperty UVMappingMaskDetail = null;
        protected const string kUVMappingMaskDetail = "_UVMappingMaskDetail";
        
        // Detail Map
        protected MaterialProperty detailMap = null;
        protected const string kDetailMap = "_DetailMap";

        // Fuzz Detail
        protected MaterialProperty fuzzDetailMap = null;
        protected const string kFuzzDetailMap = "_FuzzDetailMap";

        // Link detail with base
        protected MaterialProperty linkDetailsWithBase = null;
        protected const string kLinkDetailsWithBase = "_LinkDetailsWithBase";     

        // Detail adjusting
        protected MaterialProperty detailAOScale = null;
        protected const string kDetailAOScale = "_DetailAOScale";
        protected MaterialProperty detailNormalScale = null;
        protected const string kDetailNormalScale = "_DetailNormalScale";
        protected MaterialProperty detailSmoothnessScale = null;
        protected const string kDetailSmoothnessScale = "_DetailSmoothnessScale";

        // protected MaterialProperty tangentMap = null;
        // protected const string kTangentMap = "_TangentMap";
        protected MaterialProperty anisotropy = null;
        protected const string kAnisotropy = "_Anisotropy";
        protected MaterialProperty anisotropyMap = null;
        protected const string kAnisotropyMap = "_AnisotropyMap";

        // UV Emissive Set & Mask
        protected MaterialProperty UVEmissive = null;
        protected const string kUVEmissive = "_UVEmissive";
        protected MaterialProperty UVMappingMaskEmissive = null;
        protected const string kUVMappingMaskEmissive = "_UVMappingMaskEmissive";

        // Emissive
        protected MaterialProperty emissiveColor = null;
        protected const string kEmissiveColor = "_EmissiveColor";
        protected MaterialProperty emissiveColorMap = null;
        protected const string kEmissiveColorMap = "_EmissiveColorMap";

        // protected MaterialProperty enableSpecularOcclusion = null;
        // protected const string kEnableSpecularOcclusion = "_EnableSpecularOcclusion";


        override protected void FindMaterialProperties(MaterialProperty[] props)
        {
            // Fabric Type
            fabricType = FindProperty(kFabricType, props);           

            // Base UV set & mask
            UVBase = FindProperty(kUVBase, props);
            UVMappingMask = FindProperty(kUVMappingMask, props);

            // Base Color & Map
            baseColor = FindProperty(kBaseColor, props);
            baseColorMap = FindProperty(kBaseColorMap, props);

            // Smoothness
            smoothness = FindProperty(kSmoothness, props);

            // Mask and remapping values
            maskMap = FindProperty(kMaskMap, props);
            smoothnessRemapMin = FindProperty(kSmoothnessRemapMin, props);
            smoothnessRemapMax = FindProperty(kSmoothnessRemapMax, props);
            aoRemapMin = FindProperty(kAORemapMin, props);
            aoRemapMax = FindProperty(kAORemapMax, props);

            // Normal map and scale
            normalMap = FindProperty(kNormalMap, props);
            normalScale = FindProperty(kNormalScale, props);
            // bentNormalMap = FindProperty(kBentNormalMap, props);

            // Tangent map
            tangentMap = FindProperty(kTangentMap, props);

            // Fuzz tint
            fuzzTint = FindProperty(kFuzzTint, props);

            // Diffusion profile
            diffusionProfileID = FindProperty(kDiffusionProfileID, props);

            // Transmission
            enableTransmission = FindProperty(kEnableTransmission, props);

            // Sub surface
            enableSubsurfaceScattering = FindProperty(kEnableSubsurfaceScattering, props);
            subsurfaceMask = FindProperty(kSubsurfaceMask, props);
            subsurfaceMaskMap = FindProperty(kSubsurfaceMaskMap, props);

            // Thickness
            thickness = FindProperty(kThickness, props);
            thicknessMap = FindProperty(kThicknessMap, props);
            thicknessRemap = FindProperty(kThicknessRemap, props);

            // Details Set and Mask
            UVDetail = FindProperty(kUVDetail, props);
            UVMappingMaskDetail = FindProperty(kUVMappingMaskDetail, props);
            
            // Detail map and rmapping
            detailMap = FindProperty(kDetailMap, props);
            fuzzDetailMap = FindProperty(kFuzzDetailMap, props);
            detailAOScale = FindProperty(kDetailAOScale, props);
            detailNormalScale = FindProperty(kDetailNormalScale, props);
            detailSmoothnessScale = FindProperty(kDetailSmoothnessScale, props);
            linkDetailsWithBase = FindProperty(kLinkDetailsWithBase, props);

            // Anisotropy
            // tangentMap = FindProperty(kTangentMap, props);
            anisotropy = FindProperty(kAnisotropy, props);
            anisotropyMap = FindProperty(kAnisotropyMap, props);

            // UV Emissive set & Mask
            UVEmissive = FindProperty(kUVEmissive, props);
            UVMappingMaskEmissive = FindProperty(kUVMappingMaskEmissive, props);

            // Emissive Data
            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);

            // Specular occlusion
            // enableSpecularOcclusion = FindProperty(kEnableSpecularOcclusion, props);
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

        public enum UVEmissiveMapping
        {
            UV0,
            UV1,
            UV2,
            UV3
        }

        protected void BaseUVMappingInputGUI()
        {
            m_MaterialEditor.ShaderProperty(UVBase, Styles.UVBaseMappingText);

            UVBaseMapping uvBaseMapping = (UVBaseMapping)UVBase.floatValue;

            float X, Y, Z, W;
            X = (uvBaseMapping == UVBaseMapping.UV0) ? 1.0f : 0.0f;
            Y = (uvBaseMapping == UVBaseMapping.UV1) ? 1.0f : 0.0f;
            Z = (uvBaseMapping == UVBaseMapping.UV2) ? 1.0f : 0.0f;
            W = (uvBaseMapping == UVBaseMapping.UV3) ? 1.0f : 0.0f;

            UVMappingMask.colorValue = new Color(X, Y, Z, W);

            m_MaterialEditor.TextureScaleOffsetProperty(baseColorMap);
        }

        protected void BaseInputGUI(Material material)
        {
            // The set of inputs offered for customizing the material 
            EditorGUILayout.LabelField(Styles.InputsText, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // The base color map and matching base color value
            m_MaterialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap, baseColor);

            // If no mask texture was provided, we display the smoothness value
            if (maskMap.textureValue == null)
            {
                m_MaterialEditor.ShaderProperty(smoothness, Styles.smoothnessText);
            }

            // If we have a mask map, we do not use values but remapping fields instead
            m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapSpecularText, maskMap);
            if (maskMap.textureValue != null)
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

            // The primal normal map field
            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap, normalScale);

            // The fuzz tint value (that affects the color of the specular lighting term)
            m_MaterialEditor.ShaderProperty(fuzzTint, Styles.fuzzTintText);

            // m_MaterialEditor.TexturePropertySingleLine(Styles.bentNormalMapText, bentNormalMap);

            // The diffusion/transmission/subsurface gui
            ShaderSSSAndTransmissionInputGUI(material);

            // Anisotropy GUI
            ShaderAnisoInputGUI(material);

            // Define the UV mapping for the base textures
            BaseUVMappingInputGUI();

            EditorGUI.indentLevel--;
        }

        protected void DetailsInput(Material material)
        {
            EditorGUILayout.LabelField(Styles.detailText, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            m_MaterialEditor.TexturePropertySingleLine(Styles.detailMapNormalText, detailMap);

            if (material.GetTexture(kDetailMap))
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(detailAOScale, Styles.detailAOScaleText);
                m_MaterialEditor.ShaderProperty(detailNormalScale, Styles.detailNormalScaleText);
                m_MaterialEditor.ShaderProperty(detailSmoothnessScale, Styles.detailSmoothnessScaleText);
                EditorGUI.indentLevel--;
            }

            m_MaterialEditor.TexturePropertySingleLine(Styles.FuzzDetailText, fuzzDetailMap);

            if (material.GetTexture(kDetailMap) || material.GetTexture(kFuzzDetailMap))
            {
                EditorGUI.indentLevel++;

                m_MaterialEditor.ShaderProperty(UVDetail, Styles.UVDetailMappingText);

                // Setup the UVSet for detail, if planar/triplanar is use for base, it will override the mapping of detail (See shader code)
                float X, Y, Z, W;
                X = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV0) ? 1.0f : 0.0f;
                Y = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV1) ? 1.0f : 0.0f;
                Z = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV2) ? 1.0f : 0.0f;
                W = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV3) ? 1.0f : 0.0f;
                UVMappingMaskDetail.colorValue = new Color(X, Y, Z, W);

                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(linkDetailsWithBase, Styles.linkDetailsWithBaseText);
                EditorGUI.indentLevel--;
                m_MaterialEditor.TextureScaleOffsetProperty(detailMap);
            }
            EditorGUI.indentLevel--;
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

         
            // Enable transmission toggle
            m_MaterialEditor.ShaderProperty(enableTransmission, Styles.transmissionToggleText);

            // Subsurface toggle and options
            m_MaterialEditor.ShaderProperty(enableSubsurfaceScattering, Styles.subsurfaceToggleText);
            if (enableSubsurfaceScattering.floatValue == 1.0f)
            {
                m_MaterialEditor.ShaderProperty(subsurfaceMask, Styles.subsurfaceMaskText);
                m_MaterialEditor.TexturePropertySingleLine(Styles.subsurfaceMaskMapText, subsurfaceMaskMap);
            }

            // The thickness sub-menu is toggled if either the transmission or subsurface are requested
            if (enableSubsurfaceScattering.floatValue == 1.0f || enableTransmission.floatValue == 1.0f)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.thicknessMapText, thicknessMap);
                if (thicknessMap.textureValue != null)
                {
                    // Display the remap of texture values.
                    Vector2 remap = thicknessRemap.vectorValue;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(Styles.thicknessRemapText, ref remap.x, ref remap.y, 0.0f, 1.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        thicknessRemap.vectorValue = remap;
                    }
                }
                else
                {
                    // Allow the user to set the constant value of thickness if no thickness map is provided.
                    m_MaterialEditor.ShaderProperty(thickness, Styles.thicknessText);
                }
            }

            // We only need to display the diffusion profile if we have either transmission or diffusion
            // TODO: Optimize me
            if (enableSubsurfaceScattering.floatValue == 1.0f || enableTransmission.floatValue == 1.0f)
            {
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
            }
        }
        protected void EmissiveInputGUI(Material material)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.emissiveLabelText, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);

            if (material.GetTexture(kEmissiveColorMap))
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(UVEmissive, Styles.UVMappingEmissiveText);
                UVBaseMapping uvEmissiveMapping = (UVBaseMapping)UVEmissive.floatValue;

                float X, Y, Z, W;
                X = (uvEmissiveMapping == UVBaseMapping.UV0) ? 1.0f : 0.0f;
                Y = (uvEmissiveMapping == UVBaseMapping.UV1) ? 1.0f : 0.0f;
                Z = (uvEmissiveMapping == UVBaseMapping.UV2) ? 1.0f : 0.0f;
                W = (uvEmissiveMapping == UVBaseMapping.UV3) ? 1.0f : 0.0f;

                UVMappingMaskEmissive.colorValue = new Color(X, Y, Z, W);


                m_MaterialEditor.TextureScaleOffsetProperty(emissiveColorMap);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        protected void ShaderAnisoInputGUI(Material material)
        {
            // We only have anisotropy for the silk fabric
            FabricType fabricType = (FabricType)material.GetFloat(kFabricType);
            if(fabricType == FabricType.Silk)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.tangentMapText, tangentMap);
                m_MaterialEditor.ShaderProperty(anisotropy, Styles.anisotropyText);
                m_MaterialEditor.TexturePropertySingleLine(Styles.anisotropyMapText, anisotropyMap);
            }
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            GUILayout.Label("Fabric Options", EditorStyles.boldLabel);

            // The generic type of the fabric (either cotton/wool or silk)
            EditorGUI.indentLevel++;
            m_MaterialEditor.ShaderProperty(fabricType, Styles.fabricTypeText);
            EditorGUI.indentLevel--;

            // Base GUI
            EditorGUI.indentLevel++;
            BaseInputGUI(material);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Emissive GUI
            EditorGUI.indentLevel++;
            EmissiveInputGUI(material);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Details Input
            EditorGUI.indentLevel++;
            DetailsInput(material);
            EditorGUI.indentLevel--;
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
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));

            // However, the tangent map flag is only bound to the presence of a tangent map
            // CoreUtils.SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMap));

            // For the moment, we do not support the bent normal map
            // CoreUtils.SetKeyword(material, "_BENTNORMALMAP", material.GetTexture(kBentNormalMap));

            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));

            // We do not support specular occlusion for the moment
            // CoreUtils.SetKeyword(material, "_ENABLESPECULAROCCLUSION", material.GetFloat(kEnableSpecularOcclusion) > 0.0f);

            CoreUtils.SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
            CoreUtils.SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap));
            CoreUtils.SetKeyword(material, "_FUZZDETAIL_MAP", material.GetTexture(kFuzzDetailMap));
            CoreUtils.SetKeyword(material, "_SUBSURFACE_MASK_MAP", material.GetTexture(kSubsurfaceMaskMap));
            CoreUtils.SetKeyword(material, "_THICKNESSMAP", material.GetTexture(kThicknessMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap)); 

            // Require and set 
            bool needUV2 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV2 || (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV2 || (UVBaseMapping)material.GetFloat(kUVEmissive) == UVBaseMapping.UV2;
            bool needUV3 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV3 || (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV3 || (UVBaseMapping)material.GetFloat(kUVEmissive) == UVBaseMapping.UV2;

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

            // Fetch the fabric type
            FabricType fabricType = (FabricType)material.GetFloat(kFabricType);

            // If the material is of type cotton/wool we inject it! Otherwise it is necessarily of silk/anisotropy type (we don't inject it to save keywords)
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_COTTON_WOOL", fabricType == FabricType.CottonWool);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", material.GetFloat(kEnableSubsurfaceScattering) > 0.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", material.GetFloat(kEnableTransmission) > 0.0f);

            
        }
    }
} // namespace UnityEditor
