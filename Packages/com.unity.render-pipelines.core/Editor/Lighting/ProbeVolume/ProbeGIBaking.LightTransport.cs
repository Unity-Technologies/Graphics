using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEditor;

using UnityEngine.LightTransport;
using UnityEngine.LightTransport.PostProcessing;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.SceneManagement;
using TouchupVolumeWithBoundsList = System.Collections.Generic.List<(UnityEngine.Rendering.ProbeReferenceVolume.Volume obb, UnityEngine.Bounds aabb, UnityEngine.Rendering.ProbeAdjustmentVolume volume)>;

namespace UnityEngine.Rendering
{
    partial class AdaptiveProbeVolumes
    {
        /// <summary>
        /// Lighting baker
        /// </summary>
        public abstract class LightingBaker : IDisposable
        {
            /// <summary>Indicates that the Step method can be safely called from a thread.</summary>
            public virtual bool isThreadSafe => false;
            /// <summary>Set to true when the main thread cancels baking.</summary>
            public static bool cancel { get; internal set; }

            /// <summary>The current baking step.</summary>
            public abstract ulong currentStep { get; }
            /// <summary>The total amount of step.</summary>
            public abstract ulong stepCount { get; }

            /// <summary>Array storing the probe lighting as Spherical Harmonics.</summary>
            public abstract NativeArray<SphericalHarmonicsL2> irradiance { get; }
            /// <summary>Array storing the probe validity. A value of 1 means a probe is invalid.</summary>
            public abstract NativeArray<float> validity { get; }
            /// <summary>Array storing 4 light occlusion values for each probe.</summary>
            public abstract NativeArray<Vector4> occlusion { get; }

            /// <summary>
            /// This is called before the start of baking to allow allocating necessary resources.
            /// </summary>
            /// <param name="bakeProbeOcclusion">Whether to bake occlusion for mixed lights for each probe.</param>
            /// <param name="probePositions">The probe positions. Also contains reflection probe positions used for normalization.</param>
            public abstract void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions);

            /// <summary>
            /// This is called before the start of baking to allow allocating necessary resources.
            /// </summary>
            /// <param name="bakeProbeOcclusion">Whether to bake occlusion for mixed lights for each probe.</param>
            /// <param name="probePositions">The probe positions. Also contains reflection probe positions used for normalization.</param>
            /// <param name="bakedRenderingLayerMasks">The rendering layer masks assigned to each probe. It is used when fixing seams between subdivision levels</param>
            public abstract void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions, NativeArray<uint> bakedRenderingLayerMasks);

            /// <summary>
            /// Run a step of light baking. Baking is considered done when currentStep property equals stepCount.
            /// If isThreadSafe is true, this method may be called from a different thread.
            /// </summary>
            /// <returns>Return false if bake failed and should be stopped.</returns>
            public abstract bool Step();

