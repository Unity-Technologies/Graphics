#if UNITY_EDITOR

// Options to force disable paths (don't know which packages are available, don't want to force any)
//#define DISABLE_BURST
//#define DISABLE_COLLECTIONS
//#define DISABLE_JOBS
//#define DISABLE_MATHEMATICS

// Use Burst, Collections, Jobs, Mathematics?
#if HAS_BURST && !DISABLE_BURST
    #define USE_BURST
#endif
#if HAS_COLLECTIONS && !DISABLE_COLLECTIONS
    #define USE_COLLECTIONS
#endif
#if !DISABLE_JOBS
    #define USE_JOBS
#endif
#if HAS_MATHEMATICS && !DISABLE_MATHEMATICS
    #define USE_MATHEMATICS
#endif

using System.Collections.Generic;
using Unity.Collections;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

using Brick = UnityEngine.Experimental.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using ProfilerMarker = Unity.Profiling.ProfilerMarker;

// We'll use NativeArrays and Job structs no matter what
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

// Use Burst only if available.
#if USE_BURST
    using Unity.Burst;
#endif

// Use Mathematics only if available.
#if USE_MATHEMATICS
    using Unity.Mathematics;
#else
    using int3 = UnityEngine.Vector3Int;
    using float3 = UnityEngine.Vector3;
    static class math
    {
        internal static float distancesq(float3 a, float3 b) => float3.Dot(a, b);
        internal static float rcp(float a) => 1f / a;
        internal static float ceil(float a) => UnityEngine.Mathf.Ceil(a);
        internal static float pow(float a, float b) => UnityEngine.Mathf.Pow(a, b);
    }
#endif

