using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeDynamicGIManager
    {

        private static ProbeDynamicGIManager s_Instance = new ProbeDynamicGIManager();

        internal static ProbeDynamicGIManager instance { get { return s_Instance; } }


        #region ExtraData Definition

        // The distance to check occlusion for.
        // TODO: Needs to be exposed.

        static internal void InitExtraData(ref ProbeExtraData extraData)
        {
            extraData.neighbourColour = new Vector3[ProbeExtraData.s_AxisCount];
            extraData.neighbourNormal = new Vector3[ProbeExtraData.s_AxisCount];
            extraData.neighbourDistance = new float[ProbeExtraData.s_AxisCount];
            extraData.requestIndex = new int[ProbeExtraData.s_AxisCount];
            for (int i = 0; i < ProbeExtraData.s_AxisCount; ++i)
            {
                extraData.requestIndex[i] = -1;
            }
        }


        #endregion

        #region PopulatingBuffer
        private uint PackAlbedo(Vector3 color, float distance)
        {
            float albedoR = Mathf.Clamp01(color.x);
            float albedoG = Mathf.Clamp01(color.y);
            float albedoB = Mathf.Clamp01(color.z);

            float normalizedDistance = Mathf.Clamp01(distance / (ProbeReferenceVolume.instance.MinDistanceBetweenProbes() * Mathf.Sqrt(3.0f)));

            uint packedOutput = 0;

            packedOutput |= ((uint)(albedoR * 255.5f) << 0);
            packedOutput |= ((uint)(albedoG * 255.5f) << 8);
            packedOutput |= ((uint)(albedoB * 255.5f) << 16);
            packedOutput |= ((uint)(normalizedDistance * 255.0f) << 24);

            return packedOutput;
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

        private struct FinalDataPacked
        {
            internal uint packedIndices;
            internal uint packedAlbedo;
            internal uint packedNormal;

            internal Vector3 position;
        };

        // We 1 albedo, 1 normal and 1 distance per axis in ProbeExtraData.NeighbourAxis.
        // These informations are packed as follow in a uint2:
        //  UINT32:  8bit: AlbedoR, 8bit: AlbedoG, 8bit: AlbedoB, 8bit: normalized t along ray (the length of the ray is constant so we can derive the actual distance after reading data.
        //  UINT32:  8-8-8: Normal encoded with PackNormalOctQuadEncode like HDRP's normal encoding, 8bit: UNUSED
        private struct PackedExtraData
        {
            public uint[] packedAlbedo;
            public uint[] packedNormal;
        }

        private PackedExtraData PackProbeExtraData(ProbeExtraData probeExtraData)
        {
            PackedExtraData packedData;
            packedData.packedAlbedo = new uint[ProbeExtraData.s_AxisCount];
            packedData.packedNormal = new uint[ProbeExtraData.s_AxisCount];

            for (int i = 0; i < ProbeExtraData.s_AxisCount; ++i)
            {
                bool miss = probeExtraData.neighbourDistance[i] >= ProbeReferenceVolume.instance.MinDistanceBetweenProbes() * Mathf.Sqrt(3.0f) || probeExtraData.neighbourDistance[i] < 0.005f;

                packedData.packedAlbedo[i] = PackAlbedo(probeExtraData.neighbourColour[i], miss ? 0.0f : probeExtraData.neighbourDistance[i]);
                packedData.packedNormal[i] = PackNormalAndAxis(probeExtraData.neighbourNormal[i], i);
            }

            return packedData;
        }

        private void PopulateExtraDataBuffer(ProbeReferenceVolume.Cell cell)
        {
            var probeCount = cell.probePositions.Length;

            List<FinalDataPacked> hitIndices = new List<FinalDataPacked>(probeCount * ProbeExtraData.s_AxisCount);
            List<FinalDataPacked> missIndices = new List<FinalDataPacked>(probeCount * ProbeExtraData.s_AxisCount);

            var finalExtraData = new List<uint>(probeCount * ProbeExtraData.s_AxisCount * 3);

            var probeLocations = new List<float>(probeCount * 3);

            for (int i = 0; i < probeCount; ++i)
            {
                int probeIndex = probeLocations.Count / 3;
                var probeLocation = cell.probePositions[i];

                probeLocations.Add(probeLocation.x);
                probeLocations.Add(probeLocation.y);
                probeLocations.Add(probeLocation.z);

                var probeExtraData = cell.extraData[i];
                PackedExtraData extraDataPacked = PackProbeExtraData(probeExtraData);

                for (int axis = 0; axis < ProbeExtraData.s_AxisCount; ++axis)
                {
                    bool miss = probeExtraData.neighbourDistance[axis] >= (ProbeReferenceVolume.instance.MinDistanceBetweenProbes() * Mathf.Sqrt(3.0f)) || probeExtraData.neighbourDistance[axis] == 0.0f;

                    FinalDataPacked index;
                    index.packedIndices = PackIndexAndValidity((uint)probeIndex, (uint)axis, probeExtraData.validity);
                    index.packedAlbedo = extraDataPacked.packedAlbedo[axis];
                    index.packedNormal = extraDataPacked.packedNormal[axis];
                    index.position = probeLocation;

                    if (miss)
                    {
                        missIndices.Add(index);
                    }
                    else
                    {
                        hitIndices.Add(index);
                    }
                }
            }

            var refVol = ProbeReferenceVolume.instance;

            for (int i = 0; i < hitIndices.Count; ++i)
            {
                FinalDataPacked index = hitIndices[i];
                finalExtraData.Add(index.packedAlbedo);
                finalExtraData.Add(index.packedNormal);
                finalExtraData.Add(index.packedIndices);
            }

            int hitProbesAxisCount = hitIndices.Count;

            for (int i = 0; i < missIndices.Count; ++i)
            {
                FinalDataPacked index = missIndices[i];
                finalExtraData.Add(index.packedAlbedo);
                finalExtraData.Add(index.packedNormal);
                finalExtraData.Add(index.packedIndices);
            }

            int missProbesAxisCount = missIndices.Count;

            cell.probeExtraDataBuffers.PopulateComputeBuffer(probeLocations, finalExtraData, hitProbesAxisCount, missProbesAxisCount);
            cell.probeExtraDataBuffers.ClearIrradianceCaches(probeCount, ProbeExtraData.s_AxisCount);

        }

        internal void InitExtraDataBuffers(List<ProbeReferenceVolume.Cell> cells)
        {
            foreach (var cell in cells)
            {
                if (cell.probeExtraDataBuffers != null)
                {
                    cell.probeExtraDataBuffers.Dispose();
                }

                cell.probeExtraDataBuffers = new ProbeExtraDataBuffers(cell, ProbeExtraData.s_AxisCount);

                PopulateExtraDataBuffer(cell);
                cell.extraDataBufferInit = true;

            }
        }
        #endregion
    }
}
