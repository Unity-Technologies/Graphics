using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(SurfaceCacheGIRendererFeature))]
    internal class SurfaceCacheGIEditor : Editor
    {
        private const string k_UnsupportedRayTracingBackendMessage = "Surface Cache GI will not run on this device because it does not support hardware ray tracing or compute shaders.";

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
                CoreEditorUtils.DrawFixMeBox(SurfaceCacheGIRendererFeature.k_StaticBatchingErrorMesssage, surfaceCacheGIRendererFeature.isActive ? MessageType.Error : MessageType.Warning, "Open", () =>
                    PlayerSettingsInspectorUtility.OpenAndScrollTo(PlayerSettingsInspectorUtility.Section.OtherSettings, "Static Batching"));
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
