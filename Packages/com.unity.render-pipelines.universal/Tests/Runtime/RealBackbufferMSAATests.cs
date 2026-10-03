using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    // Verifies the real back buffer MSAA optimization end to end: URP requests a single-sampled back
    // buffer for frames where no camera renders MSAA directly into it, and keeps it multisampled
    // otherwise. Each scenario asserts:
    //   - the *requested* count (Screen.msaaSamples) settles to the expected value, both per frame and at
    //     the instant each camera renders (so a one-frame apply lag is caught),
    //   - the *current* back buffer count (Screen.currentBackbufferMSAASamples) - the actual applied count -
    //     agrees with the request at the instant each camera renders, and
    //   - that each on-screen camera actually rendered the way the scenario intends - directly to the back
    //     buffer, or through an intermediate - via UniversalResourceData.isActiveTargetBackBuffer probed
    //     after opaques. This makes the tests robust: a camera that is silently forced through an
    //     intermediate (e.g. by some default setting) is caught instead of masquerading as "direct".
    //
    // "Uses an intermediate" is forced directly by a ScriptableRenderPass with
    // requiresIntermediateTexture = true (injected per camera via beginCameraRendering), so the tests are
    // decoupled from how any specific URP feature happens to trigger an intermediate.
    //
    // The fixture is skipped where MSAA must be resolved explicitly (editor, desktop, Intel/AMD Mac): the
    // request is always 1 there with nothing to discriminate. The meaningful coverage runs on the
    // device/player jobs (Android/iOS/Metal) via the Foundation/PostPro testables.
    [TestFixture]
    class RealBackbufferMSAATests
    {
        const int k_AssetMsaaSampleCount = 4;
        // Downgrade hysteresis + one-frame apply lag + margin for frame-pacing jitter.
        const int k_SettleFrames = UniversalRenderPipeline.realBackbufferMSAADowngradeDelayFrames + 1 + 3;
        const int k_StabilityFrames = 8;
        // Upgrades skip the hysteresis, so they must apply promptly: 1 frame to set the request + 1 frame for
        // the backend to reallocate. Strict bound to catch a regression that delays re-enabling back buffer
        // MSAA when a camera needs it.
        const int k_UpgradeFrames = 3;

        RenderPipelineAsset m_PreviousQualityRenderPipelineAsset;
        int m_PreviousMsaaSamples;
        UniversalRenderPipelineAsset m_Asset;
        UniversalRendererData m_RendererData;

        readonly List<Camera> m_DisabledCameras = new();
        readonly List<Object> m_TestObjects = new();
        readonly HashSet<Camera> m_ProbeCameras = new();        // on-screen cameras whose target we verify
        readonly HashSet<Camera> m_IntermediateCameras = new(); // cameras expected to use an intermediate
        readonly ForceIntermediatePass m_ForceIntermediatePass = new();
        readonly ActiveTargetProbePass m_ProbePass = new();

        // Per-frame applied back buffer MSAA, dumped in the failure diagnostics so a transition or
        // reallocation lag is visible frame by frame, not just at the settled state.
        readonly List<int> m_CurrentBackbufferMsaaPerFrame = new();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Capture the global state we may mutate BEFORE any early-out. NUnit runs OneTimeTearDown even
            // when OneTimeSetUp calls Assert.Ignore, so these must already hold the real previous values -
            // otherwise teardown restores defaults (null pipeline / 0 samples) and clobbers the active URP
            // asset for the rest of the test session (silently dropping sibling graphics tests to BiRP).
            m_PreviousQualityRenderPipelineAsset = QualitySettings.renderPipeline;
            m_PreviousMsaaSamples = Screen.msaaSamples;

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                IgnoreWithLog("These tests only run when URP is the active render pipeline.");

            // No multisampled back buffer where MSAA is resolved explicitly (editor, desktop, Intel/AMD Mac),
            // so the request is always 1 and there is nothing to discriminate. Real coverage runs on the
            // device/player jobs (see fixture header).
            if (UniversalRenderer.PlatformRequiresExplicitMsaaResolve())
                IgnoreWithLog("Platform resolves MSAA explicitly (no multisampled back buffer); the real back buffer MSAA request is always 1, so there is nothing to test here.");

            // Under XR, URP renders into the XR-owned back buffer and never multisamples it directly, so the
            // optimization is inactive. A global gate (rather than URP's per-camera xr.enabled check) is enough
            // to skip the device XR and mock-XR jobs, where the fixture is meaningless.
            if (XRSystem.displayActive || XRGraphicsAutomatedTests.enabled)
                IgnoreWithLog("XR is active; URP renders into the XR-owned back buffer and never multisamples it directly, so there is nothing to test here.");

            // Frame isolation: only cameras a test creates should contribute to the per-frame back
            // buffer MSAA aggregation, so disable any cameras already present in the scene.
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            {
                if (!cam.enabled)
                    continue;
                cam.enabled = false;
                m_DisabledCameras.Add(cam);
            }

            m_Asset = CreateAsset();
            m_RendererData = (UniversalRendererData)m_Asset.rendererDataList[0];
            QualitySettings.renderPipeline = m_Asset;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            QualitySettings.renderPipeline = m_PreviousQualityRenderPipelineAsset;

            // Don't leak the changed back buffer MSAA request into sibling fixtures in the same run.
            if (Screen.msaaSamples != m_PreviousMsaaSamples)
                Screen.SetMSAASamples(m_PreviousMsaaSamples);

            foreach (var cam in m_DisabledCameras)
            {
                if (cam != null)
                    cam.enabled = true;
            }
            m_DisabledCameras.Clear();

            if (m_RendererData != null)
                Object.DestroyImmediate(m_RendererData);
            if (m_Asset != null)
                Object.DestroyImmediate(m_Asset);
        }

        [SetUp]
        public void SetUp()
        {
            m_Asset.msaaSampleCount = k_AssetMsaaSampleCount;
        }

        [TearDown]
        public void TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
                LogFailureDiagnostics();

            // Detach render textures from cameras before releasing them (releasing an RT still set as
            // Camera.targetTexture logs an error that would fail the test).
            foreach (var obj in m_TestObjects)
            {
                if (obj is GameObject go && go != null && go.TryGetComponent<Camera>(out var cam))
                    cam.targetTexture = null;
            }

            foreach (var obj in m_TestObjects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            m_TestObjects.Clear();
            m_ProbeCameras.Clear();
            m_IntermediateCameras.Clear();
            m_ProbePass.observedActiveTargetIsBackbuffer.Clear();
            m_ProbePass.observedCurrentBackbufferMsaa.Clear();
            m_ProbePass.observedRequestedMsaa.Clear();
            m_CurrentBackbufferMsaaPerFrame.Clear();
        }

        // Injects the per-camera passes: the active-target probe on every on-screen camera we verify, and
        // the force-intermediate pass on cameras the test marks as "uses an intermediate". Done via
        // beginCameraRendering + EnqueuePass so it doesn't depend on any URP feature.
        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                return;

            if (m_IntermediateCameras.Contains(camera))
                data.scriptableRenderer.EnqueuePass(m_ForceIntermediatePass);
            if (m_ProbeCameras.Contains(camera))
                data.scriptableRenderer.EnqueuePass(m_ProbePass);
        }

        // ---- Scenarios ----------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator SingleCamera_DirectToBackbuffer_KeepsMSAA()
        {
            CreateScreenCamera(intermediate: false);
            yield return SettleAndAssert(needsRealBackbufferMSAA: true);
        }

        [UnityTest]
        public IEnumerator SingleCamera_DirectButCameraMSAADisabled_DisablesMSAA()
        {
            // Even when rendering directly to the back buffer, a camera with allowMSAA == false doesn't need
            // it multisampled - one of the per-camera checks the previous optimization lacked.
            var camera = CreateScreenCamera(intermediate: false);
            camera.allowMSAA = false;
            yield return SettleAndAssert(needsRealBackbufferMSAA: false);
        }

        [UnityTest]
        public IEnumerator SingleCamera_Intermediate_DisablesMSAA()
        {
            CreateScreenCamera(intermediate: true);
            yield return SettleAndAssert(needsRealBackbufferMSAA: false);
        }

        [UnityTest]
        public IEnumerator MultipleCameras_AllDirect_KeepsMSAA()
        {
            CreateScreenCamera(intermediate: false);
            CreateScreenCamera(intermediate: false);
            yield return SettleAndAssert(needsRealBackbufferMSAA: true);
        }

        [UnityTest]
        public IEnumerator MultipleCameras_AllIntermediate_DisablesMSAA()
        {
            CreateScreenCamera(intermediate: true);
            CreateScreenCamera(intermediate: true);
            yield return SettleAndAssert(needsRealBackbufferMSAA: false);
        }

        [UnityTest]
        public IEnumerator MultipleCameras_MixedDirectAndIntermediate_KeepsMSAA()
        {
            CreateScreenCamera(intermediate: true);   // resolves through an intermediate
            CreateScreenCamera(intermediate: false);  // renders direct -> vetoes, keeps MSAA on
            yield return SettleAndAssert(needsRealBackbufferMSAA: true);
        }

        [UnityTest]
        public IEnumerator CameraStack_BaseUsesIntermediate_DisablesMSAA()
        {
            // A base camera with a stack always composites through an intermediate, so the back buffer
            // only receives the resolved single-sampled result. Mark the base as intermediate so the
            // active-target probe expects (and confirms) the intermediate.
            var baseCamera = CreateScreenCamera(intermediate: true);
            var baseData = baseCamera.GetComponent<UniversalAdditionalCameraData>();

            var overlayGo = new GameObject("MSAA Test Overlay Camera");
            m_TestObjects.Add(overlayGo);
            var overlayCamera = overlayGo.AddComponent<Camera>();
            var overlayData = overlayGo.AddComponent<UniversalAdditionalCameraData>();
            overlayData.renderType = CameraRenderType.Overlay;
            overlayData.SetRenderer(0);

            Assume.That(baseData.TryAddCameraToStack(overlayCamera), "Could not add the overlay camera to the stack.");

            yield return SettleAndAssert(needsRealBackbufferMSAA: false);
        }

        [UnityTest]
        public IEnumerator SingleCamera_RenderTexture_DisablesMSAA()
        {
            CreateRenderTextureCamera();
            // Nothing renders to the real back buffer, so it doesn't need MSAA.
            yield return SettleAndAssert(needsRealBackbufferMSAA: false);
        }

        [UnityTest]
        public IEnumerator TileOnly_DirectCamera_KeepsMSAA()
        {
            // Tile-only must be set at renderer construction, so use a dedicated asset. Tile-only's whole
            // point is to keep rendering on-tile and avoid intermediates, so a direct camera should render
            // straight to the (multisampled) back buffer - the probe confirms no intermediate is added.
            var previousAsset = QualitySettings.renderPipeline;
            var tileOnlyAsset = CreateAsset(tileOnly: true);
            QualitySettings.renderPipeline = tileOnlyAsset;
            try
            {
                CreateScreenCamera(intermediate: false);

                // Let the renderer build, then confirm tile-only actually engaged on this platform.
                yield return null;
                var renderer = tileOnlyAsset.GetRenderer(0) as UniversalRenderer;
                if (renderer == null || !renderer.useTileOnlyMode)
                    Assert.Ignore("Tile-only mode is not active on this platform.");

                yield return SettleAndAssert(needsRealBackbufferMSAA: true);
            }
            finally
            {
                QualitySettings.renderPipeline = previousAsset;
                var rendererData = tileOnlyAsset.rendererDataList[0];
                Object.DestroyImmediate(tileOnlyAsset);
                if (rendererData != null)
                    Object.DestroyImmediate(rendererData);
            }
        }

        [UnityTest]
        public IEnumerator RenderRequest_DoesNotChangeBackbufferMSAA()
        {
            // Establish a converged state with an on-screen camera.
            CreateScreenCamera(intermediate: false);
            yield return TickFrames(k_SettleFrames);
            int requestedBefore = Screen.msaaSamples;

            // A camera that would render directly to the back buffer, but only ever via a render request
            // (disabled so it never participates in the normal frame loop).
            var go = new GameObject("MSAA Test Request Camera");
            m_TestObjects.Add(go);
            var requestCamera = go.AddComponent<Camera>();
            requestCamera.allowMSAA = true;
            requestCamera.enabled = false;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.SetRenderer(0);

            var rt = new RenderTexture(128, 128, 16);
            m_TestObjects.Add(rt);

            // SubmitRenderRequest renders synchronously, so the before/after read brackets exactly its effect.
            RenderPipeline.SubmitRenderRequest(requestCamera, new RenderPipeline.StandardRequest { destination = rt });

            Assert.AreEqual(requestedBefore, Screen.msaaSamples,
                "A render request must not change the real back buffer MSAA request.");
        }

        [UnityTest]
        public IEnumerator Transition_CameraMSAATogglesWithinSession_BackbufferFollows()
        {
            // Drives the real reallocation path the fresh-fixture scenarios never hit: transitions within one
            // continuous session (4 -> 1 -> 4). AssertStableForFrames adds the steady-state no-churn check.
            int multisampledCount = m_Asset.msaaSampleCount;
            var camera = CreateScreenCamera(intermediate: false);

            // Direct camera with MSAA -> the back buffer must be multisampled, and stay there.
            yield return SettleAndAssert(needsRealBackbufferMSAA: true);
            yield return AssertStableForFrames(multisampledCount, k_StabilityFrames);

            // Same camera still rendering directly, but MSAA off -> the back buffer must drop to single-sampled.
            int before = Screen.currentBackbufferMSAASamples;
            camera.allowMSAA = false;
            yield return SettleAndAssert(needsRealBackbufferMSAA: false);
            Assert.AreNotEqual(before, Screen.currentBackbufferMSAASamples, "Expected the back buffer MSAA to transition down when MSAA was disabled.");
            yield return AssertStableForFrames(1, k_StabilityFrames);

            // MSAA back on -> the back buffer must become multisampled again, and stay there.
            before = Screen.currentBackbufferMSAASamples;
            camera.allowMSAA = true;
            yield return SettleAndAssert(needsRealBackbufferMSAA: true);
            Assert.AreNotEqual(before, Screen.currentBackbufferMSAASamples, "Expected the back buffer MSAA to transition back up when MSAA was re-enabled.");
            yield return AssertStableForFrames(multisampledCount, k_StabilityFrames);
        }

        // ---- Helpers ------------------------------------------------------------------------------

        // Assert.Ignore writes its reason to the NUnit result XML, but not to the player log. On device/player
        // jobs the player log (TestRunnerLog.txt) is what's at hand, so mirror the skip reason and the caps
        // that drove it there first - this is how we confirm, from the device itself, why a platform skipped
        // (e.g. what supportsMultisampleAutoResolve / supportsMultisampledBackBuffer actually report on iOS).
        static void IgnoreWithLog(string reason)
        {
            Debug.Log($"[RealBackbufferMSAATests] Skipping fixture: {reason} " +
                $"(graphicsDeviceType={SystemInfo.graphicsDeviceType}, " +
                $"supportsMultisampleAutoResolve={SystemInfo.supportsMultisampleAutoResolve}, " +
                $"supportsMultisampledBackBuffer={SystemInfo.supportsMultisampledBackBuffer}, " +
                $"explicitMsaaResolve={UniversalRenderer.PlatformRequiresExplicitMsaaResolve()}, " +
                $"xrDisplayActive={XRSystem.displayActive}, xrAutomatedTests={XRGraphicsAutomatedTests.enabled})");
            Assert.Ignore(reason);
        }

        static bool IsForcedUGK() =>
            System.Array.Exists(System.Environment.GetCommandLineArgs(),
                arg => arg.Equals("-force-ugk", System.StringComparison.OrdinalIgnoreCase));

        // Ticks 'count' frames, recording the applied back buffer MSAA each one for the diagnostics timeline.
        IEnumerator TickFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
                m_CurrentBackbufferMsaaPerFrame.Add(Screen.currentBackbufferMSAASamples);
            }
        }

        // Asserts the applied back buffer MSAA stays at 'expected' for 'count' frames - the steady-state
        // no-churn check. Observes the applied count, so it can't see a redundant SetMSAASamples(sameValue)
        // reallocation (no managed hook for that).
        IEnumerator AssertStableForFrames(int expected, int count)
        {
            int start = m_CurrentBackbufferMsaaPerFrame.Count;
            yield return TickFrames(count);
            for (int i = start; i < m_CurrentBackbufferMsaaPerFrame.Count; i++)
                Assert.AreEqual(expected, m_CurrentBackbufferMsaaPerFrame[i],
                    $"Back buffer MSAA churned at steady state (frame index {i} was {m_CurrentBackbufferMsaaPerFrame[i]}, expected a stable {expected}). " +
                    $"Current MSAA per ticked frame: [{string.Join(", ", m_CurrentBackbufferMsaaPerFrame)}]");
        }

        // Establishes an MSAA-on baseline the real way: a needing camera appears (back buffer goes
        // multisampled), then leaves - so the off-test measures a genuine downgrade, not an already-off state.
        IEnumerator EstablishMsaaOn()
        {
            int multisampledCount = m_Asset.msaaSampleCount;
            var go = new GameObject("MSAA Test Baseline Camera");
            m_TestObjects.Add(go);
            var camera = go.AddComponent<Camera>();
            camera.allowMSAA = true;
            camera.allowHDR = false;
            camera.allowDynamicResolution = false;
            camera.targetTexture = null;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.SetRenderer(0);
            data.renderPostProcessing = false;

            // Tick at least once with the camera present (even if the back buffer is already multisampled
            // from a prior test) so the helper actually drives the baseline and resets the downgrade clock,
            // making the following downgrade deterministic.
            for (int i = 0; i < k_SettleFrames; i++)
            {
                yield return null;
                m_CurrentBackbufferMsaaPerFrame.Add(Screen.currentBackbufferMSAASamples);
                if (Screen.currentBackbufferMSAASamples == multisampledCount)
                    break;
            }
            Assert.AreEqual(multisampledCount, Screen.currentBackbufferMSAASamples,
                "Failed to establish the MSAA-on baseline before measuring the downgrade.");

            Object.DestroyImmediate(go);
        }

        IEnumerator SettleAndAssert(bool needsRealBackbufferMSAA)
        {
            // The fixture only runs where the platform supports a multisampled back buffer (see
            // OneTimeSetUp), so a camera that needs it keeps the asset's sample count; otherwise it's 1.
            int expectedRequest = needsRealBackbufferMSAA ? m_Asset.msaaSampleCount : 1;

            // Arrange the opposite baseline (asserted), so each test measures a real transition rather
            // than an incidental steady state.
            if (needsRealBackbufferMSAA)
            {
                // Off baseline via the cameras' off form - allowMSAA=false still renders direct, so routing
                // is unchanged - then enable MSAA and require the upgrade within its tight bound.
                foreach (var camera in m_ProbeCameras)
                    camera.allowMSAA = false;
                yield return TickFrames(k_SettleFrames);
                Assert.AreEqual(1, Screen.currentBackbufferMSAASamples,
                    "Failed to establish the MSAA-off baseline before measuring the upgrade.");

                foreach (var camera in m_ProbeCameras)
                    camera.allowMSAA = true;
                yield return TickFrames(k_UpgradeFrames);
            }
            else
            {
                yield return EstablishMsaaOn();
                yield return TickFrames(k_SettleFrames);
            }

            // What the optimization decided to request.
            Assert.AreEqual(expectedRequest, Screen.msaaSamples,
                $"Requested real back buffer MSAA should be {expectedRequest} (needs={needsRealBackbufferMSAA}).");

            foreach (var camera in m_ProbeCameras)
            {
                // Confirm the camera actually rendered the way the scenario intends, so a camera silently
                // forced through an intermediate can't pass as "direct" (or vice versa).
                bool expectedDirect = !m_IntermediateCameras.Contains(camera);
                Assert.IsTrue(m_ProbePass.observedActiveTargetIsBackbuffer.TryGetValue(camera, out bool renderedDirect),
                    $"Camera '{camera.name}' did not render, so its back buffer usage couldn't be probed.");
                Assert.AreEqual(expectedDirect, renderedDirect,
                    $"Camera '{camera.name}' should render {(expectedDirect ? "directly to the back buffer (no intermediate)" : "through an intermediate")}.");

                // Confirm the request is already settled to the expected count at the instant the camera
                // renders - not just by teardown - so a one-frame apply lag would be caught.
                Assert.IsTrue(m_ProbePass.observedRequestedMsaa.TryGetValue(camera, out int requestedAtRender),
                    $"Camera '{camera.name}' did not render, so its requested back buffer MSAA couldn't be probed.");
                Assert.AreEqual(expectedRequest, requestedAtRender,
                    $"Camera '{camera.name}' requested back buffer MSAA at render time should be {expectedRequest}.");

                // The actual applied count must match the request once settled (SettleFrames absorbs the
                // one-frame apply lag), so any remaining disagreement is a real bug.
                Assert.IsTrue(m_ProbePass.observedCurrentBackbufferMsaa.TryGetValue(camera, out int currentAtRender),
                    $"Camera '{camera.name}' did not render, so its current back buffer MSAA couldn't be probed.");
                Assert.AreEqual(expectedRequest, currentAtRender,
                    $"Camera '{camera.name}' current back buffer MSAA at render time should be {expectedRequest}.");
            }
        }

        // On any failure in this fixture, dump the captured back buffer MSAA state so CI logs explain why
        // without needing a device repro: the platform/caps, the requested vs current counts, and per camera
        // how it actually rendered (direct vs intermediate) and the current back buffer MSAA seen at render time.
        void LogFailureDiagnostics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RealBackbufferMSAATests] '{TestContext.CurrentContext.Test.Name}' failed. Captured state:");
            sb.AppendLine($"  graphicsDeviceType={SystemInfo.graphicsDeviceType}, supportsMultisampledBackBuffer={SystemInfo.supportsMultisampledBackBuffer}, explicitMsaaResolve={UniversalRenderer.PlatformRequiresExplicitMsaaResolve()}");
            sb.AppendLine($"  asset.msaaSampleCount={m_Asset.msaaSampleCount}, Screen.msaaSamples(requested)={Screen.msaaSamples}, Screen.currentBackbufferMSAASamples(in teardown)={Screen.currentBackbufferMSAASamples}");
            foreach (var camera in m_ProbeCameras)
            {
                string rendered = m_ProbePass.observedActiveTargetIsBackbuffer.TryGetValue(camera, out bool isBackbuffer)
                    ? (isBackbuffer ? "direct" : "intermediate") : "<not rendered>";
                string current = m_ProbePass.observedCurrentBackbufferMsaa.TryGetValue(camera, out int currentMsaa)
                    ? currentMsaa.ToString() : "<not rendered>";
                string requestedAtProbe = m_ProbePass.observedRequestedMsaa.TryGetValue(camera, out int reqMsaa)
                    ? reqMsaa.ToString() : "<not rendered>";
                sb.AppendLine($"  camera '{camera.name}': expectedIntermediate={m_IntermediateCameras.Contains(camera)}, rendered={rendered}, requestedMSAA(at render time)={requestedAtProbe}, currentBackbufferMSAA(at render time)={current}");
            }
            sb.AppendLine($"  current back buffer MSAA per ticked frame: [{string.Join(", ", m_CurrentBackbufferMsaaPerFrame)}]");
            Debug.Log(sb.ToString());
        }

        Camera CreateScreenCamera(bool intermediate)
        {
            var go = new GameObject("MSAA Test Screen Camera");
            m_TestObjects.Add(go);
            var camera = go.AddComponent<Camera>();
            // Pin every camera setting that would otherwise push the camera through an intermediate, so a
            // "direct" camera is direct because the test says so, not because of defaults.
            camera.allowMSAA = true;
            camera.allowHDR = false;
            camera.allowDynamicResolution = false;
            camera.targetTexture = null;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.SetRenderer(0);
            data.renderPostProcessing = false;
            m_ProbeCameras.Add(camera);
            if (intermediate)
                m_IntermediateCameras.Add(camera);
            return camera;
        }

        Camera CreateRenderTextureCamera()
        {
            var rt = new RenderTexture(128, 128, 16) { antiAliasing = k_AssetMsaaSampleCount };
            m_TestObjects.Add(rt);
            var go = new GameObject("MSAA Test RenderTexture Camera");
            m_TestObjects.Add(go);
            var camera = go.AddComponent<Camera>();
            camera.allowMSAA = true;
            camera.targetTexture = rt;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.SetRenderer(0);
            return camera;
        }

        UniversalRenderPipelineAsset CreateAsset(bool tileOnly = false)
        {
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.renderingMode = RenderingMode.Forward;
            rendererData.tileOnlyMode = tileOnly;

            var instance = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            instance.m_RendererDataList[0] = rendererData;
            // Pin every setting that feeds RequiresIntermediateColorTexture, rather than trusting fresh-asset
            // defaults: any of these flipping on would push a "direct" camera through an intermediate and
            // silently invalidate the test. With these off, the direct-vs-intermediate distinction is
            // controlled solely by the force-intermediate pass.
            instance.msaaSampleCount = k_AssetMsaaSampleCount;
            instance.supportsHDR = false;
            instance.supportsCameraOpaqueTexture = false;
            instance.supportsCameraDepthTexture = false;
            instance.renderScale = 1.0f;
            return instance;
        }

        // A no-op pass that only declares it needs an intermediate texture, forcing URP to render the
        // camera through an intermediate attachment (decoupled from any specific URP feature).
        class ForceIntermediatePass : ScriptableRenderPass
        {
            public ForceIntermediatePass()
            {
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
            }
        }

        // Records, per camera, whether the active render target after opaques is the back buffer (direct)
        // or an intermediate. Read back by the test to verify each camera rendered as intended.
        class ActiveTargetProbePass : ScriptableRenderPass
        {
            public readonly Dictionary<Camera, bool> observedActiveTargetIsBackbuffer = new();
            public readonly Dictionary<Camera, int> observedCurrentBackbufferMsaa = new();
            public readonly Dictionary<Camera, int> observedRequestedMsaa = new();

            public ActiveTargetProbePass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();
                observedActiveTargetIsBackbuffer[cameraData.camera] = resourceData.isActiveTargetBackBuffer;
                // Capture applied and requested counts at the same instant the camera renders, so a failure
                // shows whether they disagree mid-render rather than just by teardown.
                observedCurrentBackbufferMsaa[cameraData.camera] = Screen.currentBackbufferMSAASamples;
                observedRequestedMsaa[cameraData.camera] = Screen.msaaSamples;
            }
        }
    }
}
