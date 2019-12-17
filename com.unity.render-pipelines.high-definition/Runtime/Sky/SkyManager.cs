using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public enum SkyResolution
    {
        SkyResolution128 = 128,
        SkyResolution256 = 256,
        SkyResolution512 = 512,
        SkyResolution1024 = 1024,
        SkyResolution2048 = 2048,
        SkyResolution4096 = 4096
    }

    public enum EnvironmentUpdateMode
    {
        OnChanged = 0,
        OnDemand,
        Realtime
    }

    public class BuiltinSkyParameters
    {
        public HDCamera                 hdCamera;
        public Matrix4x4                pixelCoordToViewDirMatrix;
        public Vector3                  worldSpaceCameraPos;
        public Matrix4x4                viewMatrix;
        public Vector4                  screenSize;
        public CommandBuffer            commandBuffer;
        public Light                    sunLight;
        public RTHandle                 colorBuffer;
        public RTHandle                 depthBuffer;
        public int                      frameIndex;
        public SkySettings              skySettings;

        public DebugDisplaySettings debugSettings;

        public static RenderTargetIdentifier nullRT = -1;
    }

    struct CachedSkyContext
    {
        public Type                 type;
        public SkyRenderingContext  renderingContext;
        public SkyRenderer          renderer;
        public int                  lastFrameUsed;
        public int                  hash;
        public int                  refCount;

        public void Reset()
        {
            // We keep around the renderer and the rendering context to avoid useless allocation if they get reused.
            lastFrameUsed = 0;
            hash = 0;
            refCount = 0;
            if (renderingContext != null)
                renderingContext.ClearAmbientProbe();
        }

        public void Cleanup()
        {
            Reset();

            if (renderer != null)
            {
                renderer.Cleanup();
                renderer = null;
            }
            if (renderingContext != null)
            {
                renderingContext.Cleanup();
                renderingContext = null;
            }
        }
    }

    class SkyManager
    {
        Material                m_StandardSkyboxMaterial; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Material                m_BlitCubemapMaterial;
        Material                m_OpaqueAtmScatteringMaterial;

        SphericalHarmonicsL2    m_BlackAmbientProbe = new SphericalHarmonicsL2();

        bool                    m_UpdateRequired = false;
        int                     m_Resolution;

        // Sky used for static lighting. It will be used for ambient lighting if Ambient Mode is set to Static (even when realtime GI is enabled)
        // It will also be used for lightmap and light probe baking
        SkyUpdateContext m_StaticLightingSky = new SkyUpdateContext();

        // This interpolation volume stack is used to interpolate the lighting override separately from the visual sky.
        // If a sky setting is present in this volume then it will be used for lighting override.
        public VolumeStack lightingOverrideVolumeStack { get; private set; }
        public LayerMask lightingOverrideLayerMask { get; private set; } = -1;

        static Dictionary<int, Type> m_SkyTypesDict = null;
        public static Dictionary<int, Type> skyTypesDict { get { if (m_SkyTypesDict == null) UpdateSkyTypes(); return m_SkyTypesDict; } }

        // This list will hold the static lighting sky that should be used for baking ambient probe.
        // In practice we will always use the last one registered but we use a list to be able to roll back to the previous one once the user deletes the superfluous instances.
        private static List<StaticLightingSky> m_StaticLightingSkies = new List<StaticLightingSky>();

        // Only show the procedural sky upgrade message once
        static bool         logOnce = true;

        MaterialPropertyBlock m_OpaqueAtmScatteringBlock;

#if UNITY_EDITOR
        // For Preview windows we want to have a 'fixed' sky, so we can display chrome metal and have always the same look
        HDRISky m_DefaultPreviewSky;
#endif

        // Shared resources for sky rendering.
        IBLFilterBSDF[]         m_IBLFilterArray;
        RTHandle                m_SkyboxBSDFCubemapIntermediate;
        Vector4                 m_CubemapScreenSize;
        Matrix4x4[]             m_facePixelCoordToViewDirMatrices = new Matrix4x4[6];
        Matrix4x4[]             m_CameraRelativeViewMatrices = new Matrix4x4[6];
        BuiltinSkyParameters    m_BuiltinParameters = new BuiltinSkyParameters();
        ComputeShader           m_ComputeAmbientProbeCS;
        readonly int            m_AmbientProbeOutputBufferParam = Shader.PropertyToID("_AmbientProbeOutputBuffer");
        readonly int            m_AmbientProbeInputCubemap = Shader.PropertyToID("_AmbientProbeInputCubemap");
        int                     m_ComputeAmbientProbeKernel;
        CubemapArray            m_BlackCubemapArray;

        // 2 by default: Static sky + one dynamic. Will grow if needed.
        DynamicArray<CachedSkyContext> m_CachedSkyContexts = new DynamicArray<CachedSkyContext>(2);

        int m_CurrentFrameIndex = -1;

        public SkyManager()
        {
#if UNITY_EDITOR
            UnityEditor.Lightmapping.bakeStarted += OnBakeStarted;
    #endif
        }

        ~SkyManager()
        {
#if UNITY_EDITOR
            UnityEditor.Lightmapping.bakeStarted -= OnBakeStarted;
#endif
        }

        internal static SkySettings GetSkySetting(VolumeStack stack)
        {
            var visualEnv = stack.GetComponent<VisualEnvironment>();
            int skyID = visualEnv.skyType.value;
            Type skyType;
            if (skyTypesDict.TryGetValue(skyID, out skyType))
            {
                return (SkySettings)stack.GetComponent(skyType);
            }
            else
            {
                if (skyID == (int)SkyType.Procedural && logOnce)
                {
                    Debug.LogError("You are using the deprecated Procedural Sky in your Scene. You can still use it but, to do so, you must install it separately. To do this, open the Package Manager window and import the 'Procedural Sky' sample from the HDRP package page, then close and re-open your project without saving.");
                    logOnce = false;
                }

                return null;
            }
        }

        static void UpdateSkyTypes()
        {
            if (m_SkyTypesDict == null)
            {
                m_SkyTypesDict = new Dictionary<int, Type>();

                var skyTypes = CoreUtils.GetAllTypesDerivedFrom<SkySettings>().Where(t => !t.IsAbstract);
                foreach (Type skyType in skyTypes)
                {
                    var uniqueIDs = skyType.GetCustomAttributes(typeof(SkyUniqueID), false);
                    if (uniqueIDs.Length == 0)
                    {
                        Debug.LogWarningFormat("Missing attribute SkyUniqueID on class {0}. Class won't be registered as an available sky.", skyType);
                    }
                    else
                    {
                        int uniqueID = ((SkyUniqueID)uniqueIDs[0]).uniqueID;
                        if (uniqueID == 0)
                        {
                            Debug.LogWarningFormat("0 is a reserved SkyUniqueID and is used in class {0}. Class won't be registered as an available sky.", skyType);
                            continue;
                        }

                        Type value;
                        if (m_SkyTypesDict.TryGetValue(uniqueID, out value))
                        {
                            Debug.LogWarningFormat("SkyUniqueID {0} used in class {1} is already used in class {2}. Class won't be registered as an available sky.", uniqueID, skyType, value);
                            continue;
                        }

                        m_SkyTypesDict.Add(uniqueID, skyType);
                    }
                }
            }
        }

        public void UpdateCurrentSkySettings(HDCamera hdCamera)
        {
            hdCamera.UpdateCurrentSky(this);
        }

        public void SetGlobalSkyData(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (IsCachedContextValid(hdCamera.lightingSky))
            {
                var renderer = m_CachedSkyContexts[hdCamera.lightingSky.cachedSkyRenderingContextId].renderer;
                if (renderer != null)
                {
                    m_BuiltinParameters.skySettings = hdCamera.lightingSky.skySettings;
                    renderer.SetGlobalSkyData(cmd, m_BuiltinParameters);
                }
            }
        }

#if UNITY_EDITOR
        internal HDRISky GetDefaultPreviewSkyInstance()
        {
            if (m_DefaultPreviewSky == null)
            {
                m_DefaultPreviewSky = ScriptableObject.CreateInstance<HDRISky>();
                m_DefaultPreviewSky.hdriSky.overrideState = true;
                m_DefaultPreviewSky.hdriSky.value = HDRenderPipeline.currentAsset?.renderPipelineResources?.textures?.defaultHDRISky;
            }

            return m_DefaultPreviewSky;
        }

#endif

        public void Build(HDRenderPipelineAsset hdAsset, RenderPipelineResources defaultResources, IBLFilterBSDF[] iblFilterBSDFArray)
        {
            var hdrp = HDRenderPipeline.defaultAsset;

            m_Resolution = (int)hdAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;
            m_IBLFilterArray = iblFilterBSDFArray;

            m_StandardSkyboxMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.skyboxCubemapPS);
            m_BlitCubemapMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitCubemapPS);

            m_OpaqueAtmScatteringMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.opaqueAtmosphericScatteringPS);
            m_OpaqueAtmScatteringBlock = new MaterialPropertyBlock();

            m_ComputeAmbientProbeCS = hdrp.renderPipelineResources.shaders.ambientProbeConvolutionCS;
            m_ComputeAmbientProbeKernel = m_ComputeAmbientProbeCS.FindKernel("AmbientProbeConvolution");

            lightingOverrideVolumeStack = VolumeManager.instance.CreateStack();
            lightingOverrideLayerMask = hdAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask;

            int resolution = (int)hdAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;
            m_SkyboxBSDFCubemapIntermediate = RTHandles.Alloc(resolution, resolution, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Cube, useMipMap: true, autoGenerateMips: false, filterMode: FilterMode.Trilinear, name: "SkyboxBSDFIntermediate");
            m_CubemapScreenSize = new Vector4((float)resolution, (float)resolution, 1.0f / (float)resolution, 1.0f / (float)resolution);

            var cubeProj = Matrix4x4.Perspective(90.0f, 1.0f, 0.01f, 1.0f);

            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                var worldToView = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...

                m_facePixelCoordToViewDirMatrices[i] = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, Vector2.zero, m_CubemapScreenSize, worldToView, true);
                m_CameraRelativeViewMatrices[i] = worldToView;
            }

            InitializeBlackCubemapArray();
        }

        void InitializeBlackCubemapArray()
        {
            if (m_BlackCubemapArray == null)
            {
                m_BlackCubemapArray = new CubemapArray(1, m_IBLFilterArray.Length, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    wrapModeV = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0,
                    name = "BlackCubemapArray"
                };

                Color32[] black = { new Color32(0, 0, 0, 0) };

                for (int element = 0; element < m_IBLFilterArray.Length; ++element)
                {
                    for (int i = 0; i < 6; i++)
                        m_BlackCubemapArray.SetPixels32(black, (CubemapFace)i, element);
                }

                m_BlackCubemapArray.Apply();
            }
        }


        public void Cleanup()
        {
            CoreUtils.Destroy(m_StandardSkyboxMaterial);
            CoreUtils.Destroy(m_BlitCubemapMaterial);
            CoreUtils.Destroy(m_OpaqueAtmScatteringMaterial);

            RTHandles.Release(m_SkyboxBSDFCubemapIntermediate);
            CoreUtils.Destroy(m_BlackCubemapArray);

            for (int i = 0; i < m_CachedSkyContexts.size; ++i)
                m_CachedSkyContexts[i].Cleanup();

#if UNITY_EDITOR
            CoreUtils.Destroy(m_DefaultPreviewSky);
#endif
        }

        public bool IsLightingSkyValid(HDCamera hdCamera)
        {
            return hdCamera.lightingSky.IsValid();
        }

        public bool IsVisualSkyValid(HDCamera hdCamera)
        {
            return hdCamera.visualSky.IsValid();
        }

        SphericalHarmonicsL2 GetAmbientProbe(SkyUpdateContext skyContext)
        {
            if (skyContext.IsValid() && IsCachedContextValid(skyContext))
            {
                ref var context = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                context.lastFrameUsed = m_CurrentFrameIndex;
                return context.renderingContext.ambientProbe;
            }
            else
            {
                return m_BlackAmbientProbe;
            }
        }

        Texture GetSkyCubemap(SkyUpdateContext skyContext)
        {
            if (skyContext.IsValid() && IsCachedContextValid(skyContext))
            {
                ref var context = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                context.lastFrameUsed = m_CurrentFrameIndex;
                return context.renderingContext.skyboxCubemapRT;
            }
            else
            {
                return CoreUtils.blackCubeTexture;
            }
        }

        Texture GetReflectionTexture(SkyUpdateContext skyContext)
        {
            if (skyContext.IsValid() && IsCachedContextValid(skyContext))
        {
                ref var context = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                context.lastFrameUsed = m_CurrentFrameIndex;
                return context.renderingContext.skyboxBSDFCubemapArray;
            }
            else
            {
                return m_BlackCubemapArray;
            }
        }

        public Texture GetSkyReflection(HDCamera hdCamera)
        {
            return GetReflectionTexture(hdCamera.lightingSky);
        }

        internal void SetupAmbientProbe(HDCamera hdCamera)
        {
            // If a camera just returns from being disabled, sky is not setup yet for it.
            if (hdCamera.lightingSky == null && hdCamera.skyAmbientMode == SkyAmbientMode.Dynamic)
            {
                RenderSettings.ambientMode = AmbientMode.Custom;
                RenderSettings.ambientProbe = m_BlackAmbientProbe;
                return;
            }

            bool useRealtimeGI = true;
#if UNITY_EDITOR
#pragma warning disable 618
            useRealtimeGI = UnityEditor.Lightmapping.realtimeGI;
#pragma warning restore 618
#endif
            // Working around GI current system
            // When using baked lighting, setting up the ambient probe should be sufficient => We only need to update RenderSettings.ambientProbe with either the static or visual sky ambient probe (computed from GPU)
            // When using real time GI. Enlighten will pull sky information from Skybox material. So in order for dynamic GI to work, we update the skybox material texture and then set the ambient mode to SkyBox
            // Problem: We can't check at runtime if realtime GI is enabled so we need to take extra care (see useRealtimeGI usage below)
            RenderSettings.ambientMode = AmbientMode.Custom; // Needed to specify ourselves the ambient probe (this will update internal ambient probe data passed to shaders)
            if (hdCamera.skyAmbientMode == SkyAmbientMode.Static)
            {
                RenderSettings.ambientProbe = GetAmbientProbe(m_StaticLightingSky);
                m_StandardSkyboxMaterial.SetTexture("_Tex", GetSkyCubemap(m_StaticLightingSky));
            }
            else
            {
                RenderSettings.ambientProbe = GetAmbientProbe(hdCamera.lightingSky);
                // Workaround in the editor:
                // When in the editor, if we use baked lighting, we need to setup the skybox material with the static lighting texture otherwise when baking, the dynamic texture will be used
                if (useRealtimeGI)
                {
                    m_StandardSkyboxMaterial.SetTexture("_Tex", GetSkyCubemap(hdCamera.lightingSky));
                }
                else
                {
                    m_StandardSkyboxMaterial.SetTexture("_Tex", GetSkyCubemap(m_StaticLightingSky));
                }
            }

            // This is only needed if we use realtime GI otherwise enlighten won't get the right sky information
            RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material as the default to be use in RenderSettings
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.ambientMode = AmbientMode.Skybox; // Force skybox for our HDRI
            RenderSettings.reflectionIntensity = 1.0f;
            RenderSettings.customReflection = null;
        }

        void BlitCubemap(CommandBuffer cmd, Cubemap source, RenderTexture dest)
        {
            var propertyBlock = new MaterialPropertyBlock();

            for (int i = 0; i < 6; ++i)
            {
                CoreUtils.SetRenderTarget(cmd, dest, ClearFlag.None, 0, (CubemapFace)i);
                propertyBlock.SetTexture("_MainTex", source);
                propertyBlock.SetFloat("_faceIndex", (float)i);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitCubemapMaterial, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
            }

            // Generate mipmap for our cubemap
            Debug.Assert(dest.autoGenerateMips == false);
            cmd.GenerateMips(dest);
        }

        void RenderSkyToCubemap(SkyUpdateContext skyContext)
        {
            var renderingContext = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId].renderingContext;
            var renderer = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId].renderer;

            for (int i = 0; i < 6; ++i)
            {
                m_BuiltinParameters.pixelCoordToViewDirMatrix = m_facePixelCoordToViewDirMatrices[i];
                m_BuiltinParameters.viewMatrix = m_CameraRelativeViewMatrices[i];
                m_BuiltinParameters.colorBuffer = renderingContext.skyboxCubemapRT;
                m_BuiltinParameters.depthBuffer = null;

                CoreUtils.SetRenderTarget(m_BuiltinParameters.commandBuffer, renderingContext.skyboxCubemapRT, ClearFlag.None, 0, (CubemapFace)i);
                renderer.RenderSky(m_BuiltinParameters, true, skyContext.skySettings.includeSunInBaking.value);
            }

            // Generate mipmap for our cubemap
            Debug.Assert(renderingContext.skyboxCubemapRT.rt.autoGenerateMips == false);
            m_BuiltinParameters.commandBuffer.GenerateMips(renderingContext.skyboxCubemapRT);
        }

        void RenderCubemapGGXConvolution(SkyUpdateContext skyContext)
        {
            using (new ProfilingSample(m_BuiltinParameters.commandBuffer, "Update Env: GGX Convolution"))
            {
                var renderingContext = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId].renderingContext;
                var renderer = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId].renderer;

                for (int bsdfIdx = 0; bsdfIdx < m_IBLFilterArray.Length; ++bsdfIdx)
                {
                    // First of all filter this cubemap using the target filter
                    m_IBLFilterArray[bsdfIdx].FilterCubemap(m_BuiltinParameters.commandBuffer, renderingContext.skyboxCubemapRT, m_SkyboxBSDFCubemapIntermediate);
                    // Then copy it to the cubemap array slice
                    for (int i = 0; i < 6; ++i)
                    {
                        m_BuiltinParameters.commandBuffer.CopyTexture(m_SkyboxBSDFCubemapIntermediate, i, renderingContext.skyboxBSDFCubemapArray, 6 * bsdfIdx + i);
                    }
                }
            }
        }

        // We do our own hash here because Unity does not provide correct hash for builtin types
        // Moreover, we don't want to test every single parameters of the light so we filter them here in this specific function.
        int GetSunLightHashCode(Light light)
        {
            HDAdditionalLightData ald = light.GetComponent<HDAdditionalLightData>();
            unchecked
            {
                // Sun could influence the sky (like for procedural sky). We need to handle this possibility. If sun property change, then we need to update the sky
                int hash = 13;
                hash = hash * 23 + light.transform.position.GetHashCode();
                hash = hash * 23 + light.transform.rotation.GetHashCode();
                hash = hash * 23 + light.color.GetHashCode();
                hash = hash * 23 + light.colorTemperature.GetHashCode();
                hash = hash * 23 + light.intensity.GetHashCode();
                // Note: We don't take into account cookie as it doesn't influence GI
                if (ald != null)
                {
                    hash = hash * 23 + ald.lightDimmer.GetHashCode();
                }

                return hash;
            }
        }


        void AllocateNewRenderingContext(SkyUpdateContext skyContext, int slot, int newHash, bool supportConvolution, in SphericalHarmonicsL2 previousAmbientProbe)
        {
            Debug.Assert(m_CachedSkyContexts[slot].hash == 0);
            ref var context = ref m_CachedSkyContexts[slot];
            context.hash = newHash;
            context.refCount = 1;

            var rendererType = skyContext.skySettings.GetSkyRendererType();
            if (rendererType != context.type || context.renderer == null)
            {
                if (context.renderer != null)
                    context.renderer.Cleanup();
                context.type = rendererType;
                context.renderer = (SkyRenderer)Activator.CreateInstance(rendererType);
                context.renderer.Build();
            }

            if (context.renderingContext != null && context.renderingContext.supportsConvolution != supportConvolution)
            {
                context.renderingContext.Cleanup();
                context.renderingContext = null;
            }

            if (context.renderingContext == null)
                context.renderingContext = new SkyRenderingContext(m_Resolution, m_IBLFilterArray.Length, supportConvolution, previousAmbientProbe);
            else
                context.renderingContext.UpdateAmbientProbe(previousAmbientProbe);
            skyContext.cachedSkyRenderingContextId = slot;
        }

        // Returns whether or not the data should be updated
        bool AcquireSkyRenderingContext(SkyUpdateContext updateContext, int newHash)
        {
            bool supportConvolution = true; // TODO: See how we can avoid allocating associated RT for static sky (issue is when the same sky is used for both static and lighting sky)
            SphericalHarmonicsL2 cachedAmbientProbe = new SphericalHarmonicsL2();
            // Release the old context if needed.
            if (IsCachedContextValid(updateContext))
            {
                var cachedContext = m_CachedSkyContexts[updateContext.cachedSkyRenderingContextId];
                if (newHash != cachedContext.hash || updateContext.skySettings.GetSkyRendererType() != cachedContext.type)
                {
                    // When a sky just changes hash without changing renderer, we need to keep previous ambient probe to avoid flickering transition through a default black probe
                    if (updateContext.skySettings.GetSkyRendererType() == cachedContext.type)
                    {
                        cachedAmbientProbe = cachedContext.renderingContext.ambientProbe;
                    }

                    ReleaseCachedContext(updateContext.cachedSkyRenderingContextId);
                }
                else
                {
                    // If the hash hasn't changed, keep it.
                    return false;
                }
            }

            // Else allocate a new one
            int firstFreeContext = -1;
            for (int i = 0; i < m_CachedSkyContexts.size; ++i)
            {
                // Try to find a matching slot
                if (m_CachedSkyContexts[i].hash == newHash)
                {
                    m_CachedSkyContexts[i].refCount++;
                    updateContext.cachedSkyRenderingContextId = i;
                    updateContext.skyParametersHash = newHash;
                    return false;
                }

                // Find the first available slot in case we don't find a matching one.
                if (firstFreeContext == -1 && m_CachedSkyContexts[i].hash == 0)
                    firstFreeContext = i;
            }

            if (firstFreeContext != -1)
            {
                AllocateNewRenderingContext(updateContext, firstFreeContext, newHash, supportConvolution, cachedAmbientProbe);
            }
            else
            {
                int newContextId = m_CachedSkyContexts.Add(new CachedSkyContext());
                AllocateNewRenderingContext(updateContext, newContextId, newHash, supportConvolution, cachedAmbientProbe);
            }

            return true;
        }

        void ReleaseCachedContext(int id)
        {
            ref var cachedContext = ref m_CachedSkyContexts[id];
            cachedContext.refCount--;
            Debug.Assert(cachedContext.refCount >= 0);
        }

        bool IsCachedContextValid(SkyUpdateContext skyContext)
        {
            if (skyContext.skySettings == null) // Sky set to None
                return false;

            int id = skyContext.cachedSkyRenderingContextId;
            // When the renderer changes, the cached context is no longer valid so we sometimes need to check that.
            return id != -1 && (skyContext.skySettings.GetSkyRendererType() == m_CachedSkyContexts[id].type) && (m_CachedSkyContexts[id].hash != 0);
        }

        void CleanupUnusedCachedContexts()
        {
            for (int i = 0; i < m_CachedSkyContexts.size; ++i)
            {
                ref var context = ref m_CachedSkyContexts[i];
                if (context.lastFrameUsed != 0 && (m_CurrentFrameIndex - context.lastFrameUsed > 30))
                {
                    context.Cleanup();
                }
            }
        }

        int ComputeSkyHash(SkyUpdateContext skyContext, Light sunLight, SkyAmbientMode ambientMode)
        {
            int sunHash = 0;
            if (sunLight != null)
                sunHash = GetSunLightHashCode(sunLight);
            int skyHash = sunHash * 23 + skyContext.skySettings.GetHashCode();
            skyHash = skyHash * 23 + (ambientMode == SkyAmbientMode.Static ? 1 : 0);
            return skyHash;
        }

        public void RequestEnvironmentUpdate()
        {
            m_UpdateRequired = true;
        }

        public void UpdateEnvironment(HDCamera hdCamera, SkyUpdateContext skyContext, Light sunLight, bool updateRequired, bool updateAmbientProbe, SkyAmbientMode ambientMode, int frameIndex, CommandBuffer cmd)
        {
            if (skyContext.IsValid())
            {
                skyContext.currentUpdateTime += Time.deltaTime;

                m_BuiltinParameters.hdCamera = hdCamera;
                m_BuiltinParameters.commandBuffer = cmd;
                m_BuiltinParameters.sunLight = sunLight;
                m_BuiltinParameters.pixelCoordToViewDirMatrix = hdCamera.mainViewConstants.pixelCoordToViewDirWS;
                m_BuiltinParameters.worldSpaceCameraPos = hdCamera.mainViewConstants.worldSpaceCameraPos;
                m_BuiltinParameters.viewMatrix = hdCamera.mainViewConstants.viewMatrix;
                m_BuiltinParameters.screenSize = m_CubemapScreenSize;
                m_BuiltinParameters.debugSettings = null; // We don't want any debug when updating the environment.
                m_BuiltinParameters.frameIndex = frameIndex;
                m_BuiltinParameters.skySettings = skyContext.skySettings;

                int skyHash = ComputeSkyHash(skyContext, sunLight, ambientMode);
                bool forceUpdate = updateRequired;

                // Acquire the rendering context, if the context was invalid or the hash has changed, this will request for an update.
                forceUpdate |= AcquireSkyRenderingContext(skyContext, skyHash);

                ref CachedSkyContext cachedContext = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                var renderingContext = cachedContext.renderingContext;

                if (IsCachedContextValid(skyContext))
                    forceUpdate |= cachedContext.renderer.DoUpdate(m_BuiltinParameters);

                if (forceUpdate ||
                    (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.OnChanged && skyHash != skyContext.skyParametersHash) ||
                    (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.Realtime && skyContext.currentUpdateTime > skyContext.skySettings.updatePeriod.value))
                {
                    using (new ProfilingSample(cmd, "Sky Environment Pass"))
                    {
                        using (new ProfilingSample(cmd, "Update Env: Generate Lighting Cubemap"))
                        {
                            RenderSkyToCubemap(skyContext);

                            if (updateAmbientProbe)
                            {
                                using (new ProfilingSample(cmd, "Update Ambient Probe"))
                                {
                                    cmd.SetComputeBufferParam(m_ComputeAmbientProbeCS, m_ComputeAmbientProbeKernel, m_AmbientProbeOutputBufferParam, renderingContext.ambientProbeResult);
                                    cmd.SetComputeTextureParam(m_ComputeAmbientProbeCS, m_ComputeAmbientProbeKernel, m_AmbientProbeInputCubemap, renderingContext.skyboxCubemapRT);
                                    cmd.DispatchCompute(m_ComputeAmbientProbeCS, m_ComputeAmbientProbeKernel, 1, 1, 1);
                                    cmd.RequestAsyncReadback(renderingContext.ambientProbeResult, renderingContext.OnComputeAmbientProbeDone);
                                }
                            }
                        }

                        if (renderingContext.supportsConvolution)
                        {
                            using (new ProfilingSample(cmd, "Update Env: Convolve Lighting Cubemap"))
                            {
                                RenderCubemapGGXConvolution(skyContext);
                            }
                        }

                        skyContext.skyParametersHash = skyHash;
                        skyContext.currentUpdateTime = 0.0f;

#if UNITY_EDITOR
                        // In the editor when we change the sky we want to make the GI dirty so when baking again the new sky is taken into account.
                        // Changing the hash of the rendertarget allow to say that GI is dirty
                        renderingContext.skyboxCubemapRT.rt.imageContentsHash = new Hash128((uint)skyContext.skySettings.GetHashCode(), 0, 0, 0);
#endif
                    }
                }
            }
            else
            {
                if (skyContext.cachedSkyRenderingContextId != -1)
                {
                    ReleaseCachedContext(skyContext.cachedSkyRenderingContextId);
                    skyContext.cachedSkyRenderingContextId = -1;
                }
            }
        }

        public void UpdateEnvironment(HDCamera hdCamera, Light sunLight, int frameIndex, CommandBuffer cmd)
        {
            m_CurrentFrameIndex = frameIndex;

            CleanupUnusedCachedContexts();

            SkyAmbientMode ambientMode = VolumeManager.instance.stack.GetComponent<VisualEnvironment>().skyAmbientMode.value;

            UpdateEnvironment(hdCamera, hdCamera.lightingSky, sunLight, m_UpdateRequired, ambientMode == SkyAmbientMode.Dynamic, ambientMode, frameIndex, cmd);

            // Preview camera will have a different sun, therefore the hash for the static lighting sky will change and force a recomputation
            // because we only maintain one static sky. Since we don't care that the static lighting may be a bit different in the preview we never recompute
            // and we use the one from the main camera.
            if (ambientMode == SkyAmbientMode.Static && hdCamera.camera.cameraType != CameraType.Preview)
            {
                StaticLightingSky staticLightingSky = GetStaticLightingSky();
                if (staticLightingSky != null)
                {
                    m_StaticLightingSky.skySettings = staticLightingSky.skySettings;
                    UpdateEnvironment(hdCamera, m_StaticLightingSky, sunLight, false, true, ambientMode, frameIndex, cmd);
                }
            }

            m_UpdateRequired = false;

            var reflectionTexture = GetReflectionTexture(hdCamera.lightingSky);
            cmd.SetGlobalTexture(HDShaderIDs._SkyTexture, reflectionTexture);
            float mipCount = Mathf.Clamp(Mathf.Log((float)reflectionTexture.width, 2.0f) + 1, 0.0f, 6.0f);
            cmd.SetGlobalFloat(HDShaderIDs._SkyTextureMipCount, mipCount);

            if (IsLightingSkyValid(hdCamera))
            {
                cmd.SetGlobalInt(HDShaderIDs._EnvLightSkyEnabled, 1);
            }
            else
            {
                cmd.SetGlobalInt(HDShaderIDs._EnvLightSkyEnabled, 0);
            }
        }

        internal void UpdateBuiltinParameters(SkyUpdateContext skyContext, HDCamera hdCamera, Light sunLight, RTHandle colorBuffer, RTHandle depthBuffer, DebugDisplaySettings debugSettings, int frameIndex, CommandBuffer cmd)
        {
            m_BuiltinParameters.hdCamera = hdCamera;
            m_BuiltinParameters.commandBuffer = cmd;
            m_BuiltinParameters.sunLight = sunLight;
            m_BuiltinParameters.pixelCoordToViewDirMatrix = hdCamera.mainViewConstants.pixelCoordToViewDirWS;
            m_BuiltinParameters.worldSpaceCameraPos = hdCamera.mainViewConstants.worldSpaceCameraPos;
            m_BuiltinParameters.viewMatrix = hdCamera.mainViewConstants.viewMatrix;
            m_BuiltinParameters.screenSize = hdCamera.screenSize;
            m_BuiltinParameters.colorBuffer = colorBuffer;
            m_BuiltinParameters.depthBuffer = depthBuffer;
            m_BuiltinParameters.debugSettings = debugSettings;
            m_BuiltinParameters.frameIndex = frameIndex;
            m_BuiltinParameters.skySettings = skyContext.skySettings;
        }

        public void PreRenderSky(HDCamera hdCamera, Light sunLight, RTHandle colorBuffer, RTHandle normalBuffer, RTHandle depthBuffer, DebugDisplaySettings debugSettings, int frameIndex, CommandBuffer cmd)
        {
            var skyContext = hdCamera.visualSky;
            if (skyContext.IsValid())
            {
                UpdateBuiltinParameters(skyContext,
                                        hdCamera,
                                        sunLight,
                                        colorBuffer,
                                        depthBuffer,
                                        debugSettings,
                                        frameIndex,
                                        cmd);

                int skyHash = ComputeSkyHash(skyContext, sunLight, SkyAmbientMode.Static);
                AcquireSkyRenderingContext(skyContext, skyHash);
                var cachedContext = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                cachedContext.renderer.DoUpdate(m_BuiltinParameters);
                if (depthBuffer != BuiltinSkyParameters.nullRT && normalBuffer != BuiltinSkyParameters.nullRT)
                {
                    CoreUtils.SetRenderTarget(cmd, normalBuffer, depthBuffer);
                }
                else if (depthBuffer != BuiltinSkyParameters.nullRT)
                {
                    CoreUtils.SetRenderTarget(cmd, depthBuffer);
                }
                cachedContext.renderer.PreRenderSky(m_BuiltinParameters, false, hdCamera.camera.cameraType != CameraType.Reflection || skyContext.skySettings.includeSunInBaking.value);
            }
        }

        public void RenderSky(HDCamera hdCamera, Light sunLight, RTHandle colorBuffer, RTHandle depthBuffer, DebugDisplaySettings debugSettings, int frameIndex, CommandBuffer cmd)
        {
            var skyContext = hdCamera.visualSky;
            if (skyContext.IsValid() && hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky)
            {
                using (new ProfilingSample(cmd, "Sky Pass"))
                {
                    UpdateBuiltinParameters(skyContext,
                                         hdCamera,
                                         sunLight,
                                         colorBuffer,
                                         depthBuffer,
                                         debugSettings,
                                         frameIndex,
                                         cmd);

                    SkyAmbientMode ambientMode = VolumeManager.instance.stack.GetComponent<VisualEnvironment>().skyAmbientMode.value;
                    int skyHash = ComputeSkyHash(skyContext, sunLight, ambientMode);
                    AcquireSkyRenderingContext(skyContext, skyHash);
                    var cachedContext = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];

                    cachedContext.renderer.DoUpdate(m_BuiltinParameters);

                    if (depthBuffer == BuiltinSkyParameters.nullRT)
                    {
                        CoreUtils.SetRenderTarget(cmd, colorBuffer);
                    }
                    else
                    {
                        CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer);
                    }

                    // If the luxmeter is enabled, we don't render the sky
                    if (debugSettings.data.lightingDebugSettings.debugLightingMode != DebugLightingMode.LuxMeter)
                    {
                        // When rendering the visual sky for reflection probes, we need to remove the sun disk if skySettings.includeSunInBaking is false.
                        cachedContext.renderer.RenderSky(m_BuiltinParameters, false, hdCamera.camera.cameraType != CameraType.Reflection || skyContext.skySettings.includeSunInBaking.value);
                    }
                }
            }
        }

        public void RenderOpaqueAtmosphericScattering(CommandBuffer cmd, HDCamera hdCamera,
                                                      RTHandle colorBuffer,
                                                      RTHandle volumetricLighting,
                                                      RTHandle intermediateBuffer,
                                                      RTHandle depthBuffer,
                                                      Matrix4x4 pixelCoordToViewDirWS, bool isMSAA)
        {
            using (new ProfilingSample(cmd, "Opaque Atmospheric Scattering"))
            {
                m_OpaqueAtmScatteringBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, pixelCoordToViewDirWS);
                if (isMSAA)
                    m_OpaqueAtmScatteringBlock.SetTexture(HDShaderIDs._ColorTextureMS, colorBuffer);
                else
                    m_OpaqueAtmScatteringBlock.SetTexture(HDShaderIDs._ColorTexture,   colorBuffer);
                // The texture can be null when volumetrics are disabled.
                if (volumetricLighting != null)
                    m_OpaqueAtmScatteringBlock.SetTexture(HDShaderIDs._VBufferLighting, volumetricLighting);

                if (Fog.IsPBRFogEnabled(hdCamera))
                {
                    // Color -> Intermediate.
                    HDUtils.DrawFullScreen(cmd, m_OpaqueAtmScatteringMaterial, intermediateBuffer, depthBuffer, m_OpaqueAtmScatteringBlock, isMSAA ? 3 : 2);
                    // Intermediate -> Color.
                    // Note: Blit does not support MSAA (and is probably slower).
                    cmd.CopyTexture(intermediateBuffer, colorBuffer);
                }
                else
                {
                    HDUtils.DrawFullScreen(cmd, m_OpaqueAtmScatteringMaterial, colorBuffer, depthBuffer, m_OpaqueAtmScatteringBlock, isMSAA ? 1 : 0);
                }
            }
        }

        static public StaticLightingSky GetStaticLightingSky()
        {
            if (m_StaticLightingSkies.Count == 0)
                return null;
            else
                return m_StaticLightingSkies[m_StaticLightingSkies.Count - 1];
        }

        static public void RegisterStaticLightingSky(StaticLightingSky staticLightingSky)
        {
            if (!m_StaticLightingSkies.Contains(staticLightingSky))
            {
                if (m_StaticLightingSkies.Count != 0)
                {
                    Debug.LogWarning("One Static Lighting Sky component was already set for baking, only the latest one will be used.");
                }

                if (staticLightingSky.staticLightingSkyUniqueID == (int)SkyType.Procedural && !skyTypesDict.TryGetValue((int)SkyType.Procedural, out var dummy))
                {
                    Debug.LogError("You are using the deprecated Procedural Sky for static lighting in your Scene. You can still use it but, to do so, you must install it separately. To do this, open the Package Manager window and import the 'Procedural Sky' sample from the HDRP package page, then close and re-open your project without saving.");
                    return;
                }

                m_StaticLightingSkies.Add(staticLightingSky);
            }
        }

        static public void UnRegisterStaticLightingSky(StaticLightingSky staticLightingSky)
        {
            m_StaticLightingSkies.Remove(staticLightingSky);
        }

        public Texture2D ExportSkyToTexture(Camera camera)
        {
            var hdCamera = HDCamera.GetOrCreate(camera);

            if (!hdCamera.visualSky.IsValid() || !IsCachedContextValid(hdCamera.visualSky))
            {
                Debug.LogError("Cannot export sky to a texture, no valid Sky is setup (Also make sure the game view has been rendered at least once).");
                return null;
            }

            ref var cachedContext = ref m_CachedSkyContexts[hdCamera.visualSky.cachedSkyRenderingContextId];
            RenderTexture skyCubemap = cachedContext.renderingContext.skyboxCubemapRT;

            int resolution = skyCubemap.width;

            var tempRT = new RenderTexture(resolution * 6, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
            {
                dimension = TextureDimension.Tex2D,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Trilinear
            };
            tempRT.Create();

            var temp = new Texture2D(resolution * 6, resolution, TextureFormat.RGBAFloat, false);
            var result = new Texture2D(resolution * 6, resolution, TextureFormat.RGBAFloat, false);

            // Note: We need to invert in Y the cubemap faces because the current sky cubemap is inverted (because it's a RT)
            // So to invert it again so that it's a proper cubemap image we need to do it in several steps because ReadPixels does not have scale parameters:
            // - Convert the cubemap into a 2D texture
            // - Blit and invert it to a temporary target.
            // - Read this target again into the result texture.
            int offset = 0;
            for (int i = 0; i < 6; ++i)
            {
                UnityEngine.Graphics.SetRenderTarget(skyCubemap, 0, (CubemapFace)i);
                temp.ReadPixels(new Rect(0, 0, resolution, resolution), offset, 0);
                temp.Apply();
                offset += resolution;
            }

            // Flip texture.
            UnityEngine.Graphics.Blit(temp, tempRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 0.0f));

            result.ReadPixels(new Rect(0, 0, resolution * 6, resolution), 0, 0);
            result.Apply();

            UnityEngine.Graphics.SetRenderTarget(null);
            CoreUtils.Destroy(temp);
            CoreUtils.Destroy(tempRT);

            return result;
        }

