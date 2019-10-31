using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    internal enum RayTracingRendererFlag
    {
        Opaque = 0x01,
        Transparent = 0x02,
        CastShadow = 0x04,
        AmbientOcclusion = 0x08,
        Reflection = 0x10,
        GlobalIllumination = 0x20,
        RecursiveRendering = 0x40,
        PathTracing = 0x80
    }

    class HDRayTracingLights
    {
        // The list of non-directional lights in the sub-scene
        public List<HDAdditionalLightData> hdPointLightArray = new List<HDAdditionalLightData>();
        public List<HDAdditionalLightData> hdLineLightArray = new List<HDAdditionalLightData>();
        public List<HDAdditionalLightData> hdRectLightArray = new List<HDAdditionalLightData>();
        public List<HDAdditionalLightData> hdLightArray = new List<HDAdditionalLightData>();

        // The list of directional lights in the sub-scene
        public List<HDAdditionalLightData> hdDirectionalLightArray = new List<HDAdditionalLightData>();

        // The list of reflection probes
        public List<HDProbe> reflectionProbeArray = new List<HDProbe>();

        // Counter of the current number of lights
        public int lightCount;
    }

    public partial class HDRenderPipeline
    {
        // Data used for runtime evaluation
        RayTracingAccelerationStructure m_CurrentRAS = new RayTracingAccelerationStructure();
        HDRaytracingLightCluster m_RayTracingLightCluster = new HDRaytracingLightCluster();
        HDRayTracingLights m_RayTracingLights = new HDRayTracingLights();
        bool m_ValidRayTracingState = false;
        bool m_ValidRayTracingCluster = false;

        // Denoisers
        HDTemporalFilter m_TemporalFilter = new HDTemporalFilter();
        HDSimpleDenoiser m_SimpleDenoiser = new HDSimpleDenoiser();
        HDDiffuseDenoiser m_DiffuseDenoiser = new HDDiffuseDenoiser();

        // Ray-count manager data
        RayCountManager m_RayCountManager = new RayCountManager();

        const int maxNumSubMeshes = 32;
        Dictionary<int, int> m_RayTracingRendererReference = new Dictionary<int, int>();
        bool[] subMeshFlagArray = new bool[maxNumSubMeshes];
        bool[] subMeshCutoffArray = new bool[maxNumSubMeshes];
        ReflectionProbe reflectionProbe = new ReflectionProbe();
        List<Material> materialArray = new List<Material>(maxNumSubMeshes);

        public void InitRayTracingManager()
        {
            // Init the denoisers
            m_TemporalFilter.Init(m_Asset.renderPipelineRayTracingResources, m_SharedRTManager);
            m_SimpleDenoiser.Init(m_Asset.renderPipelineRayTracingResources, m_SharedRTManager);
            m_DiffuseDenoiser.Init(m_Asset.renderPipelineResources, m_Asset.renderPipelineRayTracingResources, m_SharedRTManager);

            // Init the ray count manager
            m_RayCountManager.Init(m_Asset.renderPipelineRayTracingResources);

            // Build the light cluster
            m_RayTracingLightCluster.Initialize(this);
        }

        public void ReleaseRayTracingManager()
        {
            m_RayTracingLightCluster.ReleaseResources();
            m_TemporalFilter.Release();
            m_SimpleDenoiser.Release();
            m_DiffuseDenoiser.Release();
            m_RayCountManager.Release();
        }

        void AddInstanceToRAS(Renderer currentRenderer,
            bool rayTracedShadow,
            bool aoEnabled, int aoLayerValue,
            bool reflEnabled, int reflLayerValue,
            bool giEnabled, int giLayerValue,
            bool recursiveEnabled, int rrLayerValue,
            bool pathTracingEnabled, int ptLayerValue)
        {
            currentRenderer.GetSharedMaterials(materialArray);
            if (materialArray != null)
            {
                // For every sub-mesh/sub-material let's build the right flags
                int numSubMeshes = materialArray.Count;

                // Get the layer of this object
                int objectLayerValue = 1 << currentRenderer.gameObject.layer;

                // We need to build the instance flag for this renderer
                uint instanceFlag = 0x00;

                bool singleSided = false;
                bool materialIsTransparent = false;

                for (int meshIdx = 0; meshIdx < numSubMeshes; ++meshIdx)
                {
                    Material currentMaterial = materialArray[meshIdx];
                    // The material is transparent if either it has the requested keyword or is in the transparent queue range
                    if (currentMaterial != null)
                    {
                        subMeshFlagArray[meshIdx] = true;

                        // Is the material transparent?
                        materialIsTransparent |= currentMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                        || (HDRenderQueue.k_RenderQueue_Transparent.lowerBound <= currentMaterial.renderQueue
                        && HDRenderQueue.k_RenderQueue_Transparent.upperBound >= currentMaterial.renderQueue)
                        || (HDRenderQueue.k_RenderQueue_AllTransparentRaytracing.lowerBound <= currentMaterial.renderQueue
                        && HDRenderQueue.k_RenderQueue_AllTransparentRaytracing.upperBound >= currentMaterial.renderQueue);

                        // Is the material alpha tested?
                        subMeshCutoffArray[meshIdx] = currentMaterial.IsKeywordEnabled("_ALPHATEST_ON")
                        || (HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.lowerBound <= currentMaterial.renderQueue
                        && HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.upperBound >= currentMaterial.renderQueue);

                        // Force it to be non single sided if it has the keyword if there is a reason
                        bool doubleSided = currentMaterial.doubleSidedGI || currentMaterial.IsKeywordEnabled("_DOUBLESIDED_ON");
                        singleSided |= !doubleSided;
                    }
                    else
                    {
                        subMeshFlagArray[meshIdx] = false;
                        subMeshCutoffArray[meshIdx] = false;
                        singleSided = true;
                    }
                }

                // Propagate the right mask
                instanceFlag |= materialIsTransparent ? (uint)(1 << 1) : (uint)(1 << 0);

                if (rayTracedShadow)
                {
                    // Raise the shadow casting flag if needed
                    instanceFlag |= ((currentRenderer.shadowCastingMode == ShadowCastingMode.On) ? (uint)(RayTracingRendererFlag.CastShadow) : 0x00);
                }

                if (aoEnabled && !materialIsTransparent)
                {
                    // Raise the Ambient Occlusion flag if needed
                    instanceFlag |= ((aoLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.AmbientOcclusion) : 0x00;
                }

                if (reflEnabled && !materialIsTransparent)
                {
                    // Raise the Screen Space Reflection if needed
                    instanceFlag |= ((reflLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.Reflection) : 0x00;
                }

                if (giEnabled && !materialIsTransparent)
                {
                    // Raise the Global Illumination if needed
                    instanceFlag |= ((giLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.GlobalIllumination) : 0x00;
                }

                if (recursiveEnabled)
                {
                    // Raise the Global Illumination if needed
                    instanceFlag |= ((rrLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.RecursiveRendering) : 0x00;
                }

                if (pathTracingEnabled)
                {
                    // Raise the Global Illumination if needed
                    instanceFlag |= ((ptLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.PathTracing) : 0x00;
                }

                if (instanceFlag == 0) return;

                // Add it to the acceleration structure
                m_CurrentRAS.AddInstance(currentRenderer, subMeshMask: subMeshFlagArray, subMeshTransparencyFlags: subMeshCutoffArray, enableTriangleCulling: singleSided, mask: instanceFlag);
            }
        }
        public void BuildRayTracingAccelerationStructure()
        {
            // Clear all the per frame-data
            m_RayTracingRendererReference.Clear();
            m_RayTracingLights.hdDirectionalLightArray.Clear();
            m_RayTracingLights.hdPointLightArray.Clear();
            m_RayTracingLights.hdLineLightArray.Clear();
            m_RayTracingLights.hdRectLightArray.Clear();
            m_RayTracingLights.hdLightArray.Clear();
            m_RayTracingLights.reflectionProbeArray.Clear();
            m_RayTracingLights.lightCount = 0;
            m_CurrentRAS.Dispose();
            m_CurrentRAS = new RayTracingAccelerationStructure();
            m_ValidRayTracingState = false;
            m_ValidRayTracingCluster = false;

            bool rayTracedShadow = false;

            // fetch all the lights in the scene
            HDAdditionalLightData[] hdLightArray = UnityEngine.GameObject.FindObjectsOfType<HDAdditionalLightData>();

            for (int lightIdx = 0; lightIdx < hdLightArray.Length; ++lightIdx)
            {
                HDAdditionalLightData hdLight = hdLightArray[lightIdx];
                if (hdLight.enabled)
                {
                    // Check if there is a ray traced shadow in the scene
                    rayTracedShadow |= (hdLight.useRayTracedShadows || (hdLight.useContactShadow.@override && hdLight.rayTraceContactShadow));

                    switch (hdLight.type)
                    {
                        case HDLightType.Directional:
                            m_RayTracingLights.hdDirectionalLightArray.Add(hdLight);
                            break;
                        case HDLightType.Point:
                            m_RayTracingLights.hdPointLightArray.Add(hdLight);
                            break;
                        case HDLightType.Area:
                            switch (hdLight.areaLightShape)
                            {
                                case AreaLightShape.Rectangle:
                                    m_RayTracingLights.hdRectLightArray.Add(hdLight);
                                    break;
                                case AreaLightShape.Tube:
                                    m_RayTracingLights.hdLineLightArray.Add(hdLight);
                                    break;
                                //TODO: case AreaLightShape.Disc:
                            }
                            break;
                    }
                }
            }

            m_RayTracingLights.hdLightArray.AddRange(m_RayTracingLights.hdPointLightArray);
            m_RayTracingLights.hdLightArray.AddRange(m_RayTracingLights.hdLineLightArray);
            m_RayTracingLights.hdLightArray.AddRange(m_RayTracingLights.hdRectLightArray);

            HDAdditionalReflectionData[] reflectionProbeArray = UnityEngine.GameObject.FindObjectsOfType<HDAdditionalReflectionData>();
            for (int reflIdx = 0; reflIdx < reflectionProbeArray.Length; ++reflIdx)
            {
                HDAdditionalReflectionData reflectionProbe = reflectionProbeArray[reflIdx];
                // Add it to the list if enabled
                if (reflectionProbe.enabled)
                {
                    m_RayTracingLights.reflectionProbeArray.Add(reflectionProbe);
                }
            }

            m_RayTracingLights.lightCount = m_RayTracingLights.hdPointLightArray.Count
                                            + m_RayTracingLights.hdLineLightArray.Count
                                            + m_RayTracingLights.hdRectLightArray.Count
                                            + m_RayTracingLights.reflectionProbeArray.Count;

            AmbientOcclusion aoSettings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();
            ScreenSpaceReflection reflSettings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();
            GlobalIllumination giSettings = VolumeManager.instance.stack.GetComponent<GlobalIllumination>();
            RecursiveRendering recursiveSettings = VolumeManager.instance.stack.GetComponent<RecursiveRendering>();
            PathTracing pathTracingSettings = VolumeManager.instance.stack.GetComponent<PathTracing>();

            // First of all let's process all the LOD groups
            LODGroup[] lodGroupArray = UnityEngine.GameObject.FindObjectsOfType<LODGroup>();
            for (var i = 0; i < lodGroupArray.Length; i++)
            {
                // Grab the current LOD group
                LODGroup lodGroup = lodGroupArray[i];

                // Get the set of LODs
                LOD[] lodArray = lodGroup.GetLODs();
                for (int lodIdx = 0; lodIdx < lodArray.Length; ++lodIdx)
                {
                    LOD currentLOD = lodArray[lodIdx];
                    // We only want to push to the acceleration structure the lod0, we do not have defined way to select the right LOD at the moment
                    if (lodIdx == 0)
                    {
                        for (int rendererIdx = 0; rendererIdx < currentLOD.renderers.Length; ++rendererIdx)
                        {
                            // Fetch the renderer that we are interested in
                            Renderer currentRenderer = currentLOD.renderers[rendererIdx];

                            // This objects should but included into the RAS
                            AddInstanceToRAS(currentRenderer,
                                rayTracedShadow,
                                aoSettings.rayTracing.value, aoSettings.layerMask.value,
                                reflSettings.rayTracing.value, reflSettings.layerMask.value,
                                giSettings.rayTracing.value, giSettings.layerMask.value,
                                recursiveSettings.enable.value, recursiveSettings.layerMask.value,
                                pathTracingSettings.enable.value, pathTracingSettings.layerMask.value);
                        }
                    }

                    // Add them to the processed set so that they are not taken into account when processing all the renderers
                    for (int rendererIdx = 0; rendererIdx < currentLOD.renderers.Length; ++rendererIdx)
                    {
                        Renderer currentRenderer = currentLOD.renderers[rendererIdx];
                        // Add this fella to the renderer list
                        m_RayTracingRendererReference.Add(currentRenderer.GetInstanceID(), 1);
                    }
                }
            }

            // Grab all the renderers from the scene
            var rendererArray = UnityEngine.GameObject.FindObjectsOfType<Renderer>();
            for (var i = 0; i < rendererArray.Length; i++)
            {
                // Fetch the current renderer
                Renderer currentRenderer = rendererArray[i];

                // If it is not active skip it
                if (currentRenderer.enabled == false) continue;

                // Grab the current game object
                GameObject gameObject = currentRenderer.gameObject;

                // Has this object already been processed, just skip it
                if (m_RayTracingRendererReference.ContainsKey(currentRenderer.GetInstanceID()))
                {
                    continue;
                }

                // Does this object have a reflection probe component? if yes we do not want to have it in the acceleration structure
                if (gameObject.TryGetComponent<ReflectionProbe>(out reflectionProbe)) continue;

                // This objects should but included into the RAS
                AddInstanceToRAS(currentRenderer,
                                rayTracedShadow,
                                aoSettings.rayTracing.value, aoSettings.layerMask.value,
                                reflSettings.rayTracing.value, reflSettings.layerMask.value,
                                giSettings.rayTracing.value, giSettings.layerMask.value,
                                recursiveSettings.enable.value, recursiveSettings.layerMask.value,
                                pathTracingSettings.enable.value, pathTracingSettings.layerMask.value);
            }

            // build the acceleration structure
            m_CurrentRAS.Build();

            // tag the structures as valid
            m_ValidRayTracingState = true;
        }

        internal void BuildRayTracingLightCluster(CommandBuffer cmd, HDCamera hdCamera)
        {
            ScreenSpaceReflection reflSettings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();
            GlobalIllumination giSettings = VolumeManager.instance.stack.GetComponent<GlobalIllumination>();
            RecursiveRendering recursiveSettings = VolumeManager.instance.stack.GetComponent<RecursiveRendering>();
            PathTracing pathTracingSettings = VolumeManager.instance.stack.GetComponent<PathTracing>();

            if (m_ValidRayTracingState && (reflSettings.rayTracing.value || giSettings.rayTracing.value || recursiveSettings.enable.value || pathTracingSettings.enable.value))
            {
                m_RayTracingLightCluster.EvaluateLightClusters(cmd, hdCamera, m_RayTracingLights);
                m_ValidRayTracingCluster = true;
            }
        }

        internal RayTracingAccelerationStructure RequestAccelerationStructure()
        {
            if (m_ValidRayTracingState)
            {
                return m_CurrentRAS;
            }
            return null;
        }

        internal HDRaytracingLightCluster RequestLightCluster()
        {
            if (m_ValidRayTracingCluster)
            {
                return m_RayTracingLightCluster;
            }
            return null;
        }

        // Ray Tracing is supported if the asset setting supports it and the platform supports it
        static internal bool AggreateRayTracingSupport(RenderPipelineSettings rpSetting)
        {
            return rpSetting.supportRayTracing && UnityEngine.SystemInfo.supportsRayTracing
#if UNITY_EDITOR
                && UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows64
#endif
            ;
        }
        

        internal BlueNoise GetBlueNoiseManager()
        {
            return m_BlueNoise;
        }

        internal RayCountManager GetRayCountManager()
        {
            return m_RayCountManager;
        }

        internal HDTemporalFilter GetTemporalFilter()
        {
            return m_TemporalFilter;
        }

        internal HDSimpleDenoiser GetSimpleDenoiser()
        {
            return m_SimpleDenoiser;
        }

        internal HDDiffuseDenoiser GetDiffuseDenoiser()
        {
            return m_DiffuseDenoiser;
        }

        internal bool GetRayTracingState()
        {
            return m_ValidRayTracingState;
        }

        internal bool GetRayTracingClusterState()
        {
            return m_ValidRayTracingCluster;
        }
        static internal float GetPixelSpreadTangent(float fov, int width, int height)
        {
            return Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) * 2.0f / Mathf.Min(width, height);
        }

        static internal float GetPixelSpreadAngle(float fov, int width, int height)
        {
            return Mathf.Atan(GetPixelSpreadTangent(fov, width, height));
        }
    }
}
