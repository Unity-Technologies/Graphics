using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.SceneManagement;
using UnityEditor;
using Unity.Mathematics;

using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;
using IndirectionEntryInfo = UnityEngine.Rendering.ProbeReferenceVolume.IndirectionEntryInfo;

using TouchupVolumeWithBoundsList = System.Collections.Generic.List<(UnityEngine.Rendering.ProbeReferenceVolume.Volume obb, UnityEngine.Bounds aabb, UnityEngine.Rendering.ProbeAdjustmentVolume volume)>;

namespace UnityEngine.Rendering
{
    struct BakingCell
    {
        public Vector3Int position;
        public int index;

        public Brick[] bricks;
        public Vector3[] probePositions;
        public SphericalHarmonicsL2[] sh;
        public byte[,] validityNeighbourMask;
        public Vector4[] skyOcclusionDataL0L1;
        public byte[] skyShadingDirectionIndices;
        public float[] validity;
        public Vector4[] probeOcclusion;
        public byte[] layerValidity;
        public Vector3[] offsetVectors;
        public float[] touchupVolumeInteraction;

        public int minSubdiv;
        public int indexChunkCount;
        public int shChunkCount;
        public IndirectionEntryInfo[] indirectionEntryInfo;

        public int[] probeIndices;

        public Bounds bounds;

        internal void ComputeBounds(float cellSize)
        {
            var center = new Vector3((position.x + 0.5f) * cellSize, (position.y + 0.5f) * cellSize, (position.z + 0.5f) * cellSize);
            bounds = new Bounds(center, new Vector3(cellSize, cellSize, cellSize));
        }

        internal TouchupVolumeWithBoundsList SelectIntersectingAdjustmentVolumes(TouchupVolumeWithBoundsList touchupVolumesAndBounds)
        {
            // Find the subset of touchup volumes that will be considered for this cell.
            // Capacity of the list to cover the worst case.
            var localTouchupVolumes = new TouchupVolumeWithBoundsList(touchupVolumesAndBounds.Count);
            foreach (var touchup in touchupVolumesAndBounds)
            {
                if (touchup.aabb.Intersects(bounds))
                    localTouchupVolumes.Add(touchup);
            }
            return localTouchupVolumes;
        }

        static internal void CompressSH(ref SphericalHarmonicsL2 shv, float intensityScale, bool clearForDilation)
        {
            // Compress the range of all coefficients but the DC component to [0..1]
            // Upper bounds taken from http://ppsloan.org/publications/Sig20_Advances.pptx
            // Divide each coefficient by DC*f to get to [-1,1] where f is from slide 33
            for (int rgb = 0; rgb < 3; ++rgb)
            {
                for (int k = 0; k < 9; ++k)
                    shv[rgb, k] *= intensityScale;

                var l0 = shv[rgb, 0];

                if (l0 == 0.0f)
                {
                    shv[rgb, 0] = 0.0f;
                    for (int k = 1; k < 9; ++k)
                        shv[rgb, k] = 0.5f;
                }
                else if (clearForDilation)
                {
                    for (int k = 0; k < 9; ++k)
                        shv[rgb, k] = 0.0f;
                }
                else
                {
                    // TODO: We're working on irradiance instead of radiance coefficients
                    //       Add safety margin 2 to avoid out-of-bounds values
                    float l1scale = 2.0f; // Should be: 3/(2*sqrt(3)) * 2, but rounding to 2 to issues we are observing.
                    float l2scale = 3.5777088f; // 4/sqrt(5) * 2

                    // L_1^m
                    shv[rgb, 1] = shv[rgb, 1] / (l0 * l1scale * 2.0f) + 0.5f;
                    shv[rgb, 2] = shv[rgb, 2] / (l0 * l1scale * 2.0f) + 0.5f;
                    shv[rgb, 3] = shv[rgb, 3] / (l0 * l1scale * 2.0f) + 0.5f;

                    // L_2^-2
                    shv[rgb, 4] = shv[rgb, 4] / (l0 * l2scale * 2.0f) + 0.5f;
                    shv[rgb, 5] = shv[rgb, 5] / (l0 * l2scale * 2.0f) + 0.5f;
                    shv[rgb, 6] = shv[rgb, 6] / (l0 * l2scale * 2.0f) + 0.5f;
                    shv[rgb, 7] = shv[rgb, 7] / (l0 * l2scale * 2.0f) + 0.5f;
                    shv[rgb, 8] = shv[rgb, 8] / (l0 * l2scale * 2.0f) + 0.5f;

                    for (int coeff = 1; coeff < 9; ++coeff)
                        shv[rgb, coeff] = Mathf.Clamp01(shv[rgb, coeff]);
                }
            }
        }

        static internal void DecompressSH(ref SphericalHarmonicsL2 shv)
        {
            for (int rgb = 0; rgb < 3; ++rgb)
            {
                var l0 = shv[rgb, 0];

                // See CompressSH
                float l1scale = 2.0f;
                float l2scale = 3.5777088f;

                // L_1^m
                shv[rgb, 1] = (shv[rgb, 1] - 0.5f) * (l0 * l1scale * 2.0f);
                shv[rgb, 2] = (shv[rgb, 2] - 0.5f) * (l0 * l1scale * 2.0f);
                shv[rgb, 3] = (shv[rgb, 3] - 0.5f) * (l0 * l1scale * 2.0f);

                // L_2^-2
                shv[rgb, 4] = (shv[rgb, 4] - 0.5f) * (l0 * l2scale * 2.0f);
                shv[rgb, 5] = (shv[rgb, 5] - 0.5f) * (l0 * l2scale * 2.0f);
                shv[rgb, 6] = (shv[rgb, 6] - 0.5f) * (l0 * l2scale * 2.0f);
                shv[rgb, 7] = (shv[rgb, 7] - 0.5f) * (l0 * l2scale * 2.0f);
                shv[rgb, 8] = (shv[rgb, 8] - 0.5f) * (l0 * l2scale * 2.0f);
            }
        }

