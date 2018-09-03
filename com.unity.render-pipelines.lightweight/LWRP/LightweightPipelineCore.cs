using System;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Flags]
    public enum ShaderFeatures
    {
        AdditionalLights    = (1 << 0),
        VertexLights        = (1 << 1),
        DirectionalShadows  = (1 << 2),
        LocalShadows        = (1 << 3),
        SoftShadows         = (1 << 4),
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
        public int pixelAdditionalLightsCount;
        public int totalAdditionalLightsCount;
        public int mainLightIndex;
        public List<VisibleLight> visibleLights;
        public List<int> visibleLocalLightIndices;
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
        public bool requiresSoftParticles;
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
        public bool renderDirectionalShadows;
        public bool requiresScreenSpaceShadowResolve;
        public int directionalShadowAtlasWidth;
        public int directionalShadowAtlasHeight;
        public int directionalLightCascadeCount;
        public Vector3 directionalLightCascades;
        public bool renderLocalShadows;
        public int localShadowAtlasWidth;
        public int localShadowAtlasHeight;
        public bool supportsSoftShadows;
        public int bufferBitCount;
    }

    public static class LightweightKeywordStrings
    {
        public static readonly string AdditionalLights = "_ADDITIONAL_LIGHTS";
        public static readonly string VertexLights = "_VERTEX_LIGHTS";
        public static readonly string MixedLightingSubtractive = "_MIXED_LIGHTING_SUBTRACTIVE";
        public static readonly string DirectionalShadows = "_SHADOWS_ENABLED";
        public static readonly string LocalShadows = "_LOCAL_SHADOWS_ENABLED";
        public static readonly string SoftShadows = "_SHADOWS_SOFT";
        public static readonly string CascadeShadows = "_SHADOWS_CASCADE";
        public static readonly string DepthNoMsaa = "_DEPTH_NO_MSAA";
        public static readonly string DepthMsaa2 = "_DEPTH_MSAA_2";
        public static readonly string DepthMsaa4 = "_DEPTH_MSAA_4";
        public static readonly string SoftParticles = "SOFTPARTICLES_ON";
    }

    public sealed partial class LightweightPipeline
    {
        static ShaderFeatures s_ShaderFeatures;

        public static ShaderFeatures GetSupportedShaderFeatures()
        {
            return s_ShaderFeatures;
        }

        void SortCameras(Camera[] cameras)
        {
            Array.Sort(cameras, (lhs, rhs) => (int)(lhs.depth - rhs.depth));
        }

        static void SetSupportedShaderFeatures(LightweightPipelineAsset pipelineAsset)
        {
            s_ShaderFeatures = 0U;

            // Strip variants based on selected pipeline features
            if (pipelineAsset.maxPixelLights > 1 || pipelineAsset.supportsVertexLight)
                s_ShaderFeatures |= ShaderFeatures.AdditionalLights;

            if (pipelineAsset.supportsVertexLight)
                s_ShaderFeatures |= ShaderFeatures.VertexLights;

            if (pipelineAsset.supportsDirectionalShadows)
                s_ShaderFeatures |= ShaderFeatures.DirectionalShadows;

            if (pipelineAsset.supportsLocalShadows)
                s_ShaderFeatures |= ShaderFeatures.LocalShadows;

            bool anyShadows = pipelineAsset.supportsDirectionalShadows || pipelineAsset.supportsLocalShadows;
            if (pipelineAsset.supportsSoftShadows && anyShadows)
                s_ShaderFeatures |= ShaderFeatures.SoftShadows;
        }
        public static bool IsStereoEnabled(Camera camera)
        {
            bool isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            return XRGraphicsConfig.enabled && !isSceneViewCamera && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
        }
    }
}
