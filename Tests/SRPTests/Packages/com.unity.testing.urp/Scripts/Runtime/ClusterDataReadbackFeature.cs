using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

/// <summary>
/// Reads the cluster data that shaders see back to the CPU and compares it with the clustering jobs' result.
/// </summary>
public class ClusterDataReadbackFeature : ScriptableRendererFeature
{
    const int k_ReadbackWidth = 256;
    const uint k_UnwrittenIndex = 0xFFFFFFFFu;
    const uint k_ConstantFlag = 0x80000000u;

    // Order matches LoadConstant in the readback shader.
    static readonly string[] k_ConstantNames =
    {
        "URP_FP_ZBIN_SCALE", "URP_FP_ZBIN_OFFSET", "URP_FP_PROBES_BEGIN", "URP_FP_DIRECTIONAL_LIGHTS_COUNT",
        "URP_FP_TILE_SCALE.x", "URP_FP_TILE_SCALE.y", "URP_FP_TILE_COUNT_X", "URP_FP_TILE_COUNT_Y",
        "URP_FP_WORDS_PER_TILE", "URP_FP_ZBIN_COUNT", "URP_FP_TILE_COUNT",
    };

    public Shader shader;
    public RenderPassEvent readbackEvent = RenderPassEvent.BeforeRenderingOpaques;

    public static int checkCount { get; private set; }
    public static readonly List<string> failures = new();
    public static readonly Dictionary<string, int> expectedHashPerCamera = new();
    public static readonly Dictionary<string, int> expectedWordCountPerCamera = new();

    Material m_Material;
    ReadbackPass m_Pass;
    readonly Dictionary<Camera, RTHandle> m_Targets = new();

    /// <summary>
    /// Index of the named renderer in the active pipeline asset, or -1.
    /// </summary>
    public static int FindRendererIndex(string rendererName)
    {
        var renderers = UniversalRenderPipeline.asset.rendererDataList;
        for (var i = 0; i < renderers.Length; i++)
            if (renderers[i].name == rendererName)
                return i;
        return -1;
    }

    /// <summary>
    /// Whether the renderer at the index runs the cluster light loop on this platform (Deferred+ can fall back).
    /// </summary>
    public static bool RendererUsesClustering(int rendererIndex) =>
        UniversalRenderPipeline.asset.GetRenderer(rendererIndex) is UniversalRenderer renderer && renderer.usesClusterLightLoop;

    public static void ResetResults()
    {
        checkCount = 0;
        failures.Clear();
        expectedHashPerCamera.Clear();
        expectedWordCountPerCamera.Clear();
    }

    public override void Create()
    {
        m_Pass = new ReadbackPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (shader == null || renderingData.cameraData.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
            m_Material = CoreUtils.CreateEngineMaterial(shader);

        m_Pass.renderPassEvent = readbackEvent;
        renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing)
    {
        AsyncGPUReadback.WaitAllRequests();
        foreach (var target in m_Targets.Values)
            target.Release();
        m_Targets.Clear();
        CoreUtils.Destroy(m_Material);
    }

    RTHandle GetTarget(Camera camera)
    {
        if (!m_Targets.TryGetValue(camera, out var target))
        {
            // Two RGBA8 texels per word or constant: its index, then its value.
            var height = (2 * (UniversalRenderPipeline.maxZBinWords + UniversalRenderPipeline.maxTileWords + k_ConstantNames.Length) + k_ReadbackWidth - 1) / k_ReadbackWidth;
            target = RTHandles.Alloc(k_ReadbackWidth, height, GraphicsFormat.R8G8B8A8_UNorm, name: "Cluster Data Readback");
            m_Targets.Add(camera, target);
        }
        return target;
    }

    struct Expected
    {
        public string cameraName;
        public uint[] zBins;
        public uint[] tiles;
        public uint[] constants;
    }

    // Mirrors the arguments ForwardLights.PreSetup passes, so the result is what the clustering upload holds for this camera.
    static Expected ComputeExpected(UniversalCameraData cameraData, UniversalLightData lightData, UniversalRenderingData renderingData)
    {
        var zBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.TempJob);
        var tiles = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.TempJob);
        var viewCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
        var worldToViews = new Fixed2<float4x4>(cameraData.GetViewMatrix(0), cameraData.GetViewMatrix(viewCount - 1));
        var viewToClips = new Fixed2<float4x4>(cameraData.GetProjectionMatrix(0), cameraData.GetProjectionMatrix(viewCount - 1));
        var camera = cameraData.camera;

        ForwardLights.ScheduleClusteringJobs(
            lightData.mainLightIndex != -1,
            lightData.supportsAdditionalLights,
            lightData.visibleLights,
            renderingData.cullResults.visibleReflectionProbes,
            zBins,
            tiles,
            worldToViews,
            viewToClips,
            viewCount,
            math.int2(cameraData.pixelWidth, cameraData.pixelHeight),
            camera.nearClipPlane,
            camera.farClipPlane,
            camera.orthographic,
            out var localLightCount, out var directionalLightCount, out var binCount, out var zBinScale, out var zBinOffset,
            out var tileResolution, out var actualTileWidth, out var wordsPerTile).Complete();

        // Mirrors the _FPParams values ForwardLights sets for the cluster light loop.
        var tileScale = cameraData.pixelRect.size / actualTileWidth;

