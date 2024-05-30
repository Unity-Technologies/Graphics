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
            if (!FrameTimingManager.IsFeatureEnabled())
                Assert.Ignore("Frame timing stats are disabled in Player Settings, skipping test.");

            if (Application.isBatchMode)
                Assert.Ignore("Frame timing tests are not supported in batch mode, skipping test.");

            // HACK #1 - really shouldn't have to do this here, but previous tests are leaking gameobjects
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID);
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
                yield return null;

            m_DebugFrameTiming.Reset();
        }
    }

    // Fails on WebGL and Oculus Quest.
    // Unfortunately, there is no good way to exclude Oculus Quest from the test without excluding all Android devices.
    // https://jira.unity3d.com/browse/GFXFOUND-559
    [UnityPlatform(exclude = new RuntimePlatform[] { RuntimePlatform.WebGLPlayer, RuntimePlatform.Android })]
    class RuntimeProfilerTests : RuntimeProfilerTestBase
    {
        [UnityTest]
        public IEnumerator RuntimeProfilerGivesNonZeroOutput()
        {
            if ((Application.platform == RuntimePlatform.LinuxPlayer ||
                 Application.platform == RuntimePlatform.LinuxEditor)
                && SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
                {
                    Assert.Ignore("Test is failing on Linux OpenGLCore. https://jira.unity3d.com/browse/GFXFOUND-559");
                }

            yield return Warmup();

            m_ToCleanup = new GameObject();
            var camera = m_ToCleanup.AddComponent<Camera>();
            for (int i = 0; i < k_NumFramesToRender; i++)
            {
                m_DebugFrameTiming.UpdateFrameTiming();

                var rr = new UnityEngine.Rendering.RenderPipeline.StandardRequest();
                rr.destination = RenderTexture.GetTemporary(128, 128, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
                rr.mipLevel = 0;
                rr.slice = 0;
                rr.face = CubemapFace.Unknown;
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, rr);
                RenderTexture.ReleaseTemporary(rr.destination);

                yield return null;
            }

            Assert.True(
                m_DebugFrameTiming.m_BottleneckHistory.Histogram.Balanced > 0 ||
                m_DebugFrameTiming.m_BottleneckHistory.Histogram.CPU > 0 ||
                m_DebugFrameTiming.m_BottleneckHistory.Histogram.GPU > 0 ||
                m_DebugFrameTiming.m_BottleneckHistory.Histogram.PresentLimited > 0);
        }
    }
}
