using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Flags]
    public enum PipelineCapabilities
    {
        AdditionalLights = (1 << 0),
        VertexLights = (1 << 1),
        DirectionalShadows = (1 << 2),
        LocalShadows = (1 << 3),
    }

    public class LightweightKeywords
    {
        public static readonly string AdditionalLightsText = "_ADDITIONAL_LIGHTS";
        public static readonly string VertexLightsText = "_VERTEX_LIGHTS";
        public static readonly string MixedLightingSubtractiveText = "_MIXED_LIGHTING_SUBTRACTIVE";
        public static readonly string MainLightCookieText = "_MAIN_LIGHT_COOKIE";
        public static readonly string DirectionalShadowsText = "_SHADOWS_ENABLED";
        public static readonly string LocalShadowsText = "_LOCAL_SHADOWS_ENABLED";
        
        public static readonly ShaderKeyword AdditionalLights = new ShaderKeyword(AdditionalLightsText);
        public static readonly ShaderKeyword VertexLights = new ShaderKeyword(VertexLightsText);
        public static readonly ShaderKeyword MixedLightingSubtractive = new ShaderKeyword(MixedLightingSubtractiveText);
        public static readonly ShaderKeyword MainLightCookie = new ShaderKeyword(MainLightCookieText);
        public static readonly ShaderKeyword DirectionalShadows = new ShaderKeyword(DirectionalShadowsText);
        public static readonly ShaderKeyword LocalShadows = new ShaderKeyword(LocalShadowsText);
    }

    public partial class LightweightPipeline
    {
        static PipelineCapabilities pipelineCapabilities;

        public static PipelineCapabilities GetPipelineCapabilities()
        {
            return pipelineCapabilities;
        }

        static void SetPipelineCapabilities(LightweightPipelineAsset pipelineAsset)
        {
            pipelineCapabilities = 0U;

            if (pipelineAsset.MaxPixelLights < 1 && !pipelineAsset.SupportsVertexLight)
                pipelineCapabilities |= PipelineCapabilities.AdditionalLights;

            if (pipelineAsset.SupportsVertexLight)
                pipelineCapabilities |= PipelineCapabilities.VertexLights;

            if (pipelineAsset.ShadowSetting != ShadowType.NO_SHADOW)
                pipelineCapabilities |= PipelineCapabilities.DirectionalShadows;

            pipelineCapabilities |= PipelineCapabilities.LocalShadows;
        }
    }
}