        var expected = new Expected
        {
            cameraName = camera.name,
            zBins = zBins.GetSubArray(0, binCount * (ZBinningJob.headerLength + wordsPerTile) * viewCount).ToArray(),
            tiles = tiles.GetSubArray(0, tileResolution.x * tileResolution.y * wordsPerTile * viewCount).ToArray(),
            constants = new[]
            {
                math.asuint(zBinScale), math.asuint(zBinOffset), (uint)localLightCount, (uint)directionalLightCount,
                math.asuint(tileScale.x), math.asuint(tileScale.y), (uint)tileResolution.x, (uint)tileResolution.y,
                (uint)wordsPerTile, (uint)binCount, (uint)(tileResolution.x * tileResolution.y),
            },
        };
        zBins.Dispose();
        tiles.Dispose();
        return expected;
    }

    static void Check(AsyncGPUReadbackRequest request, Expected expected)
    {
        checkCount++;
        if (request.hasError)
        {
            failures.Add($"{expected.cameraName}: GPU readback failed.");
            return;
        }

        var hash = 17;
        var hasTileBits = false;
        foreach (var word in expected.constants)
            hash = hash * 31 + (int)word;
        foreach (var word in expected.zBins)
            hash = hash * 31 + (int)word;
        foreach (var word in expected.tiles)
        {
            hash = hash * 31 + (int)word;
            hasTileBits |= word != 0;
        }
        expectedHashPerCamera[expected.cameraName] = hash;
        expectedWordCountPerCamera[expected.cameraName] = expected.zBins.Length + expected.tiles.Length;

        if (!hasTileBits)
        {
            failures.Add($"{expected.cameraName}: no light covers any tile, so the comparison is vacuous.");
            return;
        }

        var wordCount = expected.zBins.Length + expected.tiles.Length;
        var written = 0;
        var constantsWritten = 0;
        foreach (var texel in request.GetData<uint2>())
        {
            if (texel.x == k_UnwrittenIndex)
                continue;

            if ((texel.x & k_ConstantFlag) != 0)
            {
                var id = (int)(texel.x & ~k_ConstantFlag);
                if (id >= expected.constants.Length)
                {
                    failures.Add($"{expected.cameraName}: readback wrote out-of-range constant id {id}.");
                    return;
                }
                if (texel.y != expected.constants[id])
                {
                    failures.Add($"{expected.cameraName}: {k_ConstantNames[id]} is 0x{texel.y:X8} in shaders, expected 0x{expected.constants[id]:X8}.");
                    return;
                }
                constantsWritten++;
                continue;
            }

            var index = (int)texel.x;
            if (index >= wordCount)
            {
                failures.Add($"{expected.cameraName}: readback wrote out-of-range word index {index}.");
                return;
            }

            var expectedWord = index < expected.zBins.Length ? expected.zBins[index] : expected.tiles[index - expected.zBins.Length];
            if (texel.y != expectedWord)
            {
                var location = index < expected.zBins.Length ? $"z-bin word {index}" : $"tile word {index - expected.zBins.Length}";
                failures.Add($"{expected.cameraName}: {location} is 0x{texel.y:X8} in shaders, expected 0x{expectedWord:X8}.");
                return;
            }
            written++;
        }

        if (written != wordCount)
            failures.Add($"{expected.cameraName}: readback covered {written} of {wordCount} words.");
        if (constantsWritten != expected.constants.Length)
            failures.Add($"{expected.cameraName}: readback covered {constantsWritten} of {expected.constants.Length} constants.");
    }

    class ReadbackPass : ScriptableRenderPass
    {
        static readonly int s_ZBinWordCount = Shader.PropertyToID("_ZBinWordCount");
        static readonly int s_TileWordCount = Shader.PropertyToID("_TileWordCount");
        static readonly int s_ReadbackWidth = Shader.PropertyToID("_ReadbackWidth");

        readonly ClusterDataReadbackFeature m_Feature;

        public ReadbackPass(ClusterDataReadbackFeature feature)
        {
            m_Feature = feature;
            profilingSampler = new ProfilingSampler("Cluster Data Readback");
        }

        class DrawPassData
        {
            public Material material;
            public MaterialPropertyBlock properties = new();
            public int zBinWordCount;
            public int tileWordCount;
        }

        class RequestPassData
        {
            public RTHandle target;
            public Expected expected;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var expected = ComputeExpected(cameraData, frameData.Get<UniversalLightData>(), frameData.Get<UniversalRenderingData>());
            var target = m_Feature.GetTarget(cameraData.camera);
            var targetHandle = renderGraph.ImportTexture(target);

            using (var builder = renderGraph.AddRasterRenderPass<DrawPassData>("Write Cluster Data", out var passData, profilingSampler))
            {
                passData.material = m_Feature.m_Material;
                passData.zBinWordCount = expected.zBins.Length;
                passData.tileWordCount = expected.tiles.Length;
                builder.SetRenderAttachment(targetHandle, 0, AccessFlags.WriteAll);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (DrawPassData data, RasterGraphContext context) =>
                {
                    data.properties.SetInteger(s_ZBinWordCount, data.zBinWordCount);
                    data.properties.SetInteger(s_TileWordCount, data.tileWordCount);
                    data.properties.SetInteger(s_ReadbackWidth, k_ReadbackWidth);
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, data.properties);
                });
            }

            using (var builder = renderGraph.AddUnsafePass<RequestPassData>("Read Back Cluster Data", out var passData))
            {
                passData.target = target;
                passData.expected = expected;
                builder.UseTexture(targetHandle);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (RequestPassData data, UnsafeGraphContext context) =>
                {
                    var expected = data.expected;
                    CommandBufferHelpers.GetNativeCommandBuffer(context.cmd).RequestAsyncReadback(data.target.rt, request => Check(request, expected));
                });
            }
        }
    }
}
