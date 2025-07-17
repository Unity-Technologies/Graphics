using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.TestTools;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;
using UnityEngine.Rendering.Tests;

namespace UnityEditor.Rendering.Tests
{
    public class RenderGraphViewerConnectionTests
    {
        protected BuildAndRunPlayerHelper m_Runner;

        public enum WindowOpenState
        {
            BeforePlayerStart,
            AfterPlayerStarted
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // this might throw a connection no longer valid error
            LogAssert.ignoreFailingMessages = true;
            m_Runner.Dispose();
            LogAssert.ignoreFailingMessages = false;
        }

        [SetUp]
        public void SetUp()
        {
            ProfilerDriver.connectedProfiler = -1;
        }

        [TearDown]
        public void TearDown()
        {
            var rgv = RenderGraphViewerUITests.FindRenderGraphViewerWindow();
            rgv.Close();

            ProfilerDriver.connectedProfiler = -1;

            m_Runner.KillPlayer();
        }
    }

    [TestFixture]
    [RequirePlatformSupport(BuildTarget.StandaloneWindows64)]
    public class RenderGraphViewerConnectionAutoConnectTests : RenderGraphViewerConnectionTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Runner = new BuildAndRunPlayerHelper
            {
                Target = BuildTarget.StandaloneWindows64,
                ScriptingBackend = ScriptingImplementation.Mono2x,
            };

            Type[] scripts = { typeof(Camera), typeof(TestRenderGraph) };

            BuildOptions opts = BuildOptions.Development | BuildOptions.ConnectWithProfiler;
            m_Runner.BuildPlayerWithScript(opts, scripts);
        }

        [UnityTest]
        [Timeout(5 * 60 * 1000)]
        public IEnumerator AutoConnectRenderGraphViewer(
            [Values(WindowOpenState.BeforePlayerStart, WindowOpenState.AfterPlayerStarted)] WindowOpenState openState)
        {
            RenderGraphViewer viewer = null;
            if (openState == WindowOpenState.BeforePlayerStart)
                viewer = RenderGraphViewerUITests.OpenAndCheckRenderGraphViewer();

            Assume.That(m_Runner.RunPlayer());

            // Wait for autoconnect to finish
            yield return new WaitUntil(() => ProfilerDriver.connectedProfiler != -1, 60);

            if (openState == WindowOpenState.AfterPlayerStarted)
                viewer = RenderGraphViewerUITests.OpenAndCheckRenderGraphViewer();

            // Wait for debug data to be available in the viewer
            yield return new WaitUntil(() => viewer.m_CurrentDebugData != null, 60);

            Assert.That(viewer.m_CurrentDebugData != null);
            Assert.That(viewer.m_CurrentDebugData.valid);
            Assert.AreEqual(viewer.m_CurrentDebugData.passList.Count, TestRenderGraph.k_NumPasses);
        }
    }

    [TestFixture]
    [Category(nameof(RenderGraphViewerConnectionTests))]
    [RequirePlatformSupport(BuildTarget.StandaloneWindows64)]
    public class RenderGraphViewerConnectionManualConnectTests : RenderGraphViewerConnectionTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Runner = new BuildAndRunPlayerHelper
            {
                Target = BuildTarget.StandaloneWindows64,
                ScriptingBackend = ScriptingImplementation.Mono2x,
            };

            Type[] scripts = { typeof(Camera), typeof(TestRenderGraph) };

            BuildOptions opts = BuildOptions.Development;
            m_Runner.BuildPlayerWithScript(opts, scripts);
        }

        [UnityTest]
        [Timeout(5 * 60 * 1000)]
        [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor })] // Unstable: https://jira.unity3d.com/browse/UUM-110857
        public IEnumerator ManuallyConnectRenderGraphViewer(
            [Values(WindowOpenState.BeforePlayerStart, WindowOpenState.AfterPlayerStarted)] WindowOpenState openState)
        {
            RenderGraphViewer viewer = null;
            if (openState == WindowOpenState.BeforePlayerStart)
                viewer = RenderGraphViewerUITests.OpenAndCheckRenderGraphViewer();

            Assume.That(m_Runner.RunPlayer());

            if (openState == WindowOpenState.AfterPlayerStarted)
                viewer = RenderGraphViewerUITests.OpenAndCheckRenderGraphViewer();

            viewer.ConnectDebugSession<RenderGraphEditorRemoteDebugSession>();

            // Because we can't manipulate the PlayerConnectionGUI directly in test, we use ProfilerDriver instead
            yield return new WaitUntil(() => ProfilerDriver.GetAvailableProfilers().Length > 1, 60);

            var availableProfilers = ProfilerDriver.GetAvailableProfilers();
            Assert.That(availableProfilers.Length > 1);
            for (;;)
            {
                // With repeated connect/disconnect when running multiple tests, trying to set connectedProfiler can fail
                // with a socket error, in which case the value does not get set. Ignore the errors and retry if that happens.
                LogAssert.ignoreFailingMessages = true;
                ProfilerDriver.connectedProfiler = availableProfilers[1]; // [0] is -1 (Editor), [1] is the player
                LogAssert.ignoreFailingMessages = false;

                if (ProfilerDriver.connectedProfiler == -1)
                    continue;

                break;
            }

            Assume.That(ProfilerDriver.connectedProfiler != -1);

            // Wait for debug data to be available in the viewer
            yield return new WaitUntil(() => viewer.m_CurrentDebugData != null, 60);

            Assert.That(viewer.m_CurrentDebugData != null);
            Assert.That(viewer.m_CurrentDebugData.valid);
            Assert.AreEqual(TestRenderGraph.k_NumPasses, viewer.m_CurrentDebugData.passList.Count);
        }
    }
}
