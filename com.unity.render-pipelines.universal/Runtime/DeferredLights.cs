using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
        // Keep in sync with shader define USE_CBUFFER_FOR_DEPTHRANGE
        // Keep in sync with shader define USE_CBUFFER_FOR_TILELIST
        // Keep in sync with shader define USE_CBUFFER_FOR_LIGHTDATA
        // Keep in sync with shader define USE_CBUFFER_FOR_LIGHTLIST
        internal static bool IsOpenGL { get; set; }

        // DX10 uses SM 4.0. However URP shaders requires SM 4.5 or will use fallback to SM 2.0 shaders otherwise.
        // We will consider deferred renderer is not available when SM 2.0 shaders run.
        internal static bool IsDX10 { get; set; }

        // Constant buffers are used for data that a repeatedly fetched by shaders.
        // Structured buffers are used for data only consumed once.
        internal static bool UseCBufferForDepthRange
        {
            get
            {
#if !UNITY_EDITOR && UNITY_SWITCH
                return false;
#else
                return IsOpenGL;
#endif
            }
        }

        internal static bool UseCBufferForTileList
        {
            get
            {
#if !UNITY_EDITOR && UNITY_SWITCH
                return false;
#else
                return IsOpenGL;
#endif
            }
        }

        internal static bool UseCBufferForLightData
        {
            get
            {
                return true;
            }
        }

        internal static bool UseCBufferForLightList
        {
            get
            {
#if !UNITY_EDITOR && UNITY_SWITCH
                return false;
#else
                return IsOpenGL;
#endif
            }
        }

        // Keep in sync with PREFERRED_CBUFFER_SIZE.
        public const int kPreferredCBufferSize = 64 * 1024;
        public const int kPreferredStructuredBufferSize = 128 * 1024;

        public const int kTilePixelWidth = 16;
        public const int kTilePixelHeight = 16;
        // Levels of hierarchical tiling. Each level process 4x4 finer tiles. For example:
        // For platforms using 16x16 px tiles, we use a 16x16px tiles grid, a 64x64px tiles grid, and a 256x256px tiles grid
        // For platforms using  8x8  px tiles, we use a  8x8px  tiles grid, a 32x32px tiles grid, and a 128x128px tiles grid
        public const int kTilerDepth = 3;
        public const int kTilerSubdivisions = 4;

        public const int kAvgLightPerTile = 32;

        // On platforms where the tile dimensions is large (16x16), it may be faster to generate tileDepthInfo texture
        // with an intermediate mip level, as this allows spawning more pixel shaders (avoid GPU starvation).
        // Set to -1 to disable.
#if UNITY_SWITCH || UNITY_IOS
        public const int kTileDepthInfoIntermediateLevel = 1;
#else
        public const int kTileDepthInfoIntermediateLevel = -1;
#endif

#if !UNITY_EDITOR && UNITY_SWITCH
        public const bool kHasNativeQuadSupport = true;
#else
        public const bool kHasNativeQuadSupport = false;
#endif
    }

    internal enum LightFlag
    {
        // Keep in sync with kLightFlagSubtractiveMixedLighting.
        SubtractiveMixedLighting = 4
    }

    // Manages tiled-based deferred lights.
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

            public static readonly int UDepthRanges = Shader.PropertyToID("UDepthRanges");
            public static readonly int _DepthRanges = Shader.PropertyToID("_DepthRanges");
            public static readonly int _DownsamplingWidth = Shader.PropertyToID("_DownsamplingWidth");
            public static readonly int _DownsamplingHeight = Shader.PropertyToID("_DownsamplingHeight");
            public static readonly int _SourceShiftX = Shader.PropertyToID("_SourceShiftX");
            public static readonly int _SourceShiftY = Shader.PropertyToID("_SourceShiftY");
            public static readonly int _TileShiftX = Shader.PropertyToID("_TileShiftX");
            public static readonly int _TileShiftY = Shader.PropertyToID("_TileShiftY");
            public static readonly int _tileXCount = Shader.PropertyToID("_tileXCount");
            public static readonly int _DepthRangeOffset = Shader.PropertyToID("_DepthRangeOffset");
            public static readonly int _BitmaskTex = Shader.PropertyToID("_BitmaskTex");
            public static readonly int UTileList = Shader.PropertyToID("UTileList");
            public static readonly int _TileList = Shader.PropertyToID("_TileList");
            public static readonly int UPunctualLightBuffer = Shader.PropertyToID("UPunctualLightBuffer");
            public static readonly int _PunctualLightBuffer = Shader.PropertyToID("_PunctualLightBuffer");
            public static readonly int URelLightList = Shader.PropertyToID("URelLightList");
            public static readonly int _RelLightList = Shader.PropertyToID("_RelLightList");
            public static readonly int _TilePixelWidth = Shader.PropertyToID("_TilePixelWidth");
            public static readonly int _TilePixelHeight = Shader.PropertyToID("_TilePixelHeight");
            public static readonly int _InstanceOffset = Shader.PropertyToID("_InstanceOffset");
            public static readonly int _DepthTex = Shader.PropertyToID("_DepthTex");
            public static readonly int _DepthTexSize = Shader.PropertyToID("_DepthTexSize");

            public static readonly int _ScreenToWorld = Shader.PropertyToID("_ScreenToWorld");
            public static readonly int _unproject0 = Shader.PropertyToID("_unproject0");
            public static readonly int _unproject1 = Shader.PropertyToID("_unproject1");

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

        // Disable Burst for now since there are issues on macos builds.
#if URP_HAS_BURST
        [Unity.Burst.BurstCompile(CompileSynchronously = true)]
