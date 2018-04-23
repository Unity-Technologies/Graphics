using System;
using UnityEngine.Rendering;

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

        public static readonly ShaderKeyword AdditionalLights = new ShaderKeyword(AdditionalLightsText);
        public static readonly ShaderKeyword VertexLights = new ShaderKeyword(VertexLightsText);
        public static readonly ShaderKeyword MixedLightingSubtractive = new ShaderKeyword(MixedLightingSubtractiveText);
        public static readonly ShaderKeyword MainLightCookie = new ShaderKeyword(MainLightCookieText);
        public static readonly ShaderKeyword DirectionalShadows = new ShaderKeyword(DirectionalShadowsText);
        public static readonly ShaderKeyword LocalShadows = new ShaderKeyword(LocalShadowsText);
        public static readonly ShaderKeyword SoftShadows = new ShaderKeyword(SoftShadowsText);

        public static readonly ShaderKeyword Lightmap = new ShaderKeyword("LIGHTMAP_ON");
        public static readonly ShaderKeyword DirectionalLightmap = new ShaderKeyword("DIRLIGHTMAP_COMBINED");
    }

    public partial class LightweightPipeline
    {
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
    }
}