#if UNITY_EDITOR
        void OnBakeStarted()
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            if (hdrp == null)
                return;

            // Happens sometime in the tests.
            if (m_StandardSkyboxMaterial == null)
                m_StandardSkyboxMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.skyboxCubemapPS);

            // At the start of baking we need to update the GI system with the static lighting sky in order for lightmaps and probes to be baked with it.
            var staticLightingSky = GetStaticLightingSky();
            if (m_StaticLightingSky.skySettings != null && IsCachedContextValid(m_StaticLightingSky))
            {
                var renderingContext = m_CachedSkyContexts[m_StaticLightingSky.cachedSkyRenderingContextId].renderingContext;
                m_StandardSkyboxMaterial.SetTexture("_Tex", m_StaticLightingSky.IsValid() ? (Texture)renderingContext.skyboxCubemapRT : CoreUtils.blackCubeTexture);
            }
            else
            {
                m_StandardSkyboxMaterial.SetTexture("_Tex", CoreUtils.blackCubeTexture);
            }

            RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material as the default to be use in RenderSettings
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.ambientMode = AmbientMode.Skybox; // Force skybox for our HDRI
            RenderSettings.reflectionIntensity = 1.0f;
            RenderSettings.customReflection = null;

            DynamicGI.UpdateEnvironment();
        }
#endif
    }
}
