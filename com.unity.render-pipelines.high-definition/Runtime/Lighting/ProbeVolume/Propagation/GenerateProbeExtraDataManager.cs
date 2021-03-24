using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{

    public class ProbeDynamicGIExtraDataManager
    {
        internal const int kCubeFaceSize = 64;


        private ProbeDynamicGIExtraDataManager()
        { }

        private static ProbeDynamicGIExtraDataManager s_Instance = new ProbeDynamicGIExtraDataManager();
        internal static ProbeDynamicGIExtraDataManager instance { get { return s_Instance; } }

        internal class ExtraDataCubeMap
        {
            internal RTHandle albedo;
            internal RTHandle normal;
            internal RTHandle depth;

            internal void Allocate()
            {
                albedo = RTHandles.Alloc(kCubeFaceSize, kCubeFaceSize, dimension: TextureDimension.Cube, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite: true, name: "AlbedoForProbeExtraData");
                normal = RTHandles.Alloc(kCubeFaceSize, kCubeFaceSize, dimension: TextureDimension.Cube, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, name: "NormalForProbeExtraData");
                depth = RTHandles.Alloc(kCubeFaceSize, kCubeFaceSize, dimension: TextureDimension.Cube, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, name: "DepthForCustomData");
            }

            internal void Dispose()
            {
                RTHandles.Release(albedo);
                RTHandles.Release(normal);
                RTHandles.Release(depth);
            }
        }
        private const int kPoolSize = 64;
        private const float kExtraDataCameraNearPlane = 0.0001f;
        private ExtraDataCubeMap[] m_CubemapPool = new ExtraDataCubeMap[kPoolSize];
        private RTHandle m_DepthForDynamicGIExtraData = null;

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

        internal ExtraDataCubeMap GetCurrentExtraDataDestination()
        {
            Debug.Assert(m_CurrentPoolIndex < kPoolSize);
            return m_CubemapPool[m_CurrentPoolIndex];
        }

        internal int GetCurrentSlice()
        {
            int outputSlice = m_CurrentSlice;
            m_CurrentSlice = (m_CurrentSlice + 1) % 6;
            return outputSlice;
        }

        public void AllocateResources()
        {
            m_CurrentPoolIndex = 0;
            m_DepthForDynamicGIExtraData = RTHandles.Alloc(kCubeFaceSize, kCubeFaceSize, depthBufferBits: DepthBits.Depth32, name: "Depth buffer for GI extra data");
            for (int i = 0; i<kPoolSize; ++i)
            {
                m_CubemapPool[i] = new ExtraDataCubeMap();
                m_CubemapPool[i].Allocate();
            }
        }

        private void Dispose()
        {
            foreach (var extraDataCubeMap in m_CubemapPool)
            {
                extraDataCubeMap.Dispose(); // Just in case
            }
            RTHandles.Release(m_DepthForDynamicGIExtraData);
        }

        internal Matrix4x4 GetSkewRotationMatrix()
        {
            return Matrix4x4.identity;
            return Matrix4x4.Rotate(Quaternion.Euler(40.0f, 40.0f, 40.0f));
        }
    }


    public partial class HDRenderPipeline
    {
        // NOTE: If we have to do a near plane, when recovering position we need to add back the near 


        // TODOs:
        //  - For each cell: 
        //  - Once cubemap is done store in pool.
        //  - Once pool is full process in a compute 
        //  - To retrieve data:
        //      * Undo the rotation done to skew away from corners on the diagonal axis
        //      * Sample in the 14 directions we want
        //      * Write to a buffer

        //  - Once the buffer is complete read back completely and reorganize to hit/misses like is done in old version.
        //  - Flag the data as reorganized on cell and store in cell (refVol.setExtraCellData(cellIdx, DATA) so we can blindly upload when we do not need a rebake.
        //      * NOTE: The cell index is just what is in the list and is the same obtained alongside the positions, a transient thing. 
        //  - When reorganized, re upload.

        // ON THE REFERENCE VOLUME:
        //   - Prepare extra data locally and just do a set of the data.
        //   - Move all the buffer processing to HDRP and here.
        //   - Move the irradiance cache stuff here indexed by cell index? or keep on cell? <<< LATER FOR CLEANUP.
        //   - Get list <cellIndex, LISTOFPOSITIONS> 



        // ---------------------------------------------------------------------
        // -------------------------- Extract Data -----------------------------
        // ---------------------------------------------------------------------
        // This pass extracts data from cubemap to the format we expect them and pack 
        // directly into the output buffer used at runtime.





    }
}
