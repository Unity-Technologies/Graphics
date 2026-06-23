#if SURFACE_CACHE

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(SurfaceCacheGIRendererFeature))]
    internal class SurfaceCacheGIEditor : Editor
    {
        private bool m_IsInitialized;

        private SerializedProperty _primaryView;

        // Debug parameters
        private SerializedProperty _debugLogFocusTarget;
        private SerializedProperty _debugEnabled;
        private SerializedProperty _debugViewMode;
        private SerializedProperty _debugShowSamplePosition;

        private struct TextContent
        {
            public static GUIContent PrimaryView = EditorGUIUtility.TrTextContent("Primary View", "When multiple views are present, selects the type of view to prioritize for cascade origin.");
            public static GUIContent DebugLogFocusTarget = EditorGUIUtility.TrTextContent("Debug Focus Target", "Show the per-frame primary camera and focus position below.");
            public static GUIContent PrimaryCamera = EditorGUIUtility.TrTextContent("Primary Camera", "The camera selected to drive the cascade origin this frame.");
            public static GUIContent FocusPosition = EditorGUIUtility.TrTextContent("Focus Position", "World-space position the cascades are centered on.");
            public static GUIContent DebugEnabled = EditorGUIUtility.TrTextContent("Debug Enabled", "Enable debug visualization.");
            public static GUIContent DebugViewMode = EditorGUIUtility.TrTextContent("Debug View Mode", "Debug visualization mode.");
            public static GUIContent DebugShowSamplePosition = EditorGUIUtility.TrTextContent("Debug Show Sample Position", "Show sample positions in debug view.");
        }

        private void OnEnable()
        {
            EditorApplication.update += RepaintWhileShowingFocusTarget;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintWhileShowingFocusTarget;
        }

        private void RepaintWhileShowingFocusTarget()
        {
            if (m_IsInitialized && _debugLogFocusTarget != null && _debugLogFocusTarget.boolValue)
                Repaint();
        }

        private void Init()
        {
            m_IsInitialized = true;

            SerializedProperty paramSets = serializedObject.FindProperty("_parameterSet");

            _primaryView = paramSets.FindPropertyRelative("PrimaryView");
            _debugLogFocusTarget = paramSets.FindPropertyRelative("DebugLogFocusTarget");
            _debugEnabled = paramSets.FindPropertyRelative("DebugEnabled");
            _debugViewMode = paramSets.FindPropertyRelative("DebugViewMode");
            _debugShowSamplePosition = paramSets.FindPropertyRelative("DebugShowSamplePosition");
        }

        private static bool SceneHasSurfaceCacheGIVolume()
        {
            Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude);
            for (int i = 0; i < volumes.Length; i++)
            {
                if (volumes[i].sharedProfile != null && volumes[i].sharedProfile.Has<SurfaceCacheGIVolumeOverride>())
                    return true;
            }
            return false;
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
                Init();

            if (PlayerSettings.GetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget))
            {
                var surfaceCacheFeature = (SurfaceCacheGIRendererFeature)target;

                EditorGUILayout.HelpBox(SurfaceCacheGIRendererFeature.k_StaticBatchingErrorMesssage, surfaceCacheFeature.isActive ? MessageType.Error : MessageType.Warning);
            }
            else if (SceneView.lastActiveSceneView && !SceneView.lastActiveSceneView.sceneViewState.alwaysRefreshEnabled)
            {
                EditorGUILayout.HelpBox("Enable \"Always Refresh\" in the Scene View to see realtime updates in the Scene View.", MessageType.Info);
            }

            // Info box explaining volume-based control — only shown when no volume in the scene has the override
            if (!SceneHasSurfaceCacheGIVolume())
            {
                EditorGUILayout.HelpBox("Many Surface Cache settings are controlled via the Volume system. Add a 'Surface Cache Global Illumination' volume override to your scene to adjust these settings per-scene.", MessageType.Info);
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_primaryView, TextContent.PrimaryView);

            EditorGUILayout.Space();

            // Debug settings
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_debugEnabled, TextContent.DebugEnabled);
            EditorGUILayout.PropertyField(_debugViewMode, TextContent.DebugViewMode);
            EditorGUILayout.PropertyField(_debugShowSamplePosition, TextContent.DebugShowSamplePosition);
            EditorGUILayout.PropertyField(_debugLogFocusTarget, TextContent.DebugLogFocusTarget);

            if (_debugLogFocusTarget.boolValue)
            {
                var feature = (SurfaceCacheGIRendererFeature)target;
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField(TextContent.PrimaryCamera, new GUIContent(string.IsNullOrEmpty(feature.debugPrimaryCameraName) ? "—" : feature.debugPrimaryCameraName));
                    EditorGUILayout.LabelField(TextContent.FocusPosition, new GUIContent(feature.debugFocusPos.ToString("F2")));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif
