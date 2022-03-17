using System;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class ProcessOnIdleAgentTests : GraphViewTester
    {
        bool m_ProcessOnIdle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_ProcessOnIdle = Window.GraphTool.Preferences.GetBool(BoolPref.OnlyProcessWhenIdle);
            Window.GraphTool.Preferences.SetBool(BoolPref.OnlyProcessWhenIdle, true);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            Window.GraphTool.Preferences.SetBool(BoolPref.OnlyProcessWhenIdle, m_ProcessOnIdle);
        }

        [UnityTest]
        public IEnumerator ProcessOnIdleIsTriggeredAfterSomeTime()
        {
            var observedComponent = Window.GraphView.ProcessOnIdleAgent.StateComponent;
            var initialVersion = new StateComponentVersion
            {
                HashCode = observedComponent.GetHashCode(),
                Version = observedComponent.CurrentVersion
            };
            Assert.AreEqual(UpdateType.None, observedComponent.GetUpdateType(initialVersion));

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds < ProcessOnIdleAgent.idleTimeBeforeGraphProcessingMs + 500)
                yield return null;

            Assert.AreEqual(UpdateType.Complete, observedComponent.GetUpdateType(initialVersion));
        }

        [UnityTest, Ignore("GTF-658: Test often fails on Mac, failed at least once on 2020.3 Windows.")]
        public IEnumerator MouseMovePreventsProcessOnIdleToBeTriggered()
        {
            var observedComponent = Window.GraphView.ProcessOnIdleAgent.StateComponent;
            var initialVersion = new StateComponentVersion
            {
                HashCode = observedComponent.GetHashCode(),
                Version = observedComponent.CurrentVersion
            };
            Assert.AreEqual(UpdateType.None, observedComponent.GetUpdateType(initialVersion));

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            for (var i = 0; i < 8; i++)
            {
                while (stopWatch.ElapsedMilliseconds < ProcessOnIdleAgent.idleTimeBeforeGraphProcessingMs / 4)
                    yield return null;

                Helpers.MouseMoveEvent(Vector2.down, Vector2.right);
                yield return null;
            }

            Assert.AreEqual(UpdateType.None, observedComponent.GetUpdateType(initialVersion));
        }
    }
}
