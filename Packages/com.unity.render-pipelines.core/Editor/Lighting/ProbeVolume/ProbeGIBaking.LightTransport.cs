using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine.LightTransport;
using UnityEngine.LightTransport.PostProcessing;
using UnityEditor.PathTracing.LightBakerBridge;
using UnityEngine.PathTracing.Core;
using UnityEngine.PathTracing.Integration;
using UnityEngine.PathTracing.PostProcessing;
using UnityEditor.LightBaking;
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
            public override bool isThreadSafe => false; // The Unified backend is not thread safe; the bake pipeline driver must be run on the main thread for VO baking.

            public BakeType bakeType { get; set; }
            private BakePipelineDriver bakePipelineDriver;

            // Outputs
            private NativeArray<SphericalHarmonicsL2> irradianceResults;
            private NativeArray<float> validityResults;
            private NativeArray<Vector4> occlusionResults;
            private NativeArray<uint> renderingLayerMasks; // Baked in a other job, but used in this one if available when fixing seams

            private bool isDone;
            private long probeCount;
            private ulong step;
            public override ulong currentStep => isDone ? stepCount : Math.Min(step, inProgressMaxStepCount);
            private ulong inProgressMaxStepCount => stepCount == 0 ? 0 : stepCount - 1;
            public override ulong stepCount => (ulong)probeCount;

            public override NativeArray<SphericalHarmonicsL2> irradiance => irradianceResults;
            public override NativeArray<float> validity => validityResults;
            public override NativeArray<Vector4> occlusion => occlusionResults;

            public override void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions)
            {
                bakeType = BakeType.Full;
                isDone = false;
                step = 0;
                probeCount = probePositions.Length;
                irradianceResults = new NativeArray<SphericalHarmonicsL2>(probePositions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                validityResults = new NativeArray<float>(probePositions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                if (bakeProbeOcclusion)
                    occlusionResults = new NativeArray<Vector4>(probePositions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
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
                // If we are baking APV only, kick off the bake pipeline if we have not done it already.
                bool apvOnly = bakeType is BakeType.ApvOnly or BakeType.AdditionalApvOnly;
                if (apvOnly)
                {
                    float progress = 0;
                    BakePipelineDriver.StageName stage = default;

                    if (bakePipelineDriver == null)
                    {
                        Debug.Assert(UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread());

                        bakePipelineDriver = new BakePipelineDriver();

                        bakePipelineDriver.StartBake(
                        bakeType == BakeType.ApvOnly, // We need patching for ApvOnly, not for AdditionalApvOnly.
                        ref progress, ref stage);
                    }

                    // For a full bake, we are called after the bake pipeline driver has finished.
                    // For an APV only bake,we need to repeatedly call the pipeline driver until finished.
                    if (bakePipelineDriver.RunInProgress())
                    {
                        bakePipelineDriver.Step(ref progress, ref stage);
                        // Update the step count based on the progress, the step count 0 - number of probes.
                        // For APV only the stepcount is used to report progress.
                        if (stage == BakePipelineDriver.StageName.Bake)
                            step = (ulong)(Math.Clamp(progress, 0.0f, 1.0f) * probeCount);
                        return true;
                    }
                }

                // At this point, the baked data exists on disk. Either the regular LightBaker process wrote it,
                // or our local BakePipeline wrote it, in case of APV-only bake.
                {
                    using NativeArray<byte> shBytes = new(System.IO.File.ReadAllBytes(System.IO.Path.Combine(APVLightBakerPostProcessingOutputFolder, "irradiance.shl2")), Allocator.TempJob);
                    using NativeArray<SphericalHarmonicsL2> shData = shBytes.GetSubArray(sizeof(ulong), shBytes.Length - sizeof(ulong)).Reinterpret<SphericalHarmonicsL2>(sizeof(byte));
                    irradiance.CopyFrom(shData);
                }
                {
                    using NativeArray<byte> validityBytes = new(System.IO.File.ReadAllBytes(System.IO.Path.Combine(APVLightBakerOutputFolder, "validity0.float")), Allocator.TempJob);
                    using NativeArray<float> validityData = validityBytes.GetSubArray(sizeof(ulong), validityBytes.Length - sizeof(ulong)).Reinterpret<float>(sizeof(byte));
                    validity.CopyFrom(validityData);
                }
                if (occlusionResults.IsCreated)
                {
                    // Read LightProbeOcclusion structs from disk
                    using NativeArray<byte> occlusionBytes = new(System.IO.File.ReadAllBytes(System.IO.Path.Combine(APVLightBakerPostProcessingOutputFolder, "occlusion.occ")), Allocator.TempJob);
                    using NativeArray<LightProbeOcclusion> occlusionData = occlusionBytes.GetSubArray(sizeof(ulong), occlusionBytes.Length - sizeof(ulong)).Reinterpret<LightProbeOcclusion>(sizeof(byte));

                    // Create swizzled occlusion buffer which is indexed by shadowmask channel. This the format expected by shader code.
                    NativeArray<Vector4> swizzledOcclusion = new NativeArray<Vector4>(occlusionData.Length, Allocator.TempJob);
                    for (int probeIdx = 0; probeIdx < occlusionData.Length; probeIdx++)
                    {
                        LightProbeOcclusion occlusion = occlusionData[probeIdx];
                        Vector4 swizzled = Vector4.zero;
                        for (int lightIdx = 0; lightIdx < 4; lightIdx++)
                        {
                            if (occlusionData[probeIdx].GetOcclusionMaskChannel(lightIdx, out sbyte shadowmaskIdx) && shadowmaskIdx >= 0)
                            {
                                occlusion.GetOcclusion(lightIdx, out float occlusionFactor);
                                swizzled[shadowmaskIdx] = occlusionFactor;
                            }
                        }

                        swizzledOcclusion[probeIdx] = swizzled;
                    }
                    occlusion.CopyFrom(swizzledOcclusion);
                    swizzledOcclusion.Dispose();
                }

                isDone = true;

                return true;
            }

            public override void Dispose()
            {
                irradianceResults.Dispose();
                validityResults.Dispose();
                if (occlusionResults.IsCreated)
                    occlusionResults.Dispose();
                renderingLayerMasks.Dispose();

                bakePipelineDriver?.Dispose();
            }
        }

        private struct BakeJob : IDisposable
        {
            public Bounds aabb;
            public ProbeReferenceVolume.Volume obb;
            public ProbeAdjustmentVolume touchup;

            public int startOffset;
            public int probeCount;

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
            public ulong stepCount => (ulong)probeCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Create(ProbeVolumeBakingSet bakingSet, LightingSettings lightingSettings, bool ignoreEnvironement)
            {
                skyOcclusionBakingSamples = bakingSet != null ? bakingSet.skyOcclusionBakingSamples : 0;
                skyOcclusionBakingBounces = bakingSet != null ? bakingSet.skyOcclusionBakingBounces : 0;

#if UNIFIED_BAKER
                int indirectSampleCount = lightingSettings.indirectSampleCount;
#else
                int indirectSampleCount = Math.Max(lightingSettings.indirectSampleCount, lightingSettings.environmentSampleCount);
#endif
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
                Scene scene = SceneManager.GetSceneByPath(AssetDatabase.GUIDToAssetPath(new GUID(guid)));
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

            const string k_PackageLightTransport = "Packages/com.unity.render-pipelines.core";

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

                SamplingResources.Bind(cmd, m_SamplingResources);
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
        private static void BakeAdditionalProbes(out SphericalHarmonicsL2[] shValues,
            out float[] validityValues)
        {
            while (s_BakeData.lightingJob.currentStep < s_BakeData.lightingJob.stepCount)
                if (!s_BakeData.lightingJob.Step())
                    s_BakeData.failed = true;

            shValues = s_BakeData.lightingJob.irradiance.ToArray();
            validityValues = s_BakeData.lightingJob.validity.ToArray();

            CleanBakeData();
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
                m_BakingBatch = new BakingBatch(cellCount, prv);
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
                s_BakeData.InitLightingJob(m_BakingSet, uniquePositions, BakeType.ApvOnly);
                LightingBaker lightingJob = s_BakeData.lightingJob;
                while (!failed && lightingJob.currentStep < lightingJob.stepCount)
                    failed |= !lightingJob.Step();
                CleanLightingBakeData();

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
                    // Validate baking cells size before any global state modifications
                    var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
                    var hasVirtualOffsets = m_BakingSet.settings.virtualOffsetSettings.useVirtualOffset;
                    var hasRenderingLayers = m_BakingSet.useRenderingLayers;

                    if (ValidateBakingCellsSize(bakingCells, chunkSizeInProbes, hasVirtualOffsets, hasRenderingLayers))
                    {
                        for (int c = 0; c < bakingCells.Length; c++)
                        {
                            ref var cell = ref bakingCells[c];
                            ComputeValidityMasks(cell);
                        }

                        // Attempt to write the result to disk
                        if (WriteBakingCells(bakingCells))
                        {
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
                CleanUp();
        }
    }
}
