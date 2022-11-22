using System;
using System.Collections.Generic;
using Unity.Collections;

using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;

namespace UnityEngine.Rendering.Universal
{
    public enum MixedLightingSetup
    {
        None,
        ShadowMask,
        Subtractive,
    };

    /// <summary>
    /// Enumeration that indicates what kind of image scaling is occurring if any
    /// </summary>
    internal enum ImageScalingMode
    {
        /// No scaling
        None,

        /// Upscaling to a larger image
        Upscaling,

        /// Downscaling to a smaller image
        Downscaling
    }

    /// <summary>
    /// Enumeration that indicates what kind of upscaling filter is being used
    /// </summary>
    internal enum ImageUpscalingFilter
    {
        /// Bilinear filtering
        Linear,

        /// Nearest-Neighbor filtering
        Point,

        /// FidelityFX Super Resolution
        FSR
    }

    public struct RenderingData
    {
        public CullingResults cullResults;
        public CameraData cameraData;
        public LightData lightData;
        public ShadowData shadowData;
        public PostProcessingData postProcessingData;
        public bool supportsDynamicBatching;
        public PerObjectData perObjectData;

        /// <summary>
        /// True if post-processing effect is enabled while rendering the camera stack.
        /// </summary>
        public bool postProcessingEnabled;
    }

    public struct LightData
    {
        public int mainLightIndex;
        public int additionalLightsCount;
        public int maxPerObjectAdditionalLightsCount;
        public NativeArray<VisibleLight> visibleLights;
        internal NativeArray<int> originalIndices;
        public bool shadeAdditionalLightsPerVertex;
        public bool supportsMixedLighting;
        public bool reflectionProbeBoxProjection;
        public bool reflectionProbeBlending;
        public bool supportsLightLayers;

        /// <summary>
        /// True if additional lights enabled.
        /// </summary>
        public bool supportsAdditionalLights;
    }

    public struct CameraData
    {
        // Internal camera data as we are not yet sure how to expose View in stereo context.
        // We might change this API soon.
        Matrix4x4 m_ViewMatrix;
        Matrix4x4 m_ProjectionMatrix;

        internal void SetViewAndProjectionMatrix(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            m_ViewMatrix = viewMatrix;
            m_ProjectionMatrix = projectionMatrix;
        }

        /// <summary>
        /// Returns the camera view matrix.
        /// </summary>
        /// <returns></returns>
        public Matrix4x4 GetViewMatrix(int viewIndex = 0)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
                return xr.GetViewMatrix(viewIndex);
#endif
            return m_ViewMatrix;
        }

        /// <summary>
        /// Returns the camera projection matrix.
        /// </summary>
        /// <returns></returns>
        public Matrix4x4 GetProjectionMatrix(int viewIndex = 0)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
                return xr.GetProjMatrix(viewIndex);
#endif
            return m_ProjectionMatrix;
        }

        /// <summary>
        /// Returns the camera GPU projection matrix. This contains platform specific changes to handle y-flip and reverse z.
        /// Similar to <c>GL.GetGPUProjectionMatrix</c> but queries URP internal state to know if the pipeline is rendering to render texture.
        /// For more info on platform differences regarding camera projection check: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
        /// </summary>
        /// <seealso cref="GL.GetGPUProjectionMatrix(Matrix4x4, bool)"/>
        /// <returns></returns>
        public Matrix4x4 GetGPUProjectionMatrix(int viewIndex = 0)
        {
            return GL.GetGPUProjectionMatrix(GetProjectionMatrix(viewIndex), IsCameraProjectionMatrixFlipped());
        }

        public Camera camera;
        public CameraRenderType renderType;
        public RenderTexture targetTexture;
        public RenderTextureDescriptor cameraTargetDescriptor;
        internal Rect pixelRect;
        internal int pixelWidth;
        internal int pixelHeight;
        internal float aspectRatio;
        public float renderScale;
        internal ImageScalingMode imageScalingMode;
        internal ImageUpscalingFilter upscalingFilter;
        internal bool fsrOverrideSharpness;
        internal float fsrSharpness;
        public bool clearDepth;
        public CameraType cameraType;
        public bool isDefaultViewport;
        public bool isHdrEnabled;
        public bool requiresDepthTexture;
        public bool requiresOpaqueTexture;

        /// <summary>
        /// Returns true if post processing passes require depth texture.
        /// </summary>
        public bool postProcessingRequiresDepthTexture;

#if ENABLE_VR && ENABLE_XR_MODULE
        public bool xrRendering;
