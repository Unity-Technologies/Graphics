namespace UnityEngine.Rendering.HighDefinition
{
#if UNITY_EDITOR
    using UnityEditor;
    using AntialiasingMode = HDAdditionalCameraData.AntialiasingMode;

    [InitializeOnLoad]
    static class HDAdditionalSceneViewSettings
    {
        static class Styles
        {
            public static readonly GUIContent AAMode = EditorGUIUtility.TrTextContent("Camera Anti-aliasing", "The anti-alising mode that will be used in the scene view camera.");
            public static readonly GUIContent StopNaNs = EditorGUIUtility.TrTextContent("Camera Stop NaNs", "When enabled, any NaNs in the color buffer of the scene view camera will be suppressed.");
            public static readonly string HelpBox = "Temporal Anti - aliasing in the Scene View is only supported when Animated Materials are enabled.";
        }

        static AntialiasingMode s_SceneViewAntialiasing = AntialiasingMode.None;

        public static AntialiasingMode sceneViewAntialiasing
        {
            get => s_SceneViewAntialiasing;
            set => s_SceneViewAntialiasing = value;
        }

        static bool s_SceneViewStopNaNs = false;

        public static bool sceneViewStopNaNs
        {
            get => s_SceneViewStopNaNs;
            set => s_SceneViewStopNaNs = value;
        }

        static HDAdditionalSceneViewSettings()
        {
            SceneViewCameraWindow.additionalSettingsGui += DoAdditionalSettings;
        }

        static void DoAdditionalSettings(SceneView sceneView)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("HD Render Pipeline", EditorStyles.boldLabel);

            s_SceneViewAntialiasing = (AntialiasingMode)EditorGUILayout.EnumPopup(Styles.AAMode, s_SceneViewAntialiasing);
            if (s_SceneViewAntialiasing == AntialiasingMode.TemporalAntialiasing)
                EditorGUILayout.HelpBox(Styles.HelpBox, MessageType.Info);

            s_SceneViewStopNaNs = EditorGUILayout.Toggle(Styles.StopNaNs, s_SceneViewStopNaNs);
        }
    }
#endif
}
