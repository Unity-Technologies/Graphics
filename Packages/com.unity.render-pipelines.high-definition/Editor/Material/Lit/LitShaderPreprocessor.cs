using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;

namespace UnityEditor.Rendering.HighDefinition
{
    class LitShaderPreprocessor : BaseShaderPreprocessor
    {
        Dictionary<Shader, System.Type> m_ShaderGraphMasterNodeType = new Dictionary<Shader, Type>();

        public override int Priority => 50;

        public LitShaderPreprocessor()
        {
        }

        protected override bool DoShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            // CAUTION: Pass Name and Lightmode name must match in master node and .shader.
            // HDRP use LightMode to do drawRenderer and pass name is use here for stripping!

            // Using Contains to include the Tessellation variants
            bool isBuiltInTerrainLit = shader.name.Contains("HDRP/TerrainLit");
            bool isBuiltInLit = shader.name.Contains("HDRP/Lit") || shader.name.Contains("HDRP/LayeredLit") || isBuiltInTerrainLit;

            // Cache Shader Graph lookup data so we don't continually keep reloading graphs from disk.
            // TODO: Should really be able to answer the questions "is shader graph" and "uses HDLitMasterNode" without
            //       hitting disk on every invoke.
            if (shader.IsShaderGraphAsset())
            {
                if (shader.TryGetMetadataOfType<HDMetadata>(out var obj))
                {
                    isBuiltInLit |= obj.shaderID == ShaderID.SG_Lit;
                }
            }

            // Caution: Currently only HDRP/TerrainLit is using keyword _ALPHATEST_ON with multi compile, we shouldn't test any other built in shader
            if (isBuiltInTerrainLit)
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_AlphaTestOn) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportTerrainHole)
                    return true;
            }

            // When using forward only, we never need GBuffer pass (only Forward)
            // Gbuffer Pass is suppose to exist only for Lit shader thus why we test the condition here in case another shader generate a GBuffer pass (like VFX)
            bool isGBufferPass = snippet.passName == "GBuffer";
            if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly && isGBufferPass)
                return true;

            // Variant of light layer only exist in GBuffer pass, so we test it here
            bool outputLayers = hdrpAsset.currentPlatformRenderPipelineSettings.supportLightLayers || hdrpAsset.currentPlatformRenderPipelineSettings.renderingLayerMaskBuffer;
            if (inputData.shaderKeywordSet.IsEnabled(m_RenderingLayers) && isGBufferPass && !outputLayers)
                return true;

            // This test include all Lit variant from Shader Graph (Because we check "DepthOnly" pass)
            // Other forward material ("DepthForwardOnly") don't use keyword for WriteNormalBuffer but #define
            bool isDepthOnlyPass = snippet.passName == "DepthOnly";
            if (isDepthOnlyPass)
            {
                // When we are full forward, we don't have depth prepass or motion vectors pass without writeNormalBuffer
                if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly && !inputData.shaderKeywordSet.IsEnabled(m_WriteNormalBuffer))
                    return true;

                // When we are deferred, we don't have depth prepass or motion vectors pass with writeNormalBuffer
                // Note: This rule is safe with Forward Material because WRITE_NORMAL_BUFFER is not a keyword for them, so it will not be removed
                if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly && inputData.shaderKeywordSet.IsEnabled(m_WriteNormalBuffer))
                    return true;
            }


            // Apply following set of rules only to lit shader (remember that LitPreprocessor is called for any shader)
            if (isBuiltInLit)
            {
                // Forward material don't use keyword for WriteNormalBuffer but #define so we can't test for the keyword outside of isBuiltInLit
                // otherwise the pass will be remove for non-lit shader graph version (like StackLit)
                bool isMotionPass = snippet.passName == "MotionVectors";
                if (isMotionPass)
                {
                    // When we are full forward, we don't have depth prepass or motion vectors pass without writeNormalBuffer
                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly && !inputData.shaderKeywordSet.IsEnabled(m_WriteNormalBuffer))
                        return true;

                    // When we are deferred, we don't have depth prepass or motion vectors pass with writeNormalBuffer
                    // Note: This rule is safe with Forward Material because WRITE_NORMAL_BUFFER is not a keyword for them, so it will not be removed
                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly && inputData.shaderKeywordSet.IsEnabled(m_WriteNormalBuffer))
                        return true;
                }

                if (!inputData.shaderKeywordSet.IsEnabled(m_Transparent)) // Opaque
                {
                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
                    {
                        // When we are in deferred, we only support tile lighting
                        if (inputData.shaderKeywordSet.IsEnabled(m_ClusterLighting))
                            return true;

                        bool isForwardPass = snippet.passName == "Forward";
                        if (isForwardPass && !inputData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                            return true;
                    }
                }

                if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
                {
                    // If transparent, we never need GBuffer pass.
                    if (isGBufferPass)
                        return true;

                    // If transparent we don't need the depth only pass
                    if (isDepthOnlyPass)
                        return true;
                }
            }

            // TODO: Tests for later
            // We need to find a way to strip useless shader features for passes/shader stages that don't need them (example, vertex shaders won't ever need SSS Feature flag)
            // This causes several problems:
            // - Runtime code that "finds" shader variants based on feature flags might not find them anymore... thus fall backing to the "let's give a score to variant" code path that may find the wrong variant.
            // - Another issue is that if a feature is declared without a "_" fall-back, if we strip the other variants, none may be left to use! This needs to be changed on our side.
            //if (snippet.shaderType == ShaderType.Vertex && inputData.shaderKeywordSet.IsEnabled(m_FeatureSSS))
            //    return true;

            return false;
        }
    }
}
