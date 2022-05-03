using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>The UI block that represents the surface inputs for decal materials.</summary>
    public class DecalSurfaceInputsUIBlock : MaterialUIBlock
    {
        internal class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Surface Inputs");
            public static GUIContent baseColorText = new GUIContent("Base Map", "Specify the base color (RGB) and opacity (A) of the decal.");
            public static GUIContent baseOpacityText = new GUIContent("Opacity", "Specify the opacity (A) of the decal.");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Specifies the normal map for this Material (BC7/BC5/DXT5(nm)).");
            public static GUIContent decalBlendText = new GUIContent("Global Opacity", "Controls the opacity of the entire decal.");
            public static GUIContent normalOpacityChannelText = new GUIContent("Normal Opacity Channel", "Specifies the source this Material uses as opacity for its Normal Map.");
            public static GUIContent smoothnessRemappingText = new GUIContent("Smoothness Remapping", "Controls a remap for the smoothness channel in the Mask Map.");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Controls the smoothness of the decal.");
            public static GUIContent metallicRemappingText = new GUIContent("Metallic Remapping", "Controls a remap for the metallic channel in the Mask Map.");
            public static GUIContent metallicText = new GUIContent("Metallic", "Controls the metallic of the decal.");
            public static GUIContent aoRemappingText = new GUIContent("Ambient Occlusion Remapping", "Controls a remap for the ambient occlusion channel in the Mask Map.");
            public static GUIContent aoText = new GUIContent("Ambient Occlusion", "Controls the ambient occlusion of the decal.");
            public static GUIContent maskOpacityChannelText = new GUIContent("Mask Opacity Channel", "Specifies the source this Material uses as opacity for its Mask Map.");
            public static GUIContent maskMapBlueScaleText = new GUIContent("Scale Mask Map Blue Channel", "Controls the scale of the blue channel of the Mask Map. You can use this as opacity depending on the blend source you choose.");
            public static GUIContent opacityBlueScaleText = new GUIContent("Mask Opacity", "Controls the opacity of the Mask (Metallic, Ambient Occlusion, Smoothness). You can use this as opacity depending on the blend source you choose.");
            public static GUIContent useEmissionIntensityText = new GUIContent("Use Emission Intensity", "When enabled, this Material separates emission color and intensity. This makes the Emission Map into an LDR color and exposes the Emission Intensity property.");
            public static GUIContent emissiveIntensityText = new GUIContent("Emission Intensity", "Sets the overall strength of the emission effect.");
            public static GUIContent emissiveExposureWeightText = new GUIContent("Exposure weight", "Controls how the camera exposure influences the perceived intensity of the emissivity. A weight of 0 means that the emissive intensity is calculated ignoring the exposure; increasing this weight progressively increases the influence of exposure on the final emissive value.");
            public static GUIContent decalLayerText = new GUIContent("Decal Layer", "Specifies the current Decal Layers that the Decal affects.This Decal affect corresponding Material with the same Decal Layer flags.");
            public static GUIContent maskMapText = new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Metal(R), Ambient Occlusion(G), Opacity(B), Smoothness(A)");
        }

        enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue
        }

        string[] blendSourceNames = new string[2];


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

        MaterialProperty maskmapMetal = new MaterialProperty();
        const string kMaskmapMetal = "_MaskmapMetal";

        MaterialProperty maskmapAO = new MaterialProperty();
        const string kMaskmapAO = "_MaskmapAO";

        MaterialProperty maskmapSmoothness = new MaterialProperty();
        const string kMaskmapSmoothness = "_MaskmapSmoothness";

        MaterialProperty metallicRemapMin = new MaterialProperty();
        const string kMetallicRemapMin = "_MetallicRemapMin";

        MaterialProperty metallicRemapMax = new MaterialProperty();
        const string kMetallicRemapMax = "_MetallicRemapMax";

        MaterialProperty AORemapMin = new MaterialProperty();
        const string kAORemapMin = "_AORemapMin";

        MaterialProperty AORemapMax = new MaterialProperty();
        const string kAORemapMax = "_AORemapMax";

        MaterialProperty smoothnessRemapMin = new MaterialProperty();
        const string kSmoothnessRemapMin = "_SmoothnessRemapMin";

        MaterialProperty smoothnessRemapMax = new MaterialProperty();
        const string kSmoothnessRemapMax = "_SmoothnessRemapMax";


        MaterialProperty AO = new MaterialProperty();
        const string kAO = "_AO";

        MaterialProperty smoothness = new MaterialProperty();
        const string kSmoothness = "_Smoothness";

        MaterialProperty metallic = new MaterialProperty();
        const string kMetallic = "_Metallic";


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

        /// <summary>
        /// Constructs a DecalSurfaceInputsUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index used to store the foldout state</param>
        public DecalSurfaceInputsUIBlock(ExpandableBit expandableBit)
            : base(expandableBit, Styles.header)
        {
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            baseColor = FindProperty(kBaseColor);
            baseColorMap = FindProperty(kBaseColorMap);
            normalMap = FindProperty(kNormalMap);
            maskMap = FindProperty(kMaskMap);
            decalBlend = FindProperty(kDecalBlend);
            normalBlendSrc = FindProperty(kNormalBlendSrc);
            maskBlendSrc = FindProperty(kMaskBlendSrc);
            maskmapMetal = FindProperty(kMaskmapMetal);
            maskmapAO = FindProperty(kMaskmapAO);
            maskmapSmoothness = FindProperty(kMaskmapSmoothness);
            metallicRemapMin = FindProperty(kMetallicRemapMin);
            metallicRemapMax = FindProperty(kMetallicRemapMax);
            AORemapMin = FindProperty(kAORemapMin);
            AORemapMax = FindProperty(kAORemapMax);
            smoothnessRemapMin = FindProperty(kSmoothnessRemapMin);
            smoothnessRemapMax = FindProperty(kSmoothnessRemapMax);
            AO = FindProperty(kAO);
            smoothness = FindProperty(kSmoothness);
            metallic = FindProperty(kMetallic);
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

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            var material = materials[0];
            bool affectAlbedo = material.HasProperty(kAffectAlbedo) && material.GetFloat(kAffectAlbedo) == 1.0f;
            bool affectNormal = material.HasProperty(kAffectNormal) && material.GetFloat(kAffectNormal) == 1.0f;
            bool affectMetal = material.HasProperty(kAffectMetal) && material.GetFloat(kAffectMetal) == 1.0f;
            bool affectSmoothness = material.HasProperty(kAffectSmoothness) && material.GetFloat(kAffectSmoothness) == 1.0f;
            bool affectAO = material.HasProperty(kAffectAO) && material.GetFloat(kAffectAO) == 1.0f;
            bool affectEmission = material.HasProperty(kAffectEmission) && material.GetFloat(kAffectEmission) == 1.0f;
            bool affectMaskmap = affectMetal || affectAO || affectSmoothness;

            bool perChannelMask = false;
            HDRenderPipelineAsset hdrp = HDRenderPipeline.currentAsset;
            if (hdrp != null)
            {
                perChannelMask = hdrp.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;
            }

            bool allMaskMap = materials.All(m => m.GetTexture(kMaskMap));

            blendSourceNames[0] = affectAlbedo ? "Base Color Map Alpha" : "Opacity";
            blendSourceNames[1] = allMaskMap ? "Mask Map Blue Channel" : "Mask Opacity";

            if (affectAlbedo)
                materialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap, baseColor);
            else
            {
                Color color = baseColor.colorValue;
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = baseColor.hasMixedValue;
                color.a = EditorGUILayout.Slider(Styles.baseOpacityText, color.a, 0.0f, 1.0f);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                    baseColor.colorValue = color;
            }

            using (new EditorGUI.DisabledScope(!affectNormal))
            {
                materialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);
                if (materials.All(m => m.GetTexture(kNormalMap)))
                {
                    EditorGUI.indentLevel++;
                    materialEditor.PopupShaderProperty(normalBlendSrc, Styles.normalOpacityChannelText, blendSourceNames);
                    EditorGUI.indentLevel--;
                }
            }

            using (new EditorGUI.DisabledScope(!affectMaskmap))
            {
                materialEditor.TexturePropertySingleLine(Styles.maskMapText, maskMap);
                EditorGUI.indentLevel++;
                if (allMaskMap)
                {
                    if (perChannelMask)
                    {
                        using (new EditorGUI.DisabledScope(!affectMetal))
                            materialEditor.MinMaxShaderProperty(metallicRemapMin, metallicRemapMax, 0.0f, 1.0f, Styles.metallicRemappingText);
                        using (new EditorGUI.DisabledScope(!affectAO))
                            materialEditor.MinMaxShaderProperty(AORemapMin, AORemapMax, 0.0f, 1.0f, Styles.aoRemappingText);
                    }

                    using (new EditorGUI.DisabledScope(!affectSmoothness))
                        materialEditor.MinMaxShaderProperty(smoothnessRemapMin, smoothnessRemapMax, 0.0f, 1.0f, Styles.smoothnessRemappingText);
                }
                else
                {
                    if (perChannelMask)
                    {
                        using (new EditorGUI.DisabledScope(!affectMetal))
                            materialEditor.ShaderProperty(metallic, Styles.metallicText);
                        using (new EditorGUI.DisabledScope(!affectAO))
                            materialEditor.ShaderProperty(AO, Styles.aoText);
                    }
                    using (new EditorGUI.DisabledScope(!affectSmoothness))
                        materialEditor.ShaderProperty(smoothness, Styles.smoothnessText);
                }

                materialEditor.PopupShaderProperty(maskBlendSrc, Styles.maskOpacityChannelText, blendSourceNames);

                EditorGUI.indentLevel--;
            }

            bool useBlueScale = (affectMaskmap && maskBlendSrc.floatValue == (float)BlendSource.MaskMapBlue) ||
                (affectNormal && normalBlendSrc.floatValue == (float)BlendSource.MaskMapBlue);
            using (new EditorGUI.DisabledScope(!useBlueScale))
                materialEditor.ShaderProperty(maskMapBlueScale, allMaskMap ? Styles.maskMapBlueScaleText : Styles.opacityBlueScaleText);
            materialEditor.ShaderProperty(decalBlend, Styles.decalBlendText);

            using (new EditorGUI.DisabledScope(!affectEmission))
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(useEmissiveIntensity, Styles.useEmissionIntensityText);
                bool updateEmissiveColor = EditorGUI.EndChangeCheck();

                if (useEmissiveIntensity.floatValue == 0.0f)
                {
                    if (updateEmissiveColor)
                        emissiveColorHDR.colorValue = emissiveColor.colorValue;

                    EditorGUI.BeginChangeCheck();
                    EmissionUIBlock.DoEmissiveTextureProperty(materialEditor, emissiveColorMap, emissiveColorHDR);
                    if (EditorGUI.EndChangeCheck())
                        emissiveColor.colorValue = emissiveColorHDR.colorValue;
                }
                else
                {
                    if (updateEmissiveColor)
                        EmissionUIBlock.UpdateEmissiveColorLDRAndIntensityFromEmissiveColor(emissiveColorLDR, emissiveIntensity, emissiveColor);

                    EditorGUI.BeginChangeCheck();
                    EmissionUIBlock.DoEmissiveTextureProperty(materialEditor, emissiveColorMap, emissiveColorLDR);
                    EmissionUIBlock.DoEmissiveIntensityGUI(materialEditor, emissiveIntensity, emissiveIntensityUnit);
                    if (EditorGUI.EndChangeCheck() || updateEmissiveColor)
                        EmissionUIBlock.UpdateEmissiveColorFromIntensityAndEmissiveColorLDR(emissiveColorLDR, emissiveIntensity, emissiveColor);
                }

                materialEditor.ShaderProperty(emissiveExposureWeight, Styles.emissiveExposureWeightText);
            }
        }
    }
}
