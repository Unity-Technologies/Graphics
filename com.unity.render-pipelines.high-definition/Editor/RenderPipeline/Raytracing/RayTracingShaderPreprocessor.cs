using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class RayTracingShaderPreprocessor : BaseShaderPreprocessor
    {
        public RayTracingShaderPreprocessor() {}

        protected override bool DoShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            // If ray tracing is disabled, strip all ray tracing shaders
            if (hdrpAsset.currentPlatformRenderPipelineSettings.supportRayTracing == false)
            {
                // If transparent we don't need the depth only pass
                if (snippet.passName == "IndirectDXR"
                    || snippet.passName == "ForwardDXR"
                    || snippet.passName == "VisibilityDXR"
                    || snippet.passName == "PathTracingDXR"
                    || snippet.passName == "GBufferDXR"
                    || snippet.passName == "SubSurfaceDXR")
                    return true;
            }
            else
            {
                // If we only support Performance mode, we do not want the indirectDXR shader
                if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Performance
                    && snippet.passName == "IndirectDXR")
                    return true;

                // If we only support Quality mode, we do not want the indirectDXR shader
                if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality
                    && snippet.passName == "GBufferDXR")
                    return true;
            }

            return false;
        }
    }
}
