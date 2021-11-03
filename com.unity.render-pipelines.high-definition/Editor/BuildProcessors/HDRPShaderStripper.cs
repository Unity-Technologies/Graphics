using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HDRPShaderStripper : IShaderStripper
#if UNITY_2020_2_OR_NEWER
        , IComputeShaderStripper
#endif
    {
        readonly List<BaseShaderPreprocessor> shaderProcessorsList;

        public HDRPShaderStripper()
        {
            shaderProcessorsList = HDShaderUtils.GetBaseShaderPreprocessorList();
        }

        public bool isActive
        {
            get
            {
                if (HDRenderPipeline.currentAsset == null)
                    return false;

                if (HDRenderPipelineGlobalSettings.Ensure(canCreateNewAsset: false) == null)
                    return false;

                // TODO: Grab correct configuration/quality asset.
                var hdPipelineAssets = ShaderBuildPreprocessor.hdrpAssets;

                // Test if striping is enabled in any of the found HDRP assets.
                if (hdPipelineAssets.Count == 0 || !hdPipelineAssets.Any(a => a.allowShaderVariantStripping))
                    return false;

                return true;
            }
        }

        public bool isProcessed(Shader shader) => HDShaderUtils.IsHDRPShader(shader);

        public bool isVariantStripped([NotNull] Shader shader, ShaderSnippetData snippetData, ShaderCompilerData inputData)
        {
            // Remove the input by default, until we find a HDRP Asset in the list that needs it.
            bool removeInput = true;

            foreach (var hdAsset in ShaderBuildPreprocessor.hdrpAssets)
            {
                var strippedByPreprocessor = false;

                // Call list of strippers
                // Note that all strippers cumulate each other, so be aware of any conflict here
                foreach (BaseShaderPreprocessor shaderPreprocessor in shaderProcessorsList)
                {
                    if (shaderPreprocessor.ShadersStripper(hdAsset, shader, snippetData, inputData))
                    {
                        strippedByPreprocessor = true;
                        break;
                    }
                }

                if (!strippedByPreprocessor)
                {
                    removeInput = false;
                    break;
                }
            }

            return removeInput;
        }

#if UNITY_2020_2_OR_NEWER
        public bool isVariantStripped([NotNull] ComputeShader shader, string kernelName, ShaderCompilerData inputData)
        {
            throw new System.NotImplementedException();
        }

        public bool isProcessed(ComputeShader shader)
        {
            throw new System.NotImplementedException();
        }
#endif
    }
}
