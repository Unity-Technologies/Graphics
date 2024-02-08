using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using UnityEditor;

using UnityEngine.Assertions;
using UnityEngine.LightTransport;
using UnityEngine.LightTransport.PostProcessing;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;
using TouchupVolumeWithBoundsList = System.Collections.Generic.List<(UnityEngine.Rendering.ProbeReferenceVolume.Volume obb, UnityEngine.Bounds aabb, UnityEngine.Rendering.ProbeAdjustmentVolume volume)>;

namespace UnityEngine.Rendering
{
    partial class ProbeGIBaking
    {
        enum BakingStep
        {
            VirtualOffset,
            LaunchThread,
            SkyOcclusion,
            Integration,
            FinalizeCells,

            Last = FinalizeCells + 1
        }

        struct BakeData
        {
            // Inputs
            public BakeJob[] jobs;
            public NativeList<Vector3> positions;
            public InputExtraction.BakeInput input;
            public List<Vector3> additionalRequests;
            public NativeArray<Vector3> sortedPositions;

            // Workers
            public Thread bakingThread;
            public VirtualOffsetBaking virtualOffsetJob;
            public SkyOcclusionBaking skyOcclusionJob;
            public int cellIndex;

            // Outputs
            public NativeArray<SphericalHarmonicsL2> irradianceResults;
            public NativeArray<float> validityResults;

            // Progress reporting
            public BakingStep step;
            public int bakedProbeCount;
            public int totalProbeCount;
            public ulong stepCount;

            // Cancellation
            public bool cancel;
            public bool failed;

            public void Init(ProbeVolumeBakingSet bakingSet, BakeJob[] bakeJobs, NativeList<Vector3> probePositions, List<Vector3> requests)
            {
                var result = InputExtraction.ExtractFromScene(out input);
                Assert.IsTrue(result, "InputExtraction.ExtractFromScene failed.");

                jobs = bakeJobs;
                positions = probePositions;
                additionalRequests = requests;

                virtualOffsetJob.Initialize(bakingSet, probePositions);
                skyOcclusionJob.Initialize(bakingSet, jobs, positions.Length);
                cellIndex = 0;

                bakedProbeCount = 0;
                totalProbeCount = probePositions.Length + requests.Count;
                stepCount = virtualOffsetJob.stepCount + (ulong)totalProbeCount + skyOcclusionJob.stepCount;

                irradianceResults = new NativeArray<SphericalHarmonicsL2>(totalProbeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                validityResults = new NativeArray<float>(totalProbeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                step = BakingStep.VirtualOffset;
            }

            public void StartThread()
            {
                sortedPositions = new NativeArray<Vector3>(positions.Length + additionalRequests.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                SortPositions(s_BakeData.jobs, positions, virtualOffsetJob.offsets, additionalRequests, sortedPositions);

                bakingThread = new Thread(BakeThread);
                bakingThread.Start();
            }

            public bool Done()
            {
                ulong currentStep = s_BakeData.virtualOffsetJob.currentStep + (ulong)s_BakeData.bakedProbeCount + s_BakeData.skyOcclusionJob.currentStep;
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

                positions.Dispose();
                if (sortedPositions.IsCreated)
                    sortedPositions.Dispose();

                virtualOffsetJob.Dispose();
                skyOcclusionJob.Dispose();

                irradianceResults.Dispose();
                validityResults.Dispose();

                // clear references to managed arrays
                this = default;
            }
        }

        static APVRTContext s_TracingContext;
        static BakeData s_BakeData;
        static TouchupVolumeWithBoundsList s_AdjustmentVolumes;

        internal class LightTransportBakingProfiling : BakingProfiling<LightTransportBakingProfiling.Stages>, IDisposable
        {
            //protected override string LogFile => "BakeGI";
            protected override bool ShowProgressBar => false;

            public enum Stages
            {
                BakeGI,
                IntegrateDirectRadiance,
                IntegrateIndirectRadiance,
                IntegrateValidity,
                Postprocess,
                ReadBack,
                None
            }

            static Stages currentStage = Stages.None;
            public LightTransportBakingProfiling(Stages stage) : base(stage, ref currentStage) { }
            public override Stages GetLastStep() => Stages.None;
            public static void GetProgressRange(out float progress0, out float progress1) { float s = 1 / (float)Stages.None; progress0 = (float)currentStage * s; progress1 = progress0 + s; }
            public void Dispose() { OnDispose(ref currentStage); }
        }

        internal static bool PrepareBaking()
        {
            BakeJob[] jobs;
            NativeList<Vector3> positions;
            List<Vector3> requests;
            int additionalRequests;
            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.OnBakeStarted))
            {
                if (!InitializeBake())
                    return false;

                s_AdjustmentVolumes = GetAdjustementVolumes();

                requests = AdditionalGIBakeRequestsManager.GetProbeNormalizationRequests();
                additionalRequests = requests.Count;

                jobs = CreateBakingJobs(m_BakingSet, requests.Count != 0);
                var regularJobs = jobs.AsSpan(0, requests.Count != 0 ? jobs.Length - 1 : jobs.Length);

                // Note: this could be executed in the baking delegate to be non blocking
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.PlaceProbes))
                    positions = RunPlacement(regularJobs);

