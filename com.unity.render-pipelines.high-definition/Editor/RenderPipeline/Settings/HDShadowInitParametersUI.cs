using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDShadowInitParametersUI, SerializedHDShadowInitParameters>;

    class HDShadowInitParametersUI : BaseUI<SerializedHDShadowInitParameters>
    {
        public static readonly CED.IDrawer SectionAtlas = CED.Action(Drawer_FieldHDShadows);

        public HDShadowInitParametersUI()
            : base(0)
        {
        }

        static void Drawer_FieldHDShadows(HDShadowInitParametersUI s, SerializedHDShadowInitParameters d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("HD Shadow"), EditorStyles.boldLabel);
            
            ++EditorGUI.indentLevel;
            EditorGUILayout.LabelField(_.GetContent("Shadow Atlas"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.shadowAtlasWidth, _.GetContent("Atlas Width"));
            EditorGUILayout.PropertyField(d.shadowAtlasHeight, _.GetContent("Atlas Height"));
            bool shadowMap16Bits = (DepthBits)d.shadowMapDepthBits.intValue	== DepthBits.Depth16;
            shadowMap16Bits = EditorGUILayout.Toggle(_.GetContent("16-bit Shadow Maps"), shadowMap16Bits);
            d.shadowMapDepthBits.intValue = (shadowMap16Bits) ? (int)DepthBits.Depth16 : (int)DepthBits.Depth32;
            EditorGUILayout.PropertyField(d.useDynamicViewportRescale, _.GetContent("Dynamic Shadow Rescale|Scale the shadow map size using the screen size of the light to leave more space for other shadows in the atlas"));
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(_.GetContent("Shadow Map Budget"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.maxShadowRequests, _.GetContent("Max Shadow Requests|Max shadow requests (SR) per frame, 1 point light = 6 SR, 1 spot light = 1 SR and the directional is 4 SR"));
            --EditorGUI.indentLevel;
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(_.GetContent("Shadow Qualities"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.punctualShadowQuality, _.GetContent("Punctual Shadow Quality"));
            EditorGUILayout.PropertyField(d.directionalShadowQuality, _.GetContent("Directional Shadow Quality"));
            --EditorGUI.indentLevel;

            // Clamp negative values
            d.shadowAtlasHeight.intValue = Mathf.Max(0, d.shadowAtlasHeight.intValue);
            d.shadowAtlasWidth.intValue = Mathf.Max(0, d.shadowAtlasWidth.intValue);

            --EditorGUI.indentLevel;
        }
    }
}
