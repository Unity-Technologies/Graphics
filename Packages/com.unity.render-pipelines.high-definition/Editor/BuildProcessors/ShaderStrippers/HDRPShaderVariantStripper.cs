using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPShaderVariantStripper : IShaderVariantStripper
    {
        Lazy<List<BaseShaderPreprocessor>> m_ShaderProcessorsList = new Lazy<List<BaseShaderPreprocessor>>(() => HDShaderUtils.GetBaseShaderPreprocessorList());

        // Track list of materials asking for specific preprocessor step
        List<BaseShaderPreprocessor> shaderProcessorsList => m_ShaderProcessorsList.Value;

        public bool active
        {
            get
            {
                if (HDRPBuildData.instance.buildingPlayerForHDRenderPipeline)
                {
                    // Test if striping is enabled in any of the found HDRP assets.
                    foreach (var asset in HDRPBuildData.instance.renderPipelineAssets)
                    {
                        if (asset.allowShaderVariantStripping)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData shaderVariant, ShaderCompilerData shaderCompilerData)
        {
            // Remove the input by default, until we find a HDRP Asset in the list that needs it.
            bool removeInput = true;

            foreach (var hdAsset in HDRPBuildData.instance.renderPipelineAssets)
            {
                var strippedByPreprocessor = false;

                // Call list of strippers
                // Note that all strippers cumulate each other, so be aware of any conflict here
                foreach (BaseShaderPreprocessor shaderPreprocessor in shaderProcessorsList)
                {
                    if (shaderPreprocessor.ShadersStripper(hdAsset, shader, shaderVariant, shaderCompilerData))
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
    }
}
