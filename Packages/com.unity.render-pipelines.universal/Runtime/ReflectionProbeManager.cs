using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        Dictionary<EntityId, CachedProbe> m_Cache;
        Dictionary<EntityId, int> m_WarningCache;
        List<EntityId> m_NeedsUpdate;
        List<EntityId> m_NeedsRemove;

        // Persistent constant buffer path        
        const string k_ReflectionProbeCBName = "URP Reflection Probe Buffer";
        // Channel order of m_ReflectionProbeData (NativeArray<Vector4>), matching the field order in
        // CBUFFER(urp_ReflectionProbeBuffer). With N = m_MaxReflectionProbes, channel C occupies [C*N .. (C+1)*N),
        // except MipScaleOffset which spans k_MaxMipCount channels. Keep in sync with Input.hlsl.
        const int k_BoxMaxChannel         = 0;                                            // urp_ReflProbes_BoxMax
        const int k_BoxMinChannel         = 1;                                            // urp_ReflProbes_BoxMin
        const int k_ProbePositionChannel  = 2;                                            // urp_ReflProbes_ProbePosition
        const int k_MipScaleOffsetChannel = 3;                                            // urp_ReflProbes_MipScaleOffset (k_MaxMipCount channels)
        const int k_RotationChannel       = k_MipScaleOffsetChannel + k_MaxMipCount;      // urp_ReflProbes_Rotation
        const int k_ReflectionProbeChannelCount = k_RotationChannel + 1;
        NativeArray<Vector4> m_ReflectionProbeData;
        GraphicsBuffer m_ReflectionProbeBuffer;

        // loose uniform fallback path
        Vector4[] m_BoxMax;
        Vector4[] m_BoxMin;
        Vector4[] m_ProbePosition;
        Vector4[] m_MipScaleOffset;
        Vector4[] m_Rotations;

        int m_MaxReflectionProbes;
        bool m_UseConstantBuffer;

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
            public ReflectionProbe sourceProbe;
        }

        static class ShaderProperties
        {
            // CBUFFER binding for main persistent CB path
            public static readonly int ReflectionProbeBuffer = Shader.PropertyToID("urp_ReflectionProbeBuffer");

            // loose uniform fallback
            public static readonly int BoxMin = Shader.PropertyToID("urp_ReflProbes_BoxMin");
            public static readonly int BoxMax = Shader.PropertyToID("urp_ReflProbes_BoxMax");
            public static readonly int ProbePosition = Shader.PropertyToID("urp_ReflProbes_ProbePosition");
            public static readonly int MipScaleOffset = Shader.PropertyToID("urp_ReflProbes_MipScaleOffset");
            public static readonly int Rotation = Shader.PropertyToID("urp_ReflProbes_Rotation");

            public static readonly int Count = Shader.PropertyToID("urp_ReflProbes_Count");
            public static readonly int Atlas = Shader.PropertyToID("urp_ReflProbes_Atlas");
        }

        public RenderTexture atlasRT => m_AtlasTexture0;
        public RTHandle atlasRTHandle => m_AtlasTexture0Handle;

        public void PreSetup()
        {
            int maxProbes = UniversalRenderPipeline.maxVisibleReflectionProbes;
            if (maxProbes != m_MaxReflectionProbes)
            {
                m_MaxReflectionProbes = maxProbes;

                if (m_UseConstantBuffer)
                {
                    DisposeReflectionProbeConstantBuffer();
                    CreateReflectionProbeConstantBuffer();
                }
                else
                {
                    CreateReflectionProbeLooseUniformArrays();
                }
            }
        }

        public static ReflectionProbeManager Create()
        {
            var instance = new ReflectionProbeManager();
            instance.Init();
            return instance;
        }

        void Init()
        {
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
            
            m_MaxReflectionProbes = UniversalRenderPipeline.maxVisibleReflectionProbes;
            m_UseConstantBuffer = RenderingUtils.usePersistentConstantBuffer;

            m_Cache = new Dictionary<EntityId, CachedProbe>(m_MaxReflectionProbes);
            m_WarningCache = new Dictionary<EntityId, int>(m_MaxReflectionProbes);
            
            m_NeedsUpdate = new List<EntityId>(m_MaxReflectionProbes);
            m_NeedsRemove = new List<EntityId>(m_MaxReflectionProbes);

            if (m_UseConstantBuffer)
                CreateReflectionProbeConstantBuffer();
            else
                CreateReflectionProbeLooseUniformArrays();
        }

        void CreateReflectionProbeConstantBuffer()
        {
            int length = m_MaxReflectionProbes * k_ReflectionProbeChannelCount;

            m_ReflectionProbeData = new NativeArray<Vector4>(length, Allocator.Persistent);

            // GraphicsBuffer ctor will throw for zero length
            if (length > 0)
            {
                m_ReflectionProbeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, length, UnsafeUtility.SizeOf<Vector4>())
                {
                    name = k_ReflectionProbeCBName
                };
            }
        }

        void DisposeReflectionProbeConstantBuffer()
        {
            if (m_ReflectionProbeData.IsCreated)
                m_ReflectionProbeData.Dispose();

            if (m_ReflectionProbeBuffer != null)
            {
                Shader.SetGlobalConstantBuffer(ShaderProperties.ReflectionProbeBuffer, (ComputeBuffer)null, 0, 0);
                m_ReflectionProbeBuffer.Dispose();
                m_ReflectionProbeBuffer = null;
            }
        }

        void CreateReflectionProbeLooseUniformArrays()
        {
            m_BoxMax = new Vector4[m_MaxReflectionProbes];
            m_BoxMin = new Vector4[m_MaxReflectionProbes];
            m_ProbePosition = new Vector4[m_MaxReflectionProbes];
            m_MipScaleOffset = new Vector4[m_MaxReflectionProbes * k_MaxMipCount];
            m_Rotations = new Vector4[m_MaxReflectionProbes];
        }
        
        public unsafe void UpdateGpuData(CommandBuffer cmd, ref CullingResults cullResults)
        {
            var probes = cullResults.visibleReflectionProbes;
            int maxProbes = m_MaxReflectionProbes;
            var probeCount = math.min(probes.Length, maxProbes);
            var frameIndex = Time.renderedFrameCount;

            // Populate list of probes we need to remove to avoid modifying dictionary while iterating.
            foreach (var (id, cachedProbe) in m_Cache)
            {
                // Evict probe if any of the following are true:
                // - Probe was not used for more than 1 frame
                // - The texture no longer exists
                // - The texture size changed
                // - The cached texture no longer matches the texture of the reflection probe (it was reassigned)
                if (Math.Abs(cachedProbe.lastUsed - frameIndex) > 1 ||
                    !cachedProbe.texture ||
                    cachedProbe.size != cachedProbe.texture.width ||
                    (cachedProbe.sourceProbe && cachedProbe.texture != cachedProbe.sourceProbe.texture))
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
                var id = probe.reflectionProbe.GetEntityId();
#pragma warning disable 618 // Todo(@daniel.andersen): Remove deprecated API usage
                var wasCached = m_Cache.TryGetValue(id, out var cachedProbe);
#pragma warning restore 618

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
                    cachedProbe.sourceProbe = probe.reflectionProbe;

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
#pragma warning disable 618 // Todo(@daniel.andersen): Remove deprecated API usage
                        if (!m_WarningCache.ContainsKey(id)) showFullWarning = true;
                        m_WarningCache[id] = frameIndex;
#pragma warning restore 618
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
#pragma warning disable 618 // Todo(@daniel.andersen): Remove deprecated API usage
                    m_NeedsUpdate.Add(id);
#pragma warning restore 618
                }

                // If the probe is set to be updated every frame, we assign the last used frame to -1 so it's evicted in next frame.
                if (probe.reflectionProbe.mode == ReflectionProbeMode.Realtime && probe.reflectionProbe.refreshMode == ReflectionProbeRefreshMode.EveryFrame)
                    cachedProbe.lastUsed = -1;
                else
                    cachedProbe.lastUsed = frameIndex;

                cachedProbe.hdrData = probe.hdrData;
#pragma warning disable 618 // Todo(@daniel.andersen): Remove deprecated API usage
                m_Cache[id] = cachedProbe;
#pragma warning restore 618
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

            int boxMaxOffset        = maxProbes * k_BoxMaxChannel;
            int boxMinOffset        = maxProbes * k_BoxMinChannel;
            int probePositionOffset = maxProbes * k_ProbePositionChannel;
            int mipScaleOffset      = maxProbes * k_MipScaleOffsetChannel;  // occupies k_MaxMipCount channels
            int rotationOffset      = maxProbes * k_RotationChannel;

            var skipCount = 0;
            for (var probeIndex = 0; probeIndex < probeCount; probeIndex++)
            {
                var probe = probes[probeIndex];
                var id = probe.reflectionProbe.GetEntityId();
                var dataIndex = probeIndex - skipCount;
#pragma warning disable 618 // Todo(@daniel.andersen): Remove deprecated API usage
                if (!m_Cache.TryGetValue(id, out var cachedProbe) || !probe.texture)
#pragma warning restore 618
                {
                    skipCount++;
                    continue;
                }

                var boxMax        = new Vector4(probe.bounds.max.x, probe.bounds.max.y, probe.bounds.max.z, probe.blendDistance);
                var boxMin        = new Vector4(probe.bounds.min.x, probe.bounds.min.y, probe.bounds.min.z, probe.importance);
                var probePosition = new Vector4(probe.localToWorldMatrix.m03, probe.localToWorldMatrix.m13, probe.localToWorldMatrix.m23, (probe.isBoxProjection ? 1 : -1) * (cachedProbe.mipCount));
                var rot           = Quaternion.Inverse(probe.reflectionProbe.transform.rotation);
                var rotation      = new Vector4(rot.x, rot.y, rot.z, rot.w);

                if (m_UseConstantBuffer)
                {
                    m_ReflectionProbeData[boxMaxOffset + dataIndex] = boxMax;
                    m_ReflectionProbeData[boxMinOffset + dataIndex] = boxMin;
                    m_ReflectionProbeData[probePositionOffset + dataIndex] = probePosition;
                    for (var i = 0; i < cachedProbe.mipCount; i++)
                        m_ReflectionProbeData[mipScaleOffset + dataIndex * k_MaxMipCount + i] = GetScaleOffset(cachedProbe.levels[i], cachedProbe.dataIndices[i], false, false);
                    m_ReflectionProbeData[rotationOffset + dataIndex] = rotation;
                }
                else
                {
                    m_BoxMax[dataIndex] = boxMax;
                    m_BoxMin[dataIndex] = boxMin;
                    m_ProbePosition[dataIndex] = probePosition;
                    for (var i = 0; i < cachedProbe.mipCount; i++)
                        m_MipScaleOffset[dataIndex * k_MaxMipCount + i] = GetScaleOffset(cachedProbe.levels[i], cachedProbe.dataIndices[i], false, false);
                    m_Rotations[dataIndex] = rotation;
                }
            }

            if (showFullWarning)
            {
                Debug.LogWarning("A number of reflection probes have been skipped due to the reflection probe atlas being full.\nTo fix this, you can decrease the number or resolution of probes.");
            }

            using (new ProfilingScope(cmd, URPProfilingSamplers.UpdateReflectionProbeAtlas, m_AtlasTexture0))
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

                if (m_UseConstantBuffer)
                {
                    if (m_ReflectionProbeBuffer != null)
                    {
                        m_ReflectionProbeBuffer.SetData(m_ReflectionProbeData);
                        cmd.SetGlobalConstantBuffer(m_ReflectionProbeBuffer, ShaderProperties.ReflectionProbeBuffer, 0, m_ReflectionProbeData.Length * UnsafeUtility.SizeOf<Vector4>());
                    }
                }
                else
                {
                    cmd.SetGlobalVectorArray(ShaderProperties.BoxMin, m_BoxMin);
                    cmd.SetGlobalVectorArray(ShaderProperties.BoxMax, m_BoxMax);
                    cmd.SetGlobalVectorArray(ShaderProperties.ProbePosition, m_ProbePosition);
                    cmd.SetGlobalVectorArray(ShaderProperties.MipScaleOffset, m_MipScaleOffset);
                    cmd.SetGlobalVectorArray(ShaderProperties.Rotation, m_Rotations);
                }
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

            DisposeReflectionProbeConstantBuffer();

            Object.DestroyImmediate(m_AtlasTexture0);
            Object.DestroyImmediate(m_AtlasTexture1);

            this = default;
        }
    }
}
