using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

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

    internal enum InternalRayTracingBuffers
    {
        Distance,
        Direction,
        R0,
        R1,
        RG0,
        RG1,
        RGBA0,
        RGBA1,
        RGBA2,
        RGBA3,
        RGBA4
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
        SSGIDenoiser m_SSGIDenoiser;

        // Ray-count manager data
        RayCountManager m_RayCountManager;

        const int maxNumSubMeshes = 32;
        Dictionary<int, int> m_RayTracingRendererReference = new Dictionary<int, int>();
        bool[] subMeshFlagArray = new bool[maxNumSubMeshes];
        bool[] subMeshCutoffArray = new bool[maxNumSubMeshes];
        bool[] subMeshTransparentArray = new bool[maxNumSubMeshes];
        ReflectionProbe reflectionProbe = new ReflectionProbe();
        List<Material> materialArray = new List<Material>(maxNumSubMeshes);
        Dictionary<int, bool> m_ShaderValidityCache = new Dictionary<int, bool>();

        // Used to detect material and transform changes for Path Tracing
        Dictionary<int, int> m_MaterialCRCs = new Dictionary<int, int>();
        bool m_MaterialsDirty = false;
        bool m_TransformDirty = false;

        // Ray Direction/Distance buffers
        RTHandle m_RayTracingDirectionBuffer;
        RTHandle m_RayTracingDistanceBuffer;

        // Set of intermediate textures that will be used by ray tracing effects
        RTHandle m_RayTracingIntermediateBufferR0;
        RTHandle m_RayTracingIntermediateBufferR1;
        RTHandle m_RayTracingIntermediateBufferRG0;
        RTHandle m_RayTracingIntermediateBufferRG1;
        RTHandle m_RayTracingIntermediateBufferRGBA0;
        RTHandle m_RayTracingIntermediateBufferRGBA1;
        RTHandle m_RayTracingIntermediateBufferRGBA2;
        RTHandle m_RayTracingIntermediateBufferRGBA3;
        RTHandle m_RayTracingIntermediateBufferRGBA4;

        ShaderVariablesRaytracingLightLoop m_ShaderVariablesRaytracingLightLoopCB = new ShaderVariablesRaytracingLightLoop();

        internal void InitRayTracingManager()
        {
            // Init the ray count manager
            m_RayCountManager = new RayCountManager();
            m_RayCountManager.Init(m_Asset.renderPipelineRayTracingResources);

            // Build the light cluster
            m_RayTracingLightCluster = new HDRaytracingLightCluster();
            m_RayTracingLightCluster.Initialize(this);
        }

        internal void ReleaseRayTracingManager()
        {
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
            if (m_SSGIDenoiser != null)
                m_SSGIDenoiser.Release();
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

        void RaytracingManagerCleanupNonRenderGraphResources()
        {
            RTHandles.Release(m_RayTracingDistanceBuffer);
            m_RayTracingDistanceBuffer = null;
            RTHandles.Release(m_RayTracingDirectionBuffer);
            m_RayTracingDirectionBuffer = null;

            RTHandles.Release(m_RayTracingIntermediateBufferR0);
            m_RayTracingIntermediateBufferR0 = null;
            RTHandles.Release(m_RayTracingIntermediateBufferR1);
            m_RayTracingIntermediateBufferR1 = null;
            RTHandles.Release(m_RayTracingIntermediateBufferRG0);
            m_RayTracingIntermediateBufferRG0 = null;
            RTHandles.Release(m_RayTracingIntermediateBufferRG1);
            m_RayTracingIntermediateBufferRG1 = null;
            RTHandles.Release(m_RayTracingIntermediateBufferRGBA0);
            m_RayTracingIntermediateBufferRGBA0 = null;
            RTHandles.Release(m_RayTracingIntermediateBufferRGBA1);
            m_RayTracingIntermediateBufferRGBA1 = null;
            RTHandles.Release(m_RayTracingIntermediateBufferRGBA2);
            m_RayTracingIntermediateBufferRGBA2 = null;
            RTHandles.Release(m_RayTracingIntermediateBufferRGBA3);
            m_RayTracingIntermediateBufferRGBA3 = null;
            RTHandles.Release(m_RayTracingIntermediateBufferRGBA4);
            m_RayTracingIntermediateBufferRGBA4 = null;
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
                        subMeshFlagArray[meshIdx] = true;

                        // Is the sub material transparent?
                        subMeshTransparentArray[meshIdx] = currentMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                        || (HDRenderQueue.k_RenderQueue_Transparent.lowerBound <= currentMaterial.renderQueue
                        && HDRenderQueue.k_RenderQueue_Transparent.upperBound >= currentMaterial.renderQueue);

                        // Aggregate the transparency info
                        materialIsOnlyTransparent &= subMeshTransparentArray[meshIdx];
                        hasTransparentSubMaterial |= subMeshTransparentArray[meshIdx];

                        // Is the material alpha tested?
                        subMeshCutoffArray[meshIdx] = currentMaterial.IsKeywordEnabled("_ALPHATEST_ON")
                        || (HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.lowerBound <= currentMaterial.renderQueue
                        && HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.upperBound >= currentMaterial.renderQueue);

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
                {
                    subMeshFlagArray[meshIdx] = false;
                    subMeshCutoffArray[meshIdx] = false;
                }
            }

            // If the material is considered opaque, but has some transparent sub-materials
            if (!materialIsOnlyTransparent && hasTransparentSubMaterial)
            {
                for (int meshIdx = 0; meshIdx < numSubMeshes; ++meshIdx)
                {
                    subMeshCutoffArray[meshIdx] = subMeshTransparentArray[meshIdx] ? true : subMeshCutoffArray[meshIdx];
                }
            }

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
            m_CurrentRAS.AddInstance(currentRenderer, subMeshMask: subMeshFlagArray, subMeshTransparencyFlags: subMeshCutoffArray, enableTriangleCulling: !doubleSided, mask: instanceFlag);

            // Indicates that a transform has changed in our scene (mesh or light)
            m_TransformDirty |= currentRenderer.transform.hasChanged;
            currentRenderer.transform.hasChanged = false;

            // return the status
            return (!materialIsOnlyTransparent && hasTransparentSubMaterial) ? AccelerationStructureStatus.TransparencyIssue : AccelerationStructureStatus.Added;
        }

        internal void BuildRayTracingAccelerationStructure(HDCamera hdCamera)
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
            m_ValidRayTracingClusterCulling = false;
            m_RayTracedShadowsRequired = false;
            m_RayTracedContactShadowsRequired = false;

            // If the camera does not have a ray tracing frame setting or it is a preview camera (due to the fact that the sphere does not exist as a game object we can't create the RTAS) we do not want to build a RTAS
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                return;

            // We only support ray traced shadows if the camera supports ray traced shadows
            bool screenSpaceShadowsSupported = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows);

            // fetch all the lights in the scene
            HDAdditionalLightData[] hdLightArray = UnityEngine.GameObject.FindObjectsOfType<HDAdditionalLightData>();

            for (int lightIdx = 0; lightIdx < hdLightArray.Length; ++lightIdx)
            {
                HDAdditionalLightData hdLight = hdLightArray[lightIdx];
                if (hdLight.enabled)
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

            // Aggregate the shadow requirement
            bool rayTracedShadows = m_RayTracedShadowsRequired || m_RayTracedContactShadowsRequired;

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

            AmbientOcclusion aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            bool rtAOEnabled = aoSettings.rayTracing.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSAO);
            ScreenSpaceReflection reflSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            bool rtREnabled = reflSettings.enabled.value && reflSettings.rayTracing.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR);
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            bool rtGIEnabled = giSettings.enable.value && giSettings.rayTracing.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSGI);
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
                MeshRenderer currentRenderer = m_RayTracingLights.hdRectLightArray[i].emissiveMeshRenderer;

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
                                rayTracedShadows,
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
                                reflSettings.rayTracing.value, reflSettings.layerMask.value,
                                giSettings.rayTracing.value, giSettings.layerMask.value,
                                recursiveSettings.enable.value, recursiveSettings.layerMask.value,
                                pathTracingSettings.enable.value, pathTracingSettings.layerMask.value);
            }

            // Check if the amount of materials being tracked has changed
            m_MaterialsDirty |= (matCount != m_MaterialCRCs.Count);

            if (ShaderConfig.s_CameraRelativeRendering != 0)
                m_CurrentRAS.Build(hdCamera.camera.transform.position);
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

        internal int RayTracingFrameIndex(HDCamera hdCamera)
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

            return (m_ValidRayTracingState && (reflSettings.rayTracing.value
                                                || giSettings.rayTracing.value
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

        internal bool RayTracedShadowsRequired()
        {
            return m_RayTracedShadowsRequired;
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
        static internal bool GatherRayTracingSupport(RenderPipelineSettings rpSetting)
            => rpSetting.supportRayTracing && rayTracingSupportedBySystem;

        static internal bool rayTracingSupportedBySystem
            => UnityEngine.SystemInfo.supportsRayTracing
#if UNITY_EDITOR
                && (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows64
                    || UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows)
#endif
            ;

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
            if (m_TemporalFilter == null)
            {
                m_TemporalFilter = new HDTemporalFilter();
                m_TemporalFilter.Init(m_Asset.renderPipelineRayTracingResources, sharedRTManager, this);
            }
            return m_TemporalFilter;
        }

        internal HDSimpleDenoiser GetSimpleDenoiser()
        {
            if (m_SimpleDenoiser == null)
            {
                m_SimpleDenoiser = new HDSimpleDenoiser();
                m_SimpleDenoiser.Init(m_Asset.renderPipelineRayTracingResources, sharedRTManager, this);
            }
            return m_SimpleDenoiser;
        }

        internal SSGIDenoiser GetSSGIDenoiser()
        {
            if (m_SSGIDenoiser == null)
            {
                m_SSGIDenoiser = new SSGIDenoiser();
                m_SSGIDenoiser.Init(m_Asset.renderPipelineResources, sharedRTManager, this);
            }
            return m_SSGIDenoiser;
        }

        internal HDDiffuseDenoiser GetDiffuseDenoiser()
        {
            if (m_DiffuseDenoiser == null)
            {
                m_DiffuseDenoiser = new HDDiffuseDenoiser();
                m_DiffuseDenoiser.Init(m_Asset.renderPipelineResources, m_Asset.renderPipelineRayTracingResources, sharedRTManager, this);
            }
            return m_DiffuseDenoiser;
        }

        internal HDReflectionDenoiser GetReflectionDenoiser()
        {
            if (m_ReflectionDenoiser == null)
            {
                m_ReflectionDenoiser = new HDReflectionDenoiser();
                m_ReflectionDenoiser.Init(m_Asset.renderPipelineRayTracingResources, sharedRTManager, this);
            }
            return m_ReflectionDenoiser;
        }

        internal HDDiffuseShadowDenoiser GetDiffuseShadowDenoiser()
        {
            if (m_DiffuseShadowDenoiser == null)
            {
                m_DiffuseShadowDenoiser = new HDDiffuseShadowDenoiser();
                m_DiffuseShadowDenoiser.Init(m_Asset.renderPipelineRayTracingResources, sharedRTManager, this);
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

        internal RTHandle AllocateBuffer(InternalRayTracingBuffers bufferID)
        {
            switch (bufferID)
            {
                case InternalRayTracingBuffers.Direction:
                {
                    m_RayTracingDirectionBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true,useMipMap: false, name: "RaytracingDirectionBuffer");
                    return m_RayTracingDirectionBuffer;
                }
                case InternalRayTracingBuffers.Distance:
                {
                    m_RayTracingDistanceBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RaytracingDistanceBuffer");
                    return m_RayTracingDistanceBuffer;
                }
                case InternalRayTracingBuffers.R0:
                {
                    m_RayTracingIntermediateBufferR0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8_SNorm, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RayTracingIntermediateBufferR0");
                    return m_RayTracingIntermediateBufferR0;
                }
                case InternalRayTracingBuffers.R1:
                {
                    m_RayTracingIntermediateBufferR1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8_SNorm, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RayTracingIntermediateBufferR1");
                    return m_RayTracingIntermediateBufferR1;
                }
                case InternalRayTracingBuffers.RG0:
                {
                    m_RayTracingIntermediateBufferRG0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RayTracingIntermediateBufferRG0");
                    return m_RayTracingIntermediateBufferRG0;
                }
                case InternalRayTracingBuffers.RG1:
                {
                    m_RayTracingIntermediateBufferRG1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RayTracingIntermediateBufferRG1");
                    return m_RayTracingIntermediateBufferRG1;
                }
                case InternalRayTracingBuffers.RGBA0:
                {
                    m_RayTracingIntermediateBufferRGBA0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RayTracingIntermediateBufferRGBA0");
                    return m_RayTracingIntermediateBufferRGBA0;
                }
                case InternalRayTracingBuffers.RGBA1:
                {
                    m_RayTracingIntermediateBufferRGBA1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RayTracingIntermediateBufferRGBA1");
                    return m_RayTracingIntermediateBufferRGBA1;
                }
                case InternalRayTracingBuffers.RGBA2:
                {
                    m_RayTracingIntermediateBufferRGBA2 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RayTracingIntermediateBufferRGBA2");
                    return m_RayTracingIntermediateBufferRGBA2;
                }
                case InternalRayTracingBuffers.RGBA3:
                {
                    m_RayTracingIntermediateBufferRGBA3 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RayTracingIntermediateBufferRGBA3");
                    return m_RayTracingIntermediateBufferRGBA3;
                }
                case InternalRayTracingBuffers.RGBA4:
                {
                    m_RayTracingIntermediateBufferRGBA4 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RayTracingIntermediateBufferRGBA4");
                    return m_RayTracingIntermediateBufferRGBA4;
                }
                default:
                    return null;
            }
        }

        internal RTHandle GetRayTracingBuffer(InternalRayTracingBuffers bufferID)
        {
            switch (bufferID)
            {
                case InternalRayTracingBuffers.Distance:
                    return m_RayTracingDistanceBuffer != null ? m_RayTracingDistanceBuffer : AllocateBuffer(InternalRayTracingBuffers.Distance);
                case InternalRayTracingBuffers.Direction:
                    return m_RayTracingDirectionBuffer != null ? m_RayTracingDirectionBuffer : AllocateBuffer(InternalRayTracingBuffers.Direction);
                case InternalRayTracingBuffers.R0:
                    return m_RayTracingIntermediateBufferR0 != null ? m_RayTracingIntermediateBufferR0 : AllocateBuffer(InternalRayTracingBuffers.R0);
                case InternalRayTracingBuffers.R1:
                    return m_RayTracingIntermediateBufferR1 != null ? m_RayTracingIntermediateBufferR1 : AllocateBuffer(InternalRayTracingBuffers.R1);
                case InternalRayTracingBuffers.RG0:
                    return m_RayTracingIntermediateBufferRG0 != null ? m_RayTracingIntermediateBufferRG0 : AllocateBuffer(InternalRayTracingBuffers.RG0);
                case InternalRayTracingBuffers.RG1:
                    return m_RayTracingIntermediateBufferRG1 != null ? m_RayTracingIntermediateBufferRG1 : AllocateBuffer(InternalRayTracingBuffers.RG1);
                case InternalRayTracingBuffers.RGBA0:
                    return m_RayTracingIntermediateBufferRGBA0 != null ? m_RayTracingIntermediateBufferRGBA0 : AllocateBuffer(InternalRayTracingBuffers.RGBA0);
                case InternalRayTracingBuffers.RGBA1:
                    return m_RayTracingIntermediateBufferRGBA1 != null ? m_RayTracingIntermediateBufferRGBA1 : AllocateBuffer(InternalRayTracingBuffers.RGBA1);
                case InternalRayTracingBuffers.RGBA2:
                    return m_RayTracingIntermediateBufferRGBA2 != null ? m_RayTracingIntermediateBufferRGBA2 : AllocateBuffer(InternalRayTracingBuffers.RGBA2);
                case InternalRayTracingBuffers.RGBA3:
                    return m_RayTracingIntermediateBufferRGBA3 != null ? m_RayTracingIntermediateBufferRGBA3 : AllocateBuffer(InternalRayTracingBuffers.RGBA3);
                case InternalRayTracingBuffers.RGBA4:
                    return m_RayTracingIntermediateBufferRGBA4 != null ? m_RayTracingIntermediateBufferRGBA4 : AllocateBuffer(InternalRayTracingBuffers.RGBA4);
                default:
                    return null;
            }
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
