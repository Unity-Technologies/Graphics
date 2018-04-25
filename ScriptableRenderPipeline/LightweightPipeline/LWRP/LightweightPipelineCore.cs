using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Flags]
    public enum FrameRenderingConfiguration
    {
        None                             = (0 << 0),
        Stereo                           = (1 << 0),
        Msaa                             = (1 << 1),
        BeforeTransparentPostProcess     = (1 << 2),
        PostProcess                      = (1 << 3),
        DepthPrePass                     = (1 << 4),
        DepthCopy                        = (1 << 5),
        DefaultViewport                  = (1 << 6),
        IntermediateTexture              = (1 << 7)
    }

    [Flags]
    public enum PipelineCapabilities
    {
        AdditionalLights    = (1 << 0),
        VertexLights        = (1 << 1),
        DirectionalShadows  = (1 << 2),
        LocalShadows        = (1 << 3),
        SoftShadows         = (1 << 4),
    }

    public class CameraComparer : IComparer<Camera>
    {
        public int Compare(Camera lhs, Camera rhs)
        {
            return (int)(lhs.depth - rhs.depth);
        }
    }

    public class LightweightKeywords
    {
        public static readonly string AdditionalLightsText = "_ADDITIONAL_LIGHTS";
        public static readonly string VertexLightsText = "_VERTEX_LIGHTS";
        public static readonly string MixedLightingSubtractiveText = "_MIXED_LIGHTING_SUBTRACTIVE";
        public static readonly string MainLightCookieText = "_MAIN_LIGHT_COOKIE";
        public static readonly string DirectionalShadowsText = "_SHADOWS_ENABLED";
        public static readonly string LocalShadowsText = "_LOCAL_SHADOWS_ENABLED";
        public static readonly string SoftShadowsText = "_SHADOWS_SOFT";
        public static readonly string CascadeShadowsText = "_SHADOWS_CASCADE";

#if UNITY_2018_2_OR_NEWER
        public static readonly ShaderKeyword AdditionalLights = new ShaderKeyword(AdditionalLightsText);
        public static readonly ShaderKeyword VertexLights = new ShaderKeyword(VertexLightsText);
        public static readonly ShaderKeyword MixedLightingSubtractive = new ShaderKeyword(MixedLightingSubtractiveText);
        public static readonly ShaderKeyword MainLightCookie = new ShaderKeyword(MainLightCookieText);
        public static readonly ShaderKeyword DirectionalShadows = new ShaderKeyword(DirectionalShadowsText);
        public static readonly ShaderKeyword LocalShadows = new ShaderKeyword(LocalShadowsText);
        public static readonly ShaderKeyword SoftShadows = new ShaderKeyword(SoftShadowsText);

        public static readonly ShaderKeyword Lightmap = new ShaderKeyword("LIGHTMAP_ON");
        public static readonly ShaderKeyword DirectionalLightmap = new ShaderKeyword("DIRLIGHTMAP_COMBINED");
#endif
    }

    public partial class LightweightPipeline
    {
        static Mesh s_FullscreenMesh = null;
        public static Mesh fullscreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 1.0f;
                float bottomV = 0.0f;

                Mesh mesh = new Mesh { name = "Fullscreen Quad" };
                mesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                mesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                mesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                mesh.UploadMeshData(true);
                return mesh;
            }
        }

        static PipelineCapabilities s_PipelineCapabilities;

        public static PipelineCapabilities GetPipelineCapabilities()
        {
            return s_PipelineCapabilities;
        }

        static void SetPipelineCapabilities(LightweightPipelineAsset pipelineAsset)
        {
            s_PipelineCapabilities = 0U;

            if (pipelineAsset.MaxPixelLights > 1 || pipelineAsset.SupportsVertexLight)
                s_PipelineCapabilities |= PipelineCapabilities.AdditionalLights;

            if (pipelineAsset.SupportsVertexLight)
                s_PipelineCapabilities |= PipelineCapabilities.VertexLights;

            if (pipelineAsset.SupportsDirectionalShadows)
                s_PipelineCapabilities |= PipelineCapabilities.DirectionalShadows;

            if (pipelineAsset.SupportsLocalShadows)
                s_PipelineCapabilities |= PipelineCapabilities.LocalShadows;

            bool anyShadows = pipelineAsset.SupportsDirectionalShadows || pipelineAsset.SupportsLocalShadows;
            if (pipelineAsset.SupportsSoftShadows && anyShadows)
                s_PipelineCapabilities |= PipelineCapabilities.SoftShadows;
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            commandBuffer.DrawMesh(fullscreenMesh, Matrix4x4.identity, material, 0, shaderPassId, properties);
        }

        public static void StartStereoRendering(Camera camera, ref ScriptableRenderContext context, FrameRenderingConfiguration renderingConfiguration)
        {
            if (CoreUtils.HasFlag(renderingConfiguration, FrameRenderingConfiguration.Stereo))
                context.StartMultiEye(camera);
        }

        public static void StopStereoRendering(Camera camera, ref ScriptableRenderContext context, FrameRenderingConfiguration renderingConfiguration)
        {
            if (CoreUtils.HasFlag(renderingConfiguration, FrameRenderingConfiguration.Stereo))
                context.StopMultiEye(camera);
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

        public static bool IsSupportedShadowType(LightType lightType)
        {
            return lightType == LightType.Directional || lightType == LightType.Spot;
        }

        public static bool IsSupportedCookieType(LightType lightType)
        {
            return lightType == LightType.Directional || lightType == LightType.Spot;
        }

        public static bool PlatformSupportsMSAABackBuffer()
        {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SAMSUNGTV
            return true;
#else
            return false;
#endif
        }
    }
}
