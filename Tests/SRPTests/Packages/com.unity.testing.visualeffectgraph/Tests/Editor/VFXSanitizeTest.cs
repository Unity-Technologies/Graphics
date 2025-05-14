using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSanitizeTest
    {
        [TearDown]
        public void CleanUp()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [SetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest]
        public IEnumerator Check_SetCustomAttribute_Sanitize()
        {
            // No assert because if there's at least one error message in the console during the asset import+sanitize the test will fail
            var filePath = "Packages/com.unity.testing.visualeffectgraph/scenes/103_Lit.vfxtmp";
            var graph = VFXTestCommon.CopyTemporaryGraph(filePath);
            for (int i = 0; i < 16; i++)
                yield return null;
            Assert.IsNotNull(graph);
        }

        [UnityTest,
#if VFX_TESTS_HAS_URP
    Ignore("See UUM-66527")
#endif
        ]
        public IEnumerator Insure_Templates_Are_Up_To_Date()
        {
            var allTemplatesGUI = AssetDatabase.FindAssets("t:VisualEffectAsset", new []{ "Packages/com.unity.visualeffectgraph" });
            var templatePath = new List<string>();
            foreach (var guid in allTemplatesGUI)
            {
                var currentPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(currentPath);

                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(currentPath);
                Assert.IsNotNull(asset);

                var resource = asset.GetResource();
                EditorUtility.SetDirty(resource);
                AssetDatabase.ImportAsset(currentPath);

                templatePath.Add(currentPath);
            }
            AssetDatabase.SaveAssets();
            Assert.AreNotEqual(0, templatePath.Count);
            yield return null;

            using (var process = new System.Diagnostics.Process())
            {
                var rootPath = Path.Combine(Application.dataPath, "../../../../../");
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    FileName = "git",
                    Arguments = "diff Packages/com.unity.visualeffectgraph/**",
                    WorkingDirectory = rootPath
                };

                var outputBuilder = new StringBuilder();
                var errorsBuilder = new StringBuilder();
                process.OutputDataReceived += (_, args) => outputBuilder.AppendLine(args.Data);
                process.ErrorDataReceived += (_, args) => errorsBuilder.AppendLine(args.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                var output = outputBuilder.ToString().TrimEnd();
                var errors = errorsBuilder.ToString().TrimEnd();

                Assert.AreEqual(0, process.ExitCode);
                Assert.AreEqual(string.Empty, errors);
                Assert.AreEqual(string.Empty, output, output);
            }
            yield return null;
        }

        //These data are isolated repro from SpaceShip Demo at e00f4b352f08f7b5ac97b264befe9d45777ba1ef (generated with 2023.3.0b8 (d25f56a800ee))
        private static readonly string[] kScenarios = new[]
        {
            "A_4",
            "B_2",
            "C_3",
            "D_3",
            "E_2",
            "F_4",
            //"G_4", //Cover UUM-99973, fixed by PR #57335
            //"H_61"
        };


        [UnityTest, Timeout(360 * 1000), Ignore("Only a local test, too long to be run on Yamato.")]
        public IEnumerator Sanitize_Subgraph_Scenario_All()
        {
            var packagePath = $"Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/VFXSubgraphRepro_H_61.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);
            VFXAssetManager.BuildAndSave();
            for (int i = 0; i < 4; ++i)
                yield return null;
        }

        [UnityTest, Timeout(360 * 1000)]
        public IEnumerator Sanitize_Subgraph_Scenario([ValueSource(nameof(kScenarios))] string scenario)
        {
            var packagePath = $"Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/VFXSubgraphRepro_{scenario}.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            yield return null;

            //Sanitize
            Assert.IsTrue(int.TryParse(scenario.Substring(scenario.LastIndexOf('_')+1), out var expectedVFXCount));
            var allVFXAsset = AssetDatabase.FindAssets("t:VisualEffectAsset", new[] {"Assets/TmpTests"}).ToArray();
            Assert.AreNotEqual(0, expectedVFXCount);
            Assert.AreEqual(expectedVFXCount, allVFXAsset.Length);
            foreach (var guid in allVFXAsset)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                vfx.GetOrCreateResource().GetOrCreateGraph().SanitizeForImport();
                vfx.GetOrCreateResource().GetOrCreateGraph().UpdateSubAssets();
                vfx.GetOrCreateResource().WriteAsset();
            }
            yield return null;

            var allSubGraph = AssetDatabase.FindAssets("t:VisualEffectSubgraphBlock t:VisualEffectSubgraphOperator", new[] { "Assets/TmpTests" }).ToArray();
            foreach (var guid in allSubGraph)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectObject>(path);
                vfx.GetOrCreateResource().GetOrCreateGraph().SanitizeForImport();
                vfx.GetOrCreateResource().GetOrCreateGraph().UpdateSubAssets();
                vfx.GetOrCreateResource().WriteAsset();
            }

            //Trying to open VFXViewWindows (caught potential invalid states)
            var allVFXObject = AssetDatabase.FindAssets("t:VisualEffectObject", new[] { "Assets/TmpTests" }).ToArray();
            Assert.AreNotEqual(0, allVFXObject);

            foreach (var guid in allVFXObject)
            {
                var window = VFXViewWindow.GetWindow<VFXViewWindow>();
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var resource = VisualEffectResource.GetResourceAtPath(path);
                Assert.IsNotNull(resource);
                window.LoadResource(resource);
                for (int i = 0; i < 4; ++i)
                    yield return null;

#if VFX_TESTS_HAS_HDRP
                if (path.EndsWith(".vfx", StringComparison.OrdinalIgnoreCase)
                    //Mesh output only
                    && !path.Contains("BridgeTable.vfx", StringComparison.InvariantCultureIgnoreCase)
                    && !path.Contains("Outliner.vfx", StringComparison.InvariantCultureIgnoreCase)
                    && !path.Contains("Monitor.vfx", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    Assert.AreNotEqual(0, resource.GetShaderSourceCount(), "No compute at path: " + path);
                    var firstCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
                    Assert.IsNotNull(firstCompute, "No compute at path: " + path);
                }
#endif
                window.Close();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Subgraph_Block_Which_Uses_GetData()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_Subgraph_With_Euler.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            yield return null;
            var rootPath = VFXTestCommon.tempBasePath + "Root.vfx";
            var childPath = VFXTestCommon.tempBasePath + "Child.vfxblock";

            var rootAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(rootPath);
            Assert.IsNotNull(rootAsset);
            var rootGraph = rootAsset.GetOrCreateResource().GetOrCreateGraph();
            Assert.IsNotNull(rootGraph);
            rootGraph.SanitizeForImport();
            rootAsset.GetOrCreateResource().WriteAsset();
            yield return null;

            var childResource = VisualEffectResource.GetResourceAtPath(childPath);
            Assert.IsNotNull(childResource);
            var childGraph = childResource.GetOrCreateGraph();
            Assert.IsNotNull(childGraph);
            childGraph.SanitizeForImport();
            childResource.WriteAsset();
            yield return null;
        }

        public static readonly bool[] kFalseOrTrue = { false, true };

        [UnityTest, Ignore("Cover UUM-99970, fixed by PR #57335")]
        public IEnumerator Change_SG_Exposed_Properties_With_Order_Two_Subgraph([ValueSource(nameof(kFalseOrTrue))] bool sanitizeRoot, [ValueSource(nameof(kFalseOrTrue))] bool sanitizeChild)
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_SG_Subgraph_Missing_Sanitize.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var sgAPath = VFXTestCommon.tempBasePath + "VFX_SG_A.shadergraph";
            var sgBPath = VFXTestCommon.tempBasePath + "VFX_SG_B.shadergraph";
            var rootPath = VFXTestCommon.tempBasePath + "VFX_Root.vfx";
            var childPath = VFXTestCommon.tempBasePath + "VFX_Child.vfx";
            yield return null;

            var contentB = File.ReadAllText(sgBPath);
            Assert.IsFalse(string.IsNullOrEmpty(contentB));
            yield return null;

            if (sanitizeRoot)
            {
                var rootAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(rootPath);
                Assert.IsNotNull(rootAsset);
                var rootGraph = rootAsset.GetOrCreateResource().GetOrCreateGraph();
                Assert.IsNotNull(rootGraph);
                rootGraph.SanitizeForImport();
            }

            if (sanitizeChild)
            {
                var childAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(childPath);
                Assert.IsNotNull(childAsset);
                var childGraph = childAsset.GetOrCreateResource().GetOrCreateGraph();
                Assert.IsNotNull(childGraph);
                childGraph.SanitizeForImport();
            }

            File.WriteAllText(sgAPath, contentB); //sgBPath has one exposed properties while sgAPath is empty
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            for (int i = 0; i < 4; ++i)
                yield return null;

            //Check Content manually the final content
            {
                var rootAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(rootPath);
                Assert.IsNotNull(rootAsset);

                var rootGraph = rootAsset.GetOrCreateResource().GetOrCreateGraph();
                Assert.IsNotNull(rootGraph);
                Assert.AreEqual(sanitizeRoot, rootGraph.sanitized);
                var subGraphContext = rootGraph.children.FirstOrDefault() as VFXSubgraphContext;
                Assert.IsNotNull(subGraphContext);

                rootGraph.SanitizeForImport();
                Assert.IsNotNull(subGraphContext.subChildren);
                var graphOutputInRoot = subGraphContext.subChildren.OfType<VFXComposedParticleOutput>().SingleOrDefault();
                Assert.IsNotNull(graphOutputInRoot);

                var sg = graphOutputInRoot.GetShaderGraph();
                Assert.IsNotNull(sg);

                Assert.AreEqual(1, graphOutputInRoot.inputSlots.Count);
                Assert.AreEqual("_Vector3", graphOutputInRoot.inputSlots[0].fullName);
            }

            {
                var childAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(childPath);
                Assert.IsNotNull(childAsset);

                var childGraph = childAsset.GetOrCreateResource().GetOrCreateGraph();
                Assert.IsNotNull(childGraph);
                Assert.AreEqual(sanitizeChild, childGraph.sanitized);

                var graphOutputInChild = childGraph.children.OfType<VFXComposedParticleOutput>().SingleOrDefault();
                Assert.IsNotNull(graphOutputInChild);

                childGraph.SanitizeForImport();
                Assert.AreEqual(1, graphOutputInChild.inputSlots.Count);
                Assert.AreEqual("_Vector3", graphOutputInChild.inputSlots[0].fullName);

                childAsset.GetOrCreateResource().WriteAsset();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Change_VFX_Exposed_Properties_With_Order_Two_Subgraph([ValueSource(nameof(kFalseOrTrue))] bool sanitizeRoot, [ValueSource(nameof(kFalseOrTrue))] bool sanitizeChild)
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_VFX_SubgraphBlocks.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            yield return null;

            var rootPath = VFXTestCommon.tempBasePath + "VFX_SubGraph_0.vfx";
            var childPath = VFXTestCommon.tempBasePath + "VFX_SubGraph_1.vfxblock";
            var leafPath = VFXTestCommon.tempBasePath + "VFX_SubGraph_2.vfxblock";

            if (sanitizeRoot)
            {
                var rootAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(rootPath);
                Assert.IsNotNull(rootAsset);
                var rootGraph = rootAsset.GetOrCreateResource().GetOrCreateGraph();
                Assert.IsNotNull(rootGraph);
                rootGraph.SanitizeForImport();
            }

            if (sanitizeChild)
            {
                var childAsset = VisualEffectResource.GetResourceAtPath(childPath);
                Assert.IsNotNull(childAsset);
                var childGraph = childAsset.GetOrCreateGraph();
                Assert.IsNotNull(childGraph);
                childGraph.SanitizeForImport();
            }

            var leafAsset = VisualEffectResource.GetResourceAtPath(leafPath);
            Assert.IsNotNull(leafAsset);
            var leafGraph = leafAsset.GetOrCreateGraph();
            leafGraph.SanitizeForImport();

            var viewController = VFXViewController.GetController(leafAsset, true);
            viewController.useCount++;
            viewController.LightApplyChanges();
            var colorController = viewController.AllSlotContainerControllers.SingleOrDefault(o => o.model is VFXInlineOperator) as VFXOperatorController;
            Assert.IsNotNull(colorController);
            var parameter = colorController.ConvertToProperty(true);
            Assert.IsNotNull(parameter);
            leafAsset.WriteAsset();

            Assert.AreEqual(1, viewController.useCount);
            viewController.useCount--;

            yield return null;
        }
    }
}
