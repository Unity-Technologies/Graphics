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
    { // XRGConfig stores the desired XR settings for a given SRP asset. 
        public void SetConfig()
        { // If XR is enabled, sets XRSettings from our saved config
            if (!Enabled)
                return;
            XRSettings.eyeTextureResolutionScale = renderScale;
            XRSettings.renderViewportScale = viewportScale;
            XRSettings.useOcclusionMesh = useOcclusionMesh;
            XRSettings.occlusionMaskScale = occlusionMaskScale;
            XRSettings.showDeviceView = showDeviceView;
            XRSettings.gameViewRenderMode = gameViewRenderMode;
        }
        public void SetViewportScale(float viewportScale)
        { // If XR is enabled, sets XRSettings from our saved config
            if (!Enabled)
                return;
            XRSettings.renderViewportScale = viewportScale;
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

        public static XRGConfig ActualXRSettings()
        {
            XRGConfig getXRSettings = new XRGConfig();

            // Just to make it obvious if getting XR settings failed
            getXRSettings.renderScale = 0.0f;
            getXRSettings.viewportScale = 0.0f;
            if (Enabled)
            {
                getXRSettings.renderScale = XRSettings.eyeTextureResolutionScale;
                getXRSettings.viewportScale = XRSettings.renderViewportScale;
                getXRSettings.useOcclusionMesh = XRSettings.useOcclusionMesh;
                getXRSettings.occlusionMaskScale = XRSettings.occlusionMaskScale;
                getXRSettings.showDeviceView = XRSettings.showDeviceView;
                getXRSettings.gameViewRenderMode = XRSettings.gameViewRenderMode;
            }
            return getXRSettings;
        }

        public static bool Enabled
        { // SRP should use this to safely determine whether XR is enabled
            get
            {
#if ENABLE_VR 
                return XRSettings.enabled;
#else
                return false;
#endif
            }
        }

        public static RenderTextureDescriptor EyeTextureDesc
        {
            get
            {
                if (Enabled)
                    return XRSettings.eyeTextureDesc;
                return new RenderTextureDescriptor(1, 1); // Should be easy to tell if this failed
            }
        }

        public static string[] SupportedDevices
        {
            get
            {
                if (Enabled)
                    return XRSettings.supportedDevices;
                string[] empty = {"XR disabled"};
                return empty;
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
            public static GUIContent XRSettingsLabel = new GUIContent("XR Config", "Enable XR in Player Settings. Then SetConfig can be used to set this configuration to XRSettings.");
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

            EditorGUI.BeginDisabledGroup(!XRGConfig.Enabled);
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
            EditorGUI.EndDisabledGroup();
        }
    }
}
