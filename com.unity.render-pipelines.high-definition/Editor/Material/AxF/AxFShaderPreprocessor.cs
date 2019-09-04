using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class AxFShaderPreprocessor : BaseShaderPreprocessor
    {
        public AxFShaderPreprocessor() {}

        protected override bool DoShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            // Note: We know that all the rules of common stripper and Lit Stripper are apply, here we only need to do what is specific to AxF shader

            // Using Contains to include the Tessellation variants
            bool isBuiltInLit = shader.name.Contains("HDRP/AxF");

            // Apply following set of rules only to inspector version of shader
            if (isBuiltInLit)
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
                {
                    // If transparent we don't need the depth only pass
                    bool isDepthOnlyPass = snippet.passName == "DepthForwardOnly";
                    if (isDepthOnlyPass)
                        return true;

                    // If transparent we don't need the motion vector pass
                    bool isMotionPass = snippet.passName == "Motion Vectors";
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
