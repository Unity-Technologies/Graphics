namespace UnityEngine.Rendering
{
    // This file can't be in the editor assembly as we need to access it in runtime-editor-specific
    // places like OnGizmo etc and we don't want to add the editor assembly as a dependency of the
    // runtime one

    // The UI layout/styling in this panel is broken and can't match the one from built-ins
    // preference panels as everything needed is internal/private (at the time of writing this
    // comment)

#if UNITY_EDITOR
    using UnityEditor;

    public static class CoreRenderPipelinePreferences
    {
        static bool m_Loaded = false;

        static Color s_VolumeGizmoColor = new Color(0.2f, 0.8f, 0.1f, 0.5f);
        static Color s_PreviewCameraBackgroundColor = new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 0.0f);
        public static Color volumeGizmoColor
        {
            get => s_VolumeGizmoColor;
            set
            {
                if (s_VolumeGizmoColor == value) return;
                s_VolumeGizmoColor = value;
                EditorPrefs.SetInt(Keys.volumeGizmoColor, (int)ColorUtils.ToHex(value));
            }
        }

        public static Color previewBackgroundColor
        {
            get => s_PreviewCameraBackgroundColor;
            set
            {
                if (s_PreviewCameraBackgroundColor == value) return;
                s_PreviewCameraBackgroundColor = value;
                EditorPrefs.SetInt(Keys.cameraBackgroundColor, (int)ColorUtils.ToHex(value));
            }
        }

        static class Keys
        {
            internal const string volumeGizmoColor = "CoreRP.Volume.GizmoColor";
            internal const string cameraBackgroundColor = "CoreRP.PreviewCamera.BackgroundColor";
        }

        [SettingsProvider]
        static SettingsProvider PreferenceGUI()
        {
            return new SettingsProvider("Preferences/Colors/SRP", SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    if (!m_Loaded)
                        Load();
                    EditorGUIUtility.labelWidth = 170;
                    volumeGizmoColor = EditorGUILayout.ColorField("Volume Gizmo Color", volumeGizmoColor);
                    previewBackgroundColor = EditorGUILayout.ColorField("Preview Background Color", previewBackgroundColor);
                }
            };
        }

        static CoreRenderPipelinePreferences()
        {
            Load();
        }

        static void Load()
        {
            s_VolumeGizmoColor = GetColor(Keys.volumeGizmoColor, new Color(0.2f, 0.8f, 0.1f, 0.5f));
            s_PreviewCameraBackgroundColor = GetColor(Keys.cameraBackgroundColor, new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 0.0f));

            m_Loaded = true;
        }

        static Color GetColor(string key, Color defaultValue)
        {
            int value = EditorPrefs.GetInt(key, (int)ColorUtils.ToHex(defaultValue));
            return ColorUtils.ToRGBA((uint)value);
        }
    }
#endif
}