                if (positions.Length == 0)
                {
                    positions.Dispose();
                    Clear();
                    CleanBakeData();
                    return false;
                }
            }

            s_BakeData.Init(m_BakingSet, jobs, positions, requests);
            return true;
        }

        static void BakeDelegate(ref float progress, ref bool done)
        {
            if (s_BakeData.step == BakingStep.VirtualOffset)
            {
                if (s_BakeData.virtualOffsetJob.RunVirtualOffsetStep())
                    s_BakeData.step++;
            }

            if (s_BakeData.step == BakingStep.LaunchThread)
            {
                s_BakeData.StartThread();
                s_BakeData.skyOcclusionJob.StartBaking(s_BakeData.sortedPositions);

                s_BakeData.step++;
            }

            if (s_BakeData.step == BakingStep.SkyOcclusion)
            {
                if (s_BakeData.skyOcclusionJob.RunSkyOcclusionStep())
                    s_BakeData.step++;
            }

            if (s_BakeData.step == BakingStep.Integration)
            {
                if (s_BakeData.bakedProbeCount >= s_BakeData.totalProbeCount)
                    s_BakeData.step++;
            }

            if (s_BakeData.step == BakingStep.FinalizeCells)
            {
                FinalizeCell(s_BakeData.cellIndex++, s_BakeData.irradianceResults, s_BakeData.validityResults,
                    s_BakeData.virtualOffsetJob.offsets,
                    s_BakeData.skyOcclusionJob.occlusionResults, s_BakeData.skyOcclusionJob.directionResults);

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

            // Use LightTransport progress to have async report on baking progress
            ulong currentStep = s_BakeData.virtualOffsetJob.currentStep + s_BakeData.skyOcclusionJob.currentStep;
            foreach (var job in s_BakeData.jobs) currentStep += job.currentStep;
            progress = currentStep / (float)s_BakeData.stepCount;

            // Use our counter to determine when baking is done
            if (s_BakeData.Done())
            {
                FinalizeBake();
                done = true;
            }
        }

        static void BakeThread()
        {
            var context = BakeContext.New(s_BakeData.input, s_BakeData.sortedPositions);

            try
            {
                for (int i = 0; i < s_BakeData.jobs.Length; i++)
                {
                    ref var job = ref s_BakeData.jobs[i];
                    if (job.indices.Length != 0)
                    {
                        bool success = context.Bake(job, ref s_BakeData.irradianceResults, ref s_BakeData.validityResults, ref s_BakeData.cancel);
                        if (success)
                            s_BakeData.bakedProbeCount += job.indices.Length;
                        s_BakeData.failed = !success;
                    }
                }
            }
            finally
            {
                context.Dispose();
            }
        }

        static void UpdateLightStatus()
        {
            var lightingSettings = ProbeVolumeLightingTab.GetLightingSettings();

            // The contribution from all Baked and Mixed lights in the scene should be disabled to avoid double contribution.
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.lightmapBakeType != LightmapBakeType.Realtime)
                {
                    var bakingOutput = light.bakingOutput;
                    bakingOutput.isBaked = true;
                    bakingOutput.lightmapBakeType = light.lightmapBakeType;
                    bakingOutput.mixedLightingMode = lightingSettings.mixedBakeMode;
                    light.bakingOutput = bakingOutput;
                }
            }
        }

        static void FinalizeBake()
        {
            using (new BakingCompleteProfiling(BakingCompleteProfiling.Stages.FinalizingBake))
            {
                int probeCount = s_BakeData.positions.Length;
                int requestCount = s_BakeData.additionalRequests.Count;

                if (probeCount != 0)
                {
                    try
                    {
                        ApplyPostBakeOperations(s_BakeData.irradianceResults, s_BakeData.validityResults,
                            s_BakeData.virtualOffsetJob.offsets,
                            s_BakeData.skyOcclusionJob.occlusionResults, s_BakeData.skyOcclusionJob.directionResults);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                if (requestCount != 0)
                {
                    var additionalIrradiance = s_BakeData.irradianceResults.GetSubArray(s_BakeData.irradianceResults.Length - requestCount, requestCount);
                    var additionalValidity = s_BakeData.validityResults.GetSubArray(s_BakeData.validityResults.Length - requestCount, requestCount);
                    AdditionalGIBakeRequestsManager.OnAdditionalProbesBakeCompleted(additionalIrradiance, additionalValidity);
                }
            }

            CleanBakeData();

            // We need to reset that view
            ProbeReferenceVolume.instance.ResetDebugViewToMaxSubdiv();
        }

        static void OnBakeCancelled()
        {
            if (s_BakeData.bakingThread != null)
            {
                s_BakeData.cancel = true;
                s_BakeData.bakingThread.Join();
            }

            CleanBakeData();
        }

        static void CleanBakeData()
        {
            s_BakeData.Dispose();
            m_BakingBatch = null;
            s_AdjustmentVolumes = null;

            // If lighting pannel is not created, we have to dispose ourselves
            if (ProbeVolumeLightingTab.instance == null)
                ProbeGIBaking.Dispose();

            Lightmapping.ResetAdditionalBakeDelegate();

            partialBakeSceneList = null;
            ProbeReferenceVolume.instance.checksDuringBakeAction = null;
        }

        static internal void Dispose()
        {
            s_TracingContext.Dispose();
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
                touchupVolumesAndBounds.Sort((a, b) => (a.aabb.size.x * a.aabb.size.y * a.aabb.size.z).CompareTo(b.aabb.size.x * b.aabb.size.y * b.aabb.size.z));
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

        // Place positions contiguously for each bake job in a single array and apply virtual offsets
        static void SortPositions(BakeJob[] jobs, NativeList<Vector3> positions, Vector3[] offsets, List<Vector3> requests, NativeArray<Vector3> sortedPositions)
        {
            int regularJobCount = requests.Count != 0 ? jobs.Length - 1 : jobs.Length;

            // Construct position arrays
            int currentOffset = 0;
            for (int i = 0; i < regularJobCount; i++)
            {
                ref var job = ref jobs[i];
                var indices = job.indices;
                for (int j = 0; j < indices.Length; j++)
                {
                    var pos = positions[indices[j]];
                    if (offsets != null) pos += offsets[indices[j]];

                    sortedPositions[currentOffset + j] = pos;
                }

                job.startOffset = currentOffset;
                currentOffset += indices.Length;
            }

            Debug.Assert(currentOffset == positions.Length);

            if (requests.Count != 0)
            {
                ref var requestJob = ref jobs[jobs.Length - 1];
                requestJob.startOffset = currentOffset;
                for (int i = 0; i < requests.Count; i++)
                {
                    requestJob.indices.Add(currentOffset);
                    sortedPositions[currentOffset++] = requests[i];
                }

                Debug.Assert(currentOffset == sortedPositions.Length);
            }
        }

        struct BakeJob
        {
            public Bounds aabb;
            public ProbeReferenceVolume.Volume obb;
            public ProbeAdjustmentVolume touchup;

            public int startOffset;
            public NativeList<int> indices;

            public int directSampleCount;
            public int indirectSampleCount;
            public int validitySampleCount;
            public int maxBounces;

            public int skyOcclusionBakingSamples;
            public int skyOcclusionBakingBounces;

            public float indirectScale;
            public bool ignoreEnvironement;

            public BakeProgressState progress;
            public ulong currentStep => (ulong)Mathf.Min(progress.Progress() * 0.01f / (float)(directSampleCount + indirectSampleCount + validitySampleCount), stepCount); // this is how the progress is computed in c++
            public ulong stepCount => (ulong)indices.Length;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Create(ProbeVolumeBakingSet bakingSet, LightingSettings lightingSettings, bool ignoreEnvironement)
            {
                skyOcclusionBakingSamples = bakingSet != null ? bakingSet.skyOcclusionBakingSamples : 0;
                skyOcclusionBakingBounces = bakingSet != null ? bakingSet.skyOcclusionBakingBounces : 0;

                int indirectSampleCount = Math.Max(lightingSettings.indirectSampleCount, lightingSettings.environmentSampleCount);
                Create(lightingSettings, ignoreEnvironement, lightingSettings.directSampleCount, indirectSampleCount,
                    (int)lightingSettings.lightProbeSampleCountMultiplier, lightingSettings.maxBounces);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Create(LightingSettings lightingSettings, bool ignoreEnvironement, (ProbeReferenceVolume.Volume obb, Bounds aabb, ProbeAdjustmentVolume touchup) volume)
            {
                obb = volume.obb;
                aabb = volume.aabb;
                touchup = volume.touchup;

                skyOcclusionBakingSamples = touchup.skyOcclusionSampleCount;
                skyOcclusionBakingBounces = touchup.skyOcclusionMaxBounces;

                Create(lightingSettings, ignoreEnvironement, touchup.directSampleCount, touchup.indirectSampleCount, touchup.sampleCountMultiplier, touchup.maxBounces);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Create(LightingSettings lightingSettings, bool ignoreEnvironement, int directSampleCount, int indirectSampleCount, int sampleCountMultiplier, int maxBounces)
            {
                // We could preallocate wrt touchup aabb volume, or total brick count for the global job
                indices = new NativeList<int>(Allocator.Persistent);
                progress = new BakeProgressState();

                this.directSampleCount = directSampleCount * sampleCountMultiplier;
                this.indirectSampleCount = indirectSampleCount * sampleCountMultiplier;
                this.validitySampleCount = indirectSampleCount * sampleCountMultiplier;
                this.maxBounces = maxBounces;

                this.indirectScale = lightingSettings.indirectScale;
                this.ignoreEnvironement = ignoreEnvironement;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Contains(Vector3 point)
            {
                return touchup.ContainsPoint(obb, aabb.center, point);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                indices.Dispose();
                progress.Dispose();
            }
        }

        struct BakeContext
        {
            public IDeviceContext ctx;
            public IProbeIntegrator integrator;
            public IWorld world;
            public IProbePostProcessor postProcessor;

            public BufferID positionsBufferID;
            public BufferID directRadianceBufferId;
            public BufferID indirectRadianceBufferId;
            public BufferID validityBufferId;

            public BufferID windowedDirectSHBufferId;
            public BufferID boostedIndirectSHBufferId;
            public BufferID combinedSHBufferId;
            public BufferID irradianceBufferId;

            NativeArray<SphericalHarmonicsL2> jobIrradianceResults;
            NativeArray<float> jobValidityResults;

            const float k_PushOffset = 0.0001f;
            const int k_MaxProbeCountPerBatch = 128 * 1024;

            static readonly int sizeOfFloat = 4;
            static readonly int SHL2RGBElements = 3 * 9;
            static readonly int sizeSHL2RGB = sizeOfFloat * SHL2RGBElements;

            public static BakeContext New(InputExtraction.BakeInput input, NativeArray<Vector3> probePositions)
            {
                var ctx = new BakeContext
                {
                    ctx = new RadeonRaysContext(),
                    integrator = new RadeonRaysProbeIntegrator(),
                    world = new RadeonRaysWorld(),
                    postProcessor = new RadeonRaysProbePostProcessor(),
                };

                var contextInitialized = ctx.ctx.Initialize();
                Assert.AreEqual(true, contextInitialized);

                using var inputProgress = new BakeProgressState();
                var worldResult = InputExtraction.PopulateWorld(input, inputProgress, ctx.ctx, ctx.world);
                Assert.IsTrue(worldResult, "PopulateWorld failed.");

                var postProcessInit = ctx.postProcessor.Initialize(ctx.ctx);
                Assert.IsTrue(postProcessInit);

                ctx.CreateBuffers(probePositions.Length);

                // Upload probe positions
                var positionsSlice = new BufferSlice<Vector3>(ctx.positionsBufferID, 0);
                ctx.ctx.WriteBuffer(positionsSlice, probePositions);

                return ctx;
            }

            private void CreateBuffers(int probeCount)
            {
                // Allocate shared position buffer for all jobs
                var positionsBytes = (ulong)(sizeOfFloat * 3 * probeCount);
                positionsBufferID = ctx.CreateBuffer(positionsBytes);

                int batchSize = Mathf.Min(k_MaxProbeCountPerBatch, probeCount);
                var shBytes = (ulong)(sizeSHL2RGB * batchSize);
                var validityBytes = (ulong)(sizeOfFloat * batchSize);

                directRadianceBufferId = ctx.CreateBuffer(shBytes);
                indirectRadianceBufferId = ctx.CreateBuffer(shBytes);
                validityBufferId = ctx.CreateBuffer(validityBytes);

                windowedDirectSHBufferId = ctx.CreateBuffer(shBytes);
                boostedIndirectSHBufferId = ctx.CreateBuffer(shBytes);
                combinedSHBufferId = ctx.CreateBuffer(shBytes);
                irradianceBufferId = ctx.CreateBuffer(shBytes);

                jobIrradianceResults = new NativeArray<SphericalHarmonicsL2>(batchSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                jobValidityResults = new NativeArray<float>(batchSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            public bool Bake(in BakeJob job, ref NativeArray<SphericalHarmonicsL2> irradianceResults, ref NativeArray<float> validityResults, ref bool cancel)
            {
                // Divide the job into batches of 128k probes to reduce memory usage.
                int batchCount = CoreUtils.DivRoundUp(job.indices.Length, k_MaxProbeCountPerBatch);

                // Get slices for all buffers because the API require those
                // All jobs use overlapping slices as they are not run simultaneously
                var directRadianceSlice = new BufferSlice<SphericalHarmonicsL2>(directRadianceBufferId, 0);
                var indirectRadianceSlice = new BufferSlice<SphericalHarmonicsL2>(indirectRadianceBufferId, 0);
                var validitySlice = new BufferSlice<float>(validityBufferId, 0);
                var windowedDirectRadianceSlice = new BufferSlice<SphericalHarmonicsL2>(windowedDirectSHBufferId, 0);
                var boostedIndirectRadianceSlice = indirectRadianceSlice;
                var combinedSHSlice = new BufferSlice<SphericalHarmonicsL2>(combinedSHBufferId, 0);
                var irradianceSlice = new BufferSlice<SphericalHarmonicsL2>(irradianceBufferId, 0);

                // Loop over all batches
                for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
                {
                    int batchOffset = batchIndex * k_MaxProbeCountPerBatch;
                    int probeCount = Mathf.Min(job.indices.Length - batchOffset, k_MaxProbeCountPerBatch);

                    // Get the correct slice of position as all jobs share the same array.
                    var positionsSlice = new BufferSlice<Vector3>(positionsBufferID, (ulong)(job.startOffset + batchOffset));

                    /// Baking

                    // Prepare integrator.
                    integrator.Prepare(ctx, world, positionsSlice, k_PushOffset, job.maxBounces);
                    integrator.SetProgressReporter(job.progress);

                    // Bake direct radiance
                    using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.IntegrateDirectRadiance))
                    {
                        var integrationResult = integrator.IntegrateDirectRadiance(ctx, 0, probeCount, job.directSampleCount, job.ignoreEnvironement, job.ignoreEnvironement, directRadianceSlice);
                        if (integrationResult.type != IProbeIntegrator.ResultType.Success) return false;
                        if (cancel) return true;
                    }

                    // Bake indirect radiance
                    using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.IntegrateIndirectRadiance))
                    {
                        var integrationResult = integrator.IntegrateIndirectRadiance(ctx, 0, probeCount, job.indirectSampleCount, job.ignoreEnvironement, job.ignoreEnvironement, indirectRadianceSlice);
                        if (integrationResult.type != IProbeIntegrator.ResultType.Success) return false;
                        if (cancel) return true;
                    }

                    // Bake validity
                    using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.IntegrateValidity))
                    {
                        var validityResult = integrator.IntegrateValidity(ctx, 0, probeCount, job.validitySampleCount, validitySlice);
                        if (validityResult.type != IProbeIntegrator.ResultType.Success) return false;
                        if (cancel) return true;
                    }

                    /// Postprocess

                    using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.Postprocess))
                    {
                        // Apply windowing to direct component.
                        if (!postProcessor.WindowSphericalHarmonicsL2(ctx, directRadianceSlice, windowedDirectRadianceSlice, probeCount))
                            return false;

                        // Apply indirect intensity multiplier to indirect radiance
                        if (job.indirectScale.Equals(1.0f) == false)
                        {
                            boostedIndirectRadianceSlice = new BufferSlice<SphericalHarmonicsL2>(boostedIndirectSHBufferId, 0);
                            if (!postProcessor.ScaleSphericalHarmonicsL2(ctx, indirectRadianceSlice, boostedIndirectRadianceSlice, probeCount, job.indirectScale))
                                return false;
                        }

                        // Combine direct and indirect radiance
                        if (!postProcessor.AddSphericalHarmonicsL2(ctx, windowedDirectRadianceSlice, boostedIndirectRadianceSlice, combinedSHSlice, probeCount))
                            return false;

                        // Convert radiance to irradiance
                        if (!postProcessor.ConvolveRadianceToIrradiance(ctx, combinedSHSlice, irradianceSlice, probeCount))
                            return false;

                        // Transform to the format expected by the Unity renderer
                        if (!postProcessor.ConvertToUnityFormat(ctx, irradianceSlice, combinedSHSlice, probeCount))
                            return false;

                        // Apply de-ringing to combined SH
                        if (!postProcessor.DeringSphericalHarmonicsL2(ctx, combinedSHSlice, combinedSHSlice, probeCount))
                            return false;
                    }

                    /// Read results

                    // Schedule read backs to get results back from GPU memory into CPU memory.
                    var irradianceReadEvent = ctx.ReadBuffer(combinedSHSlice, jobIrradianceResults);
                    var validityReadEvent = ctx.ReadBuffer(validitySlice, jobValidityResults);
                    if (!ctx.Flush()) return false;

                    using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.ReadBack))
                    {
                        // Wait for read backs to complete.
                        bool waitResult = ctx.Wait(irradianceReadEvent) && ctx.Wait(validityReadEvent);
                        if (!waitResult) return false;
                    }

                    // Write the batch results into the final buffer
                    for (int i = 0; i < probeCount; i++)
                    {
                        var dst = job.indices[i + batchOffset];
                        irradianceResults[dst] = jobIrradianceResults[i];
                        validityResults[dst] = jobValidityResults[i];
                    }

                    if (cancel)
                        return true;
                }

                return true;
            }

            public void Dispose()
            {
                jobIrradianceResults.Dispose();
                jobValidityResults.Dispose();

                ctx.DestroyBuffer(positionsBufferID);
                ctx.DestroyBuffer(directRadianceBufferId);
                ctx.DestroyBuffer(indirectRadianceBufferId);
                ctx.DestroyBuffer(validityBufferId);

                ctx.DestroyBuffer(windowedDirectSHBufferId);
                ctx.DestroyBuffer(boostedIndirectSHBufferId);
                ctx.DestroyBuffer(combinedSHBufferId);
                ctx.DestroyBuffer(irradianceBufferId);

                postProcessor.Dispose();
                world.Dispose();
                integrator.Dispose();
                ctx.Dispose();
            }
        }

        struct APVRTContext
        {
            RayTracingContext m_Context;
            RayTracingBackend m_Backend;
            SamplingResources m_SamplingResources;
            RayTracingResources m_RayTracingResources;

            static IRayTracingShader m_ShaderVO = null;
            static IRayTracingShader m_ShaderSO = null;

            internal IRayTracingAccelStruct CreateAccelerationStructure()
            {
                return context.CreateAccelerationStructure(new AccelerationStructureOptions
                {
                    // Use PreferFastBuild to avoid bug triggered with big meshes (UUM-52552));
                    buildFlags = BuildFlags.PreferFastBuild
                });
            }

            public RayTracingContext context
            {
                get
                {
                    if (m_Context == null)
                    {
                        m_RayTracingResources = new RayTracingResources();
                        m_RayTracingResources.Load();

                        m_Backend = RayTracingContext.IsBackendSupported(RayTracingBackend.Hardware) ? RayTracingBackend.Hardware : RayTracingBackend.Compute;

                        m_Context = new RayTracingContext(m_Backend, m_RayTracingResources);
                    }

                    return m_Context;
                }
            }

            public IRayTracingShader shaderVO
            {
                get
                {
                    if (m_ShaderVO == null)
                    {
                        var bakingResources = GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeBakingResources>();
                        m_ShaderVO = m_Context.CreateRayTracingShader(m_Backend switch
                        {
                            RayTracingBackend.Hardware => bakingResources.traceVirtualOffsetRT,
                            RayTracingBackend.Compute => bakingResources.traceVirtualOffsetCS,
                            _ => null
                        });
                    }

                    return m_ShaderVO;
                }
            }

            public IRayTracingShader shaderSO
            {
                get
                {
                    if (m_ShaderSO == null)
                    {
                        var bakingResources = GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeBakingResources>();
                        m_ShaderSO = m_Context.CreateRayTracingShader(m_Backend switch
                        {
                            RayTracingBackend.Hardware => bakingResources.skyOcclusionRT,
                            RayTracingBackend.Compute => bakingResources.skyOcclusionCS,
                            _ => null
                        });
                    }

                    return m_ShaderSO;
                }
            }

            public void BindSamplingTextures(CommandBuffer cmd)
            {
                if (m_SamplingResources == null)
                {
                    m_SamplingResources = new SamplingResources();
                    m_SamplingResources.Load();
                }

                SamplingResources.BindSamplingTextures(cmd, m_SamplingResources);
            }

            public void Dispose()
            {
                if (m_Context != null)
                {
                    m_Context.Dispose();
                    m_Context = null;
                }
            }
        }


        // Helper functions to bake a subset of the probes

        static int s_AsyncBakeTaskID = -1;
        internal static bool HasAsyncBakeInProgress() => s_AsyncBakeTaskID != -1;
        internal static bool CancelAsyncBake() => Progress.Cancel(s_AsyncBakeTaskID);

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

        internal static void BakeGI()
        {
            if (HasAsyncBakeInProgress() || !PrepareBaking())
                return;

            s_AsyncBakeTaskID = Progress.Start("Bake Adaptive Probe Volumes");
            Progress.RegisterCancelCallback(s_AsyncBakeTaskID, () =>
            {
                if (s_BakeData.bakingThread != null)
                {
                    s_BakeData.cancel = true;
                    s_BakeData.bakingThread.Join();
                }

                CleanBakeData();

                EditorApplication.update -= AsyncBakeCallback;
                s_AsyncBakeTaskID = -1;
                return true;
            });

            EditorApplication.update += AsyncBakeCallback;
        }

        internal static void BakeProbes(Vector3[] positionValues, SphericalHarmonicsL2[] shValues, float[] validityValues)
        {
            int numProbes = positionValues.Length;

            var job = new BakeJob();
            job.Create(null, ProbeVolumeLightingTab.GetLightingSettings(), false);

            for (int probeIndex = 0; probeIndex < numProbes; probeIndex++)
            {
                job.indices.Add(probeIndex);
            }

            var positionsInput = new NativeArray<Vector3>(numProbes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            positionsInput.CopyFrom(positionValues);

            var irradianceResults = new NativeArray<SphericalHarmonicsL2>(numProbes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var validityResults = new NativeArray<float>(numProbes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            InputExtraction.BakeInput input;
            InputExtraction.ExtractFromScene(out input);

            var context = BakeContext.New(input, positionsInput);
            bool cancel = false;
            context.Bake(job, ref irradianceResults, ref validityResults, ref cancel);
            job.Dispose();
            context.Dispose();

            irradianceResults.CopyTo(shValues);
            validityResults.CopyTo(validityValues);

            positionsInput.Dispose();
            irradianceResults.Dispose();
            validityResults.Dispose();
        }

        internal static void BakeSingleProbe(Vector3 position, out SphericalHarmonicsL2 sh, out float validity)
        {
            Vector3[] positionValues = new Vector3[1];
            positionValues[0] = position;

            SphericalHarmonicsL2[] shValues = new SphericalHarmonicsL2[1];
            float[] validityValues = new float[1];

            BakeProbes(positionValues, shValues, validityValues);

            sh = shValues[0];
            validity = validityValues[0];
        }

        internal static void BakeAdjustmentVolume(ProbeVolumeBakingSet bakingSet, ProbeAdjustmentVolume touchup)
        {
            var prv = ProbeReferenceVolume.instance;
            var scenario = bakingSet.lightingScenario;
            if (!bakingSet.scenarios.TryGetValue(scenario, out var scenarioData) || !scenarioData.ComputeHasValidData(prv.shBands))
            {
                Debug.LogError($"Lighting for scenario '{scenario}' is not baked. You need to Generate Lighting from the Lighting Window before updating baked data");
                return;
            }

            float cellSize = bakingSet.cellSizeInMeters;
            var cellCount = bakingSet.maxCellPosition + Vector3Int.one - bakingSet.minCellPosition;

            int savedLevels = bakingSet.simplificationLevels;
            float savedDistance = bakingSet.minDistanceBetweenProbes;
            bool savedSkyOcclusion = bakingSet.skyOcclusion;
            bool savedSkyDirection  = bakingSet.skyOcclusionShadingDirection;
            bool savedVirtualOffset = bakingSet.settings.virtualOffsetSettings.useVirtualOffset;
            {
                // Patch baking set as we are not gonna use a mix of baked values and new values
                bakingSet.simplificationLevels = bakingSet.bakedSimplificationLevels;
                bakingSet.minDistanceBetweenProbes = bakingSet.bakedMinDistanceBetweenProbes;
                bakingSet.skyOcclusion = bakingSet.bakedSkyOcclusion;
                bakingSet.skyOcclusionShadingDirection = bakingSet.bakedSkyShadingDirection;
                bakingSet.settings.virtualOffsetSettings.useVirtualOffset = bakingSet.supportOffsetsChunkSize != 0;

                m_BakingSet = bakingSet;
                m_BakingBatch = new BakingBatch(cellCount);
                m_ProfileInfo = new ProbeVolumeProfileInfo();
                ModifyProfileFromLoadedData(m_BakingSet);
                m_CellPosToIndex.Clear();
                m_CellsToDilate.Clear();
            }

            // Clear loaded data
            foreach (var data in prv.perSceneDataList)
                data.QueueSceneRemoval();
            prv.Clear();

            // Recreate baking cells
            var prevSHBands = prv.shBands;
            prv.ForceNoDiskStreaming(true);
            prv.ForceSHBand(ProbeVolumeSHBands.SphericalHarmonicsL2);
            var touchupVolumesAndBounds = GetAdjustementVolumes();

            int currentCell = 0;
            var bakingCells = new BakingCell[bakingSet.cellDescs.Count];
            var cellVolumes = new TouchupVolumeWithBoundsList[bakingSet.cellDescs.Count];
            foreach (var cell in bakingSet.cellDescs.Values)
            {
                var bakingCell = ConvertCellToBakingCell(cell, bakingSet.GetCellData(cell.index));
                bakingCell.ComputeBounds(cellSize);

                bakingCells[currentCell] = bakingCell;
                cellVolumes[currentCell] = bakingCell.SelectIntersectingAdjustmentVolumes(touchupVolumesAndBounds);
                currentCell++;

                m_CellPosToIndex.Add(bakingCell.position, bakingCell.index);
            }

            // Find probe positions
            List<(int, int, int)> bakedProbes = new();
            Dictionary<int, int> positionToIndex = new();
            NativeList<Vector3> uniquePositions = new NativeList<Vector3>(Allocator.Persistent);

            touchup.GetOBBandAABB(out var obb, out var aabb);

            var job = new BakeJob();
            if (touchup.isActiveAndEnabled && touchup.mode == ProbeAdjustmentVolume.Mode.OverrideSampleCount)
                job.Create(ProbeVolumeLightingTab.GetLightingSettings(), bakingSet.bakedSkyOcclusion, (obb, aabb, touchup));
            else
                job.Create(bakingSet, ProbeVolumeLightingTab.GetLightingSettings(), bakingSet.bakedSkyOcclusion);

            for (int c = 0; c < bakingCells.Length; c++)
            {
                ref var cell = ref bakingCells[c];

                if (touchup.IntersectsVolume(obb, aabb, cell.bounds))
                {
                    for (int i = 0; i < cell.probePositions.Length; i++)
                    {
                        var pos = cell.probePositions[i];
                        if (!touchup.ContainsPoint(obb, aabb.center, pos))
                            continue;

                        int probeHash = m_BakingBatch.GetProbePositionHash(pos);
                        int subdivLevel = cell.bricks[i / 64].subdivisionLevel;
                        if (!positionToIndex.TryGetValue(probeHash, out var index))
                        {
                            index = uniquePositions.Length;
                            positionToIndex[probeHash] = index;
                            m_BakingBatch.uniqueBrickSubdiv[probeHash] = subdivLevel;
                            job.indices.Add(index);
                            uniquePositions.Add(pos);
                        }
                        else
                            m_BakingBatch.uniqueBrickSubdiv[probeHash] = Mathf.Min(subdivLevel, m_BakingBatch.uniqueBrickSubdiv[probeHash]);

                        bakedProbes.Add((index, c, i));
                        m_CellsToDilate[cell.index] = cell;
                    }
                }
            }

            if (uniquePositions.Length != 0)
            {
                // Apply virtual offset
                var virtualOffsetJob = new VirtualOffsetBaking();
                virtualOffsetJob.Initialize(bakingSet, uniquePositions);
                if (virtualOffsetJob.offsets != null)
                {
                    while (virtualOffsetJob.currentStep < virtualOffsetJob.stepCount)
                        virtualOffsetJob.RunVirtualOffsetStep();
                    for (int i = 0; i < uniquePositions.Length; i++)
                        uniquePositions[i] += virtualOffsetJob.offsets[i];
                }

                // Bake sky occlusion
                var skyOcclusionJob = new SkyOcclusionBaking();
                skyOcclusionJob.Initialize(bakingSet, new BakeJob[] { job }, uniquePositions.Length);
                if (skyOcclusionJob.stepCount != 0)
                {
                    skyOcclusionJob.StartBaking(uniquePositions.AsArray());
                    while (skyOcclusionJob.currentStep < skyOcclusionJob.stepCount)
                        skyOcclusionJob.RunSkyOcclusionStep();
                }

                // Bake probe SH
                var irradianceResults = new NativeArray<SphericalHarmonicsL2>(uniquePositions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                var validityResults = new NativeArray<float>(uniquePositions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                {
                    bool cancel = false;
                    InputExtraction.BakeInput input;
                    InputExtraction.ExtractFromScene(out input);
                    var context = BakeContext.New(input, uniquePositions.AsArray());
                    context.Bake(job, ref irradianceResults, ref validityResults, ref cancel);
                    context.Dispose();
                }

                // Upload new data in cells
                foreach ((int uniqueProbeIndex, int cellIndex, int i) in bakedProbes)
                {
                    ref var cell = ref bakingCells[cellIndex];
                    cell.SetBakedData(m_BakingSet, m_BakingBatch, cellVolumes[cellIndex], i, uniqueProbeIndex,
                        irradianceResults[uniqueProbeIndex], validityResults[uniqueProbeIndex],
                        virtualOffsetJob.offsets, skyOcclusionJob.occlusionResults, skyOcclusionJob.directionResults);
                }

                virtualOffsetJob.Dispose();
                skyOcclusionJob.Dispose();
                irradianceResults.Dispose();
                validityResults.Dispose();

                for (int c = 0; c < bakingCells.Length; c++)
                {
                    ref var cell = ref bakingCells[c];
                    ComputeValidityMasks(cell);
                }

                // Write result to disk
                WriteBakingCells(bakingCells);

                // Reload everything
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (m_BakingSet.hasDilation)
                {
                    // Force reloading of data
                    foreach (var data in prv.perSceneDataList)
                        data.Initialize();

                    InitDilationShaders();
                    PerformDilation();
                }
            }

            job.Dispose();
            uniquePositions.Dispose();

            prv.ForceNoDiskStreaming(false);
            prv.ForceSHBand(prevSHBands);

            {
                // Restore values
                bakingSet.simplificationLevels = savedLevels;
                bakingSet.minDistanceBetweenProbes = savedDistance;
                bakingSet.skyOcclusion = savedSkyOcclusion;
                bakingSet.skyOcclusionShadingDirection = savedSkyDirection;
                bakingSet.settings.virtualOffsetSettings.useVirtualOffset = savedVirtualOffset;

                m_BakingBatch = null;
                m_BakingSet = null;
            }

            if (ProbeVolumeLightingTab.instance == null)
                ProbeGIBaking.Dispose();
        }
    }
}
