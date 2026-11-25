using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    class BuildLightGridShaderTests
    {
        const string k_ShaderPath = "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Shaders/BuildLightGrid.compute";

        [Test]
        public void BuildLightGridShader_CompilesWithoutWarnings()
        {
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(k_ShaderPath);
            Assert.IsNotNull(shader, $"Could not load compute shader at {k_ShaderPath}.");

            var deviceType = SystemInfo.graphicsDeviceType;
            if (deviceType == GraphicsDeviceType.Null || !ShaderUtil.IsGraphicsAPISupported(shader, deviceType))
                Assert.Ignore($"{k_ShaderPath} has no kernels for the {deviceType} graphics API.");

            var messages = new List<ShaderMessage>();
            foreach (var kernel in new[] { "BuildConservativeLightGrid", "BuildRegirLightGrid" })
            {
                int kernelIndex = shader.FindKernel(kernel);
                foreach (var keywords in new[] { new string[0], new[] { "SPARSE_GRID" } })
                {
                    var result = ShaderUtil.CompileComputeShaderVariant(shader, kernelIndex, keywords, deviceType, EditorUserBuildSettings.activeBuildTarget);
                    messages.AddRange(result.Messages);
                }
            }

            Assert.IsEmpty(messages, $"{k_ShaderPath} has compiler messages:\n" + string.Join("\n", messages.Select(m => $"{m.severity}: {m.message} ({m.file}:{m.line})")));
        }
    }
}
