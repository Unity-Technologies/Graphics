using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.DecalSurfaceOptionsUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition
{
    class DecalSurfaceInputsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Surface Inputs";

            public static GUIContent baseColorText = new GUIContent("Base Map", "Specify the base color (RGB) and opacity (A) of the decal.");
            public static GUIContent baseColorText2 = new GUIContent("Opacity", "Specify the opacity (A) of the decal.");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Specifies the normal map for this Material (BC7/BC5/DXT5(nm)).");
            public static GUIContent decalBlendText = new GUIContent("Global Opacity", "Controls the opacity of the entire decal.");
            public static GUIContent normalOpacityChannelText = new GUIContent("Normal Opacity Channel", "Specifies the source this Material uses as opacity for its Normal Map.");
            public static GUIContent smoothnessRemappingText = new GUIContent("Smoothness Remapping", "Controls a remap for the smoothness channel in the Mask Map.");
            public static GUIContent metallicText = new GUIContent("Metallic Scale", "Controls a scale factor for the metallic channel in the Mask Map.");
            public static GUIContent aoRemappingText = new GUIContent("Ambient Occlusion Remapping", "Controls a remap for the ambient occlusion channel in the Mask Map.");
            public static GUIContent maskOpacityChannelText = new GUIContent("Mask Opacity Channel", "Specifies the source this Material uses as opacity for its Mask Map.");
            public static GUIContent maskMapBlueScaleText = new GUIContent("Scale Mask Map Blue Channel", "Controls the scale of the blue channel of the Mask Map. You can use this as opacity depending on the blend source you choose.");
            public static GUIContent useEmissionIntensityText = new GUIContent("Use Emission Intensity", "When enabled, this Material separates emission color and intensity. This makes the Emission Map into an LDR color and exposes the Emission Intensity property.");
            public static GUIContent emissionMapText = new GUIContent("Emission Map", "Specifies a map (RGB) that the Material uses for emission.");
            public static GUIContent emissiveIntensityText = new GUIContent("Emission Intensity", "Sets the overall strength of the emission effect.");
            public static GUIContent emissiveExposureWeightText = new GUIContent("Exposure weight", "Control the percentage of emission to expose.");
            public static GUIContent decalLayerText = new GUIContent("Decal Layer", "Specifies the current Decal Layers that the Decal affects.This Decal affect corresponding Material with the same Decal Layer flags.");

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

        Expandable  m_ExpandableBit;

        public float normalBlendSrcValue;
        public float maskBlendSrcValue;
        public float smoothnessRemapMinValue;
        public float smoothnessRemapMaxValue;
        public float AORemapMinValue;
        public float AORemapMaxValue;
        public Decal.MaskBlendFlags maskBlendFlags;

        enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue
        }
        string[] blendSourceNames = Enum.GetNames(typeof(BlendSource));

        string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

        MaterialProperty baseColorMap = new MaterialProperty();
        const string kBaseColorMap = "_BaseColorMap";

        MaterialProperty baseColor = new MaterialProperty();
        const string kBaseColor = "_BaseColor";

        MaterialProperty normalMap = new MaterialProperty();
        const string kNormalMap = "_NormalMap";

        MaterialProperty maskMap = new MaterialProperty();
        const string kMaskMap = "_MaskMap";

        MaterialProperty decalBlend = new MaterialProperty();
        const string kDecalBlend = "_DecalBlend";

        MaterialProperty normalBlendSrc = new MaterialProperty();
        const string kNormalBlendSrc = "_NormalBlendSrc";

        MaterialProperty maskBlendSrc = new MaterialProperty();
        const string kMaskBlendSrc = "_MaskBlendSrc";

        MaterialProperty maskBlendMode = new MaterialProperty();
        const string kMaskBlendMode = "_MaskBlendMode";

        MaterialProperty maskmapMetal = new MaterialProperty();
        const string kMaskmapMetal = "_MaskmapMetal";

        MaterialProperty maskmapAO = new MaterialProperty();
        const string kMaskmapAO = "_MaskmapAO";

        MaterialProperty maskmapSmoothness = new MaterialProperty();
        const string kMaskmapSmoothness = "_MaskmapSmoothness";

        MaterialProperty AORemapMin = new MaterialProperty();
        const string kAORemapMin = "_AORemapMin";

        MaterialProperty AORemapMax = new MaterialProperty();
        const string kAORemapMax = "_AORemapMax";

        MaterialProperty smoothnessRemapMin = new MaterialProperty();
        const string kSmoothnessRemapMin = "_SmoothnessRemapMin";

        MaterialProperty smoothnessRemapMax = new MaterialProperty();
        const string kSmoothnessRemapMax = "_SmoothnessRemapMax";

        MaterialProperty metallicScale = new MaterialProperty();
        const string kMetallicScale = "_MetallicScale";

        MaterialProperty maskMapBlueScale = new MaterialProperty();
        const string kMaskMapBlueScale = "_DecalMaskMapBlueScale";

        MaterialProperty emissiveColor = new MaterialProperty();
        const string kEmissiveColor = "_EmissiveColor";

        MaterialProperty emissiveColorMap = new MaterialProperty();
        const string kEmissiveColorMap = "_EmissiveColorMap";

        MaterialProperty affectEmission = new MaterialProperty();

        MaterialProperty emissiveIntensity = null;
        const string kEmissiveIntensity = "_EmissiveIntensity";

        MaterialProperty emissiveIntensityUnit = null;
        const string kEmissiveIntensityUnit = "_EmissiveIntensityUnit";

        MaterialProperty useEmissiveIntensity = null;
        const string kUseEmissiveIntensity = "_UseEmissiveIntensity";

        MaterialProperty emissiveColorLDR = null;
        const string kEmissiveColorLDR = "_EmissiveColorLDR";

        MaterialProperty emissiveColorHDR = null;
        const string kEmissiveColorHDR = "_EmissiveColorHDR";

        MaterialProperty emissiveExposureWeight = null;
        const string kEmissiveExposureWeight = "_EmissiveExposureWeight";

        public DecalSurfaceInputsUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            baseColor = FindProperty(kBaseColor);
            baseColorMap = FindProperty(kBaseColorMap);
            normalMap = FindProperty(kNormalMap);
            maskMap = FindProperty(kMaskMap);
            decalBlend = FindProperty(kDecalBlend);
            normalBlendSrc = FindProperty(kNormalBlendSrc);
            maskBlendSrc = FindProperty(kMaskBlendSrc);
            maskBlendMode = FindProperty(kMaskBlendMode);
            maskmapMetal = FindProperty(kMaskmapMetal);
            maskmapAO = FindProperty(kMaskmapAO);
            maskmapSmoothness = FindProperty(kMaskmapSmoothness);
            AORemapMin = FindProperty(kAORemapMin);
            AORemapMax = FindProperty(kAORemapMax);
            smoothnessRemapMin = FindProperty(kSmoothnessRemapMin);
            smoothnessRemapMax = FindProperty(kSmoothnessRemapMax);
            metallicScale = FindProperty(kMetallicScale);
            maskMapBlueScale = FindProperty(kMaskMapBlueScale);

            // TODO: move emission to the EmissionUIBlock ?
            emissiveColor = FindProperty(kEmissiveColor);
            emissiveColorMap = FindProperty(kEmissiveColorMap);
            affectEmission = FindProperty(kAffectEmission);
            useEmissiveIntensity = FindProperty(kUseEmissiveIntensity);
            emissiveIntensityUnit = FindProperty(kEmissiveIntensityUnit);
            emissiveIntensity = FindProperty(kEmissiveIntensity);
            emissiveColorLDR = FindProperty(kEmissiveColorLDR);
            emissiveColorHDR = FindProperty(kEmissiveColorHDR);
            emissiveExposureWeight = FindProperty(kEmissiveExposureWeight);

            // always instanced
            SerializedProperty instancing = materialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    DrawDecalGUI();
                }
            }
        }

        void DrawDecalGUI()
        {
            bool perChannelMask = false;
            HDRenderPipelineAsset hdrp = HDRenderPipeline.currentAsset;
            if (hdrp != null)
            {
                perChannelMask = hdrp.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;
            }

            normalBlendSrcValue = normalBlendSrc.floatValue;
            maskBlendSrcValue =  maskBlendSrc.floatValue;
            smoothnessRemapMinValue = smoothnessRemapMin.floatValue;
            smoothnessRemapMaxValue = smoothnessRemapMax.floatValue;
            AORemapMinValue = AORemapMin.floatValue;
            AORemapMaxValue = AORemapMax.floatValue;
            maskBlendFlags = (Decal.MaskBlendFlags)maskBlendMode.floatValue;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                materialEditor.TexturePropertySingleLine((materials[0].GetFloat(kAffectAlbedo) == 1.0f) ? Styles.baseColorText : Styles.baseColorText2, baseColorMap, baseColor);

                materialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);
                if (materials.All(m => m.GetTexture(kNormalMap)))
                {
                    EditorGUI.indentLevel++;
                    normalBlendSrcValue = EditorGUILayout.Popup(Styles.normalOpacityChannelText, (int)normalBlendSrcValue, blendSourceNames);
                    EditorGUI.indentLevel--;
                }

                materialEditor.TexturePropertySingleLine(Styles.maskMapText[(int)maskBlendFlags], maskMap);
                if (materials.All(m => m.GetTexture(kMaskMap)))
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.MinMaxSlider(Styles.smoothnessRemappingText, ref smoothnessRemapMinValue, ref smoothnessRemapMaxValue, 0.0f, 1.0f);
                    if (perChannelMask)
                    {
                        materialEditor.ShaderProperty(metallicScale, Styles.metallicText);
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
                            materialEditor.ShaderProperty(maskmapMetal, affectMetalText);
                        }
                        using (new EditorGUI.DisabledScope(mustDisableScope && maskmapAO.floatValue == 1.0f))
                        {
                            materialEditor.ShaderProperty(maskmapAO, affectAmbientOcclusionText);
                        }
                        using (new EditorGUI.DisabledScope(mustDisableScope && maskmapSmoothness.floatValue == 1.0f))
                        {
                            materialEditor.ShaderProperty(maskmapSmoothness, affectSmoothnessText);
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

                materialEditor.ShaderProperty(maskMapBlueScale, Styles.maskMapBlueScaleText);
                materialEditor.ShaderProperty(decalBlend, Styles.decalBlendText);
                if (affectEmission.floatValue == 1.0f)
                {
                    materialEditor.ShaderProperty(useEmissiveIntensity, Styles.useEmissionIntensityText);

                    if (useEmissiveIntensity.floatValue == 1.0f)
                    {
                        materialEditor.TexturePropertySingleLine(Styles.emissionMapText, emissiveColorMap, emissiveColorLDR);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EmissiveIntensityUnit unit = (EmissiveIntensityUnit)emissiveIntensityUnit.floatValue;

                            if (unit == EmissiveIntensityUnit.Nits)
                                materialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
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
                        materialEditor.TexturePropertySingleLine(Styles.emissionMapText, emissiveColorMap, emissiveColorHDR);
                    }

                    materialEditor.ShaderProperty(emissiveExposureWeight, Styles.emissiveExposureWeightText);
                }

                if (!perChannelMask)
                {
                    EditorGUILayout.HelpBox("Enable 'Metal and AO properties' in your HDRP Asset if you want to control the Metal and AO properties of decals.\nThere is a performance cost of enabling this option.",
                                            MessageType.Info);
                }
            }
        }
    }
}
