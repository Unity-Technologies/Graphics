using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{

    public class ProbeDynamicGIExtraDataManager
    {
        internal const int kCubeFaceSize = 16;
        private const int kPoolSize = 64;


        private ProbeDynamicGIExtraDataManager()
        { }

        private static ProbeDynamicGIExtraDataManager s_Instance = new ProbeDynamicGIExtraDataManager();
        internal static ProbeDynamicGIExtraDataManager instance { get { return s_Instance; } }

        internal bool needBaking = true;

        public CullingResults cullResult;
        public bool cullDone = false;

        internal class ExtraDataCubemapPool
        {
            internal RTHandle albedo;
            internal RTHandle normal;
            internal RTHandle depth;

            internal float[] validity = new float[kPoolSize] ;

            internal void Allocate()
            {
                albedo = RTHandles.Alloc(kCubeFaceSize, kCubeFaceSize, kPoolSize * 6, dimension: TextureDimension.CubeArray, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite: true, name: "AlbedoForProbeExtraData");
                normal = RTHandles.Alloc(kCubeFaceSize, kCubeFaceSize, kPoolSize * 6, dimension: TextureDimension.CubeArray, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, name: "NormalForProbeExtraData");
                depth = RTHandles.Alloc(kCubeFaceSize, kCubeFaceSize, kPoolSize * 6, dimension: TextureDimension.CubeArray, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, name: "DepthForCustomData");
            }

            internal void Dispose()
            {
                RTHandles.Release(albedo);
                RTHandles.Release(normal);
                RTHandles.Release(depth);
            }

        }

        private const float kExtraDataCameraNearPlane = 0.0001f;
        private RTHandle m_DepthForDynamicGIExtraData = null;

        private ExtraDataCubemapPool m_DataPool = new ExtraDataCubemapPool();

        private int m_CurrentPoolIndex = 0;
        private int m_CurrentSlice = 0;

        internal void MoveToNextExtraData()
        {
            m_CurrentPoolIndex = (m_CurrentPoolIndex + 1) % kPoolSize;
        }

        internal int GetCubeMapSize()
        {
            return kCubeFaceSize;
        }
        internal RTHandle GetDepthBuffer()
        {
            return m_DepthForDynamicGIExtraData;
        }

        internal int GetCurrentExtraDataPoolIndex()
        {
            return m_CurrentPoolIndex;
        }

        internal int GetCubemapFace()
        {
            int outputSlice = m_CurrentSlice;
            m_CurrentSlice = (m_CurrentSlice + 1) % 6;
            return outputSlice;
        }

        internal ExtraDataCubemapPool GetCubemapPool()
        {
            if (m_DataPool == null) m_DataPool.Allocate();

            return m_DataPool;
        }

        public void AllocateResources()
        {
            m_CurrentPoolIndex = 0;
            m_DepthForDynamicGIExtraData = RTHandles.Alloc(kCubeFaceSize, kCubeFaceSize, depthBufferBits: DepthBits.Depth32, name: "Depth buffer for GI extra data");
            m_DataPool.Allocate();
        }

        private void Dispose()
        {
            m_DataPool.Dispose();
            RTHandles.Release(m_DepthForDynamicGIExtraData);
        }

        internal Matrix4x4 GetSkewRotationMatrix()
        {
            return Matrix4x4.identity;
            return Matrix4x4.Rotate(Quaternion.Euler(40.0f, 40.0f, 40.0f));
        }

        internal void ReorganizeBuffer(ComputeBuffer packedData, int bufferSize, out int hitCount, out int missCount)
        {
            uint[] unsortedData = new uint[bufferSize];

            packedData.GetData(unsortedData);

            int elementCount = bufferSize / 3; // 3 uint per probe/axis couple

            List<uint> hits = new List<uint>();
            List<uint> misses = new List<uint>();
            missCount = 0;
            hitCount = 0;

            for (int i=0; i < elementCount; ++i)
            {
                uint packedAlbedo  = unsortedData[i * 3 + 0];
                uint packedNormal  = unsortedData[i * 3 + 1];
                uint packedIndices = unsortedData[i * 3 + 2];

                if (packedAlbedo == 0)
                {
                    misses.Add(packedAlbedo);
                    misses.Add(packedNormal);
                    misses.Add(packedIndices);

                    missCount++;
                }
                else
                {
                    hits.Add(packedAlbedo);
                    hits.Add(packedNormal);
                    hits.Add(packedIndices);

                    hitCount++;
                }
            }

            unsortedData = new uint[bufferSize];
            int outIndex = 0;
            for(int i=0; i<hits.Count; ++i)
            {
                unsortedData[outIndex++] = hits[i];
            }
            for (int i = 0; i < misses.Count; ++i)
            {
                unsortedData[outIndex++] = misses[i];
            }

            packedData.SetData(unsortedData);

            Debug.Assert(outIndex == bufferSize);

        }
    }

}
