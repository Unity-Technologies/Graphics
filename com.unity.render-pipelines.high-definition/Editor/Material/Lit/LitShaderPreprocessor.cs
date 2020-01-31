using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class LitShaderPreprocessor : BaseShaderPreprocessor
    {
        public LitShaderPreprocessor() {}

        public override bool ShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            // CAUTION: Pass Name and Lightmode name must match in master node and .shader.
            // HDRP use LightMode to do drawRenderer and pass name is use here for stripping!
            bool isGBufferPass = snippet.passName == "GBuffer";
            bool isForwardPass = snippet.passName == "Forward";
            bool isDepthOnlyPass = snippet.passName == "DepthOnly";
            bool isMotionPass = snippet.passName == "MotionVectors";
            bool isTransparentPrepass = snippet.passName == "TransparentDepthPrepass";
            bool isTransparentPostpass = snippet.passName == "TransparentDepthPostpass";
            bool isTransparentBackface = snippet.passName == "TransparentBackface";
            bool isDistortionPass = snippet.passName == "DistortionVectors";
            bool isTransparentForwardPass = isTransparentPostpass || isTransparentBackface || isTransparentPrepass || isDistortionPass;

            // Using Contains to include the Tessellation variants
            bool isBuiltInLit = shader.name.Contains("HDRP/Lit") || shader.name.Contains("HDRP/LayeredLit") || shader.name.Contains("HDRP/TerrainLit");

            if (shader.IsShaderGraph())
            {
                string shaderPath = AssetDatabase.GetAssetPath(shader);
                isBuiltInLit |= GraphUtil.GetOutputNodeType(shaderPath) == typeof(HDLitMasterNode);
            }

            // When using forward only, we never need GBuffer pass (only Forward)
            // Gbuffer Pass is suppose to exist only for Lit shader thus why we test the condition here in case another shader generate a GBuffer pass (like VFX)
            if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly && isGBufferPass)
                return true;

            // Variant of light layer only exist in GBuffer pass, so we test it here
            if (inputData.shaderKeywordSet.IsEnabled(m_LightLayers) && isGBufferPass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportLightLayers)
                return true;

            // This test include all Lit variant from Shader Graph (Because we check "DepthOnly" pass)
            // Other forward material ("DepthForwardOnly") don't use keyword for WriteNormalBuffer but #define
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

            // Apply following set of rules only to inspector version of shader as we don't have Transparent keyword with shader graph
            if (isBuiltInLit)
            {
                // Forward material don't use keyword for WriteNormalBuffer but #define so we can't test for the keyword outside of isBuiltInLit
                // otherwise the pass will be remove for non-lit shader graph version (like StackLit)
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
                    // If opaque, we never need transparent specific passes (even in forward only mode)
                    if (isTransparentForwardPass)
                        return true;

                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
                    {
                        // When we are in deferred, we only support tile lighting
                        if (inputData.shaderKeywordSet.IsEnabled(m_ClusterLighting))
                            return true;
                        
                        if (isForwardPass && !inputData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                            return true;
                    }

                    // TODO: Should we remove Cluster version if we know MSAA is disabled ? This prevent to manipulate LightLoop Settings (useFPTL option)
                    // For now comment following code
                    // if (inputData.shaderKeywordSet.IsEnabled(m_ClusterLighting) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportMSAA)
                    //    return true;
                }
            }

            // We strip passes for transparent passes outside of isBuiltInLit because we want Hair, Fabric
            // and StackLit shader graphs to be taken in account.
            if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
            {
                // If transparent, we never need GBuffer pass.
                if (isGBufferPass)
                    return true;

                // If transparent we don't need the depth only pass
                if (isDepthOnlyPass)
                    return true;

                // If transparent we don't need the motion vector pass
                if (isMotionPass)
                    return true;

                // If we are transparent we use cluster lighting and not tile lighting
                if (inputData.shaderKeywordSet.IsEnabled(m_TileLighting))
                    return true;
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
