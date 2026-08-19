using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEditor.Rendering.Tests
{
    [TestFixture]
    internal class RenderGraphDebugQueryPauseTests
    {
        const string k_FixturePath = "Assets/Tests/Editor/RenderGraphViewer/RenderGraph/RenderGraphDebugSessionFixture.json";
        const string k_ExpectedGraphName = "URPRenderGraph";

        MockPausableDebugSession m_MockSession;

        class MockPausableDebugSession : IRenderGraphDebugSession
        {
            readonly SerializedRenderGraphDebugSession m_InnerSession;
            bool m_IsPaused;

            public event Action onRegisteredGraphsChanged { add { } remove { } }
            public event Action<string, EntityId> onDebugDataUpdated { add { } remove { } }
            public event Action<bool> onPausedStateChanged { add { } remove { } }

            public MockPausableDebugSession(SerializedRenderGraphDebugSession innerSession)
            {
                m_InnerSession = innerSession;
            }

            public bool isActive => true;

            public bool isPaused
            {
                get => m_IsPaused;
                set => m_IsPaused = value;
            }

            public string connectionName { get => string.Empty; set { } }

            public DebugDataContainer debugDataContainer => m_InnerSession.debugDataContainer;

            public List<string> GetRegisteredGraphs() => m_InnerSession.GetRegisteredGraphs();
            public List<RenderGraph.DebugExecutionItem> GetExecutions(string graphName) => m_InnerSession.GetExecutions(graphName);
            public RenderGraph.DebugData GetDebugData(string graphName, EntityId executionId) => m_InnerSession.GetDebugData(graphName, executionId);
            public void SetDebugData(string renderGraph, EntityId executionId, RenderGraph.DebugData data) { }
            public void DeleteExecutionIds(string renderGraph, List<EntityId> executionIds) { }
            public void Dispose() { }
        }

        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(System.IO.File.Exists(k_FixturePath), $"Test fixture not found at: {k_FixturePath}");
            var serializedSession = SerializedRenderGraphDebugSession.LoadFromFile(k_FixturePath);
            m_MockSession = new MockPausableDebugSession(serializedSession);
            RenderGraphDebugSession.BeginSession(m_MockSession);

            Assert.IsTrue(RenderGraphDebugSession.hasActiveDebugSession, "Session should be active after loading fixture");
        }

        [TearDown]
        public void TearDown()
        {
            RenderGraphDebugSession.EndSession();
        }

        [Test]
        public void Dispose_RestoresPausedState()
        {
            var session = RenderGraphDebugSession.currentDebugSession;

            session.isPaused = false;

            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                Assert.IsTrue(session.isPaused, "Session should be paused after Capture");
            }

            Assert.IsFalse(session.isPaused, "Session should be unpaused after query disposal");
        }

        [Test]
        public void Query_InvalidatesWhenSessionUnpaused()
        {
            var session = RenderGraphDebugSession.currentDebugSession;

            session.isPaused = false;

            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                Assert.IsTrue(session.isPaused, "Session should be paused during query");

                var result = query.GetGraphs();
                Assert.IsTrue(result.TryGetValue(out _), "First query should succeed");

                session.isPaused = false;

                var errorResult = query.GetGraphs();
                Assert.IsFalse(errorResult.TryGetValue(out _), "Query should fail when session is unpaused");
                Assert.IsNotNull(errorResult.error);
                Assert.IsNotEmpty(errorResult.error, "Error should have a message");
                Assert.That(errorResult.error, Does.Contain("unpaused").IgnoreCase, "Error should mention session was unpaused");
            }
        }

        [Test]
        public void Query_FluentAPI_ThrowsWhenSessionInvalidated()
        {
            var session = RenderGraphDebugSession.currentDebugSession;
            session.isPaused = false;

            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem(k_ExpectedGraphName);

                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPasses();
                Assert.IsTrue(result.TryGetValue(out _), "First query should succeed");

                session.isPaused = false;

                result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPasses();
                Assert.IsFalse(result.TryGetValue(out _), "Second query should fail");
            }
        }

        RenderGraph.DebugExecutionItem GetExecutionItem(string graphName)
        {
            // Return hardcoded execution item for predictable tests
            return new RenderGraph.DebugExecutionItem(EntityId.FromULong(1099511677449), "Main Camera");
        }
    }
}
