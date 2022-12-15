using System;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using static Unity.Mathematics.math;
//#define URP_HAS_BURST

// TODO SimpleLit material, make sure when variant is !defined(_SPECGLOSSMAP) && !defined(_SPECULAR_COLOR), specular is correctly silenced.
// TODO use InitializeSimpleLitSurfaceData() in all shader code
// TODO use InitializeParticleLitSurfaceData() in forward pass for ParticleLitForwardPass.hlsl ? Similar refactoring for ParticleSimpleLitForwardPass.hlsl
// TODO Make sure GPU buffers are uploaded without copying into Unity CommandBuffer memory
// TODO BakedLit.shader has a Universal2D pass, but Unlit.shader doesn't have?

namespace UnityEngine.Rendering.Universal.Internal
{
    // Customization per platform.
    static class DeferredConfig
    {
        internal static bool IsOpenGL { get; set; }

        // DX10 uses SM 4.0. However URP shaders requires SM 4.5 or will use fallback to SM 2.0 shaders otherwise.
        // We will consider deferred renderer is not available when SM 2.0 shaders run.
        internal static bool IsDX10 { get; set; }
    }

    internal enum LightFlag
    {
        // Keep in sync with kLightFlagSubtractiveMixedLighting.
        SubtractiveMixedLighting = 4
    }

    // Manages deferred lights.
    internal class DeferredLights
    {
        internal static class ShaderConstants
        {
            public static readonly int _LitStencilRef = Shader.PropertyToID("_LitStencilRef");
            public static readonly int _LitStencilReadMask = Shader.PropertyToID("_LitStencilReadMask");
            public static readonly int _LitStencilWriteMask = Shader.PropertyToID("_LitStencilWriteMask");
            public static readonly int _SimpleLitStencilRef = Shader.PropertyToID("_SimpleLitStencilRef");
            public static readonly int _SimpleLitStencilReadMask = Shader.PropertyToID("_SimpleLitStencilReadMask");
            public static readonly int _SimpleLitStencilWriteMask = Shader.PropertyToID("_SimpleLitStencilWriteMask");
            public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
            public static readonly int _StencilReadMask = Shader.PropertyToID("_StencilReadMask");
            public static readonly int _StencilWriteMask = Shader.PropertyToID("_StencilWriteMask");
            public static readonly int _LitPunctualStencilRef = Shader.PropertyToID("_LitPunctualStencilRef");
            public static readonly int _LitPunctualStencilReadMask = Shader.PropertyToID("_LitPunctualStencilReadMask");
            public static readonly int _LitPunctualStencilWriteMask = Shader.PropertyToID("_LitPunctualStencilWriteMask");
            public static readonly int _SimpleLitPunctualStencilRef = Shader.PropertyToID("_SimpleLitPunctualStencilRef");
            public static readonly int _SimpleLitPunctualStencilReadMask = Shader.PropertyToID("_SimpleLitPunctualStencilReadMask");
            public static readonly int _SimpleLitPunctualStencilWriteMask = Shader.PropertyToID("_SimpleLitPunctualStencilWriteMask");
            public static readonly int _LitDirStencilRef = Shader.PropertyToID("_LitDirStencilRef");
            public static readonly int _LitDirStencilReadMask = Shader.PropertyToID("_LitDirStencilReadMask");
            public static readonly int _LitDirStencilWriteMask = Shader.PropertyToID("_LitDirStencilWriteMask");
            public static readonly int _SimpleLitDirStencilRef = Shader.PropertyToID("_SimpleLitDirStencilRef");
            public static readonly int _SimpleLitDirStencilReadMask = Shader.PropertyToID("_SimpleLitDirStencilReadMask");
            public static readonly int _SimpleLitDirStencilWriteMask = Shader.PropertyToID("_SimpleLitDirStencilWriteMask");
            public static readonly int _ClearStencilRef = Shader.PropertyToID("_ClearStencilRef");
            public static readonly int _ClearStencilReadMask = Shader.PropertyToID("_ClearStencilReadMask");
            public static readonly int _ClearStencilWriteMask = Shader.PropertyToID("_ClearStencilWriteMask");

            public static readonly int _ScreenToWorld = Shader.PropertyToID("_ScreenToWorld");

            public static int _MainLightPosition = Shader.PropertyToID("_MainLightPosition");   // ForwardLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightColor = Shader.PropertyToID("_MainLightColor");         // ForwardLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightLayerMask = Shader.PropertyToID("_MainLightLayerMask"); // ForwardLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _SpotLightScale = Shader.PropertyToID("_SpotLightScale");
            public static int _SpotLightBias = Shader.PropertyToID("_SpotLightBias");
            public static int _SpotLightGuard = Shader.PropertyToID("_SpotLightGuard");
            public static int _LightPosWS = Shader.PropertyToID("_LightPosWS");
            public static int _LightColor = Shader.PropertyToID("_LightColor");
            public static int _LightAttenuation = Shader.PropertyToID("_LightAttenuation");
            public static int _LightOcclusionProbInfo = Shader.PropertyToID("_LightOcclusionProbInfo");
            public static int _LightDirection = Shader.PropertyToID("_LightDirection");
            public static int _LightFlags = Shader.PropertyToID("_LightFlags");
            public static int _ShadowLightIndex = Shader.PropertyToID("_ShadowLightIndex");
            public static int _LightLayerMask = Shader.PropertyToID("_LightLayerMask");
            public static int _CookieLightIndex = Shader.PropertyToID("_CookieLightIndex");
        }

        internal static readonly string[] k_GBufferNames = new string[]
        {
            "_GBuffer0",
            "_GBuffer1",
            "_GBuffer2",
            "_GBuffer3",
            "_GBuffer4",
            "_GBuffer5",
            "_GBuffer6"
        };

        static readonly string[] k_StencilDeferredPassNames = new string[]
        {
            "Stencil Volume",
            "Deferred Punctual Light (Lit)",
            "Deferred Punctual Light (SimpleLit)",
            "Deferred Directional Light (Lit)",
            "Deferred Directional Light (SimpleLit)",
            "ClearStencilPartial",
            "Fog",
            "SSAOOnly"
        };

        internal enum StencilDeferredPasses
        {
            StencilVolume,
            PunctualLit,
            PunctualSimpleLit,
            DirectionalLit,
            DirectionalSimpleLit,
            ClearStencilPartial,
            Fog,
            SSAOOnly
        };

        static readonly ushort k_InvalidLightOffset = 0xFFFF;
        static readonly string k_SetupLights = "SetupLights";
        static readonly string k_DeferredPass = "Deferred Pass";
        static readonly string k_DeferredStencilPass = "Deferred Shading (Stencil)";
        static readonly string k_DeferredFogPass = "Deferred Fog";
        static readonly string k_ClearStencilPartial = "Clear Stencil Partial";
        static readonly string k_SetupLightConstants = "Setup Light Constants";
        static readonly float kStencilShapeGuard = 1.06067f; // stencil geometric shapes must be inflated to fit the analytic shapes.
        private static readonly ProfilingSampler m_ProfilingSetupLights = new ProfilingSampler(k_SetupLights);
        private static readonly ProfilingSampler m_ProfilingDeferredPass = new ProfilingSampler(k_DeferredPass);
        private static readonly ProfilingSampler m_ProfilingSetupLightConstants = new ProfilingSampler(k_SetupLightConstants);

