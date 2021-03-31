using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    [System.Serializable]
    public struct ProbeExtraData
    {

        // TODO_FCC: TODO FIND A WAY TO MOVE FROM HERE. (moving the baking would do.)
        public static readonly int s_AxisCount = 14;
        internal static readonly float s_DiagonalDist = Mathf.Sqrt(3.0f);
        internal static readonly float s_Diagonal = 1.0f / s_DiagonalDist;
        internal static readonly float s_2DDiagonalDist = Mathf.Sqrt(2.0f);
        internal static readonly float s_2DDiagonal = 1.0f / s_2DDiagonalDist;

        public static readonly Vector4[] NeighbourAxis =
        {
            // primary axis
            new Vector4(1, 0, 0, 1),
            new Vector4(-1, 0, 0, 1),
            new Vector4(0, 1, 0, 1),
            new Vector4(0, -1, 0, 1),
            new Vector4(0, 0, 1, 1),
            new Vector4(0, 0, -1, 1),

            // 3D diagonals
            new Vector4(s_Diagonal,  s_Diagonal,  s_Diagonal, s_DiagonalDist),
            new Vector4(s_Diagonal,  s_Diagonal, -s_Diagonal, s_DiagonalDist),
            new Vector4(s_Diagonal, -s_Diagonal,  s_Diagonal, s_DiagonalDist),
            new Vector4(s_Diagonal, -s_Diagonal, -s_Diagonal, s_DiagonalDist),

            new Vector4(-s_Diagonal,  s_Diagonal,  s_Diagonal, s_DiagonalDist),
            new Vector4(-s_Diagonal,  s_Diagonal, -s_Diagonal, s_DiagonalDist),
            new Vector4(-s_Diagonal, -s_Diagonal,  s_Diagonal, s_DiagonalDist),
            new Vector4(-s_Diagonal, -s_Diagonal, -s_Diagonal, s_DiagonalDist),
        };

        // TODO: Do this need to be public? Probably move packing to here so this can still stay hidden to other pipelines.
        public Vector3[] neighbourColour;
        public Vector3[] neighbourNormal;
        public float[] neighbourDistance;
        public int[] requestIndex;
        public float validity;

        // TODO TODO_FCC move to HDRP when moving the baking to HDRP only.
        internal void InitExtraData()
        {
            neighbourColour = new Vector3[ProbeExtraData.s_AxisCount];
            neighbourNormal = new Vector3[ProbeExtraData.s_AxisCount];
            neighbourDistance = new float[ProbeExtraData.s_AxisCount];
            requestIndex = new int[ProbeExtraData.s_AxisCount];
            for (int i = 0; i < ProbeExtraData.s_AxisCount; ++i)
            {
                requestIndex[i] = -1;
            }
        }
    }


    // TODO_FCC: Remove the note.
    // TODO_FCC: Verify if needs to be public. We might want to move stuff around.
    //NOTE: We will have one of this per cell
    public class ProbeExtraDataBuffers
    {
        ComputeBuffer m_ProbeLocationsBuffer;
        public ComputeBuffer probeLocationBuffer { get => m_ProbeLocationsBuffer; }
        
        ComputeBuffer m_IrradianceCache;
        public ComputeBuffer irradianceCache { get => m_IrradianceCache; }

        ComputeBuffer m_PrevIrradianceCache;
        public ComputeBuffer prevIrradianceCache { get => m_PrevIrradianceCache; }

        ComputeBuffer m_FinalExtraDataBuffer;
        public ComputeBuffer finalExtraDataBuffer { get => m_FinalExtraDataBuffer; }

        bool m_ComputeBufferFilled;

        public int hitProbesAxisCount;
        public int missProbesAxisCount;

        public int probeCount = 0;

        public ProbeExtraDataBuffers(ProbeReferenceVolume.Cell cell, int axisCount)
        {
            m_ComputeBufferFilled = false;
            int probeCount = cell.probePositions.Length;

            // 4 uint per axis per probe
            m_FinalExtraDataBuffer = new ComputeBuffer(probeCount * axisCount, sizeof(uint) * 3);

            // 1 float3 per probe
            m_ProbeLocationsBuffer = new ComputeBuffer(probeCount, sizeof(float) * 3);

            // 1 float3 per axis.
            m_IrradianceCache = new ComputeBuffer(probeCount * axisCount, sizeof(float) * 3);
            m_PrevIrradianceCache = new ComputeBuffer(probeCount * axisCount, sizeof(float) * 3);

            hitProbesAxisCount = 0;
            missProbesAxisCount = 0;
        }

 
        public void ClearIrradianceCaches(int probeCount, int axisCount)
        {
            int n = probeCount * 3 * axisCount;
            float[] emptyData = new float[n];
            m_IrradianceCache.SetData(emptyData);
            m_PrevIrradianceCache.SetData(emptyData);
        }

        public void SwapIrradianceCache()
        {
            var tmp = irradianceCache;
            m_IrradianceCache = prevIrradianceCache;
            m_PrevIrradianceCache = tmp;
        }

        public void PopulateComputeBuffer(List<float> probeLocations, List<uint> extraData, int hitCount, int missCount)
        {
            hitProbesAxisCount = hitCount;
            missProbesAxisCount = missCount;
            if (!m_ComputeBufferFilled &&
                probeLocations.Capacity == probeLocations.Count)
            {
                m_ProbeLocationsBuffer.SetData(probeLocations);
                m_FinalExtraDataBuffer.SetData(extraData);
                m_ComputeBufferFilled = true;
                probeCount = probeLocations.Count / 3;
            }
        }

        public void Dispose()
        {
            m_ComputeBufferFilled = false;
            CoreUtils.SafeRelease(m_FinalExtraDataBuffer);
            CoreUtils.SafeRelease(m_ProbeLocationsBuffer);
            CoreUtils.SafeRelease(m_IrradianceCache);
            CoreUtils.SafeRelease(m_PrevIrradianceCache);

            m_ProbeLocationsBuffer = null;
            m_FinalExtraDataBuffer = null;
        }
    }
}
