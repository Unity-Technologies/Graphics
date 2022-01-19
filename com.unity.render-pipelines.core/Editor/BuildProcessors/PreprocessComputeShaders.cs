using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Implements common functionality for SRP for the <see cref="IPreprocessComputeShaders"/>
    /// </summary>
    internal sealed class PreprocessComputeShaders : ShaderPreprocessor<ComputeShader, string>, IPreprocessComputeShaders
    {
        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> compilerDataList)
        {
            if (!StripShaderVariants(shader, kernelName, compilerDataList))
            {
                Debug.LogError("Error while stripping compute shader");
            }
        }
    }
}
