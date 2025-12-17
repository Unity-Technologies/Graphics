using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    [Flags]
    internal enum MeshRendererComponentMask
    {
        None = 0,
        LocalToWorld = 1 << 0,
        PrevLocalToWorld = 1 << 1,
        Mesh = 1 << 2,
        Material = 1 << 3,
        SubMeshStartIndex = 1 << 4,
        LocalBounds = 1 << 5,
        RendererSettings = 1 << 6,
        ParentLODGroup = 1 << 7,
        LODMask = 1 << 8,
        MeshLodSettings = 1 << 9,
        Lightmap = 1 << 10,
        RendererPriority = 1 << 11,
        SceneCullingMask = 1 << 12,
        RenderingEnabled = 1 << 13,
        GPUComponent = 1 << 14,
    }

    internal struct GPUComponent
    {
        public int PropertyID;
        public int SizeInBytes;

        public GPUComponent(int propertyID, int sizeInBytes)
        {
            PropertyID = propertyID;
            SizeInBytes = sizeInBytes;
        }
    }

    internal struct GPUComponentUpdate
    {
        UnsafeList<byte> m_Data;
        GPUComponent m_Component;

        public static GPUComponentUpdate FromArray<T>(GPUComponent component, NativeArray<T> data) where T : unmanaged
        {
            var typeSize = UnsafeUtility.SizeOf<T>();
            Assert.IsTrue(typeSize == component.SizeInBytes);
            return new GPUComponentUpdate(component, data.Reinterpret<byte>(typeSize));
        }

        public GPUComponentUpdate(GPUComponent component, NativeArray<byte> data)
        {
            Assert.IsTrue(data.Length % component.SizeInBytes == 0);
            m_Data = data.AsUnsafeListReadOnly();
            m_Component = component;
        }

        public GPUComponent Component => m_Component;
        public int PropertyID => m_Component.PropertyID;
        public int StrideInBytes => m_Component.SizeInBytes;
        public NativeArray<byte> Data => m_Data.AsNativeArray();
    }

    internal struct GPUComponentJaggedUpdate : IDisposable
    {
        JaggedSpan<byte> m_Data;
        GPUComponent m_Component;

        public GPUComponentJaggedUpdate(int initialCapacity, Allocator allocator, GPUComponent component)
        {
            m_Data = new JaggedSpan<byte>(initialCapacity, allocator);
            m_Component = component;
        }

        public void Dispose()
        {
            m_Data.Dispose();
        }

        public void Append(in GPUComponentUpdate section)
        {
            Assert.IsTrue(PropertyID == section.PropertyID);
            Assert.IsTrue(StrideInBytes == section.StrideInBytes);
            m_Data.Add(section.Data);
        }

        public int PropertyID => m_Component.PropertyID;
        public int StrideInBytes => m_Component.SizeInBytes;
        public JaggedSpan<byte> Data => m_Data;
    }

    internal enum MeshRendererUpdateType
    {
        Null,
        MightIncludeNewInstances,
        NoStructuralChanges,
        RecreateOnlyKnownInstances,
        OnlyNewInstances,
    }

    internal struct MeshRendererUpdateSection
    {
        public NativeArray<EntityId> instanceIDs;
        public NativeArray<float4x4> localToWorlds;
        public NativeArray<float4x4> prevLocalToWorlds;
        public NativeArray<EntityId> meshIDs;
        public NativeArray<EntityId> materialIDs;
        public NativeArray<RangeInt> subMaterialRanges;
        public NativeArray<ushort> subMeshStartIndices;
        public NativeArray<AABB> localBounds;
        public NativeArray<InternalMeshRendererSettings> rendererSettings;
        public NativeArray<EntityId> parentLODGroupIDs;
        public NativeArray<byte> lodMasks;
        public NativeArray<InternalMeshLodRendererSettings> meshLodSettings;
        public NativeArray<short> lightmapIndices;
        public NativeArray<int> rendererPriorities;
        public NativeArray<GPUComponentUpdate> gpuComponentUpdates;
        public NativeBitArray renderingEnabled;
        public NativeArray<ulong> sceneCullingMasks;
        public ulong sharedSceneCullingMask;
    }

    // This struct stores array views of all the data we want to upload to the RenderWorld for MeshRenderer-like object.
    // The data does not have to come from the managed Unity MeshRenderer.
    // It can come from anywhere, but it is expected to represent something analogous to a MeshRenderer.
    //
    // We use a custom JaggedSpan struct to store each component array view. It allows us to represent a logical sequence with multiple memory blocks (sections).
    // This is very important for DOTS which stores Entities data in chunks containing at most 128 entities.
    // Using JaggedSpans here allow our jobs to go wide over multiple chunks/sections when uploading data to the RenderWorld.
    // This is much faster than doing one update batch per EntityId chunk since it would drastically reduce parallelism.
    //
    // Most component sequences are optional. If nothing is provided, the static readonly default values will be used when needed.
    //
    // With MeshRendererUpdateType.NoStructuralChanges, GPUResidentDrawer will assume this batch deals only with known instances.
    // It will use the provided components to update the RenderWorld representation of the instances incrementally.
    // No components are expected to be added or removed in this mode, including the GPU components.
    // This is very useful to do fast incremental updates like for transforms or material property overrides for example.
    // Any other update type will cause the instances in the RenderWorld to be rebuilt.
    // So in that case, if a component sequence is not provided then a default value will be used to rebuild the instance.
    internal struct MeshRendererUpdateBatch : IDisposable
    {
        public enum LightmapUsage
        {
            Unknown,
            All,
            None
        }

        public enum BlendProbesUsage
        {
            Unknown,
            AllEnabled,
            AllDisabled
        }

        public JaggedSpan<EntityId> instanceIDs;
        public JaggedSpan<float4x4> localToWorlds;
        public JaggedSpan<float4x4> prevLocalToWorlds;
        public JaggedSpan<EntityId> meshIDs;
        public JaggedSpan<EntityId> materialIDs; // Buffer indexed using subMaterialRanges
        public JaggedSpan<RangeInt> subMaterialRanges;
        public JaggedSpan<ushort> subMeshStartIndices;
        public JaggedSpan<AABB> localBounds;
        public JaggedSpan<InternalMeshRendererSettings> rendererSettings;
        public JaggedSpan<EntityId> parentLODGroupIDs;
        public JaggedSpan<byte> lodMasks;
        public JaggedSpan<InternalMeshLodRendererSettings> meshLodSettings;
        public JaggedSpan<short> lightmapIndices;
        public JaggedSpan<int> rendererPriorities;
        public JaggedSpan<ulong> sceneCullingMasks;
        public NativeList<ulong> sharedSceneCullingMasks;
        public NativeArray<GPUComponentJaggedUpdate> gpuComponentUpdates;
        public JaggedBitSpan renderingEnabled;

        public MeshRendererComponentMask componentMask;
        public MeshRendererUpdateType updateType;
        public LightmapUsage lightmapUsage;
        public BlendProbesUsage blendProbesUsage;
        public bool useSharedSceneCullingMask; // If true, each sceneCullingMask array in a section contains only one entry that is shared for all instances.
        internal bool mightIncludeTrees; // Only used internally to support Speed Tree with GameObjects.

        public MeshRendererUpdateBatch(MeshRendererComponentMask componentMask,
            NativeArray<GPUComponent> gpuComponents,
            MeshRendererUpdateType updateType,
            LightmapUsage lightmapUsage,
            BlendProbesUsage blendProbesUsage,
            bool useSharedSceneCullingMask,
            int initialCapacity,
            Allocator allocator)
        {
            if (useSharedSceneCullingMask)
                Assert.IsTrue(componentMask.HasAnyBit(MeshRendererComponentMask.SceneCullingMask));

            this.componentMask = componentMask;
            this.updateType = updateType;
            this.lightmapUsage = lightmapUsage;
            this.blendProbesUsage = blendProbesUsage;
            this.useSharedSceneCullingMask = useSharedSceneCullingMask;
            this.mightIncludeTrees = false;

            instanceIDs = new JaggedSpan<EntityId>(initialCapacity, allocator);
            localToWorlds = new JaggedSpan<float4x4>(componentMask.HasAnyBit(MeshRendererComponentMask.LocalToWorld | MeshRendererComponentMask.LocalBounds) ? initialCapacity : 0, allocator);
            prevLocalToWorlds = new JaggedSpan<float4x4>(componentMask.HasAnyBit(MeshRendererComponentMask.PrevLocalToWorld) ? initialCapacity : 0, allocator);
            meshIDs = new JaggedSpan<EntityId>(componentMask.HasAnyBit(MeshRendererComponentMask.Mesh) ? initialCapacity : 0, allocator);
            materialIDs = new JaggedSpan<EntityId>(componentMask.HasAnyBit(MeshRendererComponentMask.Material) ? initialCapacity : 0, allocator);
            subMaterialRanges = new JaggedSpan<RangeInt>(componentMask.HasAnyBit(MeshRendererComponentMask.Material) ? initialCapacity : 0, allocator);
            subMeshStartIndices = new JaggedSpan<ushort>(componentMask.HasAnyBit(MeshRendererComponentMask.SubMeshStartIndex) ? initialCapacity : 0, allocator);
            localBounds = new JaggedSpan<AABB>(componentMask.HasAnyBit(MeshRendererComponentMask.LocalBounds) ? initialCapacity : 0, allocator);
            rendererSettings = new JaggedSpan<InternalMeshRendererSettings>(componentMask.HasAnyBit(MeshRendererComponentMask.RendererSettings) ? initialCapacity : 0, allocator);
            parentLODGroupIDs = new JaggedSpan<EntityId>(componentMask.HasAnyBit(MeshRendererComponentMask.ParentLODGroup) ? initialCapacity : 0, allocator);
            lodMasks = new JaggedSpan<byte>(componentMask.HasAnyBit(MeshRendererComponentMask.LODMask) ? initialCapacity : 0, allocator);
            meshLodSettings = new JaggedSpan<InternalMeshLodRendererSettings>(componentMask.HasAnyBit(MeshRendererComponentMask.MeshLodSettings) ? initialCapacity : 0, allocator);
            lightmapIndices = new JaggedSpan<short>(componentMask.HasAnyBit(MeshRendererComponentMask.Lightmap) ? initialCapacity : 0, allocator);
            rendererPriorities = new JaggedSpan<int>(componentMask.HasAnyBit(MeshRendererComponentMask.RendererPriority) ? initialCapacity : 0, allocator);
            sceneCullingMasks = new JaggedSpan<ulong>(componentMask.HasAnyBit(MeshRendererComponentMask.SceneCullingMask) ? initialCapacity : 0, allocator);
            sharedSceneCullingMasks = new NativeList<ulong>(useSharedSceneCullingMask ? initialCapacity : 0, allocator);
            renderingEnabled = new JaggedBitSpan(componentMask.HasAnyBit(MeshRendererComponentMask.RenderingEnabled) ? initialCapacity : 0, allocator);

            if (componentMask.HasAnyBit(MeshRendererComponentMask.GPUComponent))
            {
                gpuComponentUpdates = new NativeArray<GPUComponentJaggedUpdate>(gpuComponents.Length, allocator);
                for (int i = 0; i < gpuComponents.Length; i++)
                {
                    gpuComponentUpdates[i] = new GPUComponentJaggedUpdate(initialCapacity, allocator, gpuComponents[i]);
                }
            }
            else
            {
                Assert.IsTrue(gpuComponents.Length == 0);
                gpuComponentUpdates = new NativeArray<GPUComponentJaggedUpdate>(0, allocator);
            }
        }

        public void Dispose()
        {
            instanceIDs.Dispose();
            localToWorlds.Dispose();
            prevLocalToWorlds.Dispose();
            meshIDs.Dispose();
            materialIDs.Dispose();
            subMaterialRanges.Dispose();
            subMeshStartIndices.Dispose();
            localBounds.Dispose();
            rendererSettings.Dispose();
            parentLODGroupIDs.Dispose();
            lodMasks.Dispose();
            meshLodSettings.Dispose();
            lightmapIndices.Dispose();
            rendererPriorities.Dispose();
            sceneCullingMasks.Dispose();
            sharedSceneCullingMasks.Dispose();
            renderingEnabled.Dispose();
            foreach (GPUComponentJaggedUpdate update in gpuComponentUpdates)
                update.Dispose();
            gpuComponentUpdates.Dispose();
        }

        public int SectionCount => instanceIDs.sectionCount;

        public int TotalLength => instanceIDs.totalLength;

        public int GetSectionLength(int sectionIndex) => instanceIDs[sectionIndex].Length;

        public bool HasAnyComponent(MeshRendererComponentMask bits) => componentMask.HasAnyBit(bits);

        public NativeArray<float4x4> GetLocalToWorldSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.LocalToWorld | MeshRendererComponentMask.LocalBounds) ? localToWorlds[index] : default;
        public NativeArray<AABB> GetLocalBoundsSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.LocalBounds) ? localBounds[index] : default;
        public NativeArray<EntityId> GetMaterialSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.Material) ? materialIDs[index] : default;
        public NativeArray<RangeInt> GetSubMaterialRangeSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.Material) ? subMaterialRanges[index] : default;
        public NativeArray<EntityId> GetMeshSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.Mesh) ? meshIDs[index] : default;
        public NativeArray<short> GetLightmapIndexSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.Lightmap) ? lightmapIndices[index] : default;
        public NativeArray<int> GetRendererPrioritySectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.RendererPriority) ? rendererPriorities[index] : default;
        public NativeArray<ushort> GetSubMeshStartIndexSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.SubMeshStartIndex) ? subMeshStartIndices[index] : default;
        public NativeArray<InternalMeshRendererSettings> GetRendererSettingsSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.RendererSettings) ? rendererSettings[index] : default;
        public NativeArray<EntityId> GetParentLODGroupIDSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.ParentLODGroup) ? parentLODGroupIDs[index] : default;
        public NativeArray<byte> GetLODMaskSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.LODMask) ? lodMasks[index] : default;
        public NativeArray<InternalMeshLodRendererSettings> GetMeshLodSettingsSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.MeshLodSettings) ? meshLodSettings[index] : default;
        public UnsafeBitArray GetRenderingEnabledSectionOrDefault(int index) => HasAnyComponent(MeshRendererComponentMask.RenderingEnabled) ? renderingEnabled[index] : default;

        public void AddSection(in MeshRendererUpdateSection section)
        {
            instanceIDs.Add(section.instanceIDs);

            if (HasAnyComponent(MeshRendererComponentMask.LocalToWorld | MeshRendererComponentMask.LocalBounds))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.localToWorlds.Length);
                localToWorlds.Add(section.localToWorlds);
            }
            else
            {
                Assert.IsTrue(!section.localToWorlds.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.PrevLocalToWorld))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.prevLocalToWorlds.Length);
                prevLocalToWorlds.Add(section.prevLocalToWorlds);
            }
            else
            {
                Assert.IsTrue(!section.prevLocalToWorlds.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.Mesh))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.meshIDs.Length);
                meshIDs.Add(section.meshIDs);
            }
            else
            {
                Assert.IsTrue(!section.meshIDs.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.Material))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.subMaterialRanges.Length);

                materialIDs.Add(section.materialIDs);
                subMaterialRanges.Add(section.subMaterialRanges);
            }
            else
            {
                Assert.IsTrue(!section.materialIDs.IsCreated);
                Assert.IsTrue(!section.subMaterialRanges.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.SubMeshStartIndex))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.subMeshStartIndices.Length);
                subMeshStartIndices.Add(section.subMeshStartIndices);
            }
            else
            {
                Assert.IsTrue(!section.subMeshStartIndices.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.LocalBounds))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.localBounds.Length);
                localBounds.Add(section.localBounds);
            }
            else
            {
                Assert.IsTrue(!section.localBounds.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.RendererSettings))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.rendererSettings.Length);
                rendererSettings.Add(section.rendererSettings);
            }
            else
            {
                Assert.IsTrue(!section.rendererSettings.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.ParentLODGroup))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.parentLODGroupIDs.Length);
                parentLODGroupIDs.Add(section.parentLODGroupIDs);
            }
            else
            {
                Assert.IsTrue(!section.parentLODGroupIDs.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.LODMask))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.lodMasks.Length);
                lodMasks.Add(section.lodMasks);
            }
            else
            {
                Assert.IsTrue(!section.lodMasks.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.MeshLodSettings))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.meshLodSettings.Length);
                meshLodSettings.Add(section.meshLodSettings);
            }
            else
            {
                Assert.IsTrue(!section.meshLodSettings.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.Lightmap))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.lightmapIndices.Length);
                lightmapIndices.Add(section.lightmapIndices);
            }
            else
            {
                Assert.IsTrue(!section.lightmapIndices.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.RendererPriority))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.rendererPriorities.Length);
                rendererPriorities.Add(section.rendererPriorities);
            }
            else
            {
                Assert.IsTrue(!section.rendererPriorities.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.SceneCullingMask))
            {
                if (useSharedSceneCullingMask)
                {
                    Assert.IsTrue(!section.sceneCullingMasks.IsCreated);
                    sharedSceneCullingMasks.Add(section.sharedSceneCullingMask);
                }
                else
                {
                    Assert.IsTrue(section.instanceIDs.Length == section.sceneCullingMasks.Length);
                    Assert.IsTrue(section.sharedSceneCullingMask == 0);
                    sceneCullingMasks.Add(section.sceneCullingMasks);
                }
            }
            else
            {
                Assert.IsTrue(!section.sceneCullingMasks.IsCreated);
                Assert.IsTrue(section.sharedSceneCullingMask == 0);
            }

            if (HasAnyComponent(MeshRendererComponentMask.RenderingEnabled))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.renderingEnabled.Length);
                renderingEnabled.Add(section.renderingEnabled);
            }
            else
            {
                Assert.IsTrue(!section.renderingEnabled.IsCreated);
            }

            if (HasAnyComponent(MeshRendererComponentMask.GPUComponent))
            {
                // Layout must match exactly
                Assert.IsTrue(gpuComponentUpdates.Length == section.gpuComponentUpdates.Length);

                for (int i = 0; i < section.gpuComponentUpdates.Length; i++)
                {
                    ref GPUComponentJaggedUpdate jaggedUpdate = ref gpuComponentUpdates.ElementAtRW(i);
                    jaggedUpdate.Append(section.gpuComponentUpdates.ElementAt(i));
                }
            }
            else
            {
                Assert.IsTrue(!section.gpuComponentUpdates.IsCreated);
            }
        }

        internal void Validate()
        {
            // Disable "Unreachable code detected" warning
#pragma warning disable CS0162
            if (!GPUResidentDrawer.EnableValidation)
                return;

            using (new ProfilerMarker("MeshRendererUpdateBatch.Validate").Auto())
            {
                ValidateImpl();
            }
#pragma warning restore CS0162
        }

        private void ValidateImpl()
        {
            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.LocalToWorld, localToWorlds, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.PrevLocalToWorld, prevLocalToWorlds, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.Mesh, meshIDs, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.Material, subMaterialRanges, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.SubMeshStartIndex, subMeshStartIndices, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.LocalBounds, localBounds, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.RendererSettings, rendererSettings, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.ParentLODGroup, parentLODGroupIDs, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.LODMask, lodMasks, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.MeshLodSettings, meshLodSettings, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.Lightmap, lightmapIndices, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(MeshRendererComponentMask.RendererPriority, rendererPriorities, instanceIDs))
                return;

            if (!ValidateEmptyOrSameSectionCount(MeshRendererComponentMask.Material, materialIDs, instanceIDs))
                return;

            if (!ValidateSceneCullingMask(sceneCullingMasks, sharedSceneCullingMasks, useSharedSceneCullingMask, instanceIDs))
                return;

            foreach (GPUComponentJaggedUpdate jaggedUpdate in gpuComponentUpdates)
            {
                if (!ValidateGPUComponentUpdates(jaggedUpdate, instanceIDs))
                    return;
            }

            if (updateType == MeshRendererUpdateType.NoStructuralChanges)
            {
                if (HasAnyComponent(MeshRendererComponentMask.PrevLocalToWorld))
                {
                    // This component must only be set when new instances might be created.
                    // It is only useful to generate correct motion vectors when objects that just got created moved before they got rendered.
                    // Otherwise previous local to worlds will be handled automatically by GRD.
                    Debug.LogError("Invalid MeshRendererUpdateBatch. PrevLocalToWorld component was set in MeshRendererUpdateType.NoStructuralChanges.");
                    return;
                }
            }

            if (HasAnyComponent(MeshRendererComponentMask.PrevLocalToWorld))
            {
                if (!HasAnyComponent(MeshRendererComponentMask.LocalToWorld))
                {
                    Debug.LogError("Invalid MeshRendererUpdateBatch. PrevLocalToWorld component was set, but not the LocalToWorld.");
                    return;
                }
            }

            if (HasAnyComponent(MeshRendererComponentMask.LocalBounds))
            {
                // Note that we dont check for MeshRendererComponentMask.LocalToWorld here.
                // This is because setting the LocalToWorld bit means the matrices have changed, which is not always what we want.
                // Sometimes the matrices won't have changed, but they still need to be provided alongside the local bounds so that we can compute the new world bounds.
                // GRD doesn't store the local to worlds on the CPU. That would be wasteful memory wise.
                if (!localToWorlds.HasSameLayout(instanceIDs))
                {
                    Debug.LogError("Invalid MeshRendererUpdateBatch. LocalBounds component was set, but local to worlds were not provided.");
                    return;
                }
            }

            if (!ValidateNoDuplicatePropertyID(gpuComponentUpdates))
                return;

            if (!DeepValidateImpl())
                return;
        }

        private bool DeepValidateImpl()
        {
            // Disable "Unreachable code detected" warning
#pragma warning disable CS0162
            if (!GPUResidentDrawer.EnableDeepValidation)
                return true;

            using (new ProfilerMarker("MeshRendererUpdateBatch.DeepValidate").Auto())
            {
                if (instanceIDs.HasDuplicates())
                {
                    Debug.LogError("MeshRendererUpdateBatch contains dupplicate instanceIDs.");
                    return false;
                }
            }

            return true;
#pragma warning restore CS0162
        }

        private bool ValidateSceneCullingMask(JaggedSpan<ulong> sceneCullingMasks,
            NativeList<ulong> sharedSceneCullingMasks,
            bool useSharedSceneCullingMask,
            JaggedSpan<EntityId> instanceIDs)
        {
            if (useSharedSceneCullingMask)
            {
                if (!sceneCullingMasks.isEmpty)
                {
                    Debug.LogError("Invalid MeshRendererUpdateBatch. MeshRendererUpdateBatch.useSharedSceneCullingMask is true but MeshRendererUpdateBatch.sceneCullingMasks is not empty.");
                    return false;
                }

                if (!sharedSceneCullingMasks.IsEmpty && sharedSceneCullingMasks.Length != instanceIDs.sectionCount)
                {
                    Debug.LogError($"Invalid MeshRendererUpdateBatch. MeshRendererUpdateBatch.sharedSceneCullingMasks has an unexpected layout.");
                    return false;
                }
            }
            else
            {
                if (!sharedSceneCullingMasks.IsEmpty)
                {
                    Debug.LogError("Invalid MeshRendererUpdateBatch. MeshRendererUpdateBatch.useSharedSceneCullingMask is false but MeshRendererUpdateBatch.sharedSceneCullingMasks is not empty.");
                    return false;
                }

                if (!sceneCullingMasks.isEmpty && !sceneCullingMasks.HasSameLayout(instanceIDs))
                {
                    Debug.LogError($"Invalid MeshRendererUpdateBatch. MeshRendererUpdateBatch.sceneCullingMasks has an unexpected layout.");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateGPUComponentUpdates(in GPUComponentJaggedUpdate update, JaggedSpan<EntityId> instanceIDs)
        {
            if (!HasSameLayout(update, instanceIDs))
            {
                // Replace when Shader.PropertyIDToName API lands
                // string propertyName = Shader.PropertyIDToName(update.PropertyID);
                string propertyName = "<unknown>";
                Debug.LogError($"Invalid MeshRendererUpdateBatch. Material property update jagged span has an unexpected layout. Shader property: \"{propertyName}\". StrideInBytes: ({update.StrideInBytes}).");
                return false;
            }

            if (update.StrideInBytes % sizeof(uint) != 0)
            {
                // Replace when Shader.PropertyIDToName API lands
                // string propertyName = Shader.PropertyIDToName(update.PropertyID);
                string propertyName = "<unknown>";
                Debug.LogError($"Invalid MeshRendererUpdateBatch. Material property size must be a multiple of 4. ByteAddressBuffer only works at 32-bits granularity. Shader property: \"{propertyName}\". StrideInBytes: ({update.StrideInBytes}).");
                return false;
            }

            return true;
        }

        private bool ValidateNoDuplicatePropertyID(in NativeArray<GPUComponentJaggedUpdate> updates)
        {
            if (updates.Length == 0)
                return true;

            var uniqueIDs = new NativeHashSet<int>(updates.Length, Allocator.Temp);

            for (int i = 0; i < updates.Length; i++)
            {
                int propertyID = updates[i].PropertyID;
                if (!uniqueIDs.Add(propertyID))
                {
                    // Replace when Shader.PropertyIDToName API lands
                    // string propertyName = Shader.PropertyIDToName(propertyID);
                    string propertyName = "<unknown>";
                    Debug.LogError($"Multiple MaterialPropertyJaggedUpdate refer to the same shader property \"{propertyName}\")");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateEmptyOrSameLayout<T>(MeshRendererComponentMask component, JaggedSpan<T> components, JaggedSpan<EntityId> instanceIDs) where T : unmanaged
        {
            if (!components.isEmpty && !components.HasSameLayout(instanceIDs))
            {
                Debug.LogError($"Invalid MeshRendererUpdateBatch. {component} jagged span has an unexpected layout.");
                return false;
            }

            return true;
        }

        private bool ValidateEmptyOrSameSectionCount<T>(MeshRendererComponentMask component, JaggedSpan<T> components, JaggedSpan<EntityId> instanceIDs) where T : unmanaged
        {
            if (!components.isEmpty && components.sectionCount != instanceIDs.sectionCount)
            {
                Debug.LogError($"Invalid MeshRendererUpdateBatch. {component} jagged span has an unexpected SectionCount ({components.sectionCount}). Expected value is ({instanceIDs.sectionCount})");
                return false;
            }

            return true;
        }

        private bool HasSameLayout(in GPUComponentJaggedUpdate update, in JaggedSpan<EntityId> instanceIDs)
        {
            if (update.Data.sectionCount != instanceIDs.sectionCount)
                return false;

            for (int i = 0; i < SectionCount; i++)
            {
                if ((update.Data[i].Length / update.StrideInBytes) != instanceIDs[i].Length)
                    return false;
            }

            return true;
        }
    }
}
