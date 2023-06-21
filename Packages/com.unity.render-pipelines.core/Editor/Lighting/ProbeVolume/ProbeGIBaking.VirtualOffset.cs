#define USE_JOBS
#if HAS_BURST
#define USE_BURST
#endif
//#define VERBOSE

using System.Collections.Generic;
using UnityEditor;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#if USE_BURST
using Unity.Burst;
#endif

namespace UnityEngine.Rendering
{
    partial class ProbeGIBaking
    {
        // limit memory usage to ~200 MB (run multiple batches to keep below limit)
        const int kMaxMemoryUsage = 200 * 1024 * 1024;

        const int kMinCommandsPerJob = 512;
        const int kRayDirectionsPerPosition = 3 * 3 * 3 - 1;

        static List<MeshCollider> s_AddedOccluders;
        static List<Collider> s_ExcludedColliders;
        static List<Rigidbody> s_ExcludedRigidBodies;

        static void ApplyVirtualOffsets(Vector3[] positions, out Vector3[] offsets)
        {
            var cellToVolumes = GetTouchupsPerCell(out bool hasAppliers);

            var voSettings = m_BakingSettings.virtualOffsetSettings;
            if (!voSettings.useVirtualOffset)
            {
                offsets = null;
                return;
            }

            ModifyPhysicsComponentsForBaking();

            var queriesHitBackBefore = Physics.queriesHitBackfaces;
            try
            {
                if (!queriesHitBackBefore)
                    Physics.queriesHitBackfaces = true;

                AddOccluders();
                DoApplyVirtualOffsets(positions, out offsets, voSettings, cellToVolumes);
            }
            finally
            {
                if (!queriesHitBackBefore)
                    Physics.queriesHitBackfaces = false;

                // We need to restore even if we are going to modify again because removing colliders from a volume component might lead to changes
                // in rendering while baking is in process and this is undesirable. If we re-enable now the enabling/disabling all happen in a single frame.
                RestorePhysicsComponentsAfterBaking();
                // We cleanup occluders here in case bake is cancelled by the user. We have to add them again later when the validity masks are processed.
                CleanupOccluders();
            }
        }

        static void GeneratePhysicsComponentToModList()
        {
            s_ExcludedColliders = new List<Collider>();
            s_ExcludedRigidBodies = new List<Rigidbody>();

            // Scene may contain unwanted colliders (like Volumes for example)
            // So we disable any collider not attached to a MeshRenderer before doing the baking. Otherwise it will mess up with virtual offset and validity.
            var colliderObjects = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            foreach (var collider in colliderObjects)
            {
                if (collider.enabled && (!GIContributors.ContributesGI(collider.gameObject) || !collider.TryGetComponent<MeshRenderer>(out var _)))
                    s_ExcludedColliders.Add(collider);
            }

            // Because we need to trigger physics update to update the physics search tree when adding new occluders
            // rigid bodies might end up triggering the simulation, which is something we do not want.  Therefore we force
            // them to be kinematic and therefore blocking the forces for being applied.
            var rigidbodies = Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            foreach (var rigidBody in rigidbodies)
            {
                if (!rigidBody.isKinematic)
                    s_ExcludedRigidBodies.Add(rigidBody);
            }

        }

        static void ModifyPhysicsComponentsForBaking()
        {
            foreach (var collider in s_ExcludedColliders)
                collider.enabled = false;

            foreach (var rigidBody in s_ExcludedRigidBodies)
                rigidBody.isKinematic = true;
        }

        static void RestorePhysicsComponentsAfterBaking()
        {
            foreach (var collider in s_ExcludedColliders)
                collider.enabled = true;

            foreach (var rigidBody in s_ExcludedRigidBodies)
                rigidBody.isKinematic = false;
        }

