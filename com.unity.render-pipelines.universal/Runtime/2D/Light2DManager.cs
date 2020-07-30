using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class Light2DManager
    {
        private static List<Light2D> m_Lights = new List<Light2D>();
        public static List<Light2D> lights => m_Lights;

        // Called during OnEnable
        public static void RegisterLight(Light2D light)
        {
            Debug.Assert(!m_Lights.Contains(light));
            m_Lights.Add(light);

        }

        // Called during OnEnable
        public static void DeregisterLight(Light2D light)
        {
            Debug.Assert(m_Lights.Contains(light));
            m_Lights.Remove(light);
        }

        public static bool GetGlobalColor(int sortingLayerIndex, int blendStyleIndex, out Color color)
        {
            var  foundGlobalColor = false;
            color = Color.black;

            // This should be rewritten to search only global lights
            foreach(var light in m_Lights)
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

        public static bool ContainsDuplicateGlobalLight(int sortingLayerIndex, int blendStyleIndex)
        {
            int globalLightCount = 0;

            // This should be rewritten to search only global lights
            foreach(var light in m_Lights)
            {
                if (light.lightType == Light2D.LightType.Global &&
                    light.blendStyleIndex == blendStyleIndex &&
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
    }
}
