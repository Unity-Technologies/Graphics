using System.Collections.Generic;
using UnityEditor;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using System;

namespace UnityEngine.Rendering
{
    partial class ProbeGIBaking
    {
        // We use this scratch memory as a way of spoofing the texture.
        static DynamicArray<float> s_Validity_locData = new DynamicArray<float>();
        static DynamicArray<int> s_ProbeIndices = new DynamicArray<int>();

        static Dictionary<Vector3, Bounds> s_ForceInvalidatedProbesAndTouchupVols = new Dictionary<Vector3, Bounds>();

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


        // TODO: This whole process will need optimization.
        static bool NeighbourhoodIsEmptySpace(Vector3 pos, float searchDistance, Bounds boundsToCheckAgainst)
        {

            Vector3 halfExtents = Vector3.one * searchDistance * 0.5f;
            Vector3 brickCenter = pos + halfExtents;

            Collider[] colliders = Physics.OverlapBox(brickCenter, halfExtents);

            if (colliders.Length > 0) return false;

            // TO_VERIFY: Shall we do this check?
            //foreach (var collider in colliders)
            //{
            //    if (collider.bounds.Intersects(boundsToCheckAgainst))
            //        return false;
            //}

            return true;
        }


        // This is very much modeled  to be as close as possible to the way bricks are loaded in the texture pool.
        // Not necessarily a good thing.
        static void ComputeValidityMasks(BakingCell bakingCell)
        {
            var bricks = bakingCell.bricks;
            var cell = bakingCell;
            int chunkSize = ProbeBrickPool.GetChunkSizeInBrickCount();
            int brickChunksCount = (bricks.Length + chunkSize - 1) / chunkSize;

            var probeHasEmptySpaceInGrid = new NativeArray<bool>(bakingCell.probePositions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int shidx = 0;
            for (int chunkIndex = 0; chunkIndex < brickChunksCount; ++chunkIndex)
            {
                Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(ProbeBrickPool.GetChunkSizeInProbeCount());
                int size = locSize.x * locSize.y * locSize.z;
                int count = ProbeBrickPool.GetChunkSizeInProbeCount();
                int bx = 0, by = 0, bz = 0;

                s_Validity_locData.Resize(size);
                s_ProbeIndices.Resize(size);

                Dictionary<Vector3Int, (float, Vector3, Bounds)> probesToRestoreInfo = new Dictionary<Vector3Int, (float, Vector3, Bounds)>();
                HashSet<Vector3Int> probesToRestore = new HashSet<Vector3Int>();

                for (int brickIdx = 0; brickIdx < count; brickIdx += ProbeBrickPool.kBrickProbeCountTotal)
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

                                if (shidx >= cell.validity.Length)
                                {
                                    StoreScratchData(ix, iy, iz, locSize.x, locSize.y, 1.0f, shidx);
                                }
                                else
                                {
                                    StoreScratchData(ix, iy, iz, locSize.x, locSize.y, cell.validity[shidx], shidx);

                                    // Check if we need to do some extra check on this probe.
                                    bool hasFreeNeighbourhood = false;
                                    Bounds invalidatingTouchupBound;
                                    if (s_ForceInvalidatedProbesAndTouchupVols.TryGetValue(cell.probePositions[shidx], out invalidatingTouchupBound))
                                    {
                                        int actualBrickIdx = brickIdx / ProbeBrickPool.kBrickProbeCountTotal;
                                        float brickSize = ProbeReferenceVolume.CellSize(cell.bricks[actualBrickIdx].subdivisionLevel);
                                        Vector3 position = cell.probePositions[shidx];
                                        probesToRestore.Add(new Vector3Int(ix, iy, iz));
                                        var searchDistance = (brickSize * m_BakingProfile.minBrickSize) / ProbeBrickPool.kBrickCellCount;
                                        hasFreeNeighbourhood = NeighbourhoodIsEmptySpace(position, searchDistance, invalidatingTouchupBound);
                                    }
                                    probeHasEmptySpaceInGrid[shidx] = hasFreeNeighbourhood;
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

                for (int x = 0; x < locSize.x; ++x)
                {
                    for (int y = 0; y < locSize.y; ++y)
                    {
                        for (int z = 0; z < locSize.z; ++z)
                        {
                            int outIdx = ReadProbeIndex(x, y, z, locSize.x, locSize.y);
                            float probeValidity = ReadValidity(x, y, z, locSize.x, locSize.y);

                            if (outIdx < cell.validity.Length)
                            {
                                float[] validities = new float[8];
                                bool forceAllValid = false;
                                for (int o = 0; o < 8; ++o)
                                {
                                    Vector3Int off = GetSampleOffset(o);
                                    Vector3Int samplePos = new Vector3Int(Mathf.Clamp(x + off.x, 0, locSize.x - 1),
                                                                          Mathf.Clamp(y + off.y, 0, locSize.y - 1),
                                                                          Mathf.Clamp(z + off.z, 0, ProbeBrickPool.kBrickProbeCountPerDim - 1));

                                    if (probesToRestore.Contains(samplePos))
                                    {
                                        if (probeHasEmptySpaceInGrid[outIdx])
                                        {
                                            forceAllValid = true;
                                        }
                                    }

                                    validities[o] = ReadValidity(samplePos.x, samplePos.y, samplePos.z, locSize.x, locSize.y);
                                }

                                byte mask = forceAllValid ? (byte)255 : Convert.ToByte(PackValidity(validities));
                                float validity = probeValidity;

                                cell.validity[outIdx] = validity;
                                cell.validityNeighbourMask[outIdx] = mask;
                            }
                        }
                    }
                }
            }

            probeHasEmptySpaceInGrid.Dispose();
        }
    }
}
