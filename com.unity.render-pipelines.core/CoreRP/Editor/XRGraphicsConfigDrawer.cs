using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    [CustomPropertyDrawer(typeof(XRGraphicsConfig))]
    public class XRGraphicsConfigDrawer : PropertyDrawer
    {
        internal class Styles
        {
            public static GUIContent XRSettingsLabel = new GUIContent("XR Config", "Enable XR in Player Settings. Then SetConfig can be used to set this configuration to XRSettings.");
            public static GUIContent stereoRenderModeLabel = new GUIContent("Stereo Rendering Mode", "Use Player Settings to select between supported stereo rendering paths for current VR device.");
            public static GUIContent showDeviceViewLabel = new GUIContent("Show Device View", "If possible, mirror the render target of the VR device to the main display.");
            public static GUIContent gameViewRenderModeLabel = new GUIContent("Game View Render Mode", "Select how to reflect stereo display to game view");
            public static GUIContent useOcclusionMeshLabel = new GUIContent("Use Occlusion Mesh", "Determines whether or not to draw the occlusion mesh (goggles-shaped overlay) when rendering");
            public static GUIContent occlusionScaleLabel = new GUIContent("Occlusion Mesh Scale", "Scales the occlusion mesh");

        }
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var drawShowDeviceView = property.FindPropertyRelative("showDeviceView");
            var drawGameViewRenderMode = property.FindPropertyRelative("gameViewRenderMode");
            var drawUseOcclusionMesh = property.FindPropertyRelative("useOcclusionMesh");
            var drawOcclusionMaskScale = property.FindPropertyRelative("occlusionMaskScale");

            EditorGUI.BeginDisabledGroup(!XRGraphicsConfig.tryEnable);
            EditorGUILayout.LabelField(Styles.XRSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;            
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
