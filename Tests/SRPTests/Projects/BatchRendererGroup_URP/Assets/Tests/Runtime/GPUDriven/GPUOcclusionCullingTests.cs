using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

// Functional tests for the GPU occlusion culling instance test,
// which tests each instance's oriented bounding box (local bounds x objectToWorld) against the occluder depth pyramid,
// with a Rendering Debugger toggle to fall back to the legacy bounding sphere test.
//
// The occlusion verdicts are read from the "Occlusion Culling Events" debug stats that the CullInstances kernel accumulates on the GPU.
// Scenes are built so that verdicts cannot depend on game view resolution:
// occludees either sit behind an occluder that covers the frustum many times over (every Hi-Z gather window samples the occluder),
// or in front of it (a conservative test can never cull them).
// The relative culling efficiency of OBB vs sphere bounds is resolution-dependent by nature (gather windows snap to the texel grid of the depth pyramid),
// so it is measured manually rather than asserted here.
//
// The fixture swaps in an authored pipeline asset from this project (Assets/Resources/URPAsset_GPUOcclusionCulling) that
// enables the GPU Resident Drawer and GPU occlusion culling,
// so no pipeline objects are created in code. Platforms or graphics APIs where the GPU Resident Drawer cannot run degrade to Assert.Ignore.
[TestFixture]
[UnityPlatform(include = new[]
{
    // Editor-only: the tests read GPU occlusion culling debug stats, and enabling them (displayBatcherStats) makes URP render with the DEBUG_DISPLAY keyword,
    // whose shader variants are stripped from player builds (ShaderStrippingSetting.stripRuntimeDebugShaders).
    RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor
})]
class GPUOcclusionCullingTests
{
    const string k_PipelineAssetName = "URPAsset_GPUOcclusionCulling";
    const int k_OccludeeCount = 8;
    const int k_MaxWarmupFrames = 120;
    const int k_MeasureFrames = 30;
    const int k_PrimeFrames = 8;

    RenderPipelineAsset m_PreviousQualityRenderPipelineAsset;
    RenderPipelineAsset m_Asset;

    Camera m_Camera;
    GameObject m_CameraObject;
    GameObject m_Root;
    Material m_Material;

    class OcclusionResult
    {
        public int eventCount;
        public int maxCulledInstances;
        public int maxVisibleInstances;
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        m_Asset = Resources.Load<RenderPipelineAsset>(k_PipelineAssetName);
        Assert.IsNotNull(m_Asset,
            $"Pipeline asset '{k_PipelineAssetName}' not found in this project's Resources.");

        m_PreviousQualityRenderPipelineAsset = QualitySettings.renderPipeline;
        QualitySettings.renderPipeline = m_Asset;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (m_Asset == null)
            return;

        QualitySettings.renderPipeline = m_PreviousQualityRenderPipelineAsset;
        m_Asset = null;
    }

    [SetUp]
    public void SetUp()
    {
        m_CameraObject = new GameObject("GPUOcclusionCullingTests Camera");
        m_Camera = m_CameraObject.AddComponent<Camera>();
        // Render exclusively through the explicit SubmitCameraRender below.
        // An enabled camera is also rendered by the player loop (game view) at a different resolution,
        // and the first-phase occlusion test of each render then runs against the other render's depth pyramid;
        // the second phase re-tests and corrects the verdicts,
        // but the transient first-phase culls would poison this fixture's max-based measurements.
        m_Camera.enabled = false;
        m_Camera.clearFlags = CameraClearFlags.SolidColor;
        m_Camera.transform.position = Vector3.zero;
        m_Camera.transform.rotation = Quaternion.identity;
        m_Camera.farClipPlane = 100f;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.IsNotNull(shader, "URP Lit shader not found - test must run in a URP project.");
        m_Material = new Material(shader);
    }

