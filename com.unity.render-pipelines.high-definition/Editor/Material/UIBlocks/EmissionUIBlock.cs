using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    class EmissionUIBlock : MaterialUIBlock
    {
        // Max EV Value. Equals to LightUtils.ConvertLuminanceToEv(float.MaxValue)
        // Literal value to avoid precision issue with max float and to be independent of ColorUtils.s_LightMeterCalibrationConstant.
        static float s_MaxEvValue = 130.0f;

        [Flags]
        public enum Features
        {
            None                = 0,
            EnableEmissionForGI = 1 << 0,
            MultiplyWithBase    = 1 << 1,
            All                 = ~0
        }

        static Func<LightingSettings> GetLightingSettingsOrDefaultsFallback;

        static EmissionUIBlock()
        {
            Type lightMappingType = typeof(Lightmapping);
            var getLightingSettingsOrDefaultsFallbackInfo = lightMappingType.GetMethod("GetLightingSettingsOrDefaultsFallback", BindingFlags.Static | BindingFlags.NonPublic);
            var getLightingSettingsOrDefaultsFallbackLambda = Expression.Lambda<Func<LightingSettings>>(Expression.Call(null, getLightingSettingsOrDefaultsFallbackInfo));
            GetLightingSettingsOrDefaultsFallback = getLightingSettingsOrDefaultsFallbackLambda.Compile();
        }


        public class Styles
        {
            public const string header = "Emission Inputs";

            public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive Color (RGB).");

            public static GUIContent albedoAffectEmissiveText = new GUIContent("Emission multiply with Base", "Specifies whether or not the emission color is multiplied by the albedo.");
            public static GUIContent useEmissiveIntensityText = new GUIContent("Use Emission Intensity", "Specifies whether to use to a HDR color or a LDR color with a separate multiplier.");
            public static GUIContent emissiveIntensityText = new GUIContent("Emission Intensity", "");
            public static GUIContent emissiveIntensityFromHDRColorText = new GUIContent("The emission intensity is from the HDR color picker in luminance", "");
            public static GUIContent emissiveExposureWeightText = new GUIContent("Exposure weight", "Controls how the camera exposure influences the perceived intensity of the emissivity. A weight of 0 means that the emissive intensity is calculated ignoring the exposure; increasing this weight progressively increases the influence of exposure on the final emissive value.");

            public static GUIContent UVEmissiveMappingText = new GUIContent("Emission UV mapping", "");
            public static GUIContent texWorldScaleText = new GUIContent("World Scale", "Sets the tiling factor HDRP applies to Planar/Trilinear mapping.");
            public static GUIContent bakedEmission = new GUIContent("Baked Emission", "");
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

        void UpdateEmissiveColorFromIntensityAndEmissiveColorLDR()
        {
            materialEditor.serializedObject.ApplyModifiedProperties();
            foreach (Material target in materials)
            {
                target.UpdateEmissiveColorFromIntensityAndEmissiveColorLDR();
            }
            materialEditor.serializedObject.Update();
        }

        internal static void UpdateEmissiveColorFromIntensityAndEmissiveColorLDR(MaterialProperty emissiveColorLDR, MaterialProperty emissiveIntensity, MaterialProperty emissiveColor)
            => emissiveColor.colorValue = emissiveColorLDR.colorValue.linear * emissiveIntensity.floatValue;

        internal static void UpdateEmissiveColorLDRFromIntensityAndEmissiveColor(MaterialProperty emissiveColorLDR, MaterialProperty emissiveIntensity, MaterialProperty emissiveColor)
        {
            Color emissiveColorLDRLinear = emissiveColor.colorValue / emissiveIntensity.floatValue;
            emissiveColorLDR.colorValue = emissiveColorLDRLinear.gamma;
        }

        void UpdateEmissionUnit(float newUnitFloat)
        {
            foreach (Material target in materials)
            {
                if (target.HasProperty(kEmissiveIntensityUnit) && target.HasProperty(kEmissiveIntensity))
                {
                    target.SetFloat(kEmissiveIntensityUnit, newUnitFloat);
                }
            }
            materialEditor.serializedObject.Update();
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
                float newUnitFloat;
                float newIntensity = emissiveIntensity.floatValue;
                bool unitIsMixed = emissiveIntensityUnit.hasMixedValue;
                bool intensityIsMixed = unitIsMixed || emissiveIntensity.hasMixedValue;
                bool intensityChanged = false;
                bool unitChanged = false;
                EditorGUI.BeginChangeCheck();
                {
                    DoEmissiveTextureProperty(emissiveColorLDR);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EmissiveIntensityUnit unit = (EmissiveIntensityUnit)emissiveIntensityUnit.floatValue;
                        EditorGUI.showMixedValue = intensityIsMixed;

                        if (unit == EmissiveIntensityUnit.Nits)
                        {
                            using (var change = new EditorGUI.ChangeCheckScope())
                            {
                                materialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
                                intensityChanged = change.changed;
                                if (intensityChanged)
                                    newIntensity = Mathf.Clamp(emissiveIntensity.floatValue, 0, float.MaxValue);
                            }
                        }
                        else
                        {
                            float value = emissiveIntensity.floatValue;
                            if (!intensityIsMixed)
                            {
                                float evValue = LightUtils.ConvertLuminanceToEv(emissiveIntensity.floatValue);
                                evValue = EditorGUILayout.FloatField(Styles.emissiveIntensityText, evValue);
                                newIntensity = Mathf.Clamp(evValue, 0, s_MaxEvValue);
                                emissiveIntensity.floatValue = LightUtils.ConvertEvToLuminance(evValue);
                            }
                            else
                            {
                                using (var change = new EditorGUI.ChangeCheckScope())
                                {
                                    newIntensity = EditorGUILayout.FloatField(Styles.emissiveIntensityText, value);
                                    intensityChanged = change.changed;
                                }
                            }
                        }
                        EditorGUI.showMixedValue = false;

                        EditorGUI.showMixedValue = emissiveIntensityUnit.hasMixedValue;
                        using (var change = new EditorGUI.ChangeCheckScope())
                        {
                            newUnitFloat = (float)(EmissiveIntensityUnit)EditorGUILayout.EnumPopup(unit);
                            unitChanged = change.changed;
                        }
                        EditorGUI.showMixedValue = false;
                    }
                }
                if (EditorGUI.EndChangeCheck() || updateEmissiveColor)
                {
                    if(unitChanged)
                    {
                        if (unitIsMixed)
                            UpdateEmissionUnit(newUnitFloat);
                        else
                            emissiveIntensityUnit.floatValue = newUnitFloat;
                    }

                    // We don't allow changes on intensity if units are mixed
                    if (intensityChanged && !unitIsMixed)
                        emissiveIntensity.floatValue = newIntensity;

                    UpdateEmissiveColorFromIntensityAndEmissiveColorLDR(emissiveColorLDR, emissiveIntensity, emissiveColor);
                }
            }

            materialEditor.ShaderProperty(emissiveExposureWeight, Styles.emissiveExposureWeightText);

            if ((m_Features & Features.MultiplyWithBase) != 0)
                materialEditor.ShaderProperty(albedoAffectEmissive, Styles.albedoAffectEmissiveText);

            // Emission for GI?
            if ((m_Features & Features.EnableEmissionForGI) != 0)
            {
                BakedEmissionEnabledProperty(materialEditor);
            }
        }


        public static bool BakedEmissionEnabledProperty(MaterialEditor materialEditor)
        {
            Material[] materials = Array.ConvertAll(materialEditor.targets, (UnityEngine.Object o) => { return (Material)o; });
            
            // Calculate isMixed
            bool enabled = materials[0].globalIlluminationFlags == MaterialGlobalIlluminationFlags.BakedEmissive;
            bool isMixed = materials.Any(m => m.globalIlluminationFlags != materials[0].globalIlluminationFlags);

            // initial checkbox for enabling/disabling emission
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = isMixed;
            enabled = EditorGUILayout.Toggle(Styles.bakedEmission, enabled);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material mat in materials)
                {
                    mat.globalIlluminationFlags = enabled ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
                return enabled;
            }
            return !isMixed && enabled;
        }

        void DoEmissiveTextureProperty(MaterialProperty color)
        {
            materialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, color);

            if (materials.All(m => m.GetTexture(kEmissiveColorMap)))
            {
                EditorGUI.indentLevel++;
                if (UVEmissive != null) // Unlit does not have UVEmissive
                {
                    materialEditor.ShaderProperty(UVEmissive, Styles.UVEmissiveMappingText);
                    UVEmissiveMapping uvEmissiveMapping = (UVEmissiveMapping)UVEmissive.floatValue;

                    float X, Y, Z, W;
                    X = (uvEmissiveMapping == UVEmissiveMapping.UV0) ? 1.0f : 0.0f;
                    Y = (uvEmissiveMapping == UVEmissiveMapping.UV1) ? 1.0f : 0.0f;
                    Z = (uvEmissiveMapping == UVEmissiveMapping.UV2) ? 1.0f : 0.0f;
                    W = (uvEmissiveMapping == UVEmissiveMapping.UV3) ? 1.0f : 0.0f;

                    UVMappingMaskEmissive.colorValue = new Color(X, Y, Z, W);

                    if ((uvEmissiveMapping == UVEmissiveMapping.Planar) || (uvEmissiveMapping == UVEmissiveMapping.Triplanar))
                    {
                        materialEditor.ShaderProperty(TexWorldScaleEmissive, Styles.texWorldScaleText);
                    }
                }

                if (UVEmissive == null || (UVEmissiveMapping)UVEmissive.floatValue != UVEmissiveMapping.SameAsBase)
                    materialEditor.TextureScaleOffsetProperty(emissiveColorMap);
                EditorGUI.indentLevel--;
            }
        }
    }
}
