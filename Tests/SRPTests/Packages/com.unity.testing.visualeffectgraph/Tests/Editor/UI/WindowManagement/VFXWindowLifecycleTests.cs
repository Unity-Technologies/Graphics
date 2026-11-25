using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.UI.WindowManagement
{
    [TestFixture]
    public class VFXWindowLifecycleTests
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

        [UnityTest]
        public IEnumerator Check_Tab_Attachment_Behavior()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            while (EditorWindow.HasOpenInstances<VFXViewWindow>())
                VFXTestCommon.GetViewWindow().Close();

            while (EditorWindow.HasOpenInstances<SceneView>())
                EditorWindow.GetWindow<SceneView>().Close();

            yield return null;
            var sceneView = SceneView.GetWindow(typeof(SceneView));
            sceneView.position = new Rect(0, 0, 800, 600);
            yield return null;
            Assert.IsFalse(sceneView.docked);

            Assert.IsTrue(sceneView.m_Parent is DockArea);
            var dockArea = sceneView.m_Parent as DockArea;
            Assert.AreEqual(1, dockArea.m_Panes.Count);

            var emptyVFX = VFXTestCommon.GetWindow((VFXGraph)null, true, true);
            yield return null;
            Assert.AreNotEqual(null, emptyVFX);

            Assert.AreEqual(emptyVFX.m_Parent, sceneView.m_Parent);
            Assert.AreEqual(2, dockArea.m_Panes.Count);
            Assert.IsTrue(emptyVFX.hasFocus);

            var dummyVFX = VFXTestCommon.MakeTemporaryGraph();
            var actualVFX = VFXTestCommon.GetWindow(dummyVFX, true, true);
            yield return null;
            Assert.AreEqual(actualVFX, emptyVFX); //We are supposed to reuse the empty view
            Assert.AreEqual(actualVFX.m_Parent, sceneView.m_Parent);
            Assert.AreEqual(2, dockArea.m_Panes.Count);
            Assert.IsTrue(actualVFX.hasFocus);

            actualVFX.Close();
            yield return null;
            Assert.AreEqual(1, dockArea.m_Panes.Count);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Open_Graph_Delete_It_Expect_No_Asset()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var resource = graph.visualEffectResource;
            var window = VFXTestCommon.GetViewWindow();

            var mockView = new Mock<IVFXViewEditorAssetEventHandler>();
            mockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(AskAssetChangedBeforeCloseChoice.Discard);
            window.graphView.AssetEventHandler = mockView.Object;
            window.LoadResource(resource, null);

            for (int i = 0; i < 8; ++i)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset) == VFXCompilationMode.Edition)
                    break;
                yield return null;
            }
            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset));

            Assert.IsFalse(EditorUtility.IsDirty(graph));
            Assert.IsFalse(window.hasUnsavedChanges);
            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            initialize.position += Vector2.one;
            yield return null;

            Assert.IsTrue(EditorUtility.IsDirty(graph));
            Assert.IsTrue(window.hasUnsavedChanges);
            Assert.AreNotEqual("No Asset", window.titleContent.text);

            var vfxPath = AssetDatabase.GetAssetPath(graph);
            Assert.IsFalse(string.IsNullOrEmpty(vfxPath));
            AssetDatabase.DeleteAsset(vfxPath);
            yield return null;

            Assert.IsNotNull(graph);
            Assert.IsNotNull(resource);
            Assert.IsTrue(graph == null);
            Assert.IsTrue(resource == null);
            Assert.IsFalse(window.hasUnsavedChanges);
            Assert.AreEqual("No Asset", window.titleContent.text);

            mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Never);
            window.Close();
            mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Never);
            yield return null;
        }

        [UnityTest, Description("Repro expected behavior from importing same package twice while having the window opened")]
        public IEnumerator Open_Graph_And_Delete_And_Restore()
        {
            var packagePath = Path.Combine("Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/", "Repro_Delete_And_Reimport.unitypackage");
            UnityEditor.AssetPackage.Utility.ImportPackageImmediately(packagePath);

            var vfxPath = Path.Combine(VFXTestCommon.tempBasePath, "Delete_And_Reimport.vfx");
            var subgraphPath = Path.Combine(VFXTestCommon.tempBasePath, "Delete_And_Reimport.vfxoperator");

            VFXTestCommon.CloseAllVFXWindow();
            for (int i = 0; i < 3; i++)
            {
                UnityEditor.AssetPackage.Utility.ImportPackageImmediately(packagePath);
                var vfxResource = VisualEffectResource.GetResourceAtPath(vfxPath);
                var subgraphResource = VisualEffectResource.GetResourceAtPath(subgraphPath);
                Assert.IsNotNull(vfxResource);
                Assert.IsNotNull(subgraphResource);
                Assert.AreNotEqual(vfxResource, subgraphResource);

                var windowSubGraph = VFXTestCommon.GetWindow(subgraphResource, true, true);
                windowSubGraph.LoadResource(subgraphResource, null);
                yield return null;

                var windowMain = VFXTestCommon.GetWindow(vfxResource, true, true);
                windowMain.LoadResource(vfxResource, null);
                Assert.AreEqual(2, VFXViewWindow.GetAllWindows().Count());

                for (int frame = 0; frame < 4; ++frame)
                    yield return null;
                Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(vfxResource.asset));

                var view = windowMain.graphView;
                var controller = windowMain.graphView.controller;
                Assert.IsNotNull(view.AssetEventHandler);
                Assert.AreEqual(1, controller.useCount);

                VFXTestCommon.DeleteAllTemporaryGraph();
                yield return null;

                Assert.AreEqual(0, controller.useCount, "DisconnectController might not have been invoked.");
                Assert.IsNull(view.AssetEventHandler, "DisconnectController might not have been invoked.");
            }
        }

        [UnityTest, Description("Closing a window must not throw when other windows were restored by the layout but never initialized")]
        public IEnumerator Close_Window_With_Uninitialized_Windows_Does_Not_Throw()
        {
            VFXTestCommon.CloseAllVFXWindow();

            var otherGraphs = new[]
            {
                VFXTestCommon.CopyTemporaryGraph("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx"),
                VFXTestCommon.CopyTemporaryGraph("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx")
            };
            var graph = VFXTestCommon.CopyTemporaryGraph("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx");

            var uninitializedWindows = new List<VFXViewWindow>();
            try
            {
                // Windows that were never shown have no graph view, like docked windows that were never focused after a layout restore.
                // The resource is assigned right after creation because a second window without resource would close itself in OnEnable.
                foreach (var otherGraph in otherGraphs)
                {
                    var uninitializedWindow = ScriptableObject.CreateInstance<VFXViewWindow>();
                    uninitializedWindows.Add(uninitializedWindow);
                    VFXTestCommon.SetFieldValue(uninitializedWindow, "m_DisplayedResource", otherGraph.visualEffectResource);
                }

                var registeredWindows = VFXViewWindow.GetAllWindows();
                Assert.AreEqual(2, registeredWindows.Count);
                Assert.IsTrue(registeredWindows.All(x => x.graphView == null && x.displayedResource != null));

                var window = VFXTestCommon.GetWindow(graph, true);
                yield return null;
                Assert.IsNotNull(window);
                Assert.IsFalse(uninitializedWindows.Contains(window));
                Assert.IsNotNull(window.graphView);
                window.LoadResource(graph.visualEffectResource);
                Assert.IsNotNull(window.graphView.controller);
                Assert.IsTrue(uninitializedWindows.All(x => x.graphView == null));

                window.Close();
                yield return null;

                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                foreach (var uninitializedWindow in uninitializedWindows)
                {
                    if (uninitializedWindow != null)
                    {
                        uninitializedWindow.Close();
                    }
                }
            }
        }

        [UnityTest, Description("Unloading the graph of the only empty window must keep it open when the other windows were restored by the layout with a resource")]
        public IEnumerator Unload_Graph_With_Uninitialized_Windows_Keeps_Window_Open()
        {
            VFXTestCommon.CloseAllVFXWindow();

            var otherGraphs = new[]
            {
                VFXTestCommon.CopyTemporaryGraph("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx"),
                VFXTestCommon.CopyTemporaryGraph("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx")
            };
            var graph = VFXTestCommon.CopyTemporaryGraph("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx");

            var uninitializedWindows = new List<VFXViewWindow>();
            VFXViewWindow window = null;
            try
            {
                foreach (var otherGraph in otherGraphs)
                {
                    var uninitializedWindow = ScriptableObject.CreateInstance<VFXViewWindow>();
                    uninitializedWindows.Add(uninitializedWindow);
                    VFXTestCommon.SetFieldValue(uninitializedWindow, "m_DisplayedResource", otherGraph.visualEffectResource);
                }

                window = VFXTestCommon.GetWindow(graph, true);
                yield return null;
                window.LoadResource(graph.visualEffectResource);
                Assert.IsNotNull(window.graphView.controller);

                window.graphView.controller = null;
                yield return null;

                Assert.IsTrue(window != null, "The window was closed although the other windows display a resource.");
                Assert.IsTrue(VFXViewWindow.GetAllWindows().Contains(window));
            }
            finally
            {
                if (window != null)
                {
                    window.Close();
                }
                foreach (var uninitializedWindow in uninitializedWindows)
                {
                    if (uninitializedWindow != null)
                    {
                        uninitializedWindow.Close();
                    }
                }
            }
        }
    }
}