        internal int GBufferAlbedoIndex { get { return 0; } }
        internal int GBufferSpecularMetallicIndex { get { return 1; } }
        internal int GBufferNormalSmoothnessIndex { get { return 2; } }
        internal int GBufferLightingIndex { get { return 3; } }
        internal int GbufferDepthIndex { get { return UseRenderPass ? GBufferLightingIndex + 1 : -1; } }
        internal int GBufferShadowMask { get { return UseShadowMask ? GBufferLightingIndex + (UseRenderPass ? 1 : 0) + 1 : -1; } }
        internal int GBufferRenderingLayers { get { return UseRenderingLayers ? GBufferLightingIndex + (UseRenderPass ? 1 : 0) + (UseShadowMask ? 1 : 0) + 1 : -1; } }
        // Color buffer count (not including dephStencil).
        internal int GBufferSliceCount { get { return 4 + (UseRenderPass ? 1 : 0) + (UseShadowMask ? 1 : 0) + (UseRenderingLayers ? 1 : 0); } }

        internal GraphicsFormat GetGBufferFormat(int index)
        {
            if (index == GBufferAlbedoIndex) // sRGB albedo, materialFlags
                return QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            else if (index == GBufferSpecularMetallicIndex) // sRGB specular, [unused]
                return GraphicsFormat.R8G8B8A8_UNorm;
            else if (index == GBufferNormalSmoothnessIndex)
                return this.AccurateGbufferNormals ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8B8A8_SNorm; // normal normal normal packedSmoothness
            else if (index == GBufferLightingIndex) // Emissive+baked: Most likely B10G11R11_UFloatPack32 or R16G16B16A16_SFloat
                return GraphicsFormat.None;
            else if (index == GbufferDepthIndex) // Render-pass on mobiles: reading back real depth-buffer is either inefficient (Arm Vulkan) or impossible (Metal).
                return GraphicsFormat.R32_SFloat;
            else if (index == GBufferShadowMask) // Optional: shadow mask is outputed in mixed lighting subtractive mode for non-static meshes only
                return GraphicsFormat.R8G8B8A8_UNorm;
            else if (index == GBufferRenderingLayers) // Optional: rendering layers is outputed when light layers are enabled (subset of rendering layers)
                return RenderingLayerUtils.GetFormat(RenderingLayerMaskSize);
            else
                return GraphicsFormat.None;
        }

        // This may return different values depending on what lights are rendered for a given frame.
        internal bool UseShadowMask { get { return this.MixedLightingSetup != MixedLightingSetup.None; } }
        //
        internal bool UseRenderingLayers { get { return UseLightLayers || UseDecalLayers; } }
        //
        internal RenderingLayerUtils.MaskSize RenderingLayerMaskSize { get; set; }
        //
        internal bool UseDecalLayers { get; set; }
        //
        internal bool UseLightLayers { get { return UniversalRenderPipeline.asset.useRenderingLayers; } }
        //
        internal bool UseRenderPass { get; set; }
        //
        internal bool HasDepthPrepass { get; set; }
        //
        internal bool HasNormalPrepass { get; set; }

        // This is an overlay camera being rendered.
        internal bool IsOverlay { get; set; }
        // Not all platforms support R8G8B8A8_SNorm, so we need to check for the support and force accurate GBuffer normals and relevant shader variants
        private bool m_AccurateGbufferNormals;
        internal bool AccurateGbufferNormals
        {
            get { return m_AccurateGbufferNormals; }
            set { m_AccurateGbufferNormals = value || !RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8G8B8A8_SNorm, FormatUsage.Render); }
        }
        // We browse all visible lights and found the mixed lighting setup every frame.
        internal MixedLightingSetup MixedLightingSetup { get; set; }
        //
        internal bool UseJobSystem { get; set; }
        //
        internal int RenderWidth { get; set; }
        //
        internal int RenderHeight { get; set; }

        // Output lighting result.
        internal RTHandle[] GbufferAttachments { get; set; }
        private RTHandle[] GbufferRTHandles;
        internal TextureHandle[] GbufferTextureHandles { get; set; }
        internal RTHandle[] DeferredInputAttachments { get; set; }
        internal bool[] DeferredInputIsTransient { get; set; }
        // Input depth texture, also bound as read-only RT
        internal RTHandle DepthAttachment { get; set; }
        //
        internal RTHandle DepthCopyTexture { get; set; }

        internal GraphicsFormat[] GbufferFormats { get; set; }
        internal RTHandle DepthAttachmentHandle { get; set; }

        // Visible lights indices rendered using stencil volumes.
        NativeArray<ushort> m_stencilVisLights;
        // Offset of each type of lights in m_stencilVisLights.
        NativeArray<ushort> m_stencilVisLightOffsets;
        // Needed to access light shadow index (can be null if the pass is not queued).
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;

        // For rendering stencil point lights.
        Mesh m_SphereMesh;
        // For rendering stencil spot lights.
        Mesh m_HemisphereMesh;
        // For rendering directional lights.
        Mesh m_FullscreenMesh;

        // Hold all shaders for stencil-volume deferred shading.
        Material m_StencilDeferredMaterial;

        // Pass indices.
        int[] m_StencilDeferredPasses;

        // Avoid memory allocations.
        Matrix4x4[] m_ScreenToWorld = new Matrix4x4[2];

        ProfilingSampler m_ProfilingSamplerDeferredStencilPass = new ProfilingSampler(k_DeferredStencilPass);
        ProfilingSampler m_ProfilingSamplerDeferredFogPass = new ProfilingSampler(k_DeferredFogPass);
        ProfilingSampler m_ProfilingSamplerClearStencilPartialPass = new ProfilingSampler(k_ClearStencilPartial);

        private LightCookieManager m_LightCookieManager;

        internal struct InitParams
        {
            public Material stencilDeferredMaterial;

            public LightCookieManager lightCookieManager;
        }

        internal DeferredLights(InitParams initParams, bool useNativeRenderPass = false)
        {
            // Cache result for GL platform here. SystemInfo properties are in C++ land so repeated access will be unecessary penalized.
            // They can also only be called from main thread!
            DeferredConfig.IsOpenGL = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;

            // Cachre result for DX10 platform too. Same reasons as above.
            DeferredConfig.IsDX10 = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 && SystemInfo.graphicsShaderLevel <= 40;

            m_StencilDeferredMaterial = initParams.stencilDeferredMaterial;

            m_StencilDeferredPasses = new int[k_StencilDeferredPassNames.Length];
            InitStencilDeferredMaterial();

            this.AccurateGbufferNormals = true;
            this.UseJobSystem = true;
            if (SystemInfo.graphicsDeviceName.Contains("Apple M"))
            {
                // Currently Apple silicon machines do not support native render passes
                useNativeRenderPass = false;
            }
            this.UseRenderPass = useNativeRenderPass;
            m_LightCookieManager = initParams.lightCookieManager;
        }

        internal void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Profiler.BeginSample(k_SetupLights);

