using System;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Flags]
    public enum ShaderFeatures
    {
        MainLight               = (1 << 0),
        MainLightShadows        = (1 << 1),
        AdditionalLights        = (1 << 2),
        AdditionalLightShadows  = (1 << 3),
        VertexLighting          = (1 << 4),
        SoftShadows             = (1 << 5),
        MixedLighting           = (1 << 6),
    }

    public enum MixedLightingSetup
    {
        None,
        ShadowMask,
        Subtractive,
    };

    public struct RenderingData
    {
        public CullResults cullResults;
        public CameraData cameraData;
        public LightData lightData;
        public ShadowData shadowData;
        public bool supportsDynamicBatching;
    }

    public struct LightData
    {
        public int additionalLightsCount;
        public bool shadeAdditionalLightsPerVertex;
        public int mainLightIndex;
        public List<VisibleLight> visibleLights;
        public List<int> additionalLightIndices;
        public bool supportsMixedLighting;
    }

    public struct CameraData
    {
        public Camera camera;
        public float renderScale;
        public int msaaSamples;
        public bool isSceneViewCamera;
        public bool isDefaultViewport;
        public bool isOffscreenRender;
        public bool isHdrEnabled;
        public bool requiresDepthTexture;
        public bool requiresOpaqueTexture;
        public Downsampling opaqueTextureDownsampling;

        public SortFlags defaultOpaqueSortFlags;

        public bool isStereoEnabled;

        public float maxShadowDistance;
        public bool postProcessEnabled;
        public PostProcessLayer postProcessLayer;
    }

    public struct ShadowData
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
    }

    public sealed partial class LightweightRenderPipeline
    {
        static ShaderFeatures s_ShaderFeatures;

        public static ShaderFeatures supportedShaderFeatures
        {
            get { return s_ShaderFeatures; }
        }

        public static void SetSupportedShaderFeatures(LightweightRenderPipelineAsset pipelineAsset)
        {
            s_ShaderFeatures = ShaderFeatures.MainLight;

            if (pipelineAsset.supportsMainLightShadows)
                s_ShaderFeatures |= ShaderFeatures.MainLightShadows;

            if (pipelineAsset.additionalLightsRenderingMode == LightRenderingMode.PerVertex)
            {
                s_ShaderFeatures |= ShaderFeatures.AdditionalLights;
                s_ShaderFeatures |= ShaderFeatures.VertexLighting;
            }
            else if (pipelineAsset.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
            {
                s_ShaderFeatures |= ShaderFeatures.AdditionalLights;

                if (pipelineAsset.supportsAdditionalLightShadows)
                    s_ShaderFeatures |= ShaderFeatures.AdditionalLightShadows;
            }
            
            bool anyShadows = pipelineAsset.supportsMainLightShadows ||
                              CoreUtils.HasFlag(s_ShaderFeatures, ShaderFeatures.AdditionalLightShadows);
            if (pipelineAsset.supportsSoftShadows && anyShadows)
                s_ShaderFeatures |= ShaderFeatures.SoftShadows;

            if (pipelineAsset.supportsMixedLighting)
                s_ShaderFeatures |= ShaderFeatures.MixedLighting;
        }
        public static bool IsStereoEnabled(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

            bool isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            return XRGraphicsConfig.enabled && !isSceneViewCamera && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
        }

        void SortCameras(Camera[] cameras)
        {
            Array.Sort(cameras, (lhs, rhs) => (int)(lhs.depth - rhs.depth));
        }
    }
}
