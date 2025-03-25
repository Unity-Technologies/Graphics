using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering
{
    partial class AdaptiveProbeVolumes
    {
        // We use this scratch memory as a way of spoofing the texture.
        static DynamicArray<(float, byte)> s_ValidityLayer_locData = new DynamicArray<(float, byte)>();
        static DynamicArray<int> s_ProbeIndices = new DynamicArray<int>();

        internal static Vector3Int GetSampleOffset(int i)
        {
            return new Vector3Int(i & 1, (i >> 1) & 1, (i >> 2) & 1);
        }

        const float k_MinValidityForLeaking = APVDefinitions.probeValidityThreshold;

        internal static uint PackValidity(float[] validity)
        {
            uint outputByte = 0;
            for (int i = 0; i < 8; ++i)
            {
                uint val = (validity[i] > k_MinValidityForLeaking) ? 0u : 1u;
                outputByte |= (val << i);
            }
            return outputByte;
        }

        internal static uint PackLayer(byte[] layers, int layer)
        {
            uint outputLayer = 0;
            for (int i = 0; i < 8; ++i)
            {
                if ((layers[i] & (byte)(1 << layer)) != 0)
                    outputLayer |= (1u << i);
            }
            return outputLayer;
        }

        static void StoreScratchData(int x, int y, int z, int dataWidth, int dataHeight, float value, byte layer, int probeIndex)
        {
            int index = x + dataWidth * (y + dataHeight * z);
            s_ValidityLayer_locData[index] = (value, layer);
            s_ProbeIndices[index] = probeIndex;
        }

        static (float, byte) ReadValidity(int x, int y, int z, int dataWidth, int dataHeight)
        {
            int index = x + dataWidth * (y + dataHeight * z);
            return s_ValidityLayer_locData[index];
        }

        static int ReadProbeIndex(int x, int y, int z, int dataWidth, int dataHeight)
        {
            int index = x + dataWidth * (y + dataHeight * z);
            return s_ProbeIndices[index];
        }

        // TODO: This whole process will need optimization.
        static bool NeighbourhoodIsEmptySpace(Vector3 pos, float searchDistance, Bounds boundsToCheckAgainst)
        {
            Vector3 halfExtents = 0.5f * searchDistance * Vector3.one;
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
        static void ComputeValidityMasks(in BakingCell cell)
        {
            var bricks = cell.bricks;
            int chunkSize = ProbeBrickPool.GetChunkSizeInBrickCount();
            int brickChunksCount = (bricks.Length + chunkSize - 1) / chunkSize;
            int validityLayerCount = cell.layerValidity != null ? cell.validityNeighbourMask.GetLength(0) : 1;

            var probeHasEmptySpaceInGrid = new NativeArray<bool>(cell.probePositions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int shidx = 0;
            for (int chunkIndex = 0; chunkIndex < brickChunksCount; ++chunkIndex)
            {
                Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(ProbeBrickPool.GetChunkSizeInProbeCount());
                int size = locSize.x * locSize.y * locSize.z;
                int count = ProbeBrickPool.GetChunkSizeInProbeCount();
                int bx = 0, by = 0, bz = 0;

                s_ValidityLayer_locData.Resize(size);
                s_ProbeIndices.Resize(size);

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
                                    StoreScratchData(ix, iy, iz, locSize.x, locSize.y, 1.0f, 0, shidx);
                                }
                                else
                                {
                                    byte layer = validityLayerCount > 1 ? cell.layerValidity[shidx] : (byte)0xFF;
                                    StoreScratchData(ix, iy, iz, locSize.x, locSize.y, cell.validity[shidx], layer, shidx);

                                    // Check if we need to do some extra check on this probe.
                                    bool hasFreeNeighbourhood = false;
                                    Bounds invalidatingTouchupBound;
                                    if (m_BakingBatch.forceInvalidatedProbesAndTouchupVols.TryGetValue(cell.probePositions[shidx], out invalidatingTouchupBound))
                                    {
                                        int actualBrickIdx = brickIdx / ProbeBrickPool.kBrickProbeCountTotal;
                                        float brickSize = ProbeReferenceVolume.CellSize(cell.bricks[actualBrickIdx].subdivisionLevel);
                                        Vector3 position = cell.probePositions[shidx];
                                        probesToRestore.Add(new Vector3Int(ix, iy, iz));
                                        var searchDistance = (brickSize * m_ProfileInfo.minBrickSize) / ProbeBrickPool.kBrickCellCount;
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

                            if (outIdx < cell.validity.Length)
                            {
                                float[] validities = new float[8];
                                byte[] layers = new byte[8];
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

                                    (validities[o], layers[o]) = ReadValidity(samplePos.x, samplePos.y, samplePos.z, locSize.x, locSize.y);
                                }

                                // Keeping for safety but i think this is useless
                                (float probeValidity, uint _) = ReadValidity(x, y, z, locSize.x, locSize.y);
                                cell.validity[outIdx] = probeValidity;

                                // Pack validity with layer mask
                                uint mask = forceAllValid ? 255 : PackValidity(validities);
                                for (int l = 0; l < validityLayerCount; l++)
                                {
                                    uint layer = validityLayerCount == 1 ? 0xFF : PackLayer(layers, l);
                                    cell.validityNeighbourMask[l, outIdx] = Convert.ToByte(mask & layer);
                                }
                            }
                        }
                    }
                }
            }

            probeHasEmptySpaceInGrid.Dispose();
        }
    }
}