#endif
        internal bool requireSrgbConversion
        {
            get
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                if (xr.enabled)
                    return !xr.renderTargetDesc.sRGB && (QualitySettings.activeColorSpace == ColorSpace.Linear);
#endif

                return targetTexture == null && Display.main.requiresSrgbBlitToBackbuffer;
            }
        }

        /// <summary>
        /// True if the camera rendering is for the scene window in the editor
        /// </summary>
        public bool isSceneViewCamera => cameraType == CameraType.SceneView;

        /// <summary>
        /// True if the camera rendering is for the preview window in the editor
        /// </summary>
        public bool isPreviewCamera => cameraType == CameraType.Preview;

        internal bool isRenderPassSupportedCamera => (cameraType == CameraType.Game || cameraType == CameraType.Reflection);

        /// <summary>
        /// True if the camera device projection matrix is flipped. This happens when the pipeline is rendering
        /// to a render texture in non OpenGL platforms. If you are doing a custom Blit pass to copy camera textures
        /// (_CameraColorTexture, _CameraDepthAttachment) you need to check this flag to know if you should flip the
        /// matrix when rendering with for cmd.Draw* and reading from camera textures.
        /// </summary>
        public bool IsCameraProjectionMatrixFlipped()
        {
            // Users only have access to CameraData on URP rendering scope. The current renderer should never be null.
            var renderer = ScriptableRenderer.current;
            Debug.Assert(renderer != null, "IsCameraProjectionMatrixFlipped is being called outside camera rendering scope.");

            if (renderer != null)
            {
                bool renderingToBackBufferTarget = renderer.cameraColorTarget == BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (xr.enabled)
                    renderingToBackBufferTarget |= renderer.cameraColorTarget == xr.renderTarget && !xr.renderTargetIsRenderTexture;
#endif
                bool renderingToTexture = !renderingToBackBufferTarget || targetTexture != null;
                return SystemInfo.graphicsUVStartsAtTop && renderingToTexture;
            }

            return true;
        }

        public SortingCriteria defaultOpaqueSortFlags;

        internal XRPass xr;

        [Obsolete("Please use xr.enabled instead.")]
        public bool isStereoEnabled;

        public float maxShadowDistance;
        public bool postProcessEnabled;

        public IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> captureActions;

        public LayerMask volumeLayerMask;
        public Transform volumeTrigger;

        public bool isStopNaNEnabled;
        public bool isDitheringEnabled;
        public AntialiasingMode antialiasing;
        public AntialiasingQuality antialiasingQuality;

        /// <summary>
        /// Returns the current renderer used by this camera.
        /// <see cref="ScriptableRenderer"/>
        /// </summary>
        public ScriptableRenderer renderer;

        /// <summary>
        /// True if this camera is resolving rendering to the final camera render target.
        /// When rendering a stack of cameras only the last camera in the stack will resolve to camera target.
        /// </summary>
        public bool resolveFinalTarget;

        /// <summary>
        /// Camera position in world space.
        /// </summary>
        public Vector3 worldSpaceCameraPos;
    }

    public struct ShadowData
    {
        public bool supportsMainLightShadows;
        [Obsolete("Obsolete, this feature was replaced by new 'ScreenSpaceShadows' renderer feature")]
        public bool requiresScreenSpaceShadowResolve;
        public int mainLightShadowmapWidth;
        public int mainLightShadowmapHeight;
        public int mainLightShadowCascadesCount;
        public Vector3 mainLightShadowCascadesSplit;
        /// <summary>
        /// Main light last cascade shadow fade border.
        /// Value represents the width of shadow fade that ranges from 0 to 1.
        /// Where value 0 is used for no shadow fade.
        /// </summary>
        public float mainLightShadowCascadeBorder;
        public bool supportsAdditionalLightShadows;
        public int additionalLightsShadowmapWidth;
        public int additionalLightsShadowmapHeight;
        public bool supportsSoftShadows;
        public int shadowmapDepthBufferBits;
        public List<Vector4> bias;
        public List<int> resolution;

        internal bool isKeywordAdditionalLightShadowsEnabled;
        internal bool isKeywordSoftShadowsEnabled;
    }

    // Precomputed tile data.
    public struct PreTile
    {
        // Tile left, right, bottom and top plane equations in view space.
        // Normals are pointing out.
        public Unity.Mathematics.float4 planeLeft;
        public Unity.Mathematics.float4 planeRight;
        public Unity.Mathematics.float4 planeBottom;
        public Unity.Mathematics.float4 planeTop;
    }

    // Actual tile data passed to the deferred shaders.
    public struct TileData
    {
        public uint tileID;         // 2x 16 bits
        public uint listBitMask;    // 32 bits
        public uint relLightOffset; // 16 bits is enough
        public uint unused;
    }

    // Actual point/spot light data passed to the deferred shaders.
    public struct PunctualLightData
    {
        public Vector3 wsPos;
        public float radius; // TODO remove? included in attenuation
        public Vector4 color;
        public Vector4 attenuation; // .xy are used by DistanceAttenuation - .zw are used by AngleAttenuation (for SpotLights)
        public Vector3 spotDirection;   // for spotLights
        public int flags;
        public Vector4 occlusionProbeInfo;
        public uint layerMask;
    }

    internal static class ShaderPropertyId
    {
        public static readonly int glossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
        public static readonly int subtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

        public static readonly int glossyEnvironmentCubeMap = Shader.PropertyToID("_GlossyEnvironmentCubeMap");
        public static readonly int glossyEnvironmentCubeMapHDR = Shader.PropertyToID("_GlossyEnvironmentCubeMap_HDR");

        public static readonly int ambientSkyColor = Shader.PropertyToID("unity_AmbientSky");
        public static readonly int ambientEquatorColor = Shader.PropertyToID("unity_AmbientEquator");
        public static readonly int ambientGroundColor = Shader.PropertyToID("unity_AmbientGround");

        public static readonly int time = Shader.PropertyToID("_Time");
        public static readonly int sinTime = Shader.PropertyToID("_SinTime");
        public static readonly int cosTime = Shader.PropertyToID("_CosTime");
        public static readonly int deltaTime = Shader.PropertyToID("unity_DeltaTime");
        public static readonly int timeParameters = Shader.PropertyToID("_TimeParameters");

        public static readonly int scaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
        public static readonly int worldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
        public static readonly int screenParams = Shader.PropertyToID("_ScreenParams");
        public static readonly int projectionParams = Shader.PropertyToID("_ProjectionParams");
        public static readonly int zBufferParams = Shader.PropertyToID("_ZBufferParams");
        public static readonly int orthoParams = Shader.PropertyToID("unity_OrthoParams");
        public static readonly int globalMipBias = Shader.PropertyToID("_GlobalMipBias");

        public static readonly int screenSize = Shader.PropertyToID("_ScreenSize");

        public static readonly int viewMatrix = Shader.PropertyToID("unity_MatrixV");
        public static readonly int projectionMatrix = Shader.PropertyToID("glstate_matrix_projection");
        public static readonly int viewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixVP");

        public static readonly int inverseViewMatrix = Shader.PropertyToID("unity_MatrixInvV");
        public static readonly int inverseProjectionMatrix = Shader.PropertyToID("unity_MatrixInvP");
        public static readonly int inverseViewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixInvVP");

        public static readonly int cameraProjectionMatrix = Shader.PropertyToID("unity_CameraProjection");
        public static readonly int inverseCameraProjectionMatrix = Shader.PropertyToID("unity_CameraInvProjection");
        public static readonly int worldToCameraMatrix = Shader.PropertyToID("unity_WorldToCamera");
        public static readonly int cameraToWorldMatrix = Shader.PropertyToID("unity_CameraToWorld");

        public static readonly int cameraWorldClipPlanes = Shader.PropertyToID("unity_CameraWorldClipPlanes");

        public static readonly int billboardNormal = Shader.PropertyToID("unity_BillboardNormal");
        public static readonly int billboardTangent = Shader.PropertyToID("unity_BillboardTangent");
        public static readonly int billboardCameraParams = Shader.PropertyToID("unity_BillboardCameraParams");

        public static readonly int sourceTex = Shader.PropertyToID("_SourceTex");
        public static readonly int scaleBias = Shader.PropertyToID("_ScaleBias");
        public static readonly int scaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");

        // Required for 2D Unlit Shadergraph master node as it doesn't currently support hidden properties.
        public static readonly int rendererColor = Shader.PropertyToID("_RendererColor");
    }

    public struct PostProcessingData
    {
        public ColorGradingMode gradingMode;
        public int lutSize;
        /// <summary>
        /// True if fast approximation functions are used when converting between the sRGB and Linear color spaces, false otherwise.
        /// </summary>
        public bool useFastSRGBLinearConversion;
    }

    public static class ShaderKeywordStrings
    {
        public const string MainLightShadows = "_MAIN_LIGHT_SHADOWS";
        public const string MainLightShadowCascades = "_MAIN_LIGHT_SHADOWS_CASCADE";
        public const string MainLightShadowScreen = "_MAIN_LIGHT_SHADOWS_SCREEN";
        public const string CastingPunctualLightShadow = "_CASTING_PUNCTUAL_LIGHT_SHADOW"; // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
        public const string AdditionalLightsVertex = "_ADDITIONAL_LIGHTS_VERTEX";
        public const string AdditionalLightsPixel = "_ADDITIONAL_LIGHTS";
        internal const string ClusteredRendering = "_CLUSTERED_RENDERING";
        public const string AdditionalLightShadows = "_ADDITIONAL_LIGHT_SHADOWS";
        public const string ReflectionProbeBoxProjection = "_REFLECTION_PROBE_BOX_PROJECTION";
        public const string ReflectionProbeBlending = "_REFLECTION_PROBE_BLENDING";
        public const string SoftShadows = "_SHADOWS_SOFT";
        public const string MixedLightingSubtractive = "_MIXED_LIGHTING_SUBTRACTIVE"; // Backward compatibility
        public const string LightmapShadowMixing = "LIGHTMAP_SHADOW_MIXING";
        public const string ShadowsShadowMask = "SHADOWS_SHADOWMASK";
        public const string LightLayers = "_LIGHT_LAYERS";
        public const string RenderPassEnabled = "_RENDER_PASS_ENABLED";
        public const string BillboardFaceCameraPos = "BILLBOARD_FACE_CAMERA_POS";
        public const string LightCookies = "_LIGHT_COOKIES";

        public const string DepthNoMsaa = "_DEPTH_NO_MSAA";
        public const string DepthMsaa2 = "_DEPTH_MSAA_2";
        public const string DepthMsaa4 = "_DEPTH_MSAA_4";
        public const string DepthMsaa8 = "_DEPTH_MSAA_8";

        public const string LinearToSRGBConversion = "_LINEAR_TO_SRGB_CONVERSION";
        internal const string UseFastSRGBLinearConversion = "_USE_FAST_SRGB_LINEAR_CONVERSION";

        public const string DBufferMRT1 = "_DBUFFER_MRT1";
        public const string DBufferMRT2 = "_DBUFFER_MRT2";
        public const string DBufferMRT3 = "_DBUFFER_MRT3";
        public const string DecalNormalBlendLow = "_DECAL_NORMAL_BLEND_LOW";
        public const string DecalNormalBlendMedium = "_DECAL_NORMAL_BLEND_MEDIUM";
        public const string DecalNormalBlendHigh = "_DECAL_NORMAL_BLEND_HIGH";

        public const string SmaaLow = "_SMAA_PRESET_LOW";
        public const string SmaaMedium = "_SMAA_PRESET_MEDIUM";
        public const string SmaaHigh = "_SMAA_PRESET_HIGH";
        public const string PaniniGeneric = "_GENERIC";
        public const string PaniniUnitDistance = "_UNIT_DISTANCE";
        public const string BloomLQ = "_BLOOM_LQ";
        public const string BloomHQ = "_BLOOM_HQ";
        public const string BloomLQDirt = "_BLOOM_LQ_DIRT";
        public const string BloomHQDirt = "_BLOOM_HQ_DIRT";
        public const string UseRGBM = "_USE_RGBM";
        public const string Distortion = "_DISTORTION";
        public const string ChromaticAberration = "_CHROMATIC_ABERRATION";
        public const string HDRGrading = "_HDR_GRADING";
        public const string TonemapACES = "_TONEMAP_ACES";
        public const string TonemapNeutral = "_TONEMAP_NEUTRAL";
        public const string FilmGrain = "_FILM_GRAIN";
        public const string Fxaa = "_FXAA";
        public const string Dithering = "_DITHERING";
        public const string ScreenSpaceOcclusion = "_SCREEN_SPACE_OCCLUSION";
        public const string PointSampling = "_POINT_SAMPLING";
        public const string Rcas = "_RCAS";
        public const string Gamma20 = "_GAMMA_20";

        public const string HighQualitySampling = "_HIGH_QUALITY_SAMPLING";

        public const string DOWNSAMPLING_SIZE_2 = "DOWNSAMPLING_SIZE_2";
        public const string DOWNSAMPLING_SIZE_4 = "DOWNSAMPLING_SIZE_4";
        public const string DOWNSAMPLING_SIZE_8 = "DOWNSAMPLING_SIZE_8";
        public const string DOWNSAMPLING_SIZE_16 = "DOWNSAMPLING_SIZE_16";
        public const string _SPOT = "_SPOT";
        public const string _DIRECTIONAL = "_DIRECTIONAL";
        public const string _POINT = "_POINT";
        public const string _DEFERRED_STENCIL = "_DEFERRED_STENCIL";
        public const string _DEFERRED_FIRST_LIGHT = "_DEFERRED_FIRST_LIGHT";
        public const string _DEFERRED_MAIN_LIGHT = "_DEFERRED_MAIN_LIGHT";
        public const string _GBUFFER_NORMALS_OCT = "_GBUFFER_NORMALS_OCT";
        public const string _DEFERRED_MIXED_LIGHTING = "_DEFERRED_MIXED_LIGHTING";
        public const string LIGHTMAP_ON = "LIGHTMAP_ON";
        public const string DYNAMICLIGHTMAP_ON = "DYNAMICLIGHTMAP_ON";
        public const string _ALPHATEST_ON = "_ALPHATEST_ON";
        public const string DIRLIGHTMAP_COMBINED = "DIRLIGHTMAP_COMBINED";
        public const string _DETAIL_MULX2 = "_DETAIL_MULX2";
        public const string _DETAIL_SCALED = "_DETAIL_SCALED";
        public const string _CLEARCOAT = "_CLEARCOAT";
        public const string _CLEARCOATMAP = "_CLEARCOATMAP";
        public const string DEBUG_DISPLAY = "DEBUG_DISPLAY";

        public const string _EMISSION = "_EMISSION";
        public const string _RECEIVE_SHADOWS_OFF = "_RECEIVE_SHADOWS_OFF";
        public const string _SURFACE_TYPE_TRANSPARENT = "_SURFACE_TYPE_TRANSPARENT";
        public const string _ALPHAPREMULTIPLY_ON = "_ALPHAPREMULTIPLY_ON";
        public const string _ALPHAMODULATE_ON = "_ALPHAMODULATE_ON";
        public const string _NORMALMAP = "_NORMALMAP";

        public const string EDITOR_VISUALIZATION = "EDITOR_VISUALIZATION";

        // XR
        public const string UseDrawProcedural = "_USE_DRAW_PROCEDURAL";
    }

    public sealed partial class UniversalRenderPipeline
    {
        // Holds light direction for directional lights or position for punctual lights.
        // When w is set to 1.0, it means it's a punctual light.
        static Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        static Vector4 k_DefaultLightColor = Color.black;

        // Default light attenuation is setup in a particular way that it causes
        // directional lights to return 1.0 for both distance and angle attenuation
        static Vector4 k_DefaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        static Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        static Vector4 k_DefaultLightsProbeChannel = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

        static List<Vector4> m_ShadowBiasData = new List<Vector4>();
        static List<int> m_ShadowResolutionData = new List<int>();

        /// <summary>
        /// Checks if a camera is a game camera.
        /// </summary>
        /// <param name="camera">Camera to check state from.</param>
        /// <returns>true if given camera is a game camera, false otherwise.</returns>
        public static bool IsGameCamera(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

            return camera.cameraType == CameraType.Game || camera.cameraType == CameraType.VR;
        }

        /// <summary>
        /// Checks if a camera is rendering in stereo mode.
        /// </summary>
        /// <param name="camera">Camera to check state from.</param>
        /// <returns>Returns true if the given camera is rendering in stereo mode, false otherwise.</returns>
        [Obsolete("Please use CameraData.xr.enabled instead.")]
        public static bool IsStereoEnabled(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

            return IsGameCamera(camera) && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
        }

        /// <summary>
        /// Returns the current render pipeline asset for the current quality setting.
        /// If no render pipeline asset is assigned in QualitySettings, then returns the one assigned in GraphicsSettings.
        /// </summary>
        public static UniversalRenderPipelineAsset asset
        {
            get => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        }

        /// <summary>
        /// Checks if a camera is rendering in MultiPass stereo mode.
        /// </summary>
        /// <param name="camera">Camera to check state from.</param>
        /// <returns>Returns true if the given camera is rendering in multi pass stereo mode, false otherwise.</returns>
        [Obsolete("Please use CameraData.xr.singlePassEnabled instead.")]
        static bool IsMultiPassStereoEnabled(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

            return false;
        }

        Comparison<Camera> cameraComparison = (camera1, camera2) => { return (int)camera1.depth - (int)camera2.depth; };
#if UNITY_2021_1_OR_NEWER
        void SortCameras(List<Camera> cameras)
        {
            if (cameras.Count > 1)
                cameras.Sort(cameraComparison);
        }

#else
        void SortCameras(Camera[] cameras)
        {
            if (cameras.Length > 1)
                Array.Sort(cameras, cameraComparison);
        }

#endif

        static GraphicsFormat MakeRenderTextureGraphicsFormat(bool isHdrEnabled, bool needsAlpha)
        {
            if (isHdrEnabled)
            {
                if (!needsAlpha && RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
                    return GraphicsFormat.B10G11R11_UFloatPack32;
                if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Linear | FormatUsage.Render))
                    return GraphicsFormat.R16G16B16A16_SFloat;
                return SystemInfo.GetGraphicsFormat(DefaultFormat.HDR); // This might actually be a LDR format on old devices.
            }

            return SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
        }

        // Returns a UNORM based render texture format
        // When supported by the device, this function will prefer formats with higher precision, but the same bit-depth
        // NOTE: This function does not guarantee that the returned format will contain an alpha channel.
        internal static GraphicsFormat MakeUnormRenderTextureGraphicsFormat()
        {
            if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.A2B10G10R10_UNormPack32, FormatUsage.Linear | FormatUsage.Render))
                return GraphicsFormat.A2B10G10R10_UNormPack32;
            else
                return GraphicsFormat.R8G8B8A8_UNorm;
        }

        static RenderTextureDescriptor CreateRenderTextureDescriptor(Camera camera, float renderScale,
            bool isHdrEnabled, int msaaSamples, bool needsAlpha, bool requiresOpaqueTexture)
        {
            RenderTextureDescriptor desc;

            if (camera.targetTexture == null)
            {
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                desc.width = (int)((float)desc.width * renderScale);
                desc.height = (int)((float)desc.height * renderScale);
                desc.graphicsFormat = MakeRenderTextureGraphicsFormat(isHdrEnabled, needsAlpha);
                desc.depthBufferBits = 32;
                desc.msaaSamples = msaaSamples;
                desc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            }
            else
            {
                desc = camera.targetTexture.descriptor;
                desc.width = camera.pixelWidth;
                desc.height = camera.pixelHeight;
                if (camera.cameraType == CameraType.SceneView && !isHdrEnabled)
                {
                    desc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                }
                // SystemInfo.SupportsRenderTextureFormat(camera.targetTexture.descriptor.colorFormat)
                // will assert on R8_SINT since it isn't a valid value of RenderTextureFormat.
                // If this is fixed then we can implement debug statement to the user explaining why some
                // RenderTextureFormats available resolves in a black render texture when no warning or error
                // is given.
            }

            // Make sure dimension is non zero
            desc.width = Mathf.Max(1, desc.width);
            desc.height = Mathf.Max(1, desc.height);

            desc.enableRandomWrite = false;
            desc.bindMS = false;
            desc.useDynamicScale = camera.allowDynamicResolution;

            // The way RenderTextures handle MSAA fallback when an unsupported sample count of 2 is requested (falling back to numSamples = 1), differs fom the way
            // the fallback is handled when setting up the Vulkan swapchain (rounding up numSamples to 4, if supported). This caused an issue on Mali GPUs which don't support
            // 2x MSAA.
            // The following code makes sure that on Vulkan the MSAA unsupported fallback behaviour is consistent between RenderTextures and Swapchain.
            // TODO: we should review how all backends handle MSAA fallbacks and move these implementation details in engine code.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                // if the requested number of samples is 2, and the supported value is 1x, it means that 2x is unsupported on this GPU.
                // Then we bump up the requested value to 4.
                if (desc.msaaSamples == 2 && SystemInfo.GetRenderTextureSupportedMSAASampleCount(desc) == 1)
                    desc.msaaSamples = 4;
            }

            // check that the requested MSAA samples count is supported by the current platform. If it's not supported,
            // replace the requested desc.msaaSamples value with the actual value the engine falls back to
            desc.msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(desc);

            // if the target platform doesn't support storing multisampled RTs and we are doing a separate opaque pass, using a Load load action on the subsequent passes
            // will result in loading Resolved data, which on some platforms is discarded, resulting in losing the results of the previous passes.
            // As a workaround we disable MSAA to make sure that the results of previous passes are stored. (fix for Case 1247423).
            if (!SystemInfo.supportsStoreAndResolveAction && requiresOpaqueTexture)
                desc.msaaSamples = 1;

            return desc;
        }

        private static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
        {
            LightDataGI lightData = new LightDataGI();
#if UNITY_EDITOR
            // Always extract lights in the Editor.
            for (int i = 0; i < requests.Length; i++)
            {
                Light light = requests[i];
                var additionalLightData = light.GetUniversalAdditionalLightData();

                LightmapperUtils.Extract(light, out Cookie cookie);

                switch (light.type)
                {
                    case LightType.Directional:
                        DirectionalLight directionalLight = new DirectionalLight();
                        LightmapperUtils.Extract(light, ref directionalLight);

                        if (light.cookie != null)
                        {
                            // Size == 1 / Scale
                            cookie.sizes = additionalLightData.lightCookieSize;
                            // Offset, Map cookie UV offset to light position on along local axes.
                            if (additionalLightData.lightCookieOffset != Vector2.zero)
                            {
                                var r = light.transform.right * additionalLightData.lightCookieOffset.x;
                                var u = light.transform.up * additionalLightData.lightCookieOffset.y;
                                var offset = r + u;

                                directionalLight.position += offset;
                            }
                        }

                        lightData.Init(ref directionalLight, ref cookie);
                        break;
                    case LightType.Point:
                        PointLight pointLight = new PointLight();
                        LightmapperUtils.Extract(light, ref pointLight);
                        lightData.Init(ref pointLight, ref cookie);
                        break;
                    case LightType.Spot:
                        SpotLight spotLight = new SpotLight();
                        LightmapperUtils.Extract(light, ref spotLight);
                        spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                        spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                        lightData.Init(ref spotLight, ref cookie);
                        break;
                    case LightType.Area:
                        RectangleLight rectangleLight = new RectangleLight();
                        LightmapperUtils.Extract(light, ref rectangleLight);
                        rectangleLight.mode = LightMode.Baked;
                        lightData.Init(ref rectangleLight);
                        break;
                    case LightType.Disc:
                        DiscLight discLight = new DiscLight();
                        LightmapperUtils.Extract(light, ref discLight);
                        discLight.mode = LightMode.Baked;
                        lightData.Init(ref discLight);
                        break;
                    default:
                        lightData.InitNoBake(light.GetInstanceID());
                        break;
                }

                lightData.falloff = FalloffType.InverseSquared;
                lightsOutput[i] = lightData;
            }
#else
            // If Enlighten realtime GI isn't active, we don't extract lights.
            if (SupportedRenderingFeatures.active.enlighten == false || ((int)SupportedRenderingFeatures.active.lightmapBakeTypes | (int)LightmapBakeType.Realtime) == 0)
            {
                for (int i = 0; i < requests.Length; i++)
                {
                    Light light = requests[i];
                    lightData.InitNoBake(light.GetInstanceID());
                    lightsOutput[i] = lightData;
                }
            }
            else
            {
                for (int i = 0; i < requests.Length; i++)
                {
                    Light light = requests[i];
                    switch (light.type)
                    {
                        case LightType.Directional:
                            DirectionalLight directionalLight = new DirectionalLight();
                            LightmapperUtils.Extract(light, ref directionalLight);
                            lightData.Init(ref directionalLight);
                            break;
                        case LightType.Point:
                            PointLight pointLight = new PointLight();
                            LightmapperUtils.Extract(light, ref pointLight);
                            lightData.Init(ref pointLight);
                            break;
                        case LightType.Spot:
                            SpotLight spotLight = new SpotLight();
                            LightmapperUtils.Extract(light, ref spotLight);
                            spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                            spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                            lightData.Init(ref spotLight);
                            break;
                        case LightType.Area:
                            // Rect area light is baked only in URP.
                            lightData.InitNoBake(light.GetInstanceID());
                            break;
                        case LightType.Disc:
                            // Disc light is baked only.
                            lightData.InitNoBake(light.GetInstanceID());
                            break;
                        default:
                            lightData.InitNoBake(light.GetInstanceID());
                            break;
                    }
                    lightData.falloff = FalloffType.InverseSquared;
                    lightsOutput[i] = lightData;
                }
            }
#endif
        };

        // called from DeferredLights.cs too
        public static void GetLightAttenuationAndSpotDirection(
            LightType lightType, float lightRange, Matrix4x4 lightLocalToWorldMatrix,
            float spotAngle, float? innerSpotAngle,
            out Vector4 lightAttenuation, out Vector4 lightSpotDir)
        {
            lightAttenuation = k_DefaultLightAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;

            // Directional Light attenuation is initialize so distance attenuation always be 1.0
            if (lightType != LightType.Directional)
            {
                // Light attenuation in universal matches the unity vanilla one.
                // attenuation = 1.0 / distanceToLightSqr
                // We offer two different smoothing factors.
                // The smoothing factors make sure that the light intensity is zero at the light range limit.
                // The first smoothing factor is a linear fade starting at 80 % of the light range.
                // smoothFactor = (lightRangeSqr - distanceToLightSqr) / (lightRangeSqr - fadeStartDistanceSqr)
                // We rewrite smoothFactor to be able to pre compute the constant terms below and apply the smooth factor
                // with one MAD instruction
                // smoothFactor =  distanceSqr * (1.0 / (fadeDistanceSqr - lightRangeSqr)) + (-lightRangeSqr / (fadeDistanceSqr - lightRangeSqr)
                //                 distanceSqr *           oneOverFadeRangeSqr             +              lightRangeSqrOverFadeRangeSqr

                // The other smoothing factor matches the one used in the Unity lightmapper but is slower than the linear one.
                // smoothFactor = (1.0 - saturate((distanceSqr * 1.0 / lightrangeSqr)^2))^2
                float lightRangeSqr = lightRange * lightRange;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightRange * lightRange);

                // On untethered devices: Use the faster linear smoothing factor (SHADER_HINT_NICE_QUALITY).
                // On other devices: Use the smoothing factor that matches the GI.
                lightAttenuation.x = GraphicsSettings.HasShaderDefine(Graphics.activeTier, BuiltinShaderDefine.SHADER_API_MOBILE) || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch ? oneOverFadeRangeSqr : oneOverLightRangeSqr;
                lightAttenuation.y = lightRangeSqrOverFadeRangeSqr;
            }

            if (lightType == LightType.Spot)
            {
                Vector4 dir = lightLocalToWorldMatrix.GetColumn(2);
                lightSpotDir = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                // Spot Attenuation with a linear falloff can be defined as
                // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
                // This can be rewritten as
                // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
                // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
                // If we precompute the terms in a MAD instruction
                float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * spotAngle * 0.5f);
                // We neeed to do a null check for particle lights
                // This should be changed in the future
                // Particle lights will use an inline function
                float cosInnerAngle;
                if (innerSpotAngle.HasValue)
                    cosInnerAngle = Mathf.Cos(innerSpotAngle.Value * Mathf.Deg2Rad * 0.5f);
                else
                    cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
                float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                float invAngleRange = 1.0f / smoothAngleRange;
                float add = -cosOuterAngle * invAngleRange;
                lightAttenuation.z = invAngleRange;
                lightAttenuation.w = add;
            }
        }

        public static void InitializeLightConstants_Common(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
        {
            lightPos = k_DefaultLightPosition;
            lightColor = k_DefaultLightColor;
            lightOcclusionProbeChannel = k_DefaultLightsProbeChannel;
            lightAttenuation = k_DefaultLightAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            if (lightData.lightType == LightType.Directional)
            {
                Vector4 dir = -lightData.localToWorldMatrix.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = lightData.localToWorldMatrix.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            }

            // VisibleLight.finalColor already returns color in active color space
            lightColor = lightData.finalColor;

            GetLightAttenuationAndSpotDirection(
                lightData.lightType, lightData.range, lightData.localToWorldMatrix,
                lightData.spotAngle, lightData.light?.innerSpotAngle,
                out lightAttenuation, out lightSpotDir);

            Light light = lightData.light;

            if (light != null && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                0 <= light.bakingOutput.occlusionMaskChannel &&
                light.bakingOutput.occlusionMaskChannel < 4)
            {
                lightOcclusionProbeChannel[light.bakingOutput.occlusionMaskChannel] = 1.0f;
            }
        }
    }

    internal enum URPProfileId
    {
        // CPU
        UniversalRenderTotal,
        UpdateVolumeFramework,
        RenderCameraStack,

        // GPU
        AdditionalLightsShadow,
        ColorGradingLUT,
        CopyColor,
        CopyDepth,
        DepthNormalPrepass,
        DepthPrepass,

        // DrawObjectsPass
        DrawOpaqueObjects,
        DrawTransparentObjects,

        // RenderObjectsPass
        //RenderObjects,

        LightCookies,

        MainLightShadow,
        ResolveShadows,
        SSAO,

        // PostProcessPass
        StopNaNs,
        SMAA,
        GaussianDepthOfField,
        BokehDepthOfField,
        MotionBlur,
        PaniniProjection,
        UberPostProcess,
        Bloom,
        LensFlareDataDriven,
        MotionVectors,

        FinalBlit
    }
}
