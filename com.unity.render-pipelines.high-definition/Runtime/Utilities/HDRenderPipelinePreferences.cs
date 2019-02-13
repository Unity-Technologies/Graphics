using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Rendering
{
    // This file can't be in the editor assembly as we need to access it in runtime-editor-specific
    // places like OnGizmo etc and we don't want to add the editor assembly as a dependency of the
    // runtime one

#if UNITY_EDITOR
    using UnityEditor;
    using AntialiasingMode = HDAdditionalCameraData.AntialiasingMode;

    public static class HDRenderPipelinePreferences
    {
        static bool m_Loaded = false;

        static AntialiasingMode s_SceneViewAntialiasing = AntialiasingMode.None;
        public static AntialiasingMode sceneViewAntialiasing
        {
            get => s_SceneViewAntialiasing;
            set
            {
                if (s_SceneViewAntialiasing == value) return;
                s_SceneViewAntialiasing = value;
                EditorPrefs.SetInt(Keys.sceneViewAntialiasing, (int)s_SceneViewAntialiasing);
            }
        }

        static class Keys
        {
            internal const string sceneViewAntialiasing = "HDRP.SceneView.Antialiasing";
        }

        [SettingsProvider]
        static SettingsProvider PreferenceGUI()
        {
            return new SettingsProvider("Preferences/HD Render Pipeline", SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    if (!m_Loaded)
                        Load();

                    sceneViewAntialiasing = (AntialiasingMode)EditorGUILayout.EnumPopup("Scene View Anti-aliasing", sceneViewAntialiasing);

                    if (sceneViewAntialiasing == AntialiasingMode.TemporalAntialiasing)
                        EditorGUILayout.HelpBox("Temporal Anti-aliasing in the Scene View is only supported when Animated Materials are enabled.", MessageType.Info);
                }
            };
        }

        static HDRenderPipelinePreferences()
        {
            Load();
        }

        static void Load()
        {
            s_SceneViewAntialiasing = (AntialiasingMode)EditorPrefs.GetInt(Keys.sceneViewAntialiasing, (int)AntialiasingMode.None);

            m_Loaded = true;
        }
    }
#endif
}
