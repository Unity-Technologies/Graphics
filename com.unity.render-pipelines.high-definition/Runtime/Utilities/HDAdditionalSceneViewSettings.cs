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
#if UNITY_2020_2_OR_NEWER
            public static readonly string HelpBox = "Temporal Anti - aliasing in the Scene View is only supported when Always Refresh is enabled.";
#else
            public static readonly string HelpBox = "Temporal Anti - aliasing in the Scene View is only supported when Animated Materials are enabled.";
#endif
        }

        static readonly string k_SceneViewAntialiasingKey = "HDRP:SceneViewCamera:Antialiasing";
        static AntialiasingMode s_SceneViewAntialiasing = AntialiasingMode.None;

        public static AntialiasingMode sceneViewAntialiasing
        {
            // We update the Editor prefs only when writing. Reading goes through the cached local var to ensure that reads have no overhead.
            get => s_SceneViewAntialiasing;
            set
            {
                s_SceneViewAntialiasing = value;
                SetPref<AntialiasingMode>(k_SceneViewAntialiasingKey, value);
            }
        }

        static readonly string k_SceneViewStopNaNsKey = "HDRP:SceneViewCamera:StopNaNs";
        static bool s_SceneViewStopNaNs = false;

        public static bool sceneViewStopNaNs
        {
            // We update the Editor prefs only when writing. Reading goes through the cached local var to ensure that reads have no overhead.
            get => s_SceneViewStopNaNs;
            set
            {
                s_SceneViewStopNaNs = value;
                SetPref<bool>(k_SceneViewStopNaNsKey, value);
            } 
        }

        static HDAdditionalSceneViewSettings()
        {
            SceneViewCameraWindow.additionalSettingsGui += DoAdditionalSettings;
            sceneViewAntialiasing = GetOrCreatePref<AntialiasingMode>(k_SceneViewAntialiasingKey, AntialiasingMode.None);
            sceneViewStopNaNs = GetOrCreatePref<bool>(k_SceneViewStopNaNsKey, false);
        }

        static void DoAdditionalSettings(SceneView sceneView)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("HD Render Pipeline", EditorStyles.boldLabel);

            sceneViewAntialiasing = (AntialiasingMode)EditorGUILayout.EnumPopup(Styles.AAMode, sceneViewAntialiasing);
            if (sceneViewAntialiasing == AntialiasingMode.TemporalAntialiasing)
                EditorGUILayout.HelpBox(Styles.HelpBox, MessageType.Info);

            sceneViewStopNaNs = EditorGUILayout.Toggle(Styles.StopNaNs, sceneViewStopNaNs);
        }

        static T GetOrCreatePref<T>(string key, T defaultValue)
        {
            if (EditorPrefs.HasKey(key))
            {
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)EditorPrefs.GetBool(key);
                }
                return (T)(object)EditorPrefs.GetInt(key);
            }
            else
            {
                if (typeof(T) == typeof(bool))
                {
                    EditorPrefs.SetBool(key, (bool)(object)defaultValue);
                }
                else
                {
                    EditorPrefs.SetInt(key, (int)(object)defaultValue);
                }
                return defaultValue;
            }
        }

        static void SetPref<T>(string key, T value )
        {
            if (typeof(T) == typeof(bool))
                EditorPrefs.SetBool(key, (bool)(object)value);
            else
                EditorPrefs.SetInt(key, (int)(object)value);
        }
    }
#endif
}
