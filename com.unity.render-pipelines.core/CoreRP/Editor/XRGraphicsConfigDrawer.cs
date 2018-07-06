using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    [CustomPropertyDrawer(typeof(XRGraphicsConfig))]
    public class XRGraphicsConfigDrawer : PropertyDrawer
    {
        private float k_MinRenderScale = 0.01f;
        private float k_MaxRenderScale = 4.0f;
        internal class Styles
        {
            public static GUIContent XRSettingsLabel = new GUIContent("XR Config", "Enable XR in Player Settings. Then SetConfig can be used to set this configuration to XRSettings.");
            public static GUIContent XREnabledLabel = new GUIContent("Enable XR", "Enables stereo rendering");
            public static GUIContent renderScaleLabel = new GUIContent("Render Scale", "Scales (and reallocates) the camera render target allowing the game to render at a resolution different than native resolution. Can't be modified in play mode.");
            public static GUIContent viewportScaleLabel = new GUIContent("Viewport Scale", "Scales the section of the render target being rendered. Use for dynamic resolution adjustments.");
            public static GUIContent stereoRenderModeLabel = new GUIContent("Stereo Rendering Mode", "Use Player Settings to select between supported stereo rendering paths for current VR device.");
            public static GUIContent showDeviceViewLabel = new GUIContent("Show Device View", "If possible, mirror the render target of the VR device to the main display.");
            public static GUIContent gameViewRenderModeLabel = new GUIContent("Game View Render Mode", "Select how to reflect stereo display to game view");
            public static GUIContent useOcclusionMeshLabel = new GUIContent("Use Occlusion Mesh", "Determines whether or not to draw the occlusion mesh (goggles-shaped overlay) when rendering");
            public static GUIContent occlusionScaleLabel = new GUIContent("Occlusion Mesh Scale", "Scales the occlusion mesh");

        }
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var drawRenderScale = property.FindPropertyRelative("renderScale");
            var drawViewportScale = property.FindPropertyRelative("viewportScale");
            var drawShowDeviceView = property.FindPropertyRelative("showDeviceView");
            var drawGameViewRenderMode = property.FindPropertyRelative("gameViewRenderMode");
            var drawUseOcclusionMesh = property.FindPropertyRelative("useOcclusionMesh");
            var drawOcclusionMaskScale = property.FindPropertyRelative("occlusionMaskScale");

            EditorGUI.BeginDisabledGroup(!XRGraphicsConfig.tryEnable);
            EditorGUILayout.LabelField(Styles.XRSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;            
            drawRenderScale.floatValue = EditorGUILayout.Slider(Styles.renderScaleLabel, drawRenderScale.floatValue, k_MinRenderScale, k_MaxRenderScale);
            drawViewportScale.floatValue = EditorGUILayout.Slider(Styles.viewportScaleLabel, drawViewportScale.floatValue, k_MinRenderScale, k_MaxRenderScale);
            EditorGUILayout.PropertyField(drawUseOcclusionMesh, Styles.useOcclusionMeshLabel);
            EditorGUILayout.PropertyField(drawOcclusionMaskScale, Styles.occlusionScaleLabel);
            EditorGUILayout.PropertyField(drawShowDeviceView, Styles.showDeviceViewLabel);
            EditorGUILayout.PropertyField(drawGameViewRenderMode, Styles.gameViewRenderModeLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUI.EndDisabledGroup();
        }
    }
}
