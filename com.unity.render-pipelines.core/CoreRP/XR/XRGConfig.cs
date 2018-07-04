using UnityEngine.XR;
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public struct XRGConfig
    {
        public void SetConfig()
        { // If XR is enabled, sets XRSettings from our saved config
            if (!XRSettings.enabled)
                return;
            XRSettings.eyeTextureResolutionScale = renderScale;
            XRSettings.renderViewportScale = viewportScale;
            XRSettings.useOcclusionMesh = useOcclusionMesh;
            XRSettings.occlusionMaskScale = occlusionMaskScale;
            XRSettings.showDeviceView = showDeviceView;
            XRSettings.gameViewRenderMode = gameViewRenderMode;
        }

        public float renderScale;
        public float viewportScale;
        public bool useOcclusionMesh;
        public float occlusionMaskScale;
        public bool showDeviceView;
        public GameViewRenderMode gameViewRenderMode;

        public static readonly XRGConfig defaultXRConfig = new XRGConfig
        {
            renderScale = 1.0f,
            viewportScale = 1.0f,
            useOcclusionMesh = true,
            occlusionMaskScale = 1.0f,
            showDeviceView = true,
            gameViewRenderMode = GameViewRenderMode.BothEyes
        };
        public static bool Enabled
        {
            get
            {
                return XRSettings.enabled;
            }
        }
        public static StereoRenderingPath StereoRenderingMode
        {
            get
            {
                return (StereoRenderingPath)XRSettings.stereoRenderingMode;
            }
        }

        public RenderTextureDescriptor EyeTextureDesc
        {
            get
            {
                return XRSettings.eyeTextureDesc;
            }
        }
    }

    [CustomPropertyDrawer(typeof(XRGConfig))]
    public class XRGConfigDrawer : PropertyDrawer
    {
        private float k_MinRenderScale = 0.01f;
        private float k_MaxRenderScale = 4.0f;
        internal class Styles
        {
            public static GUIContent XRSettingsLabel = new GUIContent("XR Settings");
            public static GUIContent XREnabledLabel = new GUIContent("Enable XR", "Enables stereo rendering");
            public static GUIContent renderScaleLabel = new GUIContent("Render Scale", "Scales (and reallocates) the camera render target allowing the game to render at a resolution different than native resolution. UI is always rendered at native resolution.");
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
            var m_RenderScale = property.FindPropertyRelative("renderScale");
            var m_ViewportScale = property.FindPropertyRelative("viewportScale");
            var m_ShowDeviceView = property.FindPropertyRelative("showDeviceView");
            var m_GameViewRenderMode = property.FindPropertyRelative("gameViewRenderMode");
            var m_UseOcclusionMesh = property.FindPropertyRelative("useOcclusionMesh");
            var m_OcclusionMaskScale = property.FindPropertyRelative("occlusionMaskScale");
            
            EditorGUILayout.LabelField(Styles.XRSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            m_RenderScale.floatValue = EditorGUILayout.Slider(Styles.renderScaleLabel, m_RenderScale.floatValue, k_MinRenderScale, k_MaxRenderScale);
            m_ViewportScale.floatValue = EditorGUILayout.Slider(Styles.viewportScaleLabel, m_ViewportScale.floatValue, k_MinRenderScale, k_MaxRenderScale);
            EditorGUILayout.PropertyField(m_UseOcclusionMesh, Styles.useOcclusionMeshLabel);
            EditorGUILayout.PropertyField(m_OcclusionMaskScale, Styles.occlusionScaleLabel);
            EditorGUILayout.PropertyField(m_ShowDeviceView, Styles.showDeviceViewLabel);
            EditorGUILayout.PropertyField(m_GameViewRenderMode, Styles.gameViewRenderModeLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }
}
