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

        // Used to detect material changes for Path Tracing
        Dictionary<int, int> m_MaterialCRCs = new Dictionary<int, int>();
        bool m_MaterialsDirty = false;

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
            if (m_RayTracingDistanceBuffer != null)
                RTHandles.Release(m_RayTracingDistanceBuffer);
            if (m_RayTracingDirectionBuffer != null)
                RTHandles.Release(m_RayTracingDirectionBuffer);

            if (m_RayTracingIntermediateBufferR0 != null)
                RTHandles.Release(m_RayTracingIntermediateBufferR0);
            if (m_RayTracingIntermediateBufferR1 != null)
                RTHandles.Release(m_RayTracingIntermediateBufferR1);
            if (m_RayTracingIntermediateBufferRG0 != null)
                RTHandles.Release(m_RayTracingIntermediateBufferRG0);
            if (m_RayTracingIntermediateBufferRG1 != null)
                RTHandles.Release(m_RayTracingIntermediateBufferRG1);
            if (m_RayTracingIntermediateBufferRGBA0 != null)
                RTHandles.Release(m_RayTracingIntermediateBufferRGBA0);
            if (m_RayTracingIntermediateBufferRGBA1 != null)
                RTHandles.Release(m_RayTracingIntermediateBufferRGBA1);
            if (m_RayTracingIntermediateBufferRGBA2 != null)
                RTHandles.Release(m_RayTracingIntermediateBufferRGBA2);
            if (m_RayTracingIntermediateBufferRGBA3 != null)
                RTHandles.Release(m_RayTracingIntermediateBufferRGBA3);
            if (m_RayTracingIntermediateBufferRGBA4 != null)
                RTHandles.Release(m_RayTracingIntermediateBufferRGBA4);

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

            // Get the layer of this object
            int objectLayerValue = 1 << currentRenderer.gameObject.layer;

            // We need to build the instance flag for this renderer
            uint instanceFlag = 0x00;

            bool singleSided = false;
            bool materialIsOnlyTransparent = true;
            bool hasTransparentSubMaterial = false;

            for (int meshIdx = 0; meshIdx < numSubMeshes; ++meshIdx)
            {
                // Intially we consider the potential mesh as invalid
                bool validMesh = false;
                if (materialArray.Count > meshIdx)
                {
                    // Grab the material for the current sub-mesh
                    Material currentMaterial = materialArray[meshIdx];

                    // The material is transparent if either it has the requested keyword or is in the transparent queue range
                    if (currentMaterial != null)
                    {
                        // Mesh is valid given that all requirements are ok
                        validMesh = true;
                        subMeshFlagArray[meshIdx] = true;

                        // Is the sub material transparent?
                        subMeshTransparentArray[meshIdx] = currentMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                        || (HDRenderQueue.k_RenderQueue_Transparent.lowerBound <= currentMaterial.renderQueue
                        && HDRenderQueue.k_RenderQueue_Transparent.upperBound >= currentMaterial.renderQueue);

                        // aggregate the transparency info
                        materialIsOnlyTransparent &= subMeshTransparentArray[meshIdx];
                        hasTransparentSubMaterial |= subMeshTransparentArray[meshIdx];

                        // Is the material alpha tested?
                        subMeshCutoffArray[meshIdx] = currentMaterial.IsKeywordEnabled("_ALPHATEST_ON")
                        || (HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.lowerBound <= currentMaterial.renderQueue
                        && HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.upperBound >= currentMaterial.renderQueue);

                        // Force it to be non single sided if it has the keyword if there is a reason
                        bool doubleSided = currentMaterial.doubleSidedGI || currentMaterial.IsKeywordEnabled("_DOUBLESIDED_ON");
                        singleSided |= !doubleSided;

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

                // If the mesh was not valid, exclude it
                if (!validMesh)
                {
                    subMeshFlagArray[meshIdx] = false;
                    subMeshCutoffArray[meshIdx] = false;
                    singleSided = true;
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

            // Propagate the opacity mask only if all submaterials are opaque
            if (!hasTransparentSubMaterial)
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
                // Raise the Screen Space Reflection if needed
                instanceFlag |= ((reflLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.Reflection) : 0x00;
            }

            if (giEnabled && !materialIsOnlyTransparent && meshIsVisible)
            {
                // Raise the Global Illumination if needed
                instanceFlag |= ((giLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.GlobalIllumination) : 0x00;
            }

            if (recursiveEnabled && meshIsVisible)
            {
                // Raise the Global Illumination if needed
                instanceFlag |= ((rrLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.RecursiveRendering) : 0x00;
            }

            if (pathTracingEnabled && meshIsVisible)
            {
                // Raise the Global Illumination if needed
                instanceFlag |= ((ptLayerValue & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.PathTracing) : 0x00;
            }

            // If the object was not referenced
            if (instanceFlag == 0) return AccelerationStructureStatus.Added;

            // Add it to the acceleration structure
            m_CurrentRAS.AddInstance(currentRenderer, subMeshMask: subMeshFlagArray, subMeshTransparencyFlags: subMeshCutoffArray, enableTriangleCulling: singleSided, mask: instanceFlag);

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
            ScreenSpaceReflection reflSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
            PathTracing pathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();

            // We need to process the emissive meshes of the rectangular area lights
            for (var i = 0; i < m_RayTracingLights.hdRectLightArray.Count; i++)
            {
                // Fetch the current renderer of the rectangular area light (if any)
                MeshRenderer currentRenderer = m_RayTracingLights.hdRectLightArray[i].emissiveMeshRenderer;

                // If there is none it means that there is no emissive mesh for this light
                if (currentRenderer == null) continue;

                // This objects should be included into the RAS
                AddInstanceToRAS(currentRenderer,
                                rayTracedShadow,
                                aoSettings.rayTracing.value, aoSettings.layerMask.value,
                                reflSettings.rayTracing.value, reflSettings.layerMask.value,
                                giSettings.rayTracing.value, giSettings.layerMask.value,
                                recursiveSettings.enable.value, recursiveSettings.layerMask.value,
                                pathTracingSettings.enable.value, pathTracingSettings.layerMask.value);
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
                                rayTracedShadow,
                                aoSettings.rayTracing.value, aoSettings.layerMask.value,
                                reflSettings.rayTracing.value, reflSettings.layerMask.value,
                                giSettings.rayTracing.value, giSettings.layerMask.value,
                                recursiveSettings.enable.value, recursiveSettings.layerMask.value,
                                pathTracingSettings.enable.value, pathTracingSettings.layerMask.value);
            }

            // Check if the amount of materials being tracked has changed
            m_MaterialsDirty |= (matCount != m_MaterialCRCs.Count);

            // build the acceleration structure
            m_CurrentRAS.Build();

            // tag the structures as valid
            m_ValidRayTracingState = true;
        }

        internal bool ValidRayTracingHistory(HDCamera hdCamera)
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
            return hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)m_FrameCount % 8;
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
                m_RayTracingLightCluster.CullForRayTracing(cmd, hdCamera, m_RayTracingLights);
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
