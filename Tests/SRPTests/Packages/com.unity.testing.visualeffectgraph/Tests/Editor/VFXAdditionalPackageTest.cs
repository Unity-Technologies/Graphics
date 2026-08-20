using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXAdditionalPackageTest
    {
        public static string[] kAdditionalSampleMatches = new [] {"Additions", "Helpers"};

        private static readonly string kSampleExpectedPath = "Assets/Samples";

        [SerializeField]
        private string m_CurrentMatch;

        [UnityTest, Timeout(10 * 60 * 1000)]
        public IEnumerator Check_Additional_Doesnt_Generate_Any_Errors([ValueSource(nameof(kAdditionalSampleMatches))] string expectedMatch)
        {
            m_CurrentMatch = expectedMatch;

            if (Directory.Exists(kSampleExpectedPath))
            {
                AssetDatabase.DeleteAsset(kSampleExpectedPath);
                CompilationPipeline.RequestScriptCompilation();
                yield return new WaitForDomainReload();
            }

            Assert.IsFalse(Directory.Exists(kSampleExpectedPath));
            var searchRequest = Client.Search("com.unity.visualeffectgraph", true);
            while (!searchRequest.IsCompleted)
            {
                yield return null;
            }

            Assert.AreEqual(StatusCode.Success, searchRequest.Status);
            Assert.AreEqual(1, searchRequest.Result.Length);

            var version = searchRequest.Result[0].version;
            Assert.IsFalse(string.IsNullOrEmpty(version));

            var allSample = Sample.FindByPackage("com.unity.visualeffectgraph", version).ToArray();
            Assert.AreEqual(3, allSample.Length);

            var matching = allSample.Where(o => o.displayName.Contains(m_CurrentMatch)).ToArray();
            Assert.AreEqual(1, matching.Length);

            var current = matching[0];
            var result = current.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
            Assert.IsTrue(result);

            CompilationPipeline.RequestScriptCompilation();
            yield return new WaitForDomainReload();

            Assert.IsTrue(Directory.Exists(kSampleExpectedPath));

            m_CurrentMatch = null;

            AssetDatabase.DeleteAsset(kSampleExpectedPath);
            CompilationPipeline.RequestScriptCompilation();
            yield return new WaitForDomainReload();
        }
    }
}
