using System;
using System.Collections;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

// Regression test for per-bucket draw-call stats attribution.
//
// DrawBuffersBatchMode splits work into the hybrid (BRG/GRD) and static-batch (SRP Batcher) paths, but
// both routed through a shared draw helper that always reported kStandard*. The "Render" counters behind
// the Game View Statistics "BRG/GRD" and "SRP Batcher" lines therefore stayed at 0. The fix picks the
// bucket per batch (hybrid -> kBRG/kBRGIndirect, static -> kSrpBatcher), as Vulkan/D3D11/D3D12/Switch do.
//
// Enabled on Metal only, where the fix landed: other backends share the pattern but a blanket "> 0"
// assert won't hold (GLES and consoles never emit kSrpBatcher). Apple platforms are Metal-only, so
// [UnityPlatform] include = OSX restricts this to the fixed backend.
//
// Complements RenderStatsTests, which only checks these counters are valid (it can't drive kSrpBatcher
// or URP-only kBRG); this renders real geometry and asserts the right bucket is non-zero.
[TestFixture]
[UnityPlatform(include = new[]
{
    RuntimePlatform.OSXEditor,
    RuntimePlatform.OSXPlayer,
})]
public class DrawCallStatsAttributionTests
{
    const int k_CubeGrid = 8;            // 8x8 = 64 cubes, enough to form a batch
    const int k_WarmupFrames = 120;      // GRD processes renderers over several frames; be generous

    GameObject m_Camera;
    GameObject m_Root;
    Material m_Material;
    GPUResidentDrawerMode m_PreviousGrdMode;
    bool m_PreviousSrpBatching;
    bool m_HasPipeline;

    IGPUResidentRenderPipeline Pipeline => GraphicsSettings.currentRenderPipeline as IGPUResidentRenderPipeline;

    [SetUp]
    public void SetUp()
    {
        m_HasPipeline = Pipeline != null;
        if (m_HasPipeline)
            m_PreviousGrdMode = Pipeline.gpuResidentDrawerMode;
        m_PreviousSrpBatching = GraphicsSettings.useScriptableRenderPipelineBatching;

        m_Camera = new GameObject("SRP-1287 Camera");
        var cam = m_Camera.AddComponent<Camera>();
        cam.enabled = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -15f);
        cam.transform.rotation = Quaternion.identity;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.IsNotNull(shader, "URP Lit shader not found - test must run in a URP project.");
        m_Material = new Material(shader);

        m_Root = new GameObject("SRP-1287 Cubes");
        for (var x = 0; x < k_CubeGrid; ++x)
        for (var y = 0; y < k_CubeGrid; ++y)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(m_Root.transform, false);
            cube.transform.localPosition = new Vector3(x - k_CubeGrid * 0.5f, y - k_CubeGrid * 0.5f, 0f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = m_Material;
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (m_HasPipeline)
            Pipeline.gpuResidentDrawerMode = m_PreviousGrdMode;
        GraphicsSettings.useScriptableRenderPipelineBatching = m_PreviousSrpBatching;

        if (m_Root != null) UnityEngine.Object.Destroy(m_Root);
        if (m_Camera != null) UnityEngine.Object.Destroy(m_Camera);
        if (m_Material != null) UnityEngine.Object.Destroy(m_Material);
    }

    [UnityTest]
    public IEnumerator BrgDrawCalls_AreAttributedToBrgBucket_WhenGpuResidentDrawerEnabled()
    {
        if (!m_HasPipeline)
            Assert.Ignore("Active render pipeline does not support the GPU Resident Drawer.");

        // GPU Resident Drawer routes eligible MeshRenderers through BatchRendererGroup -> kBRG.
        Pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;

        using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "BRG Draw Calls Count", k_WarmupFrames);
        Assert.IsTrue(recorder.Valid, "Profiler counter 'BRG Draw Calls Count' is not available.");

        for (var i = 0; i < k_WarmupFrames; ++i)
            yield return null;

        long brgDrawCalls = MaxSample(recorder);

        Assert.Greater(brgDrawCalls, 0,
            "BRG/GRD draws must be counted in the BRG bucket (kBRG). Reads 0 when DrawBuffersBatchMode " +
            "misattributes the hybrid path to kStandard*.");
    }

    [UnityTest]
    public IEnumerator SrpBatcherDrawCalls_AreAttributedToSrpBatcherBucket_WhenSrpBatchingEnabled()
    {
        // Force the static-batch / SRP Batcher path: GRD off + SRP Batcher on.
        if (m_HasPipeline)
            Pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;

        using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SRP Batcher Draw Calls Count", k_WarmupFrames);
        Assert.IsTrue(recorder.Valid, "Profiler counter 'SRP Batcher Draw Calls Count' is not available.");

        for (var i = 0; i < k_WarmupFrames; ++i)
            yield return null;

        long srpBatcherDrawCalls = MaxSample(recorder);

        Assert.Greater(srpBatcherDrawCalls, 0,
            "SRP-batched draws must be counted in the SRP Batcher bucket (kSrpBatcher). Reads 0 when " +
            "DrawBuffersBatchMode misattributes the static-batch path to kStandard*.");
    }

    // Largest per-frame sample over the recorder's window. Robust against frames that skip rendering.
    static long MaxSample(ProfilerRecorder recorder)
    {
        long max = 0;
        var count = recorder.Count;
        for (var i = 0; i < count; ++i)
            max = Math.Max(max, recorder.GetSample(i).Value);
        return max;
    }
}
