using System;
using UnityEditor;
using UnityEditor.Build;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using System.Text;
using System.Reflection;

namespace UnityEditor.Rendering.Tests
{
    public class ShaderStrippingReportTest
    {
        class BuildReportTestScope : IDisposable
        {
            private IPreprocessBuildWithReport m_PreProcessReport;
            private IPostprocessBuildWithReport m_PostProcessReport;

            public BuildReportTestScope()
            {
                var type = Type.GetType(
                    "UnityEditor.Rendering.ShaderStrippingReportScope, Unity.RenderPipelines.Core.Editor");

                // We allow export for test purpouses
                var field = type.GetField("s_DefaultExport", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, true);

                // Obtain the callbacks to init and dispose
                var instance = Activator.CreateInstance(type);
                m_PostProcessReport = instance as IPostprocessBuildWithReport;
                m_PreProcessReport = instance as IPreprocessBuildWithReport;
                m_PreProcessReport.OnPreprocessBuild(default);
            }

            void IDisposable.Dispose()
            {
                m_PostProcessReport.OnPostprocessBuild(default);
            }
        }

        [Test]
        public void CheckReportIsCorrect()
        {
            using (new BuildReportTestScope())
            {
                var shaders = new List<Shader>() { Shader.Find("UI/Default"), Shader.Find("Sprites/Default") };
                foreach (var shader in shaders)
                {
                    for (uint i = 0; i < 5; ++i)
                    {
                        uint variantsIn = 10 * i;
                        ShaderStripping.reporter.OnShaderProcessed<Shader, ShaderSnippetData>(shader, default, variantsIn, (uint)(variantsIn * 0.5), i);
                    }
                }
            }

            var loggedStrippedResult = File.ReadAllText("Temp/shader-stripping.json");
            var loggedStrippedExpectedResult = File.ReadAllText(Path.GetFullPath("Packages/com.unity.render-pipelines.core/Tests/Editor/ShaderStripping/shader-stripping-result.json"));

            Assert.AreEqual(loggedStrippedExpectedResult, loggedStrippedResult);
        }
    }
}