        void SetSHCoefficients(int i, SphericalHarmonicsL2 value, float intensityScale, float valid, in ProbeDilationSettings dilationSettings)
        {
            bool clearForDilation = dilationSettings.enableDilation && dilationSettings.dilationDistance > 0.0f && valid > dilationSettings.dilationValidityThreshold;
            CompressSH(ref value, intensityScale, clearForDilation);

            SphericalHarmonicsL2Utils.SetL0(ref sh[i], new Vector3(value[0, 0], value[1, 0], value[2, 0]));
            SphericalHarmonicsL2Utils.SetL1R(ref sh[i], new Vector3(value[0, 3], value[0, 1], value[0, 2]));
            SphericalHarmonicsL2Utils.SetL1G(ref sh[i], new Vector3(value[1, 3], value[1, 1], value[1, 2]));
            SphericalHarmonicsL2Utils.SetL1B(ref sh[i], new Vector3(value[2, 3], value[2, 1], value[2, 2]));

            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 4, new Vector3(value[0, 4], value[1, 4], value[2, 4]));
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 5, new Vector3(value[0, 5], value[1, 5], value[2, 5]));
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 6, new Vector3(value[0, 6], value[1, 6], value[2, 6]));
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 7, new Vector3(value[0, 7], value[1, 7], value[2, 7]));
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 8, new Vector3(value[0, 8], value[1, 8], value[2, 8]));
        }

        void ReadAdjustmentVolumes(ProbeVolumeBakingSet bakingSet, BakingBatch bakingBatch, TouchupVolumeWithBoundsList localTouchupVolumes, int i, float validity,
            ref byte validityMask, out bool invalidatedProbe, out float intensityScale, out uint? skyShadingDirectionOverride)
        {
            invalidatedProbe = false;
            intensityScale = 1.0f;
            skyShadingDirectionOverride = null;

            foreach (var touchup in localTouchupVolumes)
            {
                var touchupBound = touchup.aabb;
                var touchupVolume = touchup.volume;

                // We check a small box around the probe to give some leniency (a couple of centimeters).
                var probeBounds = new Bounds(probePositions[i], new Vector3(0.02f, 0.02f, 0.02f));
                if (touchupVolume.IntersectsVolume(touchup.obb, touchup.aabb, probeBounds))
                {
                    if (touchupVolume.mode == ProbeAdjustmentVolume.Mode.InvalidateProbes)
                    {
                        invalidatedProbe = true;

                        if (validity < 0.05f) // We just want to add probes that were not already invalid or close to.
                        {
                            // We check as below 1 but bigger than 0 in the debug shader, so any value <1 will do to signify touched up.
                            touchupVolumeInteraction[i] = 0.5f;

                            bakingBatch.forceInvalidatedProbesAndTouchupVols[probePositions[i]] = touchupBound;
                        }
                        break;
                    }
                    else if (touchupVolume.mode == ProbeAdjustmentVolume.Mode.OverrideValidityThreshold)
                    {
                        float thresh = (1.0f - touchupVolume.overriddenDilationThreshold);
                        // The 1.0f + is used to determine the action (debug shader tests above 1), then we add the threshold to be able to retrieve it in debug phase.
                        touchupVolumeInteraction[i] = 1.0f + thresh;
                        bakingBatch.customDilationThresh[(index, i)] = thresh;
                    }
                    else if (touchupVolume.mode == ProbeAdjustmentVolume.Mode.OverrideSkyDirection && bakingSet.skyOcclusion && bakingSet.skyOcclusionShadingDirection)
                    {
                        skyShadingDirectionOverride = AdaptiveProbeVolumes.SkyOcclusionBaker.EncodeSkyShadingDirection(touchupVolume.skyDirection);
                    }
                    else if (touchupVolume.mode == ProbeAdjustmentVolume.Mode.OverrideRenderingLayerMask && bakingSet.useRenderingLayers)
                    {
                        switch (touchupVolume.renderingLayerMaskOperation)
                        {
                            case ProbeAdjustmentVolume.RenderingLayerMaskOperation.Override:
                                validityMask = touchupVolume.renderingLayerMask;
                                break;
                            case ProbeAdjustmentVolume.RenderingLayerMaskOperation.Add:
                                validityMask |= touchupVolume.renderingLayerMask;
                                break;
                            case ProbeAdjustmentVolume.RenderingLayerMaskOperation.Remove:
                                validityMask &= (byte)(~touchupVolume.renderingLayerMask);
                                break;
                        }
                    }

                    if (touchupVolume.mode == ProbeAdjustmentVolume.Mode.IntensityScale)
                        intensityScale = touchupVolume.intensityScale;
                    if (intensityScale != 1.0f)
                        touchupVolumeInteraction[i] = 2.0f + intensityScale;
                }
            }

            if (validity < 0.05f && bakingBatch.invalidatedPositions.ContainsKey(probePositions[i]) && bakingBatch.invalidatedPositions[probePositions[i]])
            {
                if (!bakingBatch.forceInvalidatedProbesAndTouchupVols.ContainsKey(probePositions[i]))
                    bakingBatch.forceInvalidatedProbesAndTouchupVols.Add(probePositions[i], new Bounds());

                invalidatedProbe = true;
            }
        }

        internal void SetBakedData(ProbeVolumeBakingSet bakingSet, BakingBatch bakingBatch, TouchupVolumeWithBoundsList localTouchupVolumes, int i, int probeIndex,
            in SphericalHarmonicsL2 sh, float validity, NativeArray<uint> renderingLayerMasks, NativeArray<Vector3> virtualOffsets, NativeArray<Vector4> skyOcclusion, NativeArray<uint> skyDirection, NativeArray<Vector4> probeOcclusion)
        {
            byte layerValidityMask = (byte)(renderingLayerMasks.IsCreated ? renderingLayerMasks[probeIndex] : 0);

            ReadAdjustmentVolumes(bakingSet, bakingBatch, localTouchupVolumes, i, validity, ref layerValidityMask, out var invalidatedProbe, out var intensityScale, out var skyShadingDirectionOverride);
            SetSHCoefficients(i, sh, intensityScale, validity, bakingSet.settings.dilationSettings);

            if (virtualOffsets.IsCreated)
                offsetVectors[i] = virtualOffsets[probeIndex];

            if (skyOcclusion.IsCreated)
            {
                skyOcclusionDataL0L1[i] = skyOcclusion[probeIndex];

                if (skyDirection.IsCreated)
                    skyShadingDirectionIndices[i] = (byte)(skyShadingDirectionOverride ?? skyDirection[probeIndex]);
            }

            if (renderingLayerMasks.IsCreated)
                layerValidity[i] = layerValidityMask;

            float currValidity = invalidatedProbe ? 1.0f : validity;
            byte currValidityNeighbourMask = 255;
            this.validity[i] = currValidity;
            for (int l = 0; l < APVDefinitions.probeMaxRegionCount; l++)
                validityNeighbourMask[l, i] = currValidityNeighbourMask;

            if (probeOcclusion.IsCreated)
                this.probeOcclusion[i] = probeOcclusion[probeIndex];
        }

        internal int GetBakingHashCode()
        {
            int hash = position.GetHashCode();
            hash = hash * 23 + minSubdiv.GetHashCode();
            hash = hash * 23 + indexChunkCount.GetHashCode();
            hash = hash * 23 + shChunkCount.GetHashCode();

            foreach (var brick in bricks)
            {
                hash = hash * 23 + brick.position.GetHashCode();
                hash = hash * 23 + brick.subdivisionLevel.GetHashCode();
            }
            return hash;
        }
    }

    class BakingBatch : IDisposable
    {
        public Dictionary<int, HashSet<string>> cellIndex2SceneReferences = new ();
        public List<BakingCell> cells = new ();
        // Used to retrieve probe data from it's position in order to fix seams
        public NativeHashMap<int, int> positionToIndex;
        // Allow to get a mapping to subdiv level with the unique positions. It stores the minimum subdiv level found for a given position.
        // Can be probably done cleaner.
        public NativeHashMap<int, int> uniqueBrickSubdiv;
        // Mapping for explicit invalidation, whether it comes from the auto finding of occluders or from the touch up volumes
        // TODO: This is not used yet. Will soon.
        public Dictionary<Vector3, bool> invalidatedPositions = new ();
        // Utilities to compute unique probe position hash
        Vector3Int maxBrickCount;
        float inverseScale;
        Vector3 offset;

        public Dictionary<(int, int), float> customDilationThresh = new ();
        public Dictionary<Vector3, Bounds> forceInvalidatedProbesAndTouchupVols = new ();

        private GIContributors? m_Contributors;
        public GIContributors contributors
        {
            get
            {
                if (!m_Contributors.HasValue)
                    m_Contributors = GIContributors.Find(GIContributors.ContributorFilter.All);
                return m_Contributors.Value;
            }
        }

        private BakingBatch() { }

        public BakingBatch(Vector3Int cellCount)
        {
            maxBrickCount = cellCount * ProbeReferenceVolume.CellSize(ProbeReferenceVolume.instance.GetMaxSubdivision());
            inverseScale = ProbeBrickPool.kBrickCellCount / ProbeReferenceVolume.instance.MinBrickSize();
            offset = ProbeReferenceVolume.instance.ProbeOffset();

            // Initialize NativeHashMaps with reasonable initial capacity
            // Using a larger capacity to reduce allocations during baking
            positionToIndex = new NativeHashMap<int, int>(100000, Allocator.Persistent);
            uniqueBrickSubdiv = new NativeHashMap<int, int>(100000, Allocator.Persistent);
        }
        
        public void Dispose()
        {
            if (positionToIndex.IsCreated)
                positionToIndex.Dispose();
            if (uniqueBrickSubdiv.IsCreated)
                uniqueBrickSubdiv.Dispose();
        }

        public int GetProbePositionHash(Vector3 position)
        {
            var brickPosition = Vector3Int.RoundToInt((position - offset) * inverseScale); // Inverse of op in ConvertBricksToPositions()
            return GetBrickPositionHash(brickPosition);
        }

        public int GetBrickPositionHash(Vector3Int brickPosition)
        {
            return brickPosition.x + brickPosition.y * maxBrickCount.x + brickPosition.z * maxBrickCount.x * maxBrickCount.y;
        }

        public int GetSubdivLevelAt(Vector3 position) => uniqueBrickSubdiv[GetProbePositionHash(position)];
    }

    /// <summary>
    /// Class responsible for baking of Probe Volumes
    /// </summary>
    [InitializeOnLoad]
    public partial class AdaptiveProbeVolumes
    {
        internal abstract class BakingProfiling<T> where T : Enum
        {
            protected virtual string LogFile => null; // Override in child classes to write profiling data to disk
            protected virtual bool ShowProgressBar => true;

            protected T prevStage;
            bool disposed = false;
            static float globalProgress = 0.0f;

            public float GetProgress(T stage) => (int)(object)stage / (float)(int)(object)GetLastStep();
            void UpdateProgressBar(T stage)
            {
                if (!ShowProgressBar)
                    return;

                if (EqualityComparer<T>.Default.Equals(stage, GetLastStep()))
                {
                    globalProgress = 0.0f;
                    EditorUtility.ClearProgressBar();
                }
                else
                {
                    globalProgress = Mathf.Max(GetProgress(stage), globalProgress); // prevent progress from going back
                    EditorUtility.DisplayProgressBar("Baking Adaptive Probe Volumes", stage.ToString(), globalProgress);
                }
            }

            public abstract T GetLastStep();

            public BakingProfiling(T stage, ref T currentStage)
            {
                if (LogFile != null && EqualityComparer<T>.Default.Equals(currentStage, GetLastStep()))
                {
                    Profiling.Profiler.logFile = LogFile;
                    Profiling.Profiler.enableBinaryLog = true;
                    Profiling.Profiler.enabled = true;
                }

                prevStage = currentStage;
                currentStage = stage;
                UpdateProgressBar(stage);

                if (LogFile != null)
                    Profiling.Profiler.BeginSample(stage.ToString());
            }

            public void OnDispose(ref T currentStage)
            {
                if (disposed) return;
                disposed = true;

                if (LogFile != null)
                    Profiling.Profiler.EndSample();

                UpdateProgressBar(prevStage);
                currentStage = prevStage;

                if (LogFile != null && EqualityComparer<T>.Default.Equals(currentStage, GetLastStep()))
                {
                    Profiling.Profiler.enabled = false;
                    Profiling.Profiler.logFile = null;
                }
            }
        }

        internal class BakingSetupProfiling : BakingProfiling<BakingSetupProfiling.Stages>, IDisposable
        {
            //protected override string LogFile => "OnBakeStarted";

            public enum Stages
            {
                OnBakeStarted,
                PrepareWorldSubdivision,
                EnsurePerSceneDataInOpenScenes,
                FindWorldBounds,
                PlaceProbes,
                BakeBricks,
                ApplySubdivisionResults,
                None
            }

            static Stages currentStage = Stages.None;
            public BakingSetupProfiling(Stages stage) : base(stage, ref currentStage) { }
            public override Stages GetLastStep() => Stages.None;
            public static void GetProgressRange(out float progress0, out float progress1) { float s = 1 / (float)Stages.None; progress0 = (float)currentStage * s; progress1 = progress0 + s; }
            public void Dispose() { OnDispose(ref currentStage); }
        }

        internal class BakingCompleteProfiling : BakingProfiling<BakingCompleteProfiling.Stages>, IDisposable
        {
            //protected override string LogFile => "OnAdditionalProbesBakeCompleted";

            public enum Stages
            {
                FinalizingBake,
                WriteBakedData,
                PerformDilation,
                None
            }

            static Stages currentStage = Stages.None;
            public BakingCompleteProfiling(Stages stage) : base(stage, ref currentStage) { }
            public override Stages GetLastStep() => Stages.None;
            public static void GetProgressRange(out float progress0, out float progress1) { float s = 1 / (float)Stages.None; progress0 = (float)currentStage * s; progress1 = progress0 + s; }
            public void Dispose() { OnDispose(ref currentStage); }
        }

        struct BakeData
        {
            // Inputs
            public BakeJob[] jobs;
            public int probeCount;
            public int reflectionProbeCount;

            public NativeArray<int> positionRemap;
            public NativeArray<Vector3> originalPositions;
            public NativeArray<Vector3> sortedPositions;

            // Workers
            public Thread bakingThread;
            public VirtualOffsetBaker virtualOffsetJob;
            public SkyOcclusionBaker skyOcclusionJob;
            public LightingBaker lightingJob;
            public RenderingLayerBaker layerMaskJob;
            public int cellIndex;

            public Thread fixSeamsThread;
            public bool doneFixingSeams;

            // Progress reporting
            public BakingStep step;
            public ulong stepCount;

            // Cancellation
            public bool failed;

            public void Init(ProbeVolumeBakingSet bakingSet, NativeList<Vector3> probePositions, List<Vector3> requests)
            {
                probeCount = probePositions.Length;
                reflectionProbeCount = requests.Count;

                jobs = CreateBakingJobs(bakingSet, requests.Count != 0);
                originalPositions = probePositions.ToArray(Allocator.Persistent);
                SortPositions(probePositions, requests);

                virtualOffsetJob = virtualOffsetOverride ?? new DefaultVirtualOffset();
                virtualOffsetJob.Initialize(bakingSet, sortedPositions.GetSubArray(0, probeCount));

                skyOcclusionJob = skyOcclusionOverride ?? new DefaultSkyOcclusion();
                skyOcclusionJob.Initialize(bakingSet, sortedPositions.GetSubArray(0, probeCount));
                if (skyOcclusionJob is DefaultSkyOcclusion defaultSOJob)
                    defaultSOJob.jobs = jobs;

                layerMaskJob = renderingLayerOverride ?? new DefaultRenderingLayer();
                layerMaskJob.Initialize(bakingSet, sortedPositions.GetSubArray(0, probeCount));

                lightingJob = lightingOverride ?? new DefaultLightTransport();
                lightingJob.Initialize(ProbeVolumeLightingTab.GetLightingSettings().mixedBakeMode != MixedLightingMode.IndirectOnly, sortedPositions, layerMaskJob.renderingLayerMasks);
                if (lightingJob is DefaultLightTransport defaultLightingJob)
                    defaultLightingJob.jobs = jobs;

                cellIndex = 0;

                LightingBaker.cancel = false;
                step = BakingStep.VirtualOffset;
                stepCount = virtualOffsetJob.stepCount + lightingJob.stepCount + skyOcclusionJob.stepCount;
            }

            public void ExecuteLightingAsync()
            {
                bakingThread = new Thread(() => {
                    var job = s_BakeData.lightingJob;
                    while (job.currentStep < job.stepCount)
                    {
                        if (!job.Step())
                        {
                            s_BakeData.failed = true;
                            return;
                        }
                        if (LightingBaker.cancel)
                            break;
                    }
                });
                bakingThread.Start();
            }

            static BakeJob[] CreateBakingJobs(ProbeVolumeBakingSet bakingSet, bool hasAdditionalRequests)
            {
                // Build the list of adjustment volumes affecting sample count
                var touchupVolumesAndBounds = new TouchupVolumeWithBoundsList();
                {
                    // This is slow, but we should have very little amount of touchup volumes.
                    foreach (var adjustment in s_AdjustmentVolumes)
                    {
                        if (adjustment.volume.mode == ProbeAdjustmentVolume.Mode.OverrideSampleCount)
                            touchupVolumesAndBounds.Add(adjustment);
                    }

                    // Sort by volume to give priority to smaller volumes
                    touchupVolumesAndBounds.Sort((a, b) => (a.volume.ComputeVolume(a.obb).CompareTo(b.volume.ComputeVolume(b.obb))));
                }

                var lightingSettings = ProbeVolumeLightingTab.GetLightingSettings();
                bool skyOcclusion = bakingSet.skyOcclusion;

                int additionalJobs = hasAdditionalRequests ? 2 : 1;
                var jobs = new BakeJob[touchupVolumesAndBounds.Count + additionalJobs];

                for (int i = 0; i < touchupVolumesAndBounds.Count; i++)
                    jobs[i].Create(lightingSettings, skyOcclusion, touchupVolumesAndBounds[i]);

                jobs[touchupVolumesAndBounds.Count + 0].Create(bakingSet, lightingSettings, skyOcclusion);
                if (hasAdditionalRequests)
                    jobs[touchupVolumesAndBounds.Count + 1].Create(bakingSet, lightingSettings, false);

                return jobs;
            }

            // Place positions contiguously for each bake job in a single array, with reflection probes at the end
            public void SortPositions(NativeList<Vector3> probePositions, List<Vector3> additionalRequests)
            {
                positionRemap = new NativeArray<int>(probePositions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                sortedPositions = new NativeArray<Vector3>(probePositions.Length + additionalRequests.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                int regularJobCount = additionalRequests.Count != 0 ? jobs.Length - 1 : jobs.Length;

                // Place each probe in the correct job
                int[] jobSize = new int[regularJobCount];
                for (int i = 0; i < probePositions.Length; i++)
                {
                    // Last regular job (so before reflection probes if they exist) is the default one
                    // In case we don't match any touchup, we should be placed in this one
                    int jobIndex = 0;
                    for (; jobIndex < regularJobCount - 1; jobIndex++)
                    {
                        if (jobs[jobIndex].Contains(probePositions[i]))
                            break;
                    }

                    positionRemap[i] = jobIndex;
                    jobSize[jobIndex]++;
                }

                // Compute the size and offset of each job in the sorted array
                int currentOffset = 0;
                for (int i = 0; i < regularJobCount; i++)
                {
                    ref var job = ref jobs[i];
                    job.startOffset = currentOffset;
                    job.probeCount = jobSize[i];
                    currentOffset += job.probeCount;
                    jobSize[i] = 0;
                }

                Debug.Assert(currentOffset == probePositions.Length);

                // Sort position and store remapping
                for (int i = 0; i < probePositions.Length; i++)
                {
                    int jobIndex = positionRemap[i];
                    int newPos = jobs[jobIndex].startOffset + jobSize[jobIndex]++;
                    positionRemap[i] = newPos;
                    sortedPositions[newPos] = probePositions[i];
                }

                // Place reflection probe positions at the end of the array
                if (additionalRequests.Count != 0)
                {
                    ref var requestJob = ref jobs[jobs.Length - 1];
                    requestJob.startOffset = currentOffset;
                    requestJob.probeCount = additionalRequests.Count;
                    for (int i = 0; i < additionalRequests.Count; i++)
                        sortedPositions[currentOffset++] = additionalRequests[i];

                    Debug.Assert(currentOffset == sortedPositions.Length);
                }
            }

            public void ApplyVirtualOffset()
            {
                var offsets = virtualOffsetJob.offsets;
                for (int i = 0; i < offsets.Length; i++)
                    sortedPositions[i] += offsets[i];
            }

            public bool Done()
            {
                ulong currentStep = s_BakeData.virtualOffsetJob.currentStep + lightingJob.currentStep + s_BakeData.skyOcclusionJob.currentStep;
                return currentStep >= s_BakeData.stepCount && s_BakeData.step == BakingStep.Last;
            }

            public void Dispose()
            {
                if (failed)
                    Debug.LogError("Probe Volume Baking failed.");

                if (jobs == null)
                    return;

                foreach (var job in jobs)
                    job.Dispose();

                positionRemap.Dispose();
                originalPositions.Dispose();
                sortedPositions.Dispose();

                skyOcclusionJob.encodedDirections.Dispose();
                virtualOffsetJob.Dispose();
                skyOcclusionJob.Dispose();
                lightingJob.Dispose();
                layerMaskJob.Dispose();

                // clear references to managed data
                this = default;
            }
        }


        static bool m_IsInit = false;
        static BakingBatch m_BakingBatch;
        static ProbeVolumeBakingSetWeakReference m_BakingSetReference = new();
        static ProbeVolumeBakingSet m_BakingSet
        {
            get => m_BakingSetReference.Get();
            set => m_BakingSetReference.Set(value);
        }
        static TouchupVolumeWithBoundsList s_AdjustmentVolumes;

        static Bounds globalBounds = new Bounds();
        static Vector3Int minCellPosition = Vector3Int.one * int.MaxValue;
        static Vector3Int maxCellPosition = Vector3Int.one * int.MinValue;
        static Vector3Int cellCount = Vector3Int.zero;

        static int pvHashesAtBakeStart = -1;
        static APVRTContext s_TracingContext;
        static BakeData s_BakeData;

        static Dictionary<int, BakingCell> m_BakedCells = new Dictionary<int, BakingCell>();

        internal static HashSet<string> partialBakeSceneList = null;
        internal static bool isBakingSceneSubset => partialBakeSceneList != null;
        internal static bool isFreezingPlacement = false;

        static SphericalHarmonicsL2 s_BlackSH;
        static bool s_BlackSHInitialized = false;

        static SphericalHarmonicsL2 GetBlackSH()
        {
            if (!s_BlackSHInitialized)
            {
                // Init SH with values that will resolve to black
                s_BlackSH = new SphericalHarmonicsL2();
                for (int channel = 0; channel < 3; ++channel)
                {
                    s_BlackSH[channel, 0] = 0.0f;
                    for (int coeff = 1; coeff < 9; ++coeff)
                        s_BlackSH[channel, coeff] = 0.5f;
                }
            }

            return s_BlackSH;
        }

        static AdaptiveProbeVolumes()
        {
            Init();
        }

        static internal void Init()
        {
            if (!m_IsInit)
            {
                m_IsInit = true;
                Lightmapping.lightingDataCleared += OnLightingDataCleared;
                Lightmapping.bakeStarted += OnBakeStarted;
                Lightmapping.bakeCancelled += OnBakeCancelled;
            }
        }

        static internal void Dispose()
        {
            s_TracingContext.Dispose();
        }

        static void OnLightingDataCleared()
        {
            if (ProbeReferenceVolume.instance == null)
                return;
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
                return;

            Clear();
        }

        static internal void Clear()
        {
            var activeSet = ProbeVolumeBakingSet.GetBakingSetForScene(SceneManager.GetActiveScene());

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                data.Clear();

            ProbeReferenceVolume.instance.Clear();

            if (activeSet != null)
                activeSet.Clear();

            var probeVolumes = GameObject.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.InstanceID);
            foreach (var probeVolume in probeVolumes)
                probeVolume.OnLightingDataAssetCleared();
        }

        static bool SetBakingContext(List<ProbeVolumePerSceneData> perSceneData)
        {
            var prv = ProbeReferenceVolume.instance;
            bool isBakingSingleScene = false;
            for (int i = 0; i < perSceneData.Count; ++i)
            {
                var data = perSceneData[i];
                var scene = data.gameObject.scene;
                var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(scene);
                if (bakingSet != null && bakingSet.singleSceneMode)
                {
                    isBakingSingleScene = true;
                    break;
                }
            }

            // We need to make sure all scenes we are baking are from the same baking set.
            // TODO: This should be ensured by the controlling panel, until we have that we need to assert.
            for (int i = 0; i < perSceneData.Count; ++i)
            {
                var data = perSceneData[i];
                var sceneGUID = data.sceneGUID;
                var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(sceneGUID);

                if (bakingSet == null)
                {
                    if (isBakingSingleScene)
                        continue;

                    var sceneName = data.gameObject.scene.name;
                    Debug.LogError($"Scene '{sceneName}' does not belong to any Baking Set. Please add it to a Baking Set in the Adaptive Probe Volumes tab of the Lighting Window.");
                    return false;
                }

                bakingSet.SetActiveScenario(bakingSet.lightingScenario, verbose: false); // Ensure we are not blending any other scenario.
                bakingSet.BlendLightingScenario(null, 0.0f);

                if (i == 0)
                    m_BakingSet = bakingSet;
                else if (!m_BakingSet.IsEquivalent(bakingSet))
                    return false;
            }

            return true;
        }

        static bool EnsurePerSceneDataInOpenScenes()
        {
            var prv = ProbeReferenceVolume.instance;
            var activeScene = SceneManager.GetActiveScene();

            var activeSet = ProbeVolumeBakingSet.GetBakingSetForScene(activeScene);
            if (activeSet == null && ProbeVolumeBakingSet.SceneHasProbeVolumes(ProbeReferenceVolume.GetSceneGUID(activeScene)))
            {
                Debug.LogError($"Active scene at {activeScene.path} is not part of any baking set.");
                return false;
            }

            // We assume that all the per scene data for all the scenes in the set have been set with the scene been saved at least once. However we also update the scenes that are currently loaded anyway for security.
            // and to have a new trigger to update the bounds we have.
            int openedScenesCount = SceneManager.sceneCount;
            for (int i = 0; i < openedScenesCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                ProbeVolumeBakingSet.OnSceneSaving(scene); // We need to perform the same actions we do when the scene is saved.
                var sceneBakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(scene);

                if (sceneBakingSet != null && sceneBakingSet != activeSet && ProbeVolumeBakingSet.SceneHasProbeVolumes(ProbeReferenceVolume.GetSceneGUID(scene)))
                {
                    Debug.LogError($"Scene at {scene.path} is loaded and has probe volumes, but not part of the same baking set as the active scene. This will result in an error. Please make sure all loaded scenes are part of the same baking set.");
                    return false;
                }
            }

            // Make sure there are no remaining per scene data in scenes where probe volume was deleted
            // iterate in reverse order because destroy will pop element from the array
            for (int i = ProbeReferenceVolume.instance.perSceneDataList.Count - 1; i >= 0; i--)
            {
                var perSceneData = ProbeReferenceVolume.instance.perSceneDataList[i];
                if (!ProbeVolumeBakingSet.SceneHasProbeVolumes(perSceneData.sceneGUID))
                    CoreUtils.Destroy(perSceneData.gameObject);
            }

            return true;
        }

        static void CachePVHashes(List<ProbeVolume> probeVolumes)
        {
            pvHashesAtBakeStart = 0;
            foreach (var pv in probeVolumes)
            {
                pvHashesAtBakeStart += pvHashesAtBakeStart * 23 + pv.GetHashCode();
            }
        }

        static void CheckPVChanges()
        {
            // If we have baking in flight.
            if (Lightmapping.isRunning && (GUIUtility.hotControl == 0))
            {
                var pvList = GetProbeVolumeList();
                int currHash = 0;
                foreach (var pv in pvList)
                {
                    currHash += currHash * 23 + pv.GetHashCode();
                }

                if (currHash != pvHashesAtBakeStart)
                {
                    Lightmapping.Cancel();
                    Lightmapping.BakeAsync();
                }
            }
        }

        static void CellCountInDirections(out Vector3Int minCellPositionXYZ, out Vector3Int maxCellPositionXYZ, float cellSizeInMeters, Vector3 worldOffset)
        {
            // Sync with ProbeVolumeProfileInfo.PositionToCell
            minCellPositionXYZ = Vector3Int.FloorToInt((globalBounds.min - worldOffset) / cellSizeInMeters);
            maxCellPositionXYZ = Vector3Int.FloorToInt((globalBounds.max - worldOffset) / cellSizeInMeters);
        }

        static TouchupVolumeWithBoundsList GetAdjustementVolumes()
        {
            // This is slow, but we should have very little amount of touchup volumes.
            var touchupVolumes = Object.FindObjectsByType<ProbeAdjustmentVolume>(FindObjectsSortMode.InstanceID);

            var touchupVolumesAndBounds = new TouchupVolumeWithBoundsList(touchupVolumes.Length);
            foreach (var touchup in touchupVolumes)
            {
                if (touchup.isActiveAndEnabled)
                {
                    touchup.GetOBBandAABB(out var obb, out var aabb);
                    touchupVolumesAndBounds.Add((obb, aabb, touchup));
                    touchup.skyDirection.Normalize();
                }
            }

            // Sort by volume to give priority to bigger volumes so smaller volumes are applied last
            touchupVolumesAndBounds.Sort((a, b) => (b.volume.ComputeVolume(b.obb).CompareTo(a.volume.ComputeVolume(a.obb))));

            return touchupVolumesAndBounds;
        }

        // Actual baking process

        enum BakingStep
        {
            VirtualOffset,
            LaunchThread,
            SkyOcclusion,
            RenderingLayerMask,
            Integration,
            FixSeams,
            FinalizeCells,

            Last = FinalizeCells + 1
        }

        static void OnBakeStarted()
        {
            if (PrepareBaking())
            {
                ProbeReferenceVolume.instance.checksDuringBakeAction = CheckPVChanges;
                Lightmapping.SetAdditionalBakeDelegate(BakeDelegate);
            }
        }

        internal static bool PrepareBaking()
        {
            if (AdaptiveProbeVolumes.isRunning)
                AdaptiveProbeVolumes.Cancel();

            List<Vector3> requests;
            NativeList<Vector3> positions;
            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.OnBakeStarted))
            {
                if (!InitializeBake())
                    return false;

                s_AdjustmentVolumes = GetAdjustementVolumes();
                requests = AdditionalGIBakeRequestsManager.GetProbeNormalizationRequests();

                bool canceledByUser = false;
                // Note: this could be executed in the baking delegate to be non blocking
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.PlaceProbes))
                    positions = RunPlacement(ref canceledByUser);

                if (positions.Length == 0 || canceledByUser)
                {
                    positions.Dispose();

                    Clear();
                    CleanBakeData();

                    if (canceledByUser)
                        Lightmapping.Cancel();

                    return false;
                }
            }

            s_BakeData.Init(m_BakingSet, positions, requests);
            positions.Dispose();
            return true;
        }

        static bool InitializeBake()
        {
            if (ProbeVolumeLightingTab.instance?.PrepareAPVBake(ProbeReferenceVolume.instance) == false)
                return false;

            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
                return false;

            using var scope = new BakingSetupProfiling(BakingSetupProfiling.Stages.PrepareWorldSubdivision);

            // Verify to make sure we can still do it. Shortcircuting so that we don't run CanFreezePlacement unless is needed.
            isFreezingPlacement = isFreezingPlacement && CanFreezePlacement();
            if (!isFreezingPlacement)
            {
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.EnsurePerSceneDataInOpenScenes))
                {
                    if (!EnsurePerSceneDataInOpenScenes())
                        return false;
                }
            }

            if (ProbeReferenceVolume.instance.perSceneDataList.Count == 0)
                return false;

            var sceneDataList = GetPerSceneDataList();
            if (sceneDataList.Count == 0)
                return false;

            var pvList = GetProbeVolumeList();
            if (pvList.Count == 0)
                return false; // We have no probe volumes.

            CachePVHashes(pvList);

            if (!SetBakingContext(sceneDataList))
                return false;

            m_TotalCellCounts = new CellCounts();
            m_ProfileInfo = GetProfileInfoFromBakingSet(m_BakingSet);
            if (isFreezingPlacement)
            {
                ModifyProfileFromLoadedData(m_BakingSet);
            }
            else
            {
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.FindWorldBounds))
                    FindWorldBounds();
            }

            // Get min/max
            CellCountInDirections(out minCellPosition, out maxCellPosition, m_ProfileInfo.cellSizeInMeters, m_ProfileInfo.probeOffset);
            cellCount = maxCellPosition + Vector3Int.one - minCellPosition;

            if (!ProbeReferenceVolume.instance.EnsureCurrentBakingSet(m_BakingSet))
                return false;

            if (!Lightmapping.TryGetLightingSettings(out var lightingSettings) || lightingSettings ==null || lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveCPU)
            {
                m_BakingSet.skyOcclusion = false;
            }

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
            {
                // It can be null if the scene was never added to a baking set and we are baking in single scene mode, in that case we don't have a baking set for it yet and we need to skip
                if (data.serializedBakingSet != null)
                    data.Initialize();
            }

            return true;
        }

        static void BakeDelegate(ref float progress, ref bool done)
        {
            if (s_BakeData.step == BakingStep.VirtualOffset)
            {
                if (!s_BakeData.virtualOffsetJob.Step())
                    s_BakeData.failed = true;
                if (s_BakeData.virtualOffsetJob.currentStep >= s_BakeData.virtualOffsetJob.stepCount)
                {
                    if (s_BakeData.virtualOffsetJob.offsets.IsCreated)
                        s_BakeData.ApplyVirtualOffset();
                    s_BakeData.step++;
                }
            }

            if (s_BakeData.step == BakingStep.LaunchThread)
            {
                if (s_BakeData.lightingJob.isThreadSafe)
                    s_BakeData.ExecuteLightingAsync();
                s_BakeData.step++;
            }

            if (s_BakeData.step == BakingStep.SkyOcclusion)
            {
                if (!s_BakeData.skyOcclusionJob.Step())
                    s_BakeData.failed = true;
                if (s_BakeData.skyOcclusionJob.currentStep >= s_BakeData.skyOcclusionJob.stepCount)
                {
                    if (!s_BakeData.failed && s_BakeData.skyOcclusionJob.shadingDirections.IsCreated)
                        s_BakeData.skyOcclusionJob.Encode();
                    s_BakeData.step++;
                }
            }

            if (s_BakeData.step == BakingStep.RenderingLayerMask)
            {
                if (!s_BakeData.layerMaskJob.Step())
                    s_BakeData.failed = true;
                if (s_BakeData.layerMaskJob.currentStep >= s_BakeData.layerMaskJob.stepCount)
                    s_BakeData.step++;
            }

            if (s_BakeData.step == BakingStep.Integration)
            {
                if (!s_BakeData.lightingJob.isThreadSafe)
                {
                    if (!s_BakeData.lightingJob.Step())
                        s_BakeData.failed = true;
                }
                if (s_BakeData.lightingJob.currentStep >= s_BakeData.lightingJob.stepCount)
                {
                    if (s_BakeData.lightingJob.isThreadSafe)
                        s_BakeData.bakingThread.Join();
                    s_BakeData.step++;
                }
            }

            if (s_BakeData.step == BakingStep.FixSeams)
            {
                // Start fixing seams in the background
                if (s_BakeData.fixSeamsThread == null)
                {
                    s_BakeData.doneFixingSeams = false;
                    s_BakeData.fixSeamsThread = new Thread(() =>
                    {
                        FixSeams(
                            s_BakeData.positionRemap,
                            s_BakeData.originalPositions,
                            s_BakeData.lightingJob.irradiance,
                            s_BakeData.lightingJob.validity,
                            s_BakeData.lightingJob.occlusion,
                            s_BakeData.skyOcclusionJob.occlusion,
                            s_BakeData.layerMaskJob.renderingLayerMasks);

                        s_BakeData.doneFixingSeams = true;
                    });
                    s_BakeData.fixSeamsThread.Start();
                }
                // Wait until fixing seams is done
                else
                {
                    if (s_BakeData.doneFixingSeams)
                    {
                        s_BakeData.fixSeamsThread.Join();
                        s_BakeData.fixSeamsThread = null;
                        s_BakeData.step++;
                    }
                }
            }

            if (s_BakeData.step == BakingStep.FinalizeCells)
            {
                FinalizeCell(s_BakeData.cellIndex++, s_BakeData.positionRemap,
                    s_BakeData.lightingJob.irradiance, s_BakeData.lightingJob.validity,
                    s_BakeData.layerMaskJob.renderingLayerMasks,
                    s_BakeData.virtualOffsetJob.offsets,
                    s_BakeData.skyOcclusionJob.occlusion, s_BakeData.skyOcclusionJob.encodedDirections,
                    s_BakeData.lightingJob.occlusion);

                if (s_BakeData.cellIndex >= m_BakingBatch.cells.Count)
                    s_BakeData.step++;
            }

            // Handle error case
            if (s_BakeData.failed)
            {
                CleanBakeData();
                done = true;
                return;
            }

            // When using default backend, live progress report is not accurate
            // So we can't rely on it to know when baking is done, but it's useful for showing progress
            ulong currentStep = s_BakeData.virtualOffsetJob.currentStep + s_BakeData.skyOcclusionJob.currentStep;
            if (s_BakeData.lightingJob is DefaultLightTransport defaultJob)
            {
                foreach (var job in defaultJob.jobs)
                    currentStep += job.currentStep;
            }
            else
                currentStep += s_BakeData.lightingJob.currentStep;
            progress = currentStep / (float)s_BakeData.stepCount;

            // Use our counter to determine when baking is done
            if (s_BakeData.Done())
            {
                FinalizeBake();
                done = true;
            }
        }

        static void FinalizeBake(bool cleanup = true)
        {
            using (new BakingCompleteProfiling(BakingCompleteProfiling.Stages.FinalizingBake))
            {
                if (s_BakeData.probeCount != 0)
                {
                    try
                    {
                        ApplyPostBakeOperations();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                if (s_BakeData.reflectionProbeCount != 0)
                {
                    var additionalIrradiance = s_BakeData.lightingJob.irradiance.GetSubArray(s_BakeData.probeCount, s_BakeData.reflectionProbeCount);
                    var additionalValidity = s_BakeData.lightingJob.validity.GetSubArray(s_BakeData.probeCount, s_BakeData.reflectionProbeCount);
                    AdditionalGIBakeRequestsManager.OnAdditionalProbesBakeCompleted(additionalIrradiance, additionalValidity);
                }
            }

            if (cleanup)
                CleanBakeData();

            // We need to reset that view
            ProbeReferenceVolume.instance.ResetDebugViewToMaxSubdiv();
        }

        static void OnBakeCancelled()
        {
            if (s_BakeData.bakingThread != null)
            {
                LightingBaker.cancel = true;
                s_BakeData.bakingThread.Join();
                LightingBaker.cancel = false;
            }

            if (s_BakeData.fixSeamsThread != null)
            {
                LightingBaker.cancel = true;
                s_BakeData.fixSeamsThread.Join();
                LightingBaker.cancel = false;
            }

            CleanBakeData();
        }

        static void CleanBakeData()
        {
            s_BakeData.Dispose();
            m_BakingBatch?.Dispose();
            m_BakingBatch = null;
            s_AdjustmentVolumes = null;

            // If lighting pannel is not created, we have to dispose ourselves
            if (ProbeVolumeLightingTab.instance == null)
                AdaptiveProbeVolumes.Dispose();

            Lightmapping.ResetAdditionalBakeDelegate();

            partialBakeSceneList = null;
            ProbeReferenceVolume.instance.checksDuringBakeAction = null;
        }

        class VoxelToBrickCache
        {
            class CacheEntry
            {
                public int access; // keep track of last access age
                public Dictionary<int, Brick> map = new();
            }

            const int k_MaxCellsCached = 64;

            int accesses = 0;
            Dictionary<int, CacheEntry> cache = new();
            ObjectPool<CacheEntry> m_BrickMetaPool = new ObjectPool<CacheEntry>(x => x.map.Clear(), null, false);

            CacheEntry BuildMap(in BakingCell cell)
            {
                var entry = m_BrickMetaPool.Get();

                // Build a map from voxel to brick
                // A voxel is the size of a brick at subdivision level 0
                foreach (var brick in cell.bricks)
                {
                    int brick_size = ProbeReferenceVolume.CellSize(brick.subdivisionLevel);
                    Vector3Int brickMin = brick.position;
                    Vector3Int brickMax = brick.position + Vector3Int.one * brick_size;

                    for (int x = brickMin.x; x < brickMax.x; ++x)
                    {
                        for (int z = brickMin.z; z < brickMax.z; ++z)
                        {
                            for (int y = brickMin.y; y < brickMax.y; ++y)
                            {
                                entry.map[m_BakingBatch.GetBrickPositionHash(new Vector3Int(x, y, z))] = brick;
                            }
                        }
                    }
                }

                return entry;
            }

            public Dictionary<int, Brick> GetMap(in BakingCell cell)
            {
                if (!cache.TryGetValue(cell.index, out var entry))
                {
                    if (cache.Count >= k_MaxCellsCached)
                    {
                        int worst = 0;
                        int oldest = int.MaxValue;
                        foreach (var ce in cache)
                        {
                            if (ce.Value.access < oldest)
                            {
                                oldest = ce.Value.access;
                                worst = ce.Key;
                            }
                        }
                        m_BrickMetaPool.Release(cache[worst]);
                        cache.Remove(worst);
                    }

                    entry = BuildMap(cell);
                    cache[cell.index] = entry;
                }

                entry.access = ++accesses;
                return entry.map;
            }
        }

        internal static void FixSeams(
            NativeArray<int> positionRemap,
            NativeArray<Vector3> positions,
            NativeArray<SphericalHarmonicsL2> sh,
            NativeArray<float> validity,
            NativeArray<Vector4> probeOcclusion,
            NativeArray<Vector4> skyOcclusion,
            NativeArray<uint> renderingLayerMasks)
        {
            // Seams are caused are caused by probes on the boundary between two subdivision levels
            // The idea is to find first them and do a kind of dilation to smooth the values on the boundary
            // the dilation process consits in doing a trilinear sample of the higher subdivision brick and override the lower subdiv with that
            // We have to mark the probes on the boundary as valid otherwise leak reduction at runtime will interfere with this method

            bool doProbeOcclusion = probeOcclusion.IsCreated && probeOcclusion.Length > 0;
            bool doSkyOcclusion = skyOcclusion.IsCreated && skyOcclusion.Length > 0;

            // Use an indirection structure to ensure mem usage stays reasonable
            VoxelToBrickCache cache = new VoxelToBrickCache();

            // Create a map from cell position to index for fast lookup across cells
            var cellPositionToIndex = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < m_BakingBatch.cells.Count; i++)
                cellPositionToIndex[m_BakingBatch.cells[i].position] = i;

            for (int c = 0; c < m_BakingBatch.cells.Count; c++)
            {
                var cell = m_BakingBatch.cells[c];
                var voxelToBrick = cache.GetMap(cell);

                float scale = m_ProfileInfo.minBrickSize / ProbeBrickPool.kBrickCellCount;
                float minBrickSize = m_ProfileInfo.minBrickSize;
                Brick largestBrick = default;

                int numProbes = cell.probePositions.Length;
                for (int probeIndex = 0; probeIndex < numProbes; ++probeIndex)
                {
                    int i = positionRemap[cell.probeIndices[probeIndex]];
                    int minSubdiv = ProbeBrickIndex.kMaxSubdivisionLevels;
                    int maxSubdiv = -1;

                    Vector3 pos = positions[i] - m_ProfileInfo.probeOffset;

                    // 1.
                    // For each unique probe, find bricks from all 8 neighbouring voxels
                    for (int o = 0; o < 8; o++)
                    {
                        Vector3 sampleOffset = m_ProfileInfo.minDistanceBetweenProbes * (Vector3)GetSampleOffset(o);
                        Vector3Int voxel = Vector3Int.FloorToInt((pos - sampleOffset) / minBrickSize);
                        int hashCode = m_BakingBatch.GetBrickPositionHash(voxel);
                        if (!voxelToBrick.TryGetValue(hashCode, out var brick))
                        {
                            // If the brick was not found in the current cell, find it in the neighbouring cells
                            Vector3Int GetCellPositionFromVoxel(Vector3Int voxelToLookup, int cellSizeInBricks)
                            {
                                return new Vector3Int(
                                    FloorDivide(voxelToLookup.x, cellSizeInBricks),
                                    FloorDivide(voxelToLookup.y, cellSizeInBricks),
                                    FloorDivide(voxelToLookup.z, cellSizeInBricks)
                                );
                                int FloorDivide(int a, int b) => a >= 0 ? a / b : (a - b + 1) / b;
                            }

                            // Find the position of the neighbouring cell that would contain the voxel
                            bool foundInOtherCell = false;
                            var cellToLookupPos = GetCellPositionFromVoxel(voxel, m_ProfileInfo.cellSizeInBricks);
                            if(cellPositionToIndex.TryGetValue(cellToLookupPos, out var cellIndex))
                            {
                                var currentCell = m_BakingBatch.cells[cellIndex];
                                var voxelToBrickNeighbouringCell = cache.GetMap(currentCell);
                                if (voxelToBrickNeighbouringCell.TryGetValue(hashCode, out brick))
                                    foundInOtherCell = true;
                            }

                            if(!foundInOtherCell)
                                continue;
                        }

                        if (brick.subdivisionLevel > maxSubdiv)
                            largestBrick = brick;

                        minSubdiv = Mathf.Min(minSubdiv, brick.subdivisionLevel);
                        maxSubdiv = Mathf.Max(maxSubdiv, brick.subdivisionLevel);
                    }

                    // 2.
                    // There is a seam when a probe is part of two bricks with different subdiv level
                    if (minSubdiv >= maxSubdiv)
                        continue;

                    // 3.
                    // Overwrite lighting data with trilinear sampled data from the brick with highest subdiv level
                    float brickSize = ProbeReferenceVolume.instance.BrickSize(largestBrick.subdivisionLevel - 1);
                    float3 uvw = math.clamp((pos - (Vector3)largestBrick.position * minBrickSize) / brickSize, 0, 3);

                    var probe = Vector3Int.FloorToInt(uvw);
                    var fract = math.frac(uvw);

                    int brick_size = ProbeReferenceVolume.CellSize(largestBrick.subdivisionLevel);
                    Vector3Int brickOffset = largestBrick.position * ProbeBrickPool.kBrickCellCount;

                    // We need to check if rendering layers masks were baked, since it happens in separate job
                    bool bakedRenderingLayerMasks = (renderingLayerMasks.IsCreated & renderingLayerMasks.Length > 0);
                    uint probeRenderingLayerMask = 0;

                    if (bakedRenderingLayerMasks) probeRenderingLayerMask = renderingLayerMasks[i];

                    float weightSum = 0.0f;
                    SphericalHarmonicsL2 shTrilinear = default;
                    Vector4 probeOcclusionTrilinear = Vector4.zero;
                    Vector4 skyOcclusionTrilinear = Vector4.zero;
                    for (int o = 0; o < 8; o++)
                    {
                        Vector3Int offset = GetSampleOffset(o);

                        // We need to make sure probe positions are computed in the same way as in ConvertBricksToPositions
                        // Otherwise floating point imprecision could give a different position hash
                        Vector3Int probeOffset = brickOffset + (probe + offset) * brick_size;
                        int probeHash = m_BakingBatch.GetProbePositionHash(m_ProfileInfo.probeOffset + (Vector3)probeOffset * scale);

                        if (m_BakingBatch.positionToIndex.TryGetValue(probeHash, out var index))
                        {
                            bool valid = validity[positionRemap[index]] <= k_MinValidityForLeaking;
                            if (!valid) continue;

                            if (bakedRenderingLayerMasks)
                            {
                                uint renderingLayerMask = renderingLayerMasks[positionRemap[index]];
                                bool commonRenderingLayer = (renderingLayerMask & probeRenderingLayerMask) != 0;
                                if (!commonRenderingLayer) continue; // We do not use this probe contribution if it does not share at least a common rendering layer
                            }

                            // Do the lerp in compressed format to match result on GPU
                            var shSample = sh[positionRemap[index]];
                            BakingCell.CompressSH(ref shSample, 1.0f, false);

                            float trilinearW =
                                ((offset.x == 1) ? fract.x : 1.0f - fract.x) *
                                ((offset.y == 1) ? fract.y : 1.0f - fract.y) *
                                ((offset.z == 1) ? fract.z : 1.0f - fract.z);

                            shTrilinear += shSample * trilinearW;

                            if (doProbeOcclusion)
                                probeOcclusionTrilinear += probeOcclusion[positionRemap[index]] * trilinearW;

                            if (doSkyOcclusion)
                                skyOcclusionTrilinear += skyOcclusion[positionRemap[index]] * trilinearW;

                            weightSum += trilinearW;
                        }
                    }

                    if (weightSum != 0.0f)
                    {
                        shTrilinear *= 1.0f / weightSum;
                        BakingCell.DecompressSH(ref shTrilinear);
                        sh[i] = shTrilinear;

                        if (doProbeOcclusion)
                        {
                            probeOcclusionTrilinear *= 1.0f / weightSum;
                            probeOcclusion[i] = probeOcclusionTrilinear;
                        }

                        if (doSkyOcclusion)
                        {
                            skyOcclusionTrilinear *= 1.0f / weightSum;
                            skyOcclusion[i] = skyOcclusionTrilinear;
                        }

                        validity[i] = k_MinValidityForLeaking;
                    }
                }
            }
        }

        static void ApplyPostBakeOperations()
        {
            var probeRefVolume = ProbeReferenceVolume.instance;

            // Clear baked data
            foreach (var data in probeRefVolume.perSceneDataList)
                data.QueueSceneRemoval();
            probeRefVolume.Clear();

            // Make sure all pending operations are done (needs to be after the Clear to unload all previous scenes)
            probeRefVolume.PerformPendingOperations();
            probeRefVolume.SetSubdivisionDimensions(m_ProfileInfo.minBrickSize, m_ProfileInfo.maxSubdivision, m_ProfileInfo.probeOffset);

            // Use the globalBounds we just computed, as the one in probeRefVolume doesn't include scenes that have never been baked
            probeRefVolume.globalBounds = globalBounds;

            // Validate baking cells size before any state modifications
            var bakingCellsArray = m_BakedCells.Values.ToArray();
            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
            var hasVirtualOffsets = m_BakingSet.settings.virtualOffsetSettings.useVirtualOffset;
            var hasRenderingLayers = m_BakingSet.useRenderingLayers;
            
            if (!ValidateBakingCellsSize(bakingCellsArray, chunkSizeInProbes, hasVirtualOffsets, hasRenderingLayers))
                return; // Early exit if validation fails

            PrepareCellsForWriting(isBakingSceneSubset);

            m_BakingSet.chunkSizeInBricks = ProbeBrickPool.GetChunkSizeInBrickCount();
            m_BakingSet.minCellPosition = minCellPosition;
            m_BakingSet.maxCellPosition = maxCellPosition;
            m_BakingSet.globalBounds = globalBounds;
            m_BakingSet.maxSHChunkCount = -1;

            m_BakingSet.scenarios.TryAdd(m_BakingSet.lightingScenario, new ProbeVolumeBakingSet.PerScenarioDataInfo());

            // Attempt to convert baking cells to runtime cells
            bool succeededWritingBakingCells;
            using (new BakingCompleteProfiling(BakingCompleteProfiling.Stages.WriteBakedData))
                succeededWritingBakingCells = WriteBakingCells(m_BakedCells.Values.ToArray());

            if (!succeededWritingBakingCells)
                return;

            // Reset internal structures depending on current bake.
            Debug.Assert(probeRefVolume.EnsureCurrentBakingSet(m_BakingSet));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            probeRefVolume.clearAssetsOnVolumeClear = false;

            if (m_BakingSet.hasDilation)
            {
                // This subsequent block needs to happen AFTER we call WriteBakingCells.
                // Otherwise, in cases where we change the spacing between probes, we end up loading cells with a certain layout in ForceSHBand
                // And then we unload cells using the wrong layout in PerformDilation (after WriteBakingCells updates the baking set object) which leads to a broken internal state.

                // Don't use Disk streaming to avoid having to wait for it when doing dilation.
                probeRefVolume.ForceNoDiskStreaming(true);
                // Increase the memory budget to make sure we can fit the current cell and all its neighbors when doing dilation.
                var prevMemoryBudget = probeRefVolume.memoryBudget;
                probeRefVolume.ForceMemoryBudget(ProbeVolumeTextureMemoryBudget.MemoryBudgetHigh);
                // Force maximum sh bands to perform baking, we need to store what sh bands was selected from the settings as we need to restore it after.
                var prevSHBands = probeRefVolume.shBands;
                probeRefVolume.ForceSHBand(ProbeVolumeSHBands.SphericalHarmonicsL2);

                // Do it now otherwise it messes the loading bar
                InitDilationShaders();

                using (new BakingCompleteProfiling(BakingCompleteProfiling.Stages.PerformDilation))
                    PerformDilation();

                // Restore the original state.
                probeRefVolume.ForceNoDiskStreaming(false);
                probeRefVolume.ForceMemoryBudget(prevMemoryBudget);
                probeRefVolume.ForceSHBand(prevSHBands);
            }
            else
            {
                foreach (var data in probeRefVolume.perSceneDataList)
                    data.Initialize();

                probeRefVolume.PerformPendingOperations();
            }

            // Mark stuff as up to date
            m_BakingBatch?.Dispose();
            m_BakingBatch = null;
            foreach (var probeVolume in GetProbeVolumeList())
                probeVolume.OnBakeCompleted();
            foreach (var adjustment in s_AdjustmentVolumes)
                adjustment.volume.cachedHashCode = adjustment.volume.GetHashCode();

            // We allocate data even if dilation is off, that should be changed
            FinalizeDilation();
        }


        static int s_AsyncBakeTaskID = -1;
        internal static void AsyncBakeCallback()
        {
            float progress = 0.0f;
            bool done = false;
            BakeDelegate(ref progress, ref done);
            Progress.Report(s_AsyncBakeTaskID, progress, s_BakeData.step.ToString());

            if (done)
            {
                UpdateLightStatus();
                Progress.Remove(s_AsyncBakeTaskID);

                EditorApplication.update -= AsyncBakeCallback;
                s_AsyncBakeTaskID = -1;
            }
        }

        /// <summary>
        /// Starts an asynchronous bake job for Adaptive Probe Volumes.
        /// </summary>
        /// <returns>Returns true if the bake was successfully started.</returns>
        public static bool BakeAsync()
        {
            if (Lightmapping.isRunning || AdaptiveProbeVolumes.isRunning || !PrepareBaking())
                return false;

            s_AsyncBakeTaskID = Progress.Start("Bake Adaptive Probe Volumes");
            Progress.RegisterCancelCallback(s_AsyncBakeTaskID, () =>
            {
                OnBakeCancelled();
                EditorApplication.update -= AsyncBakeCallback;
                s_AsyncBakeTaskID = -1;
                return true;
            });

            EditorApplication.update += AsyncBakeCallback;
            return true;
        }

        /// <summary>
        /// Returns true when the async baking of adaptive probe volumes only is running, false otherwise (Read Only).
        /// </summary>
        public static bool isRunning => s_AsyncBakeTaskID != -1;

        /// <summary>
        /// Cancels the currently running asynchronous bake job.
        /// </summary>
        /// <returns>Returns true if baking was successfully cancelled.</returns>
        public static bool Cancel() => Progress.Cancel(s_AsyncBakeTaskID);

        /// <summary>
        /// Request additional bake request manager to recompute baked data for an array of requests
        /// </summary>
        /// <param name="probeInstanceIDs">Array of instance IDs of the probes doing the request.</param>
        public static void BakeAdditionalRequests(int[] probeInstanceIDs)
        {
            List<int> validProbeInstanceIDs = new List<int>();
            List<Vector3> positions = new List<Vector3>();
            foreach (var probeInstanceID in probeInstanceIDs)
            {
                if (AdditionalGIBakeRequestsManager.GetPositionForRequest(probeInstanceID, out var position))
                {
                    validProbeInstanceIDs.Add(probeInstanceID);
                    positions.Add(position);
                }
            }

            int numValidProbes = validProbeInstanceIDs.Count;
            if (numValidProbes > 0)
            {
                SphericalHarmonicsL2[] sh = new SphericalHarmonicsL2[numValidProbes];
                float[] validity = new float[numValidProbes];

                // Bake all probes in a single batch
                BakeProbes(positions.ToArray(), sh, validity);

                for (int probeIndex = 0; probeIndex < numValidProbes; ++probeIndex)
                {
                    AdditionalGIBakeRequestsManager.SetSHCoefficients(validProbeInstanceIDs[probeIndex], sh[probeIndex], validity[probeIndex]);
                }
            }
        }

        /// <summary>
        /// Request additional bake request manager to recompute baked data for a given request
        /// </summary>
        /// <param name="probeInstanceID">The instance ID of the probe doing the request.</param>
        public static void BakeAdditionalRequest(int probeInstanceID)
        {
            int[] probeInstanceIDs = new int[1];
            probeInstanceIDs[0] = probeInstanceID;

            BakeAdditionalRequests(probeInstanceIDs);
        }

        static RenderingLayerBaker renderingLayerOverride = null;
        static VirtualOffsetBaker virtualOffsetOverride = null;
        static SkyOcclusionBaker skyOcclusionOverride = null;
        static LightingBaker lightingOverride = null;

        /// <summary>Used to override the virtual offset baking system.</summary>
        /// <param name="baker">The baker override or null to use the default system.</param>
        public static void SetVirtualOffsetBakerOverride(VirtualOffsetBaker baker)
        {
            virtualOffsetOverride = baker;
        }
        /// <summary>Used to override the lighting baking system.</summary>
        /// <param name="baker">The baker override or null to use the default system.</param>
        public static void SetLightingBakerOverride(LightingBaker baker)
        {
            lightingOverride = baker;
        }
        /// <summary>Used to override the sky occlusion baking system.</summary>
        /// <param name="baker">The baker override or null to use the default system.</param>
        public static void SetSkyOcclusionBakerOverride(SkyOcclusionBaker baker)
        {
            skyOcclusionOverride = baker;
        }

        /// <summary>Used to override the virtual offset baking system.</summary>
        /// <returns>The baker override or null if none is set.</returns>
        public static VirtualOffsetBaker GetVirtualOffsetBakerOverride()
        {
            return virtualOffsetOverride;
        }
        /// <summary>Used to override the lighting baking system.</summary>
        /// <returns>The baker override or null if none is set.</returns>
        public static LightingBaker GetLightingBakerOverride()
        {
            return lightingOverride;
        }
        /// <summary>Get the current sky occlusion baker override</summary>
        /// <returns>The baker override or null if none is set.</returns>
        public static SkyOcclusionBaker GetSkyOcclusionBakerOverride()
        {
            return skyOcclusionOverride;
        }
    }
}
