using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal class DeferredGPUTiler
    {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID || UNITY_SWITCH)
        //static readonly int kPlaformLaneCount = 32;
        static readonly int kPlaformLaneX = 8;
        static readonly int kPlaformLaneY = 4;
#else
        //static readonly int kPlaformLaneCount = 64;
        static readonly int kPlaformLaneX = 8;
        static readonly int kPlaformLaneY = 8;
#endif

        // Precomputed light data
        internal struct PrePunctualLight
        {
            // view-space position.
            public float3 posVS;
            // Radius in world unit.
            public float radius;
            // Distance between closest bound of the light and the camera. Used for sorting lights front-to-back.
//            public float minDist;
            // Projected position of the sphere centre on the screen (near plane).
            public float2 screenPos;
            // Index into renderingData.lightData.visibleLights native array.
//            public ushort visLightIndex;
        }

        internal struct TileHeader
        {
            public uint listOffset;
            public uint listCount;
//            public uint depthRange;
//            public uint bitMask;
        };

        internal struct UClearLights
        {
            public uint _TileCount;
            public uint _MaxLightsPerTile;
            public uint2 _Pad;
        }

        internal struct UCullLights
        {
            public uint2 _CoarseTileCount;
            public uint2 _TileCount;
            public uint2 _Subdivisions;
            public uint _LightCount;
            public uint _MaxLightsPerTile;
            public float _TileRadiusInv;
            public int3 _Pad0;
            public float2 _TileExtents;
            public float2 _TileSize;
            public float _FrustumPlanes_Left;
            public float _FrustumPlanes_Right;
            public float _FrustumPlanes_Bottom;
            public float _FrustumPlanes_Top;
            public float _FrustumPlanes_ZNear;
            public float _FrustumPlanes_ZFar;
            public float2 _Pad1;
        }

        internal struct UDrawIndirectArgs
        {
            public uint2 _TileCount;
            public uint _MaxLightsPerTile;
            public uint _Pad0;
        }

        int m_TilePixelWidth;
        int m_TilePixelHeight;
        int m_TileXCount;
        int m_TileYCount;
        int m_MaxTileXCount;
        int m_MaxTileYCount;
        // Fixed header size in uint in m_TileHeader.
        // Only finest tiler requires to store extra per-tile information (light list depth range, bitmask for 2.5D culling).
        int m_TileHeaderSize;
        // Indicative max lights per tile. Only used when initializing the size of m_DataTile for the first time.
        int m_MaxLightsPerTile;
        // 0, 1 or 2 (see DeferredConfig.kTilerDepth)
        int m_TilerLevel;

        // Camera frustum planes, adjusted to account for tile size.
        FrustumPlanes m_FrustumPlanes;
        // Are we dealing with an orthographic projection.
        bool m_IsOrthographic;

        // Store all visible light indices for all tiles.
        // (currently) Contains sequential blocks of ushort values (light indices and optionally lightDepthRange), for each tile
        // For example for platforms using 16x16px tiles:
        // in a finest        tiler DeferredLights.m_Tilers[0] ( 16x16px  tiles), each tile will use a block of  1 *  1 * 32 =   32 ushort values
        // in an intermediate tiler DeferredLights.m_Tilers[1] ( 64x64px  tiles), each tile will use a block of  4 *  4 * 32 =  512 ushort values
        // in a coarsest      tiler DeferredLights.m_Tilers[2] (256x256px tiles), each tile will use a block of 16 * 16 * 32 = 8192 ushort values
        ComputeBuffer m_TileData;

        // Store tile header (fixed size per tile)
        // light offset, light count, optionally additional per-tile "header" values.
        ComputeBuffer m_TileHeaders;

        ComputeShader m_tileLightCullingCS;


        public DeferredGPUTiler(ComputeShader tileLightCullingCS, int tilePixelWidth, int tilePixelHeight, int maxLightsPerTile, int tilerLevel)
        {
            m_TilePixelWidth = tilePixelWidth;
            m_TilePixelHeight = tilePixelHeight;
            m_TileXCount = 0;
            m_TileYCount = 0;
            m_MaxTileXCount = 0;
            m_MaxTileYCount = 0;
            // Finest tiler (at index 0) computes extra tile data stored into the header, so it requires more space. See CullFinalLights() vs CullIntermediateLights().
            // Finest tiler: lightListOffset, lightCount, listDepthRange, listBitMask
            // Coarse tilers: lightListOffset, lightCount
            m_TileHeaderSize = tilerLevel == 0 ? 4 : 2;
            m_MaxLightsPerTile = maxLightsPerTile;
            m_TilerLevel = tilerLevel;
            m_FrustumPlanes = new FrustumPlanes { left = 0, right = 0, bottom = 0, top = 0, zNear = 0, zFar = 0 };
            m_IsOrthographic = false;
            m_TileData = null;
            m_TileHeaders = null;

            m_tileLightCullingCS = tileLightCullingCS;
        }

        public int TilerLevel
        {
            get { return m_TilerLevel; }
        }

        public int TileXCount
        {
            get { return m_TileXCount; }
        }

        public int TileYCount
        {
            get { return m_TileYCount; }
        }

        public int MaxTileXCount
        {
            get { return m_MaxTileXCount; }
        }

        public int MaxTileYCount
        {
            get { return m_MaxTileYCount; }
        }

        public int TilePixelWidth
        {
            get { return m_TilePixelWidth; }
        }

        public int TilePixelHeight
        {
            get { return m_TilePixelHeight; }
        }

        public int TileHeaderSize
        {
            get { return m_TileHeaderSize; }
        }

        public int MaxLightsPerTile
        {
            get { return m_MaxLightsPerTile; }
        }

        public int TileDataCapacity
        {
            get { return m_MaxTileXCount * m_MaxTileYCount * m_MaxLightsPerTile; }
        }

        public ComputeBuffer TileData
        {
            get { return m_TileData; }
        }

        public ComputeBuffer TileHeaders
        {
            get { return m_TileHeaders; }
        }


        public void PrecomputeTiles(Matrix4x4 proj, bool isOrthographic, int renderWidth, int renderHeight)
        {
            m_TileXCount = (renderWidth + m_TilePixelWidth - 1) / m_TilePixelWidth;
            m_TileYCount = (renderHeight + m_TilePixelHeight - 1) / m_TilePixelHeight;

            if (m_TileXCount > m_MaxTileXCount || m_TileYCount > m_MaxTileYCount)
            {
                m_MaxTileXCount = m_TileXCount;
                m_MaxTileYCount = m_TileYCount;
            }

            m_TileData = DeferredShaderData.instance.GetGPUTilerTileData(m_TilerLevel, m_MaxTileXCount * m_MaxTileYCount * m_MaxLightsPerTile);
            m_TileHeaders = DeferredShaderData.instance.GetGPUTilerTileHeaders(m_TilerLevel, m_MaxTileXCount * m_MaxTileYCount * m_TileHeaderSize);

            // Adjust render width and height to account for tile size expanding over the screen (tiles have a fixed pixel size).
            int adjustedRenderWidth = Align(renderWidth, m_TilePixelWidth);
            int adjustedRenderHeight = Align(renderHeight, m_TilePixelHeight);

            // Now adjust the right and bottom clipping planes.
            m_FrustumPlanes = proj.decomposeProjection;
            m_FrustumPlanes.right = m_FrustumPlanes.left + (m_FrustumPlanes.right - m_FrustumPlanes.left) * (adjustedRenderWidth / (float)renderWidth);
            m_FrustumPlanes.bottom = m_FrustumPlanes.top + (m_FrustumPlanes.bottom - m_FrustumPlanes.top) * (adjustedRenderHeight / (float)renderHeight);
            m_IsOrthographic = isOrthographic;
        }

        public void CullLights(CommandBuffer cmd, ComputeBuffer preLights, ComputeBuffer tileHeaders, ComputeBuffer tileData, int coarseTileXCount, int coarseTileYCount, int lightCount, bool isOrthographic)
        {
            int kernelIndex;
            if (tileHeaders == null && m_TilerLevel != 0)
                kernelIndex = 0; // CSCullFirstLights
            else if (tileHeaders != null && m_TilerLevel != 0)
                kernelIndex = 1; // CSCullIntermediateLights
            else if (tileHeaders != null && m_TilerLevel == 0)
                kernelIndex = 2; // CSCullFinalLights
            else
                kernelIndex = 3; // CSCullFirstAndFinalLights

            if (isOrthographic)
                kernelIndex += 4;

            float2 tileSize = new float2(m_FrustumPlanes.right - m_FrustumPlanes.left, m_FrustumPlanes.top - m_FrustumPlanes.bottom) / float2(m_TileXCount, m_TileYCount);
            float tileRadiusInv = 1.0f / (sqrt(tileSize.x * tileSize.x + tileSize.y * tileSize.y) * 0.5f);

            NativeArray<UCullLights> cullLights = new NativeArray<UCullLights>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            cullLights[0] = new UCullLights() {
                _CoarseTileCount = new uint2((uint)coarseTileXCount, (uint)coarseTileYCount),
                _TileCount = new uint2((uint)m_TileXCount, (uint)m_TileYCount),
                _Subdivisions = new uint2((uint)DeferredConfig.kTilerSubdivisions, (uint)DeferredConfig.kTilerSubdivisions),
                _LightCount = (uint)lightCount,
                _MaxLightsPerTile = (uint)m_MaxLightsPerTile,
                _TileRadiusInv = tileRadiusInv,
                _TileExtents = tileSize * 0.5f,
                _TileSize = tileSize,
                _FrustumPlanes_Left = m_FrustumPlanes.left,
                _FrustumPlanes_Right = m_FrustumPlanes.right,
                _FrustumPlanes_Bottom = m_FrustumPlanes.bottom,
                _FrustumPlanes_Top = m_FrustumPlanes.top,
                _FrustumPlanes_ZNear = m_FrustumPlanes.zNear,
                _FrustumPlanes_ZFar = m_FrustumPlanes.zFar
            };
            ComputeBuffer uCullLights = DeferredShaderData.instance.ReserveBuffer<UCullLights>(1, true);
            uCullLights.SetData(cullLights);
            cullLights.Dispose();

            cmd.SetComputeBufferParam(m_tileLightCullingCS, kernelIndex, "_Lights", preLights);
            cmd.SetComputeBufferParam(m_tileLightCullingCS, kernelIndex, "_TileHeaders", tileHeaders);
            cmd.SetComputeBufferParam(m_tileLightCullingCS, kernelIndex, DeferredLights.ShaderConstants._TileData, tileData);
            cmd.SetComputeBufferParam(m_tileLightCullingCS, kernelIndex, "_OutTileHeaders", m_TileHeaders);
            cmd.SetComputeBufferParam(m_tileLightCullingCS, kernelIndex, "_OutTileData", m_TileData);
            cmd.SetComputeConstantBufferParam(m_tileLightCullingCS, "UCullLights", uCullLights, 0, Marshal.SizeOf<UCullLights>());

            /*
            int groupXCount = (m_TileXCount + kPlaformLaneX - 1) / kPlaformLaneX;
            int groupYCount = (m_TileYCount + kPlaformLaneY - 1) / kPlaformLaneY;
            cmd.DispatchCompute(m_tileLightCullingCS, kernelIndex, groupXCount, groupYCount, 1);
            */
            cmd.DispatchCompute(m_tileLightCullingCS, kernelIndex, m_TileXCount, m_TileYCount, 1);
        }

        public void FillIndirectArgs(CommandBuffer cmd, ComputeBuffer indirectArgs, ComputeBuffer tileList, RenderTargetIdentifier tileDepthInfoTexture)
        {
            int kernelIndex = 8;

            NativeArray<UDrawIndirectArgs> drawIndirectArgs = new NativeArray<UDrawIndirectArgs>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            drawIndirectArgs[0] = new UDrawIndirectArgs()
            {
                _TileCount = new uint2((uint)m_TileXCount, (uint)m_TileYCount),
                _MaxLightsPerTile = (uint)m_MaxLightsPerTile
            };
            ComputeBuffer uDrawIndirectArgs = DeferredShaderData.instance.ReserveBuffer<UDrawIndirectArgs>(1, true);
            uDrawIndirectArgs.SetData(drawIndirectArgs);
            drawIndirectArgs.Dispose();

            cmd.SetComputeTextureParam(m_tileLightCullingCS, kernelIndex, DeferredLights.ShaderConstants._TileDepthInfoTexture, tileDepthInfoTexture);
            cmd.SetComputeBufferParam(m_tileLightCullingCS, kernelIndex, DeferredLights.ShaderConstants._TileHeaders, m_TileHeaders);
            cmd.SetComputeBufferParam(m_tileLightCullingCS, kernelIndex, DeferredLights.ShaderConstants._TileData, m_TileData);
            cmd.SetComputeBufferParam(m_tileLightCullingCS, kernelIndex, "_OutIndirectArgs", indirectArgs);
            cmd.SetComputeBufferParam(m_tileLightCullingCS, kernelIndex, "_OutTileList", tileList);
            cmd.SetComputeConstantBufferParam(m_tileLightCullingCS, "UDrawIndirectArgs", uDrawIndirectArgs, 0, Marshal.SizeOf<UDrawIndirectArgs>());

            int groupXCount = (m_TileXCount + kPlaformLaneX - 1) / kPlaformLaneX;
            int groupYCount = (m_TileYCount + kPlaformLaneY - 1) / kPlaformLaneY;
            cmd.DispatchCompute(m_tileLightCullingCS, kernelIndex, groupXCount, groupYCount, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 MakePlane(float3 pb, float3 pc)
        {
            float3 v0 = pb;
            float3 v1 = pc;
            float3 n = cross(v0, v1);
            n = normalize(n);

            // The planes pass all by the origin.
            return new float4(n.x, n.y, n.z, 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 MakePlane(float3 pa, float3 pb, float3 pc)
        {
            float3 v0 = pb - pa;
            float3 v1 = pc - pa;
            float3 n = cross(v0, v1);
            n = normalize(n);

            return new float4(n.x, n.y, n.z, -dot(n, pa));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Align(int s, int alignment)
        {
            return ((s + alignment - 1) / alignment) * alignment;
        }
    }
}
