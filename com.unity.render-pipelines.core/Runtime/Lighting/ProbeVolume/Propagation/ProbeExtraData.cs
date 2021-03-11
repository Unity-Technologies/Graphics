using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    [System.Serializable]
    public partial struct ProbeExtraData
    {
        public static readonly int s_AxisCount = 14;

        internal static readonly float s_DiagonalDist = Mathf.Sqrt(3.0f);
        internal static readonly float s_Diagonal = 1.0f / s_DiagonalDist;


        internal static readonly float s_2DDiagonalDist = Mathf.Sqrt(2.0f);
        internal static readonly float s_2DDiagonal = 1.0f / s_2DDiagonalDist;

        // The distance to check occlusion for.
        // TODO: Needs to be exposed.

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
        public Vector3[] NeighbourColour;
        public Vector3[] NeighbourNormal;
        public float[] NeighbourDistance;
        public bool valid;
        public float validity;

        public void InitExtraData()
        {
            valid = false;
            NeighbourColour = new Vector3[s_AxisCount];
            NeighbourNormal = new Vector3[s_AxisCount];
            NeighbourDistance = new float[s_AxisCount];
            for (int i = 0; i < s_AxisCount; ++i)
            {
                NeighbourColour[i] = new Vector3(0.0f, 0.0f, 0.0f);
                NeighbourNormal[i] = new Vector3(0.0f, 0.0f, 0.0f);
                NeighbourDistance[i] = 0.0f;
            }
        }
    }


    // TODO_FCC: Remove the note.
    // TODO_FCC: Verify if needs to be public. We might want to move stuff around.
    //NOTE: We will have one of this per cell
    public struct ProbeExtraDataBuffers
    {
        // We 1 albedo, 1 normal and 1 distance per axis in ProbeExtraData.NeighbourAxis.
        // These informations are packed as follow in a uint2:
        //  UINT32:  8bit: AlbedoR, 8bit: AlbedoG, 8bit: AlbedoB, 8bit: normalized t along ray (the length of the ray is constant so we can derive the actual distance after reading data.
        //  UINT32:  8-8-8: Normal encoded with PackNormalOctQuadEncode like HDRP's normal encoding, 8bit: UNUSED
        struct PackedExtraData
        {
            public uint[] packedAlbedo;
            public uint[] packedNormal;
        }

        // Data as it will be laid out in the compute buffers.
        List<uint> m_FinalExtraData;

        List<float> m_ProbeLocations;

        ComputeBuffer m_ProbeLocationsBuffer;
        public ComputeBuffer probeLocationBuffer { get => m_ProbeLocationsBuffer; }

        ComputeBuffer m_IrradianceCache;
        public ComputeBuffer irradianceCache { get => m_IrradianceCache; }

        ComputeBuffer m_PrevIrradianceCache;
        public ComputeBuffer prevIrradianceCache { get => m_PrevIrradianceCache; }

        ComputeBuffer m_FinalExtraDataBuffer;
        public ComputeBuffer finalExtraDataBuffer { get => m_FinalExtraDataBuffer; }


        bool m_ComputeBufferFilled;

        public int probeCount { get => (m_ProbeLocations.Capacity / 3); }


        public int hitProbesAxisCount;
        public int missProbesAxisCount;

        struct FinalDataPacked
        {
            public uint packedIndices;
            public uint packedAlbedo;
            public uint packedNormal;
        }
        List<FinalDataPacked> m_HitIndices;
        List<FinalDataPacked> m_MissIndices;

        internal ProbeExtraDataBuffers(ProbeReferenceVolume.Cell cell)
        {
            m_ComputeBufferFilled = false;
            int probeCount = cell.probePositions.Length;


            int finalExtraDataSize = probeCount * ProbeExtraData.s_AxisCount * 3;
            m_FinalExtraData = new List<uint>(probeCount * ProbeExtraData.s_AxisCount * 3);

            m_ProbeLocations = new List<float>(probeCount * 3);

            m_HitIndices = new List<FinalDataPacked>();
            m_MissIndices = new List<FinalDataPacked>();

            // 4 uint per axis per probe
            m_FinalExtraDataBuffer = new ComputeBuffer(probeCount * ProbeExtraData.s_AxisCount, sizeof(uint) * 3);

            // 1 float3 per probe
            m_ProbeLocationsBuffer = new ComputeBuffer(probeCount, sizeof(float) * 3);

            // 1 float3 per axis.
            m_IrradianceCache = new ComputeBuffer(probeCount *  ProbeExtraData.s_AxisCount, sizeof(float) * 3);
            m_PrevIrradianceCache = new ComputeBuffer(probeCount * ProbeExtraData.s_AxisCount, sizeof(float) * 3);

            hitProbesAxisCount = 0;
            missProbesAxisCount = 0;
        }

        private uint PackAlbedo(Vector3 color, float distance)
        {
            float albedoR = Mathf.Clamp01(color.x);
            float albedoG = Mathf.Clamp01(color.y);
            float albedoB = Mathf.Clamp01(color.z);

            float normalizedDistance = Mathf.Clamp01(distance / ProbeReferenceVolume.instance.MinDistanceBetweenProbes());

            uint packedOutput = 0;

            packedOutput |= ((uint)(albedoR * 255.5f) << 0);
            packedOutput |= ((uint)(albedoG * 255.5f) << 8);
            packedOutput |= ((uint)(albedoB * 255.5f) << 16);
            packedOutput |= ((uint)(normalizedDistance * 255.0f) << 24);

            return packedOutput;
        }

        private uint PackAxisDir(Vector4 axis)
        {
            uint axisType = (axis.w == 1.0f) ? 0u : 1u;

            uint encodedX = axis.x < 0 ? 0u :
                axis.x == 0 ? 1u :
                2u;

            uint encodedY = axis.y < 0 ? 0u :
                axis.y == 0 ? 1u :
                2u;

            uint encodedZ = axis.z < 0 ? 0u :
                axis.z == 0 ? 1u :
                2u;

            uint output = 0;
            // Encode type of axis in bit 7
            output |= (axisType << 6);
            // Encode axis signs in [5:6] [3:4] [1:2]
            output |= (encodedZ << 4);
            output |= (encodedY << 2);
            output |= (encodedX << 0);

            return output;
        }

        // Same as PackNormalOctQuadEncode and PackFloat2To888 in Packing.hlsl
        private uint PackNormalAndAxis(Vector3 N, int axisIndex)
        {
            uint packedOutput = 0;
            float L1Norm = Mathf.Abs(N.x) + Mathf.Abs(N.y) + Mathf.Abs(N.z);
            N /= L1Norm;
            float t = Mathf.Clamp01(-N.z);

            Vector2 p = new Vector2(N.x + (N.x >= 0.0f ? t : -t),
                N.y + (N.y >= 0.0f ? t : -t));
            p *= 0.5f;
            p.x += 0.5f;
            p.y += 0.5f;


            uint i0 = (uint)(p.x * 4095.5f); uint i1 = (uint)(p.y * 4095.5f);
            uint hi0 = i0 >> 8; uint hi1 = i1 >> 8;
            uint lo0 = hi0 & 255; uint lo1 = hi1 & 255;

            packedOutput |= (lo0 << 0);
            packedOutput |= (lo1 << 8);
            packedOutput |= ((hi0 | (hi1 << 4)) << 16);

            packedOutput |= (PackAxisDir(ProbeExtraData.NeighbourAxis[axisIndex]) << 24);

            return packedOutput;
        }

        private PackedExtraData PackProbeExtraData(ProbeExtraData probeExtraData)
        {
            PackedExtraData packedData;
            packedData.packedAlbedo = new uint[ProbeExtraData.s_AxisCount];
            packedData.packedNormal = new uint[ProbeExtraData.s_AxisCount];

            for (int i = 0; i < ProbeExtraData.s_AxisCount; ++i)
            {
                bool miss = probeExtraData.NeighbourDistance[i] >= ProbeReferenceVolume.instance.MinDistanceBetweenProbes() || probeExtraData.NeighbourDistance[i] < 0.005f;

                packedData.packedAlbedo[i] = PackAlbedo(probeExtraData.NeighbourColour[i], miss ? 0.0f : probeExtraData.NeighbourDistance[i]);
                packedData.packedNormal[i] = PackNormalAndAxis(probeExtraData.NeighbourNormal[i], i);
            }

            return packedData;
        }

        // { probeIndex: 19 bits, validity: 8bit, axis: 5bit }
        private uint PackIndexAndValidity(uint probeIndex, uint axisIndex, float validity)
        {
            uint output = 0;

            output |= axisIndex;
            output |= ((uint)(validity * 255.5f) << 5);
            output |= (probeIndex << 13);

            return output;
        }

        public void AddProbeExtraData(ProbeExtraData probeExtraData, Vector3 probeLocation)
        {
            if (m_ProbeLocations.Capacity == m_ProbeLocations.Count) return;

            // Generate the Packed data
            PackedExtraData extraDataPacked = PackProbeExtraData(probeExtraData);

            int probeIndex = m_ProbeLocations.Count / 3;

            // Lay in memory
            m_ProbeLocations.Add(probeLocation.x);
            m_ProbeLocations.Add(probeLocation.y);
            m_ProbeLocations.Add(probeLocation.z);


            for (int i = 0; i < ProbeExtraData.s_AxisCount; ++i)
            {
                bool miss = probeExtraData.NeighbourDistance[i] >= (ProbeReferenceVolume.instance.MinDistanceBetweenProbes()) || probeExtraData.NeighbourDistance[i] == 0.0f;

                FinalDataPacked index;
                index.packedIndices = PackIndexAndValidity((uint)probeIndex, (uint)i, probeExtraData.validity);
                index.packedAlbedo = extraDataPacked.packedAlbedo[i];
                index.packedNormal = extraDataPacked.packedNormal[i];

                if (miss)
                {
                    m_MissIndices.Add(index);
                }
                else
                {
                    m_HitIndices.Add(index);
                }
            }
        }

        public void ClearIrradianceCaches()
        {
            int n = probeCount * 3 * ProbeExtraData.s_AxisCount;
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

        public void PopulateComputeBuffer()
        {
            if (!m_ComputeBufferFilled &&
                m_ProbeLocations.Capacity == m_ProbeLocations.Count)
            {
                m_ProbeLocationsBuffer.SetData(m_ProbeLocations);
                m_FinalExtraDataBuffer.SetData(m_FinalExtraData);
                m_ComputeBufferFilled = true;
            }
        }

        public void ProduceShaderConsumableExtraData()
        {
            var refVol = ProbeReferenceVolume.instance;

            for (int i = 0; i < m_HitIndices.Count; ++i)
            {
                FinalDataPacked index = m_HitIndices[i];
                m_FinalExtraData.Add(index.packedAlbedo);
                m_FinalExtraData.Add(index.packedNormal);
                m_FinalExtraData.Add(index.packedIndices);
            }

            hitProbesAxisCount = m_HitIndices.Count;

            for (int i = 0; i < m_MissIndices.Count; ++i)
            {
                FinalDataPacked index = m_MissIndices[i];
                m_FinalExtraData.Add(index.packedAlbedo);
                m_FinalExtraData.Add(index.packedNormal);
                m_FinalExtraData.Add(index.packedIndices);
            }

            missProbesAxisCount = m_MissIndices.Count;
        }

        public void Dispose()
        {
            m_ComputeBufferFilled = false;
            m_ProbeLocations.Clear();
            m_FinalExtraData.Clear();
            CoreUtils.SafeRelease(m_FinalExtraDataBuffer);
            CoreUtils.SafeRelease(m_ProbeLocationsBuffer);
            CoreUtils.SafeRelease(m_IrradianceCache);
            CoreUtils.SafeRelease(m_PrevIrradianceCache);

            m_ProbeLocationsBuffer = null;
            m_FinalExtraDataBuffer = null;
        }
    }
}
