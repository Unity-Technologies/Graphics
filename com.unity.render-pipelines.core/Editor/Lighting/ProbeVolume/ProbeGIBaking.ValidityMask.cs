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

        static Dictionary<Vector3, Bounds> s_ForceInvalidatedProbesAndTouchupVols = new Dictionary<Vector3, Bounds>();


        static HashSet<Vector3> s_PositionOfForceInvalidatedProbes = new HashSet<Vector3>();
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

        static bool NeighbourhoodIsEmptySpace(Vector3 pos, float distanceSearch, Bounds boundsToCheckAgainst)
        {
            // This *needs* to be optimized.
            // We need to look in positive directions only {1/0,1/0,1/0} - {0,0,0}

            for (int x = 0; x <= 1; ++x)
            {
                for (int y = 0; y <= 1; ++y)
                {
                    for (int z = 0; z <= 1; ++z)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;

                        float rayLen = Mathf.Sqrt(x + y + z) * distanceSearch;
                        Vector3 dir = new Vector3(x, y, z) * distanceSearch;
                        if (HasHits(pos, dir, rayLen, true, boundsToCheckAgainst))
                            return false;
                    }
                }
            }

            return true;
        }


        // TODO: PLAN TO FIX VALIDITY.  -- FOLLOWING IS DONE
        // If invalidated a probe for the sake of restoring overly invalid:
        //  - Look at the one invalidated manually.
        //  - When looping through the location to put in, verify if we are processing a probe that needs extra checks.
        //      - Store in a list the loc {x,y,z} of the one we need to check along with distance, so { {x,y,z}, brickSize }
        //      - During the validity mask loop if we are in a probe that need checking.
        //      - Shoot rays with distance equal to the brick size (distance to neighbour probes), in the 8 directions to neighbours then
        //          * If any hit: keep as is.
        //          * If no hit: Validity maks is 0.


        // I actually can't check only the invalid ones, also the ones containing the invalid one in the neighbourhood.

        //  ---------------- TODO: NOT DONE ---------------
        //      - Check for intersection only with rendering geometry that intersected the touchup volumes.

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

                Dictionary<Vector3Int, (float, Vector3, Bounds)> probesToRestoreInfo = new Dictionary<Vector3Int, (float, Vector3, Bounds)>();

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

                                    // Check if we need to do some extra check on this probe.
                                    Bounds invalidatingTouchupBound;
                                    if (s_ForceInvalidatedProbesAndTouchupVols.TryGetValue(cell.probePositions[shidx], out invalidatingTouchupBound))
                                    {
                                        int actualBrickIdx = brickIdx / ProbeBrickPool.kBrickProbeCountTotal;
                                        float brickSize = ProbeReferenceVolume.CellSize(cell.bricks[actualBrickIdx].subdivisionLevel);
                                        Vector3 position = cell.probePositions[shidx];
                                        probesToRestoreInfo.Add(new Vector3Int(ix, iy, iz), (brickSize, position, invalidatingTouchupBound));
                                    }
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
                                bool forceAllValid = false;
                                for (int o = 0; o < 8; ++o)
                                {
                                    Vector3Int off = GetSampleOffset(o);
                                    Vector3Int samplePos = new Vector3Int(Mathf.Clamp(x + off.x, 0, locSize.x - 1),
                                                                          Mathf.Clamp(y + off.y, 0, locSize.y - 1),
                                                                          Mathf.Clamp(z + off.z, 0, ProbeBrickPool.kBrickProbeCountPerDim - 1));

                                    (float, Vector3, Bounds) invalidatedProbeInfo;
                                    if (probesToRestoreInfo.TryGetValue(samplePos, out invalidatedProbeInfo))
                                    {
                                        float distBetweenProbes = invalidatedProbeInfo.Item1;
                                        Vector3 positionToTest = invalidatedProbeInfo.Item2 - new Vector3(off.x, off.y, off.z) * distBetweenProbes;

                                        if (NeighbourhoodIsEmptySpace(positionToTest, distBetweenProbes, invalidatedProbeInfo.Item3))
                                        {
                                            forceAllValid = true;
                                        }
                                    }


                                    validities[o] = ReadValidity(samplePos.x, samplePos.y, samplePos.z, locSize.x, locSize.y);
                                }

                                cell.neighbValidityMask[outIdx] = forceAllValid ? (byte)255 : Convert.ToByte(PackValidity(validities));
                            }
                        }
                    }
                }

                chunkIndex += ProbeReferenceVolume.kTemporaryDataLocChunkCount;

            }
        }

        private static bool HasMeshColliderHits(RaycastHit[] outBoundHits, RaycastHit[] inBoundHits, Vector3 outRay, Vector3 inRay, float rayEnd, bool checkNormal, Bounds validHitBounds, out float distance)
        {
            distance = float.MaxValue;
            bool hasHit = false;

            bool considerBounds = validHitBounds != new Bounds();

            foreach (var hit in outBoundHits)
            {
                if (hit.collider is MeshCollider && (!checkNormal || Vector3.Dot(outRay, hit.normal) > 0))
                {
                    if (considerBounds && !hit.collider.bounds.Intersects(validHitBounds)) continue;

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
                    if (considerBounds && !hit.collider.bounds.Intersects(validHitBounds)) continue;

                    if ((rayEnd - hit.distance) < distance)
                    {
                        distance = hit.distance;
                        hasHit = true;
                    }
                }
            }

            return hasHit;
        }


        // Important! searchDir must be not-normalized.
        static bool HasHits(Vector3 rayOrigin, Vector3 searchDir, float distanceSearch, bool hitBackFaces = false, Bounds validHitBounds = new Bounds())
        {
            bool queriesHitBackBefore = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = hitBackFaces;

            Vector3 normDir = searchDir.normalized;
            var collisionLayerMask = ~0;
            RaycastHit[] outBoundHits = Physics.RaycastAll(rayOrigin, normDir, distanceSearch, collisionLayerMask);
            RaycastHit[] inBoundHits = Physics.RaycastAll(rayOrigin + searchDir, -1.0f * normDir, distanceSearch, collisionLayerMask);

            float distanceForDir = 0;
            bool hasMeshColliderHits = HasMeshColliderHits(outBoundHits, inBoundHits, normDir, -normDir, distanceSearch, false, validHitBounds, out distanceForDir);

            Physics.queriesHitBackfaces = queriesHitBackBefore;

            return hasMeshColliderHits;
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
                        Vector3 ray = searchDir.normalized * distanceSearch;

                        if (HasHits(worldPosition, ray, distanceSearch, Physics.queriesHitBackfaces))
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
