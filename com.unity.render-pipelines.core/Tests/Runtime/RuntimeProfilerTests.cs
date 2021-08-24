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
    //        enabled or not. Re-enable when that changes.
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

    enum ExpectedBottleneck
    {
        CPU,
        GPU
    }

    [TestFixture(ExpectedBottleneck.CPU)]
    [TestFixture(ExpectedBottleneck.GPU)]
    class RuntimeProfilerBottleneckTests : RuntimeProfilerTestBase
    {
        ExpectedBottleneck m_Bottleneck;
        string m_TestObjectName;

        public RuntimeProfilerBottleneckTests(ExpectedBottleneck bottleneck)
        {
            m_Bottleneck = bottleneck;
            // *begin-nonstandard-formatting*
            m_TestObjectName = bottleneck switch
            {
                ExpectedBottleneck.CPU => "RuntimeProfilerTest_CPUBound",
                ExpectedBottleneck.GPU => "RuntimeProfilerTest_GPUBound",
                _ => throw new ArgumentOutOfRangeException(nameof(bottleneck))
            };
            // *end-nonstandard-formatting*
        }

        [UnityTest]
        public IEnumerator BottleneckIsCorrectlyDetected()
        {
            yield return Warmup();

            m_ToCleanup = GameObject.Instantiate((GameObject)Resources.Load(m_TestObjectName));
            var camera = GameObject.FindObjectOfType<Camera>();

            for (int i = 0; i < k_NumFramesToRender; i++)
            {
                m_DebugFrameTiming.UpdateFrameTiming();
                camera.Render();
                yield return new WaitForEndOfFrame();
            }

            float Threshold = 0.10f; // Accept if even 10% of the frames have the expected bottleneck.
            if (m_Bottleneck == ExpectedBottleneck.CPU)
                Assert.True(m_DebugFrameTiming.m_BottleneckHistory.Histogram.CPU > Threshold);
            if (m_Bottleneck == ExpectedBottleneck.GPU)
                Assert.True(m_DebugFrameTiming.m_BottleneckHistory.Histogram.GPU > Threshold);
        }
    }

#endif
}
