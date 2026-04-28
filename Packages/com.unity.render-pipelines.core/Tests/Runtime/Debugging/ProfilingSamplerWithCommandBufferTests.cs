using System.Collections;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    class ProfilingSamplerWithCommandBufferTests
    {
        const int k_SampledFrames = 100;

        Texture2D m_Texture;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_Texture = new Texture2D(1, 1) { name = "TestTexture" };
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(m_Texture);
        }

        static IEnumerator WaitForRecorderSample(CommandBuffer commandBuffer, ProfilerRecorder recorder, int maxFrames)
        {
            for (int i = 0; i < maxFrames; i++)
            {
                Graphics.ExecuteCommandBuffer(commandBuffer);
                yield return null;

                if (recorder.Count > 0 && recorder.GetSample(0).Count > 0)
                    yield break;
            }

            Assert.Fail($"Recorder sample count was 0 after {maxFrames} frames");
        }

        [UnityTest]
        public IEnumerator CommandBufferBeginSample_IsCapturedByProfilerRecorder()
        {
            var sampler = new ProfilingSampler(nameof(CommandBufferBeginSample_IsCapturedByProfilerRecorder));
            using var inlineRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Inl_" + sampler.name);
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, sampler.name);

            using var commandBuffer = new CommandBuffer();
            using (new ProfilingScope(commandBuffer, sampler))
            { }

            // The inline sampler should have recorded the sample even before the command buffer is executed, as it runs on the CPU immediately.
            inlineRecorder.Stop();
            Assert.AreEqual(1, inlineRecorder.Count);
            Assert.AreEqual(1, inlineRecorder.GetSample(0).Count);

            // The sampler should be recorded on the CPU after the command buffer is executed on render thread.
            yield return WaitForRecorderSample(commandBuffer, recorder, k_SampledFrames);
        }

        [UnityTest]
        public IEnumerator CommandBufferBeginSampleWithObject_IsCapturedByProfilerRecorder()
        {
            var sampler = new ProfilingSampler(nameof(CommandBufferBeginSampleWithObject_IsCapturedByProfilerRecorder));
            using var inlineRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Inl_" + sampler.name);
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, sampler.name);

            using var commandBuffer = new CommandBuffer();
            using (new ProfilingScope(commandBuffer, sampler, m_Texture))
            { }

            // The inline sampler should have recorded the sample even before the command buffer is executed, as it runs on the CPU immediately.
            inlineRecorder.Stop();
            Assert.AreEqual(1, inlineRecorder.Count);
            Assert.AreEqual(1, inlineRecorder.GetSample(0).Count);

            // The sampler should be recorded on the CPU after the command buffer is executed on render thread.
            yield return WaitForRecorderSample(commandBuffer, recorder, k_SampledFrames);
        }

        [Test]
        public void CommandBufferBeginSampleWithNullObject_DoesNotCrash()
        {
            var sampler = new ProfilingSampler(nameof(CommandBufferBeginSampleWithNullObject_DoesNotCrash));
            var commandBuffer = new CommandBuffer();
            Assert.DoesNotThrow(() =>
            {
                using (new ProfilingScope(commandBuffer, sampler, null))
                { }
                Graphics.ExecuteCommandBuffer(commandBuffer);
            });
            commandBuffer.Dispose();
        }

        [UnityTest]
        [UnityPlatform(include = new[]
        {
            RuntimePlatform.WindowsPlayer,
            RuntimePlatform.WindowsEditor,
            RuntimePlatform.PS5,
            RuntimePlatform.Switch
        })]
        public IEnumerator CommandBufferBeginSampleWithObject_GpuSamples_ReturnsNonZeroCount()
        {
            if (!SystemInfo.supportsGpuRecorder)
                yield break;

            var sampler = new ProfilingSampler(nameof(CommandBufferBeginSampleWithObject_GpuSamples_ReturnsNonZeroCount));
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, sampler.name, 1, ProfilerRecorderOptions.GpuRecorder | ProfilerRecorderOptions.Default);

            using var commandBuffer = new CommandBuffer();
            using (new ProfilingScope(commandBuffer, sampler, m_Texture))
            { }

            // The sampler should be recorded on the GPU after the command buffer is executed on render thread.
            // Need at least 4 frames of wait.
            yield return WaitForRecorderSample(commandBuffer, recorder, k_SampledFrames);
        }

        [UnityTest]
        [UnityPlatform(include = new[]
        {
            RuntimePlatform.WindowsPlayer,
            RuntimePlatform.WindowsEditor,
            RuntimePlatform.PS5,
            RuntimePlatform.Switch
        })]
        public IEnumerator CommandBufferBeginSample_GpuSamples_ReturnsNonZeroCount()
        {
            if (!SystemInfo.supportsGpuRecorder)
                yield break;

            var sampler = new ProfilingSampler(nameof(CommandBufferBeginSample_GpuSamples_ReturnsNonZeroCount));
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, sampler.name, 1, ProfilerRecorderOptions.GpuRecorder | ProfilerRecorderOptions.Default);

            using var commandBuffer = new CommandBuffer();
            using (new ProfilingScope(commandBuffer, sampler))
            { }

            // The sampler should be recorded on the GPU after the command buffer is executed on render thread.
            // Need at least 4 frames of wait.
            yield return WaitForRecorderSample(commandBuffer, recorder, k_SampledFrames);
        }
    }
}
