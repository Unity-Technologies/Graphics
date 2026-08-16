using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// GlobalShaderVariablesUploader only re-uploads the variables whose accessors were used since the last push.
// A regression there is silent: too few pushes leave stale uniforms on the GPU, too many just cost CPU. Neither
// shows up as an error, so these tests measure what actually lands in the command buffer.
namespace UnityEditor.Rendering.Universal.Tests
{
    class GlobalShaderVariablesUploaderTests
    {
        // Derived from the dirty flags themselves so adding a variable does not break these tests.
        // Only base vars are counted, we don't push anything on the 3D / 2D sides yet.
        static int fullPushCount => CountBits((uint)GlobalShaderVariablesDirty.all.varsBase);

        // System.Numerics.BitOperations is not available here.
        static int CountBits(uint mask)
        {
            int count = 0;
            while (mask != 0)
            {
                mask &= mask - 1u;
                count++;
            }

            return count;
        }

        CommandBuffer m_NativeCmd;
        UnsafeCommandBuffer m_Cmd;

        [SetUp]
        public void SetUp()
        {
            m_NativeCmd = new CommandBuffer();
            m_Cmd = CommandBufferHelpers.GetUnsafeCommandBuffer(m_NativeCmd);
        }

        [TearDown]
        public void TearDown()
        {
            m_NativeCmd.Dispose();
            m_NativeCmd = null;
        }

        // Records the same number of SetGlobalVector calls on a throwaway buffer, so expectations are expressed in
        // "number of uniform pushes" rather than in raw byte counts we would have to hardcode.
        static int CostOfPushes(int count)
        {
            using var probe = new CommandBuffer();
            int propertyId = Shader.PropertyToID("_GlobalShaderVariablesUploaderTestsProbe");

            int before = probe.sizeInBytes;
            for (int i = 0; i < count; i++)
                probe.SetGlobalVector(propertyId, Vector4.one);

            return probe.sizeInBytes - before;
        }

        // Bytes the given action records into the command buffer under test.
        int Recorded(System.Action action)
        {
            int before = m_NativeCmd.sizeInBytes;
            action();
            return m_NativeCmd.sizeInBytes - before;
        }

        // An uploader whose pending changes have already been flushed, i.e. staging is in sync with the command buffer.
        GlobalShaderVariablesUploader CreateFlushedUploader(bool is3D = true)
        {
            var uploader = new GlobalShaderVariablesUploader(is3D);
            uploader.ResetToDefault();
            uploader.PushGlobal(m_Cmd);
            return uploader;
        }

        [Test]
        public void PushGlobal_AfterResetToDefault_PushesEveryVariable([Values(true, false)] bool is3D)
        {
            var uploader = new GlobalShaderVariablesUploader(is3D);
            uploader.ResetToDefault();

            Assert.That(Recorded(() => uploader.PushGlobal(m_Cmd)), Is.EqualTo(CostOfPushes(fullPushCount)),
                "ResetToDefault must mark everything dirty.");
        }

        [Test]
        public void PushGlobal_WithNoPendingChange_RecordsNothing()
        {
            var uploader = CreateFlushedUploader();

            Assert.That(Recorded(() => uploader.PushGlobal(m_Cmd)), Is.Zero,
                "Pushing twice in a row must not re-send anything.");
        }

        [Test]
        public void Setter_MarksOnlyItsOwnVariable()
        {
            var uploader = CreateFlushedUploader();

            uploader._ScreenSize = new Vector4(1920f, 1080f, 1f / 1920f, 1f / 1080f);

            Assert.That(Recorded(() => uploader.PushGlobal(m_Cmd)), Is.EqualTo(CostOfPushes(1)),
                "Assigning one variable must push exactly that one.");
        }

        [Test]
        public void Setters_AccumulateUntilPushed()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);
            uploader._ZBufferParams = new Vector4(5f, 6f, 7f, 8f);
            uploader._GlobalMipBias = new Vector2(-1f, 0.5f);

            Assert.That(Recorded(() => uploader.PushGlobal(m_Cmd)), Is.EqualTo(CostOfPushes(3)),
                "Every variable touched since the last push must be sent, and only those.");
        }

        [Test]
        public void Setter_AssignedTwice_PushesOnce()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);
            uploader._Time = new Vector4(5f, 6f, 7f, 8f);

            Assert.That(Recorded(() => uploader.PushGlobal(m_Cmd)), Is.EqualTo(CostOfPushes(1)),
                "Flags are a set, not a counter.");
        }

        [Test]
        public void PushGlobal_ClearsFlags_SoNextChangePushesAlone()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);
            uploader.PushGlobal(m_Cmd);

            uploader._ScreenParams = new Vector4(9f, 9f, 9f, 9f);

            Assert.That(Recorded(() => uploader.PushGlobal(m_Cmd)), Is.EqualTo(CostOfPushes(1)),
                "A pushed variable must not be pushed again by the following push.");
        }

        [Test]
        public void Setter_RoundTripsThroughStaging()
        {
            var uploader = CreateFlushedUploader();
            var expected = new Vector4(1f, 2f, 3f, 4f);

            uploader._TimeParameters = expected;

            Assert.That(uploader._TimeParameters, Is.EqualTo(expected));
        }

        [Test]
        public void PushDefaultToGlobal_DiscardsLocalChanges()
        {
            var uploader = new GlobalShaderVariablesUploader(is3D: true);
            var defaultTime = new Vector4(11f, 22f, 33f, 44f);
            uploader.m_DefaultVars.varsBase._Time = defaultTime;

            uploader.ResetToDefault();
            uploader.PushGlobal(m_Cmd);
            uploader._Time = new Vector4(99f, 99f, 99f, 99f);

            uploader.PushDefaultToGlobal(m_Cmd);

            Assert.That(uploader._Time, Is.EqualTo(defaultTime),
                "PushDefaultToGlobal re-baselines staging onto the frame defaults.");
        }

        [Test]
        public void PushDefaultToGlobal_RepushesEverythingEvenWhenOnlyOneVariableChanged()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);

            Assert.That(Recorded(() => uploader.PushDefaultToGlobal(m_Cmd)), Is.EqualTo(CostOfPushes(fullPushCount)),
                "It runs right after cmd.SetupCameraProperties, which overwrites part of these globals behind our " +
                "back, so it must not trust staging and has to re-send all of them.");
        }
    }
}