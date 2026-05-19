using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.VFX.Operator;
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
            SanitizeTest_PostProcessor.Enabled = false;
        }

        [SetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            VFXTestCommon.DeleteAllTemporaryGraph();
            SanitizeTest_PostProcessor.Enabled = true;
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
            "G_4", //Cover UUM-99973
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
                vfx.GetResource().GetGraph().PrepareGraph();
                vfx.GetResource().GetGraph().UpdateSubAssets();
                vfx.GetResource().WriteAsset();
            }
            yield return null;

            var allSubGraph = AssetDatabase.FindAssets("t:VisualEffectSubgraphBlock t:VisualEffectSubgraphOperator", new[] { "Assets/TmpTests" }).ToArray();
            foreach (var guid in allSubGraph)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectObject>(path);
                vfx.GetResource().GetGraph().PrepareGraph();
                vfx.GetResource().GetGraph().UpdateSubAssets();
                vfx.GetResource().WriteAsset();
            }

            //Trying to open VFXViewWindows (caught potential invalid states)
            var allVFXObject = AssetDatabase.FindAssets("t:VisualEffectObject", new[] { "Assets/TmpTests" }).ToArray();
            Assert.AreNotEqual(0, allVFXObject);

            foreach (var guid in allVFXObject)
            {
                var window = VFXTestCommon.GetViewWindow();
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
            var rootGraph = rootAsset.GetResource().GetGraph();
            Assert.IsNotNull(rootGraph);
            rootGraph.PrepareGraph();
            rootAsset.GetResource().WriteAsset();
            yield return null;

            var childResource = VisualEffectResource.GetResourceAtPath(childPath);
            Assert.IsNotNull(childResource);
            var childGraph = childResource.GetGraph();
            Assert.IsNotNull(childGraph);
            childGraph.PrepareGraph();
            childResource.WriteAsset();
            yield return null;
        }

        public static readonly bool[] kFalseOrTrue = { false, true };

        [UnityTest, Description("Cover UUM-99970")]
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
                var rootGraph = rootAsset.GetResource().GetGraph();
                Assert.IsNotNull(rootGraph);
                rootGraph.PrepareGraph();
            }

            if (sanitizeChild)
            {
                var childAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(childPath);
                Assert.IsNotNull(childAsset);
                var childGraph = childAsset.GetResource().GetGraph();
                Assert.IsNotNull(childGraph);
                childGraph.PrepareGraph();
            }

            File.WriteAllText(sgAPath, contentB); //sgBPath has one exposed properties while sgAPath is empty
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            for (int i = 0; i < 4; ++i)
                yield return null;

            //Check Content manually the final content
            {
                var rootAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(rootPath);
                Assert.IsNotNull(rootAsset);

                var rootGraph = rootAsset.GetResource().GetGraph();
                rootGraph.PrepareGraph();
                Assert.IsNotNull(rootGraph);
                var subGraphContext = rootGraph.children.FirstOrDefault() as VFXSubgraphContext;
                Assert.IsNotNull(subGraphContext);
                
                rootGraph.PrepareGraph();
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

                var childGraph = childAsset.GetResource().GetGraph();
                Assert.IsNotNull(childGraph);

                var graphOutputInChild = childGraph.children.OfType<VFXComposedParticleOutput>().SingleOrDefault();
                Assert.IsNotNull(graphOutputInChild);

                childGraph.PrepareGraph();
                Assert.AreEqual(1, graphOutputInChild.inputSlots.Count);
                Assert.AreEqual("_Vector3", graphOutputInChild.inputSlots[0].fullName);

                childAsset.GetResource().WriteAsset();
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
                var rootGraph = rootAsset.GetResource().GetGraph();
                Assert.IsNotNull(rootGraph);
                rootGraph.PrepareGraph();
            }

            if (sanitizeChild)
            {
                var childAsset = VisualEffectResource.GetResourceAtPath(childPath);
                Assert.IsNotNull(childAsset);
                var childGraph = childAsset.GetGraph();
                Assert.IsNotNull(childGraph);
                childGraph.PrepareGraph();
            }

            var leafAsset = VisualEffectResource.GetResourceAtPath(leafPath);
            Assert.IsNotNull(leafAsset);
            var leafGraph = leafAsset.GetGraph();
            leafGraph.PrepareGraph();

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

        static bool[] s_stressTestPollingInVFXViewController = { false, true };

        [UnityTest, Description("Cover UUM-67336 (Reimport with window opened)")]
        public IEnumerator Reimport_Sanitize_Twice_With_Window_Opened([ValueSource(nameof(s_stressTestPollingInVFXViewController))] bool stressTest)
        {
            var valueNoiseGUID = "bdeb9303d55801f4da41a7faa98bd5f6";
            var noiseGUID = "a30aeb734589f22468d3ed89a2ecc09c";

            var reproPath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_VFXValueNoise_Sanitize.vfx_";

            var vfxGraph = VFXTestCommon.CopyTemporaryGraph(reproPath);
            var vfxPath = AssetDatabase.GetAssetPath(vfxGraph);
            var originalContent = File.ReadAllText(vfxPath);
            Assert.AreEqual(1, Regex.Matches(originalContent, valueNoiseGUID).Count);
            Assert.AreEqual(0, Regex.Matches(originalContent, noiseGUID).Count);

            yield return null;

            var resource = vfxGraph.GetResource();
            Assert.IsNotNull(resource);

            var beforeSanitizeModel = vfxGraph.children.Single();
            Assert.IsInstanceOf<VFXOperator>(beforeSanitizeModel);
            Assert.IsInstanceOf<ValueNoise>(beforeSanitizeModel);

            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(resource, null);

            for (int i = 0; i < 4; ++i)
            {
                window.graphView.controller.LightApplyChanges();
                var controller = window.graphView.controller.AllSlotContainerControllers.Single();

                Assert.IsInstanceOf<VFXOperator>(controller.model);
                Assert.IsNotInstanceOf<ValueNoise>(controller.model);
                Assert.IsInstanceOf<Noise>(controller.model);

                window.graphView.OnSave();
                if (!stressTest)
                {
                    //VFXViewController might keep reference on deleted scriptable object
                    yield return null;
                }

                var newContent = File.ReadAllText(vfxPath);
                Assert.AreNotEqual(originalContent, newContent);
                Assert.AreEqual(0, Regex.Matches(newContent, valueNoiseGUID).Count);
                Assert.AreEqual(1, Regex.Matches(newContent, noiseGUID).Count);
                if (!stressTest)
                {
                    yield return null;
                }

                File.WriteAllText(vfxPath, originalContent);
                AssetDatabase.Refresh();

                yield return null;
            }

            window.Close();
            yield return null;
        }

        class SanitizeTest_PostProcessor : AssetPostprocessor
        {
            public static bool Enabled = false;
            public static int Count = 0;
            public static string[] LastImportedAssets;
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (Enabled)
                {
                    Count++;
                    LastImportedAssets = (string[])importedAssets.Clone();
                }
            }
        }

        readonly string[] kInsure_OnPostprocessAllAssets_Chain = new[]
        {
            "A_SubgraphChain_0.shadergraph",
            "A_SubgraphChain_1.vfx",
            "A_SubgraphChain_2.vfxblock",
            "A_SubgraphChain_3.vfx"
        };

        readonly int[][] kInsure_OnPostprocessAllAssets_Dependencies = new[]
        {
            new int[] { },
            new int[] { 0, 2 },
            new int[] { },
            new int[] { 1 }
        };

        readonly string kInsure_OnPostprocessAllAssets_PackagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_SubgraphChain.unitypackage";
        readonly string kInsure_OnPostprocessAllAssets_Scene = "SubgraphChain.unity";

        public IEnumerator Insure_OnPostprocessAllAssets_Common()
        {
            SanitizeTest_PostProcessor.Count = 0;
            yield return null;
            Assert.AreEqual(0, SanitizeTest_PostProcessor.Count);

            AssetDatabase.ImportPackageImmediately(kInsure_OnPostprocessAllAssets_PackagePath);
            for (int i = 0; i < 4; i++)
                yield return null;
            Assert.AreEqual(1, SanitizeTest_PostProcessor.Count);
            SanitizeTest_PostProcessor.Count = 0;
        }

        [UnityTest, Description("Integration Test: Verify that Open VFX recompiles automatically when a VFX resource changes, and ensure that multiple post-process events are triggered during OnPostprocessAllAssets when assets change on disk.")]
        public IEnumerator Insure_OnPostprocessAllAssets_Called_Once_Per_Frame()
        {
            yield return Insure_OnPostprocessAllAssets_Common();
            SceneManagement.EditorSceneManager.OpenScene(Path.Combine(VFXTestCommon.tempBasePath, kInsure_OnPostprocessAllAssets_Scene));

            var vfxResource = VisualEffectResource.GetResourceAtPath(Path.Combine(VFXTestCommon.tempBasePath, kInsure_OnPostprocessAllAssets_Chain.Last()));
            Assert.IsNotNull(vfxResource);

            var window = VFXTestCommon.GetWindow(vfxResource, true, true);
            window.LoadResource(vfxResource, null);

            for (int i = 0; i < 4; i++)
                yield return null;
            Assert.AreEqual(0, SanitizeTest_PostProcessor.Count, "No expected import after opening the window");

            var switchAssetScenario = new[]
            {
                new[] { 0 },
                new[] { 1 },
                new[] { 1, 2 },
                new[] { 0, 1, 2 },
            };

            foreach (var scenario in switchAssetScenario)
            {
                //Experiment
                SanitizeTest_PostProcessor.Count = 0;
                foreach (var change in scenario)
                {
                    var basePath = Path.Combine(VFXTestCommon.tempBasePath, kInsure_OnPostprocessAllAssets_Chain[change]);
                    var newContentPath = basePath.Replace("A_", "B_");
                    var newContent = File.ReadAllBytes(newContentPath);
                    File.WriteAllBytes(basePath, newContent);
                }
                AssetDatabase.Refresh();

                for (int i = 0; i < 4; i++)
                    yield return null;
                Assert.AreEqual(1, SanitizeTest_PostProcessor.Count, "Expect only one import for any disk transaction");

                //Restore for next loop
                SanitizeTest_PostProcessor.Count = 0;
                AssetDatabase.ImportPackageImmediately(kInsure_OnPostprocessAllAssets_PackagePath);
                for (int i = 0; i < 4; i++)
                    yield return null;
                Assert.AreEqual(1, SanitizeTest_PostProcessor.Count);
                SanitizeTest_PostProcessor.Count = 0;
            }

            window.Close();
        }

        static GUID[] OnFilterResourceDependencies(string basePath)
        {
            var baseGuid = AssetDatabase.GUIDFromAssetPath(basePath);
            var externalRefsGuid = VisualEffectAssetUtility.GetVisualEffectExternalRefs(baseGuid);
            var externalRefsPath = externalRefsGuid.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var subAssets = VisualEffectResource.onFilterImportDependencies(externalRefsGuid, externalRefsPath, false);
            return subAssets;
        }

        [UnityTest, Description("Integration Test: Verify expected behavior from GetVisualEffectDynamicDependencies with OnPostprocessAllAssets invocation")]
        public IEnumerator Insure_OnPostprocessAllAssets_GetVisualEffectDynamicDependencies_Isolation()
        {
            yield return Insure_OnPostprocessAllAssets_Common();

            Assert.IsNotNull(VisualEffectResource.onFilterImportDependencies);
            for (int i = 1; i < kInsure_OnPostprocessAllAssets_Chain.Length; ++i) //First is SG
            {
                var basePath = Path.Combine(VFXTestCommon.tempBasePath, kInsure_OnPostprocessAllAssets_Chain[i]);
                var subAssets = OnFilterResourceDependencies(basePath);

                var expectedDependencies = kInsure_OnPostprocessAllAssets_Dependencies[i];
                Assert.AreEqual(subAssets.Length, expectedDependencies.Length);
                foreach (var expectedDependency in expectedDependencies)
                {
                    var dependencyPath = Path.Combine(VFXTestCommon.tempBasePath, kInsure_OnPostprocessAllAssets_Chain[expectedDependency]);
                    var dependencyGuid = AssetDatabase.GUIDFromAssetPath(dependencyPath);
                    Assert.IsTrue(subAssets.Contains(dependencyGuid), "Can't find dependency: {0} for asset: {1}", dependencyPath, basePath);
                }
            }
        }

        [UnityTest]
        public IEnumerator Insure_OnPostprocessAllAssets_GetVisualEffectDynamicDependencies_Isolation_CustomSpawner()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var spawnBlockDesc = VFXLibrary.GetBlocks().Single(o => o.name == ObjectNames.NicifyVariableName("IncrementStripIndexOnStart"));
            var spawnBlock = spawnBlockDesc.variant.CreateInstance();
            spawnerContext.AddChild(spawnBlock);
            graph.AddChild(spawnerContext);
            VFXTestCommon.ReimportVFXGraph(graph);
            yield return null;

            var basePath = AssetDatabase.GetAssetPath(graph);
            var subAssets = OnFilterResourceDependencies(basePath);
            Assert.AreEqual(1, subAssets.Length);

            var subAssetPath = AssetDatabase.GUIDToAssetPath(subAssets[0]);
            Assert.IsTrue(subAssetPath.EndsWith("IncrementStripIndexOnStart.cs"));
        }


        [UnityTest]
        public IEnumerator Insure_OnPostprocessAllAssets_GetVisualEffectDynamicDependencies_Isolation_CustomHLSL()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var operatorName = "Insure_OnPostprocessAllAssets_GetVisualEffectDynamicDependencies_Isolation_CustomHLSL";
            var shaderInclude = VFXTestCommon.CreateShaderFile("float add(float a, float b) { return a + b; } ", out var shaderIncludePath);
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_ShaderFile", shaderInclude);
            hlslOperator.SetSettingValue("m_OperatorName", operatorName);
            graph.AddChild(hlslOperator);
            VFXTestCommon.ReimportVFXGraph(graph);
            yield return null;

            var basePath = AssetDatabase.GetAssetPath(graph);
            var subAssets = OnFilterResourceDependencies(basePath);
            Assert.AreEqual(1, subAssets.Length);

            var subAssetPath = AssetDatabase.GUIDToAssetPath(subAssets[0]);
            Assert.AreEqual(shaderIncludePath, subAssetPath);
        }

        [UnityTest, Description("Integration Test: Verify expected behavior from GetVisualEffectDynamicDependencies with OnPostprocessAllAssets invocation")]
        public IEnumerator Insure_OnPostprocessAllAssets_Called_On_Dependency_Changed()
        {
            yield return Insure_OnPostprocessAllAssets_Common();
            VFXTestCommon.CloseAllVFXWindow();

            var mainAssetPath = Path.Combine(VFXTestCommon.tempBasePath, kInsure_OnPostprocessAllAssets_Chain.Last());
            foreach (var assetChain in kInsure_OnPostprocessAllAssets_Chain)
            {
                SanitizeTest_PostProcessor.Count = 0;
                SanitizeTest_PostProcessor.LastImportedAssets = Array.Empty<string>();

                var basePath = Path.Combine(VFXTestCommon.tempBasePath, assetChain);
                var newContentPath = basePath.Replace("A_", "B_");
                var newContent = File.ReadAllBytes(newContentPath);
                File.WriteAllBytes(basePath, newContent);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                yield return null;

                Assert.AreEqual(1, SanitizeTest_PostProcessor.Count, "Expect only one import for any disk transaction: {0}", assetChain);
                Assert.IsTrue(SanitizeTest_PostProcessor.LastImportedAssets.Contains(basePath), "Can't find subAsset: {0}", assetChain);
                Assert.IsTrue(SanitizeTest_PostProcessor.LastImportedAssets.Contains(mainAssetPath), "Can't find mainAsset: {0} after change of: {1}", mainAssetPath, basePath);

                SanitizeTest_PostProcessor.Count = 0;
                AssetDatabase.ImportPackageImmediately(kInsure_OnPostprocessAllAssets_PackagePath);
                yield return null;
                Assert.AreEqual(1, SanitizeTest_PostProcessor.Count);
            }
        }

        static bool FindInShaderSource(VisualEffectResource vfxResource, string match)
        {
            for (int shaderIndex = 0; shaderIndex < vfxResource.GetShaderSourceCount(); ++shaderIndex)
            {
                var shaderSource = vfxResource.GetShaderSource(shaderIndex);
                if (shaderSource.Contains(match))
                    return true;
            }

            return false;
        }

        [UnityTest, Description("Cover UUM-133319")]
        public IEnumerator Repro_Simplest_Subgraph_Chain()
        {
            VFXTestCommon.CloseAllVFXWindow();
            AssetDatabase.ImportPackageImmediately("Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_Simplest_Subgraph_Chain.unitypackage");
            AssetDatabase.Refresh();
            yield return null;

            var mainGraphPath = Path.Combine(VFXTestCommon.tempBasePath, "Repro_Simplest_Subgraph_Chain.vfx");
            var subGraphPath = Path.Combine(VFXTestCommon.tempBasePath, "Repro_Simplest_Subgraph_Chain.vfxblock");

            var mainGraphAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(mainGraphPath);
            var subGraphAsset = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraph>(subGraphPath);
            Assert.IsNotNull(mainGraphAsset);
            Assert.IsNotNull(subGraphAsset);

            Assert.IsTrue(FindInShaderSource(mainGraphAsset.GetResource(), "void Gravity"));

            var subgraphContext = subGraphAsset.GetResource().GetGraph().children.Single();
            var gravity = subgraphContext.children.OfType<Block.Gravity>().Single();
            subgraphContext.RemoveChild(gravity);
            subGraphAsset.GetResource().WriteAsset();
            yield return null;

            Assert.IsFalse(FindInShaderSource(mainGraphAsset.GetResource(), "void Gravity"));
        }
    }
}
