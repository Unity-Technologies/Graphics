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
    public class RenderGraphViewerPlayModeReconnectTests
    {
        readonly ScriptableRenderContext m_Context = new(); // NOTE: Dummy context, can't call its functions

        RenderGraphViewer m_Viewer;
        RenderGraph m_RenderGraph;
        Camera m_Camera;

        [SetUp]
        public void SetUp()
        {
            m_Viewer = RenderGraphViewerTestHelpers.OpenRenderGraphViewer();
            m_RenderGraph = new RenderGraph("TestRenderGraph");

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

        // Reproduce the debug-session half of the play-mode statics reset: clear the session and the static events
        // the viewer subscribed to, without touching the render graph's own execution registration.
        static void SimulateFastEnterPlayModeReset()
        {
            RenderGraphDebugSession.ResetStaticsOnLoad();
        }

        void PopulateDebugData()
        {
            TestRenderGraph.Update(m_RenderGraph, m_Context, m_Camera);
        }

        [Test]
        public void ReconnectsSession_WhenResetOnEnteringPlayMode()
        {
            Assert.IsNotNull(RenderGraphDebugSession.currentDebugSession, "opening the viewer should connect a debug session");

            SimulateFastEnterPlayModeReset();
            Assert.IsNull(RenderGraphDebugSession.currentDebugSession, "precondition: session was cleared");

            m_Viewer.OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);

            Assert.IsNotNull(RenderGraphDebugSession.currentDebugSession, "viewer must reconnect the debug session after entering play mode");

            PopulateDebugData();
            Assert.IsNotNull(m_Viewer.m_CurrentDebugData, "the reconnected session must receive debug data again");
            Assert.That(m_Viewer.m_CurrentDebugData.valid);
            Assert.AreEqual(TestRenderGraph.k_NumPasses, m_Viewer.m_CurrentDebugData.passList.Count);
        }

        [Test]
        public void ReconnectsSession_WhenResetOnEnteringEditMode()
        {
            SimulateFastEnterPlayModeReset();
            Assert.IsNull(RenderGraphDebugSession.currentDebugSession);

            m_Viewer.OnPlayModeStateChanged(PlayModeStateChange.EnteredEditMode);

            Assert.IsNotNull(RenderGraphDebugSession.currentDebugSession, "viewer must reconnect the debug session after returning to edit mode");
        }

        [Test]
        public void KeepsSession_WhenStillConnected()
        {
            var before = RenderGraphDebugSession.currentDebugSession;
            Assert.IsNotNull(before);

            m_Viewer.OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);

            Assert.AreSame(before, RenderGraphDebugSession.currentDebugSession,
                "an already-connected session should not be torn down and recreated");
        }

        [Test]
        public void DoesNotReconnect_WhilePaused()
        {
            m_Viewer.isPaused = true;
            SimulateFastEnterPlayModeReset();
            Assert.IsNull(RenderGraphDebugSession.currentDebugSession);

            m_Viewer.OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);

            Assert.IsNull(RenderGraphDebugSession.currentDebugSession,
                "a paused viewer should defer reconnecting so the frozen capture is not cleared");
        }

        [Test]
        public void ReconnectsOnUnpause_WhenSessionResetWhilePaused()
        {
            m_Viewer.isPaused = true;
            SimulateFastEnterPlayModeReset();
            m_Viewer.OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            Assert.IsNull(RenderGraphDebugSession.currentDebugSession, "precondition: reconnect deferred while paused");

            using (var unpause = ChangeEvent<bool>.GetPooled(true, false))
                m_Viewer.OnAutoPlayStatusChanged(unpause);

            Assert.IsNotNull(RenderGraphDebugSession.currentDebugSession, "unpausing must reconnect the deferred session");
        }

        [Test]
        public void DoesNothing_ForNonEnteringTransition()
        {
            SimulateFastEnterPlayModeReset();
            Assert.IsNull(RenderGraphDebugSession.currentDebugSession);

            m_Viewer.OnPlayModeStateChanged(PlayModeStateChange.ExitingEditMode);

            Assert.IsNull(RenderGraphDebugSession.currentDebugSession, "only Entered* transitions should trigger a reconnect");
        }

        static bool ValidDataHasBeenLoadedInUI(VisualElement root)
        {
            var emptyStateMessage = root.Q(RenderGraphViewer.Names.kEmptyStateMessage);
            return emptyStateMessage != null && emptyStateMessage.style.display.value == DisplayStyle.None;
        }

        [UnityTest]
        public IEnumerator RestoresToolbarAndGrid_AfterReconnect()
        {
            var root = m_Viewer.rootVisualElement;

            PopulateDebugData();
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root), 5);
            Assert.That(ValidDataHasBeenLoadedInUI(root), "precondition: the viewer shows debug data before the reset");

            SimulateFastEnterPlayModeReset();
            m_Viewer.OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            Assert.IsNotNull(RenderGraphDebugSession.currentDebugSession, "viewer must reconnect after entering play mode");

            PopulateDebugData();
            yield return new WaitUntil(() => ValidDataHasBeenLoadedInUI(root), 5);

            void CheckExistsAndVisible(string name)
            {
                var element = root.Q(name);
                Assert.NotNull(element, $"Element {name} is missing after reconnect");
                Assert.AreEqual(DisplayStyle.Flex, element.style.display.value, $"Element {name} is not visible after reconnect");
            }

            CheckExistsAndVisible(RenderGraphViewer.Names.kConnectionDropdown);
            CheckExistsAndVisible(RenderGraphViewer.Names.kCurrentExecutionToolbarMenu);
            CheckExistsAndVisible(RenderGraphViewer.Names.kPassFilterField);
            CheckExistsAndVisible(RenderGraphViewer.Names.kResourceFilterField);
            CheckExistsAndVisible(RenderGraphViewer.Names.kViewOptionsField);

            Assert.AreEqual(TestRenderGraph.k_NumResources, root.CountElements(RenderGraphViewer.Classes.kResourceListItem),
                "resource list should repopulate after reconnect");
            Assert.AreEqual(TestRenderGraph.k_NumPasses, root.CountElements(RenderGraphViewer.Classes.kPassListItem),
                "pass list should repopulate after reconnect");
        }
    }
}
