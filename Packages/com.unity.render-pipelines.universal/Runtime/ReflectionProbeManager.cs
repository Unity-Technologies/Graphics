using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    struct ReflectionProbeManager : IDisposable
    {
        int2 m_Resolution;
        RenderTexture m_AtlasTexture0;
        RenderTexture m_AtlasTexture1;
        RTHandle m_AtlasTexture0Handle;
        BuddyAllocator m_AtlasAllocator;
        Dictionary<int, CachedProbe> m_Cache;
        Dictionary<int, int> m_WarningCache;
        List<int> m_NeedsUpdate;
        List<int> m_NeedsRemove;

        // Pre-allocated arrays for filling constant buffers
        Vector4[] m_BoxMax;
        Vector4[] m_BoxMin;
        Vector4[] m_ProbePosition;
        Vector4[] m_MipScaleOffset;

        // There is a global max of 7 mips in Unity.
        const int k_MaxMipCount = 7;
        const string k_ReflectionProbeAtlasName = "URP Reflection Probe Atlas";

        unsafe struct CachedProbe
        {
            public uint updateCount;
            public Hash128 imageContentsHash;
            public int size;
            public int mipCount;
            // One for each mip.
            public fixed int dataIndices[k_MaxMipCount];
            public fixed int levels[k_MaxMipCount];
            public Texture texture;
            public int lastUsed;
            public Vector4 hdrData;
        }

        static class ShaderProperties
        {
            public static readonly int BoxMin = Shader.PropertyToID("urp_ReflProbes_BoxMin");
            public static readonly int BoxMax = Shader.PropertyToID("urp_ReflProbes_BoxMax");
            public static readonly int ProbePosition = Shader.PropertyToID("urp_ReflProbes_ProbePosition");
            public static readonly int MipScaleOffset = Shader.PropertyToID("urp_ReflProbes_MipScaleOffset");
            public static readonly int Count = Shader.PropertyToID("urp_ReflProbes_Count");
            public static readonly int Atlas = Shader.PropertyToID("urp_ReflProbes_Atlas");
        }

        public RenderTexture atlasRT => m_AtlasTexture0;
        public RTHandle atlasRTHandle => m_AtlasTexture0Handle;

        public static ReflectionProbeManager Create()
        {
            var instance = new ReflectionProbeManager();
            instance.Init();
            return instance;
        }

        void Init()
        {
            var maxProbes = UniversalRenderPipeline.maxVisibleReflectionProbes;

            // m_Resolution = math.min((int)reflectionProbeResolution, SystemInfo.maxTextureSize);
            m_Resolution = 1;
            var format = GraphicsFormat.B10G11R11_UFloatPack32;
            if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) { format = GraphicsFormat.R16G16B16A16_SFloat; }
            m_AtlasTexture0 = new RenderTexture(new RenderTextureDescriptor
            {
                width = m_Resolution.x,
                height = m_Resolution.y,
                volumeDepth = 1,
                dimension = TextureDimension.Tex2D,
                graphicsFormat = format,
                useMipMap = false,
                msaaSamples = 1
            });
            m_AtlasTexture0.name = k_ReflectionProbeAtlasName;
            m_AtlasTexture0.filterMode = FilterMode.Bilinear;
            m_AtlasTexture0.hideFlags = HideFlags.HideAndDontSave;
            m_AtlasTexture0.Create();
            m_AtlasTexture0Handle = RTHandles.Alloc(m_AtlasTexture0, transferOwnership: true);

            m_AtlasTexture1 = new RenderTexture(m_AtlasTexture0.descriptor);
            m_AtlasTexture1.name = k_ReflectionProbeAtlasName;
            m_AtlasTexture1.filterMode = FilterMode.Bilinear;
            m_AtlasTexture1.hideFlags = HideFlags.HideAndDontSave;

            // The smallest allocatable resolution we want is 4x4. We calculate the number of levels as:
            // log2(max) - log2(4) = log2(max) - 2
            m_AtlasAllocator = new BuddyAllocator(math.floorlog2(SystemInfo.maxTextureSize) - 2, 2);
            m_Cache = new Dictionary<int, CachedProbe>(maxProbes);
            m_WarningCache = new Dictionary<int, int>(maxProbes);
            m_NeedsUpdate = new List<int>(maxProbes);
            m_NeedsRemove = new List<int>(maxProbes);

            m_BoxMax = new Vector4[maxProbes];
            m_BoxMin = new Vector4[maxProbes];
            m_ProbePosition = new Vector4[maxProbes];
            m_MipScaleOffset = new Vector4[maxProbes * 7];
        }

        public unsafe void UpdateGpuData(CommandBuffer cmd, ref CullingResults cullResults)
        {
            var probes = cullResults.visibleReflectionProbes;
            var probeCount = math.min(probes.Length, UniversalRenderPipeline.maxVisibleReflectionProbes);
            var frameIndex = Time.renderedFrameCount;

            // Populate list of probes we need to remove to avoid modifying dictionary while iterating.
            foreach (var (id, cachedProbe) in m_Cache)
            {
                // Evict probe if not used for more than 1 frame, if the texture no longer exists, or if the size changed.
                if (Math.Abs(cachedProbe.lastUsed - frameIndex) > 1 ||
                    !cachedProbe.texture ||
                    cachedProbe.size != cachedProbe.texture.width)
                {
                    m_NeedsRemove.Add(id);
                    for (var i = 0; i < k_MaxMipCount; i++)
                    {
                        if (cachedProbe.dataIndices[i] != -1) m_AtlasAllocator.Free(new BuddyAllocation(cachedProbe.levels[i], cachedProbe.dataIndices[i]));
                    }
                }
            }

            foreach (var probeIndex in m_NeedsRemove)
            {
                m_Cache.Remove(probeIndex);
            }

            m_NeedsRemove.Clear();

            foreach (var (id, lastUsed) in m_WarningCache)
            {
                if (Math.Abs(lastUsed - frameIndex) > 1)
                {
                    m_NeedsRemove.Add(id);
                }
            }

            foreach (var probeIndex in m_NeedsRemove)
            {
                m_WarningCache.Remove(probeIndex);
            }

            m_NeedsRemove.Clear();

            var showFullWarning = false;
            var requiredAtlasSize = math.int2(0, 0);

            for (var probeIndex = 0; probeIndex < probeCount; probeIndex++)
            {
                var probe = probes[probeIndex];

                var texture = probe.texture;
                var id = probe.reflectionProbe.GetInstanceID();
                var wasCached = m_Cache.TryGetValue(id, out var cachedProbe);

                if (!texture)
                {
                    continue;
                }

                if (!wasCached)
                {
                    cachedProbe.size = texture.width;
                    var mipCount = math.ceillog2(cachedProbe.size * 4) + 1;
                    var level = m_AtlasAllocator.levelCount + 2 - mipCount;
                    cachedProbe.mipCount = math.min(mipCount, k_MaxMipCount);
                    cachedProbe.texture = texture;

                    var mip = 0;
                    for (; mip < cachedProbe.mipCount; mip++)
                    {
                        // Clamp to maximum level. This is relevant for 64x64 and lower, which will have valid content
                        // in 1x1 mip. The octahedron size is double the face size, so that ends up at 2x2. Due to
                        // borders the final mip must be 4x4 as that leaves 2x2 texels for the octahedron.
                        var mipLevel = math.min(level + mip, m_AtlasAllocator.levelCount - 1);
                        if (!m_AtlasAllocator.TryAllocate(mipLevel, out var allocation)) break;
                        // We split up the allocation struct because C# cannot do struct fixed arrays :(
                        cachedProbe.levels[mip] = allocation.level;
                        cachedProbe.dataIndices[mip] = allocation.index;
                        var scaleOffset = (int4)(GetScaleOffset(mipLevel, allocation.index, true, false) * m_Resolution.xyxy);
                        requiredAtlasSize = math.max(requiredAtlasSize, scaleOffset.zw + scaleOffset.xy);
                    }

                    // Check if we ran out of space in the atlas.
                    if (mip < cachedProbe.mipCount)
                    {
                        if (!m_WarningCache.ContainsKey(id)) showFullWarning = true;
                        m_WarningCache[id] = frameIndex;
                        for (var i = 0; i < mip; i++) m_AtlasAllocator.Free(new BuddyAllocation(cachedProbe.levels[i], cachedProbe.dataIndices[i]));
                        for (var i = 0; i < k_MaxMipCount; i++) cachedProbe.dataIndices[i] = -1;
                        continue;
                    }

                    for (; mip < k_MaxMipCount; mip++)
                    {
                        cachedProbe.dataIndices[mip] = -1;
                    }
                }

                var needsUpdate = !wasCached || cachedProbe.updateCount != texture.updateCount;
#if UNITY_EDITOR
                needsUpdate |= cachedProbe.imageContentsHash != texture.imageContentsHash;
#endif
                needsUpdate |= cachedProbe.hdrData != probe.hdrData;    // The probe needs update if the runtime intensity multiplier changes

                if (needsUpdate)
                {
                    cachedProbe.updateCount = texture.updateCount;
#if UNITY_EDITOR
                    cachedProbe.imageContentsHash = texture.imageContentsHash;
#endif
                    m_NeedsUpdate.Add(id);
                }

                // If the probe is set to be updated every frame, we assign the last used frame to -1 so it's evicted in next frame.
                if (probe.reflectionProbe.mode == ReflectionProbeMode.Realtime && probe.reflectionProbe.refreshMode == ReflectionProbeRefreshMode.EveryFrame)
                    cachedProbe.lastUsed = -1;
                else
                    cachedProbe.lastUsed = frameIndex;
                
                cachedProbe.hdrData = probe.hdrData;
                m_Cache[id] = cachedProbe;
            }

            // Grow the atlas if it's not big enough to contain the current allocations.
            if (math.any(m_Resolution < requiredAtlasSize))
            {
                requiredAtlasSize = math.max(m_Resolution, math.ceilpow2(requiredAtlasSize));
                var desc = m_AtlasTexture0.descriptor;
                desc.width = requiredAtlasSize.x;
                desc.height = requiredAtlasSize.y;
                m_AtlasTexture1.width = requiredAtlasSize.x;
                m_AtlasTexture1.height = requiredAtlasSize.y;
                m_AtlasTexture1.Create();

                if (m_AtlasTexture0.width != 1)
                {
                    if (SystemInfo.copyTextureSupport != CopyTextureSupport.None)
                    {
                        Graphics.CopyTexture(m_AtlasTexture0, 0, 0, 0, 0, m_Resolution.x, m_Resolution.y, m_AtlasTexture1, 0, 0, 0, 0);
                    }
                    else
                    {
                        Graphics.Blit(m_AtlasTexture0, m_AtlasTexture1, (float2)m_Resolution / requiredAtlasSize, Vector2.zero);
                    }
                }

                m_AtlasTexture0.Release();
                (m_AtlasTexture0, m_AtlasTexture1) = (m_AtlasTexture1, m_AtlasTexture0);
                m_Resolution = requiredAtlasSize;
            }

            var skipCount = 0;
            for (var probeIndex = 0; probeIndex < probeCount; probeIndex++)
            {
                var probe = probes[probeIndex];
                var id = probe.reflectionProbe.GetInstanceID();
                var dataIndex = probeIndex - skipCount;
                if (!m_Cache.TryGetValue(id, out var cachedProbe) || !probe.texture)
                {
                    skipCount++;
                    continue;
                }
                m_BoxMax[dataIndex] = new Vector4(probe.bounds.max.x, probe.bounds.max.y, probe.bounds.max.z, probe.blendDistance);
                m_BoxMin[dataIndex] = new Vector4(probe.bounds.min.x, probe.bounds.min.y, probe.bounds.min.z, probe.importance);
                m_ProbePosition[dataIndex] = new Vector4(probe.localToWorldMatrix.m03, probe.localToWorldMatrix.m13, probe.localToWorldMatrix.m23, (probe.isBoxProjection ? 1 : -1) * (cachedProbe.mipCount));
                for (var i = 0; i < cachedProbe.mipCount; i++) m_MipScaleOffset[dataIndex * k_MaxMipCount + i] = GetScaleOffset(cachedProbe.levels[i], cachedProbe.dataIndices[i], false, false);
            }

            if (showFullWarning)
            {
                Debug.LogWarning("A number of reflection probes have been skipped due to the reflection probe atlas being full.\nTo fix this, you can decrease the number or resolution of probes.");
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.UpdateReflectionProbeAtlas)))
            {
                cmd.SetRenderTarget(m_AtlasTexture0);

                foreach (var probeId in m_NeedsUpdate)
                {
                    var cachedProbe = m_Cache[probeId];
                    for (var mip = 0; mip < cachedProbe.mipCount; mip++)
                    {
                        var level = cachedProbe.levels[mip];
                        var dataIndex = cachedProbe.dataIndices[mip];
                        // If we need to y-flip we will instead flip the atlas since that is updated less frequent and then the lookup should be correct.
                        // By doing this we won't have to y-flip the lookup in the shader code. 
                        var scaleBias = GetScaleOffset(level, dataIndex, true, !SystemInfo.graphicsUVStartsAtTop);
                        var sizeWithoutPadding = (1 << (m_AtlasAllocator.levelCount + 1 - level)) - 2;
                        Blitter.BlitCubeToOctahedral2DQuadWithPadding(cmd, cachedProbe.texture, new Vector2(sizeWithoutPadding, sizeWithoutPadding), scaleBias, mip, true, 2, cachedProbe.hdrData);
                    }
                }

                cmd.SetGlobalVectorArray(ShaderProperties.BoxMin, m_BoxMin);
                cmd.SetGlobalVectorArray(ShaderProperties.BoxMax, m_BoxMax);
                cmd.SetGlobalVectorArray(ShaderProperties.ProbePosition, m_ProbePosition);
                cmd.SetGlobalVectorArray(ShaderProperties.MipScaleOffset, m_MipScaleOffset);
                cmd.SetGlobalFloat(ShaderProperties.Count, probeCount - skipCount);
                cmd.SetGlobalTexture(ShaderProperties.Atlas, m_AtlasTexture0);
            }

            m_NeedsUpdate.Clear();
        }

        float4 GetScaleOffset(int level, int dataIndex, bool includePadding, bool yflip)
        {
            // level = m_AtlasAllocator.levelCount + 2 - (log2(size) + 1) <=>
            // log2(size) + 1 = m_AtlasAllocator.levelCount + 2 - level <=>
            // log2(size) = m_AtlasAllocator.levelCount + 1 - level <=>
            // size = 2^(m_AtlasAllocator.levelCount + 1 - level)
            var size = (1 << (m_AtlasAllocator.levelCount + 1 - level));
            var coordinate = SpaceFillingCurves.DecodeMorton2D((uint)dataIndex);
            var scale = (size - (includePadding ? 0 : 2)) / ((float2)m_Resolution);
            var bias = ((float2) coordinate * size + (includePadding ? 0 : 1)) / (m_Resolution);
            if (yflip) bias.y = 1.0f - bias.y - scale.y;
            return math.float4(scale, bias);
        }

        public void Dispose()
        {
            if (m_AtlasTexture0)
            {
                m_AtlasTexture0.Release();
                m_AtlasTexture0Handle.Release();
            }
            m_AtlasAllocator.Dispose();

            Object.DestroyImmediate(m_AtlasTexture0);
            Object.DestroyImmediate(m_AtlasTexture1);
            
            this = default;
        }
    }
}
