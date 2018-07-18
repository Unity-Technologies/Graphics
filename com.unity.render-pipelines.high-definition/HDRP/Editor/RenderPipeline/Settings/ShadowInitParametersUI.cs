using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<ShadowInitParametersUI, SerializedShadowInitParameters>;

    class ShadowInitParametersUI : BaseUI<SerializedShadowInitParameters>
    {
        public static readonly CED.IDrawer SectionAtlas = CED.Action(Drawer_FieldShadowSize);

        public ShadowInitParametersUI()
            : base(0)
        {
        }

        static void Drawer_FieldShadowSize(ShadowInitParametersUI s, SerializedShadowInitParameters d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Shadow"), EditorStyles.boldLabel);
            
            ++EditorGUI.indentLevel;
            EditorGUILayout.LabelField(_.GetContent("Shadow Atlas"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.shadowAtlasWidth, _.GetContent("Atlas Width"));
            EditorGUILayout.PropertyField(d.shadowAtlasHeight, _.GetContent("Atlas Height"));
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(_.GetContent("Shadow Map Budget"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.maxPointLightShadows, _.GetContent("Max Point Light Shadows"));
            EditorGUILayout.PropertyField(d.maxSpotLightShadows, _.GetContent("Max Spot Light Shadows"));
            EditorGUILayout.PropertyField(d.maxDirectionalLightShadows, _.GetContent("Max Directional Light Shadows"));
            --EditorGUI.indentLevel;

            // Clamp negative values
            d.shadowAtlasHeight.intValue = Mathf.Max(0, d.shadowAtlasHeight.intValue);
            d.shadowAtlasWidth.intValue = Mathf.Max(0, d.shadowAtlasWidth.intValue);
            d.maxPointLightShadows.intValue = Mathf.Max(0, d.maxPointLightShadows.intValue);
            d.maxSpotLightShadows.intValue = Mathf.Max(0, d.maxSpotLightShadows.intValue);
            d.maxDirectionalLightShadows.intValue = Mathf.Max(0, d.maxDirectionalLightShadows.intValue);

            --EditorGUI.indentLevel;
        }
    }
}
