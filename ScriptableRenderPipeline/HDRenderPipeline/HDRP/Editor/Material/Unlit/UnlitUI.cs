using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class UnlitGUI : BaseUnlitGUI
    {
        protected static class Styles
        {
            public static string InputsText = "Inputs";

            public static GUIContent colorText = new GUIContent("Color", "Color");

            public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive");
            public static GUIContent emissiveIntensityText = new GUIContent("Emissive Intensity", "Emissive");
        }

        protected MaterialProperty color = null;
        protected const string kColor = "_UnlitColor";
        protected MaterialProperty colorMap = null;
        protected const string kColorMap = "_UnlitColorMap";
        protected MaterialProperty emissiveColor = null;
        protected const string kEmissiveColor = "_EmissiveColor";
        protected MaterialProperty emissiveColorMap = null;
        protected const string kEmissiveColorMap = "_EmissiveColorMap";
        protected MaterialProperty emissiveIntensity = null;
        protected const string kEmissiveIntensity = "_EmissiveIntensity";

        override protected void FindMaterialProperties(MaterialProperty[] props)
        {
            color = FindProperty(kColor, props);
            colorMap = FindProperty(kColorMap, props);

            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
            emissiveIntensity = FindProperty(kEmissiveIntensity, props);
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            EditorGUILayout.LabelField(Styles.InputsText, EditorStyles.boldLabel);

            m_MaterialEditor.TexturePropertySingleLine(Styles.colorText, colorMap, color);
            m_MaterialEditor.TextureScaleOffsetProperty(colorMap);

            m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
            m_MaterialEditor.TextureScaleOffsetProperty(emissiveColorMap);
            m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);

            var surfaceTypeValue = (SurfaceType)surfaceType.floatValue;
            if (surfaceTypeValue == SurfaceType.Transparent)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(StylesBaseUnlit.TransparencyInputsText, EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;

                DoDistortionInputsGUI();

                --EditorGUI.indentLevel;
            }
        }

        protected override void MaterialPropertiesAdvanceGUI(Material material)
        {
        }

        protected override void VertexAnimationPropertiesGUI()
        {

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
            SetupBaseUnlitKeywords(material);
            SetupBaseUnlitMaterialPass(material);

            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
        }
    }
} // namespace UnityEditor
