using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

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
                foreach (var shaderField in typeof(GlobalShaderVariablesBase).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Padding is never pushed, so it has no command to pay for.
                    if (shaderField.FieldType == typeof(float) && !shaderField.Name.StartsWith("_URPPadding", StringComparison.Ordinal))
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
            uploader.Push(m_Cmd);
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
        public void Push_AfterResetToDefault_PushesEveryVariable(
            [Values(GlobalShaderVariablesGroup.Base | GlobalShaderVariablesGroup.Only3D,
                GlobalShaderVariablesGroup.Base | GlobalShaderVariablesGroup.Only2D)] GlobalShaderVariablesGroup groups)
        {
            var uploader = new GlobalShaderVariablesUploader(groups);
            FillBaseDefaults(uploader);
            uploader.ResetToDefault();

            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.EqualTo(CostOfFullPush()),
                "ResetToDefault must mark everything the frame uses dirty.");
        }

        [Test]
        public void PushAfterCameraSetup_LooseUniforms_PushesTheStagedValuesTheSetupOverwrote()
        {
            var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.Base);
            FillBaseDefaults(uploader);
            uploader.PushDefault(m_Cmd);

            // A pass pushes a value of its own, which the camera setup must not drop.
            var pushed = new Vector4(5f, 6f, 7f, 8f);
            uploader._ScreenParams = pushed;
            uploader.Push(m_Cmd);

            Assert.That(Recorded(cmd => uploader.PushAfterCameraSetup(cmd)), Is.EqualTo(CostOfPushes(7)),
                "The native camera setup overwrites 7 base variables, and only those are pushed again.");
            Assert.That(uploader._ScreenParams, Is.EqualTo(pushed), "The staged value is kept, not reset to the camera's.");
            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.Zero, "Nothing is left pending.");
        }

        [Test]
        public void PushAfterCameraSetup_LooseUniforms_SendsTheStagedValueNotTheCameraDefault()
        {
            int screenParamsID = Shader.PropertyToID("_ScreenParams");
            Vector4 saved = Shader.GetGlobalVector(screenParamsID);
            try
            {
                var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.Base);
                FillBaseDefaults(uploader);
                uploader.PushDefault(m_Cmd);

                // A pass pushes a value of its own, then the engine's camera setup overwrites the loose uniform.
                var pushed = new Vector4(5f, 6f, 7f, 8f);
                uploader._ScreenParams = pushed;
                uploader.Push(m_Cmd);
                m_NativeCmd.SetGlobalVector(screenParamsID, new Vector4(-1f, -1f, -1f, -1f));

                uploader.PushAfterCameraSetup(m_Cmd);
                ExecuteTestCmd();

                Assert.That(Shader.GetGlobalVector(screenParamsID), Is.EqualTo(pushed),
                    "The value the pass pushed comes back after the camera setup, not the camera's.");
            }
            finally
            {
                Shader.SetGlobalVector(screenParamsID, saved);
            }
        }

        [Test]
        public void PushAfterCameraSetup_ConstantBufferMode_RecordsNothing()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            Assert.That(Recorded(cmd => uploader.PushAfterCameraSetup(cmd)), Is.Zero,
                "The native camera setup writes loose uniforms only: the bound buffer still holds the staged values.");
        }

        [Test]
        public void PushAfterCameraSetup_WithATemporaryCopyLeftOpen_Throws([Values] bool constantBuffer)
        {
            var uploader = constantBuffer ? CreateConstantBufferUploader() : CreateFlushedUploader();
            var leaked = uploader.Get();
            leaked._ScreenSize = k_TemporaryScreenSize;
            leaked.Push(m_Cmd);

            Assert.Throws<InvalidOperationException>(() => uploader.PushAfterCameraSetup(m_Cmd),
                "Like the other pushes of the staged variables, the one after the camera setup finds the copy still open.");
        }

        [Test]
        public void Push_WithNoPendingChange_RecordsNothing()
        {
            var uploader = CreateFlushedUploader();

            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.Zero,
                "Pushing twice in a row must not re-send anything.");
        }

        [Test]
        public void Setter_MarksOnlyItsOwnVariable()
        {
            var uploader = CreateFlushedUploader();

            uploader._ScreenSize = new Vector4(1920f, 1080f, 1f / 1920f, 1f / 1080f);

            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.EqualTo(CostOfPushes(1)),
                "Assigning one variable must push exactly that one.");
        }

        [Test]
        public void Setters_AccumulateUntilPushed()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);
            uploader._ZBufferParams = new Vector4(5f, 6f, 7f, 8f);
            uploader._GlobalMipBias = new Vector2(-1f, 0.5f);

            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.EqualTo(CostOfPushes(3)),
                "Every variable touched since the last push must be sent, and only those.");
        }

        [Test]
        public void Setter_AssignedTwice_PushesOnce()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);
            uploader._Time = new Vector4(5f, 6f, 7f, 8f);

            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.EqualTo(CostOfPushes(1)),
                "Flags are a set, not a counter.");
        }

        [Test]
        public void Push_ClearsFlags_SoNextChangePushesAlone()
        {
            var uploader = CreateFlushedUploader();

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);
            uploader.Push(m_Cmd);

            uploader._ScreenParams = new Vector4(9f, 9f, 9f, 9f);

            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.EqualTo(CostOfPushes(1)),
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
        public void PushDefault_DiscardsLocalChanges()
        {
            var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.All);
            FillBaseDefaults(uploader);

            uploader._Time = new Vector4(99f, 99f, 99f, 99f);

            uploader.PushDefault(m_Cmd);

            Assert.That(uploader._Time, Is.EqualTo(k_DefaultTime),
                "PushDefault re-baselines staging onto the frame defaults.");
        }

        [Test]
        public void PushDefault_RepushesEverythingEvenWhenOnlyOneVariableChanged()
        {
            var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.All);
            FillBaseDefaults(uploader);
            uploader.PushDefault(m_Cmd);

            uploader._Time = new Vector4(1f, 2f, 3f, 4f);

            Assert.That(Recorded(cmd => uploader.PushDefault(cmd)), Is.EqualTo(CostOfFullPush()),
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
        public void PushDefault_ConstantBufferMode_UploadsDefaultsOnceThenRecordsNothing()
        {
            var uploader = CreateConstantBufferUploader();

            Assert.That(Recorded(cmd => uploader.PushDefault(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "The first push of a camera has to get the frozen defaults onto the GPU.");

            Assert.That(Recorded(cmd => uploader.PushDefault(cmd)), Is.Zero,
                "The defaults buffer already holds exactly what an upload would write and still owns the slot, so " +
                "both the upload and the bind are pure waste.");
        }

        [Test]
        public void Push_ConstantBufferMode_OverrideDoesNotInvalidateDefaults()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            uploader._Time = new Vector4(99f, 99f, 99f, 99f);

            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "A diverged staging cannot be served by the defaults buffer, it goes to the overrides one.");

            Assert.That(Recorded(cmd => uploader.PushDefault(cmd)), Is.EqualTo(CostOfBind()),
                "The override wrote to the other buffer, so the defaults are still resident and only need a rebind.");
        }

        [Test]
        public void Push_ConstantBufferMode_OverrideAfterASkippedPushStillCostsTheDefaultsTheirBind()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            // A defaults push records nothing while its buffer is up to date and still bound. That skip must not be
            // read as "the slot is ours forever": the override below takes it over.
            uploader.PushDefault(m_Cmd);

            uploader._Time = new Vector4(99f, 99f, 99f, 99f);
            uploader.Push(m_Cmd);

            Assert.That(Recorded(cmd => uploader.PushDefault(cmd)), Is.EqualTo(CostOfBind()),
                "Whatever was skipped in between, the slot holds the override buffer and has to be bound back.");
        }

        [Test]
        public void EndFill_ConstantBufferMode_InvalidatesTheUploadedDefaults()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            // Next camera of the frame: same uploader, new defaults.
            FillBaseDefaults(uploader);

            Assert.That(Recorded(cmd => uploader.PushDefault(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "Rebinding here would bind the previous camera's values, the new defaults must reach the GPU first.");
        }

        [Test]
        public void Release_ConstantBufferMode_ForcesAReuploadOnTheNextPush()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            uploader.Release();

            Assert.That(Recorded(cmd => uploader.PushDefault(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "The buffer holding the defaults is gone, whatever replaces it starts empty.");
        }

        [Test]
        public void Push_ConstantBufferMode_WithNoPendingChange_RecordsNothing()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.Zero,
                "Nothing was staged since the last push, so there is no reason to rebind either.");
        }

        [Test]
        public void PushDefault_ConstantBufferMode_UploadsEveryGroupEvenWhenOnlyOneIsAsked()
        {
            var uploader = CreateConstantBufferUploader();

            Assert.That(Recorded(cmd => uploader.PushDefault(cmd, GlobalShaderVariablesGroup.Base)), Is.EqualTo(CostOfUploadAndBind()),
                "A constant buffer is uploaded as a whole: group filtering cannot make the push any cheaper.");
        }

        // Temporary copies (Get). A temporary copy records its restore into the command buffer it was made with,
        // so these tests measure what one step records on m_Cmd rather than on a fresh buffer. Emptied first: the payload
        // of an upload is aligned within the buffer, so only steps recorded from its start compare with the probes.
        int RecordedOnTestCmd(Action action)
        {
            m_NativeCmd.Clear();
            int before = m_NativeCmd.sizeInBytes;
            action();
            return m_NativeCmd.sizeInBytes - before;
        }

        // Runs what was recorded so far, so the global values it set can be read back with Shader.GetGlobalVector.
        void ExecuteTestCmd()
        {
            Graphics.ExecuteCommandBuffer(m_NativeCmd);
            m_NativeCmd.Clear();
        }

        static readonly int k_ScreenSizeID = Shader.PropertyToID("_ScreenSize");
        static readonly Vector4 k_StagedScreenSize = new Vector4(640f, 360f, 1f / 640f, 1f / 360f);
        static readonly Vector4 k_TemporaryScreenSize = new Vector4(256f, 256f, 1f / 256f, 1f / 256f);

        // Two compute shaders of the package, only used as targets of the recorded commands.
        static ComputeShader LoadComputeShader(string name) =>
            AssetDatabase.LoadAssetAtPath<ComputeShader>($"Packages/com.unity.render-pipelines.universal/Runtime/MipGen/Shaders/{name}.compute");

        // The same commands PushToCompute() records, as one sequence from the start of a fresh buffer.
        static int CostOfUploadAndComputeBind(ComputeShader computeShader)
        {
            using var probe = new CommandBuffer();
            var constantBuffer = new ConstantBuffer<GlobalShaderVariables>(ComputeBufferMode.Dynamic);

            int before = probe.sizeInBytes;
            constantBuffer.UpdateData(probe, default(GlobalShaderVariables));
            constantBuffer.Set(probe, computeShader, k_ProbeConstantBufferID);
            int cost = probe.sizeInBytes - before;

            constantBuffer.Release();
            return cost;
        }

        // What Dispose() records to unbind one compute shader: a null buffer removes its binding.
        static int CostOfComputeUnbind(ComputeShader computeShader)
        {
            using var probe = new CommandBuffer();
            int before = probe.sizeInBytes;
            probe.SetComputeConstantBufferParam(computeShader, k_ProbeConstantBufferID, (GraphicsBuffer)null, 0, 0);
            return probe.sizeInBytes - before;
        }

        [Test]
        public void Get_StartsFromTheStagedValues()
        {
            var uploader = CreateFlushedUploader();
            uploader._ScreenSize = k_StagedScreenSize;
            uploader.Push(m_Cmd);

            using var temporary = uploader.Get();

            Assert.That(temporary._ScreenSize, Is.EqualTo(k_StagedScreenSize),
                "A temporary copy reads what the passes before it staged, not the camera defaults.");
        }

        [Test]
        public void TemporaryFirstPush_LooseUniforms_PushesThePendingStagedValuesFirst()
        {
            var uploader = CreateFlushedUploader();
            uploader._ScreenSize = k_StagedScreenSize;
            var temporary = uploader.Get();
            temporary._Time = new Vector4(1f, 2f, 3f, 4f);

            Assert.That(RecordedOnTestCmd(() => temporary.Push(m_Cmd)), Is.EqualTo(CostOfPushes(2)),
                "Dispose() puts back what the GPU held, so the pending staged value goes out with the first push.");
            Assert.That(RecordedOnTestCmd(() => temporary.Push(m_Cmd)), Is.Zero, "Only the first push sends it.");

            ((IDisposable)temporary).Dispose();
            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.Zero, "Nothing is left pending on the staged values.");
        }

        [Test]
        public void TemporaryWithoutPush_ConstantBufferMode_LeavesThePendingChangeToTheParent()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);
            uploader._ScreenSize = k_StagedScreenSize;

            var temporary = uploader.Get();
            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.Zero,
                "The copy bound nothing, so there is nothing to put back: no global command on async compute.");
            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "The pending change is still the parent's, pushed by its next push.");
        }

        [Test]
        public void TemporaryDispose_RecordsOnTheCommandBufferOfTheLastPush()
        {
            var uploader = CreateFlushedUploader();
            var temporary = uploader.Get();
            temporary._ScreenSize = k_TemporaryScreenSize;
            temporary.Push(m_Cmd);

            using var other = new CommandBuffer();
            temporary.Push(CommandBufferHelpers.GetRasterCommandBuffer(other));

            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.Zero);
            Assert.That(other.sizeInBytes, Is.EqualTo(CostOfPushes(1)),
                "Dispose() has no command buffer of its own: it puts back on the one of the last push.");
        }

        [Test]
        public void TemporaryPush_LooseUniforms_IsUndoneByDispose()
        {
            Vector4 saved = Shader.GetGlobalVector(k_ScreenSizeID);
            try
            {
                var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.Base | GlobalShaderVariablesGroup.Only3D);
                FillBaseDefaults(uploader);
                uploader.PushDefault(m_Cmd);

                // A persistent change made by an earlier pass, like the upscaler does for _ScreenSize.
                uploader._ScreenSize = k_StagedScreenSize;
                uploader.Push(m_Cmd);

                using (var temporary = uploader.Get())
                {
                    temporary._ScreenSize = k_TemporaryScreenSize;
                    temporary.Push(m_Cmd);

                    ExecuteTestCmd();
                    Assert.That(Shader.GetGlobalVector(k_ScreenSizeID), Is.EqualTo(k_TemporaryScreenSize),
                        "The draws of the pass read the temporary value.");
                }

                ExecuteTestCmd();
                Assert.That(Shader.GetGlobalVector(k_ScreenSizeID), Is.EqualTo(k_StagedScreenSize),
                    "Dispose() restores the value staged before Get(), not the camera default: the earlier pass meant it.");
                Assert.That(uploader._ScreenSize, Is.EqualTo(k_StagedScreenSize),
                    "Temporary changes never reach the staged values.");
            }
            finally
            {
                Shader.SetGlobalVector(k_ScreenSizeID, saved);
            }
        }

        [Test]
        public void TemporaryDispose_LooseUniforms_RestoresOnlyWhatTheTemporaryCopyPushed()
        {
            var uploader = CreateFlushedUploader();
            var temporary = uploader.Get();

            temporary._Time = new Vector4(1f, 2f, 3f, 4f);
            temporary._ScreenSize = k_TemporaryScreenSize;
            temporary.Push(m_Cmd);

            // Changed but never pushed: the GPU never saw it, there is nothing to put back.
            temporary._ZBufferParams = new Vector4(5f, 6f, 7f, 8f);

            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.EqualTo(CostOfPushes(2)));
            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.Zero,
                "Nothing the temporary copy did is pending on the staged values.");
        }

        [Test]
        public void TemporaryDispose_WithoutAnyPush_RecordsNothing()
        {
            var uploader = CreateFlushedUploader();
            var temporary = uploader.Get();
            temporary._Time = new Vector4(1f, 2f, 3f, 4f);

            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.Zero);
        }

        [Test]
        public void TemporaryResetToDefault_LooseUniforms_PushesTheCameraDefaultsThenDisposeRestoresTheStagedValues()
        {
            var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.All);
            FillBaseDefaults(uploader);
            uploader.PushDefault(m_Cmd);
            uploader._Time = new Vector4(99f, 99f, 99f, 99f);
            uploader.Push(m_Cmd);

            var temporary = uploader.Get();
            temporary.ResetToDefault();

            Assert.That(temporary._Time, Is.EqualTo(k_DefaultTime));
            Assert.That(RecordedOnTestCmd(() => temporary.Push(m_Cmd)), Is.EqualTo(CostOfFullPush()));
            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.EqualTo(CostOfFullPush()));
            Assert.That(uploader._Time, Is.EqualTo(new Vector4(99f, 99f, 99f, 99f)));
        }

        [Test]
        public void TemporaryPushToCompute_LooseUniforms_PushesGloballyUntilDispose()
        {
            var computeShader = LoadComputeShader("ColorPyramid");
            var uploader = CreateFlushedUploader();
            var temporary = uploader.Get();
            temporary._ScreenSize = k_TemporaryScreenSize;

            Assert.That(RecordedOnTestCmd(() => temporary.PushToCompute(m_Cmd, computeShader)), Is.EqualTo(CostOfPushes(1)),
                "Compute shaders read the global value of what they do not set, and what they set sticks to them for good.");
            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.EqualTo(CostOfPushes(1)));
        }

        [Test]
        public void TemporaryPush_ConstantBufferMode_UploadsToItsOwnBufferAndDisposeOnlyRebinds()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            var temporary = uploader.Get();
            temporary._Time = new Vector4(99f, 99f, 99f, 99f);

            Assert.That(RecordedOnTestCmd(() => temporary.Push(m_Cmd)), Is.EqualTo(CostOfUploadAndBind()));
            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.EqualTo(CostOfBind()),
                "The temporary copy never wrote the defaults buffer, so getting them back is a bind.");
            Assert.That(Recorded(cmd => uploader.PushDefault(cmd)), Is.Zero,
                "The defaults buffer is uploaded and bound again, a defaults push has nothing left to do.");
        }

        [Test]
        public void TemporaryPush_ConstantBufferMode_KeepsTheOverridesOfEarlierPasses()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);
            uploader._Time = new Vector4(99f, 99f, 99f, 99f);
            uploader.Push(m_Cmd);

            var temporary = uploader.Get();
            temporary._ScreenSize = k_TemporaryScreenSize;

            Assert.That(RecordedOnTestCmd(() => temporary.Push(m_Cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "The temporary copy has its own buffer, the override buffer keeps the staged state.");
            Assert.That(OverrideBufferOf(temporary), Is.Not.Null.And.Not.SameAs(OverrideBufferOf(uploader)),
                "The temporary copy uploads to a buffer of its own, never to the parent's override buffer.");
            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.EqualTo(CostOfBind()),
                "The override buffer still holds the staged state, binding it back is enough.");
            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.Zero);
            Assert.That(uploader._Time, Is.EqualTo(new Vector4(99f, 99f, 99f, 99f)));
            Assert.That(Recorded(cmd => uploader.PushDefault(cmd)), Is.EqualTo(CostOfBind()),
                "Dispose() bound the override buffer back, not the defaults one, so the defaults need a bind again.");
        }

        static object OverrideBufferOf(GlobalShaderVariablesUploader uploader) =>
            typeof(GlobalShaderVariablesUploader).GetField("m_OverrideConstantBuffer", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(uploader);

        [Test]
        public void TemporaryDispose_ConstantBufferMode_BindsBackWhatWasBoundAtGet_EvenAfterAParentWrite()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            var temporary = uploader.Get();
            temporary._Time = new Vector4(99f, 99f, 99f, 99f);
            temporary.Push(m_Cmd);

            // Written on the parent while the temporary copy is open, without a push: it must not change what Dispose()
            // binds back, here the defaults buffer bound at Get().
            uploader._ScreenSize = k_StagedScreenSize;

            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.EqualTo(CostOfBind()));
            Assert.That(Recorded(cmd => uploader.Push(cmd)), Is.EqualTo(CostOfUploadAndBind()),
                "The parent's write stays pending until its own push.");
        }

        [Test]
        public void TemporaryDispose_LooseUniforms_LeavesAParentWriteToTheParentsNextPush()
        {
            Vector4 saved = Shader.GetGlobalVector(k_ScreenSizeID);
            try
            {
                var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.Base | GlobalShaderVariablesGroup.Only3D);
                FillBaseDefaults(uploader);
                uploader.PushDefault(m_Cmd);
                Vector4 cameraScreenSize = uploader._ScreenSize;

                using (var temporary = uploader.Get())
                {
                    temporary._Time = new Vector4(99f, 99f, 99f, 99f);
                    temporary.Push(m_Cmd);

                    uploader._ScreenSize = k_StagedScreenSize;
                }

                ExecuteTestCmd();
                Assert.That(Shader.GetGlobalVector(k_ScreenSizeID), Is.EqualTo(cameraScreenSize),
                    "Dispose() only puts back what the copy pushed: the parent's write waits for its own push, as with the constant buffer.");

                uploader.Push(m_Cmd);
                ExecuteTestCmd();
                Assert.That(Shader.GetGlobalVector(k_ScreenSizeID), Is.EqualTo(k_StagedScreenSize));
            }
            finally
            {
                Shader.SetGlobalVector(k_ScreenSizeID, saved);
            }
        }

        [Test]
        public void TemporaryResetToDefault_ConstantBufferMode_BindsTheParentDefaultsBuffer()
        {
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);
            uploader._Time = new Vector4(99f, 99f, 99f, 99f);
            uploader.Push(m_Cmd);

            var temporary = uploader.Get();
            temporary.ResetToDefault();

            Assert.That(RecordedOnTestCmd(() => temporary.Push(m_Cmd)), Is.EqualTo(CostOfBind()),
                "The camera defaults are already uploaded in the parent's defaults buffer.");
            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.EqualTo(CostOfBind()));
        }

        [Test]
        public void TemporaryPushToCompute_ConstantBufferMode_BindsEachComputeShaderAndDisposeUnbindsThem()
        {
            var colorPyramid = LoadComputeShader("ColorPyramid");
            var depthPyramid = LoadComputeShader("DepthPyramid");
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            var temporary = uploader.Get();
            temporary._ScreenSize = k_TemporaryScreenSize;

            Assert.That(RecordedOnTestCmd(() => temporary.PushToCompute(m_Cmd, colorPyramid)), Is.EqualTo(CostOfUploadAndComputeBind(colorPyramid)));
            Assert.That(RecordedOnTestCmd(() => temporary.PushToCompute(m_Cmd, depthPyramid)), Is.EqualTo(CostOfUploadAndComputeBind(depthPyramid)));
            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.EqualTo(CostOfComputeUnbind(colorPyramid) + CostOfComputeUnbind(depthPyramid)),
                "The global slot was never taken: Dispose() only unbinds the compute shaders, which read the global buffer again.");
        }

        [Test]
        public void ContextItemGet_ReturnsTheTemporaryCopyOfItsUploader()
        {
            var uploader = CreateFlushedUploader();
            var shaderData = new UniversalGlobalShaderData();
            shaderData.Set(uploader);
            var shaderData2D = new Universal2DGlobalShaderData();
            shaderData2D.Set(uploader);

            var temporary = shaderData.Get();
            temporary.Dispose();
            var temporary2D = shaderData2D.Get();
            temporary2D.Dispose();

            Assert.That(temporary2D, Is.SameAs(temporary), "One temporary copy per uploader, reused, so a pass allocates nothing.");
            Assert.That(temporary, Is.Not.SameAs(uploader));
            Assert.That(shaderData.GetGlobal(), Is.SameAs(uploader), "GetGlobal() hands out the renderer's uploader itself.");
            Assert.That(shaderData2D.GetGlobal(), Is.SameAs(uploader));
        }

        [Test]
        public void ContextItemGet_WhileTheRenderGraphRecords_Throws()
        {
            // The renderer only sets the uploader when the render graph executes, the item is empty while it records.
            Assert.Throws<InvalidOperationException>(() => new UniversalGlobalShaderData().Get());
            Assert.Throws<InvalidOperationException>(() => new Universal2DGlobalShaderData().Get());
            Assert.Throws<InvalidOperationException>(() => new UniversalGlobalShaderData().GetGlobal());
            Assert.Throws<InvalidOperationException>(() => new Universal2DGlobalShaderData().GetGlobal());
        }

        [Test]
        public void Push_WithATemporaryCopyLeftOpen_Throws()
        {
            var uploader = CreateFlushedUploader();
            var temporary = uploader.Get();
            temporary._ScreenSize = k_TemporaryScreenSize;
            temporary.Push(m_Cmd);

            Assert.Throws<InvalidOperationException>(() => uploader.Push(m_Cmd));
            Assert.Throws<ObjectDisposedException>(() => temporary.Push(m_Cmd), "The temporary copy is closed.");
            Assert.DoesNotThrow(() => uploader.Push(m_Cmd), "Once closed, the copy no longer blocks the pushes.");
        }

        [Test]
        public void Get_WithATemporaryCopyLeftOpen_ThrowsAndRecordsNothingOnItsCommandBuffer([Values] bool constantBuffer)
        {
            var uploader = constantBuffer ? CreateConstantBufferUploader() : CreateFlushedUploader();
            using var earlierPassCmd = new CommandBuffer();
            var leaked = uploader.Get();
            leaked._ScreenSize = k_TemporaryScreenSize;
            leaked.Push(CommandBufferHelpers.GetComputeCommandBuffer(earlierPassCmd));
            earlierPassCmd.Clear();

            Assert.Throws<InvalidOperationException>(() => uploader.Get());
            Assert.That(earlierPassCmd.sizeInBytes, Is.Zero,
                "The command buffer of the leaked copy's last push can belong to an earlier pass, or be back in the pool.");

            using var temporary = uploader.Get();
            Assert.That(temporary, Is.Not.Null, "Once closed, the copy no longer blocks Get().");
        }

        [Test]
        public void Push_WithATemporaryCopyOnlyRead_DoesNotThrow([Values] bool constantBuffer)
        {
            var uploader = constantBuffer ? CreateConstantBufferUploader() : CreateFlushedUploader();
            _ = uploader.Get()._ScreenSize;

            Assert.DoesNotThrow(() => uploader.Push(m_Cmd), "A copy that was only read has nothing to put back.");
            using var temporary = uploader.Get();
            Assert.That(temporary, Is.Not.Null);
        }

        [Test]
        public void Get_WithATemporaryCopyOnlyRead_HandsItOutAgain()
        {
            var uploader = CreateFlushedUploader();
            var readOnly = uploader.Get();
            _ = readOnly._ScreenSize;

            using var temporary = uploader.Get();
            Assert.That(temporary, Is.SameAs(readOnly));
        }

        [Test]
        public void Get_WithATemporaryCopyChangedButNotPushed_Throws()
        {
            var uploader = CreateFlushedUploader();
            uploader.Get()._ScreenSize = k_TemporaryScreenSize;

            Assert.Throws<InvalidOperationException>(() => uploader.Get(),
                "Handing out the same object again would drop the change.");
        }

        [Test]
        public void TemporaryPush_AfterDispose_Throws()
        {
            var uploader = CreateFlushedUploader();
            var temporary = uploader.Get();
            ((IDisposable)temporary).Dispose();

            Assert.Throws<ObjectDisposedException>(() => temporary.Push(m_Cmd));
            Assert.Throws<ObjectDisposedException>(() => temporary.PushToCompute(m_Cmd, LoadComputeShader("ColorPyramid")));
            Assert.That(RecordedOnTestCmd(() => ((IDisposable)temporary).Dispose()), Is.Zero, "Disposing twice is a no-op.");
        }

        [Test]
        public void Get_WhileFillingTheDefaults_Throws()
        {
            var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.All);
            uploader.BeginFill();

            Assert.Throws<InvalidOperationException>(() => uploader.Get());
        }

        [Test]
        public void EndRenderGraph_WithATemporaryCopyNeverDisposed_LogsAnErrorAndRecovers()
        {
            var uploader = CreateFlushedUploader();
            uploader.Get()._ScreenSize = k_TemporaryScreenSize;

            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("wasn't disposed"));
            using var cameraCmd = new CommandBuffer();
            uploader.EndRenderGraph(cameraCmd);
            FillBaseDefaults(uploader);

            using var temporary = uploader.Get();
            Assert.That(temporary, Is.Not.Null, "The next camera can use a temporary copy again.");
        }

        [Test]
        public void EndRenderGraph_WithAPushedTemporaryCopyNeverDisposed_RestoresOnTheCameraCommandBuffer([Values] bool constantBuffer)
        {
            Vector4 saved = Shader.GetGlobalVector(k_ScreenSizeID);
            try
            {
                GlobalShaderVariablesUploader uploader;
                if (constantBuffer)
                    uploader = CreateConstantBufferUploader();
                else
                {
                    uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.Base | GlobalShaderVariablesGroup.Only3D);
                    FillBaseDefaults(uploader);
                }
                uploader.PushDefault(m_Cmd);
                Vector4 cameraScreenSize = uploader._ScreenSize;

                var leaked = uploader.Get();
                leaked._ScreenSize = k_TemporaryScreenSize;
                leaked.Push(m_Cmd);
                // The constant buffer tests never run their commands: the global slot would keep a test buffer.
                if (!constantBuffer)
                    ExecuteTestCmd();

                LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("wasn't disposed"));
                using var cameraCmd = new CommandBuffer();
                uploader.EndRenderGraph(cameraCmd);

                Assert.That(cameraCmd.sizeInBytes, Is.EqualTo(constantBuffer ? CostOfBind() : CostOfPushes(1)),
                    "The end of the camera puts back what the copy changed, on the camera's command buffer.");
                if (!constantBuffer)
                {
                    Graphics.ExecuteCommandBuffer(cameraCmd);
                    Assert.That(Shader.GetGlobalVector(k_ScreenSizeID), Is.EqualTo(cameraScreenSize));
                }
            }
            finally
            {
                Shader.SetGlobalVector(k_ScreenSizeID, saved);
            }
        }

        [Test]
        public void EndRenderGraph_WithATemporaryCopyOnlyRead_LogsAndRecordsNothing()
        {
            var uploader = CreateFlushedUploader();
            _ = uploader.Get()._ScreenSize;

            using var cameraCmd = new CommandBuffer();
            uploader.EndRenderGraph(cameraCmd);

            Assert.That(cameraCmd.sizeInBytes, Is.Zero, "A copy that was only read has nothing to put back.");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void EndRenderGraph_ConstantBufferMode_UnbindsTheComputeShadersATemporaryCopyLeftBound([Values] bool disposedFromARasterPass)
        {
            var computeShader = LoadComputeShader("ColorPyramid");
            var uploader = CreateConstantBufferUploader();
            uploader.PushDefault(m_Cmd);

            var temporary = uploader.Get();
            temporary._ScreenSize = k_TemporaryScreenSize;
            temporary.PushToCompute(m_Cmd, computeShader);
            using var raster = new CommandBuffer();
            if (disposedFromARasterPass)
            {
                temporary.Push(CommandBufferHelpers.GetRasterCommandBuffer(raster));
                ((IDisposable)temporary).Dispose();
            }
            else
                LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("wasn't disposed"));

            using var cameraCmd = new CommandBuffer();
            uploader.EndRenderGraph(cameraCmd);
            Assert.That(cameraCmd.sizeInBytes, Is.EqualTo(CostOfComputeUnbind(computeShader)),
                "The compute shader's binding wins over the global slot: the end of the camera unbinds it.");
        }
    }
}