    [TearDown]
    public void TearDown()
    {
        var debugSettings = GPUResidentDrawer.debugDisplaySettings;
        if (debugSettings != null)
        {
            debugSettings.displayBatcherStats = false;
            debugSettings.useBoundingSphereOcclusionTest = false;
        }

        // Destroy immediately: deferred destruction keeps the previous test's occluder wall alive into the next test's first rendered frame,
        // contaminating its occlusion verdicts (the measurement takes the max over the whole window).
        if (m_Root != null) UnityEngine.Object.DestroyImmediate(m_Root);
        if (m_CameraObject != null) UnityEngine.Object.DestroyImmediate(m_CameraObject);
        if (m_Material != null) UnityEngine.Object.DestroyImmediate(m_Material);
    }

    // The occludees sit at z = 30 within +/-3 of the view axis. The occluder wall covers the 60 degree frustum many times over at either depth,
    // so which side of it the occludees are on fully determines the expected verdict regardless of resolution.
    void BuildScene(float occluderZ)
    {
        m_Root = new GameObject("GPUOcclusionCullingTests Scene");

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(m_Root.transform, false);
        wall.transform.localPosition = new Vector3(0f, 0f, occluderZ);
        wall.transform.localScale = new Vector3(400f, 400f, 1f);
        wall.GetComponent<MeshRenderer>().sharedMaterial = m_Material;

        for (var i = 0; i < k_OccludeeCount; ++i)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(m_Root.transform, false);
            box.transform.localPosition = new Vector3(i % 4 - 1.5f, i / 4 - 0.5f, 30f);
            if ((i & 1) != 0)
            {
                // Elongated, rotated, mirrored boxes exercise the OBB corner projection beyond what axis-aligned unit cubes cover.
                box.transform.localScale = new Vector3(-4f, 0.2f, 0.3f);
                box.transform.localRotation = Quaternion.Euler(30f, 45f, 15f);
            }
            box.GetComponent<MeshRenderer>().sharedMaterial = m_Material;
        }
    }

    // Cameras are not rendered automatically in batch mode (no game view), so drive the render explicitly;
    // this also constructs the pipeline from the asset assigned above.
    void SubmitCameraRender()
    {
        var request = new RenderPipeline.StandardRequest();
        var desc = new RenderTextureDescriptor(512, 512, RenderTextureFormat.Default, 32);
        request.destination = RenderTexture.GetTemporary(desc);
        if (RenderPipeline.SupportsRenderRequest(m_Camera, request))
            RenderPipeline.SubmitRenderRequest(m_Camera, request);
        RenderTexture.ReleaseTemporary(request.destination);
    }

    // The occlusion debug counter GPU buffer is created uninitialized and only starts being cleared once a debug stats update has run,
    // so the first stats snapshot after the drawer comes up can report garbage counts (whatever data the recycled GPU memory held).
    // Enable the stats and render a few frames while the scene is still empty:
    // the drawer updates clear the counter buffer while nothing dispatches occlusion tests,
    // so every event measured afterwards accumulates from zeroed counters. Call before BuildScene.
    IEnumerator PrimeDebugStats()
    {
        for (var i = 0; i < k_PrimeFrames; ++i)
        {
            SubmitCameraRender();
            yield return null;

            var debugSettings = GPUResidentDrawer.debugDisplaySettings;
            if (debugSettings != null)
                debugSettings.displayBatcherStats = true;
        }
    }

    // Renders frames while accumulating the camera's occlusion test verdicts.
    // The counters go through an async GPU readback, so verdicts show up a few frames after the test dispatch;
    // sampling every frame over the window absorbs that latency.
    // Stops early once stopWhenCulledReaches is hit,
    // but always runs the full window when it is 0 so that "nothing was ever culled" covers steady state rather than the first event.
    IEnumerator MeasureOcclusion(OcclusionResult result, int stopWhenCulledReaches)
    {
        var frameBudget = k_MaxWarmupFrames + k_MeasureFrames;
        var framesAfterFirstEvent = 0;

        for (var i = 0; i < frameBudget; ++i)
        {
            SubmitCameraRender();
            yield return null;

            // The stats gate and the settings object itself only exist once the drawer has processed a frame,
            // so keep (re)applying instead of setting it once up front.
            var debugSettings = GPUResidentDrawer.debugDisplaySettings;
            if (debugSettings != null)
                debugSettings.displayBatcherStats = true;

            var stats = GPUResidentDrawer.GetDebugStats();
            if (stats == null)
                continue;

            var sawEventThisFrame = false;
            for (var eventIndex = 0; eventIndex < stats.instanceOcclusionEventStats.Length; ++eventIndex)
            {
                var eventStats = stats.instanceOcclusionEventStats[eventIndex];
                if (eventStats.viewID != m_Camera.GetEntityId() ||
                    eventStats.eventType != InstanceOcclusionEventType.OcclusionTest)
                    continue;

                sawEventThisFrame = true;
                result.eventCount++;
                result.maxCulledInstances = Math.Max(result.maxCulledInstances, eventStats.culledInstances);
                result.maxVisibleInstances = Math.Max(result.maxVisibleInstances, eventStats.visibleInstances);
            }

            if (stopWhenCulledReaches > 0 && result.maxCulledInstances >= stopWhenCulledReaches)
                yield break;

            if (sawEventThisFrame && ++framesAfterFirstEvent >= k_MeasureFrames)
                yield break;
        }
    }

    void IgnoreIfOcclusionNeverRan(OcclusionResult result)
    {
        if (result.eventCount == 0)
            Assert.Ignore("GPU occlusion culling did not run on this platform/graphics API " +
                "(unsupported device, or the GPU Resident Drawer could not be enabled).");
    }

    [UnityTest]
    public IEnumerator ObbOcclusionTest_CullsInstancesBehindOccluder()
    {
        yield return PrimeDebugStats();
        BuildScene(occluderZ: 10f);

        var result = new OcclusionResult();
        yield return MeasureOcclusion(result, stopWhenCulledReaches: k_OccludeeCount);
        IgnoreIfOcclusionNeverRan(result);

        Assert.GreaterOrEqual(result.maxCulledInstances, k_OccludeeCount,
            "Every occludee is fully behind a frustum-covering occluder, so the OBB occlusion test must cull all of them.");
        Assert.GreaterOrEqual(result.maxVisibleInstances, 1,
            "The occluder itself has nothing in front of it and must stay visible.");
    }

    [UnityTest]
    public IEnumerator ObbOcclusionTest_DoesNotCullVisibleInstances()
    {
        yield return PrimeDebugStats();

        // Wall behind the occludees: everything is visible, so a conservative occlusion test must never cull anything, including the rotated/mirrored thin boxes.
        BuildScene(occluderZ: 60f);

        var result = new OcclusionResult();
        yield return MeasureOcclusion(result, stopWhenCulledReaches: 0);
        IgnoreIfOcclusionNeverRan(result);

        Assert.AreEqual(0, result.maxCulledInstances,
            "Nothing in the scene is occluded; a culled instance means the OBB occlusion test is not conservative.");
    }

    [UnityTest]
    public IEnumerator SphereOcclusionTestDebugToggle_StillCullsInstancesBehindOccluder()
    {
        yield return PrimeDebugStats();
        BuildScene(occluderZ: 10f);

        // Wait for the drawer to come up so the toggle reaches PrepareCulling before measuring.
        for (var i = 0; i < k_MaxWarmupFrames && GPUResidentDrawer.debugDisplaySettings == null; ++i)
        {
            SubmitCameraRender();
            yield return null;
        }

        var debugSettings = GPUResidentDrawer.debugDisplaySettings;
        if (debugSettings == null)
            Assert.Ignore("The GPU Resident Drawer could not be enabled on this platform/graphics API.");
        debugSettings.useBoundingSphereOcclusionTest = true;

        var result = new OcclusionResult();
        yield return MeasureOcclusion(result, stopWhenCulledReaches: k_OccludeeCount);
        IgnoreIfOcclusionNeverRan(result);

        Assert.GreaterOrEqual(result.maxCulledInstances, k_OccludeeCount,
            "The legacy bounding sphere fallback selected through the debug toggle must still cull fully occluded instances.");
    }
}
