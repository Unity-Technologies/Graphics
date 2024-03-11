using NUnit.Framework.Interfaces;
using UnityEngine.TestTools;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Internal.Builders;
using NUnit.Framework;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using UnityEditor.TestTools.Graphics;
#endif

namespace UnityEngine.VFX.PerformanceTest
{
    using Test = NUnit.Framework.Internal.Test;
    // If there are several UseGraphicTestCasesAttribute within the project, the AssetBundle.Load leads to an unexpected error.
    public class VFXPerformanceUseGraphicsTestCasesAttribute : UnityTestAttribute, ITestBuilder
    {
        NUnitTestCaseBuilder m_Builder = new NUnitTestCaseBuilder();

        public static string GetPrefix()
        {
            //Can't use SRPBinder here, this code is also runtime
            var currentSRP = QualitySettings.renderPipeline ?? GraphicsSettings.currentRenderPipeline;
            if (currentSRP == null)
                return "BRP";
            if (currentSRP.name.Contains("HDRenderPipeline"))
                return "HDRP";
            return currentSRP.name;
        }

        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            var results = new List<TestMethod>();
#if UNITY_EDITOR
            var scenePaths = EditorGraphicsTestCaseProvider.GetTestScenePaths().ToArray();
#else
            var scenePaths = RuntimeGraphicsTestCaseProvider.GetScenePaths();
#endif
            foreach (var scenePath in scenePaths)
            {
                var data = new TestCaseData(new object[] { new GraphicsTestCase(scenePath, Texture2D.blackTexture) });

                data.SetName(Path.GetFileNameWithoutExtension(scenePath));
                data.ExpectedResult = new Object();
                data.HasExpectedResult = true;

                var test = m_Builder.BuildTestMethod(method, suite, data);
                if (test.parms != null)
                    test.parms.HasExpectedResult = false;

                test.Name = string.Format("{0}.{1}", GetPrefix(), Path.GetFileNameWithoutExtension(scenePath));
                results.Add(test);
            }
            return results;
        }
    }
}
