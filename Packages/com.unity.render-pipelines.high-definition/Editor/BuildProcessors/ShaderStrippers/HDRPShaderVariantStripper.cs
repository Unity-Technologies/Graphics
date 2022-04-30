using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

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
                if (HDRenderPipeline.currentAsset == null)
                    return false;

                if (HDRenderPipelineGlobalSettings.Ensure(canCreateNewAsset: false) == null)
                    return false;

                // TODO: Grab correct configuration/quality asset.
                var hdPipelineAssets = ShaderBuildPreprocessor.hdrpAssets;

                if (hdPipelineAssets.Count == 0)
                    return false;

                // Test if striping is enabled in any of the found HDRP assets.
                if (hdPipelineAssets.Count == 0 || !hdPipelineAssets.Any(a => a.allowShaderVariantStripping))
                    return false;

                return true;
            }
        }

        public bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData shaderVariant, ShaderCompilerData shaderCompilerData)
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
