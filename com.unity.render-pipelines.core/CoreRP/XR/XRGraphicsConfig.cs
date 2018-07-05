using System;
using UnityEditor;
using UnityEngine.XR;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public struct XRGraphicsConfig
    { // XRGConfig stores the desired XR settings for a given SRP asset.

        public float renderScale;
        public float viewportScale;
        public bool useOcclusionMesh;
        public float occlusionMaskScale;
        public bool showDeviceView;
        public GameViewRenderMode gameViewRenderMode;

        public void SetConfig()
        { // If XR is enabled, sets XRSettings from our saved config
            if (!enabled)
                Assert.IsFalse(enabled);
            XRSettings.eyeTextureResolutionScale = renderScale;
            XRSettings.renderViewportScale = viewportScale;
            XRSettings.useOcclusionMesh = useOcclusionMesh;
            XRSettings.occlusionMaskScale = occlusionMaskScale;
            XRSettings.showDeviceView = showDeviceView;
            XRSettings.gameViewRenderMode = gameViewRenderMode;
        }
        public void SetViewportScale(float viewportScale)
        { // Only sets viewport- since this is probably the only thing getting updated every frame
            if (!enabled)
                Assert.IsFalse(enabled);
            XRSettings.renderViewportScale = viewportScale;
        }

        public static readonly XRGraphicsConfig s_DefaultXRConfig = new XRGraphicsConfig
        {
            renderScale = 1.0f,
            viewportScale = 1.0f,
            useOcclusionMesh = true,
            occlusionMaskScale = 1.0f,
            showDeviceView = true,
            gameViewRenderMode = GameViewRenderMode.BothEyes
        };

        public static XRGraphicsConfig GetActualXRSettings()
        {
            XRGraphicsConfig getXRSettings = new XRGraphicsConfig();
            
            if (!enabled)
                Assert.IsFalse(enabled);
            
            getXRSettings.renderScale = XRSettings.eyeTextureResolutionScale;
            getXRSettings.viewportScale = XRSettings.renderViewportScale;
            getXRSettings.useOcclusionMesh = XRSettings.useOcclusionMesh;
            getXRSettings.occlusionMaskScale = XRSettings.occlusionMaskScale;
            getXRSettings.showDeviceView = XRSettings.showDeviceView;
            getXRSettings.gameViewRenderMode = XRSettings.gameViewRenderMode;            
            return getXRSettings;
        }

        public static bool tryEnable
        { // TryEnable gets updated before "play" is pressed- we use this for updating GUI. 
            get { return PlayerSettings.virtualRealitySupported; }
        }

        public static bool enabled
        { // SRP should use this to safely determine whether XR is enabled at runtime.
            get
            {
#if ENABLE_VR
                return XRSettings.enabled;
#else
                return false;
#endif
            }
        }

        public static RenderTextureDescriptor eyeTextureDesc
        {
            get
            {
                if (!enabled)
                    Assert.IsFalse(enabled);
                return XRSettings.eyeTextureDesc;
            }
        }

        public static string[] supportedDevices
        {
            get
            {
                if (!enabled)
                    Assert.IsFalse(enabled);
                return XRSettings.supportedDevices;
            }
        }
    }

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
