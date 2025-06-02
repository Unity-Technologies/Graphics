using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.Rendering.Tests;

namespace UnityEditor.Rendering.Tests
{
    public enum Compiler
    {
        RenderGraph,
        NativeRenderGraph
    }

    [TestFixture(Compiler.RenderGraph)]
    [TestFixture(Compiler.NativeRenderGraph)]
    public class RenderGraphViewerUITests
    {
        readonly Compiler m_Compiler;
        readonly ScriptableRenderContext m_Context = new(); // NOTE: Dummy context, can't call its functions

        public RenderGraphViewerUITests(Compiler compiler)
        {
            m_Compiler = compiler;
        }

        public static RenderGraphViewer FindRenderGraphViewerWindow()
        {
            RenderGraphViewer rgv = null;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window is RenderGraphViewer viewer)
                {
                    rgv = viewer;
                    break;
                }
            }

            return rgv;
        }

        public static RenderGraphViewer OpenAndCheckRenderGraphViewer()
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Render Graph Viewer");
            RenderGraphViewer rgv = FindRenderGraphViewerWindow();
            Assume.That(rgv, Is.Not.Null);
            return rgv;
        }

        RenderGraphViewer m_Viewer;
        RenderGraph m_RenderGraph;
        Camera m_Camera;

        [SetUp]
        public void SetUp()
        {
            m_Viewer = OpenAndCheckRenderGraphViewer();

            m_RenderGraph = new RenderGraph("TestRenderGraph");
            m_RenderGraph.nativeRenderPassesEnabled = m_Compiler == Compiler.NativeRenderGraph;

            var cameraObject = new GameObject("TestCamera", typeof(Camera));
            m_Camera = cameraObject.GetComponent<Camera>();
        }

        [TearDown]
        public void TearDown()
        {
            m_RenderGraph.Cleanup();
            CoreUtils.Destroy(m_Camera);

            m_Viewer.Close();
        }

        [Test]
        public void RenderGraphViewer_DebugDataIsValid()
        {
            Assert.Null(m_Viewer.m_CurrentDebugData);

            TestRenderGraph.Update(m_RenderGraph, m_Context, m_Camera);

            Assert.NotNull(m_Viewer.m_CurrentDebugData);
            Assert.That(m_Viewer.m_CurrentDebugData.valid);
        }

        [Test]
        public void RenderGraphViewer_RenamedCameraIsUpdatedInDebugData()
        {
            m_Camera.name = "Before Renaming";
            TestRenderGraph.Update(m_RenderGraph, m_Context, m_Camera);
            Assert.AreEqual("Before Renaming", m_Viewer.m_CurrentDebugData.executionName);

            m_Camera.name = "After Renaming";
            TestRenderGraph.Update(m_RenderGraph, m_Context, m_Camera);
            Assert.AreEqual("After Renaming", m_Viewer.m_CurrentDebugData.executionName);
        }

        bool ValidDataHasBeenLoadedInUI(VisualElement root)
        {
            // Debug data should be on screen when the empty state message has been hidden
            var emptyStateMessage = root.Q(RenderGraphViewer.Names.kEmptyStateMessage);
            return emptyStateMessage != null && emptyStateMessage.style.display.value == DisplayStyle.None;
        }

        [UnityTest]
        public IEnumerator RenderGraphViewer_ResourcesAndPassesArePopulated()
        {
            var root = m_Viewer.rootVisualElement;

            TestRenderGraph.Update(m_RenderGraph, m_Context, m_Camera);

            Assert.AreEqual(m_Viewer.m_CurrentDebugData.passList.Count, TestRenderGraph.k_NumPasses);

            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            Assert.AreEqual(TestRenderGraph.k_NumResources, root.CountElements(RenderGraphViewer.Classes.kResourceListItem));
            Assert.AreEqual(TestRenderGraph.k_NumPasses, root.CountElements(RenderGraphViewer.Classes.kPassListItem));
            Assert.AreEqual(TestRenderGraph.k_NumPasses * TestRenderGraph.k_NumResources, root.CountElements(RenderGraphViewer.Classes.kResourceDependencyBlock));
        }

        [UnityTest]
        public IEnumerator RenderGraphViewer_ToolbarElementsAreVisible()
        {
            var root = m_Viewer.rootVisualElement;

            TestRenderGraph.Update(m_RenderGraph, m_Context, m_Camera);

            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            void CheckExistsAndVisible(string name)
            {
                var element = root.Q(name);
                Assert.NotNull(element);
                Assert.AreEqual(DisplayStyle.Flex, element.style.display.value, $"Element {name} is not visible");
            }

            CheckExistsAndVisible(RenderGraphViewer.Names.kConnectionDropdown);
            CheckExistsAndVisible(RenderGraphViewer.Names.kCurrentExecutionToolbarMenu);
            CheckExistsAndVisible(RenderGraphViewer.Names.kPassFilterField);
            CheckExistsAndVisible(RenderGraphViewer.Names.kResourceFilterField);
            CheckExistsAndVisible(RenderGraphViewer.Names.kViewOptionsField);
        }
    }
}

static class VisualElementExtensions
{
    public static int CountElements(this VisualElement element, string @class)
    {
        return element.Query(classes: @class).ToList().Count;
    }
}
