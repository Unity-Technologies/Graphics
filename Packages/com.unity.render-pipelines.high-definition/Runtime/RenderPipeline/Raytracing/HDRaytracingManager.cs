using System;
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
        PathTracing = 0x80,
        All = Opaque | CastShadow | AmbientOcclusion | Reflection | GlobalIllumination | RecursiveRendering | PathTracing,
    }

    /// <summary>
    /// Flags returned when trying to add a renderer into the ray tracing acceleration structure.
    /// </summary>
    public enum AccelerationStructureStatus
    {
        /// <summary>Initial flag state.</summary>
        Clear = 0x0,
        /// <summary>Flag that indicates that the renderer was successfully added to the ray tracing acceleration structure.</summary>
        Added = 0x1,
        /// <summary>Flag that indicates that the renderer was excluded from the ray tracing acceleration structure.</summary>
        Excluded = 0x02,
        /// <summary>Flag that indicates that the renderer was added to the ray tracing acceleration structure, but it had transparent and opaque sub-meshes.</summary>
        TransparencyIssue = 0x04,
        /// <summary>Flag that indicates that the renderer was not included into the ray tracing acceleration structure because of a missing material</summary>
        NullMaterial = 0x08,
        /// <summary>Flag that indicates that the renderer was not included into the ray tracing acceleration structure because of a missing mesh</summary>
        MissingMesh = 0x10
    }

    public partial class HDRenderPipeline
    {
        // Data used for runtime evaluation
        static readonly string m_RTASDebugRTKernel = "RTASDebug";
        HDRTASManager m_RTASManager;
        HDRaytracingLightCluster m_RayTracingLightCluster;
        HDRayTracingLights m_RayTracingLights = new HDRayTracingLights();
        bool m_ValidRayTracingState = false;
        bool m_ValidRayTracingCluster = false;
        bool m_ValidRayTracingClusterCulling = false;
        bool m_RayTracedShadowsRequired = false;
        bool m_RayTracedContactShadowsRequired = false;

        // Denoisers
        HDTemporalFilter m_TemporalFilter;
        HDDiffuseDenoiser m_DiffuseDenoiser;
        HDReflectionDenoiser m_ReflectionDenoiser;
        HDDiffuseShadowDenoiser m_DiffuseShadowDenoiser;

        // Ray-count manager data
        RayCountManager m_RayCountManager;

        // Static variables used for the dirtiness and manual rtas management
        const int maxNumSubMeshes = 32;
        static RayTracingSubMeshFlags[] subMeshFlagArray = new RayTracingSubMeshFlags[maxNumSubMeshes];
        static List<Material> materialArray = new List<Material>(maxNumSubMeshes);
        static Dictionary<int, int> m_MaterialCRCs = new Dictionary<int, int>();

        // Global shader variables ray tracing lightloop constant buffer
        ShaderVariablesRaytracingLightLoop m_ShaderVariablesRaytracingLightLoopCB = new ShaderVariablesRaytracingLightLoop();

        internal bool GetMaterialDirtiness(HDCamera hdCamera)
        {
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
#if UNITY_EDITOR
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic || hdCamera.camera.cameraType == CameraType.SceneView)
#else
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic)
#endif
            {
                return m_RTASManager.materialsDirty;
            }
            else
            {
                return hdCamera.materialsDirty;
            }
        }

        internal void ResetMaterialDirtiness(HDCamera hdCamera)
        {
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
#if UNITY_EDITOR
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic || hdCamera.camera.cameraType == CameraType.SceneView)
#else
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic)
#endif
            {
                m_RTASManager.materialsDirty = false;
            }
            else
            {
                hdCamera.materialsDirty = false;
            }
        }

        internal bool GetTransformDirtiness(HDCamera hdCamera)
        {
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
#if UNITY_EDITOR
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic || hdCamera.camera.cameraType == CameraType.SceneView)
#else
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic)
#endif
            {
                return m_RTASManager.transformsDirty;
            }
            else
            {
                return hdCamera.transformsDirty;
            }
        }

        internal void ResetTransformDirtiness(HDCamera hdCamera)
        {
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
#if UNITY_EDITOR
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic || hdCamera.camera.cameraType == CameraType.SceneView)
#else
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic)
#endif
            {
                m_RTASManager.transformsDirty = false;
            }
            else
            {
                hdCamera.transformsDirty = false;
            }
        }

        internal void InitRayTracingManager()
        {
            // Init the ray count manager
            m_RayCountManager = new RayCountManager();
            m_RayCountManager.Init(m_GlobalSettings.renderPipelineRayTracingResources);

            // Initialize the light cluster
            m_RayTracingLightCluster = new HDRaytracingLightCluster();
            m_RayTracingLightCluster.Initialize(this);

            // Initialize the RTAS manager
            m_RTASManager = new HDRTASManager();
            m_RTASManager.Initialize();
        }

        internal void ReleaseRayTracingManager()
        {
            if (m_RTASManager != null)
                m_RTASManager.ReleaseResources();

            if (m_RayTracingLightCluster != null)
                m_RayTracingLightCluster.ReleaseResources();
            if (m_RayCountManager != null)
                m_RayCountManager.Release();

            if (m_ReflectionDenoiser != null)
                m_ReflectionDenoiser.Release();
            if (m_TemporalFilter != null)
                m_TemporalFilter.Release();
            if (m_DiffuseShadowDenoiser != null)
                m_DiffuseShadowDenoiser.Release();
            if (m_DiffuseDenoiser != null)
                m_DiffuseDenoiser.Release();
        }

        static bool IsValidRayTracedMaterial(Material currentMaterial)
        {
            if (currentMaterial == null || currentMaterial.shader == null)
                return false;

            // For the time being, we only consider non-decal HDRP materials as valid
            return currentMaterial.GetTag("RenderPipeline", false) == "HDRenderPipeline" && !DecalSystem.IsDecalMaterial(currentMaterial); ;
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

        private static bool UpdateMaterialCRC(int matInstanceId, int matCRC)
        {
            int matPrevCRC;
            if (m_MaterialCRCs.TryGetValue(matInstanceId, out matPrevCRC))
            {
                m_MaterialCRCs[matInstanceId] = matCRC;
                return (matCRC != matPrevCRC);
            }
            else
            {
                m_MaterialCRCs.Add(matInstanceId, matCRC);
                return true;
            }
        }

        /// <summary>
        /// Function that adds a renderer to a ray tracing acceleration structure.
        /// </summary>
        /// <param name="targetRTAS">Ray Tracing Acceleration structure the renderer should be added to.</param>
        /// <param name="currentRenderer">The renderer that should be added to the RTAS.</param>
        /// <param name="effectsParameters">Structure defining the enabled ray tracing and path tracing effects for a camera.</param>
        /// <param name="transformDirty">Flag that indicates if the renderer's transform has changed.</param>
        /// <param name="materialsDirty">Flag that indicates if any of the renderer's materials have changed.</param>
        /// <returns></returns>
        public static AccelerationStructureStatus AddInstanceToRAS(RayTracingAccelerationStructure targetRTAS, Renderer currentRenderer, HDEffectsParameters effectsParameters, ref bool transformDirty, ref bool materialsDirty)
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

            // Let's clamp the number of sub-meshes to avoid throwing an unwanted error
            numSubMeshes = Mathf.Min(numSubMeshes, maxNumSubMeshes);

            // Get the layer of this object
            int objectLayerValue = 1 << currentRenderer.gameObject.layer;

            // We need to build the instance flag for this renderer
            uint instanceFlag = 0x00;

            bool doubleSided = false;
            bool materialIsOnlyTransparent = true;
            bool hasTransparentSubMaterial = false;

            // We disregard the ray traced shadows option when in Path Tracing
            bool rayTracedShadow = effectsParameters.shadows && !effectsParameters.pathTracing;

            // Deactivate Path Tracing if the object does not belong to the path traced layer(s)
            bool pathTracing = effectsParameters.pathTracing && (bool)((effectsParameters.ptLayerMask & objectLayerValue) != 0);

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
                        if (!materialsDirty)
                        {
                            materialsDirty |= UpdateMaterialCRC(currentMaterial.GetInstanceID(), currentMaterial.ComputeCRC());
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

            if (rayTracedShadow || pathTracing)
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

            if (effectsParameters.ambientOcclusion && !materialIsOnlyTransparent && meshIsVisible)
            {
                // Raise the Ambient Occlusion flag if needed
                instanceFlag |= ((effectsParameters.aoLayerMask & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.AmbientOcclusion) : 0x00;
            }

            if (effectsParameters.reflections && !materialIsOnlyTransparent && meshIsVisible)
            {
                // Raise the Screen Space Reflection flag if needed
                instanceFlag |= ((effectsParameters.reflLayerMask & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.Reflection) : 0x00;
            }

            if (effectsParameters.globalIllumination && !materialIsOnlyTransparent && meshIsVisible)
            {
                // Raise the Global Illumination flag if needed
                instanceFlag |= ((effectsParameters.giLayerMask & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.GlobalIllumination) : 0x00;
            }

            if (effectsParameters.recursiveRendering && meshIsVisible)
            {
                // Raise the Recursive Rendering flag if needed
                instanceFlag |= ((effectsParameters.recursiveLayerMask & objectLayerValue) != 0) ? (uint)(RayTracingRendererFlag.RecursiveRendering) : 0x00;
            }

            if (effectsParameters.pathTracing && meshIsVisible)
            {
                // Raise the Path Tracing flag if needed
                instanceFlag |= (uint)(RayTracingRendererFlag.PathTracing);
            }

            // If the object was not referenced
            if (instanceFlag == 0) return AccelerationStructureStatus.Added;

            // Add it to the acceleration structure
            targetRTAS.AddInstance(currentRenderer, subMeshFlags: subMeshFlagArray, enableTriangleCulling: !doubleSided, mask: instanceFlag);

            // Indicates that a transform has changed in our scene (mesh or light)
            transformDirty |= currentRenderer.transform.hasChanged;
            currentRenderer.transform.hasChanged = false;

            // return the status
            return (!materialIsOnlyTransparent && hasTransparentSubMaterial) ? AccelerationStructureStatus.TransparencyIssue : AccelerationStructureStatus.Added;
        }

        void CollectLightsForRayTracing(HDCamera hdCamera, ref bool transformDirty)
        {
            // fetch all the lights in the scene
            HDLightRenderDatabase lightEntities = HDLightRenderDatabase.instance;
            for (int lightIdx = 0; lightIdx < lightEntities.lightCount; ++lightIdx)
            {
                HDLightRenderEntity lightRenderEntity = lightEntities.lightEntities[lightIdx];
                HDAdditionalLightData hdLight = lightEntities.hdAdditionalLightData[lightIdx];
                if (hdLight != null && hdLight.enabled && hdLight != HDUtils.s_DefaultHDAdditionalLightData)
                {
                    Light light = hdLight.gameObject.GetComponent<Light>();
                    // If the light is null or disabled, skip it
                    if (light == null || !light.enabled)
                        continue;

                    // If the light is flagged as baked and has been effectively been baked, skip it, except if we are path tracing
                    bool isPathTracingEnabled = hdCamera.volumeStack.GetComponent<PathTracing>().enable.value;
                    if (!isPathTracingEnabled && light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked && light.bakingOutput.isBaked)
                        continue;

                    // If this light should not be included when ray tracing is active on the camera, skip it
                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && !hdLight.includeForRayTracing)
                        continue;

                    // Flag that needs to be overriden by the light and tells us if the light will need the RTAS
                    bool hasRayTracedShadows = false;

                    // Indicates that a transform has changed in our scene (mesh or light)
                    transformDirty |= hdLight.transform.hasChanged;
                    hdLight.transform.hasChanged = false;

                    switch (hdLight.type)
                    {
                        case HDLightType.Directional:
                        {
                            hasRayTracedShadows = hdLight.ShadowsEnabled() && hdLight.useScreenSpaceShadows && hdLight.useRayTracedShadows;
                            m_RayTracingLights.hdDirectionalLightArray.Add(hdLight);
                        }
                        break;
                        case HDLightType.Point:
                        case HDLightType.Spot:
                        {
                            hasRayTracedShadows = hdLight.ShadowsEnabled() && hdLight.useRayTracedShadows;
                            m_RayTracingLights.hdPointLightArray.Add(lightRenderEntity);
                        }
                        break;
                        case HDLightType.Area:
                        {
                            hasRayTracedShadows = hdLight.ShadowsEnabled() && hdLight.useRayTracedShadows;
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

                    // Check if there is a ray traced shadow in the scene
                    m_RayTracedShadowsRequired |= hasRayTracedShadows;
                    m_RayTracedContactShadowsRequired |= (hdLight.useContactShadow.@override && hdLight.rayTraceContactShadow);
                }
            }

            // Add the lights to the structure
            m_RayTracingLights.hdLightEntityArray.AddRange(m_RayTracingLights.hdPointLightArray);
            m_RayTracingLights.hdLightEntityArray.AddRange(m_RayTracingLights.hdLineLightArray);
            m_RayTracingLights.hdLightEntityArray.AddRange(m_RayTracingLights.hdRectLightArray);

            // Process the lights
            HDAdditionalReflectionData[] reflectionProbeArray = UnityEngine.GameObject.FindObjectsByType<HDAdditionalReflectionData>(FindObjectsSortMode.None);
            for (int reflIdx = 0; reflIdx < reflectionProbeArray.Length; ++reflIdx)
            {
                HDAdditionalReflectionData reflectionProbe = reflectionProbeArray[reflIdx];
                // Add it to the list if enabled
                // Skip the probe if the probe has never rendered (in real time cases) or if texture is null
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
        }

        /// <summary>
        /// Function that returns the ray tracing and path tracing effects that are enabled for a given camera.
        /// </summary>
        /// <param name="hdCamera">The input camera</param>
        /// <param name="rayTracedShadows">Flag that defines if at least one light has ray traced shadows.</param>
        /// <param name="rayTracedContactShadows">Flag that defines if at least one light has ray traced contact shadows</param>
        /// <returns>HDEffectsParameters type.</returns>
        public static HDEffectsParameters EvaluateEffectsParameters(HDCamera hdCamera, bool rayTracedShadows, bool rayTracedContactShadows)
        {
            HDEffectsParameters parameters = new HDEffectsParameters();

            // Aggregate the shadow requirements
            parameters.shadows = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows) && (rayTracedShadows || rayTracedContactShadows);

            // Aggregate the ambient occlusion parameters
            var aoSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceAmbientOcclusion>();
            parameters.ambientOcclusion = aoSettings.rayTracing.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSAO);
            parameters.aoLayerMask = aoSettings.layerMask.value;

            // Aggregate the reflections parameters
            ScreenSpaceReflection reflSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            bool opaqueReflections = hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR) && reflSettings.enabled.value;
            bool transparentReflections  = hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentSSR) && reflSettings.enabledTransparent.value;
            parameters.reflections = ScreenSpaceReflection.RayTracingActive(reflSettings) && (opaqueReflections || transparentReflections);
            parameters.reflLayerMask = reflSettings.layerMask.value;

            // Aggregate the global illumination parameters
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            parameters.globalIllumination = giSettings.enable.value && GlobalIllumination.RayTracingActive(giSettings) && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSGI);
            parameters.giLayerMask = giSettings.layerMask.value;

            // Aggregate the global illumination parameters
            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
            parameters.recursiveRendering = recursiveSettings.enable.value;
            parameters.recursiveLayerMask = recursiveSettings.layerMask.value;

            // Aggregate the sub surface parameters
            SubSurfaceScattering sssSettings = hdCamera.volumeStack.GetComponent<SubSurfaceScattering>();
            parameters.subSurface = sssSettings.rayTracing.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering);

            // Aggregate the path parameters
            PathTracing pathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();
            parameters.pathTracing = pathTracingSettings.enable.value;
            parameters.ptLayerMask = pathTracingSettings.layerMask.value;

            // We need to check if at least one effect will require the acceleration structure
            parameters.rayTracingRequired = parameters.ambientOcclusion || parameters.reflections
                || parameters.globalIllumination || parameters.recursiveRendering || parameters.subSurface
                || parameters.pathTracing || parameters.shadows;

            // Return the result
            return parameters;
        }

        internal void BuildRayTracingAccelerationStructure(HDCamera hdCamera)
        {
            // Resets the rtas manager
            m_RTASManager.Reset();

            // Resets the light lists
            m_RayTracingLights.Reset();

            // Reset all the flags
            m_ValidRayTracingState = false;
            m_ValidRayTracingCluster = false;
            m_ValidRayTracingClusterCulling = false;
            m_RayTracedShadowsRequired = false;
            m_RayTracedContactShadowsRequired = false;

            // If the camera does not have a ray tracing frame setting or it is a preview camera (due to the fact that the sphere does not exist as a game object we can't create the RTAS) we do not want to build a RTAS
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                return;

            // Collect the lights
            CollectLightsForRayTracing(hdCamera, ref m_RTASManager.transformsDirty);

            // Evaluate the parameters of the effects
            HDEffectsParameters effectParameters = EvaluateEffectsParameters(hdCamera, m_RayTracedShadowsRequired, m_RayTracedContactShadowsRequired);

            if (!effectParameters.rayTracingRequired)
                return;

            // Grab the ray tracing settings
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
#if UNITY_EDITOR
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic || hdCamera.camera.cameraType == CameraType.SceneView)
#else
            if (rtSettings.buildMode.value == RTASBuildMode.Automatic)
