using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(SurfaceCacheGIRendererFeature))]
    internal class SurfaceCacheGIEditor : Editor
    {
        private const string k_UnsupportedRayTracingBackendMessage = "Surface Cache GI will not run on this device because it does not support hardware ray tracing or compute shaders.";

        static class Styles
        {
            public static readonly GUIContent staticBatchingError = L10n.TextContentWithIcon(SurfaceCacheGIRendererFeature.k_StaticBatchingErrorMesssage, MessageType.Error, null);
            public static readonly GUIContent staticBatchingWarning = L10n.TextContentWithIcon(SurfaceCacheGIRendererFeature.k_StaticBatchingErrorMesssage, MessageType.Warning, null);
            public static readonly GUIContent openButton = L10n.TextContent("Open", null, null, null);
        }

        private static GUIStyle s_FixMeBoxStyle;

        private static void DrawFixMeBox(GUIContent message, GUIContent buttonLabel, System.Action action)
        {
            if (s_FixMeBoxStyle == null)
                s_FixMeBoxStyle = new GUIStyle(EditorStyles.helpBox);

            float buttonWidth = Mathf.Max(60f, GUI.skin.button.CalcSize(buttonLabel).x);
            s_FixMeBoxStyle.padding.right = EditorStyles.helpBox.padding.right + Mathf.CeilToInt(buttonWidth) + 4;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(GUIContent.none, s_FixMeBoxStyle);
            Rect rect = GUILayoutUtility.GetRect(message, s_FixMeBoxStyle);
            if (Event.current.type == EventType.Repaint)
                s_FixMeBoxStyle.Draw(rect, message, false, false, false, false);
            EditorGUILayout.EndHorizontal();

            Rect buttonRect = new Rect(rect.xMax - buttonWidth - 4, rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2, buttonWidth, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonRect, buttonLabel))
                action();
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
                DrawFixMeBox(surfaceCacheGIRendererFeature.isActive ? Styles.staticBatchingError : Styles.staticBatchingWarning, Styles.openButton, () =>
                    PlayerSettingsInspectorUtility.OpenAndScrollTo(PlayerSettingsInspectorUtility.Section.OtherSettings, "Static Batching"));
            }
            else if (EditorGraphicsSettings.defaultMeshBufferTarget != DefaultMeshBufferTarget.Raw)
            {
                CoreEditorUtils.DrawFixMeBox(SurfaceCacheGIRendererFeature.k_MeshBufferTargetErrorMessage, surfaceCacheGIRendererFeature.isActive ? MessageType.Error : MessageType.Warning, "Open", () =>
                    GraphicsSettingsInspectorUtility.OpenAndScrollToElement(nameof(DefaultMeshBufferTarget)));
            }
            else if (!SurfaceCacheGIRendererFeature.HasSupportedRayTracingBackend())
            {
                EditorGUILayout.HelpBox(k_UnsupportedRayTracingBackendMessage, MessageType.Warning);
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
        }
    }
}
