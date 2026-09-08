using System;
using System.Collections.Generic;
using System.Reflection;
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

        // A scalar goes out as SetGlobalFloat, which records half the bytes of the SetGlobalVector every other base
        // variable uses, so a full push is not fullPushCount identical commands. Counted off the field types rather
        // than hardcoded: GlobalShaderVariablesLayoutTests.DirtyFlagsMatchFields keeps them one to one with the flags.
        static int fullFloatPushCount
        {
            get
            {
                int count = 0;
                foreach (var field in typeof(GlobalShaderVariablesBase).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Padding is never pushed, so it has no command to pay for.
                    if (field.FieldType == typeof(float) && !field.Name.StartsWith("_URPPadding", StringComparison.Ordinal))
                        count++;
                }

                return count;
            }
        }

        // Bytes a push of the whole base group records, vector and scalar commands each counted at their own cost.
        static int CostOfFullPush() =>
            CostOfPushes(fullPushCount - fullFloatPushCount) + CostOfFloatPushes(fullFloatPushCount);

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

        // Uploaders that ran in persistent constant buffer mode own GPU buffers nobody else releases, so the tests
        // hand them back here rather than leaking a ComputeBuffer per test.
        readonly List<GlobalShaderVariablesUploader> m_ConstantBufferUploaders = new List<GlobalShaderVariablesUploader>();

        [SetUp]
        public void SetUp()
        {
            m_NativeCmd = new CommandBuffer();
            m_Cmd = CommandBufferHelpers.GetUnsafeCommandBuffer(m_NativeCmd);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var uploader in m_ConstantBufferUploaders)
                uploader.Release();
            m_ConstantBufferUploaders.Clear();

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

        // Same measurement for the scalars, which the uploader pushes with SetGlobalFloat.
        static int CostOfFloatPushes(int count)
        {
            using var probe = new CommandBuffer();
            int propertyId = Shader.PropertyToID("_GlobalShaderVariablesUploaderTestsProbe");

            int before = probe.sizeInBytes;
            for (int i = 0; i < count; i++)
                probe.SetGlobalFloat(propertyId, 1f);

            return probe.sizeInBytes - before;
        }

        // Bound by the probes below only, so releasing them never unbinds the slot the uploader really uses.
        static readonly int k_ProbeConstantBufferID = Shader.PropertyToID("_GlobalShaderVariablesUploaderTestsProbeCB");

        // Same two commands the constant buffer path records, measured on a throwaway buffer so expectations read as
        // "an upload plus a bind" or "a bind" instead of hardcoded byte counts. The probe allocates exactly the
        // ConstantBuffer<GlobalShaderVariables> the uploader allocates, so it needs nothing the tested path does not.
        static int CostOfUploadAndBind()
        {
            using var probe = new CommandBuffer();
            var constantBuffer = new ConstantBuffer<GlobalShaderVariables>(ComputeBufferMode.Dynamic);

            int before = probe.sizeInBytes;
            constantBuffer.PushGlobal(probe, default(GlobalShaderVariables), k_ProbeConstantBufferID);
            int cost = probe.sizeInBytes - before;

            constantBuffer.Release();
            return cost;
        }

        static int CostOfBind()
        {
            using var probe = new CommandBuffer();
            var constantBuffer = new ConstantBuffer<GlobalShaderVariables>(ComputeBufferMode.Dynamic);

            int before = probe.sizeInBytes;
            constantBuffer.SetGlobal(probe, k_ProbeConstantBufferID);
            int cost = probe.sizeInBytes - before;

            constantBuffer.Release();
            return cost;
        }

        // Bytes the given action records into the command buffer it is handed.
        // That buffer is brand new, the probes above record into a brand new buffer too, which is what
        // makes the two comparable.
        static int Recorded(System.Action<UnsafeCommandBuffer> action)
        {
            using var measured = new CommandBuffer();

            int before = measured.sizeInBytes;
            action(CommandBufferHelpers.GetUnsafeCommandBuffer(measured));
            return measured.sizeInBytes - before;
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
            uploader.unity_AmbientSky = new Vector4(0.2f, 0.3f, 0.45f, 1f);
            uploader.unity_AmbientEquator = new Vector4(0.15f, 0.16f, 0.17f, 1f);
            uploader.unity_AmbientGround = new Vector4(0.05f, 0.05f, 0.05f, 1f);
            uploader._GlobalMipBias = new Vector2(0f, 1f);
            uploader._DitheringTextureInvSize = 1f / 64f;

            uploader._GlossyEnvironmentColor = new Vector4(0.3f, 0.4f, 0.5f, 1f);
            uploader._SubtractiveShadowColor = new Vector4(0.1f, 0.1f, 0.15f, 1f);
            uploader._GlossyEnvironmentCubeMap_HDR = new Vector4(1f, 1f, 0f, 0f);

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

            Assert.That(Recorded(cmd => uploader.PushGlobal(cmd)), Is.EqualTo(CostOfFullPush()),
                "ResetToDefault must mark everything the frame uses dirty.");
        }

        [Test]
        public void PushGlobal_WithNoPendingChange_RecordsNothing()
        {
            var uploader = CreateFlushedUploader();

            Assert.That(Recorded(cmd => uploader.PushGlobal(cmd)), Is.Zero,
                "Pushing twice in a row must not re-send anything.");
        }

        [Test]
        public void Setter_MarksOnlyItsOwnVariable()
        {
            var uploader = CreateFlushedUploader();

            uploader._ScreenSize = new Vector4(1920f, 1080f, 1f / 1920f, 1f / 1080f);

            Assert.That(Recorded(cmd => uploader.PushGlobal(cmd)), Is.EqualTo(CostOfPushes(1)),
                "Assigning one variable must push exactly that one.");
        }

        [Test]
        public void Setters_AccumulateUntilPushed()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);
            uploader._ZBufferParams = new Vector4(5f, 6f, 7f, 8f);
            uploader._GlobalMipBias = new Vector2(-1f, 0.5f);

            Assert.That(Recorded(cmd => uploader.PushGlobal(cmd)), Is.EqualTo(CostOfPushes(3)),
                "Every variable touched since the last push must be sent, and only those.");
        }

        [Test]
        public void Setter_AssignedTwice_PushesOnce()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);
            uploader._Time = new Vector4(5f, 6f, 7f, 8f);

            Assert.That(Recorded(cmd => uploader.PushGlobal(cmd)), Is.EqualTo(CostOfPushes(1)),
                "Flags are a set, not a counter.");
        }

        [Test]
        public void PushGlobal_ClearsFlags_SoNextChangePushesAlone()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);
            uploader.PushGlobal(m_Cmd);

            uploader._ScreenParams = new Vector4(9f, 9f, 9f, 9f);

            Assert.That(Recorded(cmd => uploader.PushGlobal(cmd)), Is.EqualTo(CostOfPushes(1)),
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

            Assert.That(Recorded(cmd => uploader.PushDefaultToGlobal(cmd)), Is.EqualTo(CostOfFullPush()),
                "It runs right after cmd.SetupCameraProperties, which overwrites part of these globals behind our " +
                "back, so it must not trust staging and has to re-send all of them.");
        }

        // Persistent constant buffer mode. What matters here is not the byte count but which of the two GPU buffers a
        // push goes to, and whether it re-uploads: the whole point of a separate defaults buffer is that restoring the
        // defaults costs a rebind, no matter what the overrides wrote since.
        GlobalShaderVariablesUploader CreateConstantBufferUploader()
        {
            var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.All) { useConstantBuffer = true };
            m_ConstantBufferUploaders.Add(uploader);

            FillBaseDefaults(uploader);
            return uploader;
        }

        [Test]
        public void PushDefaultToGlobal_ConstantBufferMode_UploadsDefaultsOnceThenRecordsNothing()
        {
            var uploader = CreateConstantBufferUploader();

            Assert.That(Recorded(cmd => uploader.PushDefaultToGlobal(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "The first push of a camera has to get the frozen defaults onto the GPU.");

            Assert.That(Recorded(cmd => uploader.PushDefaultToGlobal(cmd)), Is.Zero,
                "The defaults buffer already holds exactly what an upload would write and still owns the slot, so " +
                "both the upload and the bind are pure waste.");
        }

        [Test]
        public void PushGlobal_ConstantBufferMode_OverrideDoesNotInvalidateDefaults()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefaultToGlobal(m_Cmd);

            uploader._Time = new Vector4(99f, 99f, 99f, 99f);

            Assert.That(Recorded(cmd => uploader.PushGlobal(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "A diverged staging cannot be served by the defaults buffer, it goes to the overrides one.");

            Assert.That(Recorded(cmd => uploader.PushDefaultToGlobal(cmd)), Is.EqualTo(CostOfBind()),
                "The override wrote to the other buffer, so the defaults are still resident and only need a rebind.");
        }

        [Test]
        public void PushGlobal_ConstantBufferMode_OverrideAfterASkippedPushStillCostsTheDefaultsTheirBind()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefaultToGlobal(m_Cmd);

            // A defaults push records nothing while its buffer is up to date and still bound. That skip must not be
            // read as "the slot is ours forever": the override below takes it over.
            uploader.PushDefaultToGlobal(m_Cmd);

            uploader._Time = new Vector4(99f, 99f, 99f, 99f);
            uploader.PushGlobal(m_Cmd);

            Assert.That(Recorded(cmd => uploader.PushDefaultToGlobal(cmd)), Is.EqualTo(CostOfBind()),
                "Whatever was skipped in between, the slot holds the override buffer and has to be bound back.");
        }

        [Test]
        public void EndFill_ConstantBufferMode_InvalidatesTheUploadedDefaults()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefaultToGlobal(m_Cmd);

            // Next camera of the frame: same uploader, new defaults.
            FillBaseDefaults(uploader);

            Assert.That(Recorded(cmd => uploader.PushDefaultToGlobal(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "Rebinding here would bind the previous camera's values, the new defaults must reach the GPU first.");
        }

        [Test]
        public void Release_ConstantBufferMode_ForcesAReuploadOnTheNextPush()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefaultToGlobal(m_Cmd);

            uploader.Release();

            Assert.That(Recorded(cmd => uploader.PushDefaultToGlobal(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "The buffer holding the defaults is gone, whatever replaces it starts empty.");
        }

        [Test]
        public void PushGlobal_ConstantBufferMode_WithNoPendingChange_RecordsNothing()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefaultToGlobal(m_Cmd);

            Assert.That(Recorded(cmd => uploader.PushGlobal(cmd)), Is.Zero,
                "Nothing was staged since the last push, so there is no reason to rebind either.");
        }

        [Test]
        public void PushDefaultToGlobal_ConstantBufferMode_UploadsEveryGroupEvenWhenOnlyOneIsAsked()
        {
            var uploader = CreateConstantBufferUploader();

            Assert.That(Recorded(cmd => uploader.PushDefaultToGlobal(cmd, GlobalShaderVariablesGroup.Base)), Is.EqualTo(CostOfUploadAndBind()),
                "A constant buffer is uploaded as a whole: group filtering cannot make the push any cheaper.");
        }
    }
}
