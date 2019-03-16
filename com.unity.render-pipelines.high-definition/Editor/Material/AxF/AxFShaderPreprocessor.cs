using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class AxFShaderPreprocessor : BaseShaderPreprocessor
    {
        public AxFShaderPreprocessor() {}

        public override bool ShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            // Note: We know that all the rules of common stripper and Lit Stripper are apply, here we only need to do what is specific to AxF shader
            bool isForwardPass = snippet.passName == "ForwardOnly";
            bool isDepthOnlyPass = snippet.passName == "DepthForwardOnly";
            bool isMotionPass = snippet.passName == "Motion Vectors";

            // Using Contains to include the Tessellation variants
            bool isBuiltInLit = shader.name.Contains("HDRP/AxF");

            // Apply following set of rules only to inspector version of shader
            if (isBuiltInLit)
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
                {
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
                else // Opaque
                {
                    // TODO: Should we remove Cluster version if we know MSAA is disabled ? This prevent to manipulate LightLoop Settings (useFPTL option)
                    // For now comment following code
                    // if (inputData.shaderKeywordSet.IsEnabled(m_ClusterLighting) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportMSAA)
                    //    return true;
                }
            }

            return false;
        }
    }
}
