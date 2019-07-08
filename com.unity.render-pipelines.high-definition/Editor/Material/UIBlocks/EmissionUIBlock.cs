using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class EmissionUIBlock : MaterialUIBlock
    {
        [Flags]
        public enum Features
        {
            None                = 0,
            EnableEmissionForGI = 1 << 0,
            MultiplyWithBase    = 1 << 1,
            All                 = ~0
        }

        public class Styles
        {
            public const string header = "Emission Inputs";

            public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive Color (RGB).");

            public static GUIContent albedoAffectEmissiveText = new GUIContent("Emission multiply with Base", "Specifies whether or not the emission color is multiplied by the albedo.");
            public static GUIContent useEmissiveIntensityText = new GUIContent("Use Emission Intensity", "Specifies whether to use to a HDR color or a LDR color with a separate multiplier.");
            public static GUIContent emissiveIntensityText = new GUIContent("Emission Intensity", "");
            public static GUIContent emissiveIntensityFromHDRColorText = new GUIContent("The emission intensity is from the HDR color picker in luminance", "");
            public static GUIContent emissiveExposureWeightText = new GUIContent("Exposure weight", "Control the percentage of emission to expose.");

            public static GUIContent UVEmissiveMappingText = new GUIContent("Emission UV mapping", "");
            public static GUIContent texWorldScaleText = new GUIContent("World Scale", "Sets the tiling factor HDRP applies to Planar/Trilinear mapping.");
        }

        MaterialProperty emissiveColorLDR = null;
        const string kEmissiveColorLDR = "_EmissiveColorLDR";
        MaterialProperty emissiveExposureWeight = null;
        const string kEmissiveExposureWeight = "_EmissiveExposureWeight";
        MaterialProperty useEmissiveIntensity = null;
        const string kUseEmissiveIntensity = "_UseEmissiveIntensity";
        MaterialProperty emissiveIntensityUnit = null;
        const string kEmissiveIntensityUnit = "_EmissiveIntensityUnit";
        MaterialProperty emissiveIntensity = null;
        const string kEmissiveIntensity = "_EmissiveIntensity";
        MaterialProperty emissiveColor = null;
        const string kEmissiveColor = "_EmissiveColor";
        MaterialProperty emissiveColorMap = null;
        const string kEmissiveColorMap = "_EmissiveColorMap";
        MaterialProperty UVEmissive = null;
        const string kUVEmissive = "_UVEmissive";
        MaterialProperty TexWorldScaleEmissive = null;
        const string kTexWorldScaleEmissive = "_TexWorldScaleEmissive";
        MaterialProperty UVMappingMaskEmissive = null;
        const string kUVMappingMaskEmissive = "_UVMappingMaskEmissive";
        MaterialProperty albedoAffectEmissive = null;
        const string kAlbedoAffectEmissive = "_AlbedoAffectEmissive";

        Expandable  m_ExpandableBit;
        Features    m_Features;

        public EmissionUIBlock(Expandable expandableBit, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
        }

        public override void LoadMaterialProperties()
        {
            emissiveColor = FindProperty(kEmissiveColor);
            emissiveColorMap = FindProperty(kEmissiveColorMap);
            emissiveIntensityUnit = FindProperty(kEmissiveIntensityUnit);
            emissiveIntensity = FindProperty(kEmissiveIntensity);
            emissiveExposureWeight = FindProperty(kEmissiveExposureWeight);
            emissiveColorLDR = FindProperty(kEmissiveColorLDR);
            useEmissiveIntensity = FindProperty(kUseEmissiveIntensity);
            albedoAffectEmissive = FindProperty(kAlbedoAffectEmissive);
            UVEmissive = FindProperty(kUVEmissive);
            TexWorldScaleEmissive = FindProperty(kTexWorldScaleEmissive);
            UVMappingMaskEmissive = FindProperty(kUVMappingMaskEmissive);
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                    DrawEmissionGUI();
            }
        }

        void DrawEmissionGUI()
        {
            EditorGUI.BeginChangeCheck();
            materialEditor.ShaderProperty(useEmissiveIntensity, Styles.useEmissiveIntensityText);
            bool updateEmissiveColor = EditorGUI.EndChangeCheck();

            if (useEmissiveIntensity.floatValue == 0)
            {
                EditorGUI.BeginChangeCheck();
                DoEmissiveTextureProperty(emissiveColor);
                if (EditorGUI.EndChangeCheck() || updateEmissiveColor)
                    emissiveColor.colorValue = emissiveColor.colorValue;
                EditorGUILayout.HelpBox(Styles.emissiveIntensityFromHDRColorText.text, MessageType.Info, true);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                {
                    DoEmissiveTextureProperty(emissiveColorLDR);
                    emissiveColorLDR.colorValue = NormalizeEmissionColor(ref updateEmissiveColor, emissiveColorLDR.colorValue);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EmissiveIntensityUnit unit = (EmissiveIntensityUnit)emissiveIntensityUnit.floatValue;

                        if (unit == EmissiveIntensityUnit.Luminance)
                        {
                            using (var change = new EditorGUI.ChangeCheckScope())
                            {
                                materialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
                                if (change.changed)
                                    emissiveIntensity.floatValue = Mathf.Clamp(emissiveIntensity.floatValue, 0, float.MaxValue);
                            }
                        }
                        else
                        {
                            float evValue = LightUtils.ConvertLuminanceToEv(emissiveIntensity.floatValue);
                            evValue = EditorGUILayout.FloatField(Styles.emissiveIntensityText, evValue);
                            evValue = Mathf.Clamp(evValue, 0, float.MaxValue);
                            emissiveIntensity.floatValue = LightUtils.ConvertEvToLuminance(evValue);
                        }
                        emissiveIntensityUnit.floatValue = (float)(EmissiveIntensityUnit)EditorGUILayout.EnumPopup(unit);
                    }
                }
                if (EditorGUI.EndChangeCheck() || updateEmissiveColor)
                    emissiveColor.colorValue = emissiveColorLDR.colorValue * emissiveIntensity.floatValue;
            }

            materialEditor.ShaderProperty(emissiveExposureWeight, Styles.emissiveExposureWeightText);

            if ((m_Features & Features.MultiplyWithBase) != 0)
                materialEditor.ShaderProperty(albedoAffectEmissive, Styles.albedoAffectEmissiveText);

            // Emission for GI?
            if ((m_Features & Features.EnableEmissionForGI) != 0)
            {
                if (materialEditor.EmissionEnabledProperty())
                {
                    // change the GI flag and fix it up with emissive as black if necessary
                    materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true, true);
                }
            }
        }

        void DoEmissiveTextureProperty(MaterialProperty color)
        {
            materialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, color);

            // TODO: does not support multi-selection
            if (materials[0].GetTexture(kEmissiveColorMap))
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(UVEmissive, Styles.UVEmissiveMappingText);
                UVBaseMapping uvEmissiveMapping = (UVBaseMapping)UVEmissive.floatValue;

                float X, Y, Z, W;
                X = (uvEmissiveMapping == UVBaseMapping.UV0) ? 1.0f : 0.0f;
                Y = (uvEmissiveMapping == UVBaseMapping.UV1) ? 1.0f : 0.0f;
                Z = (uvEmissiveMapping == UVBaseMapping.UV2) ? 1.0f : 0.0f;
                W = (uvEmissiveMapping == UVBaseMapping.UV3) ? 1.0f : 0.0f;

                UVMappingMaskEmissive.colorValue = new Color(X, Y, Z, W);

                if ((uvEmissiveMapping == UVBaseMapping.Planar) || (uvEmissiveMapping == UVBaseMapping.Triplanar))
                {
                    materialEditor.ShaderProperty(TexWorldScaleEmissive, Styles.texWorldScaleText);
                }

                materialEditor.TextureScaleOffsetProperty(emissiveColorMap);
                EditorGUI.indentLevel--;
            }
        }

        Color NormalizeEmissionColor(ref bool emissiveColorUpdated, Color color)
        {
            if (HDRenderPipelinePreferences.materialEmissionColorNormalization)
            {
                // When enabling the material emission color normalization the ldr color might not be normalized,
                // so we need to update the emissive color
                if (!Mathf.Approximately(ColorUtils.Luminance(color), 1))
                    emissiveColorUpdated = true;

                color = HDUtils.NormalizeColor(color);
            }
            return color;
        }
    }
}