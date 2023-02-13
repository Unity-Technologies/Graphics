using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.Universal
{
    internal static class Light2DManager
    {
        private static SortingLayer[] s_SortingLayers;

        public static List<Light2D> lights { get; } = new List<Light2D>();

        // Called during OnEnable
        public static void RegisterLight(Light2D light)
        {
            Debug.Assert(!lights.Contains(light));
            lights.Add(light);
            ErrorIfDuplicateGlobalLight(light);
        }

        // Called during OnEnable
        public static void DeregisterLight(Light2D light)
        {
            Debug.Assert(lights.Contains(light));
            lights.Remove(light);
        }

        public static void ErrorIfDuplicateGlobalLight(Light2D light)
        {
            if (light.lightType != Light2D.LightType.Global)
                return;

            foreach (var sortingLayer in light.affectedSortingLayers)
            {
                // should this really trigger at runtime?
                if (ContainsDuplicateGlobalLight(sortingLayer, light.blendStyleIndex))
                    Debug.LogError("More than one global light on layer " + SortingLayer.IDToName(sortingLayer) + " for light blend style index " + light.blendStyleIndex);
            }
        }

        public static bool GetGlobalColor(int sortingLayerIndex, int blendStyleIndex, out Color color)
        {
            var foundGlobalColor = false;
            color = Color.black;

            // This should be rewritten to search only global lights
            foreach (var light in lights)
            {
                if (light.lightType != Light2D.LightType.Global ||
                    light.blendStyleIndex != blendStyleIndex ||
                    !light.IsLitLayer(sortingLayerIndex))
                    continue;

                var inCurrentPrefabStage = true;
#if UNITY_EDITOR
                // If we found the first global light in our prefab stage
                inCurrentPrefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(light.gameObject) ?? true;
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

        private static bool ContainsDuplicateGlobalLight(int sortingLayerIndex, int blendStyleIndex)
        {
            var globalLightCount = 0;

            // This should be rewritten to search only global lights
            foreach (var light in lights)
            {
                if (light.lightType == Light2D.LightType.Global &&
                    light.blendStyleIndex == blendStyleIndex &&
                    light.IsLitLayer(sortingLayerIndex))
                {
#if UNITY_EDITOR
                    // If we found the first global light in our prefab stage
                    if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(light.gameObject) == UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage())
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
    }
}
