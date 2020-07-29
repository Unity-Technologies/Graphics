using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct LightStats
    {
        public int totalLights;
        public int totalNormalMapUsage;
        public int totalVolumetricUsage;
        public uint blendStylesUsed;
    }

    internal interface ILight2DCullResult
    {
        List<Light2D> visibleLights { get; }
        LightStats GetLightStatsByLayer(int layer);
        bool IsSceneLit();
    }
    internal class Light2DCullResult : ILight2DCullResult
    {
        private List<Light2D> m_VisibleLights = new List<Light2D>();
        public List<Light2D> visibleLights => m_VisibleLights;

        public bool IsSceneLit()
        {
            if (visibleLights.Count > 0)
                return true;

            foreach (var light in LightManager2D.lights)
            {
                if (light.lightType == Light2D.LightType.Global)
                    return true;
            }

            return false;
        }
        public LightStats GetLightStatsByLayer(int layer)
        {
            var returnStats = new LightStats();
            foreach (var light in visibleLights)
            {
                if (!light.IsLitLayer(layer))
                    continue;

                returnStats.totalLights++;
                if (light.useNormalMap)
                    returnStats.totalNormalMapUsage++;
                if (light.volumeOpacity > 0)
                    returnStats.totalVolumetricUsage++;

                returnStats.blendStylesUsed |= (uint)(1 << light.blendStyleIndex);;
            }
            return returnStats;
        }

        public void SetupCulling(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            Profiler.BeginSample("Cull 2D Lights");
            m_VisibleLights.Clear();
            foreach (var light in LightManager2D.lights)
            {
                if ((cameraData.camera.cullingMask & (1 << light.gameObject.layer)) == 0)
                    continue;

#if UNITY_EDITOR
                if (!UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(light.gameObject, cameraData.camera))
                    continue;
#endif

                var culled = false;
                var sphere = light.GetBoundingSphere();
                for (var i = 0; i < cullingParameters.cullingPlaneCount; ++i)
                {
                    var plane = cullingParameters.GetCullingPlane(i);
                    var distance = math.dot(sphere.position, plane.normal) + plane.distance;
                    if (distance < -sphere.radius)
                    {
                        culled = true;
                        break;
                    }
                }
                if (culled)
                    continue;

                m_VisibleLights.Add(light);
            }

            // must be sorted here because light order could change
            m_VisibleLights.Sort((l1, l2) => l1.lightOrder - l2.lightOrder);
            Profiler.EndSample();
        }
    }

    internal static class LightManager2D
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
