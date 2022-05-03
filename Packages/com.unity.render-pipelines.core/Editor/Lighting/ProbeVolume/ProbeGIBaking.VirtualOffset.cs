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
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    partial class ProbeGIBaking
    {
        // limit memory usage to ~200 MB (run multiple batches to keep below limit)
        const int kMaxMemoryUsage = 200 * 1024 * 1024;

        const int kMinCommandsPerJob = 512;
        const int kRayDirectionsPerPosition = 3 * 3 * 3 - 1;

        static List<MeshCollider> ms_AddedOccluders;

        static void ApplyVirtualOffsets(Vector3[] positions, out Vector3[] offsets)
        {
            var voSettings = m_BakingSettings.virtualOffsetSettings;
            if (!voSettings.useVirtualOffset)
            {
                offsets = null;
                return;
            }

            var queriesHitBackBefore = Physics.queriesHitBackfaces;
            try
            {
                if (!queriesHitBackBefore)
                    Physics.queriesHitBackfaces = true;

                AddOccluders();
                DoApplyVirtualOffsets(positions, out offsets, voSettings);
            }
            finally
            {
                if (!queriesHitBackBefore)
                    Physics.queriesHitBackfaces = false;

                // We do not cleanup occluders here as it is done after the validity masks are processed.
                //CleanupOccluders();
            }
        }

        static void AddOccluders()
        {
            ms_AddedOccluders = new List<MeshCollider>();

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
                        if ((GameObjectUtility.GetStaticEditorFlags(mr.gameObject) & StaticEditorFlags.ContributeGI) != 0 && !mr.gameObject.TryGetComponent<MeshCollider>(out _))
                        {
                            var meshCollider = mr.gameObject.AddComponent<MeshCollider>();
                            meshCollider.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

                            ms_AddedOccluders.Add(meshCollider);
                        }
                    }
                }
            }

            var autoSimState = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Physics.Simulate(0.1f);
            }
            finally
            {
                Physics.simulationMode = autoSimState;
            }
        }

        private static void CleanupOccluders()
        {
            ms_AddedOccluders.ForEach(Object.DestroyImmediate);
        }

        static void DoApplyVirtualOffsets(Vector3[] probePositions, out Vector3[] probeOffsets, VirtualOffsetSettings voSettings)
        {
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
            var offsets = new NativeArray<Vector3>(probePositions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var searchDistanceForPosition = new NativeArray<float>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var positionHasColliders = new NativeArray<bool>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            // Allocate ray cast/hit data
            var raycastCommands = new[]
            {
                new NativeArray<RaycastCommand>(maxPositionsPerBatch * kRayDirectionsPerPosition, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
                new NativeArray<RaycastCommand>( maxPositionsPerBatch * kRayDirectionsPerPosition, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)
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
                positionHasColliders = positionHasColliders,
                searchDistanceForPosition = searchDistanceForPosition
            };
            var pushOutGeometryJob = new PushOutGeometryJob
            {
                voSettings = voSettings,
                positions = positions,
                offsets = offsets,
                positionHasColliders = positionHasColliders,
            };
            var jobHandles = new JobHandle[2];

            try
            {
#if VERBOSE
                var positionsWithColliders = 0;
#endif

                for (int globalPosIdx = 0, nextBatchIdx = -1; globalPosIdx < positions.Length; globalPosIdx += maxPositionsPerBatch)
                {
                    // Run a quick overlap check for each search box before setting up rays for the position
                    var batchPosStart = globalPosIdx;
                    var batchPosEnd = Mathf.Min(positions.Length, batchPosStart + maxPositionsPerBatch);
                    for (var batchPosIdx = batchPosStart; batchPosIdx < batchPosEnd; ++batchPosIdx)
                    {
                        m_BakingBatch.uniqueBrickSubdiv.TryGetValue(positions[batchPosIdx], out var subdivLevel);
                        var brickSize = ProbeReferenceVolume.CellSize(subdivLevel);
                        var searchDistance = (brickSize * m_BakingProfile.minBrickSize) / ProbeBrickPool.kBrickCellCount;

                        var scaleForSearchDist = voSettings.searchMultiplier;
                        var distanceSearch = scaleForSearchDist * searchDistance;

                        var positionHasCollider = Physics.CheckBox(positions[batchPosIdx], new Vector3(distanceSearch, distanceSearch, distanceSearch), Quaternion.identity, voSettings.collisionMask);

#if VERBOSE
                        if (positionHasCollider)
                            ++positionsWithColliders;
#endif

                        searchDistanceForPosition[batchPosIdx] = distanceSearch;
                        positionHasColliders[batchPosIdx] = positionHasCollider;
                    }

                    // Swap buffers and sync any already running job at that slot
                    nextBatchIdx = (nextBatchIdx + 1) % 2;
                    jobHandles[nextBatchIdx].Complete();

                    // Assign ranges and ray/hit arrays
                    createRayCastCommandsJob.startIdx = batchPosStart;
                    createRayCastCommandsJob.endIdx = batchPosEnd;
                    createRayCastCommandsJob.raycastCommands = raycastCommands[nextBatchIdx];
                    pushOutGeometryJob.startIdx = batchPosStart;
                    pushOutGeometryJob.endIdx = batchPosEnd;
                    pushOutGeometryJob.raycastCommands = raycastCommands[nextBatchIdx];
                    pushOutGeometryJob.raycastHits = raycastHits[nextBatchIdx];

#if VERBOSE
                    Debug.Log($"Dispatching batch {batchPosStart/maxPositionsPerBatch} {batchPosStart} - {batchPosEnd} using index {nextBatchIdx} (accumulated colliders {positionsWithColliders}");
#endif

#if USE_JOBS
                    // Kick off jobs immediately
                    var createRayCastCommandsJobHandle = createRayCastCommandsJob.Schedule();
                    var raycastCommandsJobHandle = RaycastCommand.ScheduleBatch(raycastCommands[nextBatchIdx], raycastHits[nextBatchIdx], kMinCommandsPerJob, voSettings.maxHitsPerRay, createRayCastCommandsJobHandle);
                    jobHandles[nextBatchIdx] = pushOutGeometryJob.Schedule(raycastCommandsJobHandle);
                    JobHandle.ScheduleBatchedJobs();
#else
                    // Run jobs in-place for easier debugging
                    createRayCastCommandsJob.Run();
                    RaycastCommand.ScheduleBatch(raycastCommands[nextBatchIdx], raycastHits[nextBatchIdx], voSettings.maxHitsPerRay, kMinCommandsPerJob).Complete();
                    pushOutGeometryJob.Run();
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
                positionHasColliders.Dispose();

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
        struct CreateRayCastCommandsJob : IJob
        {
            [ReadOnly] public VirtualOffsetSettings voSettings;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<Vector3> positions;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<bool> positionHasColliders;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<float> searchDistanceForPosition;

            [ReadOnly] public int startIdx;
            [ReadOnly] public int endIdx;

            [WriteOnly] public NativeArray<RaycastCommand> raycastCommands;

            public void Execute()
            {
                var queryParams = new QueryParameters(voSettings.collisionMask, true, QueryTriggerInteraction.UseGlobal, true);

                var cmdIdx = 0;
                for (var i = startIdx; i < endIdx; ++i)
                {
                    if (positionHasColliders[i])
                    {
                        var position = positions[i];
                        var searchDistance = searchDistanceForPosition[i];

                        for (var j = 0; j < kRayDirectionsPerPosition; ++j)
                        {
                            var direction = kRayDirections[j];
                            var origin = position + direction * voSettings.rayOriginBias;
                            raycastCommands[cmdIdx++] = new RaycastCommand(origin, direction, queryParams, searchDistance);
                        }
                    }
                    else
                    {
                        // Since there's no option to dispatch commands with a subset of an array, we fill up the commands buffer with no-op raycasts.
                        for (var j = 0; j < kRayDirectionsPerPosition; ++j)
                            raycastCommands[cmdIdx++] = new RaycastCommand(Vector3.zero, Vector3.zero, new QueryParameters(), 0f);
                    }
                }

                // Zero out any remainder of the raycast array
                for (; cmdIdx < raycastCommands.Length;)
                    raycastCommands[cmdIdx++] = new RaycastCommand(Vector3.zero, Vector3.zero, new QueryParameters(), 0f);
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
        struct PushOutGeometryJob : IJob
        {
            [ReadOnly] public VirtualOffsetSettings voSettings;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<bool> positionHasColliders;

            [ReadOnly] public int startIdx;
            [ReadOnly] public int endIdx;

            [ReadOnly] public NativeArray<RaycastCommand> raycastCommands;
            [ReadOnly] public NativeArray<RaycastHit> raycastHits;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<Vector3> positions;

            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public NativeArray<Vector3> offsets;

            public void Execute()
            {
                for (int i = startIdx, cmdIdx = 0; i < endIdx; ++i)
                {
                    if (!positionHasColliders[i])
                    {
                        offsets[i] = Vector3.zero; // We need to write valid data to the entire offset array
                        cmdIdx += kRayDirectionsPerPosition; // Need to maintain cmd<->hit index mapping past noop casts
                        continue;
                    }

                    var position = positions[i];
                    var pushedPosition = PushOutOfGeometry(ref cmdIdx, position, voSettings.outOfGeoOffset, voSettings.maxHitsPerRay);
                    positions[i] = pushedPosition;
                    offsets[i] = pushedPosition - position;
                }
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
                    if (dotRaySurface > 0f && IsNewBestHit(hit.distance, distance, dotRaySurface, dotSurface))
                    {
                        distance = hit.distance;
                        dotSurface = dotRaySurface;
                    }
                }
            }

            Vector3 PushOutOfGeometry(ref int cmdIdx, Vector3 worldPosition, float biasOutGeo, int maxHitsPerRay)
            {
                var minDist = float.MaxValue;
                var maxDotSurface = -1f;
                var outDirection = Vector3.zero;

                var hitIdx = cmdIdx * maxHitsPerRay;
                for (var i = 0; i < kRayDirectionsPerPosition; ++i, hitIdx += maxHitsPerRay)
                {
                    var outBoundRay = raycastCommands[cmdIdx++];
                    GetClosestColliderHit(hitIdx, outBoundRay.direction, maxHitsPerRay, out var distanceForDir, out var dotSurface);

                    if (IsNewBestHit(distanceForDir, minDist, dotSurface, maxDotSurface))
                    {
                        outDirection = outBoundRay.direction;
                        minDist = distanceForDir;
                        maxDotSurface = dotSurface;
                    }
                }

                if (minDist < float.MaxValue)
                {
                    worldPosition += outDirection * (minDist * 1.05f + biasOutGeo);
                }

                return worldPosition;
            }
        }
    }
}