        static void AddOccluders()
        {
            s_AddedOccluders = new List<MeshCollider>();

            for (int sceneIndex = 0; sceneIndex < SceneManagement.SceneManager.sceneCount; ++sceneIndex)
            {
                SceneManagement.Scene scene = SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;

                GameObject[] gameObjects = scene.GetRootGameObjects();
                foreach (GameObject gameObject in gameObjects)
                {
                    MeshRenderer[] renderComponents = gameObject.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mr in renderComponents)
                    {
                        if (GIContributors.ContributesGI(mr.gameObject) && !mr.gameObject.TryGetComponent<MeshCollider>(out _))
                        {
                            var meshCollider = mr.gameObject.AddComponent<MeshCollider>();
                            meshCollider.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

                            s_AddedOccluders.Add(meshCollider);
                        }
                    }
                }
            }

            var autoSimState = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                // We call this only to update the search tree with the newly added occluders.
                Physics.Simulate(1e-4f);
            }
            finally
            {
                Physics.simulationMode = autoSimState;
            }
        }

        private static void CleanupOccluders()
        {
            s_AddedOccluders?.ForEach(Object.DestroyImmediate);
        }

        struct TouchupsPerCell
        {
            public List<(ProbeTouchupVolume touchup, ProbeReferenceVolume.Volume obb, Vector3 center, Vector3 offset)> appliers;
            public List<(ProbeTouchupVolume touchup, ProbeReferenceVolume.Volume obb, Vector3 center)> overriders;
        }

        static Dictionary<int, TouchupsPerCell> GetTouchupsPerCell(out bool hasAppliers)
        {
            float cellSize = m_BakingProfile.cellSizeInMeters;
            hasAppliers = false;

            Dictionary<int, TouchupsPerCell> cellToVolumes = new();
            foreach (var touchup in Object.FindObjectsByType<ProbeTouchupVolume>(FindObjectsSortMode.InstanceID))
            {
                if (!touchup.isActiveAndEnabled || (touchup.mode != ProbeTouchupVolume.Mode.ApplyVirtualOffset && touchup.mode != ProbeTouchupVolume.Mode.OverrideVirtualOffsetSettings))
                    continue;

                hasAppliers |= touchup.mode == ProbeTouchupVolume.Mode.ApplyVirtualOffset;
                touchup.GetOBBandAABB(out var obb, out var aabb);

                Vector3Int min = Vector3Int.FloorToInt(aabb.min / cellSize);
                Vector3Int max = Vector3Int.FloorToInt(aabb.max / cellSize);

                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            var cell = PosToIndex(new Vector3Int(x, y, z));
                            if (!cellToVolumes.TryGetValue(cell, out var volumes))
                                cellToVolumes[cell] = volumes = new TouchupsPerCell() { appliers = new(), overriders = new() };

                            if (touchup.mode == ProbeTouchupVolume.Mode.ApplyVirtualOffset)
                                volumes.appliers.Add((touchup, obb, touchup.transform.position, touchup.GetVirtualOffset()));
                            else
                                volumes.overriders.Add((touchup, obb, touchup.transform.position));
                        }

                    }
                }
            }

            return cellToVolumes;
        }

        static Vector3[] DoApplyVirtualOffsetsFromAdjustmentVolumes(Vector3[] positions, Dictionary<int, TouchupsPerCell> cellToVolumes)
        {
            float cellSize = m_BakingProfile.cellSizeInMeters;
            var offsets = new Vector3[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                int cellIndex = PosToIndex(Vector3Int.FloorToInt(positions[i] / cellSize));
                if (cellToVolumes.TryGetValue(cellIndex, out var volumes))
                {
                    foreach (var (touchup, obb, center, offset) in volumes.appliers)
                    {
                        if (touchup.ContainsPoint(obb, center, positions[i]))
                        {
                            positions[i] += offset;
                            offsets[i] = offset;
                            break;
                        }
                    }
                }
            }
            return offsets;
        }

        static void DoApplyVirtualOffsets(Vector3[] probePositions, out Vector3[] probeOffsets, VirtualOffsetSettings voSettings, Dictionary<int, TouchupsPerCell> cellToVolumes)
        {
            var scaleForSearchDist = voSettings.searchMultiplier;
            float rayOriginBias = voSettings.rayOriginBias;
            float geometryBias = voSettings.outOfGeoOffset;

            if (scaleForSearchDist == 0.0f)
            {
                probeOffsets = DoApplyVirtualOffsetsFromAdjustmentVolumes(probePositions, cellToVolumes);
                return;
            }

            // Limit memory usage based on ray cast / hit structures (of which there are lots per position)
            int maxPositionsPerBatch;
            {
                var rayCastBytesPerPosition = UnsafeUtility.SizeOf<RaycastCommand>() * kRayDirectionsPerPosition;
                var rayHitBytesPerPosition = UnsafeUtility.SizeOf<RaycastHit>() * kRayDirectionsPerPosition * voSettings.maxHitsPerRay;
                var rayDataBytesPerPosition = rayCastBytesPerPosition + rayHitBytesPerPosition;
                maxPositionsPerBatch = (kMaxMemoryUsage / 2) / rayDataBytesPerPosition;

#if VERBOSE
                Debug.Log($"Running virtual offset over {(probePositions.Length + maxPositionsPerBatch - 1)/maxPositionsPerBatch} batches.");
#endif
            }

            // This data is shared across all jobs
            var positions = new NativeArray<Vector3>(probePositions, Allocator.TempJob);
            var offsets = new NativeArray<Vector3>(probePositions.Length, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var searchDistanceForPosition = new NativeArray<float>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var rayOriginBiasForPosition = new NativeArray<float>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var geometryBiasForPosition = new NativeArray<float>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var positionIndex = new NativeArray<int>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            // Allocate ray cast/hit data
            var raycastCommands = new[]
            {
                new NativeArray<RaycastCommand>(maxPositionsPerBatch * kRayDirectionsPerPosition, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
                new NativeArray<RaycastCommand>(maxPositionsPerBatch * kRayDirectionsPerPosition, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)
            };
            {
                // We need to set a known per-ray maxHits up-front since raycast command schedule reads this at schedule time. This is a bit annoying but it's a
                // price we'll have to pay right now to be able to create commands from a job.
                QueryParameters queryParams = new QueryParameters();
                queryParams.hitBackfaces = true;
                queryParams.layerMask = 0;
                var defaultRaycastCommand = new RaycastCommand(Vector3.zero, Vector3.zero, queryParams, 0f);
                for (var i = 0; i < maxPositionsPerBatch * kRayDirectionsPerPosition; ++i)
                    raycastCommands[0][i] = raycastCommands[1][i] = defaultRaycastCommand;
            }
            var raycastHits = new[]
            {
                new NativeArray<RaycastHit>(maxPositionsPerBatch * kRayDirectionsPerPosition * voSettings.maxHitsPerRay, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
                new NativeArray<RaycastHit>(maxPositionsPerBatch * kRayDirectionsPerPosition * voSettings.maxHitsPerRay,Allocator.TempJob, NativeArrayOptions.UninitializedMemory)
            };

            // Create job data
            var createRayCastCommandsJob = new CreateRayCastCommandsJob
            {
                voSettings = voSettings,
                positions = positions,
                positionIndex = positionIndex,
                searchDistanceForPosition = searchDistanceForPosition,
                rayOriginBiasForPosition = rayOriginBiasForPosition,
                queryParams = new QueryParameters(voSettings.collisionMask, true, QueryTriggerInteraction.UseGlobal, true),
            };
            var pushOutGeometryJob = new PushOutGeometryJob
            {
                voSettings = voSettings,
                positions = positions,
                offsets = offsets,
                geometryBiasForPosition = geometryBiasForPosition,
                positionIndex = positionIndex,
            };
            var jobHandles = new JobHandle[2];

            try
            {
#if VERBOSE
                var positionsWithColliders = 0;
#endif

                int nextBatchIdx = 1;
                int batchPosIdx = 0;
                float cellSize = m_BakingProfile.cellSizeInMeters;
                while (batchPosIdx < positions.Length)
                {
                    // Run a quick overlap check for each search box before setting up rays for the position
                    int overlapCount = 0;
                    var batchPosStart = batchPosIdx;
                    do {
                        int subdivLevel = m_BakingBatch.GetSubdivLevelAt(positions[batchPosIdx]);
                        var brickSize = ProbeReferenceVolume.CellSize(subdivLevel);
                        var searchDistance = (brickSize * m_BakingProfile.minBrickSize) / ProbeBrickPool.kBrickCellCount;

                        var distanceSearch = scaleForSearchDist * searchDistance;

                        int cellIndex = PosToIndex(Vector3Int.FloorToInt(positions[batchPosIdx] / cellSize));
                        bool hasTouchups = cellToVolumes.TryGetValue(cellIndex, out var volumes), adjusted = false;
                        if (hasTouchups)
                        {
                            foreach (var (touchup, obb, center, offset) in volumes.appliers)
                            {
                                if (touchup.ContainsPoint(obb, center, positions[batchPosIdx]))
                                {
                                    positions[batchPosIdx] += offset;
                                    offsets[batchPosIdx] = offset;
                                    adjusted = true;
                                    break;
                                }
                            }
                        }

                        if (!adjusted && Physics.CheckBox(positions[batchPosIdx], new Vector3(distanceSearch, distanceSearch, distanceSearch), Quaternion.identity, voSettings.collisionMask))
                        {
                            if (hasTouchups)
                            {
                                foreach (var (touchup, obb, center) in volumes.overriders)
                                {
                                    if (touchup.ContainsPoint(obb, center, positions[batchPosIdx]))
                                    {
                                        rayOriginBias = touchup.rayOriginBias;
                                        geometryBias = touchup.geometryBias;
                                        break;
                                    }
                                }
                            }

                            positionIndex[batchPosStart + overlapCount] = batchPosIdx;
                            searchDistanceForPosition[batchPosStart + overlapCount] = distanceSearch;
                            rayOriginBiasForPosition[batchPosStart + overlapCount] = rayOriginBias;
                            geometryBiasForPosition[batchPosStart + overlapCount] = geometryBias;
                            ++overlapCount;
                        }
                    }
                    while (++batchPosIdx < positions.Length && overlapCount < maxPositionsPerBatch);

                    // Swap buffers and sync any already running job at that slot
                    nextBatchIdx = 1 - nextBatchIdx;
                    jobHandles[nextBatchIdx].Complete();

                    var raycastCommandsSub = raycastCommands[nextBatchIdx].GetSubArray(0, overlapCount * kRayDirectionsPerPosition);
                    var raycastHitsSub = raycastHits[nextBatchIdx].GetSubArray(0, overlapCount * kRayDirectionsPerPosition * voSettings.maxHitsPerRay);

                    // Assign ranges and ray/hit arrays
                    createRayCastCommandsJob.startIdx = batchPosStart;
                    createRayCastCommandsJob.raycastCommands = raycastCommandsSub;
                    createRayCastCommandsJob.raycastHits = raycastHitsSub;

                    pushOutGeometryJob.startIdx = batchPosStart;
                    pushOutGeometryJob.raycastCommands = raycastCommandsSub;
                    pushOutGeometryJob.raycastHits = raycastHitsSub;

#if VERBOSE
                    positionsWithColliders += overlapCount;
                    Debug.Log($"Dispatching batch {batchPosStart} - {batchPosStart+overlapCount} using index {nextBatchIdx} (accumulated colliders {positionsWithColliders})");
#endif

#if USE_JOBS
                    // Kick off jobs immediately
                    var createRayCastCommandsJobHandle = createRayCastCommandsJob.Schedule(overlapCount, 32);
                    var raycastCommandsJobHandle = RaycastCommand.ScheduleBatch(raycastCommandsSub, raycastHitsSub, kMinCommandsPerJob, voSettings.maxHitsPerRay, createRayCastCommandsJobHandle);
                    jobHandles[nextBatchIdx] = pushOutGeometryJob.Schedule(overlapCount, 16, raycastCommandsJobHandle);
                    JobHandle.ScheduleBatchedJobs();
#else
                    // Run jobs in-place for easier debugging
                    createRayCastCommandsJob.Run(overlapCount);
                    RaycastCommand.ScheduleBatch(raycastCommandsSub, raycastHitsSub, kMinCommandsPerJob, voSettings.maxHitsPerRay).Complete();
                    pushOutGeometryJob.Run(overlapCount);
#endif
                }

                // Sync any in-flight jobs (order doesn't matter)
                JobHandle.CompleteAll(ref jobHandles[0], ref jobHandles[1]);

                // Copy out result data
                positions.CopyTo(probePositions);
                probeOffsets = offsets.ToArray();

#if VERBOSE
                Debug.Log($"Earlied out {positions.Length - positionsWithColliders}/{positions.Length} probe positions from virtual offset.");
                Debug.Log($"Working memory used: {(raycastCommands[0].Length * UnsafeUtility.SizeOf<RaycastCommand>() * 2 + raycastHits[0].Length * UnsafeUtility.SizeOf<RaycastHit>() * 2) / 1024 / 1024} MB");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                JobHandle.CompleteAll(ref jobHandles[0], ref jobHandles[1]);
                probeOffsets = null;
            }
            finally
            {
                positions.Dispose();
                offsets.Dispose();
                searchDistanceForPosition.Dispose();
                rayOriginBiasForPosition.Dispose();
                geometryBiasForPosition.Dispose();
                positionIndex.Dispose();

                raycastCommands[0].Dispose();
                raycastCommands[1].Dispose();
                raycastHits[0].Dispose();
                raycastHits[1].Dispose();
            }
        }

        // A job that creates raycast commands for any probe position that has passed the initial
        // overlap culling test. Rays are created in the directions of a 3d grid around the center
        // position. (3^3-1 rays per position)
#if USE_BURST
        [BurstCompile]
#endif
        struct CreateRayCastCommandsJob : IJobParallelFor
        {
            [ReadOnly] public VirtualOffsetSettings voSettings;
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<Vector3> positions;
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<int> positionIndex;
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<float> searchDistanceForPosition;
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<float> rayOriginBiasForPosition;

            [ReadOnly] public int startIdx;

            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public NativeArray<RaycastCommand> raycastCommands;
            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public NativeArray<RaycastHit> raycastHits;

            public QueryParameters queryParams;

            public void Execute(int i)
            {
                int posIdx = positionIndex[i + startIdx];
                var position = positions[posIdx];
                var searchDistance = searchDistanceForPosition[i + startIdx];
                var rayOriginBias = rayOriginBiasForPosition[i + startIdx];

                int cmdIdx = i * kRayDirectionsPerPosition;

                // Clear result array because raycast job won't return the actual number of hits :(
                for (var j = 0; j < kRayDirectionsPerPosition * voSettings.maxHitsPerRay; ++j)
                    raycastHits[cmdIdx * voSettings.maxHitsPerRay + j] = default;
                for (var j = 0; j < kRayDirectionsPerPosition; ++j)
                {
                    var direction = kRayDirections[j];
                    var origin = position + direction * rayOriginBias;
                    raycastCommands[cmdIdx++] = new RaycastCommand(origin, direction, queryParams, searchDistance);
                }
            }

            // Typed out in a way Burst understands.
            const float k0 = 0, k1 = 1, k2 = (float)0.70710678118654752440084436210485, k3 = (float)0.57735026918962576450914878050196;
            static readonly Vector3[] kRayDirections =
            {
                new(-k3, +k3, -k3), // -1  1 -1
                new( k0, +k2, -k2), //  0  1 -1
                new(+k3, +k3, -k3), //  1  1 -1
                new(-k2, +k2,  k0), // -1  1  0
                new( k0, +k1,  k0), //  0  1  0
                new(+k2, +k2,  k0), //  1  1  0
                new(-k3, +k3, +k3), // -1  1  1
                new( k0, +k2, +k2), //  0  1  1
                new(+k3, +k3, +k3), //  1  1  1

                new(-k2,  k0, -k2), // -1  0 -1
                new( k0,  k0, -k1), //  0  0 -1
                new(+k2,  k0, -k2), //  1  0 -1
                new(-k1,  k0,  k0), // -1  0  0
                // k0, k0, k0 - skip center position (which would be a zero-length ray)
                new(+k1,  k0,  k0), //  1  0  0
                new(-k2,  k0, +k2), // -1  0  1
                new( k0,  k0, +k1), //  0  0  1
                new(+k2,  k0, +k2), //  1  0  1

                new(-k3, -k3, -k3), // -1 -1 -1
                new( k0, -k2, -k2), //  0 -1 -1
                new(+k3, -k3, -k3), //  1 -1 -1
                new(-k2, -k2,  k0), // -1 -1  0
                new( k0, -k1,  k0), //  0 -1  0
                new(+k2, -k2,  k0), //  1 -1  0
                new(-k3, -k3, +k3), // -1 -1  1
                new( k0, -k2, +k2), //  0 -1  1
                new(+k3, -k3, +k3), //  1 -1  1
            };
        }

        // A job that pushes probe positions out of geometry based on raycast results.
#if USE_BURST
        [BurstCompile]
#endif
        struct PushOutGeometryJob : IJobParallelFor
        {
            [ReadOnly] public VirtualOffsetSettings voSettings;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<int> positionIndex;

            [ReadOnly] public int startIdx;

            [ReadOnly] public NativeArray<RaycastCommand> raycastCommands;
            [ReadOnly] public NativeArray<RaycastHit> raycastHits;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<Vector3> positions;

            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public NativeArray<Vector3> offsets;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<float> geometryBiasForPosition;

            public void Execute(int i)
            {
                int cmdIdx = i * kRayDirectionsPerPosition;
                int posIdx = positionIndex[i + startIdx];
                float geometryBias = geometryBiasForPosition[i + startIdx];
                var offset = PushOutOfGeometry(cmdIdx, geometryBias, voSettings.maxHitsPerRay);
                positions[posIdx] += offset;
                offsets[posIdx] = offset;
            }

            static bool IsNewBestHit(float newDistance, float oldDistance, float newDot, float oldDot)
            {
                const float kDistanceThreshold = 5e-5f;
                const float kDotThreshold = 1e-2f;

                var distanceDiff = newDistance - oldDistance;

                // If new distance is smaller by at least kDistanceThreshold, we accept it as our new best ray.
                var newBestHit = distanceDiff < -kDistanceThreshold;

                // If new distance is larger but by no more than kDistanceThreshold, and ray is at least kDotThreshold more colinear with normal, accept it as new best ray
                if (!newBestHit && distanceDiff < kDistanceThreshold && newDot - oldDot > kDotThreshold)
                    newBestHit = true;

                return newBestHit;
            }

            void GetClosestColliderHit(int hitIdx, Vector3 outRay, int maxHitsPerRay, out float distance, out float dotSurface)
            {
                distance = float.MaxValue;
                dotSurface = -1f;

                for (var n = hitIdx + maxHitsPerRay; hitIdx < n; ++hitIdx)
                {
                    var hit = raycastHits[hitIdx];
                    if (hit.colliderInstanceID == 0)
                        break;

                    var dotRaySurface = Vector3.Dot(outRay, hit.normal);
                    if (IsNewBestHit(hit.distance, distance, dotRaySurface, dotSurface))
                    {
                        distance = hit.distance;
                        dotSurface = dotRaySurface;
                    }
                }
            }

            Vector3 PushOutOfGeometry(int cmdIdx, float biasOutGeo, int maxHitsPerRay)
            {
                var minDist = float.MaxValue;
                var maxDotSurface = -1f;
                var outDirection = Vector3.zero;

                var hitIdx = cmdIdx * maxHitsPerRay;
                for (var i = 0; i < kRayDirectionsPerPosition; ++i, hitIdx += maxHitsPerRay)
                {
                    var outBoundRay = raycastCommands[cmdIdx++];
                    GetClosestColliderHit(hitIdx, outBoundRay.direction, maxHitsPerRay, out var distanceForDir, out var dotSurface);

                    if (distanceForDir < float.MaxValue)
                    {
                        // If any of the closest hit is outside, we are not inside geometry so we don't want to virtual offset.
                        // TO VERIFY: Is this too harsh? Should we allow some level of hit of a front face?
                        if (dotSurface < 0f) return Vector3.zero;

                        if (IsNewBestHit(distanceForDir, minDist, dotSurface, maxDotSurface))
                        {
                            outDirection = outBoundRay.direction;
                            minDist = distanceForDir;
                            maxDotSurface = dotSurface;
                        }
                    }
                }

                if (minDist < float.MaxValue)
                    return outDirection * (minDist * 1.05f + biasOutGeo);

                return Vector3.zero;
            }
        }

        static internal void RecomputeVOForDebugOnly()
        {
            var prv = ProbeReferenceVolume.instance;
            GeneratePhysicsComponentToModList();
            if (prv.perSceneDataList.Count > 0)
            {
                SetBakingContext(prv.perSceneDataList);
            }
            else return;


            globalBounds = prv.globalBounds;
            CellCountInDirections(out minCellPosition, out maxCellPosition, prv.MaxBrickSize());
            cellCount = maxCellPosition + Vector3Int.one - minCellPosition;

            m_BakingBatch = new BakingBatch(128, cellCount);

            List<BakingCell> bakingCells = new List<BakingCell>();

            foreach (var cellInfo in ProbeReferenceVolume.instance.cells.Values)
            {
                var cell = cellInfo.cell;
                var bakingCell = ConvertCellToBakingCell(cell);
                var positions = bakingCell.probePositions;

                for (int i=0; i<positions.Length; ++i)
                {
                    int probeHash = m_BakingBatch.GetProbePositionHash(positions[i]);
                    int subdivLevel = bakingCell.bricks[i / 64].subdivisionLevel;
                    if (m_BakingBatch.uniqueBrickSubdiv.TryGetValue(probeHash, out int currSubdiv))
                    {
                        subdivLevel = Mathf.Min(subdivLevel, currSubdiv);
                    }
                    m_BakingBatch.uniqueBrickSubdiv[probeHash] = subdivLevel;
                }

                ApplyVirtualOffsets(bakingCell.probePositions, out Vector3[] newOffsets);

                // Remove offsets before handing it over for debug
                for (int i = 0; i < positions.Length; ++i)
                {
                    bakingCell.probePositions[i] -= newOffsets[i];
                }

                bakingCell.offsetVectors = newOffsets;

                // We need to force rebuild debug stuff.
                cellInfo.debugProbes = null;

                bakingCells.Add(bakingCell);
            }

            RestorePhysicsComponentsAfterBaking();
            CleanupOccluders();

            // Unload it all as we are gonna load back with newly written cells.
            foreach (var sceneData in prv.perSceneDataList)
            {
                prv.AddPendingAssetRemoval(sceneData.asset);
            }

            // Make sure unloading happens.
            prv.PerformPendingOperations();


            // We now need to make sure we find for each PerSceneData
            foreach (var data in prv.perSceneDataList)
            {
                List<BakingCell> newCells = new List<BakingCell>();
                // This is a bit naive now. Should be fine tho.
                for (int i = 0; i < data.asset.cells.Length; ++i)
                {
                    var currCell = data.asset.cells[i];
                    var bc = bakingCells.Find(x => x.index == currCell.index);
                    newCells.Add(bc);
                }

                // Write bake the assets.
                WriteBakingCells(data, newCells);
                data.ResolveCells();
            }


            // We can now finally reload.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var sceneData in prv.perSceneDataList)
            {
                prv.AddPendingAssetLoading(sceneData.asset);
            }

            prv.PerformPendingOperations();
        }
    }
}
