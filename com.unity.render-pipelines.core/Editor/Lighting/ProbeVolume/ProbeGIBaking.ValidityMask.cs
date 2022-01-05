using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    partial class ProbeGIBaking
    {

        // We use this scratch memory as a way of spoofing the texture.
        static DynamicArray<float> s_Validity_locData = new DynamicArray<float>();
        static DynamicArray<int> s_ProbeIndices = new DynamicArray<int>();


        internal static Vector3Int GetSampleOffset(int i)
        {
            return new Vector3Int(i & 1, (i >> 1) & 1, (i >> 2) & 1);
        }

        const float k_MinValidityForLeaking = 0.05f;

        internal static int PackValidity(float[] validity)
        {
            int outputByte = 0;
            for (int i = 0; i < 8; ++i)
            {
                int val = (validity[i] > k_MinValidityForLeaking) ? 0 : 1;
                outputByte |= (val << i);
            }
            return outputByte;
        }

        static void StoreScratchData(int x, int y, int z, int dataWidth, int dataHeight, float value, int probeIndex)
        {
            int index = x + dataWidth * (y + dataHeight * z);
            s_Validity_locData[index] = value;
            s_ProbeIndices[index] = probeIndex;
        }

        static float ReadValidity(int x, int y, int z, int dataWidth, int dataHeight)
        {
            int index = x + dataWidth * (y + dataHeight * z);
            return s_Validity_locData[index];
        }

        static int ReadProbeIndex(int x, int y, int z, int dataWidth, int dataHeight)
        {
            int index = x + dataWidth * (y + dataHeight * z);
            return s_ProbeIndices[index];
        }

        // This is very much modeled  to be as close as possible to the way bricks are loaded in the texture pool.
        // It is likely not the best way to go about it.
        static void ComputeValidityMask2(ProbeReferenceVolume.Cell cell)
        {
            var bricks = cell.bricks;

            int chunkSize = ProbeBrickPool.GetChunkSize();
            int brickChunksCount = (bricks.Count + chunkSize - 1) / chunkSize;

            int chunkIndex = 0;
            while (chunkIndex < brickChunksCount)
            {
                int chunkToProcess = Math.Min(ProbeReferenceVolume.kTemporaryDataLocChunkCount, brickChunksCount - chunkIndex);

                Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(ProbeReferenceVolume.kTemporaryDataLocChunkCount * ProbeBrickPool.GetChunkSizeInProbeCount());
                int size = locSize.x * locSize.y * locSize.z;
                int startIndex = chunkIndex * ProbeBrickPool.GetChunkSizeInProbeCount();
                int count = chunkToProcess * ProbeBrickPool.GetChunkSizeInProbeCount();
                int shidx = startIndex;
                int bx = 0, by = 0, bz = 0;

                s_Validity_locData.Resize(size);
                s_ProbeIndices.Resize(size);

                for (int brickIdx = startIndex; brickIdx < (startIndex + count); brickIdx += ProbeBrickPool.kBrickProbeCountTotal)
                {
                    for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                    {
                        for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                        {
                            for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                            {
                                int ix = bx + x;
                                int iy = by + y;
                                int iz = bz + z;

                                // We are processing chunks at a time.
                                // So in practice we can go over the number of SH we have in the input list.
                                // We fill with black to avoid copying garbage in the final atlas.
                                if (shidx >= cell.validity.Length)
                                {
                                    StoreScratchData(ix, iy, iz, locSize.x, locSize.y, 1.0f, shidx);
                                }
                                else
                                {
                                    StoreScratchData(ix, iy, iz, locSize.x, locSize.y, cell.validity[shidx], shidx);
                                }
                                shidx++;
                            }
                        }
                    }
                    // update the pool index
                    bx += ProbeBrickPool.kBrickProbeCountPerDim;
                    if (bx >= locSize.x)
                    {
                        bx = 0;
                        by += ProbeBrickPool.kBrickProbeCountPerDim;
                        if (by >= locSize.y)
                        {
                            by = 0;
                            bz += ProbeBrickPool.kBrickProbeCountPerDim;
                        }
                    }
                }

                // This can be optimized later.
                for (int x = 0; x < locSize.x; ++x)
                {
                    for (int y = 0; y < locSize.y; ++y)
                    {
                        for (int z = 0; z < locSize.z; ++z)
                        {
                            int outIdx = ReadProbeIndex(x, y, z, locSize.x, locSize.y);

                            if (outIdx < cell.validity.Length)
                            {
                                float[] validities = new float[8];
                                for (int o = 0; o < 8; ++o)
                                {
                                    Vector3Int off = GetSampleOffset(o);
                                    Vector3Int samplePos = new Vector3Int(Mathf.Clamp(x + off.x, 0, locSize.x - 1),
                                                                          Mathf.Clamp(y + off.y, 0, locSize.y - 1),
                                                                          Mathf.Clamp(z + off.z, 0, ProbeBrickPool.kBrickProbeCountPerDim - 1));

                                    validities[o] = ReadValidity(samplePos.x, samplePos.y, samplePos.z, locSize.x, locSize.y);
                                }

                                cell.neighbValidityMask[outIdx] = PackValidity(validities);
                            }
                        }
                    }
                }

                chunkIndex += ProbeReferenceVolume.kTemporaryDataLocChunkCount;

            }
        }

        private static bool HasMeshColliderHits(RaycastHit[] outBoundHits, RaycastHit[] inBoundHits, Vector3 outRay, Vector3 inRay, float rayEnd, bool checkNormal, out float distance)
        {
            distance = float.MaxValue;
            bool hasHit = false;

            foreach (var hit in outBoundHits)
            {
                if (hit.collider is MeshCollider && (!checkNormal || Vector3.Dot(outRay, hit.normal) > 0))
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        hasHit = true;
                    }
                }
            }

            foreach (var hit in inBoundHits)
            {
                if (hit.collider is MeshCollider && (!checkNormal || Vector3.Dot(outRay, hit.normal) > 0))
                {
                    if ((rayEnd - hit.distance) < distance)
                    {
                        distance = hit.distance;
                        hasHit = true;
                    }
                }
            }

            return hasHit;
        }

        private static bool HasColliderAround(Vector3 worldPosition, float distanceSearch)
        {
            int hitsFound = 0;
            const int necessaryHits = 1;

            for (int x = -1; x <= 1; ++x)
            {
                for (int y = -1; y <= 1; ++y)
                {
                    for (int z = -1; z <= 1; ++z)
                    {
                        Vector3 searchDir = new Vector3(x, y, z);
                        Vector3 normDir = searchDir.normalized;
                        Vector3 ray = normDir * distanceSearch;
                        var collisionLayerMask = ~0;
                        RaycastHit[] outBoundHits = Physics.RaycastAll(worldPosition, normDir, distanceSearch, collisionLayerMask);
                        RaycastHit[] inBoundHits = Physics.RaycastAll(worldPosition + ray, -1.0f * normDir, distanceSearch, collisionLayerMask);

                        float distanceForDir = 0;
                        if (HasMeshColliderHits(outBoundHits, inBoundHits, normDir, -normDir, distanceSearch, false, out distanceForDir))
                        {
                            hitsFound++;
                            if (hitsFound > necessaryHits) return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
