using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;

using UnityEngine.Experimental.GlobalIllumination;
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
        internal bool resolveFinalTarget;
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
        public Camera camera;
        public CameraRenderType renderType;
        public RenderTexture targetTexture;
        public RenderTextureDescriptor cameraTargetDescriptor;
        // Internal camera data as we are not yet sure how to expose View in stereo context.
        // We might change this API soon.
        internal Matrix4x4 viewMatrix;
        internal Matrix4x4 projectionMatrix;
        internal Rect pixelRect;
        internal int pixelWidth;
        internal int pixelHeight;
        internal float aspectRatio;
        public float renderScale;
        public bool clearDepth;
        public bool isSceneViewCamera;
        public bool isDefaultViewport;
        public bool isHdrEnabled;
        public bool requiresDepthTexture;
        public bool requiresOpaqueTexture;

        public SortingCriteria defaultOpaqueSortFlags;

        public bool isStereoEnabled;
        internal int numberOfXRPasses;
        internal bool isXRMultipass;

        public float maxShadowDistance;
        public bool postProcessEnabled;

        public IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> captureActions;

        public LayerMask volumeLayerMask;
        public Transform volumeTrigger;

        public bool isStopNaNEnabled;
        public bool isDitheringEnabled;
        public AntialiasingMode antialiasing;
        public AntialiasingQuality antialiasingQuality;
        internal ScriptableRenderer renderer;
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

    public struct PostProcessingData
    {
        public ColorGradingMode gradingMode;
        public int lutSize;
    }

    class CameraDataComparer : IComparer<Camera>
    {
        public int Compare(Camera lhs, Camera rhs)
        {
            return (int)lhs.depth - (int)rhs.depth;
        }
    }

    public static class ShaderKeywordStrings
    {
        public static readonly string MainLightShadows = "_MAIN_LIGHT_SHADOWS";
        public static readonly string MainLightShadowCascades = "_MAIN_LIGHT_SHADOWS_CASCADE";
        public static readonly string AdditionalLightsVertex = "_ADDITIONAL_LIGHTS_VERTEX";
        public static readonly string AdditionalLightsPixel = "_ADDITIONAL_LIGHTS";
        public static readonly string AdditionalLightShadows = "_ADDITIONAL_LIGHT_SHADOWS";
        public static readonly string SoftShadows = "_SHADOWS_SOFT";
        public static readonly string MixedLightingSubtractive = "_MIXED_LIGHTING_SUBTRACTIVE";

        public static readonly string DepthNoMsaa = "_DEPTH_NO_MSAA";
        public static readonly string DepthMsaa2 = "_DEPTH_MSAA_2";
        public static readonly string DepthMsaa4 = "_DEPTH_MSAA_4";

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

        public static readonly string HighQualitySampling = "_HIGH_QUALITY_SAMPLING";
    }

    public sealed partial class UniversalRenderPipeline
    {
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
        public static bool IsStereoEnabled(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

            bool isGameCamera = IsGameCamera(camera);
            return XRGraphics.enabled && isGameCamera && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
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
        static bool IsMultiPassStereoEnabled(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

#if ENABLE_VR && ENABLE_VR_MODULE
            return IsStereoEnabled(camera) && XR.XRSettings.stereoRenderingMode == XR.XRSettings.StereoRenderingMode.MultiPass;
#else
            return false;
#endif
        }

        void SortCameras(Camera[] cameras)
        {
            if (cameras.Length <= 1)
                return;
            Array.Sort(cameras, new CameraDataComparer());
        }

        static RenderTextureDescriptor CreateRenderTextureDescriptor(Camera camera, float renderScale,
            bool isStereoEnabled, bool isHdrEnabled, int msaaSamples, bool needsAlpha)
        {
            RenderTextureDescriptor desc;
            RenderTextureFormat renderTextureFormatDefault = RenderTextureFormat.Default;

            // NB: There's a weird case about XR and render texture
            // In test framework currently we render stereo tests to target texture
            // The descriptor in that case needs to be initialized from XR eyeTexture not render texture
            // Otherwise current tests will fail. Check: Do we need to update the test images instead?
            if (isStereoEnabled)
            {
                desc = XRGraphics.eyeTextureDesc;
                renderTextureFormatDefault = desc.colorFormat;
            }
            else if (camera.targetTexture == null)
            {
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                desc.width = (int)((float)desc.width * renderScale);
                desc.height = (int)((float)desc.height * renderScale);
            }
            else
            {
                desc = camera.targetTexture.descriptor;
            }

            if (camera.targetTexture != null)
            {
                desc.colorFormat = camera.targetTexture.descriptor.colorFormat;
                desc.depthBufferBits = camera.targetTexture.descriptor.depthBufferBits;
                desc.msaaSamples = camera.targetTexture.descriptor.msaaSamples;
                desc.sRGB = camera.targetTexture.descriptor.sRGB;
            }
            else
            {
                bool use32BitHDR = !needsAlpha && RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float);
                RenderTextureFormat hdrFormat = (use32BitHDR) ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.DefaultHDR;
            
                desc.colorFormat = isHdrEnabled ? hdrFormat : renderTextureFormatDefault;
                desc.depthBufferBits = 32;
                desc.msaaSamples = msaaSamples;
                desc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            }

            desc.enableRandomWrite = false;
            desc.bindMS = false;
            desc.useDynamicScale = camera.allowDynamicResolution;
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
    }

    internal enum URPProfileId
    {
        StopNaNs,
        SMAA,
        GaussianDepthOfField,
        BokehDepthOfField,
        MotionBlur,
        PaniniProjection,
        UberPostProcess,
        Bloom,
    }
}
