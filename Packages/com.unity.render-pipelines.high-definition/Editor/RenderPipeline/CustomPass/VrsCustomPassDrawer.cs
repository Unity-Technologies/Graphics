using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// FullScreen custom pass drawer
    /// </summary>
    [CustomPassDrawerAttribute(typeof(VrsCustomPass))]
    class VrsCustomPassDrawer : CustomPassDrawer
    {
        private class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2;

            public static GUIContent vrsColorMask = new GUIContent("Color Mask", "Color mask texture used to generate the shading-rate-image texture for variable rate shading.");
        }

        // Vrs pass
        SerializedProperty m_VrsColorMask;

        protected override PassUIFlag commonPassUIFlags => PassUIFlag.Name;
        protected override void Initialize(SerializedProperty customPass)
        {
            m_VrsColorMask = customPass.FindPropertyRelative("vrsColorMask");
        }

        protected override void DoPassGUI(SerializedProperty customPass, Rect rect)
        {
            if (HDRenderPipeline.currentAsset == null || !HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportVariableRateShading)
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP asset does not support Variable Rate Shading.", MessageType.Error,
                    HDRenderPipelineUI.ExpandableGroup.Rendering, "m_RenderPipelineSettings.supportVariableRateShading");

            EditorGUI.PropertyField(rect, m_VrsColorMask, Styles.vrsColorMask);
            rect.y += Styles.defaultLineSpace;
        }

        protected override float GetPassHeight(SerializedProperty customPass)
        {
            int lineCount = 1; // m_vrsColorMask
            int height = (int)(Styles.defaultLineSpace * lineCount);

            return height;
        }
    }
}
