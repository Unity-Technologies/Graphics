using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXAdditionalPackageTest
    {
        public static string[] kAdditionalSampleMatches = new [] {"Additions", "Helpers", "Learning"};

        private static readonly string kSampleExpectedPath = "Assets/Samples";

        [SerializeField]
        private static string m_CurrentMatch;

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

            //Workaround for UUM-63664
            var current = matching[0];
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageSelectionChange(searchRequest.Result[0]);

                //Force import of dependencies before importing anything else
                var samplePath = Path.Combine(kSampleExpectedPath, "Visual Effect Graph", version, current.displayName);
                Directory.CreateDirectory(samplePath);
                File.WriteAllText(samplePath + "/dummy.txt", "UUM-63664 workaround for test.");
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            var result = current.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
            Assert.IsTrue(result);

            CompilationPipeline.RequestScriptCompilation();
            yield return new WaitForDomainReload();

            Assert.IsTrue(Directory.Exists(kSampleExpectedPath));
            if (m_CurrentMatch == "Learning")
            {
                //Extra check for learning sample consistency
                foreach (var guid in AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { kSampleExpectedPath }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                    var graph = vfxAsset.GetResource().GetOrCreateGraph();

                    foreach (var initialize in graph.children.OfType<VFXBasicInitialize>())
                    {
                        var dataParticle = initialize.GetData() as VFXDataParticle;
                        Assert.IsNotNull(dataParticle);
                        Assert.AreEqual(BoundsSettingMode.Manual, dataParticle.boundsMode);
                    }

                    Assert.IsTrue(graph.children.OfType<VFXAbstractRenderedOutput>().Any());
                    Assert.IsTrue(graph.UIInfos.stickyNoteInfos.Length > 0);
                }
            }
            m_CurrentMatch = null;

            AssetDatabase.DeleteAsset(kSampleExpectedPath);
            CompilationPipeline.RequestScriptCompilation();
            yield return new WaitForDomainReload();
        }
    }
}