#endif
            {
                // Cull the scene for the RTAS
                RayTracingInstanceCullingResults cullingResults = m_RTASManager.Cull(hdCamera, effectParameters);

                // Update the material dirtiness for the PT
                if (effectParameters.pathTracing)
                {
                    m_RTASManager.transformsDirty |= cullingResults.transformsChanged;
                    for (int i = 0; i < cullingResults.materialsCRC.Length; i++)
                    {
                        RayTracingInstanceMaterialCRC matCRC = cullingResults.materialsCRC[i];
                        m_RTASManager.materialsDirty |= UpdateMaterialCRC(matCRC.instanceID, matCRC.crc);
                    }
                }

                // Build the ray tracing acceleration structure
                m_RTASManager.Build(hdCamera);

                // tag the structures as valid
                m_ValidRayTracingState = true;
            }
            else
            {
                // If the user fed a non null ray tracing acceleration structure, then we are all set.
                if (hdCamera.rayTracingAccelerationStructure != null)
                    m_ValidRayTracingState = true;
            }
        }

        class RTASDebugPassData
        {
            // Camera data
            public int actualWidth;
            public int actualHeight;
            public int viewCount;

            // Evaluation parameters
            public int debugMode;
            public uint layerMask;
            public Matrix4x4 pixelCoordToViewDirWS;

            // Other parameters
            public RayTracingShader debugRTASRT;
            public RayTracingAccelerationStructure rayTracingAccelerationStructure;

            // Output
            public TextureHandle outputTexture;
        }

        static uint LayerFromRTASDebugView(RTASDebugView debugView, HDCamera hdCamera)
        {
            switch (debugView)
            {
                case RTASDebugView.Shadows:
                {
                    return (uint)RayTracingRendererFlag.CastShadow;
                }
                case RTASDebugView.AmbientOcclusion:
                {
                    return (uint)RayTracingRendererFlag.AmbientOcclusion;
                }
                case RTASDebugView.GlobalIllumination:
                {
                    return (uint)RayTracingRendererFlag.GlobalIllumination;
                }
                case RTASDebugView.Reflections:
                {
                    return (uint)RayTracingRendererFlag.Reflection;
                }
                case RTASDebugView.RecursiveRayTracing:
                {
                    return (uint)RayTracingRendererFlag.RecursiveRendering;
                }
                case RTASDebugView.PathTracing:
                {
                    return (uint)RayTracingRendererFlag.PathTracing;
                }
                default:
                {
                    return (uint)RayTracingRendererFlag.All;
                }
            }
        }

        internal void EvaluateRTASDebugView(RenderGraph renderGraph, HDCamera hdCamera)
        {
            // If the ray tracing state is not valid, we cannot evaluate the debug view
            if (!m_ValidRayTracingState)
                return;

            using (var builder = renderGraph.AddRenderPass<RTASDebugPassData>("Debug view of the RTAS", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingBuildAccelerationStructureDebug)))
            {
                builder.EnableAsyncCompute(false);

                // Camera data
                passData.actualWidth = hdCamera.actualWidth;
                passData.actualHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.debugMode = (int)m_CurrentDebugDisplaySettings.data.rtasDebugMode;
                passData.layerMask = LayerFromRTASDebugView(m_CurrentDebugDisplaySettings.data.rtasDebugView, hdCamera);
                passData.pixelCoordToViewDirWS = hdCamera.mainViewConstants.pixelCoordToViewDirWS;

                // Other parameters
                passData.debugRTASRT = m_GlobalSettings.renderPipelineRayTracingResources.rtasDebug;
                passData.rayTracingAccelerationStructure = RequestAccelerationStructure(hdCamera);

                // Depending of if we will have to denoise (or not), we need to allocate the final format, or a bigger texture
                passData.outputTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "RTAS Debug" }));

                builder.SetRenderFunc(
                    (RTASDebugPassData data, RenderGraphContext ctx) =>
                    {
                        // Define the shader pass to use for the reflection pass
                        ctx.cmd.SetRayTracingShaderPass(data.debugRTASRT, "DebugDXR");

                        // Set the acceleration structure for the pass
                        ctx.cmd.SetRayTracingAccelerationStructure(data.debugRTASRT, HDShaderIDs._RaytracingAccelerationStructureName, data.rayTracingAccelerationStructure);

                        // Layer mask
                        ctx.cmd.SetRayTracingIntParam(data.debugRTASRT, "_DebugMode", data.debugMode);
                        ctx.cmd.SetRayTracingIntParam(data.debugRTASRT, "_LayerMask", (int)data.layerMask);
                        ctx.cmd.SetRayTracingMatrixParam(data.debugRTASRT, HDShaderIDs._PixelCoordToViewDirWS, data.pixelCoordToViewDirWS);

                        // Set the output texture
                        ctx.cmd.SetRayTracingTextureParam(data.debugRTASRT, "_OutputDebugBuffer", data.outputTexture);

                        // Evaluate the debug view
                        ctx.cmd.DispatchRays(data.debugRTASRT, m_RTASDebugRTKernel, (uint)data.actualWidth, (uint)data.actualHeight, (uint)data.viewCount);
                    });

                // Use the debug texture to do the full screen debug
                PushFullScreenDebugTexture(renderGraph, passData.outputTexture, FullScreenDebugMode.RayTracingAccelerationStructure);
            }
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
#if UNITY_HDRP_DXR_TESTS_DEFINE
            if (Application.isPlaying)
                return 0.0f;
            else