            Camera camera = renderingData.cameraData.camera;
            // Support for dynamic resolution.
            this.RenderWidth = camera.allowDynamicResolution ? Mathf.CeilToInt(ScalableBufferManager.widthScaleFactor * renderingData.cameraData.cameraTargetDescriptor.width) : renderingData.cameraData.cameraTargetDescriptor.width;
            this.RenderHeight = camera.allowDynamicResolution ? Mathf.CeilToInt(ScalableBufferManager.heightScaleFactor * renderingData.cameraData.cameraTargetDescriptor.height) : renderingData.cameraData.cameraTargetDescriptor.height;

            // inspect lights in renderingData.lightData.visibleLights and convert them to entries in m_stencilVisLights
            PrecomputeLights(
                out m_stencilVisLights,
                out m_stencilVisLightOffsets,
                ref renderingData.lightData.visibleLights,
                renderingData.lightData.additionalLightsCount != 0 || renderingData.lightData.mainLightIndex >= 0,
                renderingData.cameraData.camera.worldToCameraMatrix,
                renderingData.cameraData.camera.orthographic,
                renderingData.cameraData.camera.nearClipPlane
            );

            {
                var cmd = renderingData.commandBuffer;
                using (new ProfilingScope(cmd, m_ProfilingSetupLightConstants))
                {
                    // Shared uniform constants for all lights.
                    SetupShaderLightConstants(cmd, ref renderingData);

#if UNITY_EDITOR
                    // This flag is used to strip mixed lighting shader variants when a player is built.
                    // All shader variants are available in the editor.
                    bool supportsMixedLighting = true;
#else
                    bool supportsMixedLighting = renderingData.lightData.supportsMixedLighting;
#endif

                    // Setup global keywords.
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._GBUFFER_NORMALS_OCT, this.AccurateGbufferNormals);
                    bool isShadowMask = supportsMixedLighting && this.MixedLightingSetup == MixedLightingSetup.ShadowMask;
                    bool isShadowMaskAlways = isShadowMask && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
                    bool isSubtractive = supportsMixedLighting && this.MixedLightingSetup == MixedLightingSetup.Subtractive;
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightmapShadowMixing, isSubtractive || isShadowMaskAlways);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ShadowsShadowMask, isShadowMask);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive, isSubtractive); // Backward compatibility
                    // This should be moved to a more global scope when framebuffer fetch is introduced to more passes
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.RenderPassEnabled, this.UseRenderPass && renderingData.cameraData.cameraType == CameraType.Game);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightLayers, UseLightLayers);

                    RenderingLayerUtils.SetupProperties(cmd, RenderingLayerMaskSize);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            Profiler.EndSample();
        }

        internal void ResolveMixedLightingMode(ref RenderingData renderingData)
        {
            // Find the mixed lighting mode. This is the same logic as ForwardLights.
            this.MixedLightingSetup = MixedLightingSetup.None;

#if !UNITY_EDITOR
            // This flag is used to strip mixed lighting shader variants when a player is built.
            // All shader variants are available in the editor.
            if (renderingData.lightData.supportsMixedLighting)
#endif
            {
                NativeArray<VisibleLight> visibleLights = renderingData.lightData.visibleLights;
                for (int lightIndex = 0; lightIndex < renderingData.lightData.visibleLights.Length && this.MixedLightingSetup == MixedLightingSetup.None; ++lightIndex)
                {
                    Light light = visibleLights.UnsafeElementAtMutable(lightIndex).light;

                    if (light != null
                        && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed
                        && light.shadows != LightShadows.None)
                    {
                        switch (light.bakingOutput.mixedLightingMode)
                        {
                            case MixedLightingMode.Subtractive:
                                this.MixedLightingSetup = MixedLightingSetup.Subtractive;
                                break;
                            case MixedLightingMode.Shadowmask:
                                this.MixedLightingSetup = MixedLightingSetup.ShadowMask;
                                break;
                        }
                    }
                }
            }
            // Once the mixed lighting mode has been discovered, we know how many MRTs we need for the gbuffer.
            // Subtractive mixed lighting requires shadowMask output, which is actually used to store unity_ProbesOcclusion values.

            CreateGbufferResources();
        }

        // In cases when custom pass is injected between GBuffer and Deferred passes we need to fallback
        // To non-renderpass path in the middle of setup, which means recreating the gbuffer attachments as well due to GBuffer4 used for RenderPass
        internal void DisableFramebufferFetchInput()
        {
            this.UseRenderPass = false;
            CreateGbufferResources();
        }

        internal void ReleaseGbufferResources()
        {
            if (this.GbufferRTHandles != null)
            {
                // Release the old handles before creating the new one
                for (int i = 0; i < this.GbufferRTHandles.Length; ++i)
                {
                    RTHandles.Release(this.GbufferRTHandles[i]);
                }
            }
        }

        internal void CreateGbufferResources()
        {
            int gbufferSliceCount = this.GBufferSliceCount;
            if (this.GbufferRTHandles == null || this.GbufferRTHandles.Length != gbufferSliceCount)
            {
                ReleaseGbufferResources();

                this.GbufferAttachments = new RTHandle[gbufferSliceCount];
                this.GbufferRTHandles = new RTHandle[gbufferSliceCount];
                this.GbufferFormats = new GraphicsFormat[gbufferSliceCount];
                this.GbufferTextureHandles = new TextureHandle[gbufferSliceCount];
                for (int i = 0; i < gbufferSliceCount; ++i)
                {
                    this.GbufferRTHandles[i] = RTHandles.Alloc(k_GBufferNames[i], name: k_GBufferNames[i]);
                    this.GbufferAttachments[i] = this.GbufferRTHandles[i];
                    this.GbufferFormats[i] = this.GetGBufferFormat(i);
                }
            }
        }

        internal void UpdateDeferredInputAttachments()
        {
            this.DeferredInputAttachments[0] = this.GbufferAttachments[0];
            this.DeferredInputAttachments[1] = this.GbufferAttachments[1];
            this.DeferredInputAttachments[2] = this.GbufferAttachments[2];
            this.DeferredInputAttachments[3] = this.GbufferAttachments[4];
        }

        internal bool IsRuntimeSupportedThisFrame()
        {
            // GBuffer slice count can change depending actual geometry/light being rendered.
            // For instance, we only bind shadowMask RT if the scene supports mix lighting and at least one visible light has subtractive mixed ligting mode.
            return this.GBufferSliceCount <= SystemInfo.supportedRenderTargetCount && !DeferredConfig.IsOpenGL && !DeferredConfig.IsDX10;
        }

        public void Setup(ref RenderingData renderingData,
            AdditionalLightsShadowCasterPass additionalLightsShadowCasterPass,
            bool hasDepthPrepass,
            bool hasNormalPrepass,
            RTHandle depthCopyTexture,
            RTHandle depthAttachment,
            RTHandle colorAttachment)
        {
            m_AdditionalLightsShadowCasterPass = additionalLightsShadowCasterPass;
            this.HasDepthPrepass = hasDepthPrepass;
            this.HasNormalPrepass = hasNormalPrepass;

            this.DepthCopyTexture = depthCopyTexture;

            this.GbufferAttachments[this.GBufferLightingIndex] = colorAttachment;
            this.DepthAttachment = depthAttachment;

            if (this.DeferredInputAttachments == null && this.UseRenderPass && this.GbufferAttachments.Length >= 3)
            {
                this.DeferredInputAttachments = new RTHandle[4]
                {
                    GbufferAttachments[0], GbufferAttachments[1], GbufferAttachments[2], GbufferAttachments[4],
                };

                this.DeferredInputIsTransient = new bool[4]
                {
                    true, true, true, false
                };
            }
            this.DepthAttachmentHandle = this.DepthAttachment;
        }

        // Only used by RenderGraph now as the other Setup call requires providing target handles which isn't working on RG
        internal void Setup(AdditionalLightsShadowCasterPass additionalLightsShadowCasterPass)
        {
            m_AdditionalLightsShadowCasterPass = additionalLightsShadowCasterPass;
        }

        public void OnCameraCleanup(CommandBuffer cmd)
        {
            // Disable any global keywords setup in SetupLights().
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._GBUFFER_NORMALS_OCT, false);

            if (m_stencilVisLights.IsCreated)
                m_stencilVisLights.Dispose();
            if (m_stencilVisLightOffsets.IsCreated)
                m_stencilVisLightOffsets.Dispose();
        }

        internal static StencilState OverwriteStencil(StencilState s, int stencilWriteMask)
        {
            if (!s.enabled)
            {
                return new StencilState(
                    true,
                    0, (byte)stencilWriteMask,
                    CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep,
                    CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep
                );
            }

            CompareFunction funcFront = s.compareFunctionFront != CompareFunction.Disabled ? s.compareFunctionFront : CompareFunction.Always;
            CompareFunction funcBack = s.compareFunctionBack != CompareFunction.Disabled ? s.compareFunctionBack : CompareFunction.Always;
            StencilOp passFront = s.passOperationFront;
            StencilOp failFront = s.failOperationFront;
            StencilOp zfailFront = s.zFailOperationFront;
            StencilOp passBack = s.passOperationBack;
            StencilOp failBack = s.failOperationBack;
            StencilOp zfailBack = s.zFailOperationBack;

            return new StencilState(
                true,
                (byte)(s.readMask & 0x0F), (byte)(s.writeMask | stencilWriteMask),
                funcFront, passFront, failFront, zfailFront,
                funcBack, passBack, failBack, zfailBack
            );
        }

        internal static RenderStateBlock OverwriteStencil(RenderStateBlock block, int stencilWriteMask, int stencilRef)
        {
            if (!block.stencilState.enabled)
            {
                block.stencilState = new StencilState(
                    true,
                    0, (byte)stencilWriteMask,
                    CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep,
                    CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep
                );
            }
            else
            {
                StencilState s = block.stencilState;
                CompareFunction funcFront = s.compareFunctionFront != CompareFunction.Disabled ? s.compareFunctionFront : CompareFunction.Always;
                CompareFunction funcBack = s.compareFunctionBack != CompareFunction.Disabled ? s.compareFunctionBack : CompareFunction.Always;
                StencilOp passFront = s.passOperationFront;
                StencilOp failFront = s.failOperationFront;
                StencilOp zfailFront = s.zFailOperationFront;
                StencilOp passBack = s.passOperationBack;
                StencilOp failBack = s.failOperationBack;
                StencilOp zfailBack = s.zFailOperationBack;

                block.stencilState = new StencilState(
                    true,
                    (byte)(s.readMask & 0x0F), (byte)(s.writeMask | stencilWriteMask),
                    funcFront, passFront, failFront, zfailFront,
                    funcBack, passBack, failBack, zfailBack
                );
            }

            block.mask |= RenderStateMask.Stencil;
            block.stencilReference = (block.stencilReference & (int)StencilUsage.UserMask) | stencilRef;

            return block;
        }

        internal void ClearStencilPartial(CommandBuffer cmd)
        {
            if (m_FullscreenMesh == null)
                m_FullscreenMesh = CreateFullscreenMesh();

            using (new ProfilingScope(cmd, m_ProfilingSamplerClearStencilPartialPass))
            {
                cmd.DrawMesh(m_FullscreenMesh, Matrix4x4.identity, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.ClearStencilPartial]);
            }
        }

        internal void ExecuteDeferredPass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Workaround for bug.
            // When changing the URP asset settings (ex: shadow cascade resolution), all ScriptableRenderers are recreated but
            // materials passed in have not finished initializing at that point if they have fallback shader defined. In particular deferred shaders only have 1 pass available,
            // which prevents from resolving correct pass indices.
            if (m_StencilDeferredPasses[0] < 0)
                InitStencilDeferredMaterial();

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ProfilingDeferredPass))
            {
                // This does 2 things:
                // - baked geometry are skipped (do not receive dynamic lighting)
                // - non-baked geometry (== non-static geometry) use shadowMask/occlusionProbes to emulate baked shadows influences.
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._DEFERRED_MIXED_LIGHTING, this.UseShadowMask);

                // This must be set for each eye in XR mode multipass.
                SetupMatrixConstants(cmd, ref renderingData);

                // Firt directional light will apply SSAO if possible, unless there is none.
                if (!HasStencilLightsOfType(LightType.Directional))
                    RenderSSAOBeforeShading(cmd, ref renderingData);

                RenderStencilLights(context, cmd, ref renderingData);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._DEFERRED_MIXED_LIGHTING, false);

                // Legacy fog (Windows -> Rendering -> Lighting Settings -> Fog)
                RenderFog(context, cmd, ref renderingData);
            }

            // Restore shader keywords
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, renderingData.shadowData.isKeywordAdditionalLightShadowsEnabled);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, renderingData.shadowData.isKeywordSoftShadowsEnabled);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightCookies, m_LightCookieManager != null && m_LightCookieManager.IsKeywordLightCookieEnabled);
        }

        // adapted from ForwardLights.SetupShaderLightConstants
        void SetupShaderLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Universal Forward pipeline only supports a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref renderingData.lightData);
        }

        // adapted from ForwardLights.SetupShaderLightConstants
        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            if (lightData.mainLightIndex < 0)
                return;

            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
            UniversalRenderPipeline.InitializeLightConstants_Common(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

            var additionalLightData = lightData.visibleLights[lightData.mainLightIndex].light.GetUniversalAdditionalLightData();
            uint lightLayerMask = RenderingLayerUtils.ToValidRenderingLayers(additionalLightData.renderingLayers);

            cmd.SetGlobalVector(ShaderConstants._MainLightPosition, lightPos);
            cmd.SetGlobalVector(ShaderConstants._MainLightColor, lightColor);
            cmd.SetGlobalInt(ShaderConstants._MainLightLayerMask, (int)lightLayerMask);
        }

        void SetupMatrixConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;

