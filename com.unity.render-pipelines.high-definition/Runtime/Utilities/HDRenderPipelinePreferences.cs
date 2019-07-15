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

        static AntialiasingMode s_SceneViewAntialiasing;
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

        static bool s_SceneViewStopNaNs;
        public static bool sceneViewStopNaNs
        {
            get => s_SceneViewStopNaNs;
            set
            {
                if (s_SceneViewStopNaNs == value) return;
                s_SceneViewStopNaNs = value;
                EditorPrefs.SetBool(Keys.sceneViewStopNaNs, s_SceneViewStopNaNs);
            }
        }

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
                EditorPrefs.SetFloat(Keys.matcapViewMixAlbedo, s_MatcapScale);
            }
        }


        static bool s_LightColorNormalization = false;
        public static bool lightColorNormalization
        {
            get => s_LightColorNormalization;
            set
            {
                if (s_LightColorNormalization == value) return;
                s_LightColorNormalization = value;
                EditorPrefs.SetBool(Keys.lightColorNormalization, s_LightColorNormalization);
            }
        }

        static bool s_MaterialEmissionColorNormalization = false;
        public static bool materialEmissionColorNormalization
        {
            get => s_MaterialEmissionColorNormalization;
            set
            {
                if (s_MaterialEmissionColorNormalization == value) return;
                s_MaterialEmissionColorNormalization = value;
                EditorPrefs.SetBool(Keys.materialEmissionColorNormalization, s_MaterialEmissionColorNormalization);
            }
        }

        static class Keys
        {
            internal const string sceneViewAntialiasing = "HDRP.SceneView.Antialiasing";
            internal const string sceneViewStopNaNs = "HDRP.SceneView.StopNaNs";
            internal const string matcapViewMixAlbedo = "HDRP.SceneView.MatcapMixAlbedo";
            internal const string matcapViewScale = "HDRP.SceneView.MatcapViewScale";
            internal const string lightColorNormalization = "HDRP.UI.LightColorNormalization";
            internal const string materialEmissionColorNormalization = "HDRP.UI.MaterialEmissionNormalization";
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

                    sceneViewStopNaNs = EditorGUILayout.Toggle("Scene View Stop NaNs", sceneViewStopNaNs);

                    matcapViewMixAlbedo = EditorGUILayout.Toggle("Mix Albedo in the Matcap", matcapViewMixAlbedo);
                    if(matcapViewMixAlbedo)
                        matcapViewScale = EditorGUILayout.FloatField("Matcap intensity scale", matcapViewScale);

                    // Disable this until we have a good solution to handle the normalized color picking
                    // EditorGUILayout.LabelField("Color Normalization");
                    // using (new EditorGUI.IndentLevelScope())
                    // {
                    //     lightColorNormalization = EditorGUILayout.Toggle("Lights", lightColorNormalization);
                    //     materialEmissionColorNormalization = EditorGUILayout.Toggle("Material Emission", materialEmissionColorNormalization);
                    // }
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
            s_SceneViewStopNaNs = EditorPrefs.GetBool(Keys.sceneViewStopNaNs, false);
            s_MatcapMixAlbedo = EditorPrefs.GetBool(Keys.matcapViewMixAlbedo, true);
            s_MatcapScale = EditorPrefs.GetFloat(Keys.matcapViewScale, 1.0f);
            s_LightColorNormalization = EditorPrefs.GetBool(Keys.lightColorNormalization, false);
            s_MaterialEmissionColorNormalization = EditorPrefs.GetBool(Keys.materialEmissionColorNormalization, false);

            m_Loaded = true;
        }
    }
#endif
}
