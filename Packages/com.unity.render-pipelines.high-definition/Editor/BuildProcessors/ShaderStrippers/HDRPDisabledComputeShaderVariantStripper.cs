using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPDisabledComputeShaderVariantStripper : IComputeShaderVariantStripper
    {
        HashSet<ComputeShader> m_ComputeShaders = new ();

        public HDRPDisabledComputeShaderVariantStripper()
        {
            var shaders = HDRenderPipelineGlobalSettings.Ensure().renderPipelineResources.shaders;

            var fields = shaders.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.FieldType== typeof(ComputeShader))
                {
                    ComputeShader value = (ComputeShader)fieldInfo.GetValue(shaders);
                    m_ComputeShaders.Add(value);
                }
            }
        }

        public bool active => HDRenderPipeline.currentAsset == null || ShaderBuildPreprocessor.hdrpAssets.Count == 0;

        public bool CanRemoveVariant([DisallowNull] ComputeShader shader, string shaderVariant, ShaderCompilerData shaderCompilerData)
        {
            return m_ComputeShaders.Contains(shader);
        }
    }
}
