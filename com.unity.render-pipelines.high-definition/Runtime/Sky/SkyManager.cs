using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Resolution of the sky reflection cubemap.
    /// </summary>
    [Serializable]
    public enum SkyResolution
    {
        /// <summary>128x128 per face.</summary>
        SkyResolution128 = 128,
        /// <summary>256x256 per face.</summary>
        SkyResolution256 = 256,
        /// <summary>512x512 per face.</summary>
        SkyResolution512 = 512,
        /// <summary>1024x1024 per face.</summary>
        SkyResolution1024 = 1024,
        /// <summary>2048x2048 per face.</summary>
        SkyResolution2048 = 2048,
        /// <summary>4096x4096 per face.</summary>
        SkyResolution4096 = 4096
    }

    /// <summary>
    /// Environment lighting update mode.
    /// </summary>
    public enum EnvironmentUpdateMode
    {
        /// <summary>Environment lighting is updated when the sky has changed.</summary>
        OnChanged = 0,
        /// <summary>Environment lighting is updated on demand.</summary>
        OnDemand,
        /// <summary>Environment lighting is updated in real time.</summary>
        Realtime
    }

    /// <summary>
    /// Parameters passed to sky rendering functions.
    /// </summary>
    public class BuiltinSkyParameters
    {
        /// <summary>Camera used for rendering.</summary>
        public HDCamera                 hdCamera;
        /// <summary>Matrix mapping pixel coordinate to view direction.</summary>
        public Matrix4x4                pixelCoordToViewDirMatrix;
        /// <summary>World space camera position.</summary>
        public Vector3                  worldSpaceCameraPos;
        /// <summary>Camera view matrix.</summary>
        public Matrix4x4                viewMatrix;
        /// <summary>Screen size: Width, height, inverse width, inverse height.</summary>
        public Vector4                  screenSize;
        /// <summary>Command buffer used for rendering.</summary>
        public CommandBuffer            commandBuffer;
        /// <summary>Current sun light.</summary>
        public Light                    sunLight;
        /// <summary>Color buffer used for rendering.</summary>
        public RTHandle                 colorBuffer;
        /// <summary>Depth buffer used for rendering.</summary>
        public RTHandle                 depthBuffer;
        /// <summary>Current frame index.</summary>
        public int                      frameIndex;
        /// <summary>Current sky settings.</summary>
        public SkySettings              skySettings;
        /// <summary>Current debug dsplay settings.</summary>
        public DebugDisplaySettings     debugSettings;
        /// <summary>Null color buffer render target identifier.</summary>
        public static RenderTargetIdentifier nullRT = -1;
    }

    struct CachedSkyContext
    {
        public Type                 type;
        public SkyRenderingContext  renderingContext;
        public int                  hash;
        public int                  refCount;

        public void Reset()
        {
            // We keep around the renderer and the rendering context to avoid useless allocation if they get reused.
            hash = 0;
            refCount = 0;
        }

        public void Cleanup()
        {
            Reset();

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
        bool                    m_StaticSkyUpdateRequired = false;
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

        // This boolean here is only to track the first frame after a domain reload or creation.
        bool m_RequireWaitForAsyncReadBackRequest = true;

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
                var renderer = hdCamera.lightingSky.skyRenderer;
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

            m_StaticLightingSky.Cleanup();
            lightingOverrideVolumeStack.Dispose();

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

        // Return the value of the ambient probe
        internal SphericalHarmonicsL2 GetAmbientProbe(HDCamera hdCamera)
        {
            // If a camera just returns from being disabled, sky is not setup yet for it.
            if (hdCamera.lightingSky == null && hdCamera.skyAmbientMode == SkyAmbientMode.Dynamic)
            {
                return m_BlackAmbientProbe;
            }

            if (hdCamera.skyAmbientMode == SkyAmbientMode.Static)
            {
                return GetAmbientProbe(m_StaticLightingSky);
            }

            return GetAmbientProbe(hdCamera.lightingSky);
        }

        internal bool HasSetValidAmbientProbe(HDCamera hdCamera)
        {
            SkyAmbientMode ambientMode = hdCamera.volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;
            if (ambientMode == SkyAmbientMode.Static)
                return true;

            if (hdCamera.skyAmbientMode == SkyAmbientMode.Dynamic && hdCamera.lightingSky != null &&
                hdCamera.lightingSky.IsValid() && IsCachedContextValid(hdCamera.lightingSky))
            {
                ref CachedSkyContext cachedContext = ref m_CachedSkyContexts[hdCamera.lightingSky.cachedSkyRenderingContextId];
                var renderingContext = cachedContext.renderingContext;
                return renderingContext.ambientProbeIsReady;
            }

            return false;

        }

        internal void SetupAmbientProbe(HDCamera hdCamera)
        {
            // Working around GI current system
            // When using baked lighting, setting up the ambient probe should be sufficient => We only need to update RenderSettings.ambientProbe with either the static or visual sky ambient probe (computed from GPU)
            // When using real time GI. Enlighten will pull sky information from Skybox material. So in order for dynamic GI to work, we update the skybox material texture and then set the ambient mode to SkyBox
            // Problem: We can't check at runtime if realtime GI is enabled so we need to take extra care (see useRealtimeGI usage below)

            // Order is important!
            RenderSettings.ambientMode = AmbientMode.Custom; // Needed to specify ourselves the ambient probe (this will update internal ambient probe data passed to shaders)
            RenderSettings.ambientProbe = GetAmbientProbe(hdCamera);

            // If a camera just returns from being disabled, sky is not setup yet for it.
            if (hdCamera.lightingSky == null && hdCamera.skyAmbientMode == SkyAmbientMode.Dynamic)
            {
                return;
            }

            // Workaround in the editor:
            // When in the editor, if we use baked lighting, we need to setup the skybox material with the static lighting texture otherwise when baking, the dynamic texture will be used
            bool useRealtimeGI = true;
#if UNITY_EDITOR
#pragma warning disable 618
            useRealtimeGI = UnityEditor.Lightmapping.realtimeGI;
#pragma warning restore 618
#endif
            m_StandardSkyboxMaterial.SetTexture("_Tex", GetSkyCubemap((hdCamera.skyAmbientMode != SkyAmbientMode.Static && useRealtimeGI) ? hdCamera.lightingSky : m_StaticLightingSky));

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
            using (new ProfilingScope(m_BuiltinParameters.commandBuffer, ProfilingSampler.Get(HDProfileId.RenderSkyToCubemap)))
            {
                var renderingContext = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId].renderingContext;
                var renderer = skyContext.skyRenderer;

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
        }

        void RenderCubemapGGXConvolution(SkyUpdateContext skyContext)
        {
            using (new ProfilingScope(m_BuiltinParameters.commandBuffer, ProfilingSampler.Get(HDProfileId.UpdateSkyEnvironmentConvolution)))
            {
                var renderingContext = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId].renderingContext;
                var renderer = skyContext.skyRenderer;

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


        void AllocateNewRenderingContext(SkyUpdateContext skyContext, int slot, int newHash, bool supportConvolution, in SphericalHarmonicsL2 previousAmbientProbe, string name)
        {
            Debug.Assert(m_CachedSkyContexts[slot].hash == 0);
            ref var context = ref m_CachedSkyContexts[slot];
            context.hash = newHash;
            context.refCount = 1;
            context.type = skyContext.skySettings.GetSkyRendererType();

            if (context.renderingContext != null && context.renderingContext.supportsConvolution != supportConvolution)
            {
                context.renderingContext.Cleanup();
                context.renderingContext = null;
            }

            if (context.renderingContext == null)
                context.renderingContext = new SkyRenderingContext(m_Resolution, m_IBLFilterArray.Length, supportConvolution, previousAmbientProbe, name);

            skyContext.cachedSkyRenderingContextId = slot;
        }

        // Returns whether or not the data should be updated
        bool AcquireSkyRenderingContext(SkyUpdateContext updateContext, int newHash, string name = "", bool supportConvolution = true)
        {
            SphericalHarmonicsL2 cachedAmbientProbe = new SphericalHarmonicsL2();
            // Release the old context if needed.
            if (IsCachedContextValid(updateContext))
            {
                ref var cachedContext = ref m_CachedSkyContexts[updateContext.cachedSkyRenderingContextId];
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

            if (name == "")
                name = "SkyboxCubemap";

            if (firstFreeContext != -1)
            {
                AllocateNewRenderingContext(updateContext, firstFreeContext, newHash, supportConvolution, cachedAmbientProbe, name);
            }
            else
            {
                int newContextId = m_CachedSkyContexts.Add(new CachedSkyContext());
                AllocateNewRenderingContext(updateContext, newContextId, newHash, supportConvolution, cachedAmbientProbe, name);
            }

            return true;
        }

        internal void ReleaseCachedContext(int id)
        {
            if (id == -1)
                return;

            ref var cachedContext = ref m_CachedSkyContexts[id];

            // This can happen if 2 cameras use the same context and release it in the same frame.
            // The first release the context but the next one will still have this id.
            if (cachedContext.refCount == 0)
            {
                Debug.Assert(cachedContext.renderingContext == null); // Context should already have been cleaned up.
                return;
            }

            cachedContext.refCount--;
            if (cachedContext.refCount == 0)
                cachedContext.Reset();
        }

        bool IsCachedContextValid(SkyUpdateContext skyContext)
        {
            if (skyContext.skySettings == null) // Sky set to None
                return false;

            int id = skyContext.cachedSkyRenderingContextId;
            // When the renderer changes, the cached context is no longer valid so we sometimes need to check that.
            return id != -1 && (skyContext.skySettings.GetSkyRendererType() == m_CachedSkyContexts[id].type) && (m_CachedSkyContexts[id].hash != 0);
        }

        int ComputeSkyHash(HDCamera camera, SkyUpdateContext skyContext, Light sunLight, SkyAmbientMode ambientMode, bool staticSky = false)
        {
            int sunHash = 0;
            if (sunLight != null)
                sunHash = GetSunLightHashCode(sunLight);

            // For planar reflections we want to use the parent position for hash.
            Camera cameraForHash = camera.camera;
            if (camera.camera.cameraType == CameraType.Reflection && camera.parentCamera != null)
            {
                cameraForHash = camera.parentCamera;
            }

            int skyHash = sunHash * 23 + skyContext.skySettings.GetHashCode(cameraForHash);
            skyHash = skyHash * 23 + (staticSky ? 1 : 0);
            skyHash = skyHash * 23 + (ambientMode == SkyAmbientMode.Static ? 1 : 0);
            return skyHash;
        }

        public void RequestEnvironmentUpdate()
        {
            m_UpdateRequired = true;
        }

        internal void RequestStaticEnvironmentUpdate()
        {
            m_StaticSkyUpdateRequired = true;
            m_RequireWaitForAsyncReadBackRequest = true;
        }

        public void UpdateEnvironment(  HDCamera                hdCamera,
                                        ScriptableRenderContext renderContext,
                                        SkyUpdateContext        skyContext,
                                        Light                   sunLight,
                                        bool                    updateRequired,
                                        bool                    updateAmbientProbe,
                                        bool                    staticSky,
                                        SkyAmbientMode          ambientMode,
                                        int                     frameIndex,
                                        CommandBuffer           cmd)
        {
            if (skyContext.IsValid())
            {
                skyContext.currentUpdateTime += Time.deltaTime; // Consider using HDRenderPipeline.GetTime().

                m_BuiltinParameters.hdCamera = hdCamera;
                m_BuiltinParameters.commandBuffer = cmd;
                m_BuiltinParameters.sunLight = sunLight;
                m_BuiltinParameters.pixelCoordToViewDirMatrix = hdCamera.mainViewConstants.pixelCoordToViewDirWS;
                Vector3 worldSpaceCameraPos = hdCamera.mainViewConstants.worldSpaceCameraPos;
                // For planar reflections we use the parent camera position for all the runtime computations.
                // This is to avoid cases in which the probe camera is below ground and the parent is not, leading to
                // in case of PBR sky to a black sky. All other parameters are left as is.
                // This can introduce inaccuracies, but they should be acceptable if the distance parent camera - probe camera is
                // small.
                if (hdCamera.camera.cameraType == CameraType.Reflection && hdCamera.parentCamera != null)
                {
                    worldSpaceCameraPos = hdCamera.parentCamera.transform.position;
                }
                m_BuiltinParameters.worldSpaceCameraPos = worldSpaceCameraPos;
                m_BuiltinParameters.viewMatrix = hdCamera.mainViewConstants.viewMatrix;
                m_BuiltinParameters.screenSize = m_CubemapScreenSize;
                m_BuiltinParameters.debugSettings = null; // We don't want any debug when updating the environment.
                m_BuiltinParameters.frameIndex = frameIndex;
                m_BuiltinParameters.skySettings = skyContext.skySettings;

                int skyHash = ComputeSkyHash(hdCamera, skyContext, sunLight, ambientMode, staticSky);
                bool forceUpdate = updateRequired;

                // Acquire the rendering context, if the context was invalid or the hash has changed, this will request for an update.
                forceUpdate |= AcquireSkyRenderingContext(skyContext, skyHash, staticSky ? "SkyboxCubemap_Static" : "SkyboxCubemap", !staticSky);

                ref CachedSkyContext cachedContext = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                var renderingContext = cachedContext.renderingContext;

                if (IsCachedContextValid(skyContext))
                    forceUpdate |= skyContext.skyRenderer.DoUpdate(m_BuiltinParameters);

                if (forceUpdate ||
                    (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.OnChanged && skyHash != skyContext.skyParametersHash) ||
                    (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.Realtime && skyContext.currentUpdateTime > skyContext.skySettings.updatePeriod.value))
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UpdateSkyEnvironment)))
                    {
                        RenderSkyToCubemap(skyContext);

                        if (updateAmbientProbe)
                        {
                            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UpdateSkyAmbientProbe)))
                            {
                                cmd.SetComputeBufferParam(m_ComputeAmbientProbeCS, m_ComputeAmbientProbeKernel, m_AmbientProbeOutputBufferParam, renderingContext.ambientProbeResult);
                                cmd.SetComputeTextureParam(m_ComputeAmbientProbeCS, m_ComputeAmbientProbeKernel, m_AmbientProbeInputCubemap, renderingContext.skyboxCubemapRT);
                                cmd.DispatchCompute(m_ComputeAmbientProbeCS, m_ComputeAmbientProbeKernel, 1, 1, 1);
                                cmd.RequestAsyncReadback(renderingContext.ambientProbeResult, renderingContext.OnComputeAmbientProbeDone);

                                // When the profiler is enabled, we don't want to submit the render context because
                                // it will break all the profiling sample Begin() calls issued previously, which leads
                                // to profiling sample mismatch errors in the console.
                                if (!UnityEngine.Profiling.Profiler.enabled)
                                {
                                    // In case we are the first frame after a domain reload, we need to wait for async readback request to complete
                                    // otherwise ambient probe isn't correct for one frame.
                                    if (m_RequireWaitForAsyncReadBackRequest)
                                    {
                                        cmd.WaitAllAsyncReadbackRequests();
                                        renderContext.ExecuteCommandBuffer(cmd);
                                        CommandBufferPool.Release(cmd);
                                        renderContext.Submit();
                                        cmd = CommandBufferPool.Get();
                                        m_RequireWaitForAsyncReadBackRequest = false;
                                    }
                                }
                            }
                        }

                        if (renderingContext.supportsConvolution)
                        {
                            RenderCubemapGGXConvolution(skyContext);
                        }

                        skyContext.skyParametersHash = skyHash;
                        skyContext.currentUpdateTime = 0.0f;

#if UNITY_EDITOR
                        // In the editor when we change the sky we want to make the GI dirty so when baking again the new sky is taken into account.
                        // Changing the hash of the rendertarget allow to say that GI is dirty
                        renderingContext.skyboxCubemapRT.rt.imageContentsHash = new Hash128((uint)skyContext.skySettings.GetHashCode(hdCamera.camera), 0, 0, 0);
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

        public void UpdateEnvironment(HDCamera hdCamera, ScriptableRenderContext renderContext, Light sunLight, int frameIndex, CommandBuffer cmd)
        {
            SkyAmbientMode ambientMode = hdCamera.volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;

            UpdateEnvironment(hdCamera, renderContext, hdCamera.lightingSky, sunLight, m_UpdateRequired, ambientMode == SkyAmbientMode.Dynamic, false, ambientMode, frameIndex, cmd);

            // Preview camera will have a different sun, therefore the hash for the static lighting sky will change and force a recomputation
            // because we only maintain one static sky. Since we don't care that the static lighting may be a bit different in the preview we never recompute
            // and we use the one from the main camera.
            bool forceStaticUpdate = false;
            StaticLightingSky staticLightingSky = GetStaticLightingSky();
#if UNITY_EDITOR
            // In the editor, we might need the static sky ready for baking lightmaps/lightprobes regardless of the current ambient mode so we force it to update in this case if it's not been computed yet..
            // We don't test if the hash of the static sky has changed here because it depends on the sun direction and in the case of LookDev, sun will be different from the main rendering so it will induce improper recomputation.
            forceStaticUpdate = staticLightingSky != null && m_StaticLightingSky.skyParametersHash == -1; ;
#endif
            if ((ambientMode == SkyAmbientMode.Static || forceStaticUpdate) && hdCamera.camera.cameraType != CameraType.Preview)
            {
                m_StaticLightingSky.skySettings = staticLightingSky != null ? staticLightingSky.skySettings : null;
                UpdateEnvironment(hdCamera, renderContext, m_StaticLightingSky, sunLight, m_StaticSkyUpdateRequired, true, true, SkyAmbientMode.Static, frameIndex, cmd);
                m_StaticSkyUpdateRequired = false;
            }

            m_UpdateRequired = false;

            var reflectionTexture = GetReflectionTexture(hdCamera.lightingSky);
            cmd.SetGlobalTexture(HDShaderIDs._SkyTexture, reflectionTexture);

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

                SkyAmbientMode ambientMode = hdCamera.volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;
                int skyHash = ComputeSkyHash(hdCamera, skyContext, sunLight, ambientMode);
                AcquireSkyRenderingContext(skyContext, skyHash);
                skyContext.skyRenderer.DoUpdate(m_BuiltinParameters);
                if (depthBuffer != BuiltinSkyParameters.nullRT && normalBuffer != BuiltinSkyParameters.nullRT)
                {
                    CoreUtils.SetRenderTarget(cmd, normalBuffer, depthBuffer);
                }
                else if (depthBuffer != BuiltinSkyParameters.nullRT)
                {
                    CoreUtils.SetRenderTarget(cmd, depthBuffer);
                }
                skyContext.skyRenderer.PreRenderSky(m_BuiltinParameters, false, hdCamera.camera.cameraType != CameraType.Reflection || skyContext.skySettings.includeSunInBaking.value);
            }
        }

        public void RenderSky(HDCamera hdCamera, Light sunLight, RTHandle colorBuffer, RTHandle depthBuffer, DebugDisplaySettings debugSettings, int frameIndex, CommandBuffer cmd)
        {
            var skyContext = hdCamera.visualSky;
            if (skyContext.IsValid() && hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderSky)))
                {
                    UpdateBuiltinParameters(skyContext,
                                         hdCamera,
                                         sunLight,
                                         colorBuffer,
                                         depthBuffer,
                                         debugSettings,
                                         frameIndex,
                                         cmd);

                    SkyAmbientMode ambientMode = hdCamera.volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;
                    int skyHash = ComputeSkyHash(hdCamera, skyContext, sunLight, ambientMode);
                    AcquireSkyRenderingContext(skyContext, skyHash);

                    skyContext.skyRenderer.DoUpdate(m_BuiltinParameters);

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
                        skyContext.skyRenderer.RenderSky(m_BuiltinParameters, false, hdCamera.camera.cameraType != CameraType.Reflection || skyContext.skySettings.includeSunInBaking.value);
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
            using (new ProfilingScope(m_BuiltinParameters.commandBuffer, ProfilingSampler.Get(HDProfileId.OpaqueAtmosphericScattering)))
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
