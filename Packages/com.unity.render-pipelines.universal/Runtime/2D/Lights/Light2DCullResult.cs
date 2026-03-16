using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal
{
    internal struct LightStats
    {
        public int totalLights;
        public int totalShadowLights;
        public int totalShadows;
        public int totalNormalMapUsage;
        public int totalVolumetricUsage;
        public int totalVolumetricShadowUsage;
        public uint blendStylesUsed;
        public uint blendStylesWithLights;

        public bool useLights { get { return totalLights > 0; } }
        public bool useShadows { get { return totalShadows > 0; } }
        public bool useVolumetricLights { get { return totalVolumetricUsage > 0; } }
        public bool useVolumetricShadowLights { get { return totalVolumetricShadowUsage > 0; } }
        public bool useNormalMap { get { return totalNormalMapUsage > 0; } }
    }

    internal interface ILight2DCullResult
    {
        List<Light2D> visibleLights { get; }
        HashSet<ShadowCasterGroup2D> visibleShadows { get; }
        LightStats GetLightStatsByLayer(int layerID, ref LayerBatch layer);
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
        private HashSet<ShadowCasterGroup2D> m_VisibleShadows = new HashSet<ShadowCasterGroup2D>();
        public HashSet<ShadowCasterGroup2D> visibleShadows => m_VisibleShadows;

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

        public LightStats GetLightStatsByLayer(int layerID, ref LayerBatch layer)
        {
            layer.lights.Clear();
            layer.shadowIndices.Clear();
            layer.shadowCasters.Clear();
            var returnStats = new LightStats();

            foreach (var light in visibleLights)
            {
                if (!light.IsLitLayer(layerID))
                    continue;

                if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled)
                    returnStats.totalNormalMapUsage++;
                if (light.volumeIntensity > 0 && light.volumetricEnabled)
                    returnStats.totalVolumetricUsage++;
                if (light.volumeIntensity > 0 && light.volumetricEnabled && RendererLighting.CanCastShadows(light, layerID))
                    returnStats.totalVolumetricShadowUsage++;

                returnStats.blendStylesUsed |= (uint)(1 << light.blendStyleIndex);
                if (light.lightType != Light2D.LightType.Global)
                    returnStats.blendStylesWithLights |= (uint)(1 << light.blendStyleIndex);

                // Check if layer has shadows
                bool isShadowed = false;
                if (RendererLighting.CanCastShadows(light, layerID))
                {
                    foreach (var group in visibleShadows)
                    {
                        var shadowCasters = group.GetShadowCasters();
                        if (shadowCasters != null)
                        {
                            foreach (var shadowCaster in shadowCasters)
                            {
                                if (shadowCaster.IsLit(light) && shadowCaster.IsShadowedLayer(layerID))
                                {
                                    isShadowed = true;
                                    returnStats.totalShadows++;

                                    if (!layer.shadowCasters.Contains(group))
                                        layer.shadowCasters.Add(group);
                                }
                            }
                        }
                    }
                }

                if (isShadowed)
                {
                    returnStats.totalShadowLights++;
                    layer.shadowIndices.Add(layer.lights.Count);
                }

                returnStats.totalLights++;
                layer.lights.Add(light);
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
                foreach (var group in ShadowCasterGroup2DManager.shadowCasterGroups)
                {
                    var shadowCasters = group.GetShadowCasters();
                    if (shadowCasters != null)
                    {
                        foreach (var shadowCaster in shadowCasters)
                        {
                            // Cull against visible lights in the scene
                            foreach (var light in m_VisibleLights)
                            {
                                if (shadowCaster.IsLit(light) && !m_VisibleShadows.Contains(group))
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
