using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Profiling.LowLevel;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    class ProfilingSamplerTests
    {
        const int k_FrameCount = 4;

        ProfilingSampler m_Sampler;

        [SetUp]
        public void SetUp()
        {
            m_Sampler = new ProfilingSampler("TestSampler");
        }

        [TearDown]
        public void TearDown()
        {
            m_Sampler.enableRecording = false;
        }

        static void WaitAtLeastMs(int milliseconds)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < milliseconds)
            {
#if !UNITY_WEBGL
                System.Threading.Thread.Sleep(milliseconds);
#endif
            }
        }

        [Test]
        public void Ctor_Name_SetsNameProperty()
        {
            var sampler = new ProfilingSampler("MyMarker");
            Assert.AreEqual("MyMarker", sampler.name);
        }

        [Test]
        public void Ctor_Default_IsValid()
        {
            Assert.IsTrue(m_Sampler.IsValid());
        }

        [Test]
        public void Create_ReturnsNonNull()
        {
            // Create() returns null in release builds; in editor/dev it must return a valid instance.
            // Recorders are allocated lazily; none should be valid before enableRecording = true.
            var sampler = ProfilingSampler.Create("CreatedMarker", MarkerFlags.Default);
            Assert.IsNotNull(sampler);
            Assert.IsTrue(sampler.IsValid());
#if USE_RECORDER
            Assert.IsFalse(sampler.m_Recorder.Valid);
            Assert.IsFalse(sampler.m_GpuRecorder.Valid);
            Assert.IsFalse(sampler.m_InlineRecorder.Valid);
#endif
        }

        [Test]
        public void CreateWithFlags_IsValid()
        {
            var sampler = ProfilingSampler.Create("FlagsMarker", MarkerFlags.VerbosityAdvanced);
            Assert.IsTrue(sampler.IsValid());
#if USE_RECORDER
            // Recorders are allocated lazily; none should be valid before enableRecording = true.
            Assert.IsFalse(sampler.m_Recorder.Valid);
            Assert.IsFalse(sampler.m_GpuRecorder.Valid);
            Assert.IsFalse(sampler.m_InlineRecorder.Valid);
#endif
        }

        [Test]
        public void Begin_End_NullCmd_DoNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                m_Sampler.Begin(null);
                m_Sampler.End(null);
            });
        }

        [Test]
        public void Begin_End_WithCmd_DoNotThrow()
        {
            var cmd = new CommandBuffer();
            Assert.DoesNotThrow(() =>
            {
                m_Sampler.Begin(cmd);
                m_Sampler.End(cmd);
            });
            cmd.Dispose();
        }

        [Test]
        public void EnableRecording_Toggle_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                m_Sampler.enableRecording = true;
                m_Sampler.enableRecording = false;
            });
        }

        [UnityTest]
        public IEnumerator TimingProperties_ReturnZero_WhenNotRecording()
        {
            // Record a bit to ensure that we had non-zero state
            m_Sampler.enableRecording = true;
            for (int i = 0; i < 2; i++)
            {
                m_Sampler.Begin(null);
                m_Sampler.End(null);
                yield return null;
            }

            m_Sampler.enableRecording = false;
            Assert.AreEqual(0.0f, m_Sampler.gpuElapsedTime);
            Assert.AreEqual(0,    m_Sampler.gpuSampleCount);
            Assert.AreEqual(0.0f, m_Sampler.cpuElapsedTime);
            Assert.AreEqual(0,    m_Sampler.cpuSampleCount);
            Assert.AreEqual(0.0f, m_Sampler.inlineCpuElapsedTime);
            Assert.AreEqual(0,    m_Sampler.inlineCpuSampleCount);
        }

