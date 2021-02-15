using NUnit.Framework.Interfaces;
using UnityEngine.TestTools;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Internal.Builders;
using NUnit.Framework;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace UnityEngine.VFX.PerformanceTest
{
    using Test = NUnit.Framework.Internal.Test;
    // If there are several UseGraphicTestCasesAttribute within the project, the AssetBundle.Load leads to an unexpected error.
    public class VFXPerformanceUseGraphicsTestCasesAttribute : UnityTestAttribute, ITestBuilder
    {
        NUnitTestCaseBuilder m_Builder = new NUnitTestCaseBuilder();

        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            var results = new List<TestMethod>();
#if UNITY_EDITOR
            var scenePaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .Where(s =>
                {
                    var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(s);
                    var labels = AssetDatabase.GetLabels(asset);
                    return !labels.Contains("ExcludeGfxTests");
                }).ToArray();

#else
            var scenePaths = File.ReadAllLines(Application.streamingAssetsPath + "/SceneList.txt");
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

                test.Name = Path.GetFileNameWithoutExtension(scenePath);
                results.Add(test);
            }
            return results;
        }
    }
}
