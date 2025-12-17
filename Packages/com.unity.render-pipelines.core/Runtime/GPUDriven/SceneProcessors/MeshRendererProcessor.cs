using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    internal class MeshRendererProcessor : IDisposable
    {
        public struct GPUComponentUploadSource
        {
            public JaggedSpan<byte> data;
            public GPUComponentHandle component;
            public int componentSize;
        }

        private GPUDrivenProcessor m_GPUDrivenProcessor;
        private InstanceCullingBatcher m_CullingBatcher;
        private NativeReference<GPUArchetypeManager> m_ArchetypeManager;
        private InstanceDataSystem m_InstanceDataSystem;
        private LODGroupDataSystem m_LODGroupDataSystem;
        private GPUDrivenRendererDataCallback m_ProcessGameObjectUpdateBatchCallback;
        private NativeArray<uint> m_CPUUploadBuffer;
        private GraphicsBuffer m_GPUUploadBuffer;

        public MeshRendererProcessor(GPUDrivenProcessor gpuDrivenProcessor, GPUResidentContext grdContext)
        {
            m_GPUDrivenProcessor = gpuDrivenProcessor;
            m_CullingBatcher = grdContext.batcher;
            m_ArchetypeManager = grdContext.instanceDataSystem.archetypeManager;
            m_InstanceDataSystem = grdContext.instanceDataSystem;
            m_LODGroupDataSystem = grdContext.lodGroupDataSystem;

            // Create the delegate object in advance to prevent a GC allocation each time we pass the instance method to the GPUDrivenProcessor.
            m_ProcessGameObjectUpdateBatchCallback = ProcessGameObjectUpdateBatch;
        }

        public void Dispose()
        {
            m_CPUUploadBuffer.Dispose();
            m_GPUUploadBuffer?.Release();
        }

        public void DestroyInstances(NativeArray<EntityId> destroyedRenderers)
        {
            if (destroyedRenderers.Length == 0)
                return;

            Profiler.BeginSample("DestroyMeshRendererInstances");
            var destroyedInstances = new NativeArray<InstanceHandle>(destroyedRenderers.Length, Allocator.TempJob);
            m_InstanceDataSystem.QueryRendererInstances(destroyedRenderers, destroyedInstances);
            m_CullingBatcher.DestroyDrawInstances(destroyedInstances);
            m_InstanceDataSystem.FreeInstances(destroyedInstances);
            destroyedInstances.Dispose();
            Profiler.EndSample();
        }

        public void ProcessGameObjectChanges(NativeArray<EntityId> changedRenderers)
        {
            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(changedRenderers, m_ProcessGameObjectUpdateBatchCallback);
        }

        public void ProcessGameObjectTransformChanges(in TransformDispatchData transformChanges)
        {
            var updateBatch = new MeshRendererUpdateBatch(MeshRendererComponentMask.LocalToWorld,
                default,
                MeshRendererUpdateType.NoStructuralChanges,
                MeshRendererUpdateBatch.LightmapUsage.Unknown,
                MeshRendererUpdateBatch.BlendProbesUsage.Unknown,
                useSharedSceneCullingMask: false,
                1, Allocator.TempJob);

            updateBatch.AddSection(new MeshRendererUpdateSection
            {
                instanceIDs = transformChanges.transformedID,
                localToWorlds = transformChanges.localToWorldMatrices.Reinterpret<float4x4>()
            });

            updateBatch.Validate();
            ProcessUpdateBatch(ref updateBatch);

            updateBatch.Dispose();
        }

        public void ProcessRendererMaterialAndMeshChanges(NativeArray<EntityId> excludedRenderers,
            NativeArray<EntityId> changedMaterials,
            NativeArray<GPUDrivenMaterialData> changedMaterialDatas,
            NativeArray<EntityId> changedMeshes)
        {
            if (changedMaterials.Length == 0 && changedMeshes.Length == 0)
                return;

            // Update the material/mesh maps and retrieve the IDs of the materials/meshes for which the data actually changed.
            Profiler.BeginSample("GetMaterialsAndMeshesWithChangedData");
            NativeHashSet<EntityId> materialsWithChangedData = m_CullingBatcher.UpdateMaterialData(changedMaterials, changedMaterialDatas, Allocator.TempJob);
            NativeHashSet<EntityId> meshesWithChangedData = m_CullingBatcher.UpdateMeshData(changedMeshes, Allocator.TempJob);
            Profiler.EndSample();

            if (materialsWithChangedData.Count == 0 && meshesWithChangedData.Count == 0)
            {
                materialsWithChangedData.Dispose();
                meshesWithChangedData.Dispose();
                return;
            }

            var sortedExcludedRenderers = new NativeArray<EntityId>(excludedRenderers, Allocator.TempJob);
            if (sortedExcludedRenderers.Length > 0)
            {
                Profiler.BeginSample("ProcessRendererMaterialAndMeshChanges.Sort");
                sortedExcludedRenderers.Reinterpret<int>().ParallelSort().Complete();
                Profiler.EndSample();
            }

            Profiler.BeginSample("FindRenderersFromMaterialsOrMeshes");
            var (renderersWithChangedMaterials, renderersWithChangeMeshes) = FindRenderersFromMaterialsOrMeshes(sortedExcludedRenderers,
                materialsWithChangedData,
                meshesWithChangedData,
                Allocator.TempJob);
            Profiler.EndSample();

            materialsWithChangedData.Dispose();
            meshesWithChangedData.Dispose();
            sortedExcludedRenderers.Dispose();

            if (renderersWithChangedMaterials.Length == 0 && renderersWithChangeMeshes.Length == 0)
            {
                renderersWithChangedMaterials.Dispose();
                renderersWithChangeMeshes.Dispose();
                return;
            }

            Profiler.BeginSample("UpdateRenderers");
            var changedMaterialsCount = renderersWithChangedMaterials.Length;
            var changedMeshesCount = renderersWithChangeMeshes.Length;
            var totalCount = changedMaterialsCount + changedMeshesCount;

            var changedRenderers = new NativeArray<EntityId>(totalCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<EntityId>.Copy(renderersWithChangedMaterials.AsArray(), changedRenderers, changedMaterialsCount);
            NativeArray<EntityId>.Copy(renderersWithChangeMeshes.AsArray(), changedRenderers.GetSubArray(changedMaterialsCount, changedMeshesCount), changedMeshesCount);

            var changedInstances = new NativeArray<InstanceHandle>(totalCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_InstanceDataSystem.QueryRendererInstances(changedRenderers, changedInstances);
            
            m_CullingBatcher.BuildBatches(changedInstances);

            changedRenderers.Dispose();
            changedInstances.Dispose();
            renderersWithChangedMaterials.Dispose();
            renderersWithChangeMeshes.Dispose();
            Profiler.EndSample();
        }

        private (NativeList<EntityId> renderersWithMaterials, NativeList<EntityId> renderersWithMeshes)
            FindRenderersFromMaterialsOrMeshes(NativeArray<EntityId> sortedExcludeRenderers,
                NativeHashSet<EntityId> materials,
                NativeHashSet<EntityId> meshes,
                Allocator rendererListAllocator)
        {
            ref RenderWorld renderWorld = ref m_InstanceDataSystem.renderWorld;
            var renderersWithMaterials = new NativeList<EntityId>(renderWorld.instanceCount, rendererListAllocator);
            var renderersWithMeshes = new NativeList<EntityId>(renderWorld.instanceCount, rendererListAllocator);

            new FindRenderersFromMaterialOrMeshJob
            {
                renderWorld = renderWorld,
                materialIDs = materials,
                meshIDs = meshes,
                sortedExcludeRendererIDs = sortedExcludeRenderers,
                selectedRenderIDsForMaterials = renderersWithMaterials.AsParallelWriter(),
                selectedRenderIDsForMeshes = renderersWithMeshes.AsParallelWriter()
            }
            .ScheduleBatch(renderWorld.instanceIDs.Length, FindRenderersFromMaterialOrMeshJob.k_BatchSize)
            .Complete();

            return (renderersWithMaterials, renderersWithMeshes);
        }

        public unsafe void ProcessUpdateBatch(ref MeshRendererUpdateBatch updateBatch)
        {
            if (updateBatch.TotalLength == 0)
                return;

            Profiler.BeginSample("ProcessMeshRendererUpdateBatch");

            MeshRendererUpdateType updateType = updateBatch.updateType;
            bool anyInstanceUseBlendProbes = updateBatch.blendProbesUsage != MeshRendererUpdateBatch.BlendProbesUsage.AllDisabled;
            bool updateArchetype = updateType != MeshRendererUpdateType.NoStructuralChanges;
            bool hasOnlyKnowInstances = updateType == MeshRendererUpdateType.NoStructuralChanges
                || updateType == MeshRendererUpdateType.RecreateOnlyKnownInstances;

            const MeshRendererComponentMask UpdateInstanceDataMask = MeshRendererComponentMask.Mesh
                | MeshRendererComponentMask.Material
                | MeshRendererComponentMask.SubMeshStartIndex
                | MeshRendererComponentMask.LocalBounds
                | MeshRendererComponentMask.RendererSettings
                | MeshRendererComponentMask.ParentLODGroup
                | MeshRendererComponentMask.LODMask
                | MeshRendererComponentMask.MeshLodSettings
                | MeshRendererComponentMask.Lightmap
                | MeshRendererComponentMask.RendererPriority
                | MeshRendererComponentMask.SceneCullingMask
                | MeshRendererComponentMask.RenderingEnabled;

            bool updateInstanceData = updateType != MeshRendererUpdateType.NoStructuralChanges || updateBatch.HasAnyComponent(UpdateInstanceDataMask);

            const MeshRendererComponentMask UpdateDrawBatchesMask = MeshRendererComponentMask.Mesh
                | MeshRendererComponentMask.Material
                | MeshRendererComponentMask.SubMeshStartIndex
                | MeshRendererComponentMask.RendererSettings
                | MeshRendererComponentMask.Lightmap
                | MeshRendererComponentMask.RendererPriority;

            bool updateDrawBatches = updateType != MeshRendererUpdateType.NoStructuralChanges || updateBatch.HasAnyComponent(UpdateDrawBatchesMask);

            GPUComponentSet overrideComponentSet = default;
            NativeArray<GPUComponentUploadSource> componentUploadSources = default;
            if (updateBatch.HasAnyComponent(MeshRendererComponentMask.GPUComponent))
                componentUploadSources = BuildGPUComponentOverrideUploadSources(updateBatch, Allocator.TempJob, out overrideComponentSet);

            NativeArray<GPUArchetypeHandle> archetypes = default;
            if (updateArchetype)
                archetypes = ComputeInstanceGPUArchetypes(ref updateBatch, overrideComponentSet, Allocator.TempJob);

            var instances = new NativeArray<InstanceHandle>(updateBatch.TotalLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            if (updateType == MeshRendererUpdateType.OnlyNewInstances)
            {
                m_InstanceDataSystem.AllocateNewInstances(updateBatch.instanceIDs, instances, archetypes, instances.Length);
            }
            else if (updateType == MeshRendererUpdateType.MightIncludeNewInstances)
            {
                int newInstanceCount = 0;
                m_InstanceDataSystem.QueryRendererInstances(updateBatch.instanceIDs, instances, new UnsafeAtomicCounter32(&newInstanceCount));
                m_InstanceDataSystem.AllocOrGPUReallocInstances(updateBatch.instanceIDs, instances, archetypes, newInstanceCount);
            }
            else if (hasOnlyKnowInstances)
            {
                m_InstanceDataSystem.QueryRendererInstances(updateBatch.instanceIDs, instances);

                if (updateArchetype)
                    m_InstanceDataSystem.ReallocateExistingGPUInstances(instances, archetypes);
            }

            if (!updateArchetype)
                ValidateGPUArchetypesDidNotChange(instances, updateBatch, overrideComponentSet);

            if (!anyInstanceUseBlendProbes)
                ValidateNoInstanceUsesBlendProbes(instances);

            if (updateInstanceData)
                m_InstanceDataSystem.UpdateInstanceData(instances, updateBatch, m_LODGroupDataSystem.lodGroupDataHash);

            if (!overrideComponentSet.isEmpty)
                UploadGPUComponentOverrides(overrideComponentSet, componentUploadSources, instances);

            if (hasOnlyKnowInstances)
            {
                if (updateBatch.HasAnyComponent(MeshRendererComponentMask.LocalToWorld))
                    m_InstanceDataSystem.UpdateInstanceTransforms(instances, updateBatch.localToWorlds, anyInstanceUseBlendProbes);
            }
            else
            {
                JaggedSpan<float4x4> prevLocalToWorlds = updateBatch.HasAnyComponent(MeshRendererComponentMask.PrevLocalToWorld)
                    ? updateBatch.prevLocalToWorlds
                    : updateBatch.localToWorlds;

                m_InstanceDataSystem.InitializeInstanceTransforms(instances, updateBatch.localToWorlds, prevLocalToWorlds, anyInstanceUseBlendProbes);
            }

            if (updateDrawBatches)
                m_CullingBatcher.RegisterAndBuildBatches(instances, updateBatch);

            instances.Dispose();
            archetypes.Dispose();
            componentUploadSources.Dispose();
            Profiler.EndSample();
        }

        unsafe NativeArray<GPUArchetypeHandle> ComputeInstanceGPUArchetypes(ref MeshRendererUpdateBatch updateBatch, GPUComponentSet overrideComponentSet, Allocator allocator)
        {
            using (new ProfilerMarker("ComputeInstanceGPUArchetypes").Auto())
            {
                bool useSharedGPUArchetype = updateBatch.blendProbesUsage != MeshRendererUpdateBatch.BlendProbesUsage.Unknown
                    && updateBatch.lightmapUsage != MeshRendererUpdateBatch.LightmapUsage.Unknown
                    && !updateBatch.mightIncludeTrees;

                int archetypeCount = useSharedGPUArchetype ? 1 : updateBatch.TotalLength;
                NativeArray<GPUArchetypeHandle> archetypes = new NativeArray<GPUArchetypeHandle>(archetypeCount, allocator);

                fixed (MeshRendererUpdateBatch* updateBatchPtr = &updateBatch)
                {
                    MeshRendererProcessorBurst.ComputeInstanceGPUArchetypes(m_ArchetypeManager,
                        m_InstanceDataSystem.defaultGPUComponents,
                        updateBatchPtr,
                        overrideComponentSet,
                        useSharedGPUArchetype,
                        ref archetypes);
                }

                return archetypes;
            }
        }

        unsafe NativeArray<GPUComponentUploadSource> BuildGPUComponentOverrideUploadSources(in MeshRendererUpdateBatch updateBatch,
            Allocator allocator,
            out GPUComponentSet overrideComponentSet)
        {
            NativeArray<GPUComponentJaggedUpdate> componentUpdates = updateBatch.gpuComponentUpdates;
            if (componentUpdates.Length == 0)
            {
                overrideComponentSet = default;
                return default;
            }

            using (new ProfilerMarker("BuildGPUComponentOverrideUploadSources").Auto())
            {
                NativeArray<GPUComponentUploadSource> uploadSources = new NativeArray<GPUComponentUploadSource>(componentUpdates.Length, allocator);
                GPUComponentSet componentSet = default;

                MeshRendererProcessorBurst.BuildGPUComponentOverrideUploadSources(m_ArchetypeManager, componentUpdates, ref uploadSources, &componentSet);

                overrideComponentSet = componentSet;
                return uploadSources;
            }
        }

        void UploadGPUComponentOverrides(GPUComponentSet componentSet, NativeArray<GPUComponentUploadSource> uploadSources, NativeArray<InstanceHandle> instances)
        {
            Assert.IsTrue(!componentSet.isEmpty);
            Assert.IsTrue(uploadSources.Length > 0);

            Profiler.BeginSample("UploadGPUComponentOverrides");

            GPUInstanceUploadData uploadData = m_InstanceDataSystem.CreateInstanceUploadData(componentSet, instances.Length, Allocator.TempJob);

            EnsureUploadBufferUintCount(uploadData.uploadDataUIntSize);

            NativeArray<uint> writeBuffer = m_CPUUploadBuffer.GetSubArray(0, uploadData.uploadDataUIntSize);

            JobHandle allWritesJobHandle = default;

            for (int i = 0; i < uploadSources.Length; i++)
            {
                ref readonly GPUComponentUploadSource source = ref uploadSources.ElementAt(i);
                JobHandle jobHandle = uploadData.ScheduleWriteComponentsJob(source.data, source.component, source.componentSize, writeBuffer);
                allWritesJobHandle = JobHandle.CombineDependencies(jobHandle, allWritesJobHandle);
            }

            using (new ProfilerMarker("SyncWriteGPUComponentJobs").Auto())
            {
                allWritesJobHandle.Complete();
            }

            m_GPUUploadBuffer.SetData(writeBuffer);

            m_InstanceDataSystem.UploadDataToGPU(instances, m_GPUUploadBuffer, uploadData);

            uploadData.Dispose();
            Profiler.EndSample();
        }

        void EnsureUploadBufferUintCount(int uintCount)
        {
            int currentCPUBufferLength = m_CPUUploadBuffer.IsCreated ? m_CPUUploadBuffer.Length : 0;
            int currentGPUBufferLength = m_GPUUploadBuffer != null ? m_GPUUploadBuffer.count : 0;

            Assert.IsTrue(currentCPUBufferLength == currentGPUBufferLength);
            int currentUintCount = currentCPUBufferLength;

            if (uintCount > currentUintCount)
            {
                // At least double on resize
                int newUintCount = math.max(currentUintCount * 2, uintCount);

                m_CPUUploadBuffer.Dispose();
                m_GPUUploadBuffer?.Release();

                m_CPUUploadBuffer = new NativeArray<uint>(newUintCount, Allocator.Persistent);
                m_GPUUploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, newUintCount, sizeof(uint));
            }

            Assert.IsTrue(m_GPUUploadBuffer.count == m_CPUUploadBuffer.Length);
        }

        void ProcessGameObjectUpdateBatch(in GPUDrivenMeshRendererData rendererData)
        {
            if (rendererData.invalidRenderer.Length > 0)
            {
                m_GPUDrivenProcessor.DisableGPUDrivenRendering(rendererData.invalidRenderer);
                DestroyInstances(rendererData.invalidRenderer);
            }

            if (rendererData.renderer.Length == 0)
                return;

            var gpuComponents = new NativeArray<GPUComponent>(2, Allocator.Temp);
            gpuComponents[0] = new GPUComponent(DefaultShaderPropertyID.unity_LightmapST, UnsafeUtility.SizeOf<Vector4>());
            gpuComponents[1] = new GPUComponent(DefaultShaderPropertyID.unity_RendererUserValuesPropertyEntry, UnsafeUtility.SizeOf<uint>());

            var gpuComponentUpdates = new NativeArray<GPUComponentUpdate>(2, Allocator.Temp);
            gpuComponentUpdates[0] = GPUComponentUpdate.FromArray(gpuComponents[0], rendererData.lightmapScaleOffset);
            gpuComponentUpdates[1] = GPUComponentUpdate.FromArray(gpuComponents[1], rendererData.rendererUserValues);

            const MeshRendererComponentMask UpdateMask = MeshRendererComponentMask.LocalToWorld
                | MeshRendererComponentMask.PrevLocalToWorld
                | MeshRendererComponentMask.Mesh
                | MeshRendererComponentMask.Material
                | MeshRendererComponentMask.SubMeshStartIndex
                | MeshRendererComponentMask.LocalBounds
                | MeshRendererComponentMask.RendererSettings
                | MeshRendererComponentMask.ParentLODGroup
                | MeshRendererComponentMask.LODMask
                | MeshRendererComponentMask.MeshLodSettings
                | MeshRendererComponentMask.Lightmap
                | MeshRendererComponentMask.RendererPriority
                | MeshRendererComponentMask.GPUComponent
                | MeshRendererComponentMask.SceneCullingMask;

            var updateBatch = new MeshRendererUpdateBatch(UpdateMask,
                gpuComponents,
                MeshRendererUpdateType.MightIncludeNewInstances,
                MeshRendererUpdateBatch.LightmapUsage.Unknown,
                MeshRendererUpdateBatch.BlendProbesUsage.Unknown,
                useSharedSceneCullingMask: false,
                1, Allocator.TempJob);

            updateBatch.mightIncludeTrees = true;

            updateBatch.AddSection(new MeshRendererUpdateSection
            {
                instanceIDs = rendererData.renderer,
                localToWorlds = rendererData.localToWorldMatrix.Reinterpret<float4x4>(),
                prevLocalToWorlds = rendererData.prevLocalToWorldMatrix.Reinterpret<float4x4>(),
                meshIDs = rendererData.mesh,
                materialIDs = rendererData.material,
                subMaterialRanges = rendererData.subMaterialRange,
                subMeshStartIndices = rendererData.subMeshStartIndex,
                localBounds = rendererData.localBounds.Reinterpret<AABB>(),
                rendererSettings = rendererData.rendererSettings,
                parentLODGroupIDs = rendererData.lodGroup,
                lodMasks = rendererData.lodMask,
                meshLodSettings = rendererData.meshLodSettings,
                lightmapIndices = rendererData.lightmapIndex,
                rendererPriorities = rendererData.rendererPriority,
                sceneCullingMasks = rendererData.sceneCullingMask,
                gpuComponentUpdates = gpuComponentUpdates,
            });

            updateBatch.Validate();
            ProcessUpdateBatch(ref updateBatch);

            updateBatch.Dispose();
        }

        internal static GPUComponentSet ComputeComponentSet(in DefaultGPUComponents defaultGPUComponents,
            MeshRendererUpdateBatch.LightmapUsage lightmapUsage,
            MeshRendererUpdateBatch.BlendProbesUsage blendProbesUsage)
        {
            Assert.IsTrue(lightmapUsage != MeshRendererUpdateBatch.LightmapUsage.Unknown);
            Assert.IsTrue(blendProbesUsage != MeshRendererUpdateBatch.BlendProbesUsage.Unknown);

            bool useLightmaps = lightmapUsage == MeshRendererUpdateBatch.LightmapUsage.All;
            bool blendProbes = blendProbesUsage == MeshRendererUpdateBatch.BlendProbesUsage.AllEnabled;
            bool hasTree = false;

            return ComputeComponentSet(defaultGPUComponents,
                useLightmaps,
                blendProbes,
                hasTree);
        }

        internal static GPUComponentSet ComputeComponentSet(in DefaultGPUComponents defaultGPUComponents,
            InternalMeshRendererSettings rendererSettings,
            int lightmapIndex)
        {
            bool useLightmaps = LightmapUtils.UsesLightmaps(lightmapIndex);
            bool blendProbes = rendererSettings.LightProbeUsage == LightProbeUsage.BlendProbes;
            bool hasTree = rendererSettings.HasTree;

            return ComputeComponentSet(defaultGPUComponents,
                useLightmaps,
                blendProbes,
                hasTree);
        }

        internal static GPUComponentSet ComputeComponentSet(in DefaultGPUComponents defaultGPUComponents, bool useLightmaps, bool blendProbes, bool hasTree)
        {
            GPUComponentSet componentSet = defaultGPUComponents.requiredComponentSet;

            if (useLightmaps)
            {
                // Add per-instance lightmap parameters
                componentSet.Add(defaultGPUComponents.lightmapScaleOffset);
            }
            else
            {
                // Only add the component when needed to store blended results (shader will use the ambient probe when not present)
                if (blendProbes)
                    componentSet.Add(defaultGPUComponents.shCoefficients);
            }

            if (hasTree)
                componentSet.AddSet(defaultGPUComponents.speedTreeComponentSet);

            return componentSet;
        }

        void ValidateNoInstanceUsesBlendProbes(NativeArray<InstanceHandle> instances)
        {
            // Disable "Unreachable code detected" warning
#pragma warning disable CS0162
            if (!GPUResidentDrawer.EnableDeepValidation)
                return;

            using (new ProfilerMarker("DeepValidation.NoInstanceUsesBlendProbes").Auto())
            {
                if (AnyInstanceUseBlendProbes(instances))
                    Debug.LogError("One instance has LightProbeUsage == LightProbeUsage.BlendProbes whereas it wasn't expected.");
            }

#pragma warning restore CS0162
        }

        bool AnyInstanceUseBlendProbes(NativeArray<InstanceHandle> instances) => MeshRendererProcessorBurst.AnyInstanceUseBlendProbes(instances, ref m_InstanceDataSystem.renderWorld);

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private unsafe struct FindRenderersFromMaterialOrMeshJob : IJobParallelForBatch
        {
            public const int k_BatchSize = 128;

            [ReadOnly] public RenderWorld renderWorld;
            [ReadOnly] public NativeHashSet<EntityId> materialIDs;
            [ReadOnly] public NativeHashSet<EntityId> meshIDs;
            [ReadOnly] public NativeArray<EntityId> sortedExcludeRendererIDs;

            [WriteOnly] public NativeList<EntityId>.ParallelWriter selectedRenderIDsForMaterials;
            [WriteOnly] public NativeList<EntityId>.ParallelWriter selectedRenderIDsForMeshes;

            public void Execute(int startIndex, int count)
            {
                EntityId* renderersToAddForMaterialsPtr = stackalloc EntityId[k_BatchSize];
                var renderersToAddForMaterials = new UnsafeList<EntityId>(renderersToAddForMaterialsPtr, k_BatchSize);
                renderersToAddForMaterials.Length = 0;

                EntityId* renderersToAddForMeshesPtr = stackalloc EntityId[k_BatchSize];
                var renderersToAddForMeshes = new UnsafeList<EntityId>(renderersToAddForMeshesPtr, k_BatchSize);
                renderersToAddForMeshes.Length = 0;

                for (int index = 0; index < count; index++)
                {
                    int rendererIndex = startIndex + index;
                    EntityId rendererID = renderWorld.instanceIDs[rendererIndex];

                    // We ignore this renderer if it is in the excluded list.
                    if (sortedExcludeRendererIDs.BinarySearch(rendererID) >= 0)
                        continue;

                    EntityId meshID = renderWorld.meshIDs[rendererIndex];
                    if (meshIDs.Contains(meshID))
                    {
                        renderersToAddForMeshes.AddNoResize(rendererID);
                        // We can skip the material check if we found a mesh match since at this point
                        // the renderer is already added and will be processed by the mesh branch
                        continue;
                    }

                    EmbeddedArray32<EntityId> rendererMaterials = renderWorld.materialIDArrays[rendererIndex];
                    for (int materialIndex = 0; materialIndex < rendererMaterials.Length; materialIndex++)
                    {
                        var materialID = rendererMaterials[materialIndex];
                        if (materialIDs.Contains(materialID))
                        {
                            renderersToAddForMaterials.AddNoResize(rendererID);
                            break;
                        }
                    }
                }

                selectedRenderIDsForMaterials.AddRangeNoResize(renderersToAddForMaterialsPtr, renderersToAddForMaterials.Length);
                selectedRenderIDsForMeshes.AddRangeNoResize(renderersToAddForMeshesPtr, renderersToAddForMeshes.Length);
            }
        }

        void ValidateGPUArchetypesDidNotChange(NativeArray<InstanceHandle> instances,
           in MeshRendererUpdateBatch updateBatch,
           GPUComponentSet overrideComponentSet)
        {
            // Disable "Unreachable code detected" warning
#pragma warning disable CS0162
            if (!GPUResidentDrawer.EnableDeepValidation)
                return;

            using (new ProfilerMarker("DeepValidation.GPUArchetypesDidNotChange").Auto())
            {
                bool archetypeMightHaveChanged = updateBatch.HasAnyComponent(MeshRendererComponentMask.RendererSettings
                    | MeshRendererComponentMask.Lightmap
                    | MeshRendererComponentMask.GPUComponent);

                if (!archetypeMightHaveChanged)
                    return;

                if (DidGPUArchetypesChange(instances, updateBatch, overrideComponentSet))
                    Debug.LogError($"Unexpected GPUArchetype changes with the update type {updateBatch.updateType}.");
            }

#pragma warning restore CS0162
        }

        unsafe bool DidGPUArchetypesChange(NativeArray<InstanceHandle> instances, in MeshRendererUpdateBatch updateBatch, GPUComponentSet overrideComponentSet)
        {
            fixed (MeshRendererUpdateBatch* updateBatchPtr = &updateBatch)
            {
                return MeshRendererProcessorBurst.DidGPUArchetypeChange(m_ArchetypeManager,
                    m_InstanceDataSystem.defaultGPUComponents,
                    instances,
                    updateBatchPtr,
                    ref m_InstanceDataSystem.renderWorld,
                    overrideComponentSet);
            }
        }
    }
}
