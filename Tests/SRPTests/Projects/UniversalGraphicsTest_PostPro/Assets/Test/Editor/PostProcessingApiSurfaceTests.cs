using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Tests
{
    /// <summary>
    /// That the functions in PostProcessing/Common.hlsl which user shaders call keep the signatures they
    /// document, by compiling a fixture shader that calls each documented arity.
    /// </summary>
    [TestFixture]
    class PostProcessingApiSurfaceTests
    {
        const string k_ShaderName = "Hidden/Test/PostProcessingApiSurface";

        // ApplyTonemap and ApplyColorGrading, with and without agxParams.
        const int k_CoveredArities = 4;

        const ShaderCompilerPlatform k_CompilerPlatform = ShaderCompilerPlatform.D3D;
        const BuildTarget k_BuildTarget = BuildTarget.StandaloneWindows64;

        [Test]
        public void EveryCoveredArityCompiles()
        {
            Shader shader = Shader.Find(k_ShaderName);
            Assert.IsNotNull(shader, $"Could not find '{k_ShaderName}'.");

            ShaderData shaderData = ShaderUtil.GetShaderData(shader);
            Assert.IsNotNull(shaderData, $"Could not get ShaderData for '{k_ShaderName}'.");
            ShaderData.Subshader subshader = shaderData.GetSerializedSubshader(0);

            // A fixture that lost a pass would otherwise pass this test while covering less.
            Assert.AreEqual(k_CoveredArities, subshader.PassCount,
                "The fixture no longer has one pass per covered arity.");

            var failures = new StringBuilder();
            for (int passIndex = 0; passIndex < subshader.PassCount; ++passIndex)
            {
                ShaderData.Pass pass = subshader.GetPass(passIndex);
                ShaderData.VariantCompileInfo info = pass.CompileVariant(
                    ShaderType.Fragment, new string[0], k_CompilerPlatform, k_BuildTarget);
                if (info.Success)
                    continue;

                failures.AppendLine($"'{pass.Name}' failed to compile:");
                foreach (ShaderMessage m in info.Messages)
                    failures.AppendLine($"  {m.file}({m.line}): {m.message}");
            }

            Assert.IsEmpty(failures.ToString(),
                $"A PostProcessing/Common.hlsl signature changed without keeping a back-compat overload:\n{failures}");
        }
    }
}
