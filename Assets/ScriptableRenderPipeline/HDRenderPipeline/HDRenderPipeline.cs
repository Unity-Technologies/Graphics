using UnityEngine.Rendering;
using System;
using System.Linq;
using UnityEngine.Experimental.Rendering.HDPipeline.TilePass;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    // This HDRenderPipeline assume linear lighting. Don't work with gamma.
    public class HDRenderPipeline : RenderPipelineAsset
    {
        const string k_HDRenderPipelinePath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/HDRenderPipeline.asset";

#if UNITY_EDITOR
        [MenuItem("RenderPipeline/Create HDRenderPipeline")]
        static void CreateHDRenderPipeline()
        {
            var instance = CreateInstance<HDRenderPipeline>();
            AssetDatabase.CreateAsset(instance, k_HDRenderPipelinePath);
        }

        [UnityEditor.MenuItem("HDRenderPipeline/UpdateHDRenderPipeline")]
        static void UpdateHDRenderPipeline()
        {
            var guids = AssetDatabase.FindAssets("t:HDRenderPipeline");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var pipeline = AssetDatabase.LoadAssetAtPath<HDRenderPipeline>(path);
                EditorUtility.SetDirty(pipeline);
            }
        }

        [UnityEditor.MenuItem("HDRenderPipeline/Add \"Additional Light Data\" (if not present)")]
        static void AddAdditionalLightData()
        {
            Light[] lights = FindObjectsOfType(typeof(Light)) as Light[];

            foreach (Light light in lights)
            {
                // Do not add a component if there already is one.
                if (light.GetComponent<AdditionalLightData>() == null)
                {
                    light.gameObject.AddComponent<AdditionalLightData>();
                }
            }
        }

