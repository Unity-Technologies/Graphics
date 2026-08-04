using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

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

            if (GraphicsSettings.currentRenderPipeline == null)
                Assert.Ignore("No active Render Pipeline is set, skipping test.");

            // HACK #1 - really shouldn't have to do this here, but previous tests are leaking gameobjects
#pragma warning disable CS0618 // Type or member is obsolete
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID);
#pragma warning restore CS0618 // Type or member is obsolete
            foreach (var o in objects)
            {
                // HACK #2 - must not destroy DebugUpdater
                if (o.GetComponent<DebugUpdater>() == null)
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

    class RuntimeProfilerTests : RuntimeProfilerTestBase
    {
        [UnityTest]
        public IEnumerator RuntimeProfilerGivesNonZeroOutput()
        {
            // GPU Frame Time support is partial (https://docs.unity3d.com/6000.6/Documentation/Manual/frame-timing-manager.html),
            // so adding exceptions here.
            bool supportsGpuFrameTime = true;

            if ((Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor) && SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
                supportsGpuFrameTime = false; // Linux + OpenGLCore
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                supportsGpuFrameTime = false; // WebGL/WebGPU
            if ((Application.platform == RuntimePlatform.EmbeddedLinuxArm64 || Application.platform == RuntimePlatform.EmbeddedLinuxX64) && SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                supportsGpuFrameTime = false; // EmbeddedLinux GLES (no GL_EXT_disjoint_timer_query)
#if ENABLE_VR && ENABLE_XR_MODULE
            if (XRSettings.enabled)
                supportsGpuFrameTime = false; // XR
#endif

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

            // After k_NumFramesToRender frames, we should have a valid average for every headline counter.
            // A failure means the counter was zero on every captured frame. RenderThreadCPUFrameTime and
            // MainThreadCPUPresentWaitTime are excluded because they can legitimately be zero (modes without
            // a separate render thread; no vsync / no frame rate cap). GPUFrameTime is only asserted on
            // platforms known to report it (see supportsGpuFrameTime above).
            var avg = m_DebugFrameTiming.m_FrameHistory.SampleAverage;
            var zeroAvg = new System.Collections.Generic.List<string>();
            if (avg.FramesPerSecond <= 0f)
                zeroAvg.Add(nameof(avg.FramesPerSecond));
            if (avg.FullFrameTime <= 0f)
                zeroAvg.Add(nameof(avg.FullFrameTime));
            if (avg.MainThreadCPUFrameTime <= 0f)
                zeroAvg.Add(nameof(avg.MainThreadCPUFrameTime));
            if (supportsGpuFrameTime && avg.GPUFrameTime <= 0f)
                zeroAvg.Add(nameof(avg.GPUFrameTime));

            Assert.That(zeroAvg, Is.Empty,
                $"After {k_NumFramesToRender} frames the following SampleAverage counters were zero: [{string.Join(", ", zeroAvg)}]. " +
                $"All averages: FramesPerSecond={avg.FramesPerSecond}, FullFrameTime={avg.FullFrameTime}, " +
                $"MainThreadCPUFrameTime={avg.MainThreadCPUFrameTime}, RenderThreadCPUFrameTime={avg.RenderThreadCPUFrameTime}, " +
                $"GPUFrameTime={avg.GPUFrameTime} (asserted={supportsGpuFrameTime}), " +
                $"MainThreadCPUPresentWaitTime={avg.MainThreadCPUPresentWaitTime}.");
        }
    }
}
