using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class RuntimeProfilerTestBase
    {
        protected const int k_NumWarmupFrames = 10;
        protected const int k_NumFramesToRender = 30;

        protected DebugFrameTiming m_DebugFrameTiming;
        protected GameObject m_ToCleanup;

        [SetUp]
        public void Setup()
        {
#if UNITY_EDITOR
            if (!UnityEditor.PlayerSettings.enableFrameTimingStats)
                Assert.Ignore("Frame timing stats are disabled in Player Settings, skipping test.");
#endif

            // HACK #1 - really shouldn't have to do this here, but previous tests are leaking gameobjects
            var objects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var o in objects)
            {
                // HACK #2 - must not destroy DebugUpdater, which happens to have an EventSystem.
                if (o.GetComponent<EventSystem>() == null)
                    CoreUtils.Destroy(o);
            }

            m_DebugFrameTiming = new DebugFrameTiming();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_ToCleanup != null)
                CoreUtils.Destroy(m_ToCleanup);
        }

        protected IEnumerator Warmup()
        {
            for (int i = 0; i < k_NumWarmupFrames; i++)
                yield return new WaitForEndOfFrame();

            m_DebugFrameTiming.Reset();
        }
    }

    // FIXME: Tests are disabled in player builds for now, since there's no API that tells whether frame timing is
    //        enabled or not. Re-enable if that changes.
#if UNITY_EDITOR
    class RuntimeProfilerTests : RuntimeProfilerTestBase
    {
        [UnityTest]
        public IEnumerator RuntimeProfilerGivesNonZeroOutput()
        {
            yield return Warmup();

            m_ToCleanup = new GameObject();
            var camera = m_ToCleanup.AddComponent<Camera>();
            for (int i = 0; i < k_NumFramesToRender; i++)
            {
                m_DebugFrameTiming.UpdateFrameTiming();
                camera.Render();
                yield return new WaitForEndOfFrame();
            }

            Assert.True(
                m_DebugFrameTiming.m_BottleneckHistory.Histogram.Balanced > 0 ||
                m_DebugFrameTiming.m_BottleneckHistory.Histogram.CPU > 0 ||
                m_DebugFrameTiming.m_BottleneckHistory.Histogram.GPU > 0 ||
                m_DebugFrameTiming.m_BottleneckHistory.Histogram.PresentLimited > 0);
        }
    }
#endif
}