#endif

        private HDRenderPipeline()
        {}

        [SerializeField]
        private LightLoopProducer m_LightLoopProducer;
        public LightLoopProducer lightLoopProducer
        {
            get { return m_LightLoopProducer; }
            set { m_LightLoopProducer = value; }
        }

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new HDRenderPipelineInstance(this);
        }

        // NOTE:
        // All those properties are public because of how HDRenderPipelineInspector retrieve those properties via serialization/reflection
        // Those that are not will be refatored later.

        // Debugging
        public GlobalDebugSettings          globalDebugSettings = new GlobalDebugSettings();

        // Renderer Settings (per project)
        public RenderingSettings            renderingSettings = new RenderingSettings();
        public SubsurfaceScatteringSettings sssSettings = new SubsurfaceScatteringSettings();

        [SerializeField]
        ShadowSettings                      m_ShadowSettings = ShadowSettings.Default;
        [SerializeField] TextureSettings    m_TextureSettings = TextureSettings.Default;

        public ShadowSettings shadowSettings                { get { return m_ShadowSettings; } }
        public TextureSettings textureSettings              { get { return m_TextureSettings; } set { m_TextureSettings = value; } }

        // Renderer Settings (per "scene")
        [SerializeField] private CommonSettings.Settings    m_CommonSettings = CommonSettings.Settings.s_Defaultsettings;
        [SerializeField] private SkySettings                m_SkySettings;

        public CommonSettings.Settings commonSettingsToUse
        {
            get
        {
                if (CommonSettingsSingleton.overrideSettings)
                    return CommonSettingsSingleton.overrideSettings.settings;

                return m_CommonSettings;
            }
        }

        public SkySettings skySettings
        {
            get { return m_SkySettings; }
            set { m_SkySettings = value; }
        }

        public SkySettings skySettingsToUse
        {
            get
            {
                if (SkySettingsSingleton.overrideSettings)
                    return SkySettingsSingleton.overrideSettings;

                return m_SkySettings;
            }
        }
        
        public void ApplyDebugSettings()
        {
            m_ShadowSettings.enabled = globalDebugSettings.lightingDebugSettings.enableShadows;

            LightingDebugSettings lightDebugSettings = globalDebugSettings.lightingDebugSettings;
            Vector4 debugModeAndAlbedo = new Vector4((float)lightDebugSettings.lightingDebugMode, lightDebugSettings.debugLightingAlbedo.r, lightDebugSettings.debugLightingAlbedo.g, lightDebugSettings.debugLightingAlbedo.b);
            Vector4 debugSmoothness = new Vector4(lightDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);
            Shader.SetGlobalVector("_DebugLightModeAndAlbedo", debugModeAndAlbedo);
            Shader.SetGlobalVector("_DebugLightingSmoothness", debugSmoothness);

        }

        public void UpdateCommonSettings()
        {
            var commonSettings = commonSettingsToUse;

            m_ShadowSettings.directionalLightCascadeCount = commonSettings.shadowCascadeCount;
            m_ShadowSettings.directionalLightCascades = new Vector3(commonSettings.shadowCascadeSplit0, commonSettings.shadowCascadeSplit1, commonSettings.shadowCascadeSplit2);
            m_ShadowSettings.maxShadowDistance = commonSettings.shadowMaxDistance;
            m_ShadowSettings.directionalLightNearPlaneOffset = commonSettings.shadowNearPlaneOffset;
        }

        public void OnValidate()
        {
            globalDebugSettings.OnValidate();
            sssSettings.OnValidate();
        }
    }

    [Serializable]
    public class RenderingSettings
    {
        public bool useForwardRenderingOnly = false; // TODO: Currently there is no way to strip the extra forward shaders generated by the shaders compiler, so we can switch dynamically.
        public bool useDepthPrepass = false;

        // we have to fallback to forward-only rendering when scene view is using wireframe rendering mode --
        // as rendering everything in wireframe + deferred do not play well together
        public bool ShouldUseForwardRenderingOnly()
        {
            return useForwardRenderingOnly || GL.wireframe;
        }
    }

    public struct HDCamera
    {
        public Camera    camera;
        public Vector4   screenSize;
        public Matrix4x4 viewProjectionMatrix;
        public Matrix4x4 invViewProjectionMatrix;
        public Matrix4x4 invProjectionMatrix;
        public Vector4   invProjectionParam;
    }

    public class GBufferManager
    {
        public const int MaxGbuffer = 8;

        public void SetBufferDescription(int index, string stringId, RenderTextureFormat inFormat, RenderTextureReadWrite inSRGBWrite)
        {
            IDs[index] = Shader.PropertyToID(stringId);
            RTIDs[index] = new RenderTargetIdentifier(IDs[index]);
            formats[index] = inFormat;
            sRGBWrites[index] = inSRGBWrite;
        }

        public void InitGBuffers(int width, int height, CommandBuffer cmd)
        {
            for (int index = 0; index < gbufferCount; index++)
            {
            /* RTs[index] = */
            cmd.GetTemporaryRT(IDs[index], width, height, 0, FilterMode.Point, formats[index], sRGBWrites[index]);
            }
        }

        public RenderTargetIdentifier[] GetGBuffers()
        {
            var colorMRTs = new RenderTargetIdentifier[gbufferCount];
            for (int index = 0; index < gbufferCount; index++)
            {
                colorMRTs[index] = RTIDs[index];
            }

            return colorMRTs;
        }

        /*
        public void BindBuffers(Material mat)
        {
            for (int index = 0; index < gbufferCount; index++)
            {
                mat.SetTexture(IDs[index], RTs[index]);
            }
        }
        */

        public int gbufferCount { get; set; }
        int[] IDs = new int[MaxGbuffer];
        RenderTargetIdentifier[] RTIDs = new RenderTargetIdentifier[MaxGbuffer];
        RenderTextureFormat[] formats = new RenderTextureFormat[MaxGbuffer];
        RenderTextureReadWrite[] sRGBWrites = new RenderTextureReadWrite[MaxGbuffer];
    }

    public class HDRenderPipelineInstance : RenderPipeline
    {
        private readonly HDRenderPipeline m_Owner;

        // TODO: Find a way to automatically create/iterate through deferred material
        // TODO TO CHECK: SebL I move allocation from Build() to here, but there was a comment "// Our object can be garbage collected, so need to be allocate here", it is still true ?
        private readonly Lit.RenderLoop m_LitRenderLoop = new Lit.RenderLoop();

        readonly GBufferManager m_gbufferManager = new GBufferManager();

        // Various set of material use in render loop
        readonly Material m_FilterSubsurfaceScattering;
        readonly Material m_FilterAndCombineSubsurfaceScattering;
        
        private Material m_DebugDisplayShadowMap;
        private Material m_DebugViewMaterialGBuffer;
        private Material m_DebugDisplayLatlong;
        
        // Various buffer
        readonly int m_CameraColorBuffer;
        readonly int m_CameraSubsurfaceBuffer;
        readonly int m_CameraFilteringBuffer;
        readonly int m_VelocityBuffer;
        readonly int m_DistortionBuffer;

        // 'm_CameraColorBuffer' does not contain diffuse lighting of SSS materials until the SSS pass.
        // It is stored within 'm_CameraSubsurfaceBufferRT'.
        readonly RenderTargetIdentifier m_CameraColorBufferRT;
        readonly RenderTargetIdentifier m_CameraSubsurfaceBufferRT;
        readonly RenderTargetIdentifier m_CameraFilteringBufferRT;
        readonly RenderTargetIdentifier m_VelocityBufferRT;
        readonly RenderTargetIdentifier m_DistortionBufferRT;

        private RenderTexture m_CameraDepthStencilBuffer = null;
        private RenderTexture m_CameraDepthStencilBufferCopy = null;
        private RenderTargetIdentifier m_CameraDepthStencilBufferRT;
        private RenderTargetIdentifier m_CameraDepthStencilBufferCopyRT;

        // Detect when windows size is changing
        int m_CurrentWidth;
        int m_CurrentHeight;

        ShadowRenderPass m_ShadowPass;
        ShadowOutput m_ShadowsResult = new ShadowOutput();

        public int GetCurrentShadowCount() { return m_ShadowsResult.shadowLights == null ? 0 : m_ShadowsResult.shadowLights.Length; }

        readonly SkyManager m_SkyManager = new SkyManager();
        private readonly BaseLightLoop m_LightLoop;

        private GlobalDebugSettings globalDebugSettings
        {
            get { return m_Owner.globalDebugSettings; }
        }

        public HDRenderPipelineInstance(HDRenderPipeline owner)
        {
            m_Owner = owner;

            m_CameraColorBuffer             = Shader.PropertyToID("_CameraColorTexture");
            m_CameraSubsurfaceBuffer        = Shader.PropertyToID("_CameraSubsurfaceTexture");
            m_CameraFilteringBuffer         = Shader.PropertyToID("_CameraFilteringBuffer");

            m_CameraColorBufferRT               = new RenderTargetIdentifier(m_CameraColorBuffer);
            m_CameraSubsurfaceBufferRT          = new RenderTargetIdentifier(m_CameraSubsurfaceBuffer);
            m_CameraFilteringBufferRT           = new RenderTargetIdentifier(m_CameraFilteringBuffer);

            m_FilterSubsurfaceScattering = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/CombineSubsurfaceScattering");
            m_FilterSubsurfaceScattering.DisableKeyword("FILTER_HORIZONTAL_AND_COMBINE");
            m_FilterSubsurfaceScattering.SetFloat("_DstBlend", (float)BlendMode.Zero);

            m_FilterAndCombineSubsurfaceScattering = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/CombineSubsurfaceScattering");
            m_FilterSubsurfaceScattering.EnableKeyword("FILTER_HORIZONTAL_AND_COMBINE");
            m_FilterAndCombineSubsurfaceScattering.SetFloat("_DstBlend", (float)BlendMode.One);

            InitializeDebugMaterials();

            m_ShadowPass = new ShadowRenderPass(owner.shadowSettings);

            // Init Gbuffer description
            m_gbufferManager.gbufferCount = m_LitRenderLoop.GetMaterialGBufferCount();
            RenderTextureFormat[] RTFormat;
            RenderTextureReadWrite[] RTReadWrite;
            m_LitRenderLoop.GetMaterialGBufferDescription(out RTFormat, out RTReadWrite);

            for (int gbufferIndex = 0; gbufferIndex < m_gbufferManager.gbufferCount; ++gbufferIndex)
            {
                m_gbufferManager.SetBufferDescription(gbufferIndex, "_GBufferTexture" + gbufferIndex, RTFormat[gbufferIndex], RTReadWrite[gbufferIndex]);
            }

            m_VelocityBuffer = Shader.PropertyToID("_VelocityTexture");
            if (ShaderConfig.s_VelocityInGbuffer == 1)
            {
                // If velocity is in GBuffer then it is in the last RT. Assign a different name to it.
                m_gbufferManager.SetBufferDescription(m_gbufferManager.gbufferCount, "_VelocityTexture", Builtin.RenderLoop.GetVelocityBufferFormat(), Builtin.RenderLoop.GetVelocityBufferReadWrite());
                m_gbufferManager.gbufferCount++;
            }
            m_VelocityBufferRT = new RenderTargetIdentifier(m_VelocityBuffer);

            m_DistortionBuffer = Shader.PropertyToID("_DistortionTexture");
            m_DistortionBufferRT = new RenderTargetIdentifier(m_DistortionBuffer);

            m_LitRenderLoop.Build();

            if (owner.lightLoopProducer)
                m_LightLoop = owner.lightLoopProducer.CreateLightLoop();

            if(m_LightLoop != null)
                m_LightLoop.Build(owner.textureSettings);

            m_SkyManager.Build();
            m_SkyManager.skySettings = owner.skySettingsToUse;
        }

        void InitializeDebugMaterials()
        {
            m_DebugDisplayShadowMap = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DebugDisplayShadowMap");
            m_DebugViewMaterialGBuffer = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DebugViewMaterialGBuffer");
            m_DebugDisplayLatlong = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DebugDisplayLatlong");
        }

        public override void Dispose()
        {
            base.Dispose();

            if (m_LightLoop != null)
                m_LightLoop.Cleanup();

            m_LitRenderLoop.Cleanup();

            Utilities.Destroy(m_DebugViewMaterialGBuffer);
            Utilities.Destroy(m_DebugDisplayShadowMap);

            m_SkyManager.Cleanup();

#if UNITY_EDITOR
            SupportedRenderingFeatures.active = SupportedRenderingFeatures.Default;
#endif
        }

#if UNITY_EDITOR
        private static readonly SupportedRenderingFeatures s_NeededFeatures = new SupportedRenderingFeatures()
        {
            reflectionProbe = SupportedRenderingFeatures.ReflectionProbe.Rotation
        };
#endif

        void CreateDepthBuffer(Camera camera)
        {
            if (m_CameraDepthStencilBuffer != null)
            {
                m_CameraDepthStencilBuffer.Release();
            }
            
            m_CameraDepthStencilBuffer = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.Depth);
            m_CameraDepthStencilBuffer.filterMode = FilterMode.Point;
            m_CameraDepthStencilBuffer.Create();
            m_CameraDepthStencilBufferRT = new RenderTargetIdentifier(m_CameraDepthStencilBuffer);

            if (NeedDepthBufferCopy())
            {
                if (m_CameraDepthStencilBufferCopy != null)
                {
                    m_CameraDepthStencilBufferCopy.Release();
                }
                m_CameraDepthStencilBufferCopy = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.Depth);
                m_CameraDepthStencilBufferCopy.filterMode = FilterMode.Point;
                m_CameraDepthStencilBufferCopy.Create();
                m_CameraDepthStencilBufferCopyRT = new RenderTargetIdentifier(m_CameraDepthStencilBufferCopy);
            }            
        }

        void Resize(Camera camera)
        {
            // TODO: Detect if renderdoc just load and force a resize in this case, as often renderdoc require to realloc resource.

            // TODO: This is the wrong way to handle resize/allocation. We can have several different camera here, mean that the loop on camera will allocate and deallocate
            // the below buffer which is bad. Best is to have a set of buffer for each camera that is persistent and reallocate resource if need
            // For now consider we have only one camera that go to this code, the main one.
            m_SkyManager.skySettings = m_Owner.skySettingsToUse;
            m_SkyManager.Resize(camera.nearClipPlane, camera.farClipPlane); // TODO: Also a bad naming, here we just want to realloc texture if skyparameters change (usefull for lookdev)

            if (m_LightLoop == null)
                return;

            bool resolutionChanged = camera.pixelWidth != m_CurrentWidth || camera.pixelHeight != m_CurrentHeight;

            if (resolutionChanged || m_CameraDepthStencilBuffer == null)
            {
                CreateDepthBuffer(camera);
            }

            if (resolutionChanged || m_LightLoop.NeedResize())
            {
                if (m_CurrentWidth > 0 && m_CurrentHeight > 0)
                {
                    m_LightLoop.ReleaseResolutionDependentBuffers();
                }

                m_LightLoop.AllocResolutionDependentBuffers(camera.pixelWidth, camera.pixelHeight);
            }

            // update recorded window resolution
            m_CurrentWidth = camera.pixelWidth;
            m_CurrentHeight = camera.pixelHeight;
        }

        public void PushGlobalParams(HDCamera hdCamera, ScriptableRenderContext renderContext, SubsurfaceScatteringSettings sssParameters)
        {
            var cmd = new CommandBuffer {name = "Push Global Parameters"};

            cmd.SetGlobalVector("_ScreenSize",        hdCamera.screenSize);
            cmd.SetGlobalMatrix("_ViewProjMatrix",    hdCamera.viewProjectionMatrix);
            cmd.SetGlobalMatrix("_InvViewProjMatrix", hdCamera.invViewProjectionMatrix);
            cmd.SetGlobalMatrix("_InvProjMatrix",     hdCamera.invProjectionMatrix);
            cmd.SetGlobalVector("_InvProjParam",      hdCamera.invProjectionParam);

            // TODO: cmd.SetGlobalInt() does not exist, so we are forced to use Shader.SetGlobalInt() instead.

            if (m_SkyManager.IsSkyValid())
            {
                m_SkyManager.SetGlobalSkyTexture();
                Shader.SetGlobalInt("_EnvLightSkyEnabled", 1);
            }
            else
            {
                Shader.SetGlobalInt("_EnvLightSkyEnabled", 0);
            }

            // Broadcast SSS parameters to all shaders.
            Shader.SetGlobalInt("_TransmissionFlags", sssParameters.transmissionFlags);
            cmd.SetGlobalFloatArray("_ThicknessRemaps", sssParameters.thicknessRemaps);
            cmd.SetGlobalVectorArray("_HalfRcpVariancesAndLerpWeights", sssParameters.halfRcpVariancesAndLerpWeights);

            switch (sssParameters.texturingMode)
            {
                case SubsurfaceScatteringSettings.TexturingMode.PreScatter:
                    cmd.EnableShaderKeyword("SSS_PRE_SCATTER_TEXTURING");
                    cmd.DisableShaderKeyword("SSS_POST_SCATTER_TEXTURING");
                    break;
                case SubsurfaceScatteringSettings.TexturingMode.PostScatter:
                    cmd.DisableShaderKeyword("SSS_PRE_SCATTER_TEXTURING");
                    cmd.EnableShaderKeyword("SSS_POST_SCATTER_TEXTURING");
                    break;
                case SubsurfaceScatteringSettings.TexturingMode.PreAndPostScatter:
                    cmd.DisableShaderKeyword("SSS_PRE_SCATTER_TEXTURING");
                    cmd.DisableShaderKeyword("SSS_POST_SCATTER_TEXTURING");
                    break;
            }

            if (globalDebugSettings.renderingDebugSettings.enableSSS)
            {
                cmd.EnableShaderKeyword("_SUBSURFACE_SCATTERING");
            }
            else
            {
                cmd.DisableShaderKeyword("_SUBSURFACE_SCATTERING");
            }

            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            if (m_LightLoop != null)
                m_LightLoop.PushGlobalParams(hdCamera.camera, renderContext);
        }

        bool NeedDepthBufferCopy()
        {
            // For now we consider only PS4 to be able to read from a bound depth buffer. Need to test/implement for other platforms.
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation4;
        }

        Texture GetDepthTexture()
        {
            if (NeedDepthBufferCopy())
                return m_CameraDepthStencilBufferCopy;
            else
                return m_CameraDepthStencilBuffer;
        }

        private void CopyDepthBufferIfNeeded(ScriptableRenderContext renderContext)
        {
            var cmd = new CommandBuffer();
            if (NeedDepthBufferCopy())
            {
                using (new Utilities.ProfilingSample("Copy depth-stencil buffer", renderContext))
                {
                    cmd.CopyTexture(m_CameraDepthStencilBufferRT, m_CameraDepthStencilBufferCopyRT);
                }
            }

            cmd.SetGlobalTexture("_MainDepthTexture", GetDepthTexture());
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            base.Render(renderContext, cameras);

#if UNITY_EDITOR
            SupportedRenderingFeatures.active = s_NeededFeatures;
#endif

            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;

            if (!m_LitRenderLoop.isInit)
                m_LitRenderLoop.RenderInit(renderContext);

            // Do anything we need to do upon a new frame.

            if (m_LightLoop != null)
                m_LightLoop.NewFrame();

            m_Owner.ApplyDebugSettings();
            m_Owner.UpdateCommonSettings();

            // Set Frame constant buffer
            // TODO...

            // we only want to render one camera for now
            // select the most main camera!

            Camera camera = cameras.OrderByDescending(x => x.tag == "MainCamera").FirstOrDefault();
            if (camera == null)
                return;

            // Set camera constant buffer
            // TODO...

            CullingParameters cullingParams;
            if (!CullResults.GetCullingParameters(camera, out cullingParams))
                return;

            m_ShadowPass.UpdateCullingParameters(ref cullingParams);

            var cullResults = CullResults.Cull(ref cullingParams, renderContext);

            Resize(camera);

            renderContext.SetupCameraProperties(camera);

            HDCamera hdCamera = Utilities.GetHDCamera(camera);

            // TODO: Find a correct place to bind these material textures
            // We have to bind the material specific global parameters in this mode
            m_LitRenderLoop.Bind();

            InitAndClearBuffer(camera, renderContext);

            RenderDepthPrepass(cullResults, camera, renderContext);

            // Forward opaque with deferred/cluster tile require that we fill the depth buffer
            // correctly to build the light list.
            // TODO: avoid double lighting by tagging stencil or gbuffer that we must not lit.
            RenderForwardOnlyOpaqueDepthPrepass(cullResults, camera, renderContext);
            RenderGBuffer(cullResults, camera, renderContext);

            // If full forward rendering, we did not do any rendering yet, so don't need to copy the buffer.
            // If Deferred then the depth buffer is full (regular GBuffer + ForwardOnly depth prepass are done so we can copy it safely.
            if(!m_Owner.renderingSettings.useForwardRenderingOnly)
            {
                CopyDepthBufferIfNeeded(renderContext);
            }

            if (globalDebugSettings.materialDebugSettings.debugViewMaterial != 0)
            {
                RenderDebugViewMaterial(cullResults, hdCamera, renderContext);
            }
            else
            {
                using (new Utilities.ProfilingSample("Shadow Pass", renderContext))
                {
                    m_ShadowPass.Render(renderContext, cullResults, out m_ShadowsResult);
                }

                renderContext.SetupCameraProperties(camera); // Need to recall SetupCameraProperties after m_ShadowPass.Render

                if (m_LightLoop != null)
                {
                    using (new Utilities.ProfilingSample("Build Light list", renderContext))
                    {
                        m_LightLoop.PrepareLightsForGPU(m_Owner.shadowSettings, cullResults, camera, ref m_ShadowsResult);
                        m_LightLoop.RenderShadows(renderContext, cullResults);
                        renderContext.SetupCameraProperties(camera); // Need to recall SetupCameraProperties after m_ShadowPass.Render
                        m_LightLoop.BuildGPULightLists(camera, renderContext, m_CameraDepthStencilBufferRT); // TODO: Use async compute here to run light culling during shadow
                    }
                }

                PushGlobalParams(hdCamera, renderContext, m_Owner.sssSettings);

                // Caution: We require sun light here as some sky use the sun light to render, mean UpdateSkyEnvironment
                // must be call after BuildGPULightLists.
                // TODO: Try to arrange code so we can trigger this call earlier and use async compute here to run sky convolution during other passes (once we move convolution shader to compute).
                UpdateSkyEnvironment(hdCamera, renderContext);

                RenderDeferredLighting(hdCamera, renderContext, m_Owner.globalDebugSettings.renderingDebugSettings.enableSSS);

                // We compute subsurface scattering here. Therefore, no objects rendered afterwards will exhibit SSS.
                // Currently, there is no efficient way to switch between SRT and MRT for the forward pass;
                // therefore, forward-rendered objects do not output split lighting required for the SSS pass.
                CombineSubsurfaceScattering(hdCamera, renderContext, m_Owner.sssSettings);

                // For opaque forward we have split rendering in two categories
                // Material that are always forward and material that can be deferred or forward depends on render pipeline options (like switch to rendering forward only mode)
                // Material that are always forward are unlit and complex (Like Hair) and don't require sorting, so it is ok to split them.
                RenderForward(cullResults, camera, renderContext, true); // Render deferred or forward opaque
                RenderForwardOnlyOpaque(cullResults, camera, renderContext);

                // If full forward rendering, we did just rendered everything, so we can copy the depth buffer
                // If Deferred nothing needs copying anymore.
                if(m_Owner.renderingSettings.useForwardRenderingOnly)
                {
                    CopyDepthBufferIfNeeded(renderContext);
                }

                RenderSky(hdCamera, renderContext);

                // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
                RenderForward(cullResults, camera, renderContext, false);

                // Planar and real time cubemap doesn't need post process and render in FP16
                if (camera.cameraType == CameraType.Reflection)
                {
                    // Simple blit
                    var cmd = new CommandBuffer { name = "Blit to final RT" };
                    cmd.Blit(m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
                    renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Dispose();
                }
                else
                {
                    RenderVelocity(cullResults, camera, renderContext); // Note we may have to render velocity earlier if we do temporalAO, temporal volumetric etc... Mean we will not take into account forward opaque in case of deferred rendering ?

                    // TODO: Check with VFX team.
                    // Rendering distortion here have off course lot of artifact.
                    // But resolving at each objects that write in distortion is not possible (need to sort transparent, render those that do not distort, then resolve, then etc...)
                    // Instead we chose to apply distortion at the end after we cumulate distortion vector and desired blurriness. This
                    RenderDistortion(cullResults, camera, renderContext);

                    FinalPass(camera, renderContext);
                }
            }

            RenderDebugOverlay(camera, renderContext);

            // bind depth surface for editor grid/gizmo/selection rendering
            if (camera.cameraType == CameraType.SceneView)
            {
                var cmd = new CommandBuffer();
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, m_CameraDepthStencilBufferRT);
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            renderContext.Submit();
        }

        void RenderOpaqueRenderList(CullResults cull, Camera camera, ScriptableRenderContext renderContext, string passName, RendererConfiguration rendererConfiguration = 0)
        {
            if (!globalDebugSettings.renderingDebugSettings.displayOpaqueObjects)
                return;

            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName(passName))
            {
                rendererConfiguration = rendererConfiguration,
                sorting = { flags = SortFlags.CommonOpaque }
            };
            settings.inputFilter.SetQueuesOpaque();
            renderContext.DrawRenderers(ref settings);
        }

        void RenderTransparentRenderList(CullResults cull, Camera camera, ScriptableRenderContext renderContext, string passName, RendererConfiguration rendererConfiguration = 0)
        {
            if (!globalDebugSettings.renderingDebugSettings.displayTransparentObjects)
                return;

            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName(passName))
            {
                rendererConfiguration = rendererConfiguration,
                sorting = { flags = SortFlags.CommonTransparent }
            };
            settings.inputFilter.SetQueuesTransparent();
            renderContext.DrawRenderers(ref settings);
        }

        void RenderDepthPrepass(CullResults cull, Camera camera, ScriptableRenderContext renderContext)
        {
            // If we are forward only we will do a depth prepass
            // TODO: Depth prepass should be enabled based on light loop settings. LightLoop define if they need a depth prepass + forward only...
            if (!m_Owner.renderingSettings.useDepthPrepass)
                return;

            using (new Utilities.ProfilingSample("Depth Prepass", renderContext))
            {
                // TODO: Must do opaque then alpha masked for performance!
                // TODO: front to back for opaque and by materal for opaque tested when we split in two
                Utilities.SetRenderTarget(renderContext, m_CameraDepthStencilBufferRT);
                RenderOpaqueRenderList(cull, camera, renderContext, "DepthOnly");
            }
        }

        void RenderGBuffer(CullResults cull, Camera camera, ScriptableRenderContext renderContext)
        {
            if (m_Owner.renderingSettings.ShouldUseForwardRenderingOnly())
            {
                return ;
            }

            using (new Utilities.ProfilingSample("GBuffer Pass", renderContext))
            {
                bool debugLighting = globalDebugSettings.lightingDebugSettings.lightingDebugMode != LightingDebugMode.None;

                // setup GBuffer for rendering
                Utilities.SetRenderTarget(renderContext, m_gbufferManager.GetGBuffers(), m_CameraDepthStencilBufferRT);
                // render opaque objects into GBuffer
                RenderOpaqueRenderList(cull, camera, renderContext, debugLighting ? "GBufferDebugLighting" : "GBuffer", Utilities.kRendererConfigurationBakedLighting);
            }
        }

        // This pass is use in case of forward opaque and deferred rendering. We need to render forward objects before tile lighting pass
        void RenderForwardOnlyOpaqueDepthPrepass(CullResults cull, Camera camera, ScriptableRenderContext renderContext)
        {
            // If we are forward only we don't need to render ForwardOnlyOpaqueDepthOnly object
            // But in case we request a prepass we render it
            if (m_Owner.renderingSettings.ShouldUseForwardRenderingOnly() && !m_Owner.renderingSettings.useDepthPrepass)
                return;

            using (new Utilities.ProfilingSample("Forward opaque depth", renderContext))
            {
                Utilities.SetRenderTarget(renderContext, m_CameraDepthStencilBufferRT);
                RenderOpaqueRenderList(cull, camera, renderContext, "ForwardOnlyOpaqueDepthOnly");
            }
        }

        void RenderDebugViewMaterial(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("DebugView Material Mode Pass", renderContext))
            // Render Opaque forward
            {
                Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, Utilities.kClearAll, Color.black);

                Shader.SetGlobalInt("_DebugViewMaterial", (int)globalDebugSettings.materialDebugSettings.debugViewMaterial);

                RenderOpaqueRenderList(cull, hdCamera.camera, renderContext, "DebugViewMaterial", Utilities.kRendererConfigurationBakedLighting);
            }

            // Render GBuffer opaque
            if (!m_Owner.renderingSettings.ShouldUseForwardRenderingOnly())
            {
                Utilities.SetupMaterialHDCamera(hdCamera, m_DebugViewMaterialGBuffer);
                m_DebugViewMaterialGBuffer.SetFloat("_DebugViewMaterial", (float)globalDebugSettings.materialDebugSettings.debugViewMaterial);

                // m_gbufferManager.BindBuffers(m_DebugViewMaterialGBuffer);
                // TODO: Bind depth textures
                var cmd = new CommandBuffer { name = "GBuffer Debug Pass" };
                cmd.Blit(null, m_CameraColorBufferRT, m_DebugViewMaterialGBuffer, 0);
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            // Render forward transparent
            {
                RenderTransparentRenderList(cull, hdCamera.camera, renderContext, "DebugViewMaterial", Utilities.kRendererConfigurationBakedLighting);
            }

            // Last blit
            {
                var cmd = new CommandBuffer { name = "Blit DebugView Material Debug" };
                cmd.Blit(m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }
        }

        void RenderDeferredLighting(HDCamera hdCamera, ScriptableRenderContext renderContext, bool enableSSS)
        {
            if (m_Owner.renderingSettings.ShouldUseForwardRenderingOnly() || m_LightLoop == null)
            {
                return ;
            }

            RenderTargetIdentifier[] colorRTs = { m_CameraColorBufferRT, m_CameraSubsurfaceBufferRT };

            if (enableSSS)
            {
                // Output split lighting for materials tagged with the SSS stencil bit.
                m_LightLoop.RenderDeferredLighting(hdCamera, renderContext, globalDebugSettings.lightingDebugSettings, colorRTs, m_CameraDepthStencilBufferRT, new RenderTargetIdentifier(GetDepthTexture()), true, enableSSS);
            }

            // Output combined lighting for all the other materials.
            m_LightLoop.RenderDeferredLighting(hdCamera, renderContext, globalDebugSettings.lightingDebugSettings, colorRTs, m_CameraDepthStencilBufferRT, new RenderTargetIdentifier(GetDepthTexture()), false, enableSSS);
        }

        // Combines specular lighting and diffuse lighting with subsurface scattering.
        void CombineSubsurfaceScattering(HDCamera hdCamera, ScriptableRenderContext context, SubsurfaceScatteringSettings sssParameters)
        {
            // Currently, forward-rendered objects do not output split lighting required for the SSS pass.
            if (m_Owner.renderingSettings.ShouldUseForwardRenderingOnly()) return;

            if (!globalDebugSettings.renderingDebugSettings.enableSSS) return;

            var cmd = new CommandBuffer() { name = "Subsurface Scattering Pass" };

            // Perform the vertical SSS filtering pass.
            m_FilterSubsurfaceScattering.SetVectorArray("_FilterKernels", sssParameters.filterKernels);
            m_FilterSubsurfaceScattering.SetVectorArray("_HalfRcpWeightedVariances", sssParameters.halfRcpWeightedVariances);
            cmd.SetGlobalTexture("_IrradianceSource", m_CameraSubsurfaceBufferRT);
            Utilities.DrawFullScreen(cmd, m_FilterSubsurfaceScattering, hdCamera,
                                     m_CameraFilteringBufferRT, m_CameraDepthStencilBufferRT);

            // Perform the horizontal SSS filtering pass, and combine diffuse and specular lighting.
            m_FilterAndCombineSubsurfaceScattering.SetVectorArray("_FilterKernels", sssParameters.filterKernels);
            m_FilterAndCombineSubsurfaceScattering.SetVectorArray("_HalfRcpWeightedVariances", sssParameters.halfRcpWeightedVariances);
            cmd.SetGlobalTexture("_IrradianceSource", m_CameraFilteringBufferRT);
            Utilities.DrawFullScreen(cmd, m_FilterAndCombineSubsurfaceScattering, hdCamera,
                                     m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);

            context.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        void UpdateSkyEnvironment(HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            m_SkyManager.UpdateEnvironment(hdCamera, m_LightLoop == null ? null :  m_LightLoop.GetCurrentSunLight(), renderContext);
        }

        void RenderSky(HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            m_SkyManager.RenderSky(hdCamera, m_LightLoop == null ? null : m_LightLoop.GetCurrentSunLight(), m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, renderContext);
        }

        void RenderForward(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext, bool renderOpaque)
        {
            // TODO: Currently we can't render opaque object forward when deferred is enabled
            // miss option
            if (!m_Owner.renderingSettings.ShouldUseForwardRenderingOnly() && renderOpaque)
                return;

            using (new Utilities.ProfilingSample("Forward Pass", renderContext))
            {
                Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);

                if (m_LightLoop != null)
                    m_LightLoop.RenderForward(camera, renderContext, renderOpaque);

                bool debugLighting = globalDebugSettings.lightingDebugSettings.lightingDebugMode != LightingDebugMode.None;

                string forwardPassName = debugLighting ? "ForwardDebugLighting" : "Forward";
                if (renderOpaque)
                {
                    RenderOpaqueRenderList(cullResults, camera, renderContext, forwardPassName, Utilities.kRendererConfigurationBakedLighting);
                }
                else
                {
                    RenderTransparentRenderList(cullResults, camera, renderContext, forwardPassName, Utilities.kRendererConfigurationBakedLighting);
                }
            }
        }

        // Render material that are forward opaque only (like eye), this include unlit material
        void RenderForwardOnlyOpaque(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("Forward Only Pass", renderContext))
            {
                Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);

                if (m_LightLoop != null)
                    m_LightLoop.RenderForward(camera, renderContext, true);

                bool debugLighting = globalDebugSettings.lightingDebugSettings.lightingDebugMode != LightingDebugMode.None;
                RenderOpaqueRenderList(cullResults, camera, renderContext, debugLighting ? "ForwardOnlyOpaqueDebugLighting" : "ForwardOnlyOpaque", Utilities.kRendererConfigurationBakedLighting);
            }
        }

        void RenderVelocity(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("Velocity Pass", renderContext))
            {
                // If opaque velocity have been render during GBuffer no need to render it here
                if ((ShaderConfig.s_VelocityInGbuffer == 1) || m_Owner.renderingSettings.ShouldUseForwardRenderingOnly())
                    return ;

                int w = camera.pixelWidth;
                int h = camera.pixelHeight;

                var cmd = new CommandBuffer { name = "" };
                cmd.GetTemporaryRT(m_VelocityBuffer, w, h, 0, FilterMode.Point, Builtin.RenderLoop.GetVelocityBufferFormat(), Builtin.RenderLoop.GetVelocityBufferReadWrite());
                cmd.SetRenderTarget(m_VelocityBufferRT, m_CameraDepthStencilBufferRT);
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                RenderOpaqueRenderList(cullResults, camera, renderContext, "MotionVectors");
            }
        }

        void RenderDistortion(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext)
        {
            if (!globalDebugSettings.renderingDebugSettings.enableDistortion)
                return ;

            using (new Utilities.ProfilingSample("Distortion Pass", renderContext))
            {
                int w = camera.pixelWidth;
                int h = camera.pixelHeight;

                var cmd = new CommandBuffer { name = "" };
                cmd.GetTemporaryRT(m_DistortionBuffer, w, h, 0, FilterMode.Point, Builtin.RenderLoop.GetDistortionBufferFormat(), Builtin.RenderLoop.GetDistortionBufferReadWrite());
                cmd.SetRenderTarget(m_DistortionBufferRT, m_CameraDepthStencilBufferRT);
                cmd.ClearRenderTarget(false, true, Color.black); // TODO: can we avoid this clear for performance ?
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                // Only transparent object can render distortion vectors
                RenderTransparentRenderList(cullResults, camera, renderContext, "DistortionVectors");
            }
        }

        void FinalPass(Camera camera, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("Final Pass", renderContext))
            {
                // All of this is temporary, sub-optimal and quickly hacked together but is necessary
                // for artists to do lighting work until the fully-featured framework is ready

                var localPostProcess = camera.GetComponent<PostProcessing>();

                bool localActive = localPostProcess != null && localPostProcess.enabled;

                if (!localActive)
                {
                    var cmd = new CommandBuffer { name = "" };
                    cmd.Blit(m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
                    renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Dispose();
                    return;
                }

                localPostProcess.Render(camera, renderContext, m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
            }
        }

        void NextOverlayCoord(ref float x, ref float y, float overlaySize, float width)
        {
            x += overlaySize;
            // Go to next line if it goes outside the screen.
            if (x + overlaySize > width)
            {
                x = 0;
                y -= overlaySize;
            }
        }

        void RenderDebugOverlay(Camera camera, ScriptableRenderContext renderContext)
        {
            // We don't want any overlay for these kind of rendering
            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                return;

            CommandBuffer debugCB = new CommandBuffer();
            debugCB.name = "Debug Overlay";

            float x = 0;
            float overlayRatio = globalDebugSettings.debugOverlayRatio;
            float overlaySize = Math.Min(camera.pixelHeight, camera.pixelWidth) * overlayRatio;
            float y = camera.pixelHeight - overlaySize;

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

            LightingDebugSettings lightingDebug = globalDebugSettings.lightingDebugSettings;

            if (lightingDebug.shadowDebugMode != ShadowMapDebugMode.None)
            {
                if (lightingDebug.shadowDebugMode == ShadowMapDebugMode.VisualizeShadowMap)
                {
                    uint visualizeShadowIndex = Math.Min(lightingDebug.shadowMapIndex, (uint)(GetCurrentShadowCount() - 1));
                    ShadowLight shadowLight = m_ShadowsResult.shadowLights[visualizeShadowIndex];
                    for (int slice = 0; slice < shadowLight.shadowSliceCount; ++slice)
                    {
                        ShadowSliceData sliceData = m_ShadowsResult.shadowSlices[shadowLight.shadowSliceIndex + slice];

                        Vector4 texcoordScaleBias = new Vector4((float)sliceData.shadowResolution / m_Owner.shadowSettings.shadowAtlasWidth,
                                                                (float)sliceData.shadowResolution / m_Owner.shadowSettings.shadowAtlasHeight,
                                                                (float)sliceData.atlasX / m_Owner.shadowSettings.shadowAtlasWidth,
                                                                (float)sliceData.atlasY / m_Owner.shadowSettings.shadowAtlasHeight);

                        propertyBlock.SetVector("_TextureScaleBias", texcoordScaleBias);

                        debugCB.SetViewport(new Rect(x, y, overlaySize, overlaySize));
                        debugCB.DrawProcedural(Matrix4x4.identity, m_DebugDisplayShadowMap, 0, MeshTopology.Triangles, 3, 1, propertyBlock);

                        NextOverlayCoord(ref x, ref y, overlaySize, camera.pixelWidth);
                    }
                }
                else if (lightingDebug.shadowDebugMode == ShadowMapDebugMode.VisualizeAtlas)
                {
                    propertyBlock.SetVector("_TextureScaleBias", new Vector4(1.0f, 1.0f, 0.0f, 0.0f));

                    debugCB.SetViewport(new Rect(x, y, overlaySize, overlaySize));
                    debugCB.DrawProcedural(Matrix4x4.identity, m_DebugDisplayShadowMap, 0, MeshTopology.Triangles, 3, 1, propertyBlock);

                    NextOverlayCoord(ref x, ref y, overlaySize, camera.pixelWidth);
                }
            }

            if(lightingDebug.displaySkyReflection)
            {
                Texture skyReflection = m_SkyManager.skyReflection;
                propertyBlock.SetTexture("_InputCubemap", skyReflection);
                propertyBlock.SetFloat("_Mipmap", lightingDebug.skyReflectionMipmap);
                debugCB.SetViewport(new Rect(x, y, overlaySize, overlaySize));
                debugCB.DrawProcedural(Matrix4x4.identity, m_DebugDisplayLatlong, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
                NextOverlayCoord(ref x, ref y, overlaySize, camera.pixelWidth);
            }

            renderContext.ExecuteCommandBuffer(debugCB);
        }

        // Function to prepare light structure for GPU lighting
        void PrepareLightsForGPU(ShadowSettings shadowSettings, CullResults cullResults, Camera camera, ref ShadowOutput shadowOutput)
        {
            // build per tile light lists
            if (m_LightLoop != null)
                m_LightLoop.PrepareLightsForGPU(shadowSettings, cullResults, camera, ref shadowOutput);
        }

        void InitAndClearBuffer(Camera camera, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("InitAndClearBuffer", renderContext))
            {
                // We clear only the depth buffer, no need to clear the various color buffer as we overwrite them.
                // Clear depth/stencil and init buffers
                using (new Utilities.ProfilingSample("InitGBuffers and clear Depth/Stencil", renderContext))
                {
                    var cmd = new CommandBuffer();
                    cmd.name = "";

                    // Init buffer
                    // With scriptable render loop we must allocate ourself depth and color buffer (We must be independent of backbuffer for now, hope to fix that later).
                    // Also we manage ourself the HDR format, here allocating fp16 directly.
                    // With scriptable render loop we can allocate temporary RT in a command buffer, they will not be release with ExecuteCommandBuffer
                    // These temporary surface are release automatically at the end of the scriptable render pipeline if not release explicitly
                    int w = camera.pixelWidth;
                    int h = camera.pixelHeight;

                    cmd.GetTemporaryRT(m_CameraColorBuffer,             w, h,  0, FilterMode.Point, RenderTextureFormat.ARGBHalf,       RenderTextureReadWrite.Linear, 1, true); // Enable UAV
                    cmd.GetTemporaryRT(m_CameraSubsurfaceBuffer,        w, h,  0, FilterMode.Point, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear, 1, true); // Enable UAV
                    cmd.GetTemporaryRT(m_CameraFilteringBuffer,         w, h,  0, FilterMode.Point, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear, 1, true); // Enable UAV

                    if (!m_Owner.renderingSettings.ShouldUseForwardRenderingOnly())
                    {
                        m_gbufferManager.InitGBuffers(w, h, cmd);
                    }

                    renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Dispose();

                    Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.ClearDepth);
                }

                // Clear the diffuse SSS lighting target
                using (new Utilities.ProfilingSample("Clear SSS diffuse target", renderContext))
                {
                    Utilities.SetRenderTarget(renderContext, m_CameraSubsurfaceBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.ClearColor, Color.black);
                }

                // Clear the SSS filtering target
                using (new Utilities.ProfilingSample("Clear SSS filtering target", renderContext))
                {
                    Utilities.SetRenderTarget(renderContext, m_CameraFilteringBuffer, m_CameraDepthStencilBufferRT, ClearFlag.ClearColor, Color.black);
                }

                // TEMP: As we are in development and have not all the setup pass we still clear the color in emissive buffer and gbuffer, but this will be removed later.

                // Clear the HDR target
                using (new Utilities.ProfilingSample("Clear HDR target", renderContext))
                {
                    Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.ClearColor, Color.black);
                }

                // Clear GBuffers
                if (!m_Owner.renderingSettings.ShouldUseForwardRenderingOnly())
                {
                    using (new Utilities.ProfilingSample("Clear GBuffer", renderContext))
                    {
                        Utilities.SetRenderTarget(renderContext, m_gbufferManager.GetGBuffers(), m_CameraDepthStencilBufferRT, ClearFlag.ClearColor, Color.black);
                    }
                }
                // END TEMP
            }
        }
    }
}
