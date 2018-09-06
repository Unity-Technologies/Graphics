using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Flags]
    public enum PipelineCapabilities
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
        public static readonly string MainLightCookie = "_MAIN_LIGHT_COOKIE";
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
        static PipelineCapabilities s_PipelineCapabilities;

        public static PipelineCapabilities GetPipelineCapabilities()
        {
            return s_PipelineCapabilities;
        }

        public void SortCameras(Camera[] cameras)
        {
            Array.Sort(cameras, (lhs, rhs) => (int)(lhs.depth - rhs.depth));
        }

        static void SetPipelineCapabilities(LightweightPipelineAsset pipelineAsset)
        {
            s_PipelineCapabilities = 0U;

            // Strip variants based on selected pipeline features
            if (!pipelineAsset.customShaderVariantStripping)
            {
                if (pipelineAsset.maxPixelLights > 1 || pipelineAsset.supportsVertexLight)
                    s_PipelineCapabilities |= PipelineCapabilities.AdditionalLights;

                if (pipelineAsset.supportsVertexLight)
                    s_PipelineCapabilities |= PipelineCapabilities.VertexLights;

                if (pipelineAsset.supportsDirectionalShadows)
                    s_PipelineCapabilities |= PipelineCapabilities.DirectionalShadows;

                if (pipelineAsset.supportsLocalShadows)
                    s_PipelineCapabilities |= PipelineCapabilities.LocalShadows;

                bool anyShadows = pipelineAsset.supportsDirectionalShadows || pipelineAsset.supportsLocalShadows;
                if (pipelineAsset.supportsSoftShadows && anyShadows)
                    s_PipelineCapabilities |= PipelineCapabilities.SoftShadows;
            }
            else
            {
                if (pipelineAsset.keepAdditionalLightVariants)
                    s_PipelineCapabilities |= PipelineCapabilities.AdditionalLights;

                if (pipelineAsset.keepVertexLightVariants)
                    s_PipelineCapabilities |= PipelineCapabilities.VertexLights;

                if (pipelineAsset.keepDirectionalShadowVariants)
                    s_PipelineCapabilities |= PipelineCapabilities.DirectionalShadows;

                if (pipelineAsset.keepLocalShadowVariants)
                    s_PipelineCapabilities |= PipelineCapabilities.LocalShadows;

                if (pipelineAsset.keepSoftShadowVariants)
                    s_PipelineCapabilities |= PipelineCapabilities.SoftShadows;
            }
        }

        public static void GetLightCookieMatrix(VisibleLight light, out Matrix4x4 cookieMatrix)
        {
            cookieMatrix = Matrix4x4.Inverse(light.localToWorld);

            if (light.lightType == LightType.Directional)
            {
                float scale = 1.0f / light.light.cookieSize;

                // apply cookie scale and offset by 0.5 to convert from [-0.5, 0.5] to texture space [0, 1]
                Vector4 row0 = cookieMatrix.GetRow(0);
                Vector4 row1 = cookieMatrix.GetRow(1);
                cookieMatrix.SetRow(0, new Vector4(row0.x * scale, row0.y * scale, row0.z * scale, row0.w * scale + 0.5f));
                cookieMatrix.SetRow(1, new Vector4(row1.x * scale, row1.y * scale, row1.z * scale, row1.w * scale + 0.5f));
            }
            else if (light.lightType == LightType.Spot)
            {
                // we want out.w = 2.0 * in.z / m_CotanHalfSpotAngle
                // c = cotHalfSpotAngle
                // 1 0 0 0
                // 0 1 0 0
                // 0 0 1 0
                // 0 0 2/c 0
                // the "2" will be used to scale .xy for the cookie as in .xy/2 + 0.5
                float scale = 1.0f / light.range;
                float halfSpotAngleRad = Mathf.Deg2Rad * light.spotAngle * 0.5f;
                float cs = Mathf.Cos(halfSpotAngleRad);
                float ss = Mathf.Sin(halfSpotAngleRad);
                float cotHalfSpotAngle = cs / ss;

                Matrix4x4 scaleMatrix = Matrix4x4.identity;
                scaleMatrix.m00 = scaleMatrix.m11 = scaleMatrix.m22 = scale;
                scaleMatrix.m33 = 0.0f;
                scaleMatrix.m32 = scale * (2.0f / cotHalfSpotAngle);

                cookieMatrix = scaleMatrix * cookieMatrix;
            }

            // Remaining light types don't support cookies
        }

        public static bool IsSupportedCookieType(LightType lightType)
        {
            return lightType == LightType.Directional || lightType == LightType.Spot;
        }

        public static bool IsStereoEnabled(Camera camera)
        {
            bool isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            return XRGraphicsConfig.enabled && !isSceneViewCamera && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
        }
    }
}
