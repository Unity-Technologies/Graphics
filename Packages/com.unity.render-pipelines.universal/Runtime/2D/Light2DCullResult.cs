using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal
{
    internal struct LightStats
    {
        public int totalLights;
        public int totalNormalMapUsage;
        public int totalVolumetricUsage;
        public uint blendStylesUsed;
        public uint blendStylesWithLights;
    }

    internal interface ILight2DCullResult
    {
        List<Light2D> visibleLights { get; }
        List<ShadowCasterGroup2D> visibleShadows { get; }
        LightStats GetLightStatsByLayer(int layer);
        bool IsSceneLit();

#if UNITY_EDITOR
        // Determine if culling result is based of game camera
        bool IsGameView();
#endif
    }

    internal class Light2DCullResult : ILight2DCullResult
    {
        private List<Light2D> m_VisibleLights = new List<Light2D>();
        public List<Light2D> visibleLights => m_VisibleLights;
        private List<ShadowCasterGroup2D> m_VisibleShadows = new List<ShadowCasterGroup2D>();
        public List<ShadowCasterGroup2D> visibleShadows => m_VisibleShadows;
#if UNITY_EDITOR
        bool m_IsGameView;
#endif

        public bool IsSceneLit()
        {
            return Light2DManager.lights.Count > 0;
        }

#if UNITY_EDITOR
        public bool IsGameView()
        {
            return m_IsGameView;
        }
#endif

        public LightStats GetLightStatsByLayer(int layer)
        {
            var returnStats = new LightStats();
            foreach (var light in visibleLights)
            {
                if (!light.IsLitLayer(layer))
                    continue;

                returnStats.totalLights++;
                if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled)
                    returnStats.totalNormalMapUsage++;
                if (light.volumeIntensity > 0)
                    returnStats.totalVolumetricUsage++;

                returnStats.blendStylesUsed |= (uint)(1 << light.blendStyleIndex);
                if (light.lightType != Light2D.LightType.Global)
                    returnStats.blendStylesWithLights |= (uint)(1 << light.blendStyleIndex);
            }

            return returnStats;
        }

        public void SetupCulling(ref ScriptableCullingParameters cullingParameters, Camera camera)
        {
#if UNITY_EDITOR
            m_IsGameView = UniversalRenderPipeline.IsGameCamera(camera);
#endif

            Profiler.BeginSample("Cull 2D Lights and Shadow Casters");
            m_VisibleLights.Clear();
            foreach (var light in Light2DManager.lights)
            {
                if ((camera.cullingMask & (1 << light.gameObject.layer)) == 0)
                    continue;

#if UNITY_EDITOR
                if (!UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(light.gameObject, camera))
                    continue;
#endif

                if (light.lightType == Light2D.LightType.Global)
                {
                    m_VisibleLights.Add(light);
                    continue;
                }

                Profiler.BeginSample("Test Planes");
                var position = light.boundingSphere.position;
                var culled = false;
                for (var i = 0; i < cullingParameters.cullingPlaneCount; ++i)
                {
                    var plane = cullingParameters.GetCullingPlane(i);
                    // most of the time is spent getting world position
                    var distance = math.dot(position, plane.normal) + plane.distance;
                    if (distance < -light.boundingSphere.radius)
                    {
                        culled = true;
                        break;
                    }
                }
                Profiler.EndSample();
                if (culled)
                    continue;

                m_VisibleLights.Add(light);
            }

            // must be sorted here because light order could change
            m_VisibleLights.Sort((l1, l2) => l1.lightOrder - l2.lightOrder);

            m_VisibleShadows.Clear();
            if (ShadowCasterGroup2DManager.shadowCasterGroups != null)
            {
                foreach(var group in ShadowCasterGroup2DManager.shadowCasterGroups)
                {
                    var shadowCasters = group.GetShadowCasters();
                    if (shadowCasters != null)
                    {
                        foreach (var shadowCaster in shadowCasters)
                        {
                            // Cull against visible lights in the scene
                            foreach (var light in m_VisibleLights)
                            {
                                if(shadowCaster.IsLit(light) && !m_VisibleShadows.Contains(group))
                                {
                                    m_VisibleShadows.Add(group);
                                    break;
                                }
                            }

                            if (m_VisibleShadows.Contains(group))
                                break;
                        }
                    }
                }
            }

            Profiler.EndSample();
        }
    }
}
