using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    // Vector2Int  and Vector3Int exist, but not Vector4Int ...
    struct Vector4Int
    {
        uint x;
        uint y;
        uint z;
        uint w;
    };

    class DeferredShaderData : IDisposable
    {
        static DeferredShaderData m_Instance = null;

        /// Precomputed tiles.
        NativeArray<PreTile> m_PreTiles;
        // Store tileID for drawing instanced tiles.
        ComputeBuffer[] m_TileIDBuffers = null;
        // Store an index in RealLightIndexBuffer for each tile.
        ComputeBuffer[] m_TileRelLightBuffers = null;
        // Store point lights data for a draw call.
        ComputeBuffer[] m_PointLightBuffers = null;
        // Store lists of lights. Each tile has a list of lights, which start address is given by m_TileRelLightBuffer.
        // The data stored is a relative light index, which is an index into m_PointLightBuffer. 
        ComputeBuffer[] m_RelLightIndexBuffers = null;

        int m_TileIDBuffer_UsedCount = 0;
        int m_TileRelLightBuffer_UsedCount = 0;
        int m_PointLightBuffer_UsedCount = 0;
        int m_RelLightIndexBuffer_UsedCount = 0;

        DeferredShaderData()
        {
            // TODO: make it a vector
            m_TileIDBuffers = new ComputeBuffer[16];
            m_TileRelLightBuffers = new ComputeBuffer[16];
            m_PointLightBuffers = new ComputeBuffer[16];
            m_RelLightIndexBuffers = new ComputeBuffer[16];

            m_TileIDBuffer_UsedCount = 0;
            m_TileRelLightBuffer_UsedCount = 0;
            m_PointLightBuffer_UsedCount = 0;
            m_RelLightIndexBuffer_UsedCount = 0;
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
            DisposeNativeArray(ref m_PreTiles);
            DisposeBuffers(m_TileIDBuffers);
            DisposeBuffers(m_TileRelLightBuffers);
            DisposeBuffers(m_PointLightBuffers);
            DisposeBuffers(m_RelLightIndexBuffers);
        }

        internal void ResetBuffers()
        {
            m_TileIDBuffer_UsedCount = 0;
            m_TileRelLightBuffer_UsedCount = 0;
            m_PointLightBuffer_UsedCount = 0;
            m_RelLightIndexBuffer_UsedCount = 0;
        }

        internal NativeArray<PreTile> GetPreTiles(int size)
        {
            return GetOrUpdateNativeArray<PreTile>(ref m_PreTiles, size);
        }

        internal ComputeBuffer ReserveTileIDBuffer(int size)
        {
            return GetOrUpdateBuffer<Vector4Int>(m_TileIDBuffers, (size + 3) / 4, ComputeBufferType.Constant, m_TileIDBuffer_UsedCount++);
        }

        internal ComputeBuffer ReserveTileRelLightBuffer(int size)
        {
            return GetOrUpdateBuffer<Vector4Int>(m_TileRelLightBuffers, (size + 3) / 4, ComputeBufferType.Constant, m_TileRelLightBuffer_UsedCount++);
        }

        internal ComputeBuffer ReservePointLightBuffer(int size)
        {
#if UNITY_SUPPORT_STRUCT_IN_CBUFFER
            return GetOrUpdateBuffer<PointLightData>(m_PointLightBuffers, size, ComputeBufferType.Constant, m_PointLightBuffer_UsedCount++);
#else
            int sizeof_PointLightData = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightData));
            int float4Count = sizeof_PointLightData / 16;
            return GetOrUpdateBuffer<Vector4>(m_PointLightBuffers, size * float4Count, ComputeBufferType.Constant, m_PointLightBuffer_UsedCount++);
#endif
        }

        internal ComputeBuffer ReserveRelLightIndexBuffer(int size)
        {
            return GetOrUpdateBuffer<Vector4Int>(m_RelLightIndexBuffers, (size + 3) / 4, ComputeBufferType.Constant, m_RelLightIndexBuffer_UsedCount++);
        }

        NativeArray<T> GetOrUpdateNativeArray<T>(ref NativeArray<T> nativeArray, int size) where T : struct
        {
            if (!nativeArray.IsCreated)
            {
                nativeArray = new NativeArray<T>(size, Allocator.Persistent);
            }
            else if (size > nativeArray.Length)
            {
                nativeArray.Dispose();
                nativeArray = new NativeArray<T>(size, Allocator.Persistent);
            }

            return nativeArray;
        }

        void DisposeNativeArray<T>(ref NativeArray<T> nativeArray) where T : struct
        {
            if (nativeArray.IsCreated)
                nativeArray.Dispose();
        }

        ComputeBuffer GetOrUpdateBuffer<T>(ComputeBuffer[] buffers, int size, ComputeBufferType type, int index) where T : struct
        {
            if (buffers[index] == null)
            {
                buffers[index] = new ComputeBuffer(size, Marshal.SizeOf<T>(), type, ComputeBufferMode.Immutable);
            }
            else if (size > buffers[index].count)
            {
                buffers[index].Dispose();
                buffers[index] = new ComputeBuffer(size, Marshal.SizeOf<T>(), type, ComputeBufferMode.Immutable);
            }

            return buffers[index];
        }

        void DisposeBuffers(ComputeBuffer[] buffers)
        {
            for (int i = 0; i < buffers.Length; ++i)
            {
                if (buffers[i] != null)
                {
                    buffers[i].Dispose();
                    buffers[i] = null;
                }
            }
        }
    }
}