            /// <summary>
            /// Performs necessary tasks to free allocated resources.
            /// </summary>
            public abstract void Dispose();
        }

        class DefaultLightTransport : LightingBaker
        {
            public override bool isThreadSafe => true;

            int bakedProbeCount;
            NativeArray<Vector3> positions;
            InputExtraction.BakeInput input;
            bool bakeProbeOcclusion;

            public BakeJob[] jobs;

            // Outputs
            public NativeArray<SphericalHarmonicsL2> irradianceResults;
            public NativeArray<float> validityResults;
            public NativeArray<Vector4> occlusionResults;

            // Baked in a other job, but used in this one if available when fixing seams
            private NativeArray<uint> renderingLayerMasks;

            public override ulong currentStep => (ulong)bakedProbeCount;
            public override ulong stepCount => (ulong)positions.Length;

            public override NativeArray<SphericalHarmonicsL2> irradiance => irradianceResults;
            public override NativeArray<float> validity => validityResults;
            public override NativeArray<Vector4> occlusion => occlusionResults;

            public override void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions)
            {
                if (!InputExtraction.ExtractFromScene(out input))
                {
                    Debug.LogError("InputExtraction.ExtractFromScene failed.");
                    return;
                }

                bakedProbeCount = 0;
                positions = probePositions;

                irradianceResults = new NativeArray<SphericalHarmonicsL2>(positions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                validityResults = new NativeArray<float>(positions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                this.bakeProbeOcclusion = bakeProbeOcclusion;
                if (bakeProbeOcclusion)
                    occlusionResults = new NativeArray<Vector4>(positions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            public override void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions, NativeArray<uint> bakedRenderingLayerMasks)
            {
                renderingLayerMasks.Dispose();
                if (bakedRenderingLayerMasks.IsCreated)
                {
                    renderingLayerMasks = new NativeArray<uint>(bakedRenderingLayerMasks.Length, Allocator.Persistent);
                    renderingLayerMasks.CopyFrom(bakedRenderingLayerMasks);
                }

                Initialize(bakeProbeOcclusion, probePositions);
            }

            public override bool Step()
            {
                if (input == null)
                    return false;

                var context = BakeContext.New(input, positions, bakeProbeOcclusion);
                if (!context.isCreated)
                    return false;

                try
                {
                    for (int i = 0; i < jobs.Length; i++)
                    {
                        ref var job = ref jobs[i];
                        if (job.probeCount != 0)
                        {
                            if (!context.Bake(job, ref irradianceResults, ref validityResults, ref occlusionResults))
                                return false;

                            bakedProbeCount += job.probeCount;
                        }
                    }
                }
                finally
                {
                    context.Dispose();
                }

                // Fixup lighting for probes part of bricks with different subdivision levels
                // When baking reflection probes, we want to skip this step
                if (m_BakingBatch != null)
                {
                    FixSeams(s_BakeData.positionRemap, positions, irradiance, validity, renderingLayerMasks);
                }

                return true;
            }

            public override void Dispose()
            {
                irradianceResults.Dispose();
                validityResults.Dispose();
                if (bakeProbeOcclusion)
                    occlusionResults.Dispose();
                renderingLayerMasks.Dispose();
            }
        }

        struct BakeJob
        {
            public Bounds aabb;
            public ProbeReferenceVolume.Volume obb;
            public ProbeAdjustmentVolume touchup;

            public int startOffset;
            public int probeCount;

            public int directSampleCount;
            public int indirectSampleCount;
            public int validitySampleCount;
            public int occlusionSampleCount;
            public int maxBounces;

            public int skyOcclusionBakingSamples;
            public int skyOcclusionBakingBounces;

            public float indirectScale;
            public bool ignoreEnvironement;

            public BakeProgressState progress;
            public ulong currentStep => (ulong)Mathf.Min(progress.Progress() * 0.01f / (float)(directSampleCount + indirectSampleCount + validitySampleCount), stepCount); // this is how the progress is computed in c++
            public ulong stepCount => (ulong)probeCount;

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
                progress = new BakeProgressState();

                this.directSampleCount = directSampleCount * sampleCountMultiplier;
                this.indirectSampleCount = indirectSampleCount * sampleCountMultiplier;
                this.validitySampleCount = indirectSampleCount * sampleCountMultiplier;
                this.occlusionSampleCount = directSampleCount * sampleCountMultiplier;
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
                progress.Dispose();
            }
        }

        struct BakeContext
        {
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
                    IntegrateOcclusion,
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

            public IDeviceContext ctx;
            public IProbeIntegrator integrator;
            public IWorld world;
            public IProbePostProcessor postProcessor;

            public BufferID positionsBufferID;
            public BufferID directRadianceBufferId;
            public BufferID indirectRadianceBufferId;
            public BufferID validityBufferId;
            public BufferID perProbeLightIndicesId;
            public BufferID occlusionBufferId;

            public BufferID windowedDirectSHBufferId;
            public BufferID boostedIndirectSHBufferId;
            public BufferID combinedSHBufferId;
            public BufferID irradianceBufferId;

            public bool allocatedBuffers;
            public bool isCreated => allocatedBuffers;

            const float k_PushOffset = 0.0001f;
            const int k_MaxProbeCountPerBatch = 128 * 1024;

            const int maxOcclusionLightsPerProbe = 4;
            static readonly int sizeOfFloat = 4;
            static readonly int SHL2RGBElements = 3 * 9;
            static readonly int sizeSHL2RGB = sizeOfFloat * SHL2RGBElements;

            int[] perProbeShadowmaskIndices;
            bool bakeProbeOcclusion;

            public static BakeContext New(InputExtraction.BakeInput input, NativeArray<Vector3> probePositions, bool bakeProbeOcclusion)
            {
                var ctx = new BakeContext
                {
                    ctx = new RadeonRaysContext(),
                    integrator = new RadeonRaysProbeIntegrator(),
                    world = new RadeonRaysWorld(),
                    postProcessor = new RadeonRaysProbePostProcessor(),
                };

                if (!ctx.ctx.Initialize())
                {
                    Debug.LogError("Failed to initialize context.");
                    return ctx;
                }

                using var inputProgress = new BakeProgressState();
                if (!InputExtraction.PopulateWorld(input, inputProgress, ctx.ctx, ctx.world))
                {
                    Debug.LogError("Failed to extract inputs.");
                    return ctx;
                }

                if (!ctx.postProcessor.Initialize(ctx.ctx))
                {
                    Debug.LogError("Failed to initialize postprocessor.");
                    return ctx;
                }

                ctx.bakeProbeOcclusion = bakeProbeOcclusion;
                ctx.CreateBuffers(probePositions.Length);

                // Upload probe positions
                var positionsSlice = new BufferSlice<Vector3>(ctx.positionsBufferID, 0);
                var positionWriteEvent = ctx.ctx.CreateEvent();
                ctx.ctx.WriteBuffer(positionsSlice, probePositions, positionWriteEvent);

                if (bakeProbeOcclusion)
                {
                    // Upload per probe light indices
                    int[] perProbeLightIndicesArray = InputExtraction.ComputeOcclusionLightIndicesFromBakeInput(input, probePositions.ToArray(), (uint)maxOcclusionLightsPerProbe);
                    using var perProbeLightIndices = new NativeArray<int>(perProbeLightIndicesArray, Allocator.TempJob);
                    var perProbeLightIndicesSlice = new BufferSlice<int>(ctx.perProbeLightIndicesId, 0);
                    var perProbeLightIndicesWriteEvent = ctx.ctx.CreateEvent();
                    ctx.ctx.WriteBuffer(perProbeLightIndicesSlice, perProbeLightIndices, perProbeLightIndicesWriteEvent);
                    ctx.ctx.Wait(perProbeLightIndicesWriteEvent);
                    ctx.ctx.DestroyEvent(perProbeLightIndicesWriteEvent);

                    // Store per-probe shadowmask indices. They will be used to swizzle the occlusion buffer.
                    ctx.perProbeShadowmaskIndices = InputExtraction.GetShadowmaskChannelsFromLightIndices(input, perProbeLightIndicesArray);
                }

                // Wait for writes to finish
                ctx.ctx.Wait(positionWriteEvent);
                ctx.ctx.DestroyEvent(positionWriteEvent);

                return ctx;
            }

            private void CreateBuffers(int probeCount)
            {
                // Allocate shared position and light index buffer for all jobs
                positionsBufferID = ctx.CreateBuffer((ulong)probeCount, (ulong)(3 * sizeOfFloat));

                int batchSize = Mathf.Min(k_MaxProbeCountPerBatch, probeCount);
                var shBytes = (ulong)(sizeSHL2RGB * batchSize);
                var validityBytes = (ulong)(sizeOfFloat * batchSize);

                directRadianceBufferId = ctx.CreateBuffer((ulong)(batchSize * SHL2RGBElements), (ulong)sizeOfFloat);
                indirectRadianceBufferId = ctx.CreateBuffer((ulong)(batchSize * SHL2RGBElements), (ulong)sizeOfFloat);
                validityBufferId = ctx.CreateBuffer((ulong)batchSize, (ulong)sizeOfFloat);

                windowedDirectSHBufferId = ctx.CreateBuffer((ulong)(batchSize * SHL2RGBElements), (ulong)sizeOfFloat);
                boostedIndirectSHBufferId = ctx.CreateBuffer((ulong)(batchSize * SHL2RGBElements), (ulong)sizeOfFloat);
                combinedSHBufferId = ctx.CreateBuffer((ulong)(batchSize * SHL2RGBElements), (ulong)sizeOfFloat);
                irradianceBufferId = ctx.CreateBuffer((ulong)(batchSize * SHL2RGBElements), (ulong)sizeOfFloat);

                if (bakeProbeOcclusion)
                {
                    var lightIndicesBytes = (ulong)(sizeOfFloat * maxOcclusionLightsPerProbe * probeCount);
                    perProbeLightIndicesId = ctx.CreateBuffer((ulong)(maxOcclusionLightsPerProbe * probeCount), (ulong)sizeOfFloat);

                    var occlusionBytes = (ulong)(sizeOfFloat * maxOcclusionLightsPerProbe * batchSize);
                    occlusionBufferId = ctx.CreateBuffer((ulong)(maxOcclusionLightsPerProbe * batchSize), (ulong)sizeOfFloat);
                }

                allocatedBuffers = true;
            }

            public bool Bake(in BakeJob job, ref NativeArray<SphericalHarmonicsL2> irradianceResults, ref NativeArray<float> validityResults, ref NativeArray<Vector4> occlusionResults)
            {
                // Divide the job into batches of 128k probes to reduce memory usage.
                int batchCount = CoreUtils.DivRoundUp(job.probeCount, k_MaxProbeCountPerBatch);

                // Get slices for all buffers because the API require those
                // All jobs use overlapping slices as they are not run simultaneously
                var directRadianceSlice = new BufferSlice<SphericalHarmonicsL2>(directRadianceBufferId, 0);
                var indirectRadianceSlice = new BufferSlice<SphericalHarmonicsL2>(indirectRadianceBufferId, 0);
                var validitySlice = new BufferSlice<float>(validityBufferId, 0);
                var occlusionSlice = new BufferSlice<Vector4>(occlusionBufferId, 0);
                var windowedDirectRadianceSlice = new BufferSlice<SphericalHarmonicsL2>(windowedDirectSHBufferId, 0);
                var boostedIndirectRadianceSlice = indirectRadianceSlice;
                var combinedSHSlice = new BufferSlice<SphericalHarmonicsL2>(combinedSHBufferId, 0);
                var irradianceSlice = new BufferSlice<SphericalHarmonicsL2>(irradianceBufferId, 0);

                // Loop over all batches
                for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
                {
                    int batchOffset = batchIndex * k_MaxProbeCountPerBatch;
                    int probeCount = Mathf.Min(job.probeCount - batchOffset, k_MaxProbeCountPerBatch);

                    // Get the correct slice of position and light indices as all jobs share the same array.
                    var positionsSlice = new BufferSlice<Vector3>(positionsBufferID, (ulong)(job.startOffset + batchOffset));
                    var perProbeLightIndicesSlice = new BufferSlice<int>(perProbeLightIndicesId, (ulong)(job.startOffset + batchOffset) * maxOcclusionLightsPerProbe);

                    /// Baking

                    // Prepare integrator.
                    integrator.Prepare(ctx, world, positionsSlice, k_PushOffset, job.maxBounces);
                    integrator.SetProgressReporter(job.progress);

                    // Bake direct radiance
                    using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.IntegrateDirectRadiance))
                    {
                        var integrationResult = integrator.IntegrateDirectRadiance(ctx, 0, probeCount, job.directSampleCount, job.ignoreEnvironement, directRadianceSlice);
                        if (integrationResult.type != IProbeIntegrator.ResultType.Success) return false;
                        if (LightingBaker.cancel) return true;
                    }

                    // Bake indirect radiance
                    using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.IntegrateIndirectRadiance))
                    {
                        var integrationResult = integrator.IntegrateIndirectRadiance(ctx, 0, probeCount, job.indirectSampleCount, job.ignoreEnvironement, indirectRadianceSlice);
                        if (integrationResult.type != IProbeIntegrator.ResultType.Success) return false;
                        if (LightingBaker.cancel) return true;
                    }

                    // Bake validity
                    using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.IntegrateValidity))
                    {
                        var validityResult = integrator.IntegrateValidity(ctx, 0, probeCount, job.validitySampleCount, validitySlice);
                        if (validityResult.type != IProbeIntegrator.ResultType.Success) return false;
                        if (LightingBaker.cancel) return true;
                    }

                    // Bake occlusion
                    if (bakeProbeOcclusion)
                    {
                        using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.IntegrateOcclusion))
                        {
                            var occlusionResult = integrator.IntegrateOcclusion(ctx, 0, probeCount, job.occlusionSampleCount, (int)maxOcclusionLightsPerProbe, perProbeLightIndicesSlice, occlusionSlice.SafeReinterpret<float>());
                            if (occlusionResult.type != IProbeIntegrator.ResultType.Success) return false;
                            if (LightingBaker.cancel) return true;
                        }
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

                    var jobIrradianceResults = irradianceResults.GetSubArray(job.startOffset + batchOffset, probeCount);
                    var jobValidityResults = validityResults.GetSubArray(job.startOffset + batchOffset, probeCount);
                    var jobOcclusionResults = default(NativeArray<Vector4>);
                    if (bakeProbeOcclusion)
                        jobOcclusionResults = occlusionResults.GetSubArray(job.startOffset + batchOffset, probeCount);

                    // Schedule read backs to get results back from GPU memory into CPU memory.
                    var irradianceReadEvent = ctx.CreateEvent();
                    ctx.ReadBuffer(combinedSHSlice, jobIrradianceResults, irradianceReadEvent);
                    var validityReadEvent = ctx.CreateEvent();
                    ctx.ReadBuffer(validitySlice, jobValidityResults, validityReadEvent);
                    var occlusionReadEvent = default(EventID);
                    if (bakeProbeOcclusion)
                    {
                        occlusionReadEvent = ctx.CreateEvent();
                        ctx.ReadBuffer(occlusionSlice, jobOcclusionResults, occlusionReadEvent);
                    }
                    if (!ctx.Flush()) return false;

                    using (new LightTransportBakingProfiling(LightTransportBakingProfiling.Stages.ReadBack))
                    {
                        // Wait for read backs to complete.
                        bool waitResult = ctx.Wait(irradianceReadEvent) && ctx.Wait(validityReadEvent) && (!bakeProbeOcclusion || ctx.Wait(occlusionReadEvent));
                        if (!waitResult) return false;
                    }

                    ctx.DestroyEvent(irradianceReadEvent);
                    ctx.DestroyEvent(validityReadEvent);

                    if (bakeProbeOcclusion)
                    {
                        ctx.DestroyEvent(occlusionReadEvent);

                        // Swizzle occlusion buffer so it is indexed by shadowmask channel.
                        // This is the format expected by shader code.
                        int baseProbeIdx = job.startOffset + batchOffset;
                        for (int probeIdx = 0; probeIdx < probeCount; probeIdx++)
                        {
                            Vector4 original = jobOcclusionResults[probeIdx];
                            Vector4 swizzled = Vector3.zero;

                            for (int lightIdx = 0; lightIdx < maxOcclusionLightsPerProbe; lightIdx++)
                            {
                                int shadowmaskIdx = perProbeShadowmaskIndices[(baseProbeIdx + probeIdx) * maxOcclusionLightsPerProbe + lightIdx];
                                if (shadowmaskIdx >= 0)
                                {
                                    swizzled[shadowmaskIdx] = original[lightIdx];
                                }
                            }

                            jobOcclusionResults[probeIdx] = swizzled;
                        }
                    }

                    if (LightingBaker.cancel)
                        return true;
                }

                return true;
            }

            public void Dispose()
            {
                if (allocatedBuffers)
                {
                    ctx.DestroyBuffer(positionsBufferID);
                    ctx.DestroyBuffer(directRadianceBufferId);
                    ctx.DestroyBuffer(indirectRadianceBufferId);
                    ctx.DestroyBuffer(validityBufferId);
                    if (bakeProbeOcclusion)
                    {
                        ctx.DestroyBuffer(occlusionBufferId);
                        ctx.DestroyBuffer(perProbeLightIndicesId);
                    }

                    ctx.DestroyBuffer(windowedDirectSHBufferId);
                    ctx.DestroyBuffer(boostedIndirectSHBufferId);
                    ctx.DestroyBuffer(combinedSHBufferId);
                    ctx.DestroyBuffer(irradianceBufferId);
                }

                postProcessor.Dispose();
                world.Dispose();
                integrator.Dispose();
                ctx.Dispose();
            }
        }

        // The contribution from all Baked and Mixed lights in the scene should be disabled to avoid double contribution.
        static void UpdateLightStatus()
        {
            var lightingSettings = ProbeVolumeLightingTab.GetLightingSettings();

            var sceneLights = new Dictionary<Scene, List<Light>>();

            // Modify each baked light, take note of which scenes they belong to.
            var allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in allLights)
            {
                if (light.lightmapBakeType != LightmapBakeType.Realtime)
                {
                    var bakingOutput = light.bakingOutput;
                    bakingOutput.isBaked = true;
                    bakingOutput.lightmapBakeType = light.lightmapBakeType;
                    bakingOutput.mixedLightingMode = lightingSettings.mixedBakeMode;
                    light.bakingOutput = bakingOutput;
                }

                // Take note of the lights from each scene
                var scene = light.gameObject.scene;
                if (!sceneLights.TryGetValue(scene, out var sceneLightList))
                {
                    sceneLightList = new List<Light>();
                    sceneLights.Add(scene, sceneLightList);
                }
                sceneLightList.Add(light);
            }

            // Now we make the modifications persistent by modifying Lighting Data Assets (LDA) on disk.
            string ldaFolderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(m_BakingSet));
            for (int i = 0; i < m_BakingSet.sceneGUIDs.Count; i++)
            {
                string guid = m_BakingSet.sceneGUIDs[i];
                Scene scene = SceneManager.GetSceneByPath(AssetDatabase.GUIDToAssetPath(guid));
                if (!scene.isLoaded)
                    continue;

                LightingDataAsset prevLDA = Lightmapping.GetLightingDataAssetForScene(scene);
                LightingDataAsset newLDA = prevLDA;

                // If the scene has no (modifiable) LDA, create a new one.
                bool isDefaultLDA = prevLDA && prevLDA.hideFlags.HasFlag(HideFlags.NotEditable);
                if (prevLDA == null || isDefaultLDA)
                {
                    newLDA = new LightingDataAsset(scene);
                }

                // Update the LDA with the new light settings
                if (sceneLights.TryGetValue(scene, out var lights))
                    newLDA.SetLights(lights.ToArray());
                else
                    newLDA.SetLights(Array.Empty<Light>());

                // If the scene was using the builtin/default LDA before, copy over environment lighting, so it doesn't change.
                if (prevLDA != null)
                {
                    newLDA.SetAmbientProbe(prevLDA.GetAmbientProbe());
                    newLDA.SetDefaultReflectionCubemap(prevLDA.GetDefaultReflectionCubemap());
                }

                // Save the LDA to disk and assign it to the scene.
                if (newLDA != prevLDA)
                {
                    string ldaPath = $"{ldaFolderPath}/LightingData-{i}.asset".Replace('\\', '/');
                    AssetDatabase.CreateAsset(newLDA, ldaPath);
                    Lightmapping.SetLightingDataAssetForScene(scene, newLDA);
                }
            }
        }

        // Helper struct to manage tracing backend

        struct APVRTContext
        {
            RayTracingContext m_Context;
            RayTracingBackend m_Backend;
            SamplingResources m_SamplingResources;
            RayTracingResources m_RayTracingResources;

            static IRayTracingShader m_ShaderVO = null;
            static IRayTracingShader m_ShaderSO = null;
            static IRayTracingShader m_ShaderRL = null;

            const string k_PackageLightTransport = "Packages/com.unity.rendering.light-transport";

            internal AccelStructAdapter CreateAccelerationStructure()
            {
                var c = context;
                return new AccelStructAdapter(c.CreateAccelerationStructure(new AccelerationStructureOptions
                {
                    // Use PreferFastBuild to avoid bug triggered with big meshes (UUM-52552));
                    buildFlags = BuildFlags.PreferFastBuild
                }),
                m_RayTracingResources
                );
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

            public IRayTracingShader shaderRL
            {
                get
                {
                    if (m_ShaderRL == null)
                    {
                        var bakingResources = GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeBakingResources>();
                        m_ShaderRL = m_Context.CreateRayTracingShader(m_Backend switch
                        {
                            RayTracingBackend.Hardware => bakingResources.renderingLayerRT,
                            RayTracingBackend.Compute => bakingResources.renderingLayerCS,
                            _ => null
                        });
                    }

                    return m_ShaderRL;
                }
            }

            public void BindSamplingTextures(CommandBuffer cmd)
            {
                if (m_SamplingResources == null)
                {
                    m_SamplingResources = new SamplingResources();
                    m_SamplingResources.Load();
                }

                SamplingResources.BindSobolBlueNoiseTextures(cmd, m_SamplingResources);
            }

            public bool TryGetMeshForAccelerationStructure(Renderer renderer, out Mesh mesh)
            {
                mesh = null;
                if (renderer.isPartOfStaticBatch)
                {
                    Debug.LogError("Static batching is not supported when baking APV.");
                    return false;
                }

                mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null)
                    return false;

                // This would error out later in LoadIndexBuffer in LightTransport package
                if ((mesh.indexBufferTarget & GraphicsBuffer.Target.Raw) == 0 && (mesh.GetIndices(0) == null || mesh.GetIndices(0).Length == 0))
                    return false;

                return true;
            }

            public void Dispose()
            {
                if (m_Context != null)
                {
                    m_Context.Dispose();
                    m_Context = null;

                    // The lifetime of these shaders are bound to the lifetime of the context.
                    m_ShaderRL = null;
                    m_ShaderSO = null;
                    m_ShaderVO = null;
                }

                m_SamplingResources?.Dispose();
                m_SamplingResources = null;
            }
        }


        // Helper functions to bake a subset of the probes

        internal static void BakeProbes(Vector3[] positionValues, SphericalHarmonicsL2[] shValues, float[] validityValues)
        {
            int numProbes = positionValues.Length;

            var positionsInput = new NativeArray<Vector3>(positionValues, Allocator.Temp);

            var lightingJob = lightingOverride ?? new DefaultLightTransport();
            lightingJob.Initialize(false, positionsInput);

            var defaultJob = lightingJob as DefaultLightTransport;
            if (defaultJob != null)
            {
                var job = new BakeJob();
                job.Create(null, ProbeVolumeLightingTab.GetLightingSettings(), false);
                job.probeCount = numProbes;

                defaultJob.jobs = new BakeJob[] { job };
            }

            while (lightingJob.currentStep < lightingJob.stepCount)
                lightingJob.Step();

            lightingJob.irradiance.CopyTo(shValues);
            lightingJob.validity.CopyTo(validityValues);

            if (defaultJob != null)
            {
                foreach (var job in defaultJob.jobs)
                job.Dispose();
            }
            lightingJob.Dispose();
            positionsInput.Dispose();
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
            bool savedRenderingLayers = bakingSet.useRenderingLayers;
            {
                // Patch baking set as we are not gonna use a mix of baked values and new values
                bakingSet.simplificationLevels = bakingSet.bakedSimplificationLevels;
                bakingSet.minDistanceBetweenProbes = bakingSet.bakedMinDistanceBetweenProbes;
                bakingSet.skyOcclusion = bakingSet.bakedSkyOcclusion;
                bakingSet.skyOcclusionShadingDirection = bakingSet.bakedSkyShadingDirection;
                bakingSet.settings.virtualOffsetSettings.useVirtualOffset = bakingSet.supportOffsetsChunkSize != 0;
                bakingSet.useRenderingLayers = bakingSet.bakedMaskCount == 1 ? false : true;

                m_BakingSet = bakingSet;
                m_BakingBatch = new BakingBatch(cellCount);
                m_ProfileInfo = new ProbeVolumeProfileInfo();
                ModifyProfileFromLoadedData(m_BakingSet);
                m_CellPosToIndex.Clear();
                m_CellsToDilate.Clear();
            }

            Debug.Assert(bakingSet.CheckCompatibleCellLayout());

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
                            job.probeCount++;
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
                bool failed = false;
                var jobs = new BakeJob[] { job };

                // Apply virtual offset
                var virtualOffsetJob = virtualOffsetOverride ?? new DefaultVirtualOffset();
                virtualOffsetJob.Initialize(bakingSet, uniquePositions.AsArray());
                while (!failed && virtualOffsetJob.currentStep < virtualOffsetJob.stepCount)
                    failed |= !virtualOffsetJob.Step();
                if (!failed && virtualOffsetJob.offsets.IsCreated)
                {
                    for (int i = 0; i < uniquePositions.Length; i++)
                        uniquePositions[i] += virtualOffsetJob.offsets[i];
                }

                // Bake sky occlusion
                var skyOcclusionJob = skyOcclusionOverride ?? new DefaultSkyOcclusion();
                skyOcclusionJob.Initialize(bakingSet, uniquePositions.AsArray());
                if (skyOcclusionJob is DefaultSkyOcclusion defaultSOJob)
                    defaultSOJob.jobs = jobs;
                while (!failed && skyOcclusionJob.currentStep < skyOcclusionJob.stepCount)
                    failed |= !skyOcclusionJob.Step();
                if (!failed && skyOcclusionJob.shadingDirections.IsCreated)
                    skyOcclusionJob.Encode();

                // Bake rendering layers
                var layerMaskJob = renderingLayerOverride ?? new DefaultRenderingLayer();
                layerMaskJob.Initialize(bakingSet, uniquePositions.AsArray());
                while (!failed && layerMaskJob.currentStep < layerMaskJob.stepCount)
                    failed |= !layerMaskJob.Step();

                // Bake probe SH
                var lightingJob = lightingOverride ?? new DefaultLightTransport();
                lightingJob.Initialize(ProbeVolumeLightingTab.GetLightingSettings().mixedBakeMode != MixedLightingMode.IndirectOnly, uniquePositions.AsArray(), layerMaskJob.renderingLayerMasks);
                if (lightingJob is DefaultLightTransport defaultLightingJob)
                    defaultLightingJob.jobs = jobs;
                while (!failed && lightingJob.currentStep < lightingJob.stepCount)
                    failed |= !lightingJob.Step();


                // Upload new data in cells
                foreach ((int uniqueProbeIndex, int cellIndex, int i) in bakedProbes)
                {
                    ref var cell = ref bakingCells[cellIndex];
                    cell.SetBakedData(m_BakingSet, m_BakingBatch, cellVolumes[cellIndex], i, uniqueProbeIndex,
                        lightingJob.irradiance[uniqueProbeIndex], lightingJob.validity[uniqueProbeIndex],
                        layerMaskJob.renderingLayerMasks, virtualOffsetJob.offsets,
                        skyOcclusionJob.occlusion, skyOcclusionJob.encodedDirections, lightingJob.occlusion);
                }

                skyOcclusionJob.encodedDirections.Dispose();
                virtualOffsetJob.Dispose();
                skyOcclusionJob.Dispose();
                lightingJob.Dispose();
                layerMaskJob.Dispose();

                if (!failed)
                {
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
                bakingSet.useRenderingLayers = savedRenderingLayers;

                m_BakingBatch = null;
                m_BakingSet = null;
            }

            if (ProbeVolumeLightingTab.instance == null)
                AdaptiveProbeVolumes.Dispose();
        }
    }
}
