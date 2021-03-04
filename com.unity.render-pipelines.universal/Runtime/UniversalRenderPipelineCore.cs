using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;

using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;

namespace UnityEngine.Rendering.Universal
{
    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum MixedLightingSetup
    {
        None,
        ShadowMask,
        Subtractive,
    };

    [MovedFrom("UnityEngine.Rendering.LWRP")] public struct RenderingData
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

    [MovedFrom("UnityEngine.Rendering.LWRP")] public struct LightData
    {
        public int mainLightIndex;
        public int additionalLightsCount;
        public int maxPerObjectAdditionalLightsCount;
        public NativeArray<VisibleLight> visibleLights;
        public bool shadeAdditionalLightsPerVertex;
        public bool supportsMixedLighting;
    }

    [MovedFrom("UnityEngine.Rendering.LWRP")] public struct CameraData
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
        public bool clearDepth;
        public CameraType cameraType;
        public bool isDefaultViewport;
        public bool isHdrEnabled;
        public bool requiresDepthTexture;
        public bool requiresOpaqueTexture;
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

                return Display.main.requiresSrgbBlitToBackbuffer;
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
    }

    [MovedFrom("UnityEngine.Rendering.LWRP")] public struct ShadowData
    {
        public bool supportsMainLightShadows;
        public bool requiresScreenSpaceShadowResolve;
        public int mainLightShadowmapWidth;
        public int mainLightShadowmapHeight;
        public int mainLightShadowCascadesCount;
        public Vector3 mainLightShadowCascadesSplit;
        public bool supportsAdditionalLightShadows;
        public int additionalLightsShadowmapWidth;
        public int additionalLightsShadowmapHeight;
        public bool supportsSoftShadows;
        public int shadowmapDepthBufferBits;
        public List<Vector4> bias;
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
        public int lightIndex;
        public Vector4 occlusionProbeInfo;
    }

    internal static class ShaderPropertyId
    {
        public static readonly int glossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
        public static readonly int subtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

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
    }

    public static class ShaderKeywordStrings
    {
        public static readonly string MainLightShadows = "_MAIN_LIGHT_SHADOWS";
        public static readonly string MainLightShadowCascades = "_MAIN_LIGHT_SHADOWS_CASCADE";
        public static readonly string AdditionalLightsVertex = "_ADDITIONAL_LIGHTS_VERTEX";
        public static readonly string AdditionalLightsPixel = "_ADDITIONAL_LIGHTS";
        public static readonly string AdditionalLightShadows = "_ADDITIONAL_LIGHT_SHADOWS";
        public static readonly string SoftShadows = "_SHADOWS_SOFT";
        public static readonly string MixedLightingSubtractive = "_MIXED_LIGHTING_SUBTRACTIVE"; // Backward compatibility
        public static readonly string LightmapShadowMixing = "LIGHTMAP_SHADOW_MIXING";
        public static readonly string ShadowsShadowMask = "SHADOWS_SHADOWMASK";

        public static readonly string DepthNoMsaa = "_DEPTH_NO_MSAA";
        public static readonly string DepthMsaa2 = "_DEPTH_MSAA_2";
        public static readonly string DepthMsaa4 = "_DEPTH_MSAA_4";
        public static readonly string DepthMsaa8 = "_DEPTH_MSAA_8";

        public static readonly string LinearToSRGBConversion = "_LINEAR_TO_SRGB_CONVERSION";

        public static readonly string SmaaLow = "_SMAA_PRESET_LOW";
        public static readonly string SmaaMedium = "_SMAA_PRESET_MEDIUM";
        public static readonly string SmaaHigh = "_SMAA_PRESET_HIGH";
        public static readonly string PaniniGeneric = "_GENERIC";
        public static readonly string PaniniUnitDistance = "_UNIT_DISTANCE";
        public static readonly string BloomLQ = "_BLOOM_LQ";
        public static readonly string BloomHQ = "_BLOOM_HQ";
        public static readonly string BloomLQDirt = "_BLOOM_LQ_DIRT";
        public static readonly string BloomHQDirt = "_BLOOM_HQ_DIRT";
        public static readonly string UseRGBM = "_USE_RGBM";
        public static readonly string Distortion = "_DISTORTION";
        public static readonly string ChromaticAberration = "_CHROMATIC_ABERRATION";
        public static readonly string HDRGrading = "_HDR_GRADING";
        public static readonly string TonemapACES = "_TONEMAP_ACES";
        public static readonly string TonemapNeutral = "_TONEMAP_NEUTRAL";
        public static readonly string FilmGrain = "_FILM_GRAIN";
        public static readonly string Fxaa = "_FXAA";
        public static readonly string Dithering = "_DITHERING";
        public static readonly string ScreenSpaceOcclusion = "_SCREEN_SPACE_OCCLUSION";

        public static readonly string HighQualitySampling = "_HIGH_QUALITY_SAMPLING";

        public static readonly string DOWNSAMPLING_SIZE_2 = "DOWNSAMPLING_SIZE_2";
        public static readonly string DOWNSAMPLING_SIZE_4 = "DOWNSAMPLING_SIZE_4";
        public static readonly string DOWNSAMPLING_SIZE_8 = "DOWNSAMPLING_SIZE_8";
        public static readonly string DOWNSAMPLING_SIZE_16 = "DOWNSAMPLING_SIZE_16";
        public static readonly string _SPOT = "_SPOT";
        public static readonly string _DIRECTIONAL = "_DIRECTIONAL";
        public static readonly string _POINT = "_POINT";
        public static readonly string _DEFERRED_ADDITIONAL_LIGHT_SHADOWS = "_DEFERRED_ADDITIONAL_LIGHT_SHADOWS";
        public static readonly string _GBUFFER_NORMALS_OCT = "_GBUFFER_NORMALS_OCT";
        public static readonly string _DEFERRED_SUBTRACTIVE_LIGHTING = "_DEFERRED_SUBTRACTIVE_LIGHTING";
        public static readonly string LIGHTMAP_ON = "LIGHTMAP_ON";
        public static readonly string _ALPHATEST_ON = "_ALPHATEST_ON";
        public static readonly string DIRLIGHTMAP_COMBINED = "DIRLIGHTMAP_COMBINED";
        public static readonly string _DETAIL_MULX2 = "_DETAIL_MULX2";
        public static readonly string _DETAIL_SCALED = "_DETAIL_SCALED";
        public static readonly string _CLEARCOAT = "_CLEARCOAT";
        public static readonly string _CLEARCOATMAP = "_CLEARCOATMAP";

        // XR
        public static readonly string UseDrawProcedural = "_USE_DRAW_PROCEDURAL";
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

        Comparison<Camera> cameraComparison = (camera1, camera2) => { return (int) camera1.depth - (int) camera2.depth; };
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

        static RenderTextureDescriptor CreateRenderTextureDescriptor(Camera camera, float renderScale,
            bool isHdrEnabled, int msaaSamples, bool needsAlpha)
        {
            RenderTextureDescriptor desc;
            GraphicsFormat renderTextureFormatDefault = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

            if (camera.targetTexture == null)
            {
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                desc.width = (int)((float)desc.width * renderScale);
                desc.height = (int)((float)desc.height * renderScale);


                GraphicsFormat hdrFormat;
                if (!needsAlpha && RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
                    hdrFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                else if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Linear | FormatUsage.Render))
                    hdrFormat = GraphicsFormat.R16G16B16A16_SFloat;
                else
                    hdrFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR); // This might actually be a LDR format on old devices.

                desc.graphicsFormat = isHdrEnabled ? hdrFormat : renderTextureFormatDefault;
                desc.depthBufferBits = 32;
                desc.msaaSamples = msaaSamples;
                desc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            }
            else
            {
                desc = camera.targetTexture.descriptor;
                desc.width = camera.pixelWidth;
                desc.height = camera.pixelHeight;
                if (camera.cameraType == CameraType.SceneView  && !isHdrEnabled)
                {
                    desc.graphicsFormat = renderTextureFormatDefault;
                }
                // SystemInfo.SupportsRenderTextureFormat(camera.targetTexture.descriptor.colorFormat)
                // will assert on R8_SINT since it isn't a valid value of RenderTextureFormat.
                // If this is fixed then we can implement debug statement to the user explaining why some
                // RenderTextureFormats available resolves in a black render texture when no warning or error
                // is given.
            }

            desc.enableRandomWrite = false;
            desc.bindMS = false;
            desc.useDynamicScale = camera.allowDynamicResolution;

            // check that the requested MSAA samples count is supported by the current platform. If it's not supported,
            // replace the requested desc.msaaSamples value with the actual value the engine falls back to
            desc.msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(desc);

            return desc;
        }

        static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
        {
            // Editor only.
#if UNITY_EDITOR
            LightDataGI lightData = new LightDataGI();

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
            LightDataGI lightData = new LightDataGI();

            for (int i = 0; i < requests.Length; i++)
            {
                Light light = requests[i];
                lightData.InitNoBake(light.GetInstanceID());
                lightsOutput[i] = lightData;
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

                // On mobile and Nintendo Switch: Use the faster linear smoothing factor (SHADER_HINT_NICE_QUALITY).
                // On other devices: Use the smoothing factor that matches the GI.
                lightAttenuation.x = Application.isMobilePlatform || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch ? oneOverFadeRangeSqr : oneOverLightRangeSqr;
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

        FinalBlit
    }
}