#if USE_RECORDER
        // Bits 10-12 all zero → SampleGPU auto-added → GpuRecorder is created on first enableRecording = true
        [Test]
        public void SampleGPU_AutoAdded_ForUserFacingMarker()
        {
            var defaultSampler = ProfilingSampler.Create("UserDefault", MarkerFlags.Default);
            defaultSampler.enableRecording = true;
            Assert.IsTrue(defaultSampler.m_GpuRecorder.Valid,
                "GpuRecorder should be valid after enableRecording = true for MarkerFlags.Default (SampleGPU auto-added)");
            defaultSampler.enableRecording = false;

            var warningsSampler = ProfilingSampler.Create("UserWarnings", MarkerFlags.Warning); // bit 4 only, not verbosity
            warningsSampler.enableRecording = true;
            Assert.IsTrue(warningsSampler.m_GpuRecorder.Valid,
                "GpuRecorder should be valid after enableRecording = true for MarkerFlags.Warning (no verbosity bits → SampleGPU auto-added)");
            warningsSampler.enableRecording = false;
        }

        // Any verbosity flag (bits 10-12) set → SampleGPU NOT auto-added → GpuRecorder never allocated
        [TestCase(MarkerFlags.VerbosityDebug,    TestName = "SampleGPU_NotAutoAdded_VerbosityDebug")]
        [TestCase(MarkerFlags.VerbosityInternal, TestName = "SampleGPU_NotAutoAdded_VerbosityInternal")]
        [TestCase(MarkerFlags.VerbosityExternal, TestName = "SampleGPU_NotAutoAdded_VerbosityExternal")]
        [TestCase(MarkerFlags.VerbosityAdvanced, TestName = "SampleGPU_NotAutoAdded_VerbosityAdvanced")]
        public void SampleGPU_NotAutoAdded_ForNonUserFacingMarker(MarkerFlags verbosityFlags)
        {
            var sampler = ProfilingSampler.Create("NonUserFacing", verbosityFlags);
            sampler.enableRecording = true;
            Assert.IsFalse(sampler.m_GpuRecorder.Valid,
                $"GpuRecorder should not be valid for verbosity flags 0x{(int)verbosityFlags:X4} (SampleGPU was NOT auto-added)");
            sampler.enableRecording = false;
        }

        [Test]
        public void RecordersNotAllocated_BeforeFirstEnableRecording()
        {
            var sampler = new ProfilingSampler("LazyTest");
            Assert.IsFalse(sampler.m_Recorder.Valid,      "m_Recorder allocated before enableRecording = true");
            Assert.IsFalse(sampler.m_GpuRecorder.Valid,   "m_GpuRecorder allocated before enableRecording = true");
            Assert.IsFalse(sampler.m_InlineRecorder.Valid, "m_InlineRecorder allocated before enableRecording = true");
        }

        [Test]
        public void RecordersAllocated_AfterFirstEnableRecording()
        {
            var sampler = new ProfilingSampler("LazyAllocTest") { enableRecording = true };
            Assert.IsTrue(sampler.m_Recorder.Valid,      "m_Recorder not valid after enableRecording = true");
            Assert.IsTrue(sampler.m_GpuRecorder.Valid,   "m_GpuRecorder not valid (SampleGPU auto-added for default flags)");
            Assert.IsTrue(sampler.m_InlineRecorder.Valid, "m_InlineRecorder not valid after enableRecording = true");
            sampler.enableRecording = false;
            sampler.Dispose();
        }

        [UnityTest]
        public IEnumerator EnableRecording_InlineCpuElapsedTime_IsGreaterThanZero()
        {
            m_Sampler.enableRecording = true;

            for (int i = 0; i < k_FrameCount; i++)
            {
                m_Sampler.Begin(null);
                WaitAtLeastMs(1);
                m_Sampler.End(null);
                yield return null;
            }

            Assert.Greater(m_Sampler.inlineCpuElapsedTime, 0.0f);
            Assert.Greater(m_Sampler.inlineCpuSampleCount, 0);
        }

        [UnityTest]
        public IEnumerator ProfilingScope_InlineCpuElapsedTime_IsGreaterThanZero()
        {
            m_Sampler.enableRecording = true;

            for (int i = 0; i < k_FrameCount; i++)
            {
                using (new ProfilingScope(m_Sampler))
                {
                    WaitAtLeastMs(1);
                }
                yield return null;
            }

            Assert.Greater(m_Sampler.inlineCpuElapsedTime, 0.0f);
            Assert.Greater(m_Sampler.inlineCpuSampleCount, 0);
        }

        [UnityTest]
        public IEnumerator ProfilingScopeWithObject_InlineCpu_IsCapturedByRecorder()
        {
            m_Sampler.enableRecording = true;

            var texture = new Texture2D(1, 1) { name = "TestTexture" };
            for (int i = 0; i < k_FrameCount; i++)
            {
                using (new ProfilingScope(m_Sampler, texture))
                {
                    WaitAtLeastMs(1);
                }
                yield return null;
            }
            UnityEngine.Object.DestroyImmediate(texture);

            Assert.Greater(m_Sampler.inlineCpuElapsedTime, 0.0f);
            Assert.Greater(m_Sampler.inlineCpuSampleCount, 0);
        }

        [Test]
        public void ProfilingScopeWithNullObject_InlineCpu_DoesNotCrash()
        {
            Assert.DoesNotThrow(() =>
            {
                using (new ProfilingScope(m_Sampler, null))
                { }
            });
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var sampler = new ProfilingSampler("DisposeTest") { enableRecording = true };
            Assert.DoesNotThrow(() => sampler.Dispose());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var sampler = new ProfilingSampler("DisposeTwiceTest");
            Assert.DoesNotThrow(() =>
            {
                sampler.Dispose();
                sampler.Dispose(); // recorders are no longer .Valid after first dispose
            });
        }
#endif
    }
}
