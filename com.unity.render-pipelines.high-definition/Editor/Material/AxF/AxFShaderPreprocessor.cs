using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class AxFShaderPreprocessor : BaseShaderPreprocessor
    {
        public AxFShaderPreprocessor() {}

        //bool m_IsForwardPass;
        bool m_IsDepthOnlyPass;
        bool m_IsMotionPass;
        bool m_IsBuiltInLit;

        public override void PrepareShaderStripping(Shader shader, ShaderSnippetData snippet)
        {
            //m_IsForwardPass = snippet.passName == "ForwardOnly";
            m_IsDepthOnlyPass = snippet.passName == "DepthForwardOnly";
            m_IsMotionPass = snippet.passName == "Motion Vectors";

            // Using Contains to include the Tessellation variants
            m_IsBuiltInLit = shader.name.Contains("HDRP/AxF");
        }

        public override bool ShouldStripShader(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet)
        {
            return false;
        }

        public override bool ShouldStripVariant(HDRenderPipelineAsset hdrpAsset, ShaderCompilerData inputData)
        {
            // Note: We know that all the rules of common stripper and Lit Stripper are apply, here we only need to do what is specific to AxF shader

            // Apply following set of rules only to inspector version of shader
            if (m_IsBuiltInLit)
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
                {
                    // If transparent we don't need the depth only pass
                    if (m_IsDepthOnlyPass)
                        return true;

                    // If transparent we don't need the motion vector pass
                    if (m_IsMotionPass)
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
