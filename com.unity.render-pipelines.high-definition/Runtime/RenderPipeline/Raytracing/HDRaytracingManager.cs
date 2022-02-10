using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    internal enum RayTracingRendererFlag
    {
        Opaque = 0x01,
        CastShadowTransparent = 0x02,
        CastShadowOpaque = 0x04,
        CastShadow = CastShadowOpaque | CastShadowTransparent,
        AmbientOcclusion = 0x08,
        Reflection = 0x10,
        GlobalIllumination = 0x20,
        RecursiveRendering = 0x40,
        PathTracing = 0x80
    }

    internal enum AccelerationStructureStatus
    {
        Clear = 0x0,
        Added = 0x1,
        Excluded = 0x02,
        TransparencyIssue = 0x04,
        NullMaterial = 0x08,
        MissingMesh = 0x10
    }

    class HDRayTracingLights
    {
        // The list of non-directional lights in the sub-scene
        public List<HDLightRenderEntity> hdPointLightArray = new List<HDLightRenderEntity>();
        public List<HDLightRenderEntity> hdLineLightArray = new List<HDLightRenderEntity>();
        public List<HDLightRenderEntity> hdRectLightArray = new List<HDLightRenderEntity>();
        public List<HDLightRenderEntity> hdLightEntityArray = new List<HDLightRenderEntity>();

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
        RayTracingAccelerationStructure m_CurrentRAS = null;
        HDRaytracingLightCluster m_RayTracingLightCluster;
        HDRayTracingLights m_RayTracingLights = new HDRayTracingLights();
        bool m_ValidRayTracingState = false;
        bool m_ValidRayTracingCluster = false;
        bool m_ValidRayTracingClusterCulling = false;
        bool m_RayTracedShadowsRequired = false;
        bool m_RayTracedContactShadowsRequired = false;

        // Denoisers
        HDTemporalFilter m_TemporalFilter;
        HDSimpleDenoiser m_SimpleDenoiser;
        HDDiffuseDenoiser m_DiffuseDenoiser;
        HDReflectionDenoiser m_ReflectionDenoiser;
        HDDiffuseShadowDenoiser m_DiffuseShadowDenoiser;

        // Ray-count manager data
        RayCountManager m_RayCountManager;

        const int maxNumSubMeshes = 32;
        Dictionary<int, int> m_RayTracingRendererReference = new Dictionary<int, int>();
        RayTracingSubMeshFlags[] subMeshFlagArray = new RayTracingSubMeshFlags[maxNumSubMeshes];
        ReflectionProbe reflectionProbe = new ReflectionProbe();
        List<Material> materialArray = new List<Material>(maxNumSubMeshes);
        Dictionary<int, bool> m_ShaderValidityCache = new Dictionary<int, bool>();

        // Used to detect material and transform changes for Path Tracing
        Dictionary<int, int> m_MaterialCRCs = new Dictionary<int, int>();
        bool m_MaterialsDirty = false;
        bool m_TransformDirty = false;

        ShaderVariablesRaytracingLightLoop m_ShaderVariablesRaytracingLightLoopCB = new ShaderVariablesRaytracingLightLoop();

        internal void InitRayTracingManager()
        {
            // Init the ray count manager
            m_RayCountManager = new RayCountManager();
            m_RayCountManager.Init(m_GlobalSettings.renderPipelineRayTracingResources);

            // Build the light cluster
            m_RayTracingLightCluster = new HDRaytracingLightCluster();
            m_RayTracingLightCluster.Initialize(this);
        }

        internal void ReleaseRayTracingManager()
        {
            if (m_CurrentRAS != null)
                m_CurrentRAS.Dispose();

            if (m_RayTracingLightCluster != null)
                m_RayTracingLightCluster.ReleaseResources();
            if (m_RayCountManager != null)
                m_RayCountManager.Release();

            if (m_ReflectionDenoiser != null)
                m_ReflectionDenoiser.Release();
            if (m_TemporalFilter != null)
                m_TemporalFilter.Release();
            if (m_SimpleDenoiser != null)
                m_SimpleDenoiser.Release();
            if (m_DiffuseShadowDenoiser != null)
                m_DiffuseShadowDenoiser.Release();
            if (m_DiffuseDenoiser != null)
                m_DiffuseDenoiser.Release();
        }

        bool IsValidRayTracedMaterial(Material currentMaterial)
        {
            if (currentMaterial == null || currentMaterial.shader == null)
                return false;

            bool isValid;

            // We use a cache, to speed up the case where materials/shaders are reused many times
            int shaderId = currentMaterial.shader.GetInstanceID();
            if (m_ShaderValidityCache.TryGetValue(shaderId, out isValid))
                return isValid;

            // For the time being, we only consider non-decal HDRP materials as valid
            isValid = currentMaterial.GetTag("RenderPipeline", false) == "HDRenderPipeline" && !DecalSystem.IsDecalMaterial(currentMaterial);

            m_ShaderValidityCache.Add(shaderId, isValid);

            return isValid;
        }

        static bool IsTransparentMaterial(Material currentMaterial)
        {
            return currentMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                || (HDRenderQueue.k_RenderQueue_Transparent.lowerBound <= currentMaterial.renderQueue
                    && HDRenderQueue.k_RenderQueue_Transparent.upperBound >= currentMaterial.renderQueue);
        }

        static bool IsAlphaTestedMaterial(Material currentMaterial)
        {
            return currentMaterial.IsKeywordEnabled("_ALPHATEST_ON")
                || (HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.lowerBound <= currentMaterial.renderQueue
                    && HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.upperBound >= currentMaterial.renderQueue);
        }

        AccelerationStructureStatus AddInstanceToRAS(Renderer currentRenderer,
            bool rayTracedShadow,
            bool aoEnabled, int aoLayerValue,
            bool reflEnabled, int reflLayerValue,
            bool giEnabled, int giLayerValue,
            bool recursiveEnabled, int rrLayerValue,
            bool pathTracingEnabled, int ptLayerValue)
        {
            // Get all the materials of the mesh renderer
            currentRenderer.GetSharedMaterials(materialArray);
            // If the array is null, we are done
            if (materialArray == null) return AccelerationStructureStatus.NullMaterial;

            // For every sub-mesh/sub-material let's build the right flags
            int numSubMeshes = 1;
            if (!(currentRenderer.GetType() == typeof(SkinnedMeshRenderer)))
            {
                currentRenderer.TryGetComponent(out MeshFilter meshFilter);
                if (meshFilter == null || meshFilter.sharedMesh == null) return AccelerationStructureStatus.MissingMesh;
                numSubMeshes = meshFilter.sharedMesh.subMeshCount;
            }
            else
            {
                SkinnedMeshRenderer skinnedMesh = (SkinnedMeshRenderer)currentRenderer;
                if (skinnedMesh.sharedMesh == null) return AccelerationStructureStatus.MissingMesh;
                numSubMeshes = skinnedMesh.sharedMesh.subMeshCount;
            }

            // Let's clamp the number of sub-meshes to avoid throwing an unwated error
            numSubMeshes = Mathf.Min(numSubMeshes, maxNumSubMeshes);

            // Get the layer of this object
            int objectLayerValue = 1 << currentRenderer.gameObject.layer;

            // We need to build the instance flag for this renderer
            uint instanceFlag = 0x00;

            bool doubleSided = false;
            bool materialIsOnlyTransparent = true;
            bool hasTransparentSubMaterial = false;

            // We disregard the ray traced shadows option when in Path Tracing
            rayTracedShadow &= !pathTracingEnabled;

            // Deactivate Path Tracing if the object does not belong to the path traced layer(s)
            pathTracingEnabled &= (bool)((ptLayerValue & objectLayerValue) != 0);

            for (int meshIdx = 0; meshIdx < numSubMeshes; ++meshIdx)
            {
                // Initially we consider the potential mesh as invalid
                bool validMesh = false;
                if (materialArray.Count > meshIdx)
                {
                    // Grab the material for the current sub-mesh
                    Material currentMaterial = materialArray[meshIdx];

                    // Make sure that the material is HDRP's and non-decal
                    if (IsValidRayTracedMaterial(currentMaterial))
                    {
                        // Mesh is valid given that all requirements are ok
                        validMesh = true;

                        // First mark the thing as valid
                        subMeshFlagArray[meshIdx] = RayTracingSubMeshFlags.Enabled;

                        // Evaluate what kind of materials we are dealing with
                        bool alphaTested = IsAlphaTestedMaterial(currentMaterial);
                        bool transparentMaterial = IsTransparentMaterial(currentMaterial);

                        // Aggregate the transparency info
                        materialIsOnlyTransparent &= transparentMaterial;
                        hasTransparentSubMaterial |= transparentMaterial;

                        // Append the additional flags depending on what kind of sub mesh this is
                        if (!transparentMaterial && !alphaTested)
                            subMeshFlagArray[meshIdx] |= RayTracingSubMeshFlags.ClosestHitOnly;
                        else if (transparentMaterial)
                            subMeshFlagArray[meshIdx] |= RayTracingSubMeshFlags.UniqueAnyHitCalls;

                        // Check if we want to enable double-sidedness for the mesh
                        // (note that a mix of single and double-sided materials will result in a double-sided mesh in the AS)
                        doubleSided |= currentMaterial.doubleSidedGI || currentMaterial.IsKeywordEnabled("_DOUBLESIDED_ON");

                        // Check if the material has changed since last time we were here
                        if (!m_MaterialsDirty)
                        {
                            int matId = currentMaterial.GetInstanceID();
                            int matPrevCRC, matCurCRC = currentMaterial.ComputeCRC();
                            if (m_MaterialCRCs.TryGetValue(matId, out matPrevCRC))
                            {
                                m_MaterialCRCs[matId] = matCurCRC;
                                m_MaterialsDirty |= (matCurCRC != matPrevCRC);
                            }
                            else
                            {
                                m_MaterialCRCs.Add(matId, matCurCRC);
                            }
                        }
                    }
                }

                // If the mesh was not valid, exclude it (without affecting sidedness)
                if (!validMesh)
                    subMeshFlagArray[meshIdx] = RayTracingSubMeshFlags.Disabled;
            }

            // If the material is considered opaque, every sub-mesh has to be enabled and with unique any hit calls
            if (!materialIsOnlyTransparent && hasTransparentSubMaterial)
                for (int meshIdx = 0; meshIdx < numSubMeshes; ++meshIdx)
                    subMeshFlagArray[meshIdx] = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.UniqueAnyHitCalls;


            // Propagate the opacity mask only if all sub materials are opaque
            bool isOpaque = !hasTransparentSubMaterial;
            if (isOpaque)
            {
                instanceFlag |= (uint)(RayTracingRendererFlag.Opaque);
            }

            if (rayTracedShadow || pathTracingEnabled)
            {
                if (hasTransparentSubMaterial)
                {
                    // Raise the shadow casting flag if needed
                    instanceFlag |= ((currentRenderer.shadowCastingMode != ShadowCastingMode.Off) ? (uint)(RayTracingRendererFlag.CastShadowTransparent) : 0x00);
                }
                else
                {
                    // Raise the shadow casting flag if needed
                    instanceFlag |= ((currentRenderer.shadowCastingMode != ShadowCastingMode.Off) ? (uint)(RayTracingRendererFlag.CastShadowOpaque) : 0x00);
                }
            }

            // We consider a mesh visible by reflection, gi, etc if it is not in the shadow only mode.
            bool meshIsVisible = currentRenderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly;

            if (aoEnabled && !materialIsOnlyTransparent && meshIsVisible)
            {
                // Raise the Ambient Occlusion flag if needed
                instanceFlag |= ((aoLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.AmbientOcclusion) : 0x00;
            }

            if (reflEnabled && !materialIsOnlyTransparent && meshIsVisible)
            {
                // Raise the Screen Space Reflection flag if needed
                instanceFlag |= ((reflLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.Reflection) : 0x00;
            }

            if (giEnabled && !materialIsOnlyTransparent && meshIsVisible)
            {
                // Raise the Global Illumination flag if needed
                instanceFlag |= ((giLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.GlobalIllumination) : 0x00;
            }

            if (recursiveEnabled && meshIsVisible)
            {
                // Raise the Recursive Rendering flag if needed
                instanceFlag |= ((rrLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.RecursiveRendering) : 0x00;
            }

            if (pathTracingEnabled && meshIsVisible)
            {
                // Raise the Path Tracing flag if needed
                instanceFlag |= (uint)(RayTracingRendererFlag.PathTracing);
            }

            // If the object was not referenced
            if (instanceFlag == 0) return AccelerationStructureStatus.Added;

            // Add it to the acceleration structure
            m_CurrentRAS.AddInstance(currentRenderer, subMeshFlags: subMeshFlagArray, enableTriangleCulling: !doubleSided, mask: instanceFlag);

            // Indicates that a transform has changed in our scene (mesh or light)
            m_TransformDirty |= currentRenderer.transform.hasChanged;
            currentRenderer.transform.hasChanged = false;

            // return the status
            return (!materialIsOnlyTransparent && hasTransparentSubMaterial) ? AccelerationStructureStatus.TransparencyIssue : AccelerationStructureStatus.Added;
        }

        // This function mimics the LOD calculation that is used for the rasterization
        // THe idea is to have rasterization and ray tracing perfectly match.
        internal float CalculateCameraDistance(float fieldOfView, float worldSpaceSize, float relativeScreenHeight)
        {
            float halfAngle = Mathf.Tan(Mathf.Deg2Rad * fieldOfView * 0.5F);
            return worldSpaceSize / (2.0f * relativeScreenHeight * halfAngle);
        }

        internal float GetWorldSpaceSize(LODGroup lodGroup)
        {
            Vector3 lossyScale = lodGroup.transform.lossyScale;
            return Mathf.Max(Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y)), Mathf.Abs(lossyScale.z)) * lodGroup.size;
        }

        internal void BuildRayTracingAccelerationStructure(HDCamera hdCamera)
        {
            // Clear all the per frame-data
            m_RayTracingRendererReference.Clear();
            m_RayTracingLights.hdDirectionalLightArray.Clear();
            m_RayTracingLights.hdPointLightArray.Clear();
            m_RayTracingLights.hdLineLightArray.Clear();
            m_RayTracingLights.hdRectLightArray.Clear();
            m_RayTracingLights.hdLightEntityArray.Clear();
            m_RayTracingLights.reflectionProbeArray.Clear();
            m_RayTracingLights.lightCount = 0;
            if (m_CurrentRAS != null)
                m_CurrentRAS.Dispose();
            m_CurrentRAS = new RayTracingAccelerationStructure();
            m_ValidRayTracingState = false;
            m_ValidRayTracingCluster = false;
            m_ValidRayTracingClusterCulling = false;
            m_RayTracedShadowsRequired = false;
            m_RayTracedContactShadowsRequired = false;

            // If the camera does not have a ray tracing frame setting or it is a preview camera (due to the fact that the sphere does not exist as a game object we can't create the RTAS) we do not want to build a RTAS
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                return;

            // We only support ray traced shadows if the camera supports ray traced shadows
            bool screenSpaceShadowsSupported = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows);

            // fetch all the lights in the scene
            HDLightRenderDatabase lightEntities = HDLightRenderDatabase.instance;
            for (int lightIdx = 0; lightIdx < lightEntities.lightCount; ++lightIdx)
            {
                HDLightRenderEntity lightRenderEntity = lightEntities.lightEntities[lightIdx];
                HDAdditionalLightData hdLight = lightEntities.hdAdditionalLightData[lightIdx];
                if (hdLight != null && hdLight.enabled && hdLight != HDUtils.s_DefaultHDAdditionalLightData)
                {
                    // Check if there is a ray traced shadow in the scene
                    m_RayTracedShadowsRequired |= (hdLight.useRayTracedShadows && screenSpaceShadowsSupported);
                    m_RayTracedContactShadowsRequired |= (hdLight.useContactShadow.@override && hdLight.rayTraceContactShadow);

                    // Indicates that a transform has changed in our scene (mesh or light)
                    m_TransformDirty |= hdLight.transform.hasChanged;
                    hdLight.transform.hasChanged = false;

                    switch (hdLight.type)
                    {
                        case HDLightType.Directional:
                            m_RayTracingLights.hdDirectionalLightArray.Add(hdLight);
                            break;
                        case HDLightType.Point:
                        case HDLightType.Spot:
                            m_RayTracingLights.hdPointLightArray.Add(lightRenderEntity);
                            break;
                        case HDLightType.Area:
                            switch (hdLight.areaLightShape)
                            {
                                case AreaLightShape.Rectangle:
                                    m_RayTracingLights.hdRectLightArray.Add(lightRenderEntity);
                                    break;
                                case AreaLightShape.Tube:
                                    m_RayTracingLights.hdLineLightArray.Add(lightRenderEntity);
                                    break;
                                    //TODO: case AreaLightShape.Disc:
                            }
                            break;
                    }
                }
            }

            // Aggregate the shadow requirement
            bool rayTracedShadows = m_RayTracedShadowsRequired || m_RayTracedContactShadowsRequired;

            m_RayTracingLights.hdLightEntityArray.AddRange(m_RayTracingLights.hdPointLightArray);
            m_RayTracingLights.hdLightEntityArray.AddRange(m_RayTracingLights.hdLineLightArray);
            m_RayTracingLights.hdLightEntityArray.AddRange(m_RayTracingLights.hdRectLightArray);

            HDAdditionalReflectionData[] reflectionProbeArray = UnityEngine.GameObject.FindObjectsOfType<HDAdditionalReflectionData>();
            for (int reflIdx = 0; reflIdx < reflectionProbeArray.Length; ++reflIdx)
            {
                HDAdditionalReflectionData reflectionProbe = reflectionProbeArray[reflIdx];
                // Add it to the list if enabled
                // Skip the probe if the probe has never rendered (in realtime cases) or if texture is null
                if (reflectionProbe != null
                    && reflectionProbe.enabled
                    && reflectionProbe.ReflectionProbeIsEnabled()
                    && reflectionProbe.gameObject.activeSelf
                    && reflectionProbe.HasValidRenderedData())
                {
                    m_RayTracingLights.reflectionProbeArray.Add(reflectionProbe);
                }
            }

            m_RayTracingLights.lightCount = m_RayTracingLights.hdPointLightArray.Count
                + m_RayTracingLights.hdLineLightArray.Count
                + m_RayTracingLights.hdRectLightArray.Count
                + m_RayTracingLights.reflectionProbeArray.Count;

            AmbientOcclusion aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            bool rtAOEnabled = aoSettings.rayTracing.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSAO);
            ScreenSpaceReflection reflSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            bool rtREnabled = (reflSettings.enabled.value || reflSettings.enabledTransparent.value) && ScreenSpaceReflection.RayTracingActive(reflSettings) && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR);
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            bool rtGIEnabled = giSettings.enable.value && GlobalIllumination.RayTracingActive(giSettings) && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSGI);
            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
            bool rrEnabled = recursiveSettings.enable.value;
            SubSurfaceScattering sssSettings = hdCamera.volumeStack.GetComponent<SubSurfaceScattering>();
            bool rtSSSEnabled = sssSettings.rayTracing.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering);
            PathTracing pathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();
            bool ptEnabled = pathTracingSettings.enable.value;

            // We need to check if we should be building the ray tracing acceleration structure (if required by any effect)
            bool rayTracingRequired = rtAOEnabled || rtREnabled || rtGIEnabled || rrEnabled || rtSSSEnabled || ptEnabled || rayTracedShadows;
            if (!rayTracingRequired)
                return;

            // We need to process the emissive meshes of the rectangular area lights
            for (var i = 0; i < m_RayTracingLights.hdRectLightArray.Count; i++)
            {
                // Fetch the current renderer of the rectangular area light (if any)
                int dataIndex = HDLightRenderDatabase.instance.GetEntityDataIndex(m_RayTracingLights.hdRectLightArray[i]);
                MeshRenderer currentRenderer = HDLightRenderDatabase.instance.hdAdditionalLightData[dataIndex].emissiveMeshRenderer;

                // If there is none it means that there is no emissive mesh for this light
                if (currentRenderer == null) continue;

                // This objects should be included into the RAS
                AddInstanceToRAS(currentRenderer,
                    rayTracedShadows,
                    rtAOEnabled, aoSettings.layerMask.value,
                    rtREnabled, reflSettings.layerMask.value,
                    rtGIEnabled, giSettings.layerMask.value,
                    rrEnabled, recursiveSettings.layerMask.value,
                    ptEnabled, pathTracingSettings.layerMask.value);
            }

            int matCount = m_MaterialCRCs.Count;

            LODGroup[] lodGroupArray = UnityEngine.GameObject.FindObjectsOfType<LODGroup>();
            for (var i = 0; i < lodGroupArray.Length; i++)
            {
                // Grab the current LOD group
                LODGroup lodGroup = lodGroupArray[i];

                // Compute the distance between the LOD group and the camera
                float lodGroupDistance = (hdCamera.camera.transform.position - lodGroup.transform.TransformPoint(lodGroup.localReferencePoint)).magnitude;

                // Flag to track if the LODGroup has been represented
                bool lodGroupProcessed = false;

                // Get the set of LODs
                LOD[] lodArray = lodGroup.GetLODs();
                for (int lodIdx = 0; lodIdx < lodArray.Length; ++lodIdx)
                {
                    // Grab the current LOD
                    LOD currentLOD = lodArray[lodIdx];

                    // Compute the distance until which this LOD may be used
                    float maxLODUsageDistance = CalculateCameraDistance(hdCamera.camera.fieldOfView, GetWorldSpaceSize(lodGroup), currentLOD.screenRelativeTransitionHeight);

                    // If the LOD should be the used one
                    if (!lodGroupProcessed && lodGroupDistance < maxLODUsageDistance)
                    {
                        for (int rendererIdx = 0; rendererIdx < currentLOD.renderers.Length; ++rendererIdx)
                        {
                            // Fetch the renderer that we are interested in
                            Renderer currentRenderer = currentLOD.renderers[rendererIdx];

                            // It is possible that the renderer is null and we need to skip it.
                            if (currentRenderer == null) continue;

                            // This objects should but included into the RAS
                            AddInstanceToRAS(currentRenderer,
                                rayTracedShadows,
                                aoSettings.rayTracing.value, aoSettings.layerMask.value,
                                ScreenSpaceReflection.RayTracingActive(reflSettings), reflSettings.layerMask.value,
                                GlobalIllumination.RayTracingActive(giSettings), giSettings.layerMask.value,
                                recursiveSettings.enable.value, recursiveSettings.layerMask.value,
                                pathTracingSettings.enable.value, pathTracingSettings.layerMask.value);
                        }

                        // Flag the LOD group as processed
                        lodGroupProcessed = true;
                    }

                    // Add them to the processed set so that they are not taken into account when processing all the renderers
                    for (int rendererIdx = 0; rendererIdx < currentLOD.renderers.Length; ++rendererIdx)
                    {
                        Renderer currentRenderer = currentLOD.renderers[rendererIdx];
                        if (currentRenderer == null) continue;

                        // Add this fella to the renderer list
                        // Unfortunately, we need to check that this renderer was not already pushed into the list (happens if the user uses the same mesh renderer
                        // for two LODs)
                        if (!m_RayTracingRendererReference.ContainsKey(currentRenderer.GetInstanceID()))
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

                // This objects should be included into the RAS
                AddInstanceToRAS(currentRenderer,
                    rayTracedShadows,
                    aoSettings.rayTracing.value, aoSettings.layerMask.value,
                    ScreenSpaceReflection.RayTracingActive(reflSettings), reflSettings.layerMask.value,
                    GlobalIllumination.RayTracingActive(giSettings), giSettings.layerMask.value,
                    recursiveSettings.enable.value, recursiveSettings.layerMask.value,
                    pathTracingSettings.enable.value, pathTracingSettings.layerMask.value);
            }

            // Check if the amount of materials being tracked has changed
            m_MaterialsDirty |= (matCount != m_MaterialCRCs.Count);

            if (ShaderConfig.s_CameraRelativeRendering != 0)
                m_CurrentRAS.Build(hdCamera.mainViewConstants.worldSpaceCameraPos);
            else
                m_CurrentRAS.Build();

            // tag the structures as valid
            m_ValidRayTracingState = true;
        }

        static internal bool ValidRayTracingHistory(HDCamera hdCamera)
        {
            return hdCamera.historyRTHandleProperties.previousViewportSize.x == hdCamera.actualWidth
                && hdCamera.historyRTHandleProperties.previousViewportSize.y == hdCamera.actualHeight;
        }

        internal static int RayTracingFrameIndex(HDCamera hdCamera)
        {
#if UNITY_HDRP_DXR_TESTS_DEFINE
            if (Application.isPlaying)
                return 0;
            else
#endif
            return (int)hdCamera.GetCameraFrameCount() % 8;
        }

        internal int RayTracingFrameIndex(HDCamera hdCamera, int targetFrameCount = 8)
        {
#if UNITY_HDRP_DXR_TESTS_DEFINE
            if (Application.isPlaying)
                return 0;
            else
#endif
            return (int)hdCamera.GetCameraFrameCount() % targetFrameCount;
        }

        internal bool RayTracingLightClusterRequired(HDCamera hdCamera)
        {
            ScreenSpaceReflection reflSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
            PathTracing pathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();
            SubSurfaceScattering subSurface = hdCamera.volumeStack.GetComponent<SubSurfaceScattering>();

            return (m_ValidRayTracingState &&
                (ScreenSpaceReflection.RayTracingActive(reflSettings)
                    || GlobalIllumination.RayTracingActive(giSettings)
                    || recursiveSettings.enable.value
                    || pathTracingSettings.enable.value
                    || subSurface.rayTracing.value));
        }

        internal void CullForRayTracing(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (m_ValidRayTracingState && RayTracingLightClusterRequired(hdCamera))
            {
                m_RayTracingLightCluster.CullForRayTracing(hdCamera, m_RayTracingLights);
                m_ValidRayTracingClusterCulling = true;
            }
        }

        internal void ReserveRayTracingCookieAtlasSlots()
        {
            if (m_ValidRayTracingState && m_ValidRayTracingClusterCulling)
            {
                m_RayTracingLightCluster.ReserveCookieAtlasSlots(m_RayTracingLights);
            }
        }

        internal void BuildRayTracingLightData(CommandBuffer cmd, HDCamera hdCamera, DebugDisplaySettings debugDisplaySettings)
        {
            if (m_ValidRayTracingState && m_ValidRayTracingClusterCulling)
            {
                m_RayTracingLightCluster.BuildRayTracingLightData(cmd, hdCamera, m_RayTracingLights, debugDisplaySettings);
                m_ValidRayTracingCluster = true;

                UpdateShaderVariablesRaytracingLightLoopCB(hdCamera, cmd);

                m_RayTracingLightCluster.BuildLightClusterBuffer(cmd, hdCamera, m_RayTracingLights);
            }
        }

        static internal float EvaluateHistoryValidity(HDCamera hdCamera)
        {
            float historyValidity = 1.0f;
#if UNITY_HDRP_DXR_TESTS_DEFINE
            if (Application.isPlaying)
                historyValidity = 0.0f;
            else
#endif
            // We need to check if something invalidated the history buffers
            historyValidity *= ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;
            return historyValidity;
        }

        internal bool RayTracingHalfResAllowed()
        {
            return DynamicResolutionHandler.instance.GetCurrentScale() >= (currentPlatformRenderPipelineSettings.dynamicResolutionSettings.rayTracingHalfResThreshold / 100.0f);
        }

        void UpdateShaderVariablesRaytracingLightLoopCB(HDCamera hdCamera, CommandBuffer cmd)
        {
            m_ShaderVariablesRaytracingLightLoopCB._MinClusterPos = m_RayTracingLightCluster.GetMinClusterPos();
            m_ShaderVariablesRaytracingLightLoopCB._LightPerCellCount = (uint)m_RayTracingLightCluster.GetLightPerCellCount();
            m_ShaderVariablesRaytracingLightLoopCB._MaxClusterPos = m_RayTracingLightCluster.GetMaxClusterPos();
            m_ShaderVariablesRaytracingLightLoopCB._PunctualLightCountRT = (uint)m_RayTracingLightCluster.GetPunctualLightCount();
            m_ShaderVariablesRaytracingLightLoopCB._AreaLightCountRT = (uint)m_RayTracingLightCluster.GetAreaLightCount();
            m_ShaderVariablesRaytracingLightLoopCB._EnvLightCountRT = (uint)m_RayTracingLightCluster.GetEnvLightCount();

            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesRaytracingLightLoopCB, HDShaderIDs._ShaderVariablesRaytracingLightLoop);
        }

        internal bool RayTracedContactShadowsRequired()
        {
            return m_RayTracedContactShadowsRequired;
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
        static internal bool PipelineSupportsRayTracing(RenderPipelineSettings rpSetting)
            => rpSetting.supportRayTracing && currentSystemSupportsRayTracing;

        static internal bool currentSystemSupportsRayTracing => SystemInfo.supportsRayTracing
#if UNITY_EDITOR
            && ((UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows64
                || UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows)
            || UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.PS5);
#else
            ;
#endif

        internal BlueNoise GetBlueNoiseManager()
        {
            return m_BlueNoise;
        }

        internal HDTemporalFilter GetTemporalFilter()
        {
            if (m_TemporalFilter == null)
            {
                m_TemporalFilter = new HDTemporalFilter();
                m_TemporalFilter.Init(m_GlobalSettings.renderPipelineResources);
            }
            return m_TemporalFilter;
        }

        internal HDSimpleDenoiser GetSimpleDenoiser()
        {
            if (m_SimpleDenoiser == null)
            {
                m_SimpleDenoiser = new HDSimpleDenoiser();
                m_SimpleDenoiser.Init(m_GlobalSettings.renderPipelineRayTracingResources);
            }
            return m_SimpleDenoiser;
        }

        internal HDDiffuseDenoiser GetDiffuseDenoiser()
        {
            if (m_DiffuseDenoiser == null)
            {
                m_DiffuseDenoiser = new HDDiffuseDenoiser();
                m_DiffuseDenoiser.Init(m_GlobalSettings.renderPipelineResources, this);
            }
            return m_DiffuseDenoiser;
        }

        internal HDReflectionDenoiser GetReflectionDenoiser()
        {
            if (m_ReflectionDenoiser == null)
            {
                m_ReflectionDenoiser = new HDReflectionDenoiser();
                m_ReflectionDenoiser.Init(m_GlobalSettings.renderPipelineRayTracingResources);
            }
            return m_ReflectionDenoiser;
        }

        internal HDDiffuseShadowDenoiser GetDiffuseShadowDenoiser()
        {
            if (m_DiffuseShadowDenoiser == null)
            {
                m_DiffuseShadowDenoiser = new HDDiffuseShadowDenoiser();
                m_DiffuseShadowDenoiser.Init(m_GlobalSettings.renderPipelineRayTracingResources);
            }
            return m_DiffuseShadowDenoiser;
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

        internal TextureHandle EvaluateHistoryValidationBuffer(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer)
        {
            // Grab the temporal filter
            HDTemporalFilter temporalFilter = GetTemporalFilter();

            // If the temporal filter is valid use it, otherwise return a white texture
            if (temporalFilter != null)
            {
                float historyValidity = EvaluateHistoryValidity(hdCamera);
                return temporalFilter.HistoryValidity(renderGraph, hdCamera, historyValidity, depthBuffer, normalBuffer, motionVectorsBuffer);
            }
            else
                return renderGraph.defaultResources.whiteTexture;
        }
    }
}
