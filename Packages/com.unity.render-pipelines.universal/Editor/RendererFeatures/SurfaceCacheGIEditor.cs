using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(SurfaceCacheGIRendererFeature))]
    internal class SurfaceCacheGIEditor : Editor
    {
        private bool m_IsInitialized;

        // Debug parameters
        private SerializedProperty _debugEnabled;
        private SerializedProperty _debugViewMode;
        private SerializedProperty _debugShowSamplePosition;

        private struct TextContent
        {
            public static GUIContent DebugEnabled = EditorGUIUtility.TrTextContent("Debug Enabled", "Enable debug visualization.");
            public static GUIContent ActiveCaches = EditorGUIUtility.TrTextContent("Active Caches", "Number of per-camera surface caches currently alive.");
            public static GUIContent WorldRenderingLayerMask = EditorGUIUtility.TrTextContent("World Rendering Layer Mask", "Combined (OR) rendering layer mask used for the shared world update this frame. This value is read only.");
            public static GUIContent DebugViewMode = EditorGUIUtility.TrTextContent("Debug View Mode", "Debug visualization mode.");
            public static GUIContent DebugShowSamplePosition = EditorGUIUtility.TrTextContent("Debug Show Sample Position", "Show sample positions in debug view.");
            public static string UnsupportedRayTracingBackend = "Surface Cache GI will not run on this device because it does not support hardware ray tracing or compute shaders.";
        }

        private void OnEnable()
        {
            EditorApplication.update += RepaintWhileDebugging;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintWhileDebugging;
        }

        private void RepaintWhileDebugging()
        {
            if (m_IsInitialized && _debugEnabled != null && _debugEnabled.boolValue)
                Repaint();
        }

        private void Init()
        {
            m_IsInitialized = true;

            SerializedProperty paramSets = serializedObject.FindProperty("_parameterSet");

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

            SurfaceCacheGIRendererFeature surfaceCacheGIRendererFeature = (SurfaceCacheGIRendererFeature)target;
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (!SurfaceCacheGISupport.IsSupportedByActiveBuildTarget(activeBuildTarget))
            {
                if (surfaceCacheGIRendererFeature.isActive)
                    EditorGUILayout.HelpBox(SurfaceCacheGISupport.k_UnsupportedErrorMessage, MessageType.Error);
                else
                    EditorGUILayout.HelpBox(SurfaceCacheGISupport.k_UnsupportedWarningMessage, MessageType.Warning);
            }

            if (PlayerSettings.GetStaticBatchingForPlatform(activeBuildTarget))
            {
                CoreEditorUtils.DrawFixMeBox(SurfaceCacheGIRendererFeature.k_StaticBatchingErrorMesssage, surfaceCacheGIRendererFeature.isActive ? MessageType.Error : MessageType.Warning, "Open", () =>
                    PlayerSettingsInspectorUtility.OpenAndScrollTo(PlayerSettingsInspectorUtility.Section.OtherSettings, "Static Batching"));
            }
            else if (!SurfaceCacheGIRendererFeature.HasSupportedRayTracingBackend())
            {
                EditorGUILayout.HelpBox(TextContent.UnsupportedRayTracingBackend, MessageType.Warning);
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

            // Debug settings
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_debugEnabled, TextContent.DebugEnabled);
            EditorGUILayout.PropertyField(_debugViewMode, TextContent.DebugViewMode);
            EditorGUILayout.PropertyField(_debugShowSamplePosition, TextContent.DebugShowSamplePosition);

            if (_debugEnabled.boolValue && surfaceCacheGIRendererFeature.IsValidForDebugging())
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField(TextContent.ActiveCaches, new GUIContent(surfaceCacheGIRendererFeature.ActiveCacheCount.ToString()));
                    EditorGUI.RenderingLayerMaskField(EditorGUILayout.GetControlRect(), TextContent.WorldRenderingLayerMask, surfaceCacheGIRendererFeature.WorldRenderingLayerMask);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