#endif
                return 1.0f;
        }

        internal bool RayTracingHalfResAllowed()
        {
            return DynamicResolutionHandler.instance.GetCurrentScale() >= (currentPlatformRenderPipelineSettings.dynamicResolutionSettings.rayTracingHalfResThreshold / 100.0f);
        }

        internal static Vector4 EvaluateRayTracingHistorySizeAndScale(HDCamera hdCamera, RTHandle buffer)
        {
            return new Vector4(hdCamera.historyRTHandleProperties.previousViewportSize.x,
                                hdCamera.historyRTHandleProperties.previousViewportSize.y,
                                (float)hdCamera.historyRTHandleProperties.previousViewportSize.x / buffer.rt.width,
                                (float)hdCamera.historyRTHandleProperties.previousViewportSize.y / buffer.rt.height);
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

        internal RayTracingAccelerationStructure RequestAccelerationStructure(HDCamera hdCamera)
        {
            if (m_ValidRayTracingState)
            {
                RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
#if UNITY_EDITOR
                if (rtSettings.buildMode.value == RTASBuildMode.Automatic || hdCamera.camera.cameraType == CameraType.SceneView)
#else
                if (rtSettings.buildMode.value == RTASBuildMode.Automatic)
#endif
                    return m_RTASManager.rtas;
                else
                    return hdCamera.rayTracingAccelerationStructure;
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
            && (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows64
                || UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows
                || UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.GameCoreXboxSeries
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
