namespace UnityEngine.Rendering.HighDefinition
{
    // This file can't be in the editor assembly as we need to access it in runtime-editor-specific
    // places like OnGizmo etc and we don't want to add the editor assembly as a dependency of the
    // runtime one

#if UNITY_EDITOR
    using UnityEditor;
    using AntialiasingMode = HDAdditionalCameraData.AntialiasingMode;

    static class HDRenderPipelinePreferences
    {
        static bool m_Loaded = false;

        static bool s_MatcapMixAlbedo;
        public static bool matcapViewMixAlbedo
        {
            get => s_MatcapMixAlbedo;
            set
            {
                if (s_MatcapMixAlbedo == value) return;
                s_MatcapMixAlbedo = value;
                EditorPrefs.SetBool(Keys.matcapViewMixAlbedo, s_MatcapMixAlbedo);
            }
        }

        static float s_MatcapScale;
        public static float matcapViewScale
        {
            get => s_MatcapScale;
            set
            {
                if (s_MatcapScale == value) return;
                s_MatcapScale = value;
                EditorPrefs.SetFloat(Keys.matcapViewScale, s_MatcapScale);
            }
        }

        static class Keys
        {
            internal const string sceneViewAntialiasing = "HDRP.SceneView.Antialiasing";
            internal const string sceneViewStopNaNs = "HDRP.SceneView.StopNaNs";
            internal const string matcapViewMixAlbedo = "HDRP.SceneView.MatcapMixAlbedo";
            internal const string matcapViewScale = "HDRP.SceneView.MatcapViewScale";
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

                    matcapViewMixAlbedo = EditorGUILayout.Toggle("Mix Albedo in the Matcap", matcapViewMixAlbedo);
                    if (matcapViewMixAlbedo)
                        matcapViewScale = EditorGUILayout.FloatField("Matcap intensity scale", matcapViewScale);
                }
            };
        }

        static HDRenderPipelinePreferences()
        {
            Load();
        }

        static void Load()
        {
            s_MatcapMixAlbedo = EditorPrefs.GetBool(Keys.matcapViewMixAlbedo, true);
            s_MatcapScale = EditorPrefs.GetFloat(Keys.matcapViewScale, 1.0f);

            m_Loaded = true;
        }
    }
#endif
}
