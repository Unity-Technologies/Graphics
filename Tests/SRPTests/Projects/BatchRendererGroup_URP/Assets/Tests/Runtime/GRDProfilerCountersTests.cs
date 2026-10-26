using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

// Functional tests for the "GPU Resident Drawer" Profiler counters, driven over real geometry.
//
// Two groups share the same harness (GRD pipeline toggle + cube grid + ProfilerRecorder):
//   - Batch stats   ("Batch Count" / "Unique Materials" / ...) feed the Game View Statistics
//     window. GPUResidentDrawer.CullerUpdateFrame emits them gate-free (outside IsCategoryEnabled).
//   - Coverage      ("GRD Renderers" / "Excluded Renderers") feed the Rendering Profiler module.
//     GPUResidentDrawer.PostPostLateUpdate emits them via EmitAll, gated on the Profiler category.
//
// In-editor PlayMode tests: PlayMode lets the test step frames deterministically with
// `yield return null`. Backend-agnostic (CPU-side counts), so they run on every desktop Editor
// rather than a single graphics API; pipelines without GRD support are skipped via Assert.Ignore.
[TestFixture]
[UnityPlatform(include = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
public class GRDProfilerCountersTests
{
    // Wire format - MUST match GRDProfilerCounters (package) and StatsField.cs (engine core).
    const string k_Category = "GPU Resident Drawer";

    // Batch stats (Game View Statistics window).
    const string k_BatchCount = "Batch Count";
    const string k_UniqueMaterials = "Unique Materials";
    const string k_UniqueMeshes = "Unique Meshes";
    const string k_SingleInstanceBatches = "Single-Instance Batches";
    const string k_Active = "GRD Active";

    // Coverage (Rendering Profiler module).
    const string k_GRDRenderers = "GRD Renderers";
    const string k_ExcludedRenderers = "Excluded Renderers";

    const int k_CubeGrid = 8;            // 8x8 = 64 cubes, enough to form batches / register renderers
    const int k_WarmupFrames = 120;      // GRD processes renderers over several frames; be generous

    GameObject m_Camera;
    GameObject m_Root;
    readonly List<Material> m_Materials = new();
    GPUResidentDrawerMode m_PreviousGrdMode;
    bool m_HasPipeline;

    IGPUResidentRenderPipeline Pipeline => GraphicsSettings.currentRenderPipeline as IGPUResidentRenderPipeline;

    [SetUp]
    public void SetUp()
    {
        m_HasPipeline = Pipeline != null;
        if (m_HasPipeline)
            m_PreviousGrdMode = Pipeline.gpuResidentDrawerMode;

        m_Camera = new GameObject("GRD-Counters Camera");
        var cam = m_Camera.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -15f);
        cam.transform.rotation = Quaternion.identity;
    }

    [TearDown]
    public void TearDown()
    {
        if (m_HasPipeline)
            Pipeline.gpuResidentDrawerMode = m_PreviousGrdMode;

        if (m_Root != null) UnityEngine.Object.Destroy(m_Root);
        if (m_Camera != null) UnityEngine.Object.Destroy(m_Camera);
        foreach (var m in m_Materials)
            if (m != null) UnityEngine.Object.Destroy(m);
        m_Materials.Clear();
    }

    Material NewLitMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.IsNotNull(shader, "URP Lit shader not found - test must run in a URP project.");
        var mat = new Material(shader);
        m_Materials.Add(mat);
        return mat;
    }

    // sharedMaterial != null: every cube shares it (instanced -> few unique mat/mesh, multi-instance
    // batches). sharedMaterial == null: each cube gets its own material (-> single-instance batches).
    void BuildCubes(Material sharedMaterial)
    {
        m_Root = new GameObject("GRD-Counters Cubes");
        for (var x = 0; x < k_CubeGrid; ++x)
        for (var y = 0; y < k_CubeGrid; ++y)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(m_Root.transform, false);
            cube.transform.localPosition = new Vector3(x - k_CubeGrid * 0.5f, y - k_CubeGrid * 0.5f, 0f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = sharedMaterial != null ? sharedMaterial : NewLitMaterial();
        }
    }

    // ===================== Batch stats (Game View Statistics window) =====================

    [UnityTest]
    public IEnumerator BatchStats_AreEmitted_WhenGpuResidentDrawerEnabled()
    {
        if (!m_HasPipeline)
            Assert.Ignore("Active render pipeline does not support the GPU Resident Drawer.");

        Pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
        BuildCubes(NewLitMaterial()); // all cubes share one material + the primitive cube mesh

        var category = new ProfilerCategory(k_Category);
        using var batchCount = ProfilerRecorder.StartNew(category, k_BatchCount, k_WarmupFrames);
        using var uniqueMaterials = ProfilerRecorder.StartNew(category, k_UniqueMaterials, k_WarmupFrames);
        using var uniqueMeshes = ProfilerRecorder.StartNew(category, k_UniqueMeshes, k_WarmupFrames);

        Assert.IsTrue(batchCount.Valid && uniqueMaterials.Valid && uniqueMeshes.Valid,
            "GRD batch stat counters are not available - GPUResidentDrawer.CullerUpdateFrame must emit them.");

        for (var i = 0; i < k_WarmupFrames; ++i)
            yield return null;

        Assert.Greater(MaxSample(batchCount), 0,
            "GRD must produce at least one draw batch; the Statistics window reads 0 if the emit never runs.");
        Assert.Greater(MaxSample(uniqueMaterials), 0, "Unique Materials must be non-zero with GRD active.");
        Assert.Greater(MaxSample(uniqueMeshes), 0, "Unique Meshes must be non-zero with GRD active.");
    }

    [UnityTest]
    public IEnumerator SingleInstanceBatches_AreCounted_WhenEachRendererIsUnique()
    {
        if (!m_HasPipeline)
            Assert.Ignore("Active render pipeline does not support the GPU Resident Drawer.");

        Pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
        BuildCubes(null); // distinct material per cube -> each is its own single-instance batch

        var category = new ProfilerCategory(k_Category);
        using var singleInstance = ProfilerRecorder.StartNew(category, k_SingleInstanceBatches, k_WarmupFrames);
        Assert.IsTrue(singleInstance.Valid, "'Single-Instance Batches' counter is not available.");

        for (var i = 0; i < k_WarmupFrames; ++i)
            yield return null;

        Assert.Greater(MaxSample(singleInstance), 0,
            "Renderers that cannot be instanced together must be counted as single-instance batches.");
    }

    // Single-instance is an authoring property, so it must hold under multi-camera rendering: a batch
    // with one instance drawn by two cameras is still a single-instance batch, not a two-instance one.
    [UnityTest]
    public IEnumerator SingleInstanceBatches_CountedCorrectly_WithMultipleCameras()
    {
        if (!m_HasPipeline)
            Assert.Ignore("Active render pipeline does not support the GPU Resident Drawer.");

        Pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
        BuildCubes(null); // distinct material per cube -> each is its own single-instance batch

        // Second camera with an identical frustum, so every cube is drawn by both (visibleCount == 2).
        var secondCamera = new GameObject("GRD-Counters Camera 2");
        var cam2 = secondCamera.AddComponent<Camera>();
        cam2.clearFlags = CameraClearFlags.SolidColor;
        cam2.transform.position = new Vector3(0f, 0f, -15f);
        cam2.transform.rotation = Quaternion.identity;
        cam2.depth = 1;
        try
        {
            var category = new ProfilerCategory(k_Category);
            using var singleInstance = ProfilerRecorder.StartNew(category, k_SingleInstanceBatches, k_WarmupFrames);
            Assert.IsTrue(singleInstance.Valid, "'Single-Instance Batches' counter is not available.");

            for (var i = 0; i < k_WarmupFrames; ++i)
                yield return null;

            Assert.Greater(MaxSample(singleInstance), 0,
                "Single-instance batches drawn by more than one camera must still be counted; the metric "
                + "reflects authored batch size, not the summed per-camera visible count.");
        }
        finally
        {
            UnityEngine.Object.Destroy(secondCamera);
        }
    }

    // The "GRD Active" liveness flag gates GRD UI visibility: the Game View Statistics window's batch
    // rows and the Rendering Profiler's GRD card. It must (1) read non-zero while GRD runs, (2) STAY
    // non-zero when the camera looks away — so the rows stay visible at 0 instead of flickering, the
    // reason it's a separate flag and not just "batch count > 0" — and (3) drop to 0 once GRD is
    // disabled, so the UI hides. Gate-free (no Profiler.enabled dance needed) and cumulative so it
    // holds between the throttled emits, emitted by GRDProfilerCounters.EmitActive on the
    // CullerUpdateFrame path and zeroed by ResetCounters on teardown.
    [UnityTest]
    public IEnumerator ActiveFlag_TracksGpuResidentDrawerRunning_AndHidesWhenDisabled()
    {
        if (!m_HasPipeline)
            Assert.Ignore("Active render pipeline does not support the GPU Resident Drawer.");

        Pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
        BuildCubes(NewLitMaterial());

        var category = new ProfilerCategory(k_Category);

        // 1) GRD running with cubes in view -> active.
        using (var active = ProfilerRecorder.StartNew(category, k_Active, k_WarmupFrames))
        {
            Assert.IsTrue(active.Valid,
                "'GRD Active' counter is not available - GPUResidentDrawer.CullerUpdateFrame must emit it.");

            for (var i = 0; i < k_WarmupFrames; ++i)
                yield return null;

            Assert.Greater(MaxSample(active), 0, "GRD is running, so the active flag must be set.");
        }

        // 2) Camera looks away: nothing is drawn (batch count -> 0), but GRD is STILL running, so the
        // flag must stay set. This is the case a value-based "hide when 0" would get wrong.
        m_Camera.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        for (var i = 0; i < 10; ++i)
            yield return null;

        using (var lookedAway = ProfilerRecorder.StartNew(category, k_Active, k_WarmupFrames))
        {
            for (var i = 0; i < k_WarmupFrames; ++i)
                yield return null;
            Assert.Greater(MaxSample(lookedAway), 0,
                "GRD is still running while the camera looks away; the rows must stay visible (flag set), only their values go to 0.");
        }

        // 3) Disable GRD: teardown calls ResetCounters, which zeroes the flag -> rows hide.
        Pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
        for (var i = 0; i < 10; ++i)
            yield return null;

        using (var disabled = ProfilerRecorder.StartNew(category, k_Active, k_WarmupFrames))
        {
            for (var i = 0; i < k_WarmupFrames; ++i)
                yield return null;
            Assert.AreEqual(0, MaxSample(disabled),
                "Once GRD is disabled the active flag must read 0 so the Statistics window hides the GRD batch rows.");
        }
    }

    // ===================== Coverage (Rendering Profiler module) =====================

    [UnityTest]
    public IEnumerator CoverageCounters_DropToZero_WhenGpuResidentDrawerDisabled()
    {
        if (!m_HasPipeline)
            Assert.Ignore("Active render pipeline does not support the GPU Resident Drawer.");

        // EmitAll is gated behind Profiler.enabled && IsCategoryEnabled (unlike the batch stats
        // counters above, which emit gate-free). A ProfilerRecorder alone does NOT enable the
        // category, so force both on - this mirrors a real Profiler capture, which enables all
        // categories. Restored in finally so the state doesn't leak into other tests.
        var category = new ProfilerCategory(k_Category);
        bool prevProfilerEnabled = UnityEngine.Profiling.Profiler.enabled;
        UnityEngine.Profiling.Profiler.enabled = true;
        UnityEngine.Profiling.Profiler.SetCategoryEnabled(category, true);
        try
        {
            Pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
            BuildCubes(NewLitMaterial());

            // 1) GRD enabled with cubes in view -> Coverage counts are non-zero.
            using (var grd = ProfilerRecorder.StartNew(category, k_GRDRenderers, k_WarmupFrames))
            {
                Assert.IsTrue(grd.Valid,
                    "'GRD Renderers' counter is not available - GRDProfilerCounters.EmitAll must emit it.");

                for (var i = 0; i < k_WarmupFrames; ++i)
                    yield return null;

                Assert.Greater(MaxSample(grd), 0,
                    "Sanity: GRD active over visible geometry must register GRD-path renderers.");
            }

            // 2) Turn GRD off. GPUResidentDrawer is disposed and EmitAll stops running.
            Pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;

            // Let the disable propagate through Dispose -> counter reset -> recorder flush before
            // measuring, so the transition frame (still carrying the last emitted count) isn't in the
            // measured window.
            for (var i = 0; i < 10; ++i)
                yield return null;

            // Start FRESH recorders so their windows cover only steady disabled frames. Cumulative
            // counters keep flushing their last value forever unless explicitly reset, so without the
            // Dispose-time reset MaxSample stays at the pre-disable count.
            using (var grd = ProfilerRecorder.StartNew(category, k_GRDRenderers, k_WarmupFrames))
            using (var excluded = ProfilerRecorder.StartNew(category, k_ExcludedRenderers, k_WarmupFrames))
            {
                for (var i = 0; i < k_WarmupFrames; ++i)
                    yield return null;

                Assert.AreEqual(0, MaxSample(grd),
                    "'GRD Renderers' must reset to 0 once GRD is disabled; a non-zero value is the stale Coverage counter ghosting in the Profiler.");
                Assert.AreEqual(0, MaxSample(excluded),
                    "'Excluded Renderers' must reset to 0 once GRD is disabled.");
            }
        }
        finally
        {
            UnityEngine.Profiling.Profiler.SetCategoryEnabled(category, false);
            UnityEngine.Profiling.Profiler.enabled = prevProfilerEnabled;
        }
    }

    // Largest per-frame sample over the recorder's window. Robust against frames that skip rendering or
    // run before GRD has finished processing the new renderers.
    static long MaxSample(ProfilerRecorder recorder)
    {
        long max = 0;
        var count = recorder.Count;
        for (var i = 0; i < count; ++i)
            max = Math.Max(max, recorder.GetSample(i).Value);
        return max;
    }
}
