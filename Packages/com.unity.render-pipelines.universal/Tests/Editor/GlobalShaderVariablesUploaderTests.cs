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
        // Only base vars are counted, these tests only ever exercise the base group.
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

        static readonly Vector4 k_DefaultTime = new Vector4(11f, 22f, 33f, 44f);

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
        GlobalShaderVariablesUploader CreateFlushedUploader(GlobalShaderVariablesGroup groups = GlobalShaderVariablesGroup.All)
        {
            var uploader = new GlobalShaderVariablesUploader(groups);
            uploader.ResetToDefault();
            uploader.PushGlobal(m_Cmd);
            return uploader;
        }

        // Mimics one camera record: BuildGlobalShaderVariablesBase writing every base var inside a fill window, so
        // the frame defaults cover the whole base mask. Tests asserting a full push need those defaults to exist first.
        static void FillBaseDefaults(GlobalShaderVariablesUploader uploader)
        {
            uploader.BeginFill();

            uploader._Time = k_DefaultTime;
            uploader._SinTime = new Vector4(0.1f, 0.2f, 0.3f, 0.4f);
            uploader._CosTime = new Vector4(0.5f, 0.6f, 0.7f, 0.8f);
            uploader.unity_DeltaTime = new Vector4(0.016f, 60f, 0.016f, 60f);
            uploader._TimeParameters = new Vector4(1f, 0.1f, 0.5f, 0f);
            uploader._LastTimeParameters = new Vector4(0.9f, 0.09f, 0.45f, 0f);
            uploader._ScreenParams = new Vector4(1920f, 1080f, 1f + 1f / 1920f, 1f + 1f / 1080f);
            uploader._ZBufferParams = new Vector4(-0.99f, 1f, -0.001f, 0.001f);
            uploader.unity_OrthoParams = new Vector4(16f, 9f, 0f, 0f);
            uploader._RTHandleScale = Vector4.one;
            uploader._ScaledScreenParams = new Vector4(960f, 540f, 1f, 1f);
            uploader._ScreenSize = new Vector4(1920f, 1080f, 1f / 1920f, 1f / 1080f);
            uploader._ScreenSizeOverride = new Vector4(1920f, 1080f, 0f, 0f);
            uploader._ScreenCoordScaleBias = new Vector4(1f, 1f, 0f, 0f);
            uploader._GlobalMipBias = new Vector2(0f, 1f);

            uploader.EndFill();
        }

        [Test]
        public void PushGlobal_AfterResetToDefault_PushesEveryVariable(
            [Values(GlobalShaderVariablesGroup.Base | GlobalShaderVariablesGroup.Only3D,
                GlobalShaderVariablesGroup.Base | GlobalShaderVariablesGroup.Only2D)] GlobalShaderVariablesGroup groups)
        {
            var uploader = new GlobalShaderVariablesUploader(groups);
            FillBaseDefaults(uploader);
            uploader.ResetToDefault();

            Assert.That(Recorded(() => uploader.PushGlobal(m_Cmd)), Is.EqualTo(CostOfPushes(fullPushCount)),
                "ResetToDefault must mark everything the frame uses dirty.");
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
            var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.All);
            FillBaseDefaults(uploader);

            uploader._Time = new Vector4(99f, 99f, 99f, 99f);

            uploader.PushDefaultToGlobal(m_Cmd);

            Assert.That(uploader._Time, Is.EqualTo(k_DefaultTime),
                "PushDefaultToGlobal re-baselines staging onto the frame defaults.");
        }

        [Test]
        public void PushDefaultToGlobal_RepushesEverythingEvenWhenOnlyOneVariableChanged()
        {
            var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.All);
            FillBaseDefaults(uploader);
            uploader.PushDefaultToGlobal(m_Cmd);

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);

            Assert.That(Recorded(() => uploader.PushDefaultToGlobal(m_Cmd)), Is.EqualTo(CostOfPushes(fullPushCount)),
                "It runs right after cmd.SetupCameraProperties, which overwrites part of these globals behind our " +
                "back, so it must not trust staging and has to re-send all of them.");
        }
    }
}
