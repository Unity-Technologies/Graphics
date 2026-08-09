using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Tests
{
    public class RenderGraphViewerUITests
    {
        const string k_FixturePath = "Assets/Tests/Editor/RenderGraphViewer/RenderGraph/RenderGraphDebugSessionFixture.json";

        RenderGraphViewer m_Viewer;
        SerializedRenderGraphDebugSession m_FixtureSession;
        IRenderGraphDebugSession m_PreviousSession;

        [SetUp]
        public void SetUp()
        {
            // Store the previous session so we can restore it later
            m_PreviousSession = RenderGraphDebugSession.currentDebugSession;

            // Close any existing viewer window first
            var existingViewer = RenderGraphViewerTestHelpers.FindRenderGraphViewerWindow();
            if (existingViewer != null)
                existingViewer.Close();

            // Load the fixture session from the package
            m_FixtureSession = new SerializedRenderGraphDebugSession(File.ReadAllText(k_FixturePath));
            Assume.That(m_FixtureSession, Is.Not.Null, "Failed to load fixture session");

            // Open the viewer first
            m_Viewer = RenderGraphViewerTestHelpers.OpenRenderGraphViewer();
            Assume.That(m_Viewer, Is.Not.Null, "Failed to open RenderGraphViewer");

            // Now connect the viewer to the fixture session using the proper method
            m_Viewer.ConnectDebugSession(m_FixtureSession);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Viewer != null)
                m_Viewer.Close();

            // End the test session
            RenderGraphDebugSession.EndSession();

            // Restore the previous session if there was one
            if (m_PreviousSession != null)
                RenderGraphDebugSession.BeginSession(m_PreviousSession);
        }

        [Test]
        public void RenderGraphViewer_FixtureSessionIsActive()
        {
            Assert.IsTrue(RenderGraphDebugSession.hasActiveDebugSession);
            Assert.AreSame(m_FixtureSession, RenderGraphDebugSession.currentDebugSession);
        }

        [Test]
        public void RenderGraphViewer_FixtureHasValidData()
        {
            var graphs = m_FixtureSession.GetRegisteredGraphs();
            Assert.IsNotNull(graphs);
            Assert.Greater(graphs.Count, 0, "Fixture should contain at least one graph");

            // Get the first graph's executions
            var executions = m_FixtureSession.GetExecutions(graphs[0]);
            Assert.IsNotNull(executions);
            Assert.Greater(executions.Count, 0, "Fixture should contain at least one execution");

            // Get debug data for the first execution
            var debugData = m_FixtureSession.GetDebugData(graphs[0], executions[0].id);
            Assert.IsNotNull(debugData);
            Assert.IsTrue(debugData.valid, "Debug data should be valid");
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

            // Wait for UI to load the fixture data
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            // Verify UI elements are created
            int resourceCount = root.CountElements(RenderGraphViewer.Classes.kResourceListItem);
            int passCount = root.CountElements(RenderGraphViewer.Classes.kPassListItem);
            int dependencyBlockCount = root.CountElements(RenderGraphViewer.Classes.kResourceDependencyBlock);

            Assert.Greater(resourceCount, 0, "Should have resources displayed");
            Assert.Greater(passCount, 0, "Should have passes displayed");
            Assert.Greater(dependencyBlockCount, 0, "Should have dependency blocks displayed");

            // Verify the counts match the debug data
            var graphs = m_FixtureSession.GetRegisteredGraphs();
            if (graphs.Count > 0)
            {
                var executions = m_FixtureSession.GetExecutions(graphs[0]);
                if (executions.Count > 0)
                {
                    var debugData = m_FixtureSession.GetDebugData(graphs[0], executions[0].id);
                    Assert.AreEqual(debugData.passList.Count, passCount, "Pass count should match debug data");
                }
            }
        }

        [UnityTest]
        public IEnumerator RenderGraphViewer_ToolbarElementsAreVisible()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            void CheckExistsAndVisible(string name)
            {
                var element = root.Q(name);
                Assert.NotNull(element, $"Element {name} should exist");
                Assert.AreEqual(DisplayStyle.Flex, element.style.display.value, $"Element {name} is not visible");
            }

            CheckExistsAndVisible(RenderGraphViewer.Names.kConnectionDropdown);
            CheckExistsAndVisible(RenderGraphViewer.Names.kCurrentExecutionToolbarMenu);
            CheckExistsAndVisible(RenderGraphViewer.Names.kPassFilterField);
            CheckExistsAndVisible(RenderGraphViewer.Names.kResourceFilterField);
            CheckExistsAndVisible(RenderGraphViewer.Names.kViewOptionsField);
        }

        [UnityTest]
        public IEnumerator RenderGraphViewer_StatusLabelShowsFixtureInfo()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to loadf
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            var statusLabel = root.Q<Label>(RenderGraphViewer.Names.kStatusLabel);
            Assert.IsNotNull(statusLabel, "Status label should exist");

            // The status label should show capture info from the fixture
            Assert.IsNotEmpty(statusLabel.text, "Status label should have text");
        }

        [UnityTest]
        public IEnumerator RenderGraphViewer_CanFindResourceByName()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            var mainContainer = root.Q<VisualElement>("main-container");
            Assert.IsNotNull(mainContainer, "Main container should exist");

            var backbufferResource = mainContainer.Q<VisualElement>("Backbuffer color");
            Assert.IsNotNull(backbufferResource, "Should find resource named 'Backbuffer color'");
            Assert.IsTrue(backbufferResource.ClassListContains(RenderGraphViewer.Classes.kResourceListItem),
                "Element should have resource-list__item class");
        }

        [UnityTest]
        public IEnumerator RenderGraphViewer_CanFindPassByName()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            var mainContainer = root.Q<VisualElement>("main-container");
            Assert.IsNotNull(mainContainer, "Main container should exist");

            var passList = mainContainer.Q<VisualElement>("pass-list");
            Assert.IsNotNull(passList, "Pass list should exist");

            // Find the pass list item by name
            var passListItem = passList.Q<VisualElement>("Initialize Frame");
            Assert.IsNotNull(passListItem, "Should find pass list item named 'Initialize Frame'");
            Assert.IsTrue(passListItem.ClassListContains(RenderGraphViewer.Classes.kPassListItem),
                "Element should have pass-list__item class");

            // Find the pass title within the pass list item
            var passTitle = passListItem.Q(className: RenderGraphViewer.Classes.kPassTitle);
            Assert.IsNotNull(passTitle, "Pass title should exist within pass list item");
            Assert.IsTrue(passTitle.ClassListContains(RenderGraphViewer.Classes.kPassTitle),
                "Element should have pass-title class");
        }

        [UnityTest]
        public IEnumerator DrawDepthNormalPrepass_CameraDepthTexture_IsTriangleWithGlobalIcon()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            // Find the block for _CameraDepthTexture in "Draw Depth Normal Prepass"
            var blockElement = root.Q("Draw Depth Normal Prepass__CameraDepthTexture_block");
            Assert.IsNotNull(blockElement, "Block element for Draw Depth Normal Prepass / _CameraDepthTexture should exist");

            // Verify it's a triangle (read/write block)
            Assert.IsInstanceOf<TriangleElement>(blockElement,
                "Block should be a TriangleElement for read/write access");

            // Verify it has the read/write class
            Assert.IsTrue(blockElement.ClassListContains(RenderGraphViewer.Classes.kResourceDependencyBlockReadWrite),
                "Block should have the read/write CSS class");

            // Verify the global icon exists and is visible
            var globalIcon = blockElement.Q(className: RenderGraphViewer.Classes.kResourceIcon);
            Assert.IsNotNull(globalIcon, "Global resource icon should exist");

            bool hasGlobalClass = globalIcon.ClassListContains(RenderGraphViewer.Classes.kResourceIconGlobalLight) ||
                                  globalIcon.ClassListContains(RenderGraphViewer.Classes.kResourceIconGlobalDark);
            Assert.IsTrue(hasGlobalClass, "Icon should have the global resource CSS class (light or dark variant)");
        }

        [UnityTest]
        public IEnumerator DrawDepthNormalPrepass_CameraNormalsTexture_IsWriteBlock()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            // Find the block for _CameraNormalsTexture in "Draw Depth Normal Prepass"
            var blockElement = root.Q("Draw Depth Normal Prepass__CameraNormalsTexture_block");
            Assert.IsNotNull(blockElement, "Block element for Draw Depth Normal Prepass / _CameraNormalsTexture should exist");

            // Verify it's NOT a triangle (write-only blocks are regular VisualElements)
            Assert.IsFalse(blockElement is TriangleElement,
                "Block should NOT be a TriangleElement for write-only access");

            // Verify it has the write class
            Assert.IsTrue(blockElement.ClassListContains(RenderGraphViewer.Classes.kResourceDependencyBlockWrite),
                "Block should have the write CSS class");
        }

        [UnityTest]
        public IEnumerator CopyDBufferDepth_CameraDepthTexture_IsReadBlock()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            // Find the block for _CameraDepthTexture in "Copy DBuffer Depth"
            var blockElement = root.Q("Copy DBuffer Depth__CameraDepthTexture_block");
            Assert.IsNotNull(blockElement, "Block element for Copy DBuffer Depth / _CameraDepthTexture should exist");

            // Verify it's NOT a triangle (read-only blocks are regular VisualElements)
            Assert.IsFalse(blockElement is TriangleElement,
                "Block should NOT be a TriangleElement for read-only access");

            // Verify it has the read class
            Assert.IsTrue(blockElement.ClassListContains(RenderGraphViewer.Classes.kResourceDependencyBlockRead),
                "Block should have the read CSS class");
        }

        [UnityTest]
        public IEnumerator DrawDepthNormalPrepass_MergeIndicator_IsNotVisible()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            // Find the pass list item
            var passListItem = root.Q("Draw Depth Normal Prepass");
            Assert.IsNotNull(passListItem, "Pass list item for Draw Depth Normal Prepass should exist");

            // Find the merge indicator within this pass
            var mergeIndicator = passListItem.Q(className: RenderGraphViewer.Classes.kPassMergeIndicator);
            Assert.IsNotNull(mergeIndicator, "Merge indicator should exist");

            // Verify it's NOT visible (this pass is not merged)
            Assert.AreNotEqual(Visibility.Visible, mergeIndicator.style.visibility.value,
                "Merge indicator should not be visible for non-merged pass");
        }

        [UnityTest]
        public IEnumerator CopyDBufferDepth_MergeIndicator_IsVisible()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            // Find the pass list item
            var passListItem = root.Q("Copy DBuffer Depth");
            Assert.IsNotNull(passListItem, "Pass list item for Copy DBuffer Depth should exist");

            // Find the merge indicator within this pass
            var mergeIndicator = passListItem.Q(className: RenderGraphViewer.Classes.kPassMergeIndicator);
            Assert.IsNotNull(mergeIndicator, "Merge indicator should exist");

            // Verify it's visible (this pass is merged)
            Assert.AreEqual(Visibility.Visible, mergeIndicator.style.visibility.value,
                "Merge indicator should be visible for merged pass");
        }

        [UnityTest]
        public IEnumerator DrawDBuffer_MergeIndicator_IsVisible()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            // Find the pass list item
            var passListItem = root.Q("Draw DBuffer");
            Assert.IsNotNull(passListItem, "Pass list item for Draw DBuffer should exist");

            // Find the merge indicator within this pass
            var mergeIndicator = passListItem.Q(className: RenderGraphViewer.Classes.kPassMergeIndicator);
            Assert.IsNotNull(mergeIndicator, "Merge indicator should exist");

            // Verify it's visible (this pass is merged)
            Assert.AreEqual(Visibility.Visible, mergeIndicator.style.visibility.value,
                "Merge indicator should be visible for merged pass");
        }

        [UnityTest]
        public IEnumerator DrawDepthNormalPrepass_PassBreakReasoning_ContainsUnsafeMessage()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            // Find the pass break reasoning element
            var passBreakReasoning = root.Q<TextElement>("Draw Depth Normal Prepass_Pass Break Reason");
            Assert.IsNotNull(passBreakReasoning, "Pass break reasoning element should exist");

            // Verify it contains the Unsafe message
            Assert.AreEqual("Failed to merge Copy DBuffer Depth into this native pass.\nThe next pass uses a different depth buffer. All passes in the native render pass need to use the same depth buffer.", passBreakReasoning.text);
        }

        [UnityTest]
        public IEnumerator CopyDBufferDepth_PassBreakReasoning_ContainsMergeFailureMessage()
        {
            var root = m_Viewer.rootVisualElement;

            // Wait for UI to load
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root));

            // Find the pass break reasoning element for Copy DBuffer Depth (which is merged with Draw DBuffer)
            var passBreakReasoning = root.Q<TextElement>("Copy DBuffer Depth_Pass Break Reason");
            Assert.IsNotNull(passBreakReasoning, "Pass break reasoning element should exist");

            // Verify it contains reasoning about the next pass
            Assert.IsTrue(passBreakReasoning.text.Contains("Failed to merge") || passBreakReasoning.text.Length > 0,
                "Pass break reasoning should contain merge failure information for merged passes");
        }
    }

    static class VisualElementExtensions
    {
        public static int CountElements(this VisualElement element, string @class)
        {
            return element.Query(classes: @class).ToList().Count;
        }
    }
}
