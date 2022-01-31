using System;
using UnityEditor;
using UnityEditor.Build;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.Tests
{
    public class HDRPPreprocessShadersTests
    {
        [SetUp]
        public void InitializeBuildPreprocessors()
        {
            // Try to initialize all build preprocessors
            foreach (var preprocessor in TypeCache.GetTypesDerivedFrom<IPreprocessBuildWithReport>())
            {
                try
                {
                    var buildReport = Activator.CreateInstance(preprocessor) as IPreprocessBuildWithReport;
                    buildReport.OnPreprocessBuild(default);
                }
                catch
                {
                }
            }
        }

        [Test]
        public void StripHDRPShader_WhenHDRPisNotCurrentPipeline()
        {
            var variants = new List<UnityEditor.Rendering.ShaderCompilerData>() { default, default };
            foreach (var stripper in TypeCache.GetTypesDerivedFrom<IPreprocessShaders>())
            {
                var stripperInstance = Activator.CreateInstance(stripper) as IPreprocessShaders;
                stripperInstance.OnProcessShader(Shader.Find("HDRP/Lit"), default, variants);
            }

            Assert.IsEmpty(variants);
        }
    }
}
