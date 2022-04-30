using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Tests.ShaderStripping
{
    class BuildReportTestScope : IDisposable
    {
        private ShaderStrippingReportScope m_Scope;

        public BuildReportTestScope()
        {
            ShaderStrippingReportScope.s_DefaultExport = true;
            m_Scope = new ShaderStrippingReportScope();
            m_Scope.OnPreprocessBuild(default);
        }

        void IDisposable.Dispose()
        {
            m_Scope.OnPostprocessBuild(default);
            m_Scope = null;
            ShaderStrippingReportScope.s_DefaultExport = false;
        }
    }

    public class ShaderStrippingReportTest
    {
        [UnitySetUp]
        public void GlobalSetUp()
        {
            if (k_UpdateShaderStrippingResult && !Directory.Exists(k_ShaderResultsDirectory))
                    Directory.CreateDirectory(k_ShaderResultsDirectory);
        }

        static string k_ShaderResultsDirectory = "Packages/com.unity.render-pipelines.core/Tests/Editor/ShaderStripping/Results";

        static bool k_UpdateShaderStrippingResult = false;

        private void CheckForceUpdate(string path, string loggedStrippedResult)
        {
            if (k_UpdateShaderStrippingResult)
            {
                if (File.Exists(path))
                    File.Delete(path);
                File.WriteAllText(path, loggedStrippedResult);
            }
            Assert.IsFalse(k_UpdateShaderStrippingResult, "Note to Developer: You forgot to set back the variable `k_UpdateShaderStrippingResult` to false");
        }

        static TestCaseData[] s_StrippedShaderInputs =
        {
            new TestCaseData( new Shader[] { Shader.Find("UI/Default") }, 0u, 0f)
                .SetName("Given a shader with no variants, the result is not accumulated"),
            new TestCaseData( new Shader[] { Shader.Find("UI/Default"), Shader.Find("Sprites/Default") },  5u, 0f)
                .SetName("Given a shader with all the variants stripped, the result is 0"),
            new TestCaseData( new Shader[] { Shader.Find("UI/Default"), Shader.Find("Sprites/Default") },  5u, .5f)
                .SetName("Given a set of shaders, the result has the correct totals")
        };

        private void PerformFakeReport(Shader[] shaders, uint steps, float variantsOutMultiplier)
        {
            using (new BuildReportTestScope())
            {
                foreach (var shader in shaders)
                {
                    for (uint i = 0; i < steps; ++i)
                    {
                        uint variantsIn = steps * 2 * i;
                        UnityEditor.Rendering.ShaderStripping.reporter.OnShaderProcessed<Shader, ShaderSnippetData>(shader, default, string.Empty, variantsIn, (uint)(variantsIn * variantsOutMultiplier), i);
                    }
                }
            }
        }

        [Test, TestCaseSource(nameof(s_StrippedShaderInputs))]
        public void JSONOutput(Shader[] shaders, uint steps, float variantsOutMultiplier)
        {
            string fileName = $"{string.Join("_", shaders.Select(s => s.name))}_{steps}_{variantsOutMultiplier}.json".Replace("/", "_");
            string path = Path.GetFullPath(Path.Combine(k_ShaderResultsDirectory, fileName));

            PerformFakeReport(shaders, steps, variantsOutMultiplier);

            var loggedStrippedResult = File.ReadAllText(ShaderStrippingReport.k_ShaderOutputPath);

            CheckForceUpdate(path, loggedStrippedResult);

            Assert.AreEqual(File.ReadAllText(path), loggedStrippedResult, "Note to Developer: You can set the variable `k_UpdateShaderStrippingResult` to true, to override the output if you changed what is reported");
        }
    }
}
