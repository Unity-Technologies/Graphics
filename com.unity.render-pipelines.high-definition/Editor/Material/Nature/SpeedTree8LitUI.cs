using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SpeedTree8LitGUI : UnityEditor.ShaderGUI
    {
        bool m_FirstTimeApply = true;

        private static class Styles
        {
            public static GUIContent colorText = EditorGUIUtility.TrTextContent("Color", "Color (RGB) and Opacity (A)");
            public static GUIContent normalMapText = EditorGUIUtility.TrTextContent("Normal", "Normal (RGB)");
            public static GUIContent extraMapText = EditorGUIUtility.TrTextContent("Extra", "Smoothness (R), Metallic (G), AO (B)");
            public static GUIContent subsurfaceMapText = EditorGUIUtility.TrTextContent("Subsurface", "Subsurface (RGB)");

            public static GUIContent smoothnessText = EditorGUIUtility.TrTextContent("Smoothness", "Smoothness value");
            public static GUIContent metallicText = EditorGUIUtility.TrTextContent("Metallic", "Metallic value");

            public static GUIContent twoSidedText = EditorGUIUtility.TrTextContent("Two-Sided", "Set this material to render as two-sided");
            public static GUIContent windQualityText = EditorGUIUtility.TrTextContent("Wind Quality", "Wind quality setting");
            public static GUIContent hueVariationText = EditorGUIUtility.TrTextContent("Hue Variation", "Hue variation Color (RGB) and Amount (A)");
            public static GUIContent normalMappingText = EditorGUIUtility.TrTextContent("Normal Map", "Enable normal mapping");
            public static GUIContent subsurfaceText = EditorGUIUtility.TrTextContent("Subsurface", "Enable subsurface scattering");
            public static GUIContent subsurfaceIndirectText = EditorGUIUtility.TrTextContent("Indirect Subsurface", "Scalar on subsurface from indirect light");

            public static GUIContent billboardText = EditorGUIUtility.TrTextContent("Billboard", "Enable billboard features (crossfading, etc.)");
            public static GUIContent billboardShadowFadeText = EditorGUIUtility.TrTextContent("Shadow Fade", "Fade shadow effect on billboards");

            public static GUIContent primaryMapsText = EditorGUIUtility.TrTextContent("Maps");
            public static GUIContent optionsText = EditorGUIUtility.TrTextContent("Options");
            public static GUIContent advancedText = EditorGUIUtility.TrTextContent("Advanced Options");
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (m_FirstTimeApply)
            {
                foreach (var obj in materialEditor.targets)
                {
                    MaterialChanged((Material)obj);
                }
                m_FirstTimeApply = false;
            }

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0.0f;

            EditorGUI.BeginChangeCheck();
            {
                GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);

                // The idea with this containiner is to hold only those properties we are not handling
                // explicitly within this OnGUI call (which are handled in a special way).  So we take
                // everything and remove anything that was handled specially, and then just display
                // whatever remains based on 
                List<MaterialProperty> remainingProps = new List<MaterialProperty>(properties);

                // color
                var colorTexProp = ShaderGUI.FindProperty("_MainTex", properties);
                var colorProp = ShaderGUI.FindProperty("_Color", properties);
                materialEditor.TexturePropertySingleLine(Styles.colorText, colorTexProp, null, colorProp);

                remainingProps.Remove(colorTexProp);
                remainingProps.Remove(colorProp);

                // normal
                var normalTexProp = ShaderGUI.FindProperty("_BumpMap", properties);
                materialEditor.TexturePropertySingleLine(Styles.normalMapText, normalTexProp);
                remainingProps.Remove(normalTexProp);

                // extra
                var extraTexProp = ShaderGUI.FindProperty("_ExtraTex", properties);
                materialEditor.TexturePropertySingleLine(Styles.extraMapText, extraTexProp, null);
                remainingProps.Remove(extraTexProp);
                if (extraTexProp.textureValue == null)
                {
                    var glossProp = ShaderGUI.FindProperty("_Glossiness", properties);
                    materialEditor.ShaderProperty(glossProp, Styles.smoothnessText, 2);
                    var metallicProp = ShaderGUI.FindProperty("_Metallic", properties);
                    materialEditor.ShaderProperty(metallicProp, Styles.metallicText, 2);
                }
                // Need to remove this whether we showed it or not.
                remainingProps.Remove(ShaderGUI.FindProperty("_Glossiness", properties));
                remainingProps.Remove(ShaderGUI.FindProperty("_Metallic", properties));

                // subsurface
                var ssTexProp = ShaderGUI.FindProperty("_SubsurfaceTex", properties);
                var ssProp = ShaderGUI.FindProperty("_SubsurfaceColor", properties);
                materialEditor.TexturePropertySingleLine(Styles.subsurfaceMapText, ssTexProp, null, ssProp);
                remainingProps.Remove(ssTexProp);
                remainingProps.Remove(ssProp);

                // other options
                EditorGUILayout.Space();
                GUILayout.Label(Styles.optionsText, EditorStyles.boldLabel);

                MakeAlignedProperty(FindProperty("_TwoSided", properties), Styles.twoSidedText, materialEditor, true);
                MakeAlignedProperty(FindProperty("_WindQuality", properties), Styles.windQualityText, materialEditor, true);
                MakeCheckedProperty(FindProperty("_HueVariationKwToggle", properties), FindProperty("_HueVariationColor", properties), Styles.hueVariationText, materialEditor);
                MakeAlignedProperty(FindProperty("_NormalMapKwToggle", properties), Styles.normalMappingText, materialEditor, true);
                remainingProps.Remove(FindProperty("_TwoSided", properties));
                remainingProps.Remove(FindProperty("_WindQuality", properties));
                remainingProps.Remove(FindProperty("_HueVariationKwToggle", properties));
                remainingProps.Remove(FindProperty("_HueVariationColor", properties));
                remainingProps.Remove(FindProperty("_NormalMapKwToggle", properties));

                // subsurface
                var subsurfaceToggle = FindProperty("_SubsurfaceKwToggle", properties);
                MakeAlignedProperty(subsurfaceToggle, Styles.subsurfaceText, materialEditor, true);
                remainingProps.Remove(subsurfaceToggle);
                if (subsurfaceToggle.floatValue > 0.0f)
                {
                    var sssIndirectProp = ShaderGUI.FindProperty("_SubsurfaceIndirect", properties);
                    materialEditor.ShaderProperty(sssIndirectProp, Styles.subsurfaceIndirectText, 2);
                }
                remainingProps.Remove(FindProperty("_SubsurfaceIndirect", properties));

                // billboard
                var billboardToggle = FindProperty("_BillboardKwToggle", properties);
                MakeAlignedProperty(billboardToggle, Styles.billboardText, materialEditor, true);
                remainingProps.Remove(billboardToggle);
                if (billboardToggle.floatValue > 0.0f)
                {
                    var prop = ShaderGUI.FindProperty("_BillboardShadowFade", properties);
                    materialEditor.ShaderProperty(prop, Styles.billboardShadowFadeText, 2);
                }
                remainingProps.Remove(FindProperty("_BillboardShadowFade", properties));

                // Now that the only things that remain are the properties that might be unique to this shader
                // that haven't been handled already.
                foreach(var prop in remainingProps)
                {
                    if ((prop.flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) != 0)
                        continue;
                    materialEditor.ShaderProperty(prop, prop.displayName);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in materialEditor.targets)
                {
                    Material mat = (Material)obj;
                    MaterialChanged(mat);
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label(Styles.advancedText, EditorStyles.boldLabel);
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }

        static void MakeAlignedProperty(MaterialProperty prop, GUIContent text, MaterialEditor materialEditor, bool doubleWide = false)
        {
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2.0f);
            r.width = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth * (doubleWide ? 2.0f : 1.0f);

            materialEditor.ShaderProperty(r, prop, text);
        }

        static void MakeCheckedProperty(MaterialProperty keywordToggleProp, MaterialProperty prop, GUIContent text, MaterialEditor materialEditor, bool doubleWide = false)
        {
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2.0f);
            r.width = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth / 2;

            materialEditor.ShaderProperty(r, keywordToggleProp, text);

            using (new EditorGUI.DisabledScope(keywordToggleProp.floatValue == 0.0f))
            {
                r.width = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth * (doubleWide ? 2.0f : 1.0f);
                r.x += EditorGUIUtility.fieldWidth / 2;

                materialEditor.ShaderProperty(r, prop, " ");
            }
        }

        static void MaterialChanged(Material material)
        {
            SetKeyword(material, "EFFECT_EXTRA_TEX", material.GetTexture("_ExtraTex"));
        }

        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
}
