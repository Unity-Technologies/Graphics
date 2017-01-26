using UnityEngine.Rendering;
using System;
using UnityEditor;
using System.Linq;
using UnityEngine.Experimental.Rendering.HDPipeline.TilePass;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    // This HDRenderPipeline assume linear lighting. Don't work with gamma.
    public class HDRenderPipeline : RenderPipelineAsset
    {
        const string k_HDRenderPipelinePath = "Assets/ScriptableRenderLoop/HDRenderPipeline/HDRenderPipeline.asset";

#if UNITY_EDITOR
        [MenuItem("RenderPipeline/CreateHDRenderPipeline")]
        static void CreateHDRenderPipeline()
        {
            var instance = CreateInstance<HDRenderPipeline>();
            AssetDatabase.CreateAsset(instance, k_HDRenderPipelinePath);

            instance.m_Setup = AssetDatabase.LoadAssetAtPath<HDRenderPipelineSetup>("Assets/HDRenderPipelineSetup.asset");
        }

        [UnityEditor.MenuItem("RenderPipeline/UpdateHDLoop")]
        static void UpdateHDLoop()
        {
            var guids = AssetDatabase.FindAssets("t:HDRenderPipeline");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var loop = AssetDatabase.LoadAssetAtPath<HDRenderPipeline>(path);
                loop.m_Setup = AssetDatabase.LoadAssetAtPath<HDRenderPipelineSetup>("Assets/HDRenderPipelineSetup.asset");
                EditorUtility.SetDirty(loop);
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
        { }

        [SerializeField]
        private HDRenderPipelineSetup m_Setup;

        public HDRenderPipelineSetup renderPipelineSetup
        {
            get { return m_Setup; }
        }

        [SerializeField]
        private CommonSettings.Settings m_CommonSettings = CommonSettings.Settings.s_Defaultsettings;

        public CommonSettings.Settings commonSettingsToUse
        {
            get
            {
                if (CommonSettingsSingleton.overrideSettings)
                    return CommonSettingsSingleton.overrideSettings.settings;

                return m_CommonSettings;
            }
        }

        [SerializeField]
        private SkyParameters m_SkyParameters;

        public SkyParameters skyParametersToUse
        {
            get
        {
                if (SkyParametersSingleton.overrideSettings)
                    return SkyParametersSingleton.overrideSettings;

                return m_SkyParameters;
            }
        }

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new HDRenderPipelineInstance(this);
        }

        readonly DebugParameters m_DebugParameters = new DebugParameters();
        public DebugParameters debugParameters
        {
            get { return m_DebugParameters; }
        }

        [SerializeField]
        ShadowSettings m_ShadowSettings = ShadowSettings.Default;

        public ShadowSettings shadowSettings
        {
            get { return m_ShadowSettings; }
        }

        [SerializeField]
        TextureSettings m_TextureSettings = TextureSettings.Default;

        public TextureSettings textureSettings
        {
            get { return m_TextureSettings; }
            set { m_TextureSettings = value; }
        }

        [SerializeField]
        TileSettings m_TileSettings = new TileSettings();

        public TileSettings tileSettings
        {
            get { return m_TileSettings; }
        }

        public void UpdateCommonSettings()
        {
            var commonSettings = commonSettingsToUse;
            m_ShadowSettings.directionalLightCascadeCount = commonSettings.shadowCascadeCount;
            m_ShadowSettings.directionalLightCascades = new Vector3(commonSettings.shadowCascadeSplit0, commonSettings.shadowCascadeSplit1, commonSettings.shadowCascadeSplit2);
            m_ShadowSettings.maxShadowDistance = commonSettings.shadowMaxDistance;
        }
        }

    [Serializable]
    public class TileSettings
        {
        public bool enableDrawLightBoundsDebug = false;
        public bool disableTileAndCluster = true; // For debug / test
        public bool disableDeferredShadingInCompute = true;
        public bool enableSplitLightEvaluation = true;
        public bool enableComputeLightEvaluation = false;

        // clustered light list specific buffers and data begin
        public int debugViewTilesFlags = 0;
        public bool enableClustered = false;
        public bool disableFptlWhenClustered = true;    // still useful on opaques. Should be false by default to force tile on opaque.
        public bool enableBigTilePrepass = false;
    }

    public struct HDCamera
    {
        public Camera camera;
        public Vector4 screenSize;
        public Matrix4x4 viewProjectionMatrix;
        public Matrix4x4 invViewProjectionMatrix;
        }

        public class DebugParameters
        {
            // Material Debugging
            public int debugViewMaterial = 0;

            // Rendering debugging
            public bool displayOpaqueObjects = true;
            public bool displayTransparentObjects = true;

            public bool useForwardRenderingOnly = false; // TODO: Currently there is no way to strip the extra forward shaders generated by the shaders compiler, so we can switch dynamically.
            public bool useDepthPrepass = false;
            public bool useDistortion = true;

            // we have to fallback to forward-only rendering when scene view is using wireframe rendering mode --
            // as rendering everything in wireframe + deferred do not play well together
            public bool ShouldUseForwardRenderingOnly () { return useForwardRenderingOnly || GL.wireframe; }
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
        readonly Material m_DebugViewMaterialGBuffer;

        // Various buffer
        readonly int m_CameraColorBuffer;
        readonly int m_CameraDepthBuffer;
        readonly int m_VelocityBuffer;
        readonly int m_DistortionBuffer;

        readonly RenderTargetIdentifier m_CameraColorBufferRT;
        readonly RenderTargetIdentifier m_CameraDepthBufferRT;
        readonly RenderTargetIdentifier m_VelocityBufferRT;
        readonly RenderTargetIdentifier m_DistortionBufferRT;

        // Detect when windows size is changing
        int m_CurrentWidth;
        int m_CurrentHeight;

        ShadowRenderPass m_ShadowPass;

        readonly SkyManager m_SkyManager = new SkyManager();

        private DebugParameters debugParameters
        {
            get { return m_Owner.debugParameters; }
        }

        public HDRenderPipelineInstance(HDRenderPipeline owner)
        {
            m_Owner = owner;

            m_CameraColorBuffer = Shader.PropertyToID("_CameraColorTexture");
            m_CameraDepthBuffer  = Shader.PropertyToID("_CameraDepthTexture");

            m_CameraColorBufferRT = new RenderTargetIdentifier(m_CameraColorBuffer);
            m_CameraDepthBufferRT = new RenderTargetIdentifier(m_CameraDepthBuffer);

            m_DebugViewMaterialGBuffer = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DebugViewMaterialGBuffer");

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
            
            m_lightLoop = new LightLoop(owner);
            m_lightLoop.Build(owner.textureSettings);

            m_SkyManager.skyParameters = owner.skyParametersToUse;
        }

        public override void Dispose()
        {
            base.Dispose();

            m_lightLoop.Cleanup();
            m_LitRenderLoop.Cleanup();

            Utilities.Destroy(m_DebugViewMaterialGBuffer);
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

        private LightLoop m_lightLoop;
#endif

        void Resize(Camera camera)
        {
            // TODO: Detect if renderdoc just load and force a resize in this case, as often renderdoc require to realloc resource.

            // TODO: This is the wrong way to handle resize/allocation. We can have several different camera here, mean that the loop on camera will allocate and deallocate
            // the below buffer which is bad. Best is to have a set of buffer for each camera that is persistent and reallocate resource if need
            // For now consider we have only one camera that go to this code, the main one.
            m_SkyManager.skyParameters = m_Owner.skyParametersToUse;
            m_SkyManager.Resize(camera.nearClipPlane, camera.farClipPlane); // TODO: Also a bad naming, here we just want to realloc texture if skyparameters change (usefull for lookdev)

            if (camera.pixelWidth != m_CurrentWidth || camera.pixelHeight != m_CurrentHeight || m_lightLoop.NeedResize())
            {
                if (m_CurrentWidth > 0 && m_CurrentHeight > 0)
                {
                    m_lightLoop.ReleaseResolutionDependentBuffers();
                }

                m_lightLoop.AllocResolutionDependentBuffers(camera.pixelWidth, camera.pixelHeight);

                // update recorded window resolution
                m_CurrentWidth = camera.pixelWidth;
                m_CurrentHeight = camera.pixelHeight;
            }
        }

        public void PushGlobalParams(HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            if (m_SkyManager.IsSkyValid())
            {
                m_SkyManager.SetGlobalSkyTexture();
                Shader.SetGlobalInt("_EnvLightSkyEnabled", 1);
            }
            else
                    {
                Shader.SetGlobalInt("_EnvLightSkyEnabled", 0);
                    }

            var cmd = new CommandBuffer { name = "Push Global Parameters" };

            cmd.SetGlobalVector("_ScreenSize", hdCamera.screenSize);
            cmd.SetGlobalMatrix("_ViewProjMatrix", hdCamera.viewProjectionMatrix);
            cmd.SetGlobalMatrix("_InvViewProjMatrix", hdCamera.invViewProjectionMatrix);

                    renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Dispose();

            m_lightLoop.PushGlobalParams(hdCamera.camera, renderContext);
                }

        public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            base.Render(renderContext, cameras);

#if UNITY_EDITOR
            SupportedRenderingFeatures.active = s_NeededFeatures;
#endif

            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;

            m_SkyManager.Build();

            if (!m_LitRenderLoop.isInit)
                m_LitRenderLoop.RenderInit(renderContext);

            // Do anything we need to do upon a new frame.
            m_lightLoop.NewFrame();

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

            if (debugParameters.debugViewMaterial != 0)
            {
                RenderDebugViewMaterial(cullResults, hdCamera, renderContext);
                return;
            }

            ShadowOutput shadows;
            using (new Utilities.ProfilingSample("Shadow Pass", renderContext))
                {
                m_ShadowPass.Render(renderContext, cullResults, out shadows);
                }

            renderContext.SetupCameraProperties(camera); // Need to recall SetupCameraProperties after m_ShadowPass.Render

            using (new Utilities.ProfilingSample("Build Light list", renderContext))
                {
                m_lightLoop.PrepareLightsForGPU(m_Owner.shadowSettings, cullResults, camera, ref shadows);
                m_lightLoop.BuildGPULightLists(camera, renderContext, m_CameraDepthBufferRT); // TODO: Use async compute here to run light culling during shadow
            }

            PushGlobalParams(hdCamera, renderContext);

            // Caution: We require sun light here as some sky use the sun light to render, mean UpdateSkyEnvironment
            // must be call after BuildGPULightLists. 
            // TODO: Try to arrange code so we can trigger this call earlier and use async compute here to run sky convolution during other passes (once we move convolution shader to compute).
            UpdateSkyEnvironment(hdCamera, renderContext);

            RenderDeferredLighting(hdCamera, renderContext);

            // For opaque forward we have split rendering in two categories
            // Material that are always forward and material that can be deferred or forward depends on render pipeline options (like switch to rendering forward only mode)
            // Material that are always forward are unlit and complex (Like Hair) and don't require sorting, so it is ok to split them.
            RenderForward(cullResults, camera, renderContext, true); // Render deferred or forward opaque
            RenderForwardOnlyOpaque(cullResults, camera, renderContext);

            RenderSky(hdCamera, renderContext);

            // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
            RenderForward(cullResults, camera, renderContext, false);

            RenderVelocity(cullResults, camera, renderContext); // Note we may have to render velocity earlier if we do temporalAO, temporal volumetric etc... Mean we will not take into account forward opaque in case of deferred rendering ?

            // TODO: Check with VFX team.
            // Rendering distortion here have off course lot of artifact.
            // But resolving at each objects that write in distortion is not possible (need to sort transparent, render those that do not distort, then resolve, then etc...)
            // Instead we chose to apply distortion at the end after we cumulate distortion vector and desired blurriness. This
            RenderDistortion(cullResults, camera, renderContext);

            FinalPass(camera, renderContext);


            // bind depth surface for editor grid/gizmo/selection rendering
            if (camera.cameraType == CameraType.SceneView)
                    {
                var cmd = new CommandBuffer();
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, m_CameraDepthBufferRT);
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            renderContext.Submit();
        }

        void RenderOpaqueRenderList(CullResults cull, Camera camera, ScriptableRenderContext renderContext, string passName, RendererConfiguration rendererConfiguration = 0)
        {
            if (!debugParameters.displayOpaqueObjects)
                return;

            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName(passName))
            {
                rendererConfiguration = rendererConfiguration,
                sorting = { flags = SortFlags.CommonOpaque }
            };
            settings.inputFilter.SetQueuesOpaque();
            renderContext.DrawRenderers(settings);
        }

        void RenderTransparentRenderList(CullResults cull, Camera camera, ScriptableRenderContext renderContext, string passName, RendererConfiguration rendererConfiguration = 0)
        {
            if (!debugParameters.displayTransparentObjects)
                return;

            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName(passName))
            {
                rendererConfiguration = rendererConfiguration,
                sorting = { flags = SortFlags.CommonTransparent }
            };
            settings.inputFilter.SetQueuesTransparent();
            renderContext.DrawRenderers(settings);
        }

        void RenderDepthPrepass(CullResults cull, Camera camera, ScriptableRenderContext renderContext)
        {
            // If we are forward only we will do a depth prepass
            // TODO: Depth prepass should be enabled based on light loop settings. LightLoop define if they need a depth prepass + forward only...
            if (!debugParameters.useDepthPrepass)
                return;

            using (new Utilities.ProfilingSample("Depth Prepass", renderContext))
            {
                // TODO: Must do opaque then alpha masked for performance!
                // TODO: front to back for opaque and by materal for opaque tested when we split in two
                Utilities.SetRenderTarget(renderContext, m_CameraDepthBufferRT);
                RenderOpaqueRenderList(cull, camera, renderContext, "DepthOnly");
            }
        }

        void RenderGBuffer(CullResults cull, Camera camera, ScriptableRenderContext renderContext)
        {
            if (debugParameters.ShouldUseForwardRenderingOnly())
            {
                return ;
            }

            using (new Utilities.ProfilingSample("GBuffer Pass", renderContext))
            {
                // setup GBuffer for rendering
                Utilities.SetRenderTarget(renderContext, m_gbufferManager.GetGBuffers(), m_CameraDepthBufferRT);
                // render opaque objects into GBuffer
                RenderOpaqueRenderList(cull, camera, renderContext, "GBuffer", Utilities.kRendererConfigurationBakedLighting);
            }
        }

        // This pass is use in case of forward opaque and deferred rendering. We need to render forward objects before tile lighting pass
        void RenderForwardOnlyOpaqueDepthPrepass(CullResults cull, Camera camera, ScriptableRenderContext renderContext)
        {
            // If we are forward only we don't need to render ForwardOnlyOpaqueDepthOnly object
            // But in case we request a prepass we render it
            if (debugParameters.ShouldUseForwardRenderingOnly() && !debugParameters.useDepthPrepass)
                return;

            using (new Utilities.ProfilingSample("Forward opaque depth", renderContext))
            {
                Utilities.SetRenderTarget(renderContext, m_CameraDepthBufferRT);
                RenderOpaqueRenderList(cull, camera, renderContext, "ForwardOnlyOpaqueDepthOnly");
            }
        }

        void RenderDebugViewMaterial(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("DebugView Material Mode Pass", renderContext))
            // Render Opaque forward
            {
                Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthBufferRT, Utilities.kClearAll, Color.black);

                Shader.SetGlobalInt("_DebugViewMaterial", (int)debugParameters.debugViewMaterial);

                RenderOpaqueRenderList(cull, hdCamera.camera, renderContext, "DebugViewMaterial", Utilities.kRendererConfigurationBakedLighting);
            }

            // Render GBuffer opaque
            if (!debugParameters.ShouldUseForwardRenderingOnly())
            {
                Utilities.SetupMaterialHDCamera(hdCamera, m_DebugViewMaterialGBuffer);
                m_DebugViewMaterialGBuffer.SetFloat("_DebugViewMaterial", (float)debugParameters.debugViewMaterial);

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

        void RenderDeferredLighting(HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            if (debugParameters.ShouldUseForwardRenderingOnly())
            {
                return ;
            }

            m_lightLoop.RenderDeferredLighting(hdCamera, renderContext, m_CameraColorBuffer);
        }

        void UpdateSkyEnvironment(HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            m_SkyManager.UpdateEnvironment(hdCamera, m_lightLoop.GetCurrentSunLight(), renderContext);
        }

        void RenderSky(HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            m_SkyManager.RenderSky(hdCamera, m_lightLoop.GetCurrentSunLight(), m_CameraColorBufferRT, m_CameraDepthBufferRT, renderContext);
        }
        
        void RenderForward(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext, bool renderOpaque)
        {
            // TODO: Currently we can't render opaque object forward when deferred is enabled
            // miss option
            if (!debugParameters.ShouldUseForwardRenderingOnly() && renderOpaque)
                return;

            using (new Utilities.ProfilingSample("Forward Pass", renderContext))
            {
                Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthBufferRT);

                m_lightLoop.RenderForward(camera, renderContext, renderOpaque);

                if (renderOpaque)
                {
                    RenderOpaqueRenderList(cullResults, camera, renderContext, "Forward", Utilities.kRendererConfigurationBakedLighting);
                }
                else
                {
                    RenderTransparentRenderList(cullResults, camera, renderContext, "Forward", Utilities.kRendererConfigurationBakedLighting);
                }
            }
        }

        // Render material that are forward opaque only (like eye), this include unlit material
        void RenderForwardOnlyOpaque(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("Forward Only Pass", renderContext))
            {
                Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthBufferRT);

                m_lightLoop.RenderForward(camera, renderContext, true);
                RenderOpaqueRenderList(cullResults, camera, renderContext, "ForwardOnlyOpaque", Utilities.kRendererConfigurationBakedLighting);
            }
        }

        void RenderVelocity(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("Velocity Pass", renderContext))
            {
                // If opaque velocity have been render during GBuffer no need to render it here
                if ((ShaderConfig.s_VelocityInGbuffer == 1) || debugParameters.ShouldUseForwardRenderingOnly())
                    return ;

                int w = camera.pixelWidth;
                int h = camera.pixelHeight;

                var cmd = new CommandBuffer { name = "" };
                cmd.GetTemporaryRT(m_VelocityBuffer, w, h, 0, FilterMode.Point, Builtin.RenderLoop.GetVelocityBufferFormat(), Builtin.RenderLoop.GetVelocityBufferReadWrite());
                cmd.SetRenderTarget(m_VelocityBufferRT, m_CameraDepthBufferRT);
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                RenderOpaqueRenderList(cullResults, camera, renderContext, "MotionVectors");
            }
        }

        void RenderDistortion(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext)
        {
            if (!debugParameters.useDistortion)
                return ;

            using (new Utilities.ProfilingSample("Distortion Pass", renderContext))
            {
                int w = camera.pixelWidth;
                int h = camera.pixelHeight;

                var cmd = new CommandBuffer { name = "" };
                cmd.GetTemporaryRT(m_DistortionBuffer, w, h, 0, FilterMode.Point, Builtin.RenderLoop.GetDistortionBufferFormat(), Builtin.RenderLoop.GetDistortionBufferReadWrite());
                cmd.SetRenderTarget(m_DistortionBufferRT, m_CameraDepthBufferRT);
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
                /*var globalPostProcess = commonSettings == null
                    ? null
                    : commonSettings.GetComponent<PostProcessing>();*/

                bool localActive = localPostProcess != null && localPostProcess.enabled;
               // bool globalActive = globalPostProcess != null && globalPostProcess.enabled;

             //   if (!localActive && !globalActive)
                {
                    var cmd = new CommandBuffer { name = "" };
                    cmd.Blit(m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
                    renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Dispose();
                    return;
                }

                /*var target = localActive ? localPostProcess : globalPostProcess;
                target.Render(camera, renderContext, m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);*/
            }
        }


        // Function to prepare light structure for GPU lighting
        void PrepareLightsForGPU(ShadowSettings shadowSettings, CullResults cullResults, Camera camera, ref ShadowOutput shadowOutput)
        {
            // build per tile light lists
            m_lightLoop.PrepareLightsForGPU(shadowSettings, cullResults, camera, ref shadowOutput);
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

                    cmd.GetTemporaryRT(m_CameraColorBuffer, w, h, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, true);   // Enable UAV
                    cmd.GetTemporaryRT(m_CameraDepthBuffer, w, h, 24, FilterMode.Point, RenderTextureFormat.Depth);
                    if (!m_Owner.debugParameters.ShouldUseForwardRenderingOnly())
        {
                        m_gbufferManager.InitGBuffers(w, h, cmd);
            }
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

                    Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthBufferRT, ClearFlag.ClearDepth);
        }

                // TEMP: As we are in development and have not all the setup pass we still clear the color in emissive buffer and gbuffer, but this will be removed later.

                // Clear HDR target
                using (new Utilities.ProfilingSample("Clear HDR target", renderContext))
        {
                    Utilities.SetRenderTarget(renderContext, m_CameraColorBufferRT, m_CameraDepthBufferRT, ClearFlag.ClearColor, Color.black);
            }

                // Clear GBuffers
                if (!debugParameters.ShouldUseForwardRenderingOnly())
            {
                    using (new Utilities.ProfilingSample("Clear GBuffer", renderContext))
                {
                        Utilities.SetRenderTarget(renderContext, m_gbufferManager.GetGBuffers(), m_CameraDepthBufferRT, ClearFlag.ClearColor, Color.black);
                }
                    }
                // END TEMP
            }
        }
    }
}
