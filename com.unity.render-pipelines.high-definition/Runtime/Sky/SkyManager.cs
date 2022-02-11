using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        public HDCamera hdCamera;
        /// <summary>Matrix mapping pixel coordinate to view direction.</summary>
        public Matrix4x4 pixelCoordToViewDirMatrix;
        /// <summary>World space camera position.</summary>
        public Vector3 worldSpaceCameraPos;
        /// <summary>Camera view matrix.</summary>
        public Matrix4x4 viewMatrix;
        /// <summary>Screen size: Width, height, inverse width, inverse height.</summary>
        public Vector4 screenSize;
        /// <summary>Command buffer used for rendering.</summary>
        public CommandBuffer commandBuffer;
        /// <summary>Current sun light.</summary>
        public Light sunLight;
        /// <summary>Color buffer used for rendering.</summary>
        public RTHandle colorBuffer;
        /// <summary>Depth buffer used for rendering.</summary>
        public RTHandle depthBuffer;
        /// <summary>Current frame index.</summary>
        public int frameIndex;
        /// <summary>Current sky settings.</summary>
        public SkySettings skySettings;
        /// <summary>Current cloud settings.</summary>
        public CloudSettings cloudSettings;
        /// <summary>Current volumetric cloud settings.</summary>
        public VolumetricClouds volumetricClouds;
        /// <summary>Current debug dsplay settings.</summary>
        public DebugDisplaySettings debugSettings;
        /// <summary>Null color buffer render target identifier.</summary>
        public static RenderTargetIdentifier nullRT = -1;
        /// <summary>Index of the current cubemap face to render (Unknown for texture2D).</summary>
        public CubemapFace cubemapFace = CubemapFace.Unknown;

        /// <summary>
        /// Copy content of this BuiltinSkyParameters to another instance.
        /// </summary>
        /// <param name="other">Other instance to copy to.</param>
        public void CopyTo(BuiltinSkyParameters other)
        {
            other.hdCamera = hdCamera;
            other.pixelCoordToViewDirMatrix = pixelCoordToViewDirMatrix;
            other.worldSpaceCameraPos = worldSpaceCameraPos;
            other.viewMatrix = viewMatrix;
            other.screenSize = screenSize;
            other.commandBuffer = commandBuffer;
            other.sunLight = sunLight;
            other.colorBuffer = colorBuffer;
            other.depthBuffer = depthBuffer;
            other.frameIndex = frameIndex;
            other.skySettings = skySettings;
            other.cloudSettings = cloudSettings;
            other.volumetricClouds = volumetricClouds;
            other.debugSettings = debugSettings;
            other.cubemapFace = cubemapFace;
        }
    }

    /// <summary>
    /// Parameters passed to sun light cookie rendering functions.
    /// </summary>
    public struct BuiltinSunCookieParameters
    {
        /// <summary>Camera used for rendering.</summary>
        public HDCamera hdCamera;
        /// <summary>Command buffer used for rendering.</summary>
        public CommandBuffer commandBuffer;
        /// <summary>Current cloud settings.</summary>
        public CloudSettings cloudSettings;
        /// <summary>Current sun light.</summary>
        public Light sunLight;
    }

    struct CachedSkyContext
    {
        public Type type;
        public SkyRenderingContext renderingContext;
        public int hash;
        public int refCount;

        public void Reset()
        {
            // We keep around the rendering context to avoid useless allocation if they get reused.
            hash = 0;
            refCount = 0;
            if (renderingContext != null)
                renderingContext.Reset();
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
        Material m_StandardSkyboxMaterial; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Material m_BlitCubemapMaterial;
        Material m_OpaqueAtmScatteringMaterial;

        SphericalHarmonicsL2 m_BlackAmbientProbe = new SphericalHarmonicsL2();

        bool m_UpdateRequired = false;
        bool m_StaticSkyUpdateRequired = false;
        int m_Resolution;

        // Sky used for static lighting. It will be used for ambient lighting if Ambient Mode is set to Static (even when realtime GI is enabled)
        // It will also be used for lightmap and light probe baking
        SkyUpdateContext m_StaticLightingSky = new SkyUpdateContext();

        // This interpolation volume stack is used to interpolate the lighting override separately from the visual sky.
        // If a sky setting is present in this volume then it will be used for lighting override.
        public VolumeStack lightingOverrideVolumeStack { get; private set; }
        public LayerMask lightingOverrideLayerMask { get; private set; } = -1;

        static Dictionary<int, Type> m_SkyTypesDict = null;
        public static Dictionary<int, Type> skyTypesDict { get { if (m_SkyTypesDict == null) UpdateSkyTypes(); return m_SkyTypesDict; } }

        static Dictionary<int, Type> m_CloudTypesDict = null;
        public static Dictionary<int, Type> cloudTypesDict { get { if (m_CloudTypesDict == null) UpdateCloudTypes(); return m_CloudTypesDict; } }

        // This list will hold the static lighting sky that should be used for baking ambient probe.
        // In practice we will always use the last one registered but we use a list to be able to roll back to the previous one once the user deletes the superfluous instances.
        private static List<StaticLightingSky> m_StaticLightingSkies = new List<StaticLightingSky>();

        // Only show the procedural sky upgrade message once
        static bool logOnce = true;

        MaterialPropertyBlock m_OpaqueAtmScatteringBlock;

#if UNITY_EDITOR
        // For Preview windows we want to have a 'fixed' sky, so we can display chrome metal and have always the same look
        HDRISky m_DefaultPreviewSky;
#endif

        // Shared resources for sky rendering.
        IBLFilterBSDF[] m_IBLFilterArray;
        Vector4 m_CubemapScreenSize;
        Matrix4x4[] m_facePixelCoordToViewDirMatrices = new Matrix4x4[6];
        Matrix4x4[] m_CameraRelativeViewMatrices = new Matrix4x4[6];
        BuiltinSkyParameters m_BuiltinParameters = new BuiltinSkyParameters();
        ComputeShader m_ComputeAmbientProbeCS;
        static readonly int s_AmbientProbeOutputBufferParam = Shader.PropertyToID("_AmbientProbeOutputBuffer");
        static readonly int s_VolumetricAmbientProbeOutputBufferParam = Shader.PropertyToID("_VolumetricAmbientProbeOutputBuffer");
        static readonly int s_DiffuseAmbientProbeOutputBufferParam = Shader.PropertyToID("_DiffuseAmbientProbeOutputBuffer");
        static readonly int s_AmbientProbeInputCubemap = Shader.PropertyToID("_AmbientProbeInputCubemap");
        static readonly int s_FogParameters = Shader.PropertyToID("_FogParameters");
        int m_ComputeAmbientProbeKernel;
        int m_ComputeAmbientProbeVolumetricKernel;
        int m_ComputeAmbientProbeCloudsKernel;

        CubemapArray m_BlackCubemapArray;
        ComputeBuffer m_BlackAmbientProbeBuffer;

        // 2 by default: Static sky + one dynamic. Will grow if needed.
        DynamicArray<CachedSkyContext> m_CachedSkyContexts = new DynamicArray<CachedSkyContext>(2);

        public SkyManager()
        { }

        ~SkyManager()
        { }

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

        internal static CloudSettings GetCloudSetting(VolumeStack stack)
        {
            var visualEnv = stack.GetComponent<VisualEnvironment>();
            int cloudID = visualEnv.cloudType.value;
            Type cloudType;
            if (cloudTypesDict.TryGetValue(cloudID, out cloudType))
                return (CloudSettings)stack.GetComponent(cloudType);
            return null;
        }

        internal static VolumetricClouds GetVolumetricClouds(VolumeStack stack)
        {
            return stack.GetComponent<VolumetricClouds>();
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

        static void UpdateCloudTypes()
        {
            if (m_CloudTypesDict == null)
            {
                m_CloudTypesDict = new Dictionary<int, Type>();

                var types = CoreUtils.GetAllTypesDerivedFrom<CloudSettings>().Where(t => !t.IsAbstract);
                foreach (Type type in types)
                {
                    var uniqueIDs = type.GetCustomAttributes(typeof(CloudUniqueID), false);
                    if (uniqueIDs.Length == 0)
                    {
                        Debug.LogWarningFormat("Missing attribute CloudUniqueID on class {0}. Class won't be registered as an available cloud type.", type);
                    }
                    else
                    {
                        int uniqueID = ((CloudUniqueID)uniqueIDs[0]).uniqueID;
                        if (uniqueID == 0)
                        {
                            Debug.LogWarningFormat("0 is a reserved CloudUniqueID and is used in class {0}. Class won't be registered as an available cloud type.", type);
                            continue;
                        }

                        Type value;
                        if (m_CloudTypesDict.TryGetValue(uniqueID, out value))
                        {
                            Debug.LogWarningFormat("CloudUniqueID {0} used in class {1} is already used in class {2}. Class won't be registered as an available cloud type.", uniqueID, type, value);
                            continue;
                        }

                        m_CloudTypesDict.Add(uniqueID, type);
                    }
                }
            }
        }

        public void UpdateCurrentSkySettings(HDCamera hdCamera)
        {
            hdCamera.UpdateCurrentSky(this);
        }

        class SetGlobalSkyDataPassData
        {
            public BuiltinSkyParameters builtinParameters = new BuiltinSkyParameters();
            public SkyRenderer skyRenderer;
        }

        void SetGlobalSkyData(RenderGraph renderGraph, SkyUpdateContext skyContext)
        {
            if (IsCachedContextValid(skyContext) && skyContext.skyRenderer != null)
            {
                using (var builder = renderGraph.AddRenderPass<SetGlobalSkyDataPassData>("SetGlobalSkyData", out var passData))
                {
                    builder.AllowPassCulling(false);

                    m_BuiltinParameters.CopyTo(passData.builtinParameters);
                    passData.skyRenderer = skyContext.skyRenderer;
                    passData.builtinParameters.skySettings = skyContext.skySettings;
                    passData.builtinParameters.cloudSettings = skyContext.cloudSettings;
                    passData.builtinParameters.volumetricClouds = skyContext.volumetricClouds;

                    builder.SetRenderFunc(
                    (SetGlobalSkyDataPassData data, RenderGraphContext ctx) =>
                {
                    data.builtinParameters.commandBuffer = ctx.cmd;
                    data.skyRenderer.SetGlobalSkyData(ctx.cmd, data.builtinParameters);
                });
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

        public void Build(HDRenderPipelineAsset hdAsset, HDRenderPipelineRuntimeResources defaultResources, IBLFilterBSDF[] iblFilterBSDFArray)
        {
            m_Resolution = (int)hdAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;
            m_IBLFilterArray = iblFilterBSDFArray;

            m_StandardSkyboxMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.skyboxCubemapPS);
            m_BlitCubemapMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitCubemapPS);

            m_OpaqueAtmScatteringMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.opaqueAtmosphericScatteringPS);
            m_OpaqueAtmScatteringBlock = new MaterialPropertyBlock();

            m_ComputeAmbientProbeCS = HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.ambientProbeConvolutionCS;
            m_ComputeAmbientProbeKernel = m_ComputeAmbientProbeCS.FindKernel("AmbientProbeConvolutionDiffuse");
            m_ComputeAmbientProbeVolumetricKernel = m_ComputeAmbientProbeCS.FindKernel("AmbientProbeConvolutionDiffuseVolumetric");
            m_ComputeAmbientProbeCloudsKernel = m_ComputeAmbientProbeCS.FindKernel("AmbientProbeConvolutionClouds");

            lightingOverrideVolumeStack = VolumeManager.instance.CreateStack();
            lightingOverrideLayerMask = hdAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask;

            m_CubemapScreenSize = new Vector4((float)m_Resolution, (float)m_Resolution, 1.0f / (float)m_Resolution, 1.0f / (float)m_Resolution);

            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                var worldToView = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...

                m_facePixelCoordToViewDirMatrices[i] = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, Vector2.zero, m_CubemapScreenSize, worldToView, true);
                m_CameraRelativeViewMatrices[i] = worldToView;
            }

            InitializeBlackCubemapArray();

            // Initialize black ambient probe buffer
            if (m_BlackAmbientProbeBuffer == null)
            {
                // 27 SH Coeffs in 7 float4
                m_BlackAmbientProbeBuffer = new ComputeBuffer(7, 16);
                float[] blackValues = new float[28];
                for (int i = 0; i < 28; ++i)
                    blackValues[i] = 0.0f;
                m_BlackAmbientProbeBuffer.SetData(blackValues);
            }

#if UNITY_EDITOR
            UnityEditor.Lightmapping.bakeStarted += OnBakeStarted;
#endif
        }

        void InitializeBlackCubemapArray()
        {
            if (m_BlackCubemapArray == null)
            {
                m_BlackCubemapArray = new CubemapArray(1, m_IBLFilterArray.Length, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None)
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

            CoreUtils.Destroy(m_BlackCubemapArray);
            m_BlackAmbientProbeBuffer.Release();

            for (int i = 0; i < m_CachedSkyContexts.size; ++i)
                m_CachedSkyContexts[i].Cleanup();

            m_StaticLightingSky.Cleanup();
            lightingOverrideVolumeStack.Dispose();

#if UNITY_EDITOR
            CoreUtils.Destroy(m_DefaultPreviewSky);
            UnityEditor.Lightmapping.bakeStarted -= OnBakeStarted;
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

        ComputeBuffer GetDiffuseAmbientProbeBuffer(SkyUpdateContext skyContext)
        {
            if (skyContext.IsValid() && IsCachedContextValid(skyContext))
            {
                ref var context = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                return context.renderingContext.diffuseAmbientProbeBuffer;
            }
            else
            {
                return m_BlackAmbientProbeBuffer;
            }
        }

        ComputeBuffer GetVolumetricAmbientProbeBuffer(SkyUpdateContext skyContext)
        {
            if (skyContext.IsValid() && IsCachedContextValid(skyContext))
            {
                ref var context = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                return context.renderingContext.volumetricAmbientProbeBuffer;
            }
            else
            {
                return m_BlackAmbientProbeBuffer;
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

        SkyUpdateContext GetLightingSky(HDCamera hdCamera)
        {
            if (hdCamera.skyAmbientMode == SkyAmbientMode.Static
                || (hdCamera.camera.cameraType == CameraType.Reflection && HDRenderPipeline.currentPipeline.reflectionProbeBaking))
            {
                return m_StaticLightingSky;
            }

            return hdCamera.lightingSky;
        }

        // Return the value of the ambient probe
        internal SphericalHarmonicsL2 GetAmbientProbe(HDCamera hdCamera)
        {
            // If a camera just returns from being disabled, sky is not setup yet for it.
            if (hdCamera.lightingSky == null && hdCamera.skyAmbientMode == SkyAmbientMode.Dynamic)
            {
                return m_BlackAmbientProbe;
            }

            return GetAmbientProbe(GetLightingSky(hdCamera));
        }

        internal ComputeBuffer GetDiffuseAmbientProbeBuffer(HDCamera hdCamera)
        {
            // If a camera just returns from being disabled, sky is not setup yet for it.
            if (hdCamera.lightingSky == null && hdCamera.skyAmbientMode == SkyAmbientMode.Dynamic)
            {
                return m_BlackAmbientProbeBuffer;
            }

            return GetDiffuseAmbientProbeBuffer(GetLightingSky(hdCamera));
        }

        internal ComputeBuffer GetVolumetricAmbientProbeBuffer(HDCamera hdCamera)
        {
            // If a camera just returns from being disabled, sky is not setup yet for it.
            if (hdCamera.lightingSky == null && hdCamera.skyAmbientMode == SkyAmbientMode.Dynamic)
            {
                return m_BlackAmbientProbeBuffer;
            }

            return GetVolumetricAmbientProbeBuffer(GetLightingSky(hdCamera));
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
            // For preview camera don't update the skybox material. This can inadvertently trigger GI baking. case 1314361/1314373.
            if (hdCamera.camera.cameraType == CameraType.Preview)
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
            RenderSettings.customReflectionTexture = null;
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

        class RenderSkyToCubemapPassData
        {
            public BuiltinSkyParameters builtinParameters = new BuiltinSkyParameters();
            public SkyRenderer skyRenderer;
            public CloudRenderer cloudRenderer;
            public Matrix4x4[] cameraViewMatrices;
            public Matrix4x4[] facePixelCoordToViewDirMatrices;
            public bool includeSunInBaking;
            public TextureHandle output;
        }

        internal TextureHandle RenderSkyToCubemap(RenderGraph renderGraph, SkyUpdateContext skyContext, bool includeSunInBaking, bool renderCloudLayers, TextureHandle outputCubemap)
        {
            using (var builder = renderGraph.AddRenderPass<RenderSkyToCubemapPassData>("RenderSkyToCubemap", out var passData, ProfilingSampler.Get(HDProfileId.RenderSkyToCubemap)))
            {
                m_BuiltinParameters.CopyTo(passData.builtinParameters);
                passData.builtinParameters.skySettings = skyContext.skySettings;
                passData.skyRenderer = skyContext.skyRenderer;
                passData.cloudRenderer = renderCloudLayers ? skyContext.cloudRenderer : null;
                passData.cameraViewMatrices = m_CameraRelativeViewMatrices;
                passData.facePixelCoordToViewDirMatrices = m_facePixelCoordToViewDirMatrices;
                passData.includeSunInBaking = skyContext.skySettings.includeSunInBaking.value;
                passData.output = builder.WriteTexture(outputCubemap);

                builder.SetRenderFunc(
                (RenderSkyToCubemapPassData data, RenderGraphContext ctx) =>
                {
                    data.builtinParameters.commandBuffer = ctx.cmd;

                    for (int i = 0; i < 6; ++i)
                    {
                        data.builtinParameters.pixelCoordToViewDirMatrix = data.facePixelCoordToViewDirMatrices[i];
                        data.builtinParameters.viewMatrix = data.cameraViewMatrices[i];
                        data.builtinParameters.colorBuffer = data.output;
                        data.builtinParameters.depthBuffer = null;
                        data.builtinParameters.cubemapFace = (CubemapFace)i;

                        CoreUtils.SetRenderTarget(ctx.cmd, data.output, ClearFlag.None, 0, (CubemapFace)i);
                        data.skyRenderer.RenderSky(data.builtinParameters, true, data.includeSunInBaking);
                        if (data.cloudRenderer != null)
                            data.cloudRenderer.RenderClouds(data.builtinParameters, true);
                    }
                });

                return passData.output;
            }
        }

        class UpdateAmbientProbePassData
        {
            public ComputeShader computeAmbientProbeCS;
            public int computeAmbientProbeKernel;
            public TextureHandle skyCubemap;
            public ComputeBuffer ambientProbeResult;
            public ComputeBuffer diffuseAmbientProbeResult;
            public ComputeBuffer volumetricAmbientProbeResult;
            public Vector4 fogParameters;
            public Action<AsyncGPUReadbackRequest> callback;
        }

        internal void UpdateAmbientProbe(RenderGraph renderGraph, TextureHandle skyCubemap, bool outputForClouds, ComputeBuffer ambientProbeResult, ComputeBuffer diffuseAmbientProbeResult, ComputeBuffer volumetricAmbientProbeResult, Fog fog, Action<AsyncGPUReadbackRequest> callback)
        {
            using (var builder = renderGraph.AddRenderPass<UpdateAmbientProbePassData>("UpdateAmbientProbe", out var passData, ProfilingSampler.Get(HDProfileId.UpdateSkyAmbientProbe)))
            {
                passData.computeAmbientProbeCS = m_ComputeAmbientProbeCS;
                if (outputForClouds)
                    passData.computeAmbientProbeKernel = m_ComputeAmbientProbeCloudsKernel;
                else
                    passData.computeAmbientProbeKernel = volumetricAmbientProbeResult != null ? m_ComputeAmbientProbeVolumetricKernel : m_ComputeAmbientProbeKernel;

                passData.skyCubemap = builder.ReadTexture(skyCubemap);
                passData.ambientProbeResult = ambientProbeResult;
                passData.diffuseAmbientProbeResult = diffuseAmbientProbeResult;
                passData.volumetricAmbientProbeResult = volumetricAmbientProbeResult;
                passData.fogParameters = fog != null ? new Vector4(fog.globalLightProbeDimmer.value, fog.anisotropy.value, 0.0f, 0.0f) : Vector4.zero;
                passData.callback = callback;

                builder.SetRenderFunc(
                (UpdateAmbientProbePassData data, RenderGraphContext ctx) =>
                {
                    ctx.cmd.SetComputeBufferParam(data.computeAmbientProbeCS, data.computeAmbientProbeKernel, s_AmbientProbeOutputBufferParam, data.ambientProbeResult);
                    ctx.cmd.SetComputeTextureParam(data.computeAmbientProbeCS, data.computeAmbientProbeKernel, s_AmbientProbeInputCubemap, data.skyCubemap);
                    if (data.diffuseAmbientProbeResult != null)
                        ctx.cmd.SetComputeBufferParam(data.computeAmbientProbeCS, data.computeAmbientProbeKernel, s_DiffuseAmbientProbeOutputBufferParam, data.diffuseAmbientProbeResult);
                    if (data.volumetricAmbientProbeResult != null)
                    {
                        ctx.cmd.SetComputeBufferParam(data.computeAmbientProbeCS, data.computeAmbientProbeKernel, s_VolumetricAmbientProbeOutputBufferParam, data.volumetricAmbientProbeResult);
                        ctx.cmd.SetComputeVectorParam(data.computeAmbientProbeCS, s_FogParameters, data.fogParameters);
                    }
                    ctx.cmd.DispatchCompute(data.computeAmbientProbeCS, data.computeAmbientProbeKernel, 1, 1, 1);
                    ctx.cmd.RequestAsyncReadback(data.ambientProbeResult, data.callback);
                });
            }
        }

        TextureHandle GenerateSkyCubemap(RenderGraph renderGraph, SkyUpdateContext skyContext)
        {
            var renderingContext = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId].renderingContext;
            var volumetricClouds = skyContext.volumetricClouds;

            // TODO: Currently imported and not temporary only because of enlighten and the baking back-end requiring this texture instead of a more direct API.
            var outputCubemap = renderGraph.ImportTexture(renderingContext.skyboxCubemapRT);
            outputCubemap = RenderSkyToCubemap(renderGraph, skyContext, includeSunInBaking: skyContext.skySettings.includeSunInBaking.value, renderCloudLayers: true, outputCubemap);

            // Render the volumetric clouds into the cubemap
            if (skyContext.volumetricClouds != null)
            {
                // The volumetric clouds explicitly rely on the physically based sky. We need to make sure that the sun textures are properly bound.
                // Unfortunately, the global binding happens too late, so we need to bind it here.
                SetGlobalSkyData(renderGraph, skyContext);
                outputCubemap = HDRenderPipeline.currentPipeline.RenderVolumetricClouds_Sky(renderGraph, m_BuiltinParameters.hdCamera, m_facePixelCoordToViewDirMatrices,
                    m_BuiltinParameters.volumetricClouds, (int)m_BuiltinParameters.screenSize.x, (int)m_BuiltinParameters.screenSize.y, outputCubemap);
            }

            // Generate mipmap for our cubemap
            HDRenderPipeline.GenerateMipmaps(renderGraph, outputCubemap);

            return outputCubemap;
        }

        class SkyEnvironmentConvolutionPassData
        {
            public TextureHandle input;
            public TextureHandle intermediateTexture;
            public CubemapArray output; // Only instance of cubemap array in HDRP and RTHandles don't support them. Don't want to make a special API just for this case.
            public IBLFilterBSDF[] bsdfs;
        }

        void RenderCubemapGGXConvolution(RenderGraph renderGraph, TextureHandle input, CubemapArray output)
        {
            using (var builder = renderGraph.AddRenderPass<SkyEnvironmentConvolutionPassData>("UpdateSkyEnvironmentConvolution", out var passData, ProfilingSampler.Get(HDProfileId.UpdateSkyEnvironmentConvolution)))
            {
                passData.bsdfs = m_IBLFilterArray;
                passData.input = builder.ReadTexture(input);
                passData.output = output;
                passData.intermediateTexture = builder.CreateTransientTexture(new TextureDesc(m_Resolution, m_Resolution)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, dimension = TextureDimension.Cube, useMipMap = true, autoGenerateMips = false, filterMode = FilterMode.Trilinear, name = "SkyboxBSDFIntermediate" });

                builder.SetRenderFunc(
                (SkyEnvironmentConvolutionPassData data, RenderGraphContext ctx) =>
                {
                    for (int bsdfIdx = 0; bsdfIdx < data.bsdfs.Length; ++bsdfIdx)
                    {
                        // First of all filter this cubemap using the target filter
                        data.bsdfs[bsdfIdx].FilterCubemap(ctx.cmd, data.input, data.intermediateTexture);
                        // Then copy it to the cubemap array slice
                        for (int i = 0; i < 6; ++i)
                        {
                            ctx.cmd.CopyTexture(data.intermediateTexture, i, data.output, 6 * bsdfIdx + i);
                        }
                    }
                });
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

            // If we detected a big difference with previous settings, then carrying over the previous ambient probe is probably going to lead to unexpected result.
            // Instead we at least fallback to a neutral one until async readback has finished.
            if (skyContext.settingsHadBigDifferenceWithPrev)
                context.renderingContext.ClearAmbientProbe();

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
            if (sunLight != null && skyContext.skyRenderer.SupportDynamicSunLight)
                sunHash = GetSunLightHashCode(sunLight);

            // For planar reflections we want to use the parent position for hash.
            Camera cameraForHash = camera.camera;
            if (camera.camera.cameraType == CameraType.Reflection && camera.parentCamera != null)
            {
                cameraForHash = camera.parentCamera;
            }

            int skyHash = sunHash * 23 + skyContext.skySettings.GetHashCode(cameraForHash);
            if (skyContext.HasClouds())
                skyHash = skyHash * 23 + skyContext.cloudSettings.GetHashCode(cameraForHash);
            if (skyContext.HasVolumetricClouds())
            {
                skyHash = skyHash * 23 + skyContext.volumetricClouds.GetHashCode();
                skyHash = skyHash * 23 + camera.frameSettings.IsEnabled(FrameSettingsField.FullResolutionCloudsForSky).GetHashCode();
            }
            skyHash = skyHash * 23 + (staticSky ? 1 : 0);
            skyHash = skyHash * 23 + (ambientMode == SkyAmbientMode.Static ? 1 : 0);

            // These parameters have an effect on the ambient probe computed for volumetric lighting. Therefore we need to include them to the hash.
            if (camera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics))
            {
                var fog = camera.volumeStack.GetComponent<Fog>();
                skyHash = skyHash * 23 + fog.globalLightProbeDimmer.GetHashCode();
                skyHash = skyHash * 23 + fog.anisotropy.GetHashCode();
            }
            return skyHash;
        }

        public void RequestEnvironmentUpdate()
        {
            m_UpdateRequired = true;
        }

        internal void RequestStaticEnvironmentUpdate()
        {
            m_StaticSkyUpdateRequired = true;
        }

        public void UpdateEnvironment(
            RenderGraph renderGraph,
            HDCamera hdCamera,
            SkyUpdateContext skyContext,
            Light sunLight,
            bool updateRequired,
            bool updateAmbientProbe,
            bool staticSky,
            SkyAmbientMode ambientMode)
        {
            if (skyContext.IsValid())
            {
                using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.UpdateEnvironment)))
                {
                    skyContext.currentUpdateTime += hdCamera.deltaTime;

                    m_BuiltinParameters.commandBuffer = null;
                    m_BuiltinParameters.hdCamera = hdCamera;
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
                    m_BuiltinParameters.frameIndex = (int)hdCamera.GetCameraFrameCount();
                    m_BuiltinParameters.skySettings = skyContext.skySettings;
                    m_BuiltinParameters.cloudSettings = skyContext.cloudSettings;
                    m_BuiltinParameters.volumetricClouds = skyContext.volumetricClouds;

                    // When update is not requested and the context is already valid (ie: already computed at least once),
                    // we need to early out in two cases:
                    // - updateMode is "OnDemand" in which case we never update unless explicitly requested
                    // - updateMode is "Realtime" in which case we only update if the time threshold for realtime update is passed.
                    if (IsCachedContextValid(skyContext) && !updateRequired)
                    {
                        if (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.OnDemand)
                            return;
                        else if (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.Realtime && skyContext.currentUpdateTime < skyContext.skySettings.updatePeriod.value)
                            return;
                    }

                    int skyHash = ComputeSkyHash(hdCamera, skyContext, sunLight, ambientMode, staticSky);
                    bool forceUpdate = updateRequired;

                    // Acquire the rendering context, if the context was invalid or the hash has changed, this will request for an update.
                    forceUpdate |= AcquireSkyRenderingContext(skyContext, skyHash, staticSky ? "SkyboxCubemap_Static" : "SkyboxCubemap", !staticSky);

                    ref CachedSkyContext cachedContext = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                    var renderingContext = cachedContext.renderingContext;

                    if (IsCachedContextValid(skyContext))
                    {
                        forceUpdate |= skyContext.skyRenderer.DoUpdate(m_BuiltinParameters);
                        forceUpdate |= (skyContext.HasClouds() && skyContext.cloudRenderer.DoUpdate(m_BuiltinParameters));
                    }

                    if (forceUpdate ||
                        (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.OnChanged && skyHash != skyContext.skyParametersHash) ||
                        (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.Realtime && skyContext.currentUpdateTime > skyContext.skySettings.updatePeriod.value))
                    {
                        var skyCubemap = GenerateSkyCubemap(renderGraph, skyContext);

                        if (updateAmbientProbe)
                        {
                            UpdateAmbientProbe(renderGraph, skyCubemap, outputForClouds: false,
                                renderingContext.ambientProbeResult, renderingContext.diffuseAmbientProbeBuffer, renderingContext.volumetricAmbientProbeBuffer,
                                hdCamera.volumeStack.GetComponent<Fog>(), renderingContext.OnComputeAmbientProbeDone);
                        }

                        if (renderingContext.supportsConvolution)
                            RenderCubemapGGXConvolution(renderGraph, skyCubemap, renderingContext.skyboxBSDFCubemapArray);

                        skyContext.skyParametersHash = skyHash;
                        skyContext.currentUpdateTime = 0.0f;

#if UNITY_EDITOR
                            // In the editor when we change the sky we want to make the GI dirty so when baking again the new sky is taken into account.
                            // Changing the hash of the rendertarget allow to say that GI is dirty
                            renderingContext.skyboxCubemapRT.rt.imageContentsHash = new Hash128((uint)skyHash, 0, 0, 0);
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

        public void UpdateEnvironment(RenderGraph renderGraph, HDCamera hdCamera, Light sunLight)
        {
            SkyAmbientMode ambientMode = hdCamera.volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;

            UpdateEnvironment(renderGraph, hdCamera, hdCamera.lightingSky, sunLight, m_UpdateRequired, ambientMode == SkyAmbientMode.Dynamic, false, ambientMode);

            // Preview camera will have a different sun, therefore the hash for the static lighting sky will change and force a recomputation
            // because we only maintain one static sky. Since we don't care that the static lighting may be a bit different in the preview we never recompute
            // and we use the one from the main camera.
            bool forceStaticUpdate = false;
            StaticLightingSky staticLightingSky = GetStaticLightingSky();
#if UNITY_EDITOR
            // In the editor, we might need the static sky ready for baking lightmaps/lightprobes regardless of the current ambient mode so we force it to update in this case if it's not been computed yet..
            // We always force an update of the static sky when we're in scene view mode. Previous behaviour was to prevent forced updates if the hash of the static sky was non-null, but this was preventing
            // the lightmapper from updating in response to changes in environment. See GFXGI-237 for a better description of this issue.

            forceStaticUpdate = hdCamera.camera.cameraType == CameraType.SceneView;
#endif
            if ((ambientMode == SkyAmbientMode.Static || forceStaticUpdate) && hdCamera.camera.cameraType != CameraType.Preview)
            {
                m_StaticLightingSky.skySettings = staticLightingSky != null ? staticLightingSky.skySettings : null;
                m_StaticLightingSky.cloudSettings = staticLightingSky != null ? staticLightingSky.cloudSettings : null;
                m_StaticLightingSky.volumetricClouds = staticLightingSky != null ? staticLightingSky.volumetricClouds : null;
                UpdateEnvironment(renderGraph, hdCamera, m_StaticLightingSky, sunLight, m_StaticSkyUpdateRequired || m_UpdateRequired, true, true, SkyAmbientMode.Static);
                m_StaticSkyUpdateRequired = false;
            }

            m_UpdateRequired = false;

            SetGlobalSkyData(renderGraph, hdCamera.lightingSky);

            // Keep global setter for now. We should probably remove it and set it explicitly where needed like any other resource. As is it breaks resource lifetime contract with render graph.
            HDRenderPipeline.SetGlobalTexture(renderGraph, HDShaderIDs._SkyTexture, GetReflectionTexture(hdCamera.lightingSky));
            HDRenderPipeline.SetGlobalBuffer(renderGraph, HDShaderIDs._AmbientProbeData, GetDiffuseAmbientProbeBuffer(hdCamera));
        }

        internal void UpdateBuiltinParameters(SkyUpdateContext skyContext, HDCamera hdCamera, Light sunLight, RTHandle colorBuffer, RTHandle depthBuffer, DebugDisplaySettings debugSettings, CommandBuffer cmd)
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
            m_BuiltinParameters.frameIndex = (int)hdCamera.GetCameraFrameCount();
            m_BuiltinParameters.skySettings = skyContext.skySettings;
            m_BuiltinParameters.cloudSettings = skyContext.cloudSettings;
            m_BuiltinParameters.volumetricClouds = skyContext.volumetricClouds;
        }

        public bool TryGetCloudSettings(HDCamera hdCamera, out CloudSettings cloudSettings, out CloudRenderer cloudRenderer)
        {
            var skyContext = hdCamera.visualSky;
            cloudSettings = skyContext.cloudSettings;
            cloudRenderer = skyContext.cloudRenderer;
            return skyContext.HasClouds();
        }

        public bool RequiresPreRenderSky(HDCamera hdCamera)
        {
            var skyContext = hdCamera.visualSky;
            return skyContext.IsValid() && (skyContext.skyRenderer.RequiresPreRender(skyContext.skySettings) ||
                (skyContext.HasClouds() && skyContext.cloudRenderer.RequiresPreRenderClouds(m_BuiltinParameters)));
        }

        public void PreRenderSky(HDCamera hdCamera, Light sunLight, RTHandle colorBuffer, RTHandle normalBuffer, RTHandle depthBuffer, DebugDisplaySettings debugSettings, CommandBuffer cmd)
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
                    cmd);

                bool preRenderSky = skyContext.skyRenderer.RequiresPreRender(skyContext.skySettings);
                if (preRenderSky)
                {
                    SkyAmbientMode ambientMode = hdCamera.volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;
                    int skyHash = ComputeSkyHash(hdCamera, skyContext, sunLight, ambientMode);
                    AcquireSkyRenderingContext(skyContext, skyHash);
                    skyContext.skyRenderer.DoUpdate(m_BuiltinParameters);
                }

                if (depthBuffer != BuiltinSkyParameters.nullRT && normalBuffer != BuiltinSkyParameters.nullRT)
                {
                    CoreUtils.SetRenderTarget(cmd, normalBuffer, depthBuffer);
                }
                else if (depthBuffer != BuiltinSkyParameters.nullRT)
                {
                    CoreUtils.SetRenderTarget(cmd, depthBuffer);
                }

                if (preRenderSky)
                    skyContext.skyRenderer.PreRenderSky(m_BuiltinParameters);

                if (skyContext.HasClouds() && skyContext.cloudRenderer.RequiresPreRenderClouds(m_BuiltinParameters))
                {
                    skyContext.cloudRenderer.DoUpdate(m_BuiltinParameters);
                    skyContext.cloudRenderer.PreRenderClouds(m_BuiltinParameters, false);
                }
            }
        }

        public void RenderSky(HDCamera hdCamera, Light sunLight, RTHandle colorBuffer, RTHandle depthBuffer, DebugDisplaySettings debugSettings, CommandBuffer cmd)
        {
            if (hdCamera.clearColorMode != HDAdditionalCameraData.ClearColorMode.Sky ||
                // If the luxmeter is enabled, we don't render the sky
                debugSettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuxMeter)
                return;

            var skyContext = hdCamera.visualSky;
            if (skyContext.IsValid())
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderSky)))
                {
                    UpdateBuiltinParameters(skyContext,
                        hdCamera,
                        sunLight,
                        colorBuffer,
                        depthBuffer,
                        debugSettings,
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

                    // When rendering the visual sky for reflection probes, we need to remove the sun disk if skySettings.includeSunInBaking is false.
                    skyContext.skyRenderer.RenderSky(m_BuiltinParameters, false, hdCamera.camera.cameraType != CameraType.Reflection || skyContext.skySettings.includeSunInBaking.value);
                }

                if (!skyContext.HasClouds())
                    return;

                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderClouds)))
                {
                    skyContext.cloudRenderer.DoUpdate(m_BuiltinParameters);
                    skyContext.cloudRenderer.RenderClouds(m_BuiltinParameters, false);
                }
            }
        }

        public void RenderOpaqueAtmosphericScattering(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle colorBuffer,
            RTHandle depthTexture,
            RTHandle volumetricLighting,
            RTHandle intermediateBuffer,
            RTHandle depthBuffer,
            Matrix4x4 pixelCoordToViewDirWS, bool isMSAA)
        {
            using (new ProfilingScope(m_BuiltinParameters.commandBuffer, ProfilingSampler.Get(HDProfileId.OpaqueAtmosphericScattering)))
            {
                m_OpaqueAtmScatteringBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, pixelCoordToViewDirWS);
                m_OpaqueAtmScatteringBlock.SetTexture(isMSAA ? HDShaderIDs._DepthTextureMS : HDShaderIDs._CameraDepthTexture, depthTexture);

                // The texture can be null when volumetrics are disabled.
                if (volumetricLighting != null)
                    m_OpaqueAtmScatteringBlock.SetTexture(HDShaderIDs._VBufferLighting, volumetricLighting);

                if (Fog.IsPBRFogEnabled(hdCamera))
                {
                    m_OpaqueAtmScatteringBlock.SetTexture(isMSAA ? HDShaderIDs._ColorTextureMS : HDShaderIDs._ColorTexture, colorBuffer);

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

            var tempRT = new RenderTexture(resolution * 6, resolution, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                dimension = TextureDimension.Tex2D,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Trilinear
            };
            tempRT.Create();

            var temp = new Texture2D(resolution * 6, resolution, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);
            var result = new Texture2D(resolution * 6, resolution, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);

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
            if (!HDRenderPipeline.isReady)
                return;

            // Happens sometime in the tests.
            if (m_StandardSkyboxMaterial == null)
                m_StandardSkyboxMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.skyboxCubemapPS);

            // It is possible that HDRP hasn't rendered any frame when clicking the bake lighting button.
            // This can happen when baked lighting debug are used for example and no other window with HDRP is visible.
            // This will result in the static lighting cubemap not being up to date with what the user put in the Environment Lighting panel.
            // We detect this here (basically we just check if the skySetting in the currently processed m_StaticLightingSky is the same as the one the user set).
            // And issue a warning if applicable.
            var staticLightingSky = GetStaticLightingSky();
            if (staticLightingSky != null && staticLightingSky.skySettings != m_StaticLightingSky.skySettings)
            {
                Debug.LogWarning("Static Lighting Sky is not ready for baking. Please make sure that at least one frame has been rendered with HDRP before baking. For example you can achieve this by having Scene View visible with Draw Mode set to Shaded.");
            }

            // At the start of baking we need to update the GI system with the static lighting sky in order for lightmaps and probes to be baked with it.
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
            RenderSettings.customReflectionTexture = null;

            DynamicGI.UpdateEnvironment();
        }

#endif
    }
}