namespace UnityEngine.Experimental.Rendering
{
    struct DilationProbe : IComparable<DilationProbe>
#if USE_COLLECTIONS
        , IEquatable<DilationProbe>
#endif
    {
        public int idx;
        public float dist;

        public DilationProbe(int idx, float dist)
        {
            this.idx = idx;
            this.dist = dist;
        }

        public int CompareTo(DilationProbe other)
        {
            return dist.CompareTo(other.dist);
        }

#if USE_COLLECTIONS
        public bool Equals(DilationProbe other)
        {
            return idx == other.idx && dist.Equals(other.dist);
        }

        public override bool Equals(object obj)
        {
            return obj is DilationProbe other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (idx * 397) ^ dist.GetHashCode();
            }
        }
#endif
    }

    class BakingBatch
    {
        public struct BakingBrick
        {
            public int3 position;
            public int subdivisionLevel;
        }

        public struct BakingCell : IDisposable
        {
            public int index;
            public int3 position;
        
            [ReadOnly] public NativeArray<BakingBrick> bricks;
            [ReadOnly] public NativeArray<float3> probePositions;
        
            public NativeArray<SphericalHarmonicsL2> sh;
            public NativeArray<float> validity;

            [ReadOnly] public NativeArray<int> probeIndices;

            internal ProbeReferenceVolume.Cell ToCell()
            {
                var cell = new ProbeReferenceVolume.Cell();
                cell.index = index;
                cell.position = new Vector3Int(position.x, position.y, position.z);
                cell.bricks = bricks.Reinterpret<Brick>().ToArray();
                cell.sh = sh.ToArray();
            
                cell.probePositions = probePositions.Reinterpret<Vector3>().ToArray();
                cell.validity = validity.ToArray();
            
                return cell;
            }

            public void Dispose()
            {
                if (bricks.IsCreated) bricks.Dispose();
                if (probePositions.IsCreated) probePositions.Dispose();
                if (sh.IsCreated) sh.Dispose();
                if (validity.IsCreated) validity.Dispose();
                if (probeIndices.IsCreated) probeIndices.Dispose();
            }
        }
        
        public int index;
        public Dictionary<int, List<Scene>> cellIndex2SceneReferences = new Dictionary<int, List<Scene>>();
        public List<BakingCell> cells = new List<BakingCell>();
        public Dictionary<Vector3, int> uniquePositions = new Dictionary<Vector3, int>();

        private BakingBatch() {}

        public BakingBatch(int index)
        {
            this.index = index;
        }

        public void Clear()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(index, null);

            foreach (var cell in cells)
                cell.Dispose();
            
            cells.Clear();
            cellIndex2SceneReferences.Clear();
        }

        public int uniqueProbeCount => uniquePositions.Keys.Count;
    }

    [InitializeOnLoad]
    class ProbeGIBaking
    {
        static bool m_IsInit = false;
        static BakingBatch m_BakingBatch;
        static ProbeReferenceVolumeAuthoring m_BakingReferenceVolumeAuthoring = null;
        static int m_BakingBatchIndex = 0;

        static Bounds globalBounds = new Bounds();
        static bool hasFoundBounds = false;

        static ProbeGIBaking()
        {
            Init();
        }

        public static void Init()
        {
            if (!m_IsInit)
            {
                m_IsInit = true;
                Lightmapping.lightingDataCleared += OnLightingDataCleared;
                Lightmapping.bakeStarted += OnBakeStarted;
                Lightmapping.bakeCompleted += OnBakeCompleted;
            }
        }

        static public void Clear()
        {
            var refVolAuthList = GameObject.FindObjectsOfType<ProbeReferenceVolumeAuthoring>();

            foreach (var refVolAuthoring in refVolAuthList)
            {
                if (!refVolAuthoring.enabled || !refVolAuthoring.gameObject.activeSelf)
                    continue;

                refVolAuthoring.volumeAsset = null;

                var refVol = ProbeReferenceVolume.instance;
                refVol.Clear();
                refVol.SetTRS(Vector3.zero, Quaternion.identity, refVolAuthoring.brickSize);
                refVol.SetMaxSubdivision(refVolAuthoring.maxSubdivision);
            }

            var probeVolumes = GameObject.FindObjectsOfType<ProbeVolume>();
            foreach (var probeVolume in probeVolumes)
            {
                probeVolume.OnLightingDataAssetCleared();
            }
        }

        static public void ClearBakingBatch()
        {
            m_BakingBatch?.Clear();
            m_BakingBatchIndex = 0;
        }

        public static void FindWorldBounds()
        {
            ProbeReferenceVolume.instance.clearAssetsOnVolumeClear = true;

            var sceneBounds = ProbeReferenceVolume.instance.sceneBounds;

            var prevScenes = new List<string>();
            for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                sceneBounds.UpdateSceneBounds(scene);
                prevScenes.Add(scene.path);
            }

            List<Scene> openedScenes = new List<Scene>();
            hasFoundBounds = false;

            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                var scenePath = buildScene.path;
                bool hasProbeVolumes = false;
                if (sceneBounds.hasProbeVolumes.TryGetValue(scenePath, out hasProbeVolumes))
                {
                    if (hasProbeVolumes)
                    {
                        Bounds localBound;
                        if (sceneBounds.sceneBounds.TryGetValue(scenePath, out localBound))
                        {
                            if (hasFoundBounds)
                            {
                                globalBounds.Encapsulate(localBound);
                            }
                            else
                            {
                                globalBounds = localBound;
                                hasFoundBounds = true;
                            }
                        }
                    }
                }
                else // we need to open the scene to test.
                {
                    var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Additive);
                    openedScenes.Add(scene);
                    sceneBounds.UpdateSceneBounds(scene);
                    Bounds localBound = sceneBounds.sceneBounds[buildScene.path];
                    if (hasFoundBounds)
                        globalBounds.Encapsulate(localBound);
                    else
                        globalBounds = localBound;
                }
            }

            if (openedScenes.Count > 0)
            {
                foreach (var scene in openedScenes)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static ProbeReferenceVolumeAuthoring GetCardinalAuthoringComponent(ProbeReferenceVolumeAuthoring[] refVolAuthList)
        {
            List<ProbeReferenceVolumeAuthoring> enabledVolumes = new List<ProbeReferenceVolumeAuthoring>();

            foreach (var refVolAuthoring in refVolAuthList)
            {
                if (!refVolAuthoring.enabled || !refVolAuthoring.gameObject.activeSelf)
                    continue;

                enabledVolumes.Add(refVolAuthoring);
            }

            int numVols = enabledVolumes.Count;

            if (numVols == 0)
                return null;

            if (numVols == 1)
                return enabledVolumes[0];

            var reference = enabledVolumes[0];
            for (int c = 1; c < numVols; ++c)
            {
                var compare = enabledVolumes[c];
                if (!reference.profile.IsEquivalent(compare.profile))
                    return null;
            }

            return reference;
        }

        static void OnBakeStarted()
        {
            if (!ProbeReferenceVolume.instance.isInitialized) return;

            var refVolAuthList = GameObject.FindObjectsOfType<ProbeReferenceVolumeAuthoring>();
            if (refVolAuthList.Length == 0)
                return;

            FindWorldBounds();
            refVolAuthList = GameObject.FindObjectsOfType<ProbeReferenceVolumeAuthoring>();

            m_BakingReferenceVolumeAuthoring = GetCardinalAuthoringComponent(refVolAuthList);

            if (m_BakingReferenceVolumeAuthoring == null)
            {
                Debug.Log("Scene(s) have multiple inconsistent ProbeReferenceVolumeAuthoring components. Please ensure they use identical profiles and transforms before baking.");
                return;
            }
            
            RunPlacement();
        }

        static void OnBakeCompleted()
        {
            ClearBakingBatch();
        }

        static void CellCountInDirections(out Vector3Int cellsInXYZ, float cellSizeInMeters)
        {
            cellsInXYZ = Vector3Int.zero;

            Vector3 center = Vector3.zero;
            var centeredMin = globalBounds.min - center;
            var centeredMax = globalBounds.max - center;

            cellsInXYZ.x = Mathf.Max(Mathf.CeilToInt(Mathf.Abs(centeredMin.x / cellSizeInMeters)), Mathf.CeilToInt(Mathf.Abs(centeredMax.x / cellSizeInMeters))) * 2;
            cellsInXYZ.y = Mathf.Max(Mathf.CeilToInt(Mathf.Abs(centeredMin.y / cellSizeInMeters)), Mathf.CeilToInt(Mathf.Abs(centeredMax.y / cellSizeInMeters))) * 2;
            cellsInXYZ.z = Mathf.Max(Mathf.CeilToInt(Mathf.Abs(centeredMin.z / cellSizeInMeters)), Mathf.CeilToInt(Mathf.Abs(centeredMax.z / cellSizeInMeters))) * 2;
        }

        static readonly ProfilerMarker sPMOnAdditionalProbesBakeCompleted = new("OnAdditionalProbesBakeCompleted");
        static readonly ProfilerMarker sPMSyncProbeJobs = new("SyncProbeJobs");

        static void OnAdditionalProbesBakeCompleted()
        {
            using var pmOnAdditionalProbesBakeCompleted = sPMOnAdditionalProbesBakeCompleted.Auto();
           
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;
            
            var bakingCells = m_BakingBatch.cells;
            var numCells = bakingCells.Count;

            int numUniqueProbes = m_BakingBatch.uniqueProbeCount;

            using var sh = new NativeArray<SphericalHarmonicsL2>(numUniqueProbes, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            using var validity = new NativeArray<float>(numUniqueProbes, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            using var bakedProbeOctahedralDepth = new NativeArray<float>(numUniqueProbes * 64, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(m_BakingBatch.index, sh, validity, bakedProbeOctahedralDepth);

            var dilationSettings = m_BakingReferenceVolumeAuthoring.GetDilationSettings();
            var jobHandles = new NativeArray<JobHandle>(numCells, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            
            // Schedule processing jobs for all cells
            for (int c = 0; c < numCells; ++c)
            {
                var bakingCell = bakingCells[c];

                if (!bakingCell.probePositions.IsCreated)
                {
                    jobHandles[c] = default;
                    continue;
                }

                int numProbes = bakingCell.probePositions.Length;
                if (numProbes == 0)
                {
                    Debug.LogError("Expected numProbes > 0");
                    continue;
                }

                // Allocate output arrays for processing
                bakingCell.sh = new NativeArray<SphericalHarmonicsL2>(numProbes, Allocator.TempJob);
                bakingCell.validity = new NativeArray<float>(numProbes, Allocator.TempJob);
                bakingCells[c] = bakingCell;

                // Ideally we'd use IJobParallelFor directly over the cells. But because we're trying to stick to non-preview / opt-in
                // packages we don't have access to the Unsafe* containers of the collections package. It's a bit cumbersome to set up
                // the data structures for that with naked memory so we're doing one explicit schedule per cell for now.
                var job = new ProcessCellProbesJob
                {
                    dilationSettings = dilationSettings,
                    
                    sh = sh,
                    validity = validity,
                    bakedProbeOctahedralDepth = bakedProbeOctahedralDepth,

                    bakingCell = bakingCell,
                };
                
                // Either schedule or execute the work.
#if USE_JOBS
                jobHandles[c] = job.Schedule();
#else
                job.Execute();           
#endif
            }

            // Sync jobs if enabled.
#if USE_JOBS
            {
                using var pmSyncProbeJobs = sPMSyncProbeJobs.Auto();
                JobHandle.CombineDependencies(jobHandles).Complete();
            }
#endif
            
            // Convert processed cells to their serializable formats
            for (int c = 0; c < numCells; ++c)
            {
                var bakingCell = bakingCells[c];

                if (bakingCell.probePositions.IsCreated)
                    ProbeReferenceVolume.instance.cells[bakingCell.index] = bakingCell.ToCell();
                else
                    ProbeReferenceVolume.instance.cells[bakingCell.index] = null;
            }

            m_BakingBatchIndex = 0;

            // Reset index
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, null);

            // Map from each scene to an existing reference volume
            var scene2RefVol = new Dictionary<Scene, ProbeReferenceVolumeAuthoring>();
            foreach (var refVol in GameObject.FindObjectsOfType<ProbeReferenceVolumeAuthoring>())
                if (refVol.enabled)
                    scene2RefVol[refVol.gameObject.scene] = refVol;

            // Map from each reference volume to its asset
            var refVol2Asset = new Dictionary<ProbeReferenceVolumeAuthoring, ProbeVolumeAsset>();
            foreach (var refVol in scene2RefVol.Values)
            {
                refVol2Asset[refVol] = ProbeVolumeAsset.CreateAsset(refVol.gameObject.scene);
            }

            // Put cells into the respective assets
            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                if(cell == null)
                    continue;
                
                foreach (var scene in m_BakingBatch.cellIndex2SceneReferences[cell.index])
                {
                    // This scene has a reference volume authoring component in it?
                    ProbeReferenceVolumeAuthoring refVol = null;
                    if (scene2RefVol.TryGetValue(scene, out refVol))
                    {
                        var asset = refVol2Asset[refVol];
                        asset.cells.Add(cell);
                        if (hasFoundBounds)
                        {
                            Vector3Int cellsInDir;
                            float cellSizeInMeters = Mathf.CeilToInt(refVol.profile.cellSizeInMeters);
                            CellCountInDirections(out cellsInDir, cellSizeInMeters);

                            asset.maxCellIndex.x = cellsInDir.x * (int)refVol.profile.cellSizeInBricks;
                            asset.maxCellIndex.y = cellsInDir.y * (int)refVol.profile.cellSizeInBricks;
                            asset.maxCellIndex.z = cellsInDir.z * (int)refVol.profile.cellSizeInBricks;
                        }
                        else
                        {
                            foreach (var p in cell.probePositions)
                            {
                                float x = Mathf.Abs((float)p.x + refVol.transform.position.x) / refVol.profile.minBrickSize;
                                float y = Mathf.Abs((float)p.y + refVol.transform.position.y) / refVol.profile.minBrickSize;
                                float z = Mathf.Abs((float)p.z + refVol.transform.position.z) / refVol.profile.minBrickSize;
                                asset.maxCellIndex.x = Mathf.Max(asset.maxCellIndex.x, Mathf.CeilToInt(x * 2));
                                asset.maxCellIndex.y = Mathf.Max(asset.maxCellIndex.y, Mathf.CeilToInt(y * 2));
                                asset.maxCellIndex.z = Mathf.Max(asset.maxCellIndex.z, Mathf.CeilToInt(z * 2));
                            }
                        }
                    }
                }
            }

            // Connect the assets to their components
            foreach (var pair in refVol2Asset)
            {
                var refVol = pair.Key;
                var asset = pair.Value;

                refVol.volumeAsset = asset;

                if (UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.Iterative)
                {
                    UnityEditor.EditorUtility.SetDirty(refVol);
                    UnityEditor.EditorUtility.SetDirty(refVol.volumeAsset);
                }
            }

            var probeVolumes = GameObject.FindObjectsOfType<ProbeVolume>();
            foreach (var probeVolume in probeVolumes)
            {
                probeVolume.OnBakeCompleted();
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            ProbeReferenceVolume.instance.clearAssetsOnVolumeClear = false;

            foreach (var refVol in refVol2Asset.Keys)
            {
                if (refVol.enabled && refVol.gameObject.activeSelf)
                    refVol.QueueAssetLoading();
            }
        }

        static void OnLightingDataCleared()
        {
            Clear();
        }

        static float CalculateSurfaceArea(Matrix4x4 transform, Mesh mesh)
        {
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = transform * vertices[i];
            }

            double sum = 0.0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 corner = vertices[triangles[i]];
                Vector3 a = vertices[triangles[i + 1]] - corner;
                Vector3 b = vertices[triangles[i + 2]] - corner;

                sum += Vector3.Cross(a, b).magnitude;
            }

            return (float)(sum / 2.0);
        }
        
#if USE_BURST
        [BurstCompile]
#endif
        struct ProcessCellProbesJob : IJob
        {
            static readonly ProfilerMarker sPMDilateProbes = new("DilateProbes");

            internal const int k64 = 64;
            
            internal ProbeDilationSettings dilationSettings;
            
            [ReadOnly] internal NativeArray<SphericalHarmonicsL2> sh;
            [ReadOnly] internal NativeArray<float> validity;
            [ReadOnly] internal NativeArray<float> bakedProbeOctahedralDepth;

            internal BakingBatch.BakingCell bakingCell;

#if USE_BURST
            [BurstCompile]
#endif
            public unsafe void Execute()
            {
                int numProbes = bakingCell.probePositions.Length;
                for (int i = 0; i < numProbes; ++i)
                {
                    int j = bakingCell.probeIndices[i];
                    SphericalHarmonicsL2 shv = sh[j];

                    // Compress the range of all coefficients but the DC component to [0..1]
                    // Upper bounds taken from http://ppsloan.org/publications/Sig20_Advances.pptx
                    // Divide each coefficient by DC*f to get to [-1,1] where f is from slide 33
                    for (int rgb = 0; rgb < 3; ++rgb)
                    {
                        var l0 = sh[j][rgb, 0];

                        if (l0 == 0.0f)
                        {
							// Since we're now allocating uninitialized memory we need to fully clear these.
                            bakingCell.sh[i] = default;
                            bakingCell.validity[i] = default;
                            continue;
                        }

                        // TODO: We're working on irradiance instead of radiance coefficients
                        //       Add safety margin 2 to avoid out-of-bounds values
                        float l1scale = 2.0f; // Should be: 3/(2*sqrt(3)) * 2, but rounding to 2 to issues we are observing.
                        float l2scale = 3.5777088f; // 4/sqrt(5) * 2
                        
                        float rcpL0L1 = math.rcp(l0 * l1scale * 2.0f);
                        float rcpL0L2 = math.rcp(l0 * l2scale * 2.0f);

                        // L_1^m
                        shv[rgb, 1] = sh[j][rgb, 1] * rcpL0L1 + 0.5f;
                        shv[rgb, 2] = sh[j][rgb, 2] * rcpL0L1 + 0.5f;
                        shv[rgb, 3] = sh[j][rgb, 3] * rcpL0L1 + 0.5f;

                        // L_2^-2
                        shv[rgb, 4] = sh[j][rgb, 4] * rcpL0L2 + 0.5f;
                        shv[rgb, 5] = sh[j][rgb, 5] * rcpL0L2 + 0.5f;
                        shv[rgb, 6] = sh[j][rgb, 6] * rcpL0L2 + 0.5f;
                        shv[rgb, 7] = sh[j][rgb, 7] * rcpL0L2 + 0.5f;
                        shv[rgb, 8] = sh[j][rgb, 8] * rcpL0L2 + 0.5f;

                        for (int coeff = 1; coeff < 9; ++coeff)
                            Debug.Assert(shv[rgb, coeff] >= 0.0f && shv[rgb, coeff] <= 1.0f);
                    }

                    ref SphericalHarmonicsL2 sho = ref UnsafeUtility.ArrayElementAsRef<SphericalHarmonicsL2>(bakingCell.sh.GetUnsafePtr(), i);
                    SphericalHarmonicsL2Utils.SetL0(ref sho, new Vector3(shv[0, 0], shv[1, 0], shv[2, 0]));
                    SphericalHarmonicsL2Utils.SetL1R(ref sho, new Vector3(shv[0, 3], shv[0, 1], shv[0, 2]));
                    SphericalHarmonicsL2Utils.SetL1G(ref sho, new Vector3(shv[1, 3], shv[1, 1], shv[1, 2]));
                    SphericalHarmonicsL2Utils.SetL1B(ref sho, new Vector3(shv[2, 3], shv[2, 1], shv[2, 2]));
                    
                    SphericalHarmonicsL2Utils.SetCoefficient(ref sho, 4, new Vector3(shv[0, 4], shv[1, 4], shv[2, 4]));
                    SphericalHarmonicsL2Utils.SetCoefficient(ref sho, 5, new Vector3(shv[0, 5], shv[1, 5], shv[2, 5]));
                    SphericalHarmonicsL2Utils.SetCoefficient(ref sho, 6, new Vector3(shv[0, 6], shv[1, 6], shv[2, 6]));
                    SphericalHarmonicsL2Utils.SetCoefficient(ref sho, 7, new Vector3(shv[0, 7], shv[1, 7], shv[2, 7]));
                    SphericalHarmonicsL2Utils.SetCoefficient(ref sho, 8, new Vector3(shv[0, 8], shv[1, 8], shv[2, 8]));
                    
                    bakingCell.validity[i] = validity[j];
                }

                // Performance warning: this function is super slow (probably 90% of loading time after baking)
                // This is less of a problem after moving it to bursted jobs, but it's still algorithmically really slow.
                DilateInvalidProbes(ref bakingCell, dilationSettings);
            }
        
            static void DilateInvalidProbes(ref BakingBatch.BakingCell bakingCell, ProbeDilationSettings dilationSettings)
            {
                using var pmDilateProbes = sPMDilateProbes.Auto();
                
                // For each brick
                using NativeArray<DilationProbe> culledProbes = new NativeArray<DilationProbe>(bakingCell.bricks.Length * k64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                using NativeArray<DilationProbe> nearProbes = new NativeArray<DilationProbe>(dilationSettings.maxDilationSamples, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
#if USE_COLLECTIONS
                using NativeHashSet<DilationProbe> nearProbesHash = new NativeHashSet<DilationProbe>(bakingCell.bricks.Length * k64, Allocator.Temp);
#endif
                for (int brickIdx = 0; brickIdx < bakingCell.bricks.Length; brickIdx++)
                {
                    // Find probes that are in bricks nearby
                    CullDilationProbes(bakingCell.bricks[brickIdx], bakingCell.bricks, bakingCell.validity, dilationSettings, culledProbes, out int culledProbesCount);
                    
                    // Iterate probes in current brick
                    for (int probeOffset = 0; probeOffset < k64; probeOffset++)
                    {
                        int probeIdx = brickIdx * k64 + probeOffset;

                        // Skip valid probes
                        if (bakingCell.validity[probeIdx] <= dilationSettings.dilationValidityThreshold)
                            continue;

                        // Find distance weighted probes nearest to current probe
                        FindNearProbes(bakingCell.probePositions[probeIdx], bakingCell.probePositions, culledProbes, culledProbesCount, ref dilationSettings, 
    #if USE_COLLECTIONS
                            nearProbesHash,
    #endif
                            nearProbes, out int nearProbesCount, out float invDistSum);

                        // Set invalid probe to weighted average of found neighboring probes
                        var shAverage = new SphericalHarmonicsL2();
                        for (int nearProbeIdx = 0; nearProbeIdx < nearProbesCount; nearProbeIdx++)
                        {
                            var nearProbe = nearProbes[nearProbeIdx];
                            float weight = nearProbe.dist / invDistSum;
                            var target = bakingCell.sh[nearProbe.idx];

                            for (int c = 0; c < 9; ++c)
                            {
                                shAverage[0, c] += target[0, c] * weight;
                                shAverage[1, c] += target[1, c] * weight;
                                shAverage[2, c] += target[2, c] * weight;
                            }
                        }

                        bakingCell.sh[probeIdx] = shAverage;
                        
                        // This seems rather pointless, so disabled.
                        //bakingCell.validity[probeIdx] = bakingCell.validity[probeIdx];
                    }
                }
            }
            
            // Given a brick index, find and accumulate probes in nearby bricks
            static void CullDilationProbes(BakingBatch.BakingBrick currentBrick, NativeArray<BakingBatch.BakingBrick> bricks, NativeArray<float> validity, ProbeDilationSettings dilationSettings,
                NativeArray<DilationProbe> outProbeIndices, out int culledProbesCount)
            {
                culledProbesCount = 0;
                
                for (int otherBrickIdx = 0; otherBrickIdx < bricks.Length; otherBrickIdx++)
                {
                    var otherBrick = bricks[otherBrickIdx];

                    float currentBrickSize = math.pow(3f, currentBrick.subdivisionLevel);
                    float otherBrickSize = math.pow(3f, otherBrick.subdivisionLevel);

                    // TODO: This should probably be revisited.
                    float sqrt2 = 1.41421356237f;
                    float maxDistance = sqrt2 * currentBrickSize + sqrt2 * otherBrickSize;
                    float interval = dilationSettings.maxDilationSampleDistance / dilationSettings.brickSize;
                    maxDistance = interval * math.ceil(maxDistance / interval);
                    float maxDistanceSqr = maxDistance * maxDistance;

                    float halfCurrentBrickSize = currentBrickSize / 2f;
                    float halfOtherBrickSize = otherBrickSize / 2f;
                    float3 currentBrickCenter = currentBrick.position + new float3(halfCurrentBrickSize, halfCurrentBrickSize, halfCurrentBrickSize);
                    float3 otherBrickCenter = otherBrick.position + new float3(halfOtherBrickSize, halfOtherBrickSize, halfOtherBrickSize);

                    if (math.distancesq(currentBrickCenter, otherBrickCenter) <= maxDistanceSqr)
                    {
                        for (int probeOffset = 0; probeOffset < k64; probeOffset++)
                        {
                            int otherProbeIdx = otherBrickIdx * k64 + probeOffset;

                            if (validity[otherProbeIdx] <= dilationSettings.dilationValidityThreshold)
                            {
                                outProbeIndices[culledProbesCount++] = new DilationProbe(otherProbeIdx, 0);
                            }
                        }
                    }
                }
            }

            // Given a probe index, find nearby probes weighted by inverse distance
            static void FindNearProbes(float3 probePosition, NativeArray<float3> probePositions, NativeArray<DilationProbe> culledProbes, int culledProbesCount, ref ProbeDilationSettings dilationSettings,
    #if USE_COLLECTIONS
                NativeHashSet<DilationProbe> nearProbesHash, 
    #endif
                NativeArray<DilationProbe> outNearProbes, out int nearProbesCount, out float invDistSum)
            {
    #if USE_COLLECTIONS
                nearProbesHash.Clear();
    #endif
               
                nearProbesCount = 0;
                invDistSum = 0;
                
#if !USE_COLLECTIONS
                int dilationProbesCount = 0;
#endif
                // Sort probes by distance to prioritize closer ones
                for (int culledProbeIdx = 0; culledProbeIdx < culledProbesCount; ++culledProbeIdx)
                {
                    int culledProbePosIdx = culledProbes[culledProbeIdx].idx;
                    float distSqr = math.distancesq(probePositions[culledProbePosIdx], probePosition);
                    
                    // Algorithmic tweak: don't collect probes we already know are too far away
                    if (distSqr > dilationSettings.maxDilationSampleDistanceSqr)
                        continue;

    #if USE_COLLECTIONS
                    nearProbesHash.Add(new DilationProbe(culledProbePosIdx, distSqr));
    #else
                    culledProbes[dilationProbesCount++] = new DilationProbe(culledProbePosIdx, distSqr);
    #endif
                }

    #if USE_COLLECTIONS
                ref NativeHashSet<DilationProbe> orderedProbes = ref nearProbesHash;
    #else
                NativeArray<DilationProbe> orderedProbes = culledProbes.GetSubArray(0, dilationProbesCount);
                if (!dilationSettings.greedyDilation)
                {
                    orderedProbes.Sort();
                }
    #endif

                // Return specified amount of probes under given max distance
                int numSamples = 0;
                foreach (var current in orderedProbes)
                {
                    if (numSamples >= dilationSettings.maxDilationSamples)
                        return;

                    // Algorithmic tweak: we've already discarded probes that are too far away, and dist is already squared
                    var invDist = math.rcp(current.dist);
                    invDistSum += invDist;
                    outNearProbes[nearProbesCount++] = new DilationProbe(current.idx, invDist);

                    numSamples++;
                }
            }
        }

        private static void DeduplicateProbePositions(in Vector3[] probePositions, Dictionary<Vector3, int> uniquePositions, out NativeArray<int> indices)
        {
            indices = new NativeArray<int>(probePositions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            int uniqueIndex = uniquePositions.Count;

            for (int i = 0; i < probePositions.Length; i++)
            {
                var pos = probePositions[i];

                if (uniquePositions.TryGetValue(pos, out var index))
                {
                    indices[i] = index;
                }
                else
                {
                    uniquePositions[pos] = uniqueIndex;
                    indices[i] = uniqueIndex;
                    uniqueIndex++;
                }
            }
        }
            
        public static void RunPlacement()
        {
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnAdditionalProbesBakeCompleted;

            // Clear baked data
            Clear();

            // Subdivide the scene and place the bricks
            var ctx = PrepareProbeSubdivisionContext(m_BakingReferenceVolumeAuthoring);
            var result = BakeBricks(ctx);

            // Compute probe positions and send them to the Lightmapper
            ApplySubdivisionResults(result);
        }

        public static ProbeSubdivisionContext PrepareProbeSubdivisionContext(ProbeReferenceVolumeAuthoring refVolume)
        {
            ProbeSubdivisionContext ctx = new ProbeSubdivisionContext();

            // Prepare all the information in the scene for baking GI.
            ctx.Initialize(refVolume);

            return ctx;
        }

        static void TrackSceneRefs(Scene origin, Dictionary<Scene, int> sceneRefs)
        {
            if (!sceneRefs.ContainsKey(origin))
                sceneRefs[origin] = 0;
            else
                sceneRefs[origin] += 1;
        }

        public static ProbeSubdivisionResult BakeBricks(ProbeSubdivisionContext ctx)
        {
            var result = new ProbeSubdivisionResult();
            var sceneRefs = new Dictionary<Scene, int>();

            bool realtimeSubdivision = ProbeReferenceVolume.instance.debugDisplay.realtimeSubdivision;
            if (realtimeSubdivision)
                ctx.refVolume.realtimeSubdivisionInfo.Clear();

            using (var gpuResources = ProbePlacement.AllocateGPUResources(ctx.probeVolumes.Count, ctx.refVolume.profile.maxSubdivision))
            {
                // subdivide all the cells and generate brick positions
                foreach (var cell in ctx.cells)
                {
                    sceneRefs.Clear();

                    // Calculate overlaping probe volumes to avoid unnecessary work
                    var overlappingProbeVolumes = new List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)>();
                    foreach (var probeVolume in ctx.probeVolumes)
                    {
                        if (ProbeVolumePositioning.OBBIntersect(probeVolume.volume, cell.volume))
                        {
                            overlappingProbeVolumes.Add(probeVolume);
                            TrackSceneRefs(probeVolume.component.gameObject.scene, sceneRefs);
                        }
                    }

                    // Calculate valid renderers to avoid unnecessary work (a renderer needs to overlap a probe volume and match the layer)
                    var validRenderers = new List<(Renderer component, ProbeReferenceVolume.Volume volume)>();
                    foreach (var renderer in ctx.renderers)
                    {
                        var go = renderer.component.gameObject;
                        int rendererLayerMask = 1 << go.layer;

                        foreach (var probeVolume in overlappingProbeVolumes)
                        {
                            if (ProbeVolumePositioning.OBBIntersect(renderer.volume, probeVolume.volume)
                                && ProbeVolumePositioning.OBBIntersect(renderer.volume, cell.volume))
                            {
                                // Check if the renderer has a matching layer with probe volume
                                if ((probeVolume.component.objectLayerMask & rendererLayerMask) != 0)
                                {
                                    validRenderers.Add(renderer);
                                    TrackSceneRefs(go.scene, sceneRefs);
                                }
                            }
                        }
                    }

                    // Skip empty cells
                    if (validRenderers.Count == 0 && overlappingProbeVolumes.Count == 0)
                        continue;

                    var bricks = ProbePlacement.SubdivideCell(cell.volume, ctx, gpuResources, validRenderers, overlappingProbeVolumes);

                    // Each cell keeps a number of references it has to each scene it was influenced by
                    // We use this list to determine which scene's ProbeVolume asset to assign this cells data to
                    var sortedRefs = new SortedDictionary<int, Scene>();
                    foreach (var item in sceneRefs)
                        sortedRefs[-item.Value] = item.Key;

                    result.cellPositions.Add(cell.position);
                    result.bricksPerCells[cell.position] = bricks;
                    result.sortedRefs = sortedRefs;

                    // If realtime subdivision is enabled, we save a copy of the data inside the authoring component for the debug view
                    if (realtimeSubdivision)
                        ctx.refVolume.realtimeSubdivisionInfo[cell.volume] = bricks;
                }
            }

            return result;
        }

        public static void ApplySubdivisionResults(ProbeSubdivisionResult results)
        {
            int index = 0;
            // For now we just have one baking batch. Later we'll have more than one for a set of scenes.
            // All probes need to be baked only once for the whole batch and not once per cell
            // The reason is that the baker is not deterministic so the same probe position baked in two different cells may have different values causing seams artefacts.
            m_BakingBatch = new BakingBatch(m_BakingBatchIndex++);

            for (var cellIdx = 0; cellIdx < results.cellPositions.Count; ++cellIdx)
            {
                var cellPos = results.cellPositions[cellIdx];
                var bricks = results.bricksPerCells[cellPos];
                
                var bakingCell = new BakingBatch.BakingCell();
                bakingCell.position = new int3(cellPos.x, cellPos.y, cellPos.z);
                bakingCell.index = index++;
                if (bricks.Count > 0)
                {
                    // Convert bricks to positions
                    var probePositionsArr = new Vector3[bricks.Count * ProbeBrickPool.kBrickProbeCountTotal];
                    ProbeReferenceVolume.instance.ConvertBricksToPositions(bricks, probePositionsArr);

                    DeduplicateProbePositions(in probePositionsArr, m_BakingBatch.uniquePositions, out bakingCell.probeIndices);
                    bakingCell.probePositions = new NativeArray<Vector3>(probePositionsArr, Allocator.Persistent).Reinterpret<float3>();
                    bakingCell.bricks = new NativeArray<Brick>(bricks.ToArray(), Allocator.Persistent).Reinterpret<BakingBatch.BakingBrick>();

                    m_BakingBatch.cells.Add(bakingCell);
                    m_BakingBatch.cellIndex2SceneReferences[bakingCell.index] = new List<Scene>(results.sortedRefs.Values);
                }
            }

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, m_BakingBatch.uniquePositions.Keys.ToArray());
        }
    }
}

#endif
