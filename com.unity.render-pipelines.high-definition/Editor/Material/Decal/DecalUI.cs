using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class DecalUI : ExpandableAreaMaterial
    {
        [Flags]
        enum Expandable : uint
        {
            Input = 1 << 0,
            Sorting = 1 << 1,
        }
        protected override uint defaultExpandedState { get { return (uint)Expandable.Input; } }

        protected static class Styles
        {
            public static string InputsText = "Surface Inputs";
            public static string SortingText = "Sorting Inputs";

            public static GUIContent baseColorText = new GUIContent("Base Map", "Specify the base color (RGB) and opacity (A) of the decal.");
            public static GUIContent baseColorText2 = new GUIContent("Opacity", "Specify the opacity (A) of the decal.");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Specifies the normal map for this Material (BC7/BC5/DXT5(nm)).");
            public static GUIContent decalBlendText = new GUIContent("Global Opacity", "Controls the opacity of the entire decal.");
            public static GUIContent albedoModeText = new GUIContent("Affect BaseColor", "Base color + Opacity, Opacity only.");
            public static GUIContent normalOpacityChannelText = new GUIContent("Normal Opacity Channel", "Specifies the source this Material uses as opacity for its Normal Map.");
 			public static GUIContent meshDecalDepthBiasText = new GUIContent("Mesh Decal Depth Bias", "Sets a depth bias to stop the decal's Mesh from overlapping with other Meshes.");
	 		public static GUIContent drawOrderText = new GUIContent("Draw Order", "Controls the draw order of Decal Projectors. HDRP draws decals with lower values first.");
            public static GUIContent smoothnessRemappingText = new GUIContent("Smoothness Remapping", "Controls a remap for the smoothness channel in the Mask Map.");
            public static GUIContent metallicText = new GUIContent("Metallic Scale", "Controls a scale factor for the metallic channel in the Mask Map.");
            public static GUIContent aoRemappingText = new GUIContent("Ambient Occlusion Remapping", "Controls a remap for the ambient occlusion channel in the Mask Map.");
            public static GUIContent maskOpacityChannelText = new GUIContent("Mask Opacity Channel", "Specifies the source this Material uses as opacity for its Mask Map.");
            public static GUIContent affectMetalText = new GUIContent("Affect Metal", "When enabled, this decal uses the metallic channel of its Mask Map. When disabled, the decal has no metallic effect.");
            public static GUIContent affectAmbientOcclusionText = new GUIContent("Affect Ambient Occlusion", "When enabled, this decal uses the smoothness channel of its Mask Map. When disabled, the decal has no smoothness effect.");
            public static GUIContent affectSmoothnessText = new GUIContent("Affect Smoothness", "When enabled, this decal uses the ambient occlusion channel of its Mask Map. When disabled, the decal has no ambient occlusion effect.");
            public static GUIContent maskMapBlueScaleText = new GUIContent("Scale Mask Map Blue Channel", "Controls the scale of the blue channel of the Mask Map. You can use this as opacity depending on the blend source you choose.");
            public static GUIContent emissiveText = new GUIContent("Emissive", "When enabled, this Material becomes emissive and appears self-illuminated.");
            public static GUIContent useEmissionIntensityText = new GUIContent("Use Emission Intensity", "When enabled, this Material separates emission color and intensity. This makes the Emission Map into an LDR color and exposes the Emission Intensity property.");
            public static GUIContent emissionMapText = new GUIContent("Emission Map", "Specifies a map (RGB) that the Material uses for emission.");
            public static GUIContent emissiveIntensityText = new GUIContent("Emission Intensity", "Sets the overall strength of the emission effect.");
            public static GUIContent emissiveExposureWeightText = new GUIContent("Exposure weight", "Control the percentage of emission to expose.");


            public static GUIContent[] maskMapText =
            {
                new GUIContent("Error", "Mask map"), // Not possible
                new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Metal(R), Opacity(B)"), // Decal.MaskBlendFlags.Metal:
                new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Ambient Occlusion(G), Opacity(B)"), // Decal.MaskBlendFlags.AO:
                new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Metal(R), Ambient Occlusion(G), Opacity(B)"), // Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO:
                new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Opacity(B), Smoothness(A)"), // Decal.MaskBlendFlags.Smoothness:
                new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Metal(R), Opacity(B), Smoothness(A)"), // Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.Smoothness:
                new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Ambient Occlusion(G), Opacity(B), Smoothness(A)"), // Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Metal(R), Ambient Occlusion(G), Opacity(B), Smoothness(A)") // Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
            };
        }

        enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue
        }
        protected string[] blendSourceNames = Enum.GetNames(typeof(BlendSource));

        protected string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

        protected MaterialProperty baseColorMap = new MaterialProperty();
        protected const string kBaseColorMap = "_BaseColorMap";

        protected MaterialProperty baseColor = new MaterialProperty();
        protected const string kBaseColor = "_BaseColor";

        protected MaterialProperty normalMap = new MaterialProperty();
        protected const string kNormalMap = "_NormalMap";

        protected MaterialProperty maskMap = new MaterialProperty();
        protected const string kMaskMap = "_MaskMap";

        protected MaterialProperty decalBlend = new MaterialProperty();
        protected const string kDecalBlend = "_DecalBlend";

        protected MaterialProperty albedoMode = new MaterialProperty();
        protected const string kAlbedoMode = "_AlbedoMode";

        protected MaterialProperty normalBlendSrc = new MaterialProperty();
        protected const string kNormalBlendSrc = "_NormalBlendSrc";

        protected MaterialProperty maskBlendSrc = new MaterialProperty();
        protected const string kMaskBlendSrc = "_MaskBlendSrc";

        protected MaterialProperty maskBlendMode = new MaterialProperty();
        protected const string kMaskBlendMode = "_MaskBlendMode";

        protected MaterialProperty maskmapMetal = new MaterialProperty();
        protected const string kMaskmapMetal = "_MaskmapMetal";

        protected MaterialProperty maskmapAO = new MaterialProperty();
        protected const string kMaskmapAO = "_MaskmapAO";

        protected MaterialProperty maskmapSmoothness = new MaterialProperty();
        protected const string kMaskmapSmoothness = "_MaskmapSmoothness";

        protected MaterialProperty decalMeshDepthBias = new MaterialProperty();
        protected const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

        protected MaterialProperty drawOrder = new MaterialProperty();
        protected const string kDrawOrder = "_DrawOrder";

        protected const string kDecalStencilWriteMask = "_DecalStencilWriteMask";
        protected const string kDecalStencilRef = "_DecalStencilRef";

        protected MaterialProperty AORemapMin = new MaterialProperty();
        protected const string kAORemapMin = "_AORemapMin";

        protected MaterialProperty AORemapMax = new MaterialProperty();
        protected const string kAORemapMax = "_AORemapMax";

        protected MaterialProperty smoothnessRemapMin = new MaterialProperty();
        protected const string kSmoothnessRemapMin = "_SmoothnessRemapMin";

        protected MaterialProperty smoothnessRemapMax = new MaterialProperty();
        protected const string kSmoothnessRemapMax = "_SmoothnessRemapMax";

        protected MaterialProperty metallicScale = new MaterialProperty();
        protected const string kMetallicScale = "_MetallicScale";

        protected MaterialProperty maskMapBlueScale = new MaterialProperty();
        protected const string kMaskMapBlueScale = "_DecalMaskMapBlueScale";

        protected MaterialProperty emissiveColor = new MaterialProperty();
        protected const string kEmissiveColor = "_EmissiveColor";

        protected MaterialProperty emissiveColorMap = new MaterialProperty();
        protected const string kEmissiveColorMap = "_EmissiveColorMap";

        protected MaterialProperty emissive = new MaterialProperty();
        protected const string kEmissive = "_Emissive";

        protected MaterialProperty emissiveIntensity = null;
        protected const string kEmissiveIntensity = "_EmissiveIntensity";

        protected MaterialProperty emissiveIntensityUnit = null;
        protected const string kEmissiveIntensityUnit = "_EmissiveIntensityUnit";

        protected MaterialProperty useEmissiveIntensity = null;
        protected const string kUseEmissiveIntensity = "_UseEmissiveIntensity";

        protected MaterialProperty emissiveColorLDR = null;
        protected const string kEmissiveColorLDR = "_EmissiveColorLDR";

        protected MaterialProperty emissiveColorHDR = null;
        protected const string kEmissiveColorHDR = "_EmissiveColorHDR";

        protected MaterialProperty emissiveExposureWeight = null;
        protected const string kEmissiveExposureWeight = "_EmissiveExposureWeight";

        protected MaterialEditor m_MaterialEditor;

        void FindMaterialProperties(MaterialProperty[] props)
        {
            baseColor = FindProperty(kBaseColor, props);
            baseColorMap = FindProperty(kBaseColorMap, props);
            normalMap = FindProperty(kNormalMap, props);
            maskMap = FindProperty(kMaskMap, props);
            decalBlend = FindProperty(kDecalBlend, props);
            albedoMode = FindProperty(kAlbedoMode, props);
            normalBlendSrc = FindProperty(kNormalBlendSrc, props);
            maskBlendSrc = FindProperty(kMaskBlendSrc, props);
            maskBlendMode = FindProperty(kMaskBlendMode, props);
            maskmapMetal = FindProperty(kMaskmapMetal, props);
            maskmapAO = FindProperty(kMaskmapAO, props);
            maskmapSmoothness = FindProperty(kMaskmapSmoothness, props);            
            decalMeshDepthBias = FindProperty(kDecalMeshDepthBias, props);            
            drawOrder = FindProperty(kDrawOrder, props);
            AORemapMin = FindProperty(kAORemapMin, props);
            AORemapMax = FindProperty(kAORemapMax, props);
            smoothnessRemapMin = FindProperty(kSmoothnessRemapMin, props);
            smoothnessRemapMax = FindProperty(kSmoothnessRemapMax, props);
            metallicScale = FindProperty(kMetallicScale, props);
            maskMapBlueScale = FindProperty(kMaskMapBlueScale, props);
            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
            emissive = FindProperty(kEmissive, props);
            useEmissiveIntensity = FindProperty(kUseEmissiveIntensity, props);
            emissiveIntensityUnit = FindProperty(kEmissiveIntensityUnit, props);
            emissiveIntensity = FindProperty(kEmissiveIntensity, props);
            emissiveColorLDR = FindProperty(kEmissiveColorLDR, props);
            emissiveColorHDR = FindProperty(kEmissiveColorHDR, props);
            emissiveExposureWeight = FindProperty(kEmissiveExposureWeight, props);
            
            // always instanced
            SerializedProperty instancing = m_MaterialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            Decal.MaskBlendFlags blendMode = (Decal.MaskBlendFlags)material.GetFloat(kMaskBlendMode);

            CoreUtils.SetKeyword(material, "_ALBEDOCONTRIBUTION", material.GetFloat(kAlbedoMode) == 1.0f);
            CoreUtils.SetKeyword(material, "_COLORMAP", material.GetTexture(kBaseColorMap));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
            CoreUtils.SetKeyword(material, "_EMISSIVEMAP", material.GetTexture(kEmissiveColorMap));

            material.SetInt(kDecalStencilWriteMask, (int)HDRenderPipeline.StencilBitMask.Decals);
            material.SetInt(kDecalStencilRef, (int)HDRenderPipeline.StencilBitMask.Decals);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecals3RTStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsForwardEmissive, material.GetFloat("_Emissive") == 1.0f);
            switch (blendMode)
            {
                case Decal.MaskBlendFlags.Metal:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMStr, true);
                    break;

                case Decal.MaskBlendFlags.AO:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOStr, true);
                    break;

                case Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsSStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMSStr, true);
                    break;

                case Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOSStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOSStr, true);
                    break;
            }
        }

        protected void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;
            float normalBlendSrcValue = normalBlendSrc.floatValue;
            float maskBlendSrcValue =  maskBlendSrc.floatValue;
            float smoothnessRemapMinValue = smoothnessRemapMin.floatValue;
            float smoothnessRemapMaxValue = smoothnessRemapMax.floatValue;
            float AORemapMinValue = AORemapMin.floatValue;
            float AORemapMaxValue = AORemapMax.floatValue;

            Decal.MaskBlendFlags maskBlendFlags = (Decal.MaskBlendFlags)maskBlendMode.floatValue;              

            HDRenderPipelineAsset hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            bool perChannelMask = hdrp.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;

            using (var header = new HeaderScope(Styles.InputsText, (uint)Expandable.Input, this))
            {
                if (header.expanded)
                {
                    // Detect any changes to the material
                    EditorGUI.BeginChangeCheck();
                    {
                        m_MaterialEditor.TexturePropertySingleLine((material.GetFloat(kAlbedoMode) == 1.0f) ? Styles.baseColorText : Styles.baseColorText2, baseColorMap, baseColor);
                        // Currently always display Albedo contribution as we have an albedo tint that apply
                        EditorGUI.indentLevel++;
                        m_MaterialEditor.ShaderProperty(albedoMode, Styles.albedoModeText);
                        EditorGUI.indentLevel--;
                        m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);
                        if (material.GetTexture(kNormalMap))
                        {
                            EditorGUI.indentLevel++;
                            normalBlendSrcValue = EditorGUILayout.Popup(Styles.normalOpacityChannelText, (int)normalBlendSrcValue, blendSourceNames);
                            EditorGUI.indentLevel--;
                        }

                        m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapText[(int)maskBlendFlags], maskMap);
                        if (material.GetTexture(kMaskMap))
                        {
                            EditorGUI.indentLevel++;
                            
                            EditorGUILayout.MinMaxSlider(Styles.smoothnessRemappingText, ref smoothnessRemapMinValue, ref smoothnessRemapMaxValue, 0.0f, 1.0f);
                            if (perChannelMask)
                            {
                                m_MaterialEditor.ShaderProperty(metallicScale, Styles.metallicText);
                                EditorGUILayout.MinMaxSlider(Styles.aoRemappingText, ref AORemapMinValue, ref AORemapMaxValue, 0.0f, 1.0f);
                            }

                            maskBlendSrcValue = EditorGUILayout.Popup(Styles.maskOpacityChannelText, (int)maskBlendSrcValue, blendSourceNames);

                            if (perChannelMask)
                            {
                                bool mustDisableScope = false;
                                if (maskmapMetal.floatValue + maskmapAO.floatValue + maskmapSmoothness.floatValue == 1.0f)
                                    mustDisableScope = true;

                                using (new EditorGUI.DisabledScope(mustDisableScope && maskmapMetal.floatValue == 1.0f))
                                {
                                    m_MaterialEditor.ShaderProperty(maskmapMetal, Styles.affectMetalText);
                                }
                                using (new EditorGUI.DisabledScope(mustDisableScope && maskmapAO.floatValue == 1.0f))
                                {
                                    m_MaterialEditor.ShaderProperty(maskmapAO, Styles.affectAmbientOcclusionText);
                                }
                                using (new EditorGUI.DisabledScope(mustDisableScope && maskmapSmoothness.floatValue == 1.0f))
                                {
                                    m_MaterialEditor.ShaderProperty(maskmapSmoothness, Styles.affectSmoothnessText);
                                }

                                // Sanity condition in case for whatever reasons all value are 0.0 but it should never happen
                                if ((maskmapMetal.floatValue == 0.0f) && (maskmapAO.floatValue == 0.0f) && (maskmapSmoothness.floatValue == 0.0f))
                                    maskmapSmoothness.floatValue = 1.0f;

                                maskBlendFlags = 0; // Re-init the mask

                                if (maskmapMetal.floatValue == 1.0f)
                                    maskBlendFlags |= Decal.MaskBlendFlags.Metal;
                                if (maskmapAO.floatValue == 1.0f)
                                    maskBlendFlags |= Decal.MaskBlendFlags.AO;
                                if (maskmapSmoothness.floatValue == 1.0f)
                                    maskBlendFlags |= Decal.MaskBlendFlags.Smoothness;
                            }
                            else // if perChannelMask is not enabled, force to have smoothness
                            {
                                maskBlendFlags = Decal.MaskBlendFlags.Smoothness;
                            }

                            EditorGUI.indentLevel--;
                        }

                        m_MaterialEditor.ShaderProperty(maskMapBlueScale, Styles.maskMapBlueScaleText);
                        m_MaterialEditor.ShaderProperty(decalBlend, Styles.decalBlendText);
                        m_MaterialEditor.ShaderProperty(emissive, Styles.emissiveText);
                        if (emissive.floatValue == 1.0f)
                        {
                            m_MaterialEditor.ShaderProperty(useEmissiveIntensity, Styles.useEmissionIntensityText);
                            
                            if (useEmissiveIntensity.floatValue == 1.0f)
                            {
                                m_MaterialEditor.TexturePropertySingleLine(Styles.emissionMapText, emissiveColorMap, emissiveColorLDR);
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EmissiveIntensityUnit unit = (EmissiveIntensityUnit)emissiveIntensityUnit.floatValue;

                                    if (unit == EmissiveIntensityUnit.Luminance)
                                        m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
                                    else
                                    {
                                        float evValue = LightUtils.ConvertLuminanceToEv(emissiveIntensity.floatValue);
                                        evValue = EditorGUILayout.FloatField(Styles.emissiveIntensityText, evValue);
                                        emissiveIntensity.floatValue = LightUtils.ConvertEvToLuminance(evValue);
                                    }
                                    emissiveIntensityUnit.floatValue = (float)(EmissiveIntensityUnit)EditorGUILayout.EnumPopup(unit);
                                }
                            }
                            else
                            {
                                m_MaterialEditor.TexturePropertySingleLine(Styles.emissionMapText, emissiveColorMap, emissiveColorHDR);
                            }

                            m_MaterialEditor.ShaderProperty(emissiveExposureWeight, Styles.emissiveExposureWeightText);
                        }

                        EditorGUI.indentLevel--;

                        EditorGUILayout.HelpBox(
                            "Enable 'Metal and AO properties' in your HDRP Asset if you want to control the Metal and AO properties of decals.\nThere is a performance cost of enabling this option.",
                            MessageType.Info);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        normalBlendSrc.floatValue = normalBlendSrcValue;
                        maskBlendSrc.floatValue = maskBlendSrcValue;
                        maskBlendMode.floatValue = (float)maskBlendFlags;
                        smoothnessRemapMin.floatValue = smoothnessRemapMinValue;
                        smoothnessRemapMax.floatValue = smoothnessRemapMaxValue;
                        AORemapMin.floatValue = AORemapMinValue;
                        AORemapMax.floatValue = AORemapMaxValue;
                        if (useEmissiveIntensity.floatValue == 1.0f)
                        {
                            emissiveColor.colorValue = emissiveColorLDR.colorValue * emissiveIntensity.floatValue;
                        }
                        else
                        {
                            emissiveColor.colorValue = emissiveColorHDR.colorValue;
                        }

                        foreach (var obj in m_MaterialEditor.targets)
                            SetupMaterialKeywordsAndPassInternal((Material)obj);
                    }
                }
            }

            EditorGUI.indentLevel++;
            using (var header = new HeaderScope(Styles.SortingText, (uint)Expandable.Sorting, this))
            {
                if (header.expanded)
                {
                    m_MaterialEditor.ShaderProperty(drawOrder, Styles.drawOrderText);
                    m_MaterialEditor.ShaderProperty(decalMeshDepthBias, Styles.meshDecalDepthBiasText);                    
                }
            }
            EditorGUI.indentLevel--;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_MaterialEditor = materialEditor;

            // We should always register the key used to keep collapsable state
            InitExpandableState(materialEditor);

            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();
            
            FindMaterialProperties(props);

            Material material = materialEditor.target as Material;
            ShaderPropertiesGUI(material);

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
}
