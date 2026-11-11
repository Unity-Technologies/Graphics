using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Moq;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.UI.WindowManagement
{
    [TestFixture]
    public class VFXWindowSaveAndRevertTests
    {
        public bool m_GenerateShadersWithDebugSymbols;

        [OneTimeSetUp]
        public void Setup()
        {
            m_GenerateShadersWithDebugSymbols = VFXViewPreference.generateShadersWithDebugSymbols;

            EditorPrefs.SetBool(VFXGlobalImportSettings.generateShadersWithDebugSymbolsKey, false);
            VFXViewPreference.SetDirty();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            VFXTestCommon.DeleteAllTemporaryGraph();

            EditorPrefs.SetBool(VFXGlobalImportSettings.generateShadersWithDebugSymbolsKey, m_GenerateShadersWithDebugSymbols);
            VFXViewPreference.SetDirty();
        }

        [TearDown]
        public void TearDown()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            VFXTestCommon.CloseAllVFXWindow();
        }

        // The window syncs its unsaved state from the dirty graph on its Update tick, so wait for it.
        static IEnumerator WaitUntilWindowIsDirty(VFXViewWindow window, string message)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!window.hasUnsavedChanges && sw.ElapsedMilliseconds < 5000)
                yield return null;
            Assert.IsTrue(window.hasUnsavedChanges, message);
        }

        [UnityTest, Description("Cover UUM-131449")]
        public IEnumerator Save_Then_Reimport_Redirties_Graph_Does_Not_Prompt_On_Close()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(graph.visualEffectResource, null);
            yield return null;

            var mockView = new Mock<IVFXViewEditorAssetEventHandler>();
            mockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(AskAssetChangedBeforeCloseChoice.Save);
            window.graphView.AssetEventHandler = mockView.Object;

            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            initialize.SetSettingValue("capacity", 431u);
            yield return null;
            Assert.IsTrue(window.hasUnsavedChanges);

            window.SaveChanges();
            Assert.IsFalse(window.hasUnsavedChanges);

            EditorUtility.SetDirty(graph);
            Assert.IsTrue(EditorUtility.IsDirty(graph));
            Assert.IsFalse(window.hasUnsavedChanges);

            window.Close();
            yield return null;
            mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Never);
        }

        [UnityTest, Description("Cover UUM-131449: Save All batches per-window VFX imports into a single import")]
        public IEnumerator Save_All_Batches_Imports_Into_Single_Pass()
        {
            const int count = 4;
            var windows = new List<VFXViewWindow>();
            for (int i = 0; i < count; i++)
            {
                var graph = VFXTestCommon.CreateGraph_And_System();
                var window = VFXTestCommon.GetWindow(graph.visualEffectResource, true, true);
                window.LoadResource(graph.visualEffectResource);
                windows.Add(window);
                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                windows[i].Focus();
                yield return null;
                var controller = windows[i].graphView.controller;
                controller.graph.children.OfType<VFXBasicInitialize>().Single().SetSettingValue("capacity", (uint)(1000 + i));
                controller.LightApplyChanges();
                yield return WaitUntilWindowIsDirty(windows[i], $"Window {i} did not become dirty.");
            }

            VFXTestImportPassCounter.Passes = 0;
            VFXTestImportPassCounter.Armed = true;
            try
            {
                // Mirror ContainerWindow.AskToClose "Save All": save every window inside one import batch.
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var window in windows)
                        window.SaveChanges();
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
                yield return null;
            }
            finally
            {
                VFXTestImportPassCounter.Armed = false;
            }

            foreach (var window in windows)
                Assert.IsFalse(window.hasUnsavedChanges, "Save All did not clear a window's unsaved state.");
            Assert.Less(VFXTestImportPassCounter.Passes, count,
                "Save All should flush every VFX write in one import pass; a pass per window means the imports regressed to un-batched.");
        }

        [UnityTest, Description("Cover UUM-131449: Save All of graphs sharing a subgraph stays batched (no N*M import fan-out)")]
        public IEnumerator Save_All_With_Shared_Subgraph_Stays_Batched()
        {
            const int count = 4;
            var subgraph = VFXTestCommon.MakeTemporarySubGraphOperator();

            var windows = new List<VFXViewWindow>();
            for (int i = 0; i < count; i++)
            {
                var graph = VFXTestCommon.CreateGraph_And_System();
                var window = VFXTestCommon.GetWindow(graph.visualEffectResource, true, true);
                window.LoadResource(graph.visualEffectResource);
                windows.Add(window);
                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                windows[i].Focus();
                yield return null;
                var controller = windows[i].graphView.controller;
                // Reference the shared subgraph so the graphs are interdependent (the N*M-prone case).
                var subgraphOperator = ScriptableObject.CreateInstance<VFXSubgraphOperator>();
                subgraphOperator.SetSettingValue("m_Subgraph", subgraph);
                controller.graph.AddChild(subgraphOperator);
                // Tweak a setting so the window is reliably dirty.
                controller.graph.children.OfType<VFXBasicInitialize>().Single().SetSettingValue("capacity", (uint)(2000 + i));
                controller.LightApplyChanges();
                yield return WaitUntilWindowIsDirty(windows[i], $"Window {i} did not become dirty.");
            }

            VFXTestImportPassCounter.Passes = 0;
            VFXTestImportPassCounter.Armed = true;
            try
            {
                // Mirror ContainerWindow.AskToClose "Save All": save every window inside one import batch.
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var window in windows)
                        window.SaveChanges();
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
                yield return null;
            }
            finally
            {
                VFXTestImportPassCounter.Armed = false;
            }

            foreach (var window in windows)
                Assert.IsFalse(window.hasUnsavedChanges, "Save All did not clear a window's unsaved state.");
            Assert.Less(VFXTestImportPassCounter.Passes, count,
                "Graphs sharing a subgraph should still flush in one import pass; a pass per window is the N*M regression.");
        }

        static bool[] kRepro_Modify_Save_Modify_With_Window_OpenedCase = { false, true };
        [UnityTest, Description("Cover WriteAssetWithSubAssets which can trigger an reimport switch asset from editor to runtime")]
        public IEnumerator Modify_Save_Modify_With_Window_Opened([ValueSource(nameof(kRepro_Modify_Save_Modify_With_Window_OpenedCase))] bool useReimport)
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(graph.visualEffectResource, null);
            yield return null;

            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            var setAttribute = initialize.children.Single();

            var currentValue = setAttribute.inputSlots[0].value;
            Assert.IsInstanceOf<Position>(currentValue);

            var currentPosition = (Position)currentValue;
            currentPosition.position += Vector3.forward;

            Assert.IsFalse(EditorUtility.IsDirty(graph));
            setAttribute.inputSlots[0].value = currentPosition;
            Assert.IsTrue(EditorUtility.IsDirty(graph));

            if (useReimport)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
                Assert.IsTrue(EditorUtility.IsDirty(graph));
            }
            else
            {
                //Explicit Save Scenario (which also triggers a reimport)
                window.graphView.OnSave();
                Assert.IsFalse(EditorUtility.IsDirty(graph));
            }

            currentPosition.position += Vector3.forward;
            setAttribute.inputSlots[0].value = currentPosition;

            for (int i = 0; i < 4; ++i)
                yield return null;

            window.Close();
            yield return null;
        }

        static object[] kAskSaveBehaviors =
            Enum.GetValues(typeof(AskAssetChangedBeforeCloseChoice))
                .OfType<object>().ToArray();

        static bool FindExpectedCapacityOnDisk(VFXGraph graph, uint expectedCapacity)
        {
            var expectedStringOnDisk = $"capacity: {expectedCapacity}";
            var assetPath = AssetDatabase.GetAssetPath(graph);
            var fileContent = File.ReadAllText(assetPath);
            var matchCount = Regex.Matches(fileContent, expectedStringOnDisk).Count;
            return matchCount == 1;
        }

        static bool FindExpectedCapacityOnMemory(VFXGraph graph, uint expectedCapacity)
        {
            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            var readCapacity = (uint)initialize.GetSettingValue("capacity");
            return readCapacity == expectedCapacity;
        }

        [UnityTest, Description("Cover Nodal Window behavior asking for save")]
        public IEnumerator Modify_Graph_And_Check_Nodal_Ask_Asset_Changed([ValueSource(nameof(kAskSaveBehaviors))] object askAssetChangedBeforeCloseChoiceObject)
        {
            var choice = (AskAssetChangedBeforeCloseChoice)askAssetChangedBeforeCloseChoiceObject;

            var graph = VFXTestCommon.CreateGraph_And_System();
            var resource = graph.GetResource();
            var vfxPath = AssetDatabase.GetAssetPath(resource);
            Assert.IsFalse(FindExpectedCapacityOnDisk(graph, 349u));
            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            initialize.SetSettingValue("capacity", 349u);
            VFXTestCommon.ReimportVFXGraph(graph);
            Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 349u));

            VFXTestCommon.CloseAllVFXWindow();
            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(graph.visualEffectResource, null);
            yield return null;

            var mockView = new Mock<IVFXViewEditorAssetEventHandler>();
            mockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(choice);
            window.graphView.AssetEventHandler = mockView.Object;

            Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 349u));

            Assert.IsFalse(EditorUtility.IsDirty(graph));
            initialize.SetSettingValue("capacity", 431u);
            Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 431u));
            Assert.IsTrue(EditorUtility.IsDirty(graph));
            yield return null;

            var currentCompilationMode = VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset);
            Assert.AreEqual(VFXCompilationMode.Edition, currentCompilationMode);

            window.Close();
            yield return null;
            mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Once);
            resource = VisualEffectResource.GetResourceAtPath(vfxPath);
            graph = resource.GetGraph();

            currentCompilationMode = VisualEffectAssetUtility.GetCompilationMode(resource.asset);
            if (choice == AskAssetChangedBeforeCloseChoice.Save)
            {
                Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 431u));
                Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 431u));
                Assert.IsFalse(EditorUtility.IsDirty(graph));
                Assert.AreEqual(VFXCompilationMode.Runtime, currentCompilationMode);
                Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 0);
            }
            else if (choice == AskAssetChangedBeforeCloseChoice.Cancel)
            {
                Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 349u));
                Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 431u));
                Assert.IsTrue(EditorUtility.IsDirty(graph));
                Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 1);
                Assert.AreEqual(VFXCompilationMode.Edition, currentCompilationMode);
                Assert.AreEqual(graph, VFXViewWindow.GetAllWindows().First().graphView.controller.graph);
            }
            else if (choice == AskAssetChangedBeforeCloseChoice.Discard)
            {
                Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 349u));
                Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 349u));
                Assert.IsFalse(EditorUtility.IsDirty(graph));
                Assert.AreEqual(VFXCompilationMode.Runtime, currentCompilationMode);
                Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 0);
            }
            else if (choice == AskAssetChangedBeforeCloseChoice.Ignore)
            {
                //Same than cancel but view is closed, this is only valid in test
                Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 349u));
                Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 431u));
                Assert.IsTrue(EditorUtility.IsDirty(graph));
                Assert.AreEqual(VFXCompilationMode.Edition, currentCompilationMode);
                Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 0);
            }
            else
            {
                Assert.Fail("Unexpected choice: " + choice);
            }

            //Try reopen, check dirty status and close
            if (VFXViewWindow.GetAllWindows().Count == 0)
            {
                window = VFXTestCommon.GetViewWindow();
                window.LoadResource(graph.visualEffectResource, null);
                yield return null;

                if (choice != AskAssetChangedBeforeCloseChoice.Ignore)
                {
                    Assert.IsFalse(EditorUtility.IsDirty(graph));
                }
                else
                {
                    Assert.IsTrue(EditorUtility.IsDirty(graph));
                }

                window.graphView.AssetEventHandler = mockView.Object;

                if (choice == AskAssetChangedBeforeCloseChoice.Ignore)
                {
                    yield return WaitUntilWindowIsDirty(window, "Reopened window did not reflect the dirty graph before close.");
                }

                window.Close();
                yield return null;

                if (choice != AskAssetChangedBeforeCloseChoice.Ignore)
                {
                    mockView.VerifyNoOtherCalls();
                }
                else
                {
                    mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Exactly(2));
                }
            }
            VFXTestCommon.CloseAllVFXWindow();
        }

        [UnityTest, Description("Cover Nodal Window revert, insure that cache value are invalidated")]
        public IEnumerator Modify_Graph_And_Check_On_Enable_Invoked()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var vfxPath = AssetDatabase.GetAssetPath(graph);
            var resource = graph.visualEffectResource;
            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            var setAttribute = initialize.children.Single();

            VFXTestCommon.CloseAllVFXWindow();
            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(graph.visualEffectResource, null);
            yield return null;
            Assert.IsTrue(setAttribute.enabled);

            Assert.IsFalse(EditorUtility.IsDirty(graph));
            setAttribute.activationSlot.value = false;
            Assert.IsTrue(EditorUtility.IsDirty(graph));
            yield return null;

            Assert.IsFalse(setAttribute.enabled);
            var currentCompilationMode = VisualEffectAssetUtility.GetCompilationMode(resource.asset);
            Assert.AreEqual(VFXCompilationMode.Edition, currentCompilationMode);

            var mockView = new Mock<IVFXViewEditorAssetEventHandler>();
            mockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(AskAssetChangedBeforeCloseChoice.Discard);
            window.graphView.AssetEventHandler = mockView.Object;

            window.Close();
            yield return null;
            mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Once);

            //Former SetAttribute block is expected to be deleted by previous ReloadFromDisk
            Assert.IsTrue(setAttribute == null);
            Assert.IsFalse(System.Object.ReferenceEquals(setAttribute, null));
            resource = VisualEffectResource.GetResourceAtPath(vfxPath);
            graph = resource.GetGraph();
            initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            setAttribute = initialize.children.Single();

            currentCompilationMode = VisualEffectAssetUtility.GetCompilationMode(resource.asset);
            Assert.IsFalse(EditorUtility.IsDirty(resource));
            Assert.AreEqual(VFXCompilationMode.Runtime, currentCompilationMode);
            Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 0);
            Assert.IsTrue(setAttribute.enabled, "SetAttribute is probably still caching former enable state.");

            yield return null;
        }

        static readonly string[] kOpen_Graph_With_Missing_Reference_And_DiscardCaseSource =
        {
            "Repro_Missing_Reference_HDRP.vfx_",
            "Repro_Missing_Reference_URP.vfx_",
            "Repro_Old_Camera_Reference.vfx_"
        };

        static readonly bool[] kOpen_Graph_With_Missing_Reference_And_DiscardCaseModify =
        {
            false,
            //Cover an issue where modifying activation slot won't be restored in its initial state while loading Repro_Old_Camera_Reference
            //m_ActivationSlot is kept after ReloadFromDisk, m_ActivationSlot being a new field between 22.3 & current version.
            true
        };

        [UnityTest, Description("Cover revert on disk with missing data")]
        public IEnumerator Open_Graph_With_Missing_Reference_And_Discard(
            [ValueSource(nameof(kOpen_Graph_With_Missing_Reference_And_DiscardCaseSource))] string sourceFile,
            [ValueSource(nameof(kOpen_Graph_With_Missing_Reference_And_DiscardCaseModify))] bool modifyActivationSlot)
        {
            var sourcePath = Path.Combine("Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/", sourceFile);
            var originalContent = File.ReadAllText(sourcePath);

            var vfxGraph = VFXTestCommon.CopyTemporaryGraph(sourcePath);
            var vfxPath = AssetDatabase.GetAssetPath(vfxGraph);
            var vfxAsset = vfxGraph.GetResource().asset;
            Assert.IsNotNull(vfxAsset);
            Assert.IsNotNull(vfxPath);
            Assert.AreEqual(originalContent, File.ReadAllText(vfxPath));

            VFXTestCommon.CloseAllVFXWindow();

            for (int i = 0; i < 2; i++)
            {
                var previousContent = File.ReadAllText(vfxPath);

                vfxGraph = vfxAsset.GetResource().GetGraph();
                Assert.AreEqual(0, VFXViewWindow.GetAllWindows().Count());
                Assert.IsFalse(EditorUtility.IsDirty(vfxGraph));

                var window = VFXTestCommon.GetWindow(vfxGraph, true, true);
                window.LoadResource(vfxGraph.visualEffectResource, null);
                yield return null;

                bool isThereASanitize = EditorUtility.IsDirty(vfxGraph);
                if (sourceFile.StartsWith("Repro_Missing_Reference"))
                {
                    //N.B.: This following test is only valid until we are changing something in models used in Repro_Missing_Reference
                    Assert.IsFalse(isThereASanitize);
                }

                if (modifyActivationSlot)
                {
                    var basicUpdate = vfxGraph.children.OfType<VFXBasicUpdate>().First(o => o.GetNbChildren() == 1);
                    var block = basicUpdate.children.First();
                    Assert.IsTrue((bool)block.activationSlot.value, "Revert failure at iteration: " + i);
                    block.activationSlot.value = false;
                }

                vfxGraph.children.First().position += Vector2.one;
                Assert.IsTrue(EditorUtility.IsDirty(vfxGraph));
                Assert.AreEqual(previousContent, File.ReadAllText(vfxPath));

                yield return WaitUntilWindowIsDirty(window, "Window did not reflect the dirty graph before close.");

                var mockView = new Mock<IVFXViewEditorAssetEventHandler>();
                mockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(AskAssetChangedBeforeCloseChoice.Discard);
                window.graphView.AssetEventHandler = mockView.Object;

                window.Close();
                yield return null;
                mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Once);

                var resource = VisualEffectResource.GetResourceAtPath(vfxPath);
                vfxGraph = resource.GetGraph();
                Assert.IsFalse(EditorUtility.IsDirty(vfxGraph));
                Assert.AreEqual(0, VFXViewWindow.GetAllWindows().Count());

                var finalContent = File.ReadAllText(vfxPath);
                Assert.AreEqual(previousContent, finalContent);
            }
        }
    }
}

namespace UnityEditor.VFX.Test
{
    // Counts import passes touching the test temp folder, so tests can assert Save All batches its writes into one import.
    class VFXTestImportPassCounter : AssetPostprocessor
    {
        public static bool Armed;
        public static int Passes;

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!Armed)
                return;
            foreach (var path in imported)
            {
                if (path.StartsWith(VFXTestCommon.tempBasePath, StringComparison.OrdinalIgnoreCase))
                {
                    Passes++;
                    return;
                }
            }
        }
    }
}
