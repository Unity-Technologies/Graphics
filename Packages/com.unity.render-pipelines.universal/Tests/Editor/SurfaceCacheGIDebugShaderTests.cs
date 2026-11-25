using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.Rendering.Tests
{
    class SurfaceCacheGIDebugShaderTests
    {
        const string k_ShaderPath = "Packages/com.unity.render-pipelines.universal/Runtime/RendererFeatures/SurfaceCacheGIRendererFeature/Debug.compute";

        [Test]
        public void SurfaceCacheGIDebugShader_WhenCompiledForMetal_CompilesWithoutWarnings(
            [Values("VisualizeMonoCompute", "VisualizeSinglePassStereoInstancingCompute")] string kernelName,
            [Values("SURFACE_CACHE_PATH_TRACING_TRANSPARENCY_MODE_OPAQUES_ONLY", "SURFACE_CACHE_PATH_TRACING_TRANSPARENCY_MODE_CUTOUTS")] string transparencyKeyword)
        {
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(k_ShaderPath);
            Assert.IsNotNull(shader, $"Could not load compute shader at {k_ShaderPath}.");

            var result = ShaderUtil.CompileComputeShaderVariant(
                shader,
                shader.FindKernel(kernelName),
                new[] { transparencyKeyword },
                GraphicsDeviceType.Metal,
                BuildTarget.StandaloneOSX);

            Assert.IsTrue(result.Success, $"{kernelName} failed to compile for Metal.");

            // Messages without a source line come from the host toolchain, not from the shader code
            var messages = (result.Messages ?? new ShaderMessage[0]).Where(m => m.line > 0).ToArray();
            Assert.IsEmpty(messages, $"{kernelName} has compiler messages on Metal:\n" + string.Join("\n", messages.Select(m => $"{m.severity}: {m.message} ({m.file}:{m.line})")));
        }
    }
}
