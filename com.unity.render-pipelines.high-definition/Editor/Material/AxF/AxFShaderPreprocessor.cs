using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class AxFShaderPreprocessor : BaseShaderPreprocessor
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
                }
                else // Opaque
                {

                }
            }

            return false;
        }
    }
}
