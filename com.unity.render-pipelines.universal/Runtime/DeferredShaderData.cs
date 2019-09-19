using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    struct Vector4UInt
    {
        public uint x;
        public uint y;
        public uint z;
        public uint w;

        public Vector4UInt(uint _x, uint _y, uint _z, uint _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }
    };


    class DeferredShaderData : IDisposable
    {
        static DeferredShaderData m_Instance = null;

        /// Precomputed tiles.
        NativeArray<PreTile>[] m_PreTiles = null;
        // Store tileData for drawing instanced tiles.
        ComputeBuffer[,] m_TileLists = null;
        // Store point lights data for a draw call.
        ComputeBuffer[,] m_PointLightBuffers = null;
        // Store lists of lights. Each tile has a list of lights, which start address is given by m_TileList.
        // The data stored is a relative light index, which is an index into m_PointLightBuffer. 
        ComputeBuffer[,] m_RelLightLists = null;

        int m_TileList_UsedCount = 0;
        int m_PointLightBuffer_UsedCount = 0;
        int m_RelLightList_UsedCount = 0;

        int m_FrameLatency = 4;
        int m_FrameIndex = 0;

        DeferredShaderData()
        {
            // TODO: make it a vector
            m_PreTiles = new NativeArray<PreTile>[DeferredConfig.kTilerDepth];
            m_TileLists = new ComputeBuffer[m_FrameLatency, 32]; 
            m_PointLightBuffers = new ComputeBuffer[m_FrameLatency,32];
            m_RelLightLists = new ComputeBuffer[m_FrameLatency,32];

            m_TileList_UsedCount = 0;
            m_PointLightBuffer_UsedCount = 0;
            m_RelLightList_UsedCount = 0;
        }

        internal static DeferredShaderData instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new DeferredShaderData();

                return m_Instance;
            }
        }

        public void Dispose()
        {
            DisposeNativeArrays(ref m_PreTiles);
            DisposeBuffers(m_TileLists);
            DisposeBuffers(m_PointLightBuffers);
            DisposeBuffers(m_RelLightLists);
        }

        internal void ResetBuffers()
        {
            m_TileList_UsedCount = 0;
            m_PointLightBuffer_UsedCount = 0;
            m_RelLightList_UsedCount = 0;
            m_FrameIndex = (m_FrameIndex + 1) % m_FrameLatency;
        }

        internal NativeArray<PreTile> GetPreTiles(int level, int count)
        {
            return GetOrUpdateNativeArray<PreTile>(ref m_PreTiles, level, count);
        }

        internal ComputeBuffer ReserveTileList(int count)
        {
            if (DeferredConfig.kUseCBufferForTileList)
                return GetOrUpdateBuffer<Vector4UInt>(m_TileLists, count, ComputeBufferType.Constant, m_TileList_UsedCount++);
            else
                return GetOrUpdateBuffer<TileData>(m_TileLists, count, ComputeBufferType.Structured, m_TileList_UsedCount++);
        }

        internal ComputeBuffer ReservePointLightBuffer(int count)
        {
            if (DeferredConfig.kUseCBufferForLightData)
            {
                int sizeof_PointLightData = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightData));
                int vec4Count = sizeof_PointLightData / 16;
                return GetOrUpdateBuffer<Vector4UInt>(m_PointLightBuffers, count * vec4Count, ComputeBufferType.Constant, m_PointLightBuffer_UsedCount++);
            }
            else
                return GetOrUpdateBuffer<PointLightData>(m_PointLightBuffers, count, ComputeBufferType.Structured, m_PointLightBuffer_UsedCount++);
        }

        internal ComputeBuffer ReserveRelLightList(int count)
        {
            if (DeferredConfig.kUseCBufferForLightList)
                return GetOrUpdateBuffer<Vector4UInt>(m_RelLightLists, (count + 3) / 4, ComputeBufferType.Constant, m_RelLightList_UsedCount++);
            else
                return GetOrUpdateBuffer<uint>(m_RelLightLists, count, ComputeBufferType.Structured, m_RelLightList_UsedCount++);
        }

        NativeArray<T> GetOrUpdateNativeArray<T>(ref NativeArray<T>[] nativeArrays, int level, int count) where T : struct
        {
            if (!nativeArrays[level].IsCreated)
            {
                nativeArrays[level] = new NativeArray<T>(count, Allocator.Persistent);
            }
            else if (count > nativeArrays[level].Length)
            {
                nativeArrays[level].Dispose();
                nativeArrays[level] = new NativeArray<T>(count, Allocator.Persistent);
            }

            return nativeArrays[level];
        }

        void DisposeNativeArrays<T>(ref NativeArray<T>[] nativeArrays) where T : struct
        {
            for (int i = 0; i < nativeArrays.Length; ++i)
            {
                if (nativeArrays[i].IsCreated)
                    nativeArrays[i].Dispose();
            }
        }

        ComputeBuffer GetOrUpdateBuffer<T>(ComputeBuffer[,] buffers, int count, ComputeBufferType type, int index) where T : struct
        {
            if (buffers[m_FrameIndex,index] == null)
            {
                buffers[m_FrameIndex, index] = new ComputeBuffer(count, Marshal.SizeOf<T>(), type, ComputeBufferMode.Immutable);
            }
            else if (count > buffers[m_FrameIndex, index].count)
            {
                buffers[m_FrameIndex, index].Dispose();
                buffers[m_FrameIndex, index] = new ComputeBuffer(count, Marshal.SizeOf<T>(), type, ComputeBufferMode.Immutable);
            }

            return buffers[m_FrameIndex, index];
        }

        void DisposeBuffers(ComputeBuffer[,] buffers)
        {
            for (int i = 0; i < buffers.GetLength(0); ++i)
            {
                for (int j = 0; j < buffers.GetLength(1); ++j)
                {

                    if (buffers[i, j] != null)
                    {
                        buffers[i, j].Dispose();
                        buffers[i, j] = null;
                    }
                }
            }
        }
    }
}
