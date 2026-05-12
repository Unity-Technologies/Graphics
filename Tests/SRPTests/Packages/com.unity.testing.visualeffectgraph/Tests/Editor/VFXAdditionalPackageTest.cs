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

        [Test]
        public void ImportSampleDependencies_Reflection_Still_Valid()
        {
            var packageInfo = PackageManager.PackageInfo.FindForAssetPath(VisualEffectGraphPackageInfo.assetPackagePath);
            var sample = Sample.FindByPackage(VisualEffectGraphPackageInfo.name, null).FirstOrDefault();
            Assert.IsNotNull(packageInfo);
            Assert.IsNotNull(sample);
            VFXTemplateHelperInternal.ImportSampleDependencies(packageInfo, sample);
        }

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

            //Workaround for UUM-63664
            var current = matching[0];
            {
                VFXTemplateHelperInternal.ImportSampleDependencies(searchRequest.Result[0], current);
            }

            var result = current.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
            Assert.IsTrue(result);

            CompilationPipeline.RequestScriptCompilation();
            yield return new WaitForDomainReload();

            Assert.IsTrue(Directory.Exists(kSampleExpectedPath));

            bool checkOpenWindow = false;
#if VFX_TESTS_HAS_URP
            checkOpenWindow = m_CurrentMatch == "Learning";
#endif
            if (checkOpenWindow)
            {
                //This setup repro issue leading to corrupted VFX after import, see PR #84641
#if VFX_TESTS_HAS_URP
                var sceneGuid = AssetDatabase.FindAssets("t:scene URP_", new[] {kSampleExpectedPath}).Single();
#else
                var sceneGuid = AssetDatabase.FindAssets("t:scene HDRP_", new[] { kSampleExpectedPath }).Single();
#endif
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, SceneManagement.OpenSceneMode.Single);
                foreach (var guid in AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { kSampleExpectedPath }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                    var resource = vfxAsset.GetResource();

                    var window = VFXTestCommon.GetWindow(resource, true, true);
                    window.LoadResource(resource, null);
                    yield return null;

                    Assert.AreNotEqual(0, window.graphView.controller.AllSlotContainerControllers.Count());
                    window.Close();
                }
            }

            if (m_CurrentMatch == "Learning")
            {
                //Extra check for learning sample consistency
                foreach (var guid in AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { kSampleExpectedPath }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                    var graph = vfxAsset.GetResource().GetGraph();

                    foreach (var initialize in graph.children.OfType<VFXBasicInitialize>())
                    {
                        var dataParticle = initialize.GetData() as VFXDataParticle;
                        Assert.IsNotNull(dataParticle);
                        Assert.AreEqual(BoundsSettingMode.Manual, dataParticle.boundsMode, "Failure at " + path);
                    }

                    Assert.IsTrue(graph.children.OfType<VFXAbstractRenderedOutput>().Any(), "Failure at " + path);
                    Assert.IsTrue(graph.UIInfos.stickyNoteInfos.Length > 0, "Failure at " + path);
                }
            }

            m_CurrentMatch = null;

            AssetDatabase.DeleteAsset(kSampleExpectedPath);
            CompilationPipeline.RequestScriptCompilation();
            yield return new WaitForDomainReload();
        }
    }
}