#if ENABLE_VR && ENABLE_XR_MODULE
            int eyeCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
#else
            int eyeCount = 1;
#endif
            Matrix4x4[] screenToWorld = m_ScreenToWorld; // deferred shaders expects 2 elements

            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 proj = cameraData.GetProjectionMatrix(eyeIndex);
                Matrix4x4 view = cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(proj, false);

                // xy coordinates in range [-1; 1] go to pixel coordinates.
                Matrix4x4 toScreen = new Matrix4x4(
                    new Vector4(0.5f * this.RenderWidth, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, 0.5f * this.RenderHeight, 0.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                    new Vector4(0.5f * this.RenderWidth, 0.5f * this.RenderHeight, 0.0f, 1.0f)
                );

                Matrix4x4 zScaleBias = Matrix4x4.identity;
                if (DeferredConfig.IsOpenGL)
                {
                    // We need to manunally adjust z in NDC space from [-1; 1] to [0; 1] (storage in depth texture).
                    zScaleBias = new Matrix4x4(
                        new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                        new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
                        new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                        new Vector4(0.0f, 0.0f, 0.5f, 1.0f)
                    );
                }

                screenToWorld[eyeIndex] = Matrix4x4.Inverse(toScreen * zScaleBias * gpuProj * view);
            }

            cmd.SetGlobalMatrixArray(ShaderConstants._ScreenToWorld, screenToWorld);
        }

        void PrecomputeLights(
            out NativeArray<ushort> stencilVisLights,
            out NativeArray<ushort> stencilVisLightOffsets,
            ref NativeArray<VisibleLight> visibleLights,
            bool hasAdditionalLights,
            Matrix4x4 view,
            bool isOrthographic,
            float zNear)
        {
            const int lightTypeCount = (int)LightType.Disc + 1;

            if (!hasAdditionalLights)
            {
                stencilVisLights = new NativeArray<ushort>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                stencilVisLightOffsets = new NativeArray<ushort>(lightTypeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < lightTypeCount; ++i)
                    stencilVisLightOffsets[i] = k_InvalidLightOffset;
                return;
            }

            NativeArray<int> stencilLightCounts = new NativeArray<int>(lightTypeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
            stencilVisLightOffsets = new NativeArray<ushort>(lightTypeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);

            // Count the number of lights per type.
            int visibleLightCount = visibleLights.Length;
            for (ushort visLightIndex = 0; visLightIndex < visibleLightCount; ++visLightIndex)
            {
                ref VisibleLight vl = ref visibleLights.UnsafeElementAtMutable(visLightIndex);
                ++stencilVisLightOffsets[(int)vl.lightType];
            }

            int totalStencilLightCount = stencilVisLightOffsets[(int)LightType.Spot] + stencilVisLightOffsets[(int)LightType.Directional] + stencilVisLightOffsets[(int)LightType.Point];
            stencilVisLights = new NativeArray<ushort>(totalStencilLightCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0, soffset = 0; i < stencilVisLightOffsets.Length; ++i)
            {
                if (stencilVisLightOffsets[i] == 0)
                    stencilVisLightOffsets[i] = k_InvalidLightOffset;
                else
                {
                    int c = stencilVisLightOffsets[i];
                    stencilVisLightOffsets[i] = (ushort)soffset;
                    soffset += c;
                }
            }

            for (ushort visLightIndex = 0; visLightIndex < visibleLightCount; ++visLightIndex)
            {
                ref VisibleLight vl = ref visibleLights.UnsafeElementAtMutable(visLightIndex);
                int i = stencilLightCounts[(int)vl.lightType]++;
                stencilVisLights[stencilVisLightOffsets[(int)vl.lightType] + i] = visLightIndex;
            }
            stencilLightCounts.Dispose();
        }

        bool HasStencilLightsOfType(LightType type)
        {
            return m_stencilVisLightOffsets[(int)type] != k_InvalidLightOffset;
        }

        void RenderStencilLights(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (m_stencilVisLights.Length == 0)
                return;

            if (m_StencilDeferredMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_StencilDeferredMaterial, GetType().Name);
                return;
            }

            Profiler.BeginSample(k_DeferredStencilPass);

            using (new ProfilingScope(cmd, m_ProfilingSamplerDeferredStencilPass))
            {
                NativeArray<VisibleLight> visibleLights = renderingData.lightData.visibleLights;

                if (HasStencilLightsOfType(LightType.Directional))
                    RenderStencilDirectionalLights(cmd, ref renderingData, visibleLights, renderingData.lightData.mainLightIndex);
                if (HasStencilLightsOfType(LightType.Point))
                    RenderStencilPointLights(cmd, ref renderingData, visibleLights);
                if (HasStencilLightsOfType(LightType.Spot))
                    RenderStencilSpotLights(cmd, ref renderingData, visibleLights);
            }

            Profiler.EndSample();
        }

        void RenderStencilDirectionalLights(CommandBuffer cmd, ref RenderingData renderingData, NativeArray<VisibleLight> visibleLights, int mainLightIndex)
        {
            if (m_FullscreenMesh == null)
                m_FullscreenMesh = CreateFullscreenMesh();

            cmd.EnableShaderKeyword(ShaderKeywordStrings._DIRECTIONAL);

            // Directional lights.
            bool isFirstLight = true;

            // TODO bundle extra directional lights rendering by batches of 8.
            // Also separate shadow caster lights from non-shadow caster.
            for (int soffset = m_stencilVisLightOffsets[(int)LightType.Directional]; soffset < m_stencilVisLights.Length; ++soffset)
            {
                ushort visLightIndex = m_stencilVisLights[soffset];
                ref VisibleLight vl = ref visibleLights.UnsafeElementAtMutable(visLightIndex);
                if (vl.lightType != LightType.Directional)
                    break;

                // Avoid light find on every access.
                Light light = vl.light;

                Vector4 lightDir, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
                UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, visLightIndex, out lightDir, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

                int lightFlags = 0;
                if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
                    lightFlags |= (int)LightFlag.SubtractiveMixedLighting;

                var additionalLightData = light.GetUniversalAdditionalLightData();
                uint lightLayerMask = RenderingLayerUtils.ToValidRenderingLayers(additionalLightData.renderingLayers);

                // Setup shadow paramters:
                // - for the main light, they have already been setup globally, so nothing to do.
                // - for other directional lights, it is actually not supported by URP, but the code would look like this.
                bool hasDeferredShadows;
                if (visLightIndex == mainLightIndex)
                {
                    hasDeferredShadows = light && light.shadows != LightShadows.None;
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, false);
                }
                else
                {
                    int shadowLightIndex = m_AdditionalLightsShadowCasterPass != null ? m_AdditionalLightsShadowCasterPass.GetShadowLightIndexFromLightIndex(visLightIndex) : -1;
                    hasDeferredShadows = light && light.shadows != LightShadows.None && shadowLightIndex >= 0;
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, hasDeferredShadows);

                    cmd.SetGlobalInt(ShaderConstants._ShadowLightIndex, shadowLightIndex);
                }

                bool hasSoftShadow = hasDeferredShadows && renderingData.shadowData.supportsSoftShadows && light.shadows == LightShadows.Soft;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, hasSoftShadow);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._DEFERRED_FIRST_LIGHT, isFirstLight); // First directional light applies SSAO
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._DEFERRED_MAIN_LIGHT, visLightIndex == mainLightIndex); // main directional light use different uniform constants from additional directional lights

                cmd.SetGlobalVector(ShaderConstants._LightColor, lightColor); // VisibleLight.finalColor already returns color in active color space
                cmd.SetGlobalVector(ShaderConstants._LightDirection, lightDir);
                cmd.SetGlobalInt(ShaderConstants._LightFlags, lightFlags);
                cmd.SetGlobalInt(ShaderConstants._LightLayerMask, (int)lightLayerMask);

                // Lighting pass.
                cmd.DrawMesh(m_FullscreenMesh, Matrix4x4.identity, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.DirectionalLit]);
                cmd.DrawMesh(m_FullscreenMesh, Matrix4x4.identity, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.DirectionalSimpleLit]);

                isFirstLight = false;
            }

            cmd.DisableShaderKeyword(ShaderKeywordStrings._DIRECTIONAL);
        }

        void RenderStencilPointLights(CommandBuffer cmd, ref RenderingData renderingData, NativeArray<VisibleLight> visibleLights)
        {
            if (m_SphereMesh == null)
                m_SphereMesh = CreateSphereMesh();

            cmd.EnableShaderKeyword(ShaderKeywordStrings._POINT);

            for (int soffset = m_stencilVisLightOffsets[(int)LightType.Point]; soffset < m_stencilVisLights.Length; ++soffset)
            {
                ushort visLightIndex = m_stencilVisLights[soffset];
                ref VisibleLight vl = ref visibleLights.UnsafeElementAtMutable(visLightIndex);
                if (vl.lightType != LightType.Point)
                    break;

                // Avoid light find on every access.
                Light light = vl.light;

                Vector3 posWS = vl.localToWorldMatrix.GetColumn(3);
                Matrix4x4 transformMatrix = new Matrix4x4(
                    new Vector4(vl.range, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, vl.range, 0.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, vl.range, 0.0f),
                    new Vector4(posWS.x, posWS.y, posWS.z, 1.0f)
                );

                Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
                UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, visLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

                var additionalLightData = light.GetUniversalAdditionalLightData();
                uint lightLayerMask = RenderingLayerUtils.ToValidRenderingLayers(additionalLightData.renderingLayers);

                int lightFlags = 0;
                if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
                    lightFlags |= (int)LightFlag.SubtractiveMixedLighting;

                int shadowLightIndex = m_AdditionalLightsShadowCasterPass != null ? m_AdditionalLightsShadowCasterPass.GetShadowLightIndexFromLightIndex(visLightIndex) : -1;
                bool hasDeferredLightShadows = light && light.shadows != LightShadows.None && shadowLightIndex >= 0;
                bool hasSoftShadow = hasDeferredLightShadows && renderingData.shadowData.supportsSoftShadows && light.shadows == LightShadows.Soft;

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, hasDeferredLightShadows);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, hasSoftShadow);

                if (m_LightCookieManager != null)
                {
                    int cookieLightIndex = m_LightCookieManager.GetLightCookieShaderDataIndex(visLightIndex);
                    // We could test this in shader (static if) a variant (shader change) is undesirable. Same for spot light.
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightCookies, cookieLightIndex >= 0);
                    cmd.SetGlobalInt(ShaderConstants._CookieLightIndex, cookieLightIndex);
                }

                cmd.SetGlobalVector(ShaderConstants._LightPosWS, lightPos);
                cmd.SetGlobalVector(ShaderConstants._LightColor, lightColor);
                cmd.SetGlobalVector(ShaderConstants._LightAttenuation, lightAttenuation);
                cmd.SetGlobalVector(ShaderConstants._LightOcclusionProbInfo, lightOcclusionChannel);
                cmd.SetGlobalInt(ShaderConstants._LightFlags, lightFlags);
                cmd.SetGlobalInt(ShaderConstants._ShadowLightIndex, shadowLightIndex);
                cmd.SetGlobalInt(ShaderConstants._LightLayerMask, (int)lightLayerMask);

                // Stencil pass.
                cmd.DrawMesh(m_SphereMesh, transformMatrix, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.StencilVolume]);

                // Lighting pass.
                cmd.DrawMesh(m_SphereMesh, transformMatrix, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.PunctualLit]);
                cmd.DrawMesh(m_SphereMesh, transformMatrix, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.PunctualSimpleLit]);
            }

            cmd.DisableShaderKeyword(ShaderKeywordStrings._POINT);
        }

        void RenderStencilSpotLights(CommandBuffer cmd, ref RenderingData renderingData, NativeArray<VisibleLight> visibleLights)
        {
            if (m_HemisphereMesh == null)
                m_HemisphereMesh = CreateHemisphereMesh();

            cmd.EnableShaderKeyword(ShaderKeywordStrings._SPOT);

            for (int soffset = m_stencilVisLightOffsets[(int)LightType.Spot]; soffset < m_stencilVisLights.Length; ++soffset)
            {
                ushort visLightIndex = m_stencilVisLights[soffset];
                ref VisibleLight vl = ref visibleLights.UnsafeElementAtMutable(visLightIndex);
                if (vl.lightType != LightType.Spot)
                    break;

                // Cache light to local, avoid light find on every access.
                Light light = vl.light;

                float alpha = Mathf.Deg2Rad * vl.spotAngle * 0.5f;
                float cosAlpha = Mathf.Cos(alpha);
                float sinAlpha = Mathf.Sin(alpha);
                // Artificially inflate the geometric shape to fit the analytic spot shape.
                // The tighter the spot shape, the lesser inflation is needed.
                float guard = Mathf.Lerp(1.0f, kStencilShapeGuard, sinAlpha);

                Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
                UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, visLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

                var additionalLightData = light.GetUniversalAdditionalLightData();
                uint lightLayerMask = RenderingLayerUtils.ToValidRenderingLayers(additionalLightData.renderingLayers);

                int lightFlags = 0;
                if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
                    lightFlags |= (int)LightFlag.SubtractiveMixedLighting;

                int shadowLightIndex = m_AdditionalLightsShadowCasterPass != null ? m_AdditionalLightsShadowCasterPass.GetShadowLightIndexFromLightIndex(visLightIndex) : -1;
                bool hasDeferredLightShadows = light && light.shadows != LightShadows.None && shadowLightIndex >= 0;
                bool hasSoftShadow = hasDeferredLightShadows && renderingData.shadowData.supportsSoftShadows && light.shadows == LightShadows.Soft;

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, hasDeferredLightShadows);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, hasSoftShadow);

                if (m_LightCookieManager != null)
                {
                    int cookieLightIndex = m_LightCookieManager.GetLightCookieShaderDataIndex(visLightIndex);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightCookies, cookieLightIndex >= 0);
                    cmd.SetGlobalInt(ShaderConstants._CookieLightIndex, cookieLightIndex);
                }

                cmd.SetGlobalVector(ShaderConstants._SpotLightScale, new Vector4(sinAlpha, sinAlpha, 1.0f - cosAlpha, vl.range));
                cmd.SetGlobalVector(ShaderConstants._SpotLightBias, new Vector4(0.0f, 0.0f, cosAlpha, 0.0f));
                cmd.SetGlobalVector(ShaderConstants._SpotLightGuard, new Vector4(guard, guard, guard, cosAlpha * vl.range));
                cmd.SetGlobalVector(ShaderConstants._LightPosWS, lightPos);
                cmd.SetGlobalVector(ShaderConstants._LightColor, lightColor);
                cmd.SetGlobalVector(ShaderConstants._LightAttenuation, lightAttenuation);
                cmd.SetGlobalVector(ShaderConstants._LightDirection, new Vector3(lightSpotDir.x, lightSpotDir.y, lightSpotDir.z));
                cmd.SetGlobalVector(ShaderConstants._LightOcclusionProbInfo, lightOcclusionChannel);
                cmd.SetGlobalInt(ShaderConstants._LightFlags, lightFlags);
                cmd.SetGlobalInt(ShaderConstants._ShadowLightIndex, shadowLightIndex);
                cmd.SetGlobalInt(ShaderConstants._LightLayerMask, (int)lightLayerMask);

                // Stencil pass.
                cmd.DrawMesh(m_HemisphereMesh, vl.localToWorldMatrix, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.StencilVolume]);

                // Lighting pass.
                cmd.DrawMesh(m_HemisphereMesh, vl.localToWorldMatrix, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.PunctualLit]);
                cmd.DrawMesh(m_HemisphereMesh, vl.localToWorldMatrix, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.PunctualSimpleLit]);
            }

            cmd.DisableShaderKeyword(ShaderKeywordStrings._SPOT);
        }

        void RenderSSAOBeforeShading(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (m_FullscreenMesh == null)
                m_FullscreenMesh = CreateFullscreenMesh();

            cmd.DrawMesh(m_FullscreenMesh, Matrix4x4.identity, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.SSAOOnly]);
        }

        void RenderFog(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Legacy fog does not work in orthographic mode.
            if (!RenderSettings.fog || renderingData.cameraData.camera.orthographic)
                return;

            if (m_FullscreenMesh == null)
                m_FullscreenMesh = CreateFullscreenMesh();

            using (new ProfilingScope(cmd, m_ProfilingSamplerDeferredFogPass))
            {
                // Fog parameters and shader variant keywords are already set externally.
                cmd.DrawMesh(m_FullscreenMesh, Matrix4x4.identity, m_StencilDeferredMaterial, 0, m_StencilDeferredPasses[(int)StencilDeferredPasses.Fog]);
            }
        }

        void InitStencilDeferredMaterial()
        {
            if (m_StencilDeferredMaterial == null)
                return;

            // Pass indices can not be hardcoded because some platforms will strip out some passes, offset the index of later passes.
            for (int pass = 0; pass < k_StencilDeferredPassNames.Length; ++pass)
                m_StencilDeferredPasses[pass] = m_StencilDeferredMaterial.FindPass(k_StencilDeferredPassNames[pass]);

            m_StencilDeferredMaterial.SetFloat(ShaderConstants._StencilRef, (float)StencilUsage.MaterialUnlit);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._StencilReadMask, (float)StencilUsage.MaterialMask);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._StencilWriteMask, (float)StencilUsage.StencilLight);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._LitPunctualStencilRef, (float)((int)StencilUsage.StencilLight | (int)StencilUsage.MaterialLit));
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._LitPunctualStencilReadMask, (float)((int)StencilUsage.StencilLight | (int)StencilUsage.MaterialMask));
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._LitPunctualStencilWriteMask, (float)StencilUsage.StencilLight);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._SimpleLitPunctualStencilRef, (float)((int)StencilUsage.StencilLight | (int)StencilUsage.MaterialSimpleLit));
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._SimpleLitPunctualStencilReadMask, (float)((int)StencilUsage.StencilLight | (int)StencilUsage.MaterialMask));
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._SimpleLitPunctualStencilWriteMask, (float)StencilUsage.StencilLight);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._LitDirStencilRef, (float)StencilUsage.MaterialLit);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._LitDirStencilReadMask, (float)StencilUsage.MaterialMask);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._LitDirStencilWriteMask, 0.0f);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._SimpleLitDirStencilRef, (float)StencilUsage.MaterialSimpleLit);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._SimpleLitDirStencilReadMask, (float)StencilUsage.MaterialMask);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._SimpleLitDirStencilWriteMask, 0.0f);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._ClearStencilRef, 0.0f);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._ClearStencilReadMask, (float)StencilUsage.MaterialMask);
            m_StencilDeferredMaterial.SetFloat(ShaderConstants._ClearStencilWriteMask, (float)StencilUsage.MaterialMask);
        }

        static Mesh CreateSphereMesh()
        {
            // This icosaedron has been been slightly inflated to fit an unit sphere.
            // This is the same geometry as built-in deferred.

            Vector3[] positions =
            {
                new Vector3(0.000f,  0.000f, -1.070f), new Vector3(0.174f, -0.535f, -0.910f),
                new Vector3(-0.455f, -0.331f, -0.910f), new Vector3(0.562f,  0.000f, -0.910f),
                new Vector3(-0.455f,  0.331f, -0.910f), new Vector3(0.174f,  0.535f, -0.910f),
                new Vector3(-0.281f, -0.865f, -0.562f), new Vector3(0.736f, -0.535f, -0.562f),
                new Vector3(0.296f, -0.910f, -0.468f), new Vector3(-0.910f,  0.000f, -0.562f),
                new Vector3(-0.774f, -0.562f, -0.478f), new Vector3(0.000f, -1.070f,  0.000f),
                new Vector3(-0.629f, -0.865f,  0.000f), new Vector3(0.629f, -0.865f,  0.000f),
                new Vector3(-1.017f, -0.331f,  0.000f), new Vector3(0.957f,  0.000f, -0.478f),
                new Vector3(0.736f,  0.535f, -0.562f), new Vector3(1.017f, -0.331f,  0.000f),
                new Vector3(1.017f,  0.331f,  0.000f), new Vector3(-0.296f, -0.910f,  0.478f),
                new Vector3(0.281f, -0.865f,  0.562f), new Vector3(0.774f, -0.562f,  0.478f),
                new Vector3(-0.736f, -0.535f,  0.562f), new Vector3(0.910f,  0.000f,  0.562f),
                new Vector3(0.455f, -0.331f,  0.910f), new Vector3(-0.174f, -0.535f,  0.910f),
                new Vector3(0.629f,  0.865f,  0.000f), new Vector3(0.774f,  0.562f,  0.478f),
                new Vector3(0.455f,  0.331f,  0.910f), new Vector3(0.000f,  0.000f,  1.070f),
                new Vector3(-0.562f,  0.000f,  0.910f), new Vector3(-0.957f,  0.000f,  0.478f),
                new Vector3(0.281f,  0.865f,  0.562f), new Vector3(-0.174f,  0.535f,  0.910f),
                new Vector3(0.296f,  0.910f, -0.478f), new Vector3(-1.017f,  0.331f,  0.000f),
                new Vector3(-0.736f,  0.535f,  0.562f), new Vector3(-0.296f,  0.910f,  0.478f),
                new Vector3(0.000f,  1.070f,  0.000f), new Vector3(-0.281f,  0.865f, -0.562f),
                new Vector3(-0.774f,  0.562f, -0.478f), new Vector3(-0.629f,  0.865f,  0.000f),
            };

            int[] indices =
            {
                0,  1,  2,  0,  3,  1,  2,  4,  0,  0,  5,  3,  0,  4,  5,  1,  6,  2,
                3,  7,  1,  1,  8,  6,  1,  7,  8,  9,  4,  2,  2,  6, 10, 10,  9,  2,
                8, 11,  6,  6, 12, 10, 11, 12,  6,  7, 13,  8,  8, 13, 11, 10, 14,  9,
                10, 12, 14,  3, 15,  7,  5, 16,  3,  3, 16, 15, 15, 17,  7, 17, 13,  7,
                16, 18, 15, 15, 18, 17, 11, 19, 12, 13, 20, 11, 11, 20, 19, 17, 21, 13,
                13, 21, 20, 12, 19, 22, 12, 22, 14, 17, 23, 21, 18, 23, 17, 21, 24, 20,
                23, 24, 21, 20, 25, 19, 19, 25, 22, 24, 25, 20, 26, 18, 16, 18, 27, 23,
                26, 27, 18, 28, 24, 23, 27, 28, 23, 24, 29, 25, 28, 29, 24, 25, 30, 22,
                25, 29, 30, 14, 22, 31, 22, 30, 31, 32, 28, 27, 26, 32, 27, 33, 29, 28,
                30, 29, 33, 33, 28, 32, 34, 26, 16,  5, 34, 16, 14, 31, 35, 14, 35,  9,
                31, 30, 36, 30, 33, 36, 35, 31, 36, 37, 33, 32, 36, 33, 37, 38, 32, 26,
                34, 38, 26, 38, 37, 32,  5, 39, 34, 39, 38, 34,  4, 39,  5,  9, 40,  4,
                9, 35, 40,  4, 40, 39, 35, 36, 41, 41, 36, 37, 41, 37, 38, 40, 35, 41,
                40, 41, 39, 41, 38, 39,
            };


            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;

            return mesh;
        }

        static Mesh CreateHemisphereMesh()
        {
            // TODO reorder for pre&post-transform cache optimisation.
            // This capped hemisphere shape is in unit dimensions. It will be slightly inflated in the vertex shader
            // to fit the cone analytical shape.
            Vector3[] positions =
            {
                new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(1.000000f, 0.000000f, 0.000000f),
                new Vector3(0.923880f, 0.382683f, 0.000000f), new Vector3(0.707107f, 0.707107f, 0.000000f),
                new Vector3(0.382683f, 0.923880f, 0.000000f), new Vector3(-0.000000f, 1.000000f, 0.000000f),
                new Vector3(-0.382684f, 0.923880f, 0.000000f), new Vector3(-0.707107f, 0.707107f, 0.000000f),
                new Vector3(-0.923880f, 0.382683f, 0.000000f), new Vector3(-1.000000f, -0.000000f, 0.000000f),
                new Vector3(-0.923880f, -0.382683f, 0.000000f), new Vector3(-0.707107f, -0.707107f, 0.000000f),
                new Vector3(-0.382683f, -0.923880f, 0.000000f), new Vector3(0.000000f, -1.000000f, 0.000000f),
                new Vector3(0.382684f, -0.923879f, 0.000000f), new Vector3(0.707107f, -0.707107f, 0.000000f),
                new Vector3(0.923880f, -0.382683f, 0.000000f), new Vector3(0.000000f, 0.000000f, 1.000000f),
                new Vector3(0.707107f, 0.000000f, 0.707107f), new Vector3(0.000000f, -0.707107f, 0.707107f),
                new Vector3(0.000000f, 0.707107f, 0.707107f), new Vector3(-0.707107f, 0.000000f, 0.707107f),
                new Vector3(0.816497f, -0.408248f, 0.408248f), new Vector3(0.408248f, -0.408248f, 0.816497f),
                new Vector3(0.408248f, -0.816497f, 0.408248f), new Vector3(0.408248f, 0.816497f, 0.408248f),
                new Vector3(0.408248f, 0.408248f, 0.816497f), new Vector3(0.816497f, 0.408248f, 0.408248f),
                new Vector3(-0.816497f, 0.408248f, 0.408248f), new Vector3(-0.408248f, 0.408248f, 0.816497f),
                new Vector3(-0.408248f, 0.816497f, 0.408248f), new Vector3(-0.408248f, -0.816497f, 0.408248f),
                new Vector3(-0.408248f, -0.408248f, 0.816497f), new Vector3(-0.816497f, -0.408248f, 0.408248f),
                new Vector3(0.000000f, -0.923880f, 0.382683f), new Vector3(0.923880f, 0.000000f, 0.382683f),
                new Vector3(0.000000f, -0.382683f, 0.923880f), new Vector3(0.382683f, 0.000000f, 0.923880f),
                new Vector3(0.000000f, 0.923880f, 0.382683f), new Vector3(0.000000f, 0.382683f, 0.923880f),
                new Vector3(-0.923880f, 0.000000f, 0.382683f), new Vector3(-0.382683f, 0.000000f, 0.923880f)
            };

            int[] indices =
            {
                0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 5, 4, 0, 6, 5, 0,
                7, 6, 0, 8, 7, 0, 9, 8, 0, 10, 9, 0, 11, 10, 0, 12,
                11, 0, 13, 12, 0, 14, 13, 0, 15, 14, 0, 16, 15, 0, 1, 16,
                22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 14, 24, 34, 35,
                22, 16, 36, 23, 37, 2, 27, 35, 38, 25, 4, 37, 26, 39, 6, 30,
                38, 40, 28, 8, 39, 29, 41, 10, 33, 40, 34, 31, 12, 41, 32, 36,
                15, 22, 24, 18, 23, 22, 19, 24, 23, 3, 25, 27, 20, 26, 25, 18,
                27, 26, 7, 28, 30, 21, 29, 28, 20, 30, 29, 11, 31, 33, 19, 32,
                31, 21, 33, 32, 13, 14, 34, 15, 24, 14, 19, 34, 24, 1, 35, 16,
                18, 22, 35, 15, 16, 22, 17, 36, 37, 19, 23, 36, 18, 37, 23, 1,
                2, 35, 3, 27, 2, 18, 35, 27, 5, 38, 4, 20, 25, 38, 3, 4,
                25, 17, 37, 39, 18, 26, 37, 20, 39, 26, 5, 6, 38, 7, 30, 6,
                20, 38, 30, 9, 40, 8, 21, 28, 40, 7, 8, 28, 17, 39, 41, 20,
                29, 39, 21, 41, 29, 9, 10, 40, 11, 33, 10, 21, 40, 33, 13, 34,
                12, 19, 31, 34, 11, 12, 31, 17, 41, 36, 21, 32, 41, 19, 36, 32
            };

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;

            return mesh;
        }

        static Mesh CreateFullscreenMesh()
        {
            // TODO reorder for pre&post-transform cache optimisation.
            // Simple full-screen triangle.
            Vector3[] positions =
            {
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(-1.0f, -3.0f, 0.0f),
                new Vector3(3.0f,  1.0f, 0.0f)
            };

            int[] indices = { 0, 1, 2 };

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;

            return mesh;
        }
    }

    /*
    struct BitArray : System.IDisposable
    {
        NativeArray<uint> m_Mem; // ulong not supported in il2cpp???
        int m_BitCount;
        int m_IntCount;

        public BitArray(int bitCount, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            m_BitCount = bitCount;
            m_IntCount = (bitCount + 31) >> 5;
            m_Mem = new NativeArray<uint>(m_IntCount, allocator, options);
        }

        public void Dispose()
        {
            m_Mem.Dispose();
        }

        public void Clear()
        {
            for (int i = 0; i < m_IntCount; ++i)
                m_Mem[i] = 0;
        }

        public bool IsSet(int bitIndex)
        {
            return (m_Mem[bitIndex >> 5] & (1u << (bitIndex & 31))) != 0;
        }

        public void Set(int bitIndex, bool val)
        {
            if (val)
                m_Mem[bitIndex >> 5] |= 1u << (bitIndex & 31);
            else
                m_Mem[bitIndex >> 5] &= ~(1u << (bitIndex & 31));
        }
    };
    */
}
