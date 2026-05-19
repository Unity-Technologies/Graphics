using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace UnityEngine.Rendering.Universal
{
    internal static class Light2DManager
    {
        private static SortingLayer[] s_SortingLayers;

        static List<Light2D> s_Lights = new List<Light2D>();    // Scene/Game/Prefab view
#if UNITY_EDITOR
        static Dictionary<SceneHandle, List<Light2D>> s_LightsEditorOnly = new Dictionary<SceneHandle, List<Light2D>>();    // Preview view
#endif

        // GetLights return different list depending on the context of the GameObject requesting (Camera or Light2D)
        // We determine if the GameObject is part of the preview scene through IsPreviewSceneObject
        // Scene, Game, Prefab view uses s_Lights
        // Preview view uses s_LightsEditorOnly
        static List<Light2D> GetOrCreateLights(GameObject obj)
        {
#if UNITY_EDITOR
            if (IsPreview(obj, out var key))
            {
                if (!s_LightsEditorOnly.TryGetValue(key, out var list))
                {
                    list = new List<Light2D>();
                    s_LightsEditorOnly.Add(key, list);
                }

                return s_LightsEditorOnly[key];
            }
#endif

            return s_Lights;
        }

        internal static bool TryGetLights(GameObject obj, out List<Light2D> lights)
        {
#if UNITY_EDITOR
            if (IsPreview(obj, out var key))
                return s_LightsEditorOnly.TryGetValue(key, out lights);
#endif

            lights = s_Lights;
            return true;
        }

        internal static void RemoveLight(Light2D light)
        {
#if UNITY_EDITOR
            if (IsPreview(light.gameObject, out var key))
            {
                if (s_LightsEditorOnly.TryGetValue(key, out var list))
                {
                    Debug.Assert(list.Contains(light));
                    list.Remove(light);

                    if (list.Count == 0)
                        s_LightsEditorOnly.Remove(key);

                    return;
                }
            }
#endif

            Debug.Assert(s_Lights.Contains(light));
            s_Lights.Remove(light);
        }

#if UNITY_EDITOR
        static bool IsPreview(GameObject obj, out SceneHandle handle)
        {
            handle = obj.scene.handle;
            var isPreview = EditorSceneManager.IsPreviewSceneObject(obj);

            // Prefab scene 
            var prefabStage = PrefabStageUtility.GetPrefabStage(obj);
            if (prefabStage != null && isPreview)
            {
                if (obj.TryGetComponent<Camera>(out var cam))
                    isPreview = cam.cameraType == CameraType.Preview;
                else
                    isPreview = false;
            }

            return isPreview;
        }
#endif

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnLoad()
        {
            s_SortingLayers = null;
            s_Lights.Clear();
            s_LightsEditorOnly.Clear();
        }
#endif

        internal static void Initialize()
        {
#if UNITY_EDITOR
            SortingLayer.onLayerChanged += OnSortingLayerChanged;
#endif
        }

        internal static void Dispose()
        {
#if UNITY_EDITOR
            SortingLayer.onLayerChanged -= OnSortingLayerChanged;
#endif
        }

        // Called during OnEnable
        public static void RegisterLight(Light2D light)
        {
            var lights = GetOrCreateLights(light.gameObject);

            Debug.Assert(!lights.Contains(light));
            lights.Add(light);

            ErrorIfDuplicateGlobalLight(light);
        }

        // Called during OnEnable
        public static void DeregisterLight(Light2D light)
        {
            RemoveLight(light);
        }

        public static void ErrorIfDuplicateGlobalLight(Light2D light)
        {
            if (light.lightType != Light2D.LightType.Global)
                return;

            foreach (var sortingLayer in light.targetSortingLayers)
            {
                // should this really trigger at runtime?
                if (ContainsDuplicateGlobalLight(sortingLayer, light))
                    Debug.LogError("More than one global light on layer " + SortingLayer.IDToName(sortingLayer) + " for light blend style index " + light.blendStyleIndex);
            }
        }

        public static bool GetGlobalColor(int sortingLayerIndex, int blendStyleIndex, Camera cam, out Color color)
        {
            var foundGlobalColor = false;
            color = Color.black;

            // This should be rewritten to search only global lights
            foreach (var light in GetOrCreateLights(cam.gameObject))
            {
                if (light.lightType != Light2D.LightType.Global ||
                    light.blendStyleIndex != blendStyleIndex ||
                    !light.IsLitLayer(sortingLayerIndex))
                    continue;

                var inCurrentPrefabStage = true;
#if UNITY_EDITOR
                // If we found the first global light in our prefab stage
                inCurrentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(light.gameObject) ?? true;
#endif

                if (inCurrentPrefabStage)
                {
                    color = light.color * light.intensity;
                    return true;
                }
                else
                {
                    if (!foundGlobalColor)
                    {
                        color = light.color * light.intensity;
                        foundGlobalColor = true;
                    }
                }
            }

            return foundGlobalColor;
        }

        private static bool ContainsDuplicateGlobalLight(int sortingLayerIndex, Light2D inLight)
        {
            var globalLightCount = 0;

            // This should be rewritten to search only global lights
            foreach (var light in GetOrCreateLights(inLight.gameObject))
            {
                if (light.lightType == Light2D.LightType.Global &&
                    light.blendStyleIndex == inLight.blendStyleIndex &&
                    light.IsLitLayer(sortingLayerIndex))
                {
#if UNITY_EDITOR
                    // If we found the first global light in our prefab stage
                    if (PrefabStageUtility.GetPrefabStage(light.gameObject) == PrefabStageUtility.GetCurrentPrefabStage())
#endif
                    {
                        if (globalLightCount > 0)
                            return true;

                        globalLightCount++;
                    }
                }
            }

            return false;
        }

        public static SortingLayer[] GetCachedSortingLayer()
        {
            if (s_SortingLayers is null)
                s_SortingLayers = SortingLayer.layers;

            return s_SortingLayers;
        }

#if UNITY_EDITOR
        internal static void OnSortingLayerChanged()
        {
            // Update sorting layers that were added or removed or changed order
             s_SortingLayers = SortingLayer.layers;
        }
#endif
    }
}