#endif
        struct CullLightsJob : IJob
        {
            public DeferredTiler tiler;
            [ReadOnly]
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            public NativeArray<DeferredTiler.PrePunctualLight> prePunctualLights;
            [ReadOnly]
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            public NativeArray<ushort> coarseTiles;
            [ReadOnly]
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            public NativeArray<uint> coarseTileHeaders;
            public int coarseHeaderOffset;
            public int istart;
            public int iend;
            public int jstart;
            public int jend;

            public void Execute()
            {
                int coarseTileOffset = (int)coarseTileHeaders[coarseHeaderOffset + 0];
                int coarseVisLightCount = (int)coarseTileHeaders[coarseHeaderOffset + 1];

                if (tiler.TilerLevel != 0)
                {
                    tiler.CullIntermediateLights(
                        ref prePunctualLights,
                        ref coarseTiles, coarseTileOffset, coarseVisLightCount,
                        istart, iend, jstart, jend
                    );
                }
                else
                {
                    tiler.CullFinalLights(
                        ref prePunctualLights,
                        ref coarseTiles, coarseTileOffset, coarseVisLightCount,
                        istart, iend, jstart, jend
                    );
                }
            }
        }

        struct DrawCall
        {
            public ComputeBuffer tileList;
            public ComputeBuffer punctualLightBuffer;
            public ComputeBuffer relLightList;
            public int tileListSize;
            public int punctualLightBufferSize;
            public int relLightListSize;
            public int instanceOffset;
            public int instanceCount;
        }

        static readonly string[] k_GBufferNames = new string[]
        {
            "_GBuffer0",
            "_GBuffer1",
            "_GBuffer2",
            "_GBuffer3",
            "_GBuffer4",
            "_GBuffer5",
            "_GBuffer6"
        };

        static readonly string[] k_TileDeferredPassNames = new string[]
        {
            "Tiled Deferred Punctual Light (Lit)",
            "Tiled Deferred Punctual Light (SimpleLit)"
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

        internal enum TileDeferredPasses
        {
            PunctualLit,
            PunctualSimpleLit,
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
        static readonly string k_TileDepthInfo = "Tile Depth Info";
        static readonly string k_DeferredTiledPass = "Deferred Shading (Tile-Based)";
        static readonly string k_DeferredStencilPass = "Deferred Shading (Stencil)";
        static readonly string k_DeferredFogPass = "Deferred Fog";
        static readonly string k_ClearStencilPartial = "Clear Stencil Partial";
        static readonly string k_SetupLightConstants = "Setup Light Constants";
        static readonly float kStencilShapeGuard = 1.06067f; // stencil geometric shapes must be inflated to fit the analytic shapes.
        private static readonly ProfilingSampler m_ProfilingSetupLights = new ProfilingSampler(k_SetupLights);
        private static readonly ProfilingSampler m_ProfilingDeferredPass = new ProfilingSampler(k_DeferredPass);
        private static readonly ProfilingSampler m_ProfilingTileDepthInfo = new ProfilingSampler(k_TileDepthInfo);
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
                return GraphicsFormat.R8_UNorm;
            else
                return GraphicsFormat.None;
        }

        // This may return different values depending on what lights are rendered for a given frame.
        internal bool UseShadowMask { get { return this.MixedLightingSetup != MixedLightingSetup.None; } }
        //
        internal bool UseRenderingLayers { get { return UniversalRenderPipeline.asset.supportsLightLayers; } }
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
        // true: TileDeferred.shader used for some lights (currently: point/spot lights without shadows) - false: use StencilDeferred.shader for all lights
        internal bool TiledDeferredShading { get; set; }
        // We browse all visible lights and found the mixed lighting setup every frame.
        internal MixedLightingSetup MixedLightingSetup { get; set; }
        //
        internal bool UseJobSystem { get; set; }
        //
        internal int RenderWidth { get; set; }
        //
        internal int RenderHeight { get; set; }

        // Output lighting result.
        internal RenderTargetHandle[] GbufferAttachments { get; set; }
        internal RenderTargetIdentifier[] DeferredInputAttachments { get; set; }
        internal bool[] DeferredInputIsTransient { get; set; }
        // Input depth texture, also bound as read-only RT
        internal RenderTargetHandle DepthAttachment { get; set; }
        //
        internal RenderTargetHandle DepthCopyTexture { get; set; }
        // Intermediate depth info texture.
        internal RenderTargetHandle DepthInfoTexture { get; set; }
        // Per-tile depth info texture.
        internal RenderTargetHandle TileDepthInfoTexture { get; set; }

        internal RenderTargetIdentifier[] GbufferAttachmentIdentifiers { get; set; }
        internal GraphicsFormat[] GbufferFormats { get; set; }
        internal RenderTargetIdentifier DepthAttachmentIdentifier { get; set; }
        internal RenderTargetIdentifier DepthCopyTextureIdentifier { get; set; }
        internal RenderTargetIdentifier DepthInfoTextureIdentifier { get; set; }
        internal RenderTargetIdentifier TileDepthInfoTextureIdentifier { get; set; }

        // Cached.
        int m_CachedRenderWidth = 0;
        // Cached.
        int m_CachedRenderHeight = 0;
        // Cached.
        Matrix4x4 m_CachedProjectionMatrix;

        // Hierarchical tilers.
        DeferredTiler[] m_Tilers;
        int[] m_TileDataCapacities;

        // Should any visible lights be rendered as tile?
        bool m_HasTileVisLights;
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

        // Max number of tile depth range data that can be referenced per draw call.
        int m_MaxDepthRangePerBatch;
        // Max numer of instanced tile that can be referenced per draw call.
        int m_MaxTilesPerBatch;
        // Max number of punctual lights that can be referenced per draw call.
        int m_MaxPunctualLightPerBatch;
        // Max number of relative light indices that can be referenced per draw call.
        int m_MaxRelLightIndicesPerBatch;

        // Generate per-tile depth information.
        Material m_TileDepthInfoMaterial;
        // Hold all shaders for tiled-based deferred shading.
        Material m_TileDeferredMaterial;
        // Hold all shaders for stencil-volume deferred shading.
        Material m_StencilDeferredMaterial;

        // Pass indices.
        int[] m_StencilDeferredPasses;
        // Pass indices.
        int[] m_TileDeferredPasses;

        // Avoid memory allocations.
        Matrix4x4[] m_ScreenToWorld = new Matrix4x4[2];

        ProfilingSampler m_ProfilingSamplerDeferredTiledPass = new ProfilingSampler(k_DeferredTiledPass);
        ProfilingSampler m_ProfilingSamplerDeferredStencilPass = new ProfilingSampler(k_DeferredStencilPass);
        ProfilingSampler m_ProfilingSamplerDeferredFogPass = new ProfilingSampler(k_DeferredFogPass);
        ProfilingSampler m_ProfilingSamplerClearStencilPartialPass = new ProfilingSampler(k_ClearStencilPartial);

        private LightCookieManager m_LightCookieManager;

        internal struct InitParams
        {
            public Material tileDepthInfoMaterial;
            public Material tileDeferredMaterial;
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

            m_TileDepthInfoMaterial = initParams.tileDepthInfoMaterial;
            m_TileDeferredMaterial = initParams.tileDeferredMaterial;
            m_StencilDeferredMaterial = initParams.stencilDeferredMaterial;

            m_TileDeferredPasses = new int[k_TileDeferredPassNames.Length];
            InitTileDeferredMaterial();

            m_StencilDeferredPasses = new int[k_StencilDeferredPassNames.Length];
            InitStencilDeferredMaterial();

            // Compute some platform limits (for deferred tiling).
            m_MaxDepthRangePerBatch = (DeferredConfig.UseCBufferForDepthRange ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / sizeof(uint);
            m_MaxTilesPerBatch = (DeferredConfig.UseCBufferForTileList ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / System.Runtime.InteropServices.Marshal.SizeOf(typeof(TileData));
            m_MaxPunctualLightPerBatch = (DeferredConfig.UseCBufferForLightData ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualLightData));
            m_MaxRelLightIndicesPerBatch = (DeferredConfig.UseCBufferForLightList ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / sizeof(uint);

            m_Tilers = new DeferredTiler[DeferredConfig.kTilerDepth];
            m_TileDataCapacities = new int[DeferredConfig.kTilerDepth];

            // Initialize hierarchical tilers. Next tiler processes 4x4 of the tiles of the previous tiler.
            // Tiler 0 has finest tiles, coarser tilers follow.
            for (int tilerLevel = 0; tilerLevel < DeferredConfig.kTilerDepth; ++tilerLevel)
            {
                int scale = (int)Mathf.Pow(DeferredConfig.kTilerSubdivisions, tilerLevel);
                m_Tilers[tilerLevel] = new DeferredTiler(
                    DeferredConfig.kTilePixelWidth * scale,
                    DeferredConfig.kTilePixelHeight * scale,
                    DeferredConfig.kAvgLightPerTile * scale * scale,
                    tilerLevel
                );

                m_TileDataCapacities[tilerLevel] = 0; // not known yet
            }

            this.AccurateGbufferNormals = true;
            this.TiledDeferredShading = true;
            this.UseJobSystem = true;
            m_HasTileVisLights = false;
            this.UseRenderPass = useNativeRenderPass;
            m_LightCookieManager = initParams.lightCookieManager;
        }

        internal ref DeferredTiler GetTiler(int i)
        {
            return ref m_Tilers[i];
        }

        internal void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Profiler.BeginSample(k_SetupLights);

            DeferredShaderData.instance.ResetBuffers();

            Camera camera = renderingData.cameraData.camera;
            // Support for dynamic resolution.
            this.RenderWidth = camera.allowDynamicResolution ? Mathf.CeilToInt(ScalableBufferManager.widthScaleFactor * renderingData.cameraData.cameraTargetDescriptor.width) : renderingData.cameraData.cameraTargetDescriptor.width;
            this.RenderHeight = camera.allowDynamicResolution ? Mathf.CeilToInt(ScalableBufferManager.heightScaleFactor * renderingData.cameraData.cameraTargetDescriptor.height) : renderingData.cameraData.cameraTargetDescriptor.height;

            if (this.TiledDeferredShading)
            {
                // Precompute tile data again if the camera projection or the screen resolution has changed.
                if (m_CachedRenderWidth != this.RenderWidth
                    || m_CachedRenderHeight != this.RenderHeight
                    || m_CachedProjectionMatrix != renderingData.cameraData.camera.projectionMatrix)
                {
                    m_CachedRenderWidth = this.RenderWidth;
                    m_CachedRenderHeight = this.RenderHeight;
                    m_CachedProjectionMatrix = renderingData.cameraData.camera.projectionMatrix;

                    for (int tilerIndex = 0; tilerIndex < m_Tilers.Length; ++tilerIndex)
                    {
                        m_Tilers[tilerIndex].PrecomputeTiles(renderingData.cameraData.camera.projectionMatrix,
                            renderingData.cameraData.camera.orthographic, m_CachedRenderWidth, m_CachedRenderHeight);
                    }
                }

                // Allocate temporary resources for each hierarchical tiler.
                for (int tilerIndex = 0; tilerIndex < m_Tilers.Length; ++tilerIndex)
                    m_Tilers[tilerIndex].Setup(m_TileDataCapacities[tilerIndex]);
            }

            // Will hold punctual lights that will be rendered using tiles.
            NativeArray<DeferredTiler.PrePunctualLight> prePunctualLights;

            // inspect lights in renderingData.lightData.visibleLights and convert them to entries in prePunctualLights OR m_stencilVisLights
            // currently we store point lights and spot lights that can be rendered by TiledDeferred, in the same prePunctualLights list
            PrecomputeLights(
                out prePunctualLights,
                out m_stencilVisLights,
                out m_stencilVisLightOffsets,
                ref renderingData.lightData.visibleLights,
                renderingData.lightData.additionalLightsCount != 0 || renderingData.lightData.mainLightIndex >= 0,
                renderingData.cameraData.camera.worldToCameraMatrix,
                renderingData.cameraData.camera.orthographic,
                renderingData.cameraData.camera.nearClipPlane
            );

            {
                CommandBuffer cmd = CommandBufferPool.Get();
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
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            if (this.TiledDeferredShading)
            {
                // Sort lights front to back.
                // This allows a further optimisation where per-tile light lists can be more easily trimmed on both ends in the vertex shading instancing the tiles.
                SortLights(ref prePunctualLights);

                NativeArray<ushort> defaultIndices = new NativeArray<ushort>(prePunctualLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < prePunctualLights.Length; ++i)
                    defaultIndices[i] = (ushort)i;

                NativeArray<uint> defaultHeaders = new NativeArray<uint>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                defaultHeaders[0] = 0; // tileHeaders offset
                defaultHeaders[1] = (uint)prePunctualLights.Length; // tileHeaders count

                // Cull tile-friendly lights into the coarse tile structure.
                ref DeferredTiler coarsestTiler = ref m_Tilers[m_Tilers.Length - 1];
                if (m_Tilers.Length != 1)
                {
                    NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>();
                    int jobOffset = 0;
                    int jobCount = 0;

                    if (this.UseJobSystem)
                    {
                        int totalJobCount = 1;
                        for (int t = m_Tilers.Length - 1; t > 0; --t)
                        {
                            ref DeferredTiler coarseTiler = ref m_Tilers[t];
                            totalJobCount += coarseTiler.TileXCount * coarseTiler.TileYCount;
                        }
                        jobHandles = new NativeArray<JobHandle>(totalJobCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    }

                    // Fill coarsestTiler.m_Tiles with for each tile, a list of lightIndices from prePunctualLights that intersect the tile
                    CullLightsJob coarsestJob = new CullLightsJob
                    {
                        tiler = coarsestTiler,
                        prePunctualLights = prePunctualLights,
                        coarseTiles = defaultIndices,
                        coarseTileHeaders = defaultHeaders,
                        coarseHeaderOffset = 0,
                        istart = 0,
                        iend = coarsestTiler.TileXCount,
                        jstart = 0,
                        jend = coarsestTiler.TileYCount,
                    };
                    if (this.UseJobSystem)
                    {
                        jobHandles[jobCount++] = coarsestJob.Schedule();
                        // Start this job now, as the main thread will be busy setting up all the dependent jobs.
                        JobHandle.ScheduleBatchedJobs();
                    }
                    else
                        coarsestJob.Execute();

                    // Filter to fine tile structure.
                    for (int t = m_Tilers.Length - 1; t > 0; --t)
                    {
                        ref DeferredTiler fineTiler = ref m_Tilers[t - 1];
                        ref DeferredTiler coarseTiler = ref m_Tilers[t];
                        int fineTileXCount = fineTiler.TileXCount;
                        int fineTileYCount = fineTiler.TileYCount;
                        int coarseTileXCount = coarseTiler.TileXCount;
                        int coarseTileYCount = coarseTiler.TileYCount;
                        int subdivX = (t == m_Tilers.Length - 1) ? coarseTileXCount : DeferredConfig.kTilerSubdivisions;
                        int subdivY = (t == m_Tilers.Length - 1) ? coarseTileYCount : DeferredConfig.kTilerSubdivisions;
                        int superCoarseTileXCount = (coarseTileXCount + subdivX - 1) / subdivX;
                        int superCoarseTileYCount = (coarseTileYCount + subdivY - 1) / subdivY;
                        NativeArray<ushort> coarseTiles = coarseTiler.Tiles;
                        NativeArray<uint> coarseTileHeaders = coarseTiler.TileHeaders;
                        int fineStepX = coarseTiler.TilePixelWidth / fineTiler.TilePixelWidth;
                        int fineStepY = coarseTiler.TilePixelHeight / fineTiler.TilePixelHeight;

                        for (int j = 0; j < coarseTileYCount; ++j)
                            for (int i = 0; i < coarseTileXCount; ++i)
                            {
                                int fine_istart = i * fineStepX;
                                int fine_jstart = j * fineStepY;
                                int fine_iend = Mathf.Min(fine_istart + fineStepX, fineTileXCount);
                                int fine_jend = Mathf.Min(fine_jstart + fineStepY, fineTileYCount);
                                int coarseHeaderOffset = coarseTiler.GetTileHeaderOffset(i, j);

                                CullLightsJob job = new CullLightsJob
                                {
                                    tiler = m_Tilers[t - 1],
                                    prePunctualLights = prePunctualLights,
                                    coarseTiles = coarseTiles,
                                    coarseTileHeaders = coarseTileHeaders,
                                    coarseHeaderOffset = coarseHeaderOffset,
                                    istart = fine_istart,
                                    iend = fine_iend,
                                    jstart = fine_jstart,
                                    jend = fine_jend,
                                };

                                if (this.UseJobSystem)
                                    jobHandles[jobCount++] = job.Schedule(jobHandles[jobOffset + (i / subdivX) + (j / subdivY) * superCoarseTileXCount]);
                                else
                                    job.Execute();
                            }

                        jobOffset += superCoarseTileXCount * superCoarseTileYCount;
                    }

                    if (this.UseJobSystem)
                    {
                        JobHandle.CompleteAll(jobHandles);
                        jobHandles.Dispose();
                    }
                }
                else
                {
                    coarsestTiler.CullFinalLights(
                        ref prePunctualLights,
                        ref defaultIndices, 0, prePunctualLights.Length,
                        0, coarsestTiler.TileXCount, 0, coarsestTiler.TileYCount
                    );
                }

                defaultIndices.Dispose();
                defaultHeaders.Dispose();
            }

            // We don't need this array anymore as all the lights have been inserted into the tile-grid structures.
            if (prePunctualLights.IsCreated)
                prePunctualLights.Dispose();

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
                    Light light = visibleLights[lightIndex].light;

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

            CreateGbufferAttachments();
        }

        // In cases when custom pass is injected between GBuffer and Deferred passes we need to fallback
        // To non-renderpass path in the middle of setup, which means recreating the gbuffer attachments as well due to GBuffer4 used for RenderPass
        internal void DisableFramebufferFetchInput()
        {
            this.UseRenderPass = false;
            CreateGbufferAttachments();
        }

        internal void CreateGbufferAttachments()
        {
            int gbufferSliceCount = this.GBufferSliceCount;
            if (this.GbufferAttachments == null || this.GbufferAttachments.Length != gbufferSliceCount)
            {
                this.GbufferAttachments = new RenderTargetHandle[gbufferSliceCount];
                for (int i = 0; i < gbufferSliceCount; ++i)
                    this.GbufferAttachments[i].Init(k_GBufferNames[i]);
            }
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
            RenderTargetHandle depthCopyTexture,
            RenderTargetHandle depthInfoTexture,
            RenderTargetHandle tileDepthInfoTexture,
            RenderTargetHandle depthAttachment,
            RenderTargetHandle colorAttachment)
        {
            m_AdditionalLightsShadowCasterPass = additionalLightsShadowCasterPass;
            this.HasDepthPrepass = hasDepthPrepass;
            this.HasNormalPrepass = hasNormalPrepass;

            this.DepthCopyTexture = depthCopyTexture;
            this.DepthInfoTexture = depthInfoTexture;
            this.TileDepthInfoTexture = tileDepthInfoTexture;

            this.GbufferAttachments[this.GBufferLightingIndex] = colorAttachment;
            this.DepthAttachment = depthAttachment;

            this.DepthCopyTextureIdentifier = this.DepthCopyTexture.Identifier();
            this.DepthInfoTextureIdentifier = this.DepthInfoTexture.Identifier();
            this.TileDepthInfoTextureIdentifier = this.TileDepthInfoTexture.Identifier();
            if (this.GbufferAttachmentIdentifiers == null || this.GbufferAttachmentIdentifiers.Length != this.GbufferAttachments.Length)
            {
                this.GbufferAttachmentIdentifiers = new RenderTargetIdentifier[this.GbufferAttachments.Length];
                this.GbufferFormats = new GraphicsFormat[this.GbufferAttachments.Length];
            }
            for (int i = 0; i < this.GbufferAttachments.Length; ++i)
            {
                this.GbufferAttachmentIdentifiers[i] = this.GbufferAttachments[i].Identifier();
                this.GbufferFormats[i] = this.GetGBufferFormat(i);
            }
            if (this.DeferredInputAttachments == null && this.UseRenderPass && this.GbufferAttachments.Length >= 5)
            {
                this.DeferredInputAttachments = new RenderTargetIdentifier[4]
                {
                    this.GbufferAttachmentIdentifiers[0], this.GbufferAttachmentIdentifiers[1],
                    this.GbufferAttachmentIdentifiers[2], this.GbufferAttachmentIdentifiers[4]
                };
                this.DeferredInputIsTransient = new bool[4]
                {
                    true, true, true, false
                };
            }
            this.DepthAttachmentIdentifier = depthAttachment.Identifier();
#if ENABLE_VR && ENABLE_XR_MODULE
            // In XR SinglePassInstance mode, the RTs are texture-array and all slices must be bound.
            if (renderingData.cameraData.xr.enabled)
            {
                this.DepthCopyTextureIdentifier = new RenderTargetIdentifier(this.DepthCopyTextureIdentifier, 0, CubemapFace.Unknown, -1);
                this.DepthInfoTextureIdentifier = new RenderTargetIdentifier(this.DepthInfoTextureIdentifier, 0, CubemapFace.Unknown, -1);
                this.TileDepthInfoTextureIdentifier = new RenderTargetIdentifier(this.TileDepthInfoTextureIdentifier, 0, CubemapFace.Unknown, -1);

                for (int i = 0; i < this.GbufferAttachmentIdentifiers.Length; ++i)
                    this.GbufferAttachmentIdentifiers[i] = new RenderTargetIdentifier(this.GbufferAttachmentIdentifiers[i], 0, CubemapFace.Unknown, -1);
                this.DepthAttachmentIdentifier = new RenderTargetIdentifier(this.DepthAttachmentIdentifier, 0, CubemapFace.Unknown, -1);
            }
#endif

            m_HasTileVisLights = this.TiledDeferredShading && CheckHasTileLights(ref renderingData.lightData.visibleLights);
        }

        public void OnCameraCleanup(CommandBuffer cmd)
        {
            // Disable any global keywords setup in SetupLights().
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._GBUFFER_NORMALS_OCT, false);

            for (int tilerIndex = 0; tilerIndex < m_Tilers.Length; ++tilerIndex)
            {
                m_TileDataCapacities[tilerIndex] = max(m_TileDataCapacities[tilerIndex], m_Tilers[tilerIndex].TileDataCapacity);
                m_Tilers[tilerIndex].OnCameraCleanup();
            }

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

        internal bool HasTileLights()
        {
            return m_HasTileVisLights;
        }

        internal bool HasTileDepthRangeExtraPass()
        {
            ref DeferredTiler tiler = ref m_Tilers[0];
            int tilePixelWidth = tiler.TilePixelWidth;
            int tilePixelHeight = tiler.TilePixelHeight;
            int tileMipLevel = (int)Mathf.Log(Mathf.Min(tilePixelWidth, tilePixelHeight), 2);
            return DeferredConfig.kTileDepthInfoIntermediateLevel >= 0 && DeferredConfig.kTileDepthInfoIntermediateLevel < tileMipLevel;
        }

        internal void ExecuteTileDepthInfoPass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_TileDepthInfoMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDepthInfoMaterial, GetType().Name);
                return;
            }

            Assertions.Assert.IsTrue(
                m_Tilers[0].TilePixelWidth == m_Tilers[0].TilePixelHeight || DeferredConfig.kTileDepthInfoIntermediateLevel <= 0,
                "for non square tiles, cannot use intermediate mip level for TileDepthInfo texture generation (todo)"
            );

            uint invalidDepthRange = (uint)Mathf.FloatToHalf(-2.0f) | (((uint)Mathf.FloatToHalf(-1.0f)) << 16);

            ref DeferredTiler tiler = ref m_Tilers[0];
            int tileXCount = tiler.TileXCount;
            int tileYCount = tiler.TileYCount;
            int tilePixelWidth = tiler.TilePixelWidth;
            int tilePixelHeight = tiler.TilePixelHeight;
            int tileMipLevel = (int)Mathf.Log(Mathf.Min(tilePixelWidth, tilePixelHeight), 2);
            int intermediateMipLevel = DeferredConfig.kTileDepthInfoIntermediateLevel >= 0 && DeferredConfig.kTileDepthInfoIntermediateLevel < tileMipLevel ? DeferredConfig.kTileDepthInfoIntermediateLevel : tileMipLevel;
            int tileShiftMipLevel = tileMipLevel - intermediateMipLevel;
            int alignment = 1 << intermediateMipLevel;
            int depthInfoWidth = (this.RenderWidth + alignment - 1) >> intermediateMipLevel;
            int depthInfoHeight = (this.RenderHeight + alignment - 1) >> intermediateMipLevel;
            NativeArray<ushort> tiles = tiler.Tiles;
            NativeArray<uint> tileHeaders = tiler.TileHeaders;

            NativeArray<uint> depthRanges = new NativeArray<uint>(m_MaxDepthRangePerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingTileDepthInfo))
            {
                RenderTargetIdentifier depthSurface = this.DepthAttachmentIdentifier;
                RenderTargetIdentifier depthInfoSurface = (tileMipLevel == intermediateMipLevel) ? this.TileDepthInfoTextureIdentifier : this.DepthInfoTextureIdentifier;

                cmd.SetGlobalTexture(ShaderConstants._DepthTex, depthSurface);
                cmd.SetGlobalVector(ShaderConstants._DepthTexSize, new Vector4(this.RenderWidth, this.RenderHeight, 1.0f / this.RenderWidth, 1.0f / this.RenderHeight));
                cmd.SetGlobalInt(ShaderConstants._DownsamplingWidth, tilePixelWidth);
                cmd.SetGlobalInt(ShaderConstants._DownsamplingHeight, tilePixelHeight);
                cmd.SetGlobalInt(ShaderConstants._SourceShiftX, intermediateMipLevel);
                cmd.SetGlobalInt(ShaderConstants._SourceShiftY, intermediateMipLevel);
                cmd.SetGlobalInt(ShaderConstants._TileShiftX, tileShiftMipLevel);
                cmd.SetGlobalInt(ShaderConstants._TileShiftY, tileShiftMipLevel);

                Matrix4x4 proj = renderingData.cameraData.camera.projectionMatrix;
                Matrix4x4 clip = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0.5f, 0), new Vector4(0, 0, 0.5f, 1));
                Matrix4x4 projScreenInv = Matrix4x4.Inverse(clip * proj);
                cmd.SetGlobalVector(ShaderConstants._unproject0, projScreenInv.GetRow(2));
                cmd.SetGlobalVector(ShaderConstants._unproject1, projScreenInv.GetRow(3));

                string shaderVariant = null;
                if (tilePixelWidth == tilePixelHeight)
                {
                    if (intermediateMipLevel == 1)
                        shaderVariant = ShaderKeywordStrings.DOWNSAMPLING_SIZE_2;
                    else if (intermediateMipLevel == 2)
                        shaderVariant = ShaderKeywordStrings.DOWNSAMPLING_SIZE_4;
                    else if (intermediateMipLevel == 3)
                        shaderVariant = ShaderKeywordStrings.DOWNSAMPLING_SIZE_8;
                    else if (intermediateMipLevel == 4)
                        shaderVariant = ShaderKeywordStrings.DOWNSAMPLING_SIZE_16;
                }

                if (shaderVariant != null)
                    cmd.EnableShaderKeyword(shaderVariant);

                int tileY = 0;
                int tileYIncrement = (DeferredConfig.UseCBufferForDepthRange ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / (tileXCount * 4);

                while (tileY < tileYCount)
                {
                    int tileYEnd = Mathf.Min(tileYCount, tileY + tileYIncrement);

                    for (int j = tileY; j < tileYEnd; ++j)
                    {
                        for (int i = 0; i < tileXCount; ++i)
                        {
                            int headerOffset = tiler.GetTileHeaderOffset(i, j);
                            int tileLightCount = (int)tileHeaders[headerOffset + 1];
                            uint listDepthRange = tileLightCount == 0 ? invalidDepthRange : tileHeaders[headerOffset + 2];
                            depthRanges[i + (j - tileY) * tileXCount] = listDepthRange;
                        }
                    }

                    ComputeBuffer _depthRanges = DeferredShaderData.instance.ReserveBuffer<uint>(m_MaxDepthRangePerBatch, DeferredConfig.UseCBufferForDepthRange);
                    _depthRanges.SetData(depthRanges, 0, 0, depthRanges.Length);

                    if (DeferredConfig.UseCBufferForDepthRange)
                        cmd.SetGlobalConstantBuffer(_depthRanges, ShaderConstants.UDepthRanges, 0, m_MaxDepthRangePerBatch * 4);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants._DepthRanges, _depthRanges);

                    cmd.SetGlobalInt(ShaderConstants._tileXCount, tileXCount);
                    cmd.SetGlobalInt(ShaderConstants._DepthRangeOffset, tileY * tileXCount);

                    cmd.EnableScissorRect(new Rect(0, tileY << tileShiftMipLevel, depthInfoWidth, (tileYEnd - tileY) << tileShiftMipLevel));
                    cmd.Blit(depthSurface, depthInfoSurface, m_TileDepthInfoMaterial, 0);

                    tileY = tileYEnd;
                }

                cmd.DisableScissorRect();

                if (shaderVariant != null)
                    cmd.DisableShaderKeyword(shaderVariant);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            depthRanges.Dispose();
        }

        internal void ExecuteDownsampleBitmaskPass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_TileDepthInfoMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDepthInfoMaterial, GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingTileDepthInfo))
            {
                RenderTargetIdentifier depthInfoSurface = this.DepthInfoTextureIdentifier;
                RenderTargetIdentifier tileDepthInfoSurface = this.TileDepthInfoTextureIdentifier;

                ref DeferredTiler tiler = ref m_Tilers[0];
                int tilePixelWidth = tiler.TilePixelWidth;
                int tilePixelHeight = tiler.TilePixelHeight;
                int tileWidthLevel = (int)Mathf.Log(tilePixelWidth, 2);
                int tileHeightLevel = (int)Mathf.Log(tilePixelHeight, 2);
                int intermediateMipLevel = DeferredConfig.kTileDepthInfoIntermediateLevel;
                int diffWidthLevel = tileWidthLevel - intermediateMipLevel;
                int diffHeightLevel = tileHeightLevel - intermediateMipLevel;

                cmd.SetGlobalTexture(ShaderConstants._BitmaskTex, depthInfoSurface);
                cmd.SetGlobalInt(ShaderConstants._DownsamplingWidth, tilePixelWidth);
                cmd.SetGlobalInt(ShaderConstants._DownsamplingHeight, tilePixelHeight);

                int alignment = 1 << DeferredConfig.kTileDepthInfoIntermediateLevel;
                int depthInfoWidth = (this.RenderWidth + alignment - 1) >> DeferredConfig.kTileDepthInfoIntermediateLevel;
                int depthInfoHeight = (this.RenderHeight + alignment - 1) >> DeferredConfig.kTileDepthInfoIntermediateLevel;
                cmd.SetGlobalVector("_BitmaskTexSize", new Vector4(depthInfoWidth, depthInfoHeight, 1.0f / depthInfoWidth, 1.0f / depthInfoHeight));

                string shaderVariant = null;
                if (diffWidthLevel == 1 && diffHeightLevel == 1)
                    shaderVariant = ShaderKeywordStrings.DOWNSAMPLING_SIZE_2;
                else if (diffWidthLevel == 2 && diffHeightLevel == 2)
                    shaderVariant = ShaderKeywordStrings.DOWNSAMPLING_SIZE_4;
                else if (diffWidthLevel == 3 && diffHeightLevel == 3)
                    shaderVariant = ShaderKeywordStrings.DOWNSAMPLING_SIZE_8;

                if (shaderVariant != null)
                    cmd.EnableShaderKeyword(shaderVariant);

                cmd.Blit(depthInfoSurface, tileDepthInfoSurface, m_TileDepthInfoMaterial, 1);

                if (shaderVariant != null)
                    cmd.DisableShaderKeyword(shaderVariant);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
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

            CommandBuffer cmd = CommandBufferPool.Get();
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

                // Stencil lights must be applied before tile light because main directional light may require to overwrite lighting buffer for SSAO.
                RenderStencilLights(context, cmd, ref renderingData);

                RenderTileLights(context, cmd, ref renderingData);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._DEFERRED_MIXED_LIGHTING, false);

                // Legacy fog (Windows -> Rendering -> Lighting Settings -> Fog)
                RenderFog(context, cmd, ref renderingData);
            }

            // Restore shader keywords
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, renderingData.shadowData.isKeywordAdditionalLightShadowsEnabled);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, renderingData.shadowData.isKeywordSoftShadowsEnabled);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightCookies, m_LightCookieManager.IsKeywordLightCookieEnabled);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
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
            uint lightLayerMask = (uint)additionalLightData.lightLayerMask;

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

        void SortLights(ref NativeArray<DeferredTiler.PrePunctualLight> prePunctualLights)
        {
            DeferredTiler.PrePunctualLight[] array = prePunctualLights.ToArray(); // TODO Use NativeArrayExtensions and avoid dynamic memory allocation.
            System.Array.Sort<DeferredTiler.PrePunctualLight>(array, new SortPrePunctualLight());
            prePunctualLights.CopyFrom(array);
        }

        bool CheckHasTileLights(ref NativeArray<VisibleLight> visibleLights)
        {
            for (int visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
            {
                if (IsTileLight(visibleLights[visLightIndex]))
                    return true;
            }

            return false;
        }

        void PrecomputeLights(out NativeArray<DeferredTiler.PrePunctualLight> prePunctualLights,
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
                prePunctualLights = new NativeArray<DeferredTiler.PrePunctualLight>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                stencilVisLights = new NativeArray<ushort>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                stencilVisLightOffsets = new NativeArray<ushort>(lightTypeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < lightTypeCount; ++i)
                    stencilVisLightOffsets[i] = k_InvalidLightOffset;
                return;
            }

            // number of supported lights rendered by the TileDeferred system, for each light type (Spot, Directional, Point, Area, Rectangle, Disc, plus one slot at the end)
            NativeArray<int> tileLightOffsets = new NativeArray<int>(lightTypeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
            NativeArray<int> tileLightCounts = new NativeArray<int>(lightTypeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
            NativeArray<int> stencilLightCounts = new NativeArray<int>(lightTypeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
            stencilVisLightOffsets = new NativeArray<ushort>(lightTypeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);

            // Count the number of lights per type.
            for (ushort visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
            {
                VisibleLight vl = visibleLights[visLightIndex];

                if (this.TiledDeferredShading && IsTileLight(vl))
                    ++tileLightOffsets[(int)vl.lightType];
                else // All remaining lights are processed as stencil volumes.
                    ++stencilVisLightOffsets[(int)vl.lightType];
            }

            int totalTileLightCount = tileLightOffsets[(int)LightType.Point] + tileLightOffsets[(int)LightType.Spot];
            int totalStencilLightCount = stencilVisLightOffsets[(int)LightType.Spot] + stencilVisLightOffsets[(int)LightType.Directional] + stencilVisLightOffsets[(int)LightType.Point];
            prePunctualLights = new NativeArray<DeferredTiler.PrePunctualLight>(totalTileLightCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            stencilVisLights = new NativeArray<ushort>(totalStencilLightCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // Calculate correct offsets now.
            for (int i = 0, toffset = 0; i < tileLightOffsets.Length; ++i)
            {
                int c = tileLightOffsets[i];
                tileLightOffsets[i] = toffset;
                toffset += c;
            }
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

            // Precompute punctual light data.
            for (ushort visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
            {
                VisibleLight vl = visibleLights[visLightIndex];

                if (this.TiledDeferredShading && IsTileLight(vl))
                {
                    DeferredTiler.PrePunctualLight ppl;
                    ppl.posVS = view.MultiplyPoint(vl.localToWorldMatrix.GetColumn(3)); // By convention, OpenGL RH coordinate space
                    ppl.radius = vl.range;
                    ppl.minDist = max(0.0f, length(ppl.posVS) - ppl.radius);

                    ppl.screenPos = new Vector2(ppl.posVS.x, ppl.posVS.y);
                    // Project on screen for perspective projections.
                    if (!isOrthographic && ppl.posVS.z <= zNear)
                        ppl.screenPos = ppl.screenPos * (-zNear / ppl.posVS.z);

                    ppl.visLightIndex = visLightIndex;

                    int i = tileLightCounts[(int)vl.lightType]++;
                    prePunctualLights[tileLightOffsets[(int)vl.lightType] + i] = ppl;
                }
                else
                {
                    // All remaining lights are processed as stencil volumes.
                    int i = stencilLightCounts[(int)vl.lightType]++;
                    stencilVisLights[stencilVisLightOffsets[(int)vl.lightType] + i] = visLightIndex;
                }
            }
            tileLightOffsets.Dispose();
            tileLightCounts.Dispose();
            stencilLightCounts.Dispose();
        }

        void RenderTileLights(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (!m_HasTileVisLights)
                return;

            if (m_TileDeferredMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDeferredMaterial, GetType().Name);
                return;
            }

            // Workaround for bug.
            // When changing the URP asset settings (ex: shadow cascade resolution), all ScriptableRenderers are recreated but
            // materials passed in have not finished initializing at that point if they have fallback shader defined. In particular deferred shaders only have 1 pass available,
            // which prevents from resolving correct pass indices.
            if (m_TileDeferredPasses[0] < 0)
                InitTileDeferredMaterial();

            Profiler.BeginSample(k_DeferredTiledPass);

            // Allow max 256 draw calls for rendering all the batches of tiles
            DrawCall[] drawCalls = new DrawCall[256];
            int drawCallCount = 0;

            {
                ref DeferredTiler tiler = ref m_Tilers[0];

                int sizeof_TileData = 16;
                int sizeof_vec4_TileData = sizeof_TileData >> 4;
                int sizeof_PunctualLightData = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualLightData));
                int sizeof_vec4_PunctualLightData = sizeof_PunctualLightData >> 4;

                int tileXCount = tiler.TileXCount;
                int tileYCount = tiler.TileYCount;
                int maxLightPerTile = tiler.MaxLightPerTile;
                NativeArray<ushort> tiles = tiler.Tiles;
                NativeArray<uint> tileHeaders = tiler.TileHeaders;

                int instanceOffset = 0;
                int tileCount = 0;
                int lightCount = 0;
                int relLightIndices = 0;

                ComputeBuffer _tileList = DeferredShaderData.instance.ReserveBuffer<TileData>(m_MaxTilesPerBatch, DeferredConfig.UseCBufferForTileList);
                ComputeBuffer _punctualLightBuffer = DeferredShaderData.instance.ReserveBuffer<PunctualLightData>(m_MaxPunctualLightPerBatch, DeferredConfig.UseCBufferForLightData);
                ComputeBuffer _relLightList = DeferredShaderData.instance.ReserveBuffer<uint>(m_MaxRelLightIndicesPerBatch, DeferredConfig.UseCBufferForLightList);

                NativeArray<uint4> tileList = new NativeArray<uint4>(m_MaxTilesPerBatch * sizeof_vec4_TileData, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<uint4> punctualLightBuffer = new NativeArray<uint4>(m_MaxPunctualLightPerBatch * sizeof_vec4_PunctualLightData, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<uint> relLightList = new NativeArray<uint>(m_MaxRelLightIndicesPerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                // Acceleration structure to quickly find if a light has already been added to the uniform block data for the current draw call.
                NativeArray<ushort> trimmedLights = new NativeArray<ushort>(maxLightPerTile, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<ushort> visLightToRelLights = new NativeArray<ushort>(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                BitArray usedLights = new BitArray(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

                for (int j = 0; j < tileYCount; ++j)
                {
                    for (int i = 0; i < tileXCount; ++i)
                    {
                        int tileOffset;
                        int tileLightCount;
                        tiler.GetTileOffsetAndCount(i, j, out tileOffset, out tileLightCount);
                        if (tileLightCount == 0) // empty tile
                            continue;

                        // Find lights that are not in the batch yet.
                        int trimmedLightCount = TrimLights(ref trimmedLights, ref tiles, tileOffset, tileLightCount, ref usedLights);
                        Assertions.Assert.IsTrue(trimmedLightCount <= maxLightPerTile); // too many lights overlaps a tile

                        // Checks whether one of the GPU buffers is reaching max capacity.
                        // In that case, the draw call must be flushed and new GPU buffer(s) be allocated.
                        bool tileListIsFull = (tileCount == m_MaxTilesPerBatch);
                        bool lightBufferIsFull = (lightCount + trimmedLightCount > m_MaxPunctualLightPerBatch);
                        bool relLightListIsFull = (relLightIndices + tileLightCount > m_MaxRelLightIndicesPerBatch);

                        if (tileListIsFull || lightBufferIsFull || relLightListIsFull)
                        {
                            drawCalls[drawCallCount++] = new DrawCall
                            {
                                tileList = _tileList,
                                punctualLightBuffer = _punctualLightBuffer,
                                relLightList = _relLightList,
                                tileListSize = tileCount * sizeof_TileData,
                                punctualLightBufferSize = lightCount * sizeof_PunctualLightData,
                                relLightListSize = Align(relLightIndices, 4) * 4,
                                instanceOffset = instanceOffset,
                                instanceCount = tileCount - instanceOffset
                            };

                            if (tileListIsFull)
                            {
                                _tileList.SetData(tileList, 0, 0, tileList.Length); // Must pass complete array (restriction for binding Unity Constant Buffers)
                                _tileList = DeferredShaderData.instance.ReserveBuffer<TileData>(m_MaxTilesPerBatch, DeferredConfig.UseCBufferForTileList);
                                tileCount = 0;
                            }

                            if (lightBufferIsFull)
                            {
                                _punctualLightBuffer.SetData(punctualLightBuffer, 0, 0, punctualLightBuffer.Length);
                                _punctualLightBuffer = DeferredShaderData.instance.ReserveBuffer<PunctualLightData>(m_MaxPunctualLightPerBatch, DeferredConfig.UseCBufferForLightData);
                                lightCount = 0;

                                // If punctualLightBuffer was reset, then all lights in the current tile must be added.
                                trimmedLightCount = tileLightCount;
                                for (int l = 0; l < tileLightCount; ++l)
                                    trimmedLights[l] = tiles[tileOffset + l];
                                usedLights.Clear();
                            }

                            if (relLightListIsFull)
                            {
                                _relLightList.SetData(relLightList, 0, 0, relLightList.Length);
                                _relLightList = DeferredShaderData.instance.ReserveBuffer<uint>(m_MaxRelLightIndicesPerBatch, DeferredConfig.UseCBufferForLightList);
                                relLightIndices = 0;
                            }

                            instanceOffset = tileCount;
                        }

                        // Add TileData.
                        int headerOffset = tiler.GetTileHeaderOffset(i, j);
                        uint listBitMask = tileHeaders[headerOffset + 3];
                        StoreTileData(ref tileList, tileCount, PackTileID((uint)i, (uint)j), listBitMask, (ushort)relLightIndices, (ushort)tileLightCount);
                        ++tileCount;

                        // Add newly discovered lights.
                        for (int l = 0; l < trimmedLightCount; ++l)
                        {
                            int visLightIndex = trimmedLights[l];
                            StorePunctualLightData(ref punctualLightBuffer, lightCount, ref renderingData.lightData.visibleLights, visLightIndex);
                            visLightToRelLights[visLightIndex] = (ushort)lightCount;
                            ++lightCount;
                            usedLights.Set(visLightIndex, true);
                        }

                        // Add light list for the tile.
                        for (int l = 0; l < tileLightCount; ++l)
                        {
                            ushort visLightIndex = tiles[tileOffset + l];
                            ushort relLightBitRange = tiles[tileOffset + tileLightCount + l];
                            ushort relLightIndex = visLightToRelLights[visLightIndex];
                            relLightList[relLightIndices++] = (uint)relLightIndex | (uint)(relLightBitRange << 16);
                        }
                    }
                }

                int instanceCount = tileCount - instanceOffset;
                if (instanceCount > 0)
                {
                    _tileList.SetData(tileList, 0, 0, tileList.Length); // Must pass complete array (restriction for binding Unity Constant Buffers)
                    _punctualLightBuffer.SetData(punctualLightBuffer, 0, 0, punctualLightBuffer.Length);
                    _relLightList.SetData(relLightList, 0, 0, relLightList.Length);

                    drawCalls[drawCallCount++] = new DrawCall
                    {
                        tileList = _tileList,
                        punctualLightBuffer = _punctualLightBuffer,
                        relLightList = _relLightList,
                        tileListSize = tileCount * sizeof_TileData,
                        punctualLightBufferSize = lightCount * sizeof_PunctualLightData,
                        relLightListSize = Align(relLightIndices, 4) * 4,
                        instanceOffset = instanceOffset,
                        instanceCount = instanceCount
                    };
                }

                tileList.Dispose();
                punctualLightBuffer.Dispose();
                relLightList.Dispose();
                trimmedLights.Dispose();
                visLightToRelLights.Dispose();
                usedLights.Dispose();
            }

            // Now draw all tile batches.
            using (new ProfilingScope(cmd, m_ProfilingSamplerDeferredTiledPass))
            {
                MeshTopology topology = DeferredConfig.kHasNativeQuadSupport ? MeshTopology.Quads : MeshTopology.Triangles;
                int vertexCount = DeferredConfig.kHasNativeQuadSupport ? 4 : 6;

                int tileWidth = m_Tilers[0].TilePixelWidth;
                int tileHeight = m_Tilers[0].TilePixelHeight;
                cmd.SetGlobalInt(ShaderConstants._TilePixelWidth, tileWidth);
                cmd.SetGlobalInt(ShaderConstants._TilePixelHeight, tileHeight);

                cmd.SetGlobalTexture(this.TileDepthInfoTexture.id, this.TileDepthInfoTextureIdentifier);

                for (int i = 0; i < drawCallCount; ++i)
                {
                    DrawCall dc = drawCalls[i];

                    if (DeferredConfig.UseCBufferForTileList)
                        cmd.SetGlobalConstantBuffer(dc.tileList, ShaderConstants.UTileList, 0, dc.tileListSize);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants._TileList, dc.tileList);

                    if (DeferredConfig.UseCBufferForLightData)
                        cmd.SetGlobalConstantBuffer(dc.punctualLightBuffer, ShaderConstants.UPunctualLightBuffer, 0, dc.punctualLightBufferSize);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants._PunctualLightBuffer, dc.punctualLightBuffer);

                    if (DeferredConfig.UseCBufferForLightList)
                        cmd.SetGlobalConstantBuffer(dc.relLightList, ShaderConstants.URelLightList, 0, dc.relLightListSize);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants._RelLightList, dc.relLightList);

                    cmd.SetGlobalInt(ShaderConstants._InstanceOffset, dc.instanceOffset);
                    cmd.DrawProcedural(Matrix4x4.identity, m_TileDeferredMaterial, m_TileDeferredPasses[(int)TileDeferredPasses.PunctualLit], topology, vertexCount, dc.instanceCount);
                    cmd.DrawProcedural(Matrix4x4.identity, m_TileDeferredMaterial, m_TileDeferredPasses[(int)TileDeferredPasses.PunctualSimpleLit], topology, vertexCount, dc.instanceCount);
                }
            }

            Profiler.EndSample();
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
                VisibleLight vl = visibleLights[visLightIndex];
                if (vl.lightType != LightType.Directional)
                    break;

                Vector4 lightDir, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
                UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, visLightIndex, out lightDir, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

                int lightFlags = 0;
                if (vl.light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
                    lightFlags |= (int)LightFlag.SubtractiveMixedLighting;

                var additionalLightData = vl.light.GetUniversalAdditionalLightData();
                uint lightLayerMask = (uint)additionalLightData.lightLayerMask;

                // Setup shadow paramters:
                // - for the main light, they have already been setup globally, so nothing to do.
                // - for other directional lights, it is actually not supported by URP, but the code would look like this.
                bool hasDeferredShadows;
                if (visLightIndex == mainLightIndex)
                {
                    hasDeferredShadows = vl.light && vl.light.shadows != LightShadows.None;
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, false);
                }
                else
                {
                    int shadowLightIndex = m_AdditionalLightsShadowCasterPass != null ? m_AdditionalLightsShadowCasterPass.GetShadowLightIndexFromLightIndex(visLightIndex) : -1;
                    hasDeferredShadows = vl.light && vl.light.shadows != LightShadows.None && shadowLightIndex >= 0;
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, hasDeferredShadows);

                    cmd.SetGlobalInt(ShaderConstants._ShadowLightIndex, shadowLightIndex);
                }

                bool hasSoftShadow = hasDeferredShadows && renderingData.shadowData.supportsSoftShadows && vl.light.shadows == LightShadows.Soft;
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
                VisibleLight vl = visibleLights[visLightIndex];
                if (vl.lightType != LightType.Point)
                    break;

                Vector3 posWS = vl.localToWorldMatrix.GetColumn(3);

                Matrix4x4 transformMatrix = new Matrix4x4(
                    new Vector4(vl.range, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, vl.range, 0.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, vl.range, 0.0f),
                    new Vector4(posWS.x, posWS.y, posWS.z, 1.0f)
                );

                Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
                UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, visLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

                var additionalLightData = vl.light.GetUniversalAdditionalLightData();
                uint lightLayerMask = (uint)additionalLightData.lightLayerMask;

                int lightFlags = 0;
                if (vl.light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
                    lightFlags |= (int)LightFlag.SubtractiveMixedLighting;

                int shadowLightIndex = m_AdditionalLightsShadowCasterPass != null ? m_AdditionalLightsShadowCasterPass.GetShadowLightIndexFromLightIndex(visLightIndex) : -1;
                bool hasDeferredLightShadows = vl.light && vl.light.shadows != LightShadows.None && shadowLightIndex >= 0;
                bool hasSoftShadow = hasDeferredLightShadows && renderingData.shadowData.supportsSoftShadows && vl.light.shadows == LightShadows.Soft;

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, hasDeferredLightShadows);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, hasSoftShadow);

                int cookieLightIndex = m_LightCookieManager.GetLightCookieShaderDataIndex(visLightIndex);
                // We could test this in shader (static if) a variant (shader change) is undesirable. Same for spot light.
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightCookies, cookieLightIndex >= 0);

                cmd.SetGlobalVector(ShaderConstants._LightPosWS, lightPos);
                cmd.SetGlobalVector(ShaderConstants._LightColor, lightColor);
                cmd.SetGlobalVector(ShaderConstants._LightAttenuation, lightAttenuation);
                cmd.SetGlobalVector(ShaderConstants._LightOcclusionProbInfo, lightOcclusionChannel);
                cmd.SetGlobalInt(ShaderConstants._LightFlags, lightFlags);
                cmd.SetGlobalInt(ShaderConstants._ShadowLightIndex, shadowLightIndex);
                cmd.SetGlobalInt(ShaderConstants._LightLayerMask, (int)lightLayerMask);
                cmd.SetGlobalInt(ShaderConstants._CookieLightIndex, cookieLightIndex);

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
                VisibleLight vl = visibleLights[visLightIndex];
                if (vl.lightType != LightType.Spot)
                    break;

                float alpha = Mathf.Deg2Rad * vl.spotAngle * 0.5f;
                float cosAlpha = Mathf.Cos(alpha);
                float sinAlpha = Mathf.Sin(alpha);
                // Artificially inflate the geometric shape to fit the analytic spot shape.
                // The tighter the spot shape, the lesser inflation is needed.
                float guard = Mathf.Lerp(1.0f, kStencilShapeGuard, sinAlpha);

                Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
                UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, visLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

                var additionalLightData = vl.light.GetUniversalAdditionalLightData();
                uint lightLayerMask = (uint)additionalLightData.lightLayerMask;

                int lightFlags = 0;
                if (vl.light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
                    lightFlags |= (int)LightFlag.SubtractiveMixedLighting;

                int shadowLightIndex = m_AdditionalLightsShadowCasterPass != null ? m_AdditionalLightsShadowCasterPass.GetShadowLightIndexFromLightIndex(visLightIndex) : -1;
                bool hasDeferredLightShadows = vl.light && vl.light.shadows != LightShadows.None && shadowLightIndex >= 0;
                bool hasSoftShadow = hasDeferredLightShadows && renderingData.shadowData.supportsSoftShadows && vl.light.shadows == LightShadows.Soft;

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, hasDeferredLightShadows);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, hasSoftShadow);

                int cookieLightIndex = m_LightCookieManager.GetLightCookieShaderDataIndex(visLightIndex);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightCookies, cookieLightIndex >= 0);

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
                cmd.SetGlobalInt(ShaderConstants._CookieLightIndex, cookieLightIndex);


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

        int TrimLights(ref NativeArray<ushort> trimmedLights, ref NativeArray<ushort> tiles, int offset, int lightCount, ref BitArray usedLights)
        {
            int trimCount = 0;
            for (int i = 0; i < lightCount; ++i)
            {
                ushort visLightIndex = tiles[offset + i];
                if (usedLights.IsSet(visLightIndex))
                    continue;
                trimmedLights[trimCount++] = visLightIndex;
            }
            return trimCount;
        }

        void StorePunctualLightData(ref NativeArray<uint4> punctualLightBuffer, int storeIndex, ref NativeArray<VisibleLight> visibleLights, int index)
        {
            int lightFlags = 0;
            if (visibleLights[index].light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
                lightFlags |= (int)LightFlag.SubtractiveMixedLighting;

            // tile lights do not support shadows, so shadowLightIndex is -1.
            //int shadowLightIndex = -1;

            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
            UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, index, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

            var additionalLightData = visibleLights[index].light.GetUniversalAdditionalLightData();
            uint lightLayerMask = (uint)additionalLightData.lightLayerMask;

            punctualLightBuffer[storeIndex * 6 + 0] = new uint4(FloatToUInt(lightPos.x), FloatToUInt(lightPos.y), FloatToUInt(lightPos.z), FloatToUInt(visibleLights[index].range * visibleLights[index].range));
            punctualLightBuffer[storeIndex * 6 + 1] = new uint4(FloatToUInt(lightColor.x), FloatToUInt(lightColor.y), FloatToUInt(lightColor.z), 0);
            punctualLightBuffer[storeIndex * 6 + 2] = new uint4(FloatToUInt(lightAttenuation.x), FloatToUInt(lightAttenuation.y), FloatToUInt(lightAttenuation.z), FloatToUInt(lightAttenuation.w));
            punctualLightBuffer[storeIndex * 6 + 3] = new uint4(FloatToUInt(lightSpotDir.x), FloatToUInt(lightSpotDir.y), FloatToUInt(lightSpotDir.z), (uint)lightFlags);
            punctualLightBuffer[storeIndex * 6 + 4] = new uint4(FloatToUInt(lightOcclusionChannel.x), FloatToUInt(lightOcclusionChannel.y), FloatToUInt(lightOcclusionChannel.z), FloatToUInt(lightOcclusionChannel.w));
            punctualLightBuffer[storeIndex * 6 + 5] = new uint4(lightLayerMask, 0, 0, 0);
        }

        void StoreTileData(ref NativeArray<uint4> tileList, int storeIndex, uint tileID, uint listBitMask, ushort relLightOffset, ushort lightCount)
        {
            // See struct TileData in TileDeferred.shader.
            tileList[storeIndex] = new uint4 { x = tileID, y = listBitMask, z = relLightOffset | ((uint)lightCount << 16), w = 0 };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsTileLight(VisibleLight visibleLight)
        {
            // tileDeferred might render a lot of point lights in the same draw call.
            // point light shadows require generating cube shadow maps in real-time, requiring extra CPU/GPU resources ; which can become expensive quickly
            return (visibleLight.lightType == LightType.Point && (visibleLight.light == null || visibleLight.light.shadows == LightShadows.None))
                || (visibleLight.lightType == LightType.Spot && (visibleLight.light == null || visibleLight.light.shadows == LightShadows.None));
        }

        void InitTileDeferredMaterial()
        {
            if (m_TileDeferredMaterial == null)
                return;

            for (int pass = 0; pass < k_TileDeferredPassNames.Length; ++pass)
                m_TileDeferredPasses[pass] = m_TileDeferredMaterial.FindPass(k_TileDeferredPassNames[pass]);

            m_TileDeferredMaterial.SetFloat(ShaderConstants._LitStencilRef, (float)StencilUsage.MaterialLit);
            m_TileDeferredMaterial.SetFloat(ShaderConstants._LitStencilReadMask, (float)StencilUsage.MaterialMask);
            m_TileDeferredMaterial.SetFloat(ShaderConstants._LitStencilWriteMask, 0.0f);
            m_TileDeferredMaterial.SetFloat(ShaderConstants._SimpleLitStencilRef, (float)StencilUsage.MaterialSimpleLit);
            m_TileDeferredMaterial.SetFloat(ShaderConstants._SimpleLitStencilReadMask, (float)StencilUsage.MaterialMask);
            m_TileDeferredMaterial.SetFloat(ShaderConstants._SimpleLitStencilWriteMask, 0.0f);
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

        static int Align(int s, int alignment)
        {
            return ((s + alignment - 1) / alignment) * alignment;
        }

        // Keep in sync with UnpackTileID().
        static uint PackTileID(uint i, uint j)
        {
            return i | (j << 16);
        }

        static uint FloatToUInt(float val)
        {
            // TODO different order for little-endian and big-endian platforms.
            byte[] bytes = System.BitConverter.GetBytes(val);
            return bytes[0] | (((uint)bytes[1]) << 8) | (((uint)bytes[2]) << 16) | (((uint)bytes[3]) << 24);
            //return bytes[3] | (((uint)bytes[2]) << 8) | (((uint)bytes[1]) << 16) | (((uint)bytes[0]) << 24);
        }

        static uint Half2ToUInt(float x, float y)
        {
            uint hx = Mathf.FloatToHalf(x);
            uint hy = Mathf.FloatToHalf(y);
            return hx | (hy << 16);
        }
    }

    class SortPrePunctualLight : System.Collections.Generic.IComparer<DeferredTiler.PrePunctualLight>
    {
        public int Compare(DeferredTiler.PrePunctualLight a, DeferredTiler.PrePunctualLight b)
        {
            if (a.minDist < b.minDist)
                return -1;
            else if (a.minDist > b.minDist)
                return 1;
            else
                return 0;
        }
    }

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
}
