using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.PackageManager.UI;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Tests
{
    public class SampleImportTests
    {
        static string[] s_PackagePaths =
        {
            "Packages/com.unity.render-pipelines.universal",
            "Packages/com.unity.render-pipelines.high-definition"
        };

        static IEnumerable<TestCaseData> PackageSampleCases()
        {
            foreach (var packagePath in s_PackagePaths)
            {
                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(packagePath);
                var samples = Sample.FindByPackage(packageInfo.name, packageInfo.version);
                foreach (var sample in samples)
                {
                    yield return new TestCaseData(sample)
                        .SetName($"Import {packageInfo.displayName} > {sample.displayName}")
                        .Returns(null);
                }
            }
        }

        [UnityTest, TestCaseSource(nameof(PackageSampleCases))]
        public IEnumerator ImportSamples(Sample sample)
        {
            sample.Import(Sample.ImportOptions.OverridePreviousImports | Sample.ImportOptions.HideImportWindow);

            EditorUtility.RequestScriptReload(); // Sample might not contain scripts, so ensure domain reload happens

            yield return new WaitForDomainReload();

            FileUtil.DeleteFileOrDirectory("Assets/Samples");
        }
    }
}
