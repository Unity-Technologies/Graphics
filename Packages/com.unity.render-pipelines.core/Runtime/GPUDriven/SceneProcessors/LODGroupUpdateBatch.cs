using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    [Flags]
    internal enum LODGroupComponentMask
    {
        None = 0,
        WorldSpaceReferencePoint = 1 << 0,
        WorldSpaceSize = 1 << 1,
        GroupSettings = 1 << 2,
        ForceLOD = 1 << 3,
        LODBuffer = 1 << 4,

        PermutationCount = 1 << 5,
        AllBits = PermutationCount - 1
    }

    internal enum LODGroupUpdateBatchMode
    {
        MightIncludeNewInstances,
        OnlyKnownInstances,
    }

    internal struct LODGroupUpdateSection
    {
        public NativeArray<EntityId> instanceIDs;
        public NativeArray<float3> worldSpaceReferencePoints;
        public NativeArray<float> worldSpaceSizes;
        public NativeArray<InternalLODGroupSettings> lodGroupSettings;
        public NativeArray<byte> forceLODMask;
        public NativeArray<EmbeddedLODBuffer> lodBuffers;

        public LODGroupComponentMask BuildComponentMask()
        {
            LODGroupComponentMask mask = LODGroupComponentMask.None;

            if (worldSpaceReferencePoints.IsCreated)
                mask |= LODGroupComponentMask.WorldSpaceReferencePoint;

            if (worldSpaceSizes.IsCreated)
                mask |= LODGroupComponentMask.WorldSpaceSize;

            if (lodGroupSettings.IsCreated)
                mask |= LODGroupComponentMask.GroupSettings;

            if (forceLODMask.IsCreated)
                mask |= LODGroupComponentMask.ForceLOD;

            if (lodBuffers.IsCreated)
                mask |= LODGroupComponentMask.LODBuffer;

            return mask;
        }
    }

    // This struct stores array views of all the data we want to upload to the RenderWorld for LODGroup-like object.
    // The data does not have to come from the managed Unity LODGroup.
    // It can come from anywhere, but they are intended to represent something analogous to a LODGroup.
    // This struct works in the same way as MeshRendererUpdateBatch. See this struct for more details on the way it works.
    internal struct LODGroupUpdateBatch : IDisposable
    {
        public JaggedSpan<EntityId> instanceIDs;
        public JaggedSpan<float3> worldSpaceReferencePoints;
        public JaggedSpan<float> worldSpaceSizes;
        public JaggedSpan<InternalLODGroupSettings> lodGroupSettings;
        public JaggedSpan<byte> forceLODMask;
        public JaggedSpan<EmbeddedLODBuffer> lodBuffers;
        public LODGroupComponentMask componentMask;
        public LODGroupUpdateBatchMode updateMode;

        public LODGroupUpdateBatch(LODGroupComponentMask componentMask, LODGroupUpdateBatchMode updateMode, int initialCapacity, Allocator allocator)
        {
            this.updateMode = updateMode;
            this.componentMask = componentMask;
            this.instanceIDs = new JaggedSpan<EntityId>(initialCapacity, allocator);
            this.worldSpaceReferencePoints = new JaggedSpan<float3>(componentMask.HasAnyBit(LODGroupComponentMask.WorldSpaceReferencePoint) ? initialCapacity : 0, allocator);
            this.worldSpaceSizes = new JaggedSpan<float>(componentMask.HasAnyBit(LODGroupComponentMask.WorldSpaceSize) ? initialCapacity : 0, allocator);
            this.lodGroupSettings = new JaggedSpan<InternalLODGroupSettings>(componentMask.HasAnyBit(LODGroupComponentMask.GroupSettings) ? initialCapacity : 0, allocator);
            this.forceLODMask = new JaggedSpan<byte>(componentMask.HasAnyBit(LODGroupComponentMask.ForceLOD) ? initialCapacity : 0, allocator);
            this.lodBuffers = new JaggedSpan<EmbeddedLODBuffer>(componentMask.HasAnyBit(LODGroupComponentMask.LODBuffer) ? initialCapacity : 0, allocator);
        }

        public LODGroupUpdateBatch(in LODGroupUpdateSection section, LODGroupUpdateBatchMode updateMode, Allocator allocator) :
            this(section.BuildComponentMask(), updateMode, 1, allocator)
        {
            AddSection(section);
        }

        public void Dispose()
        {
            instanceIDs.Dispose();
            worldSpaceReferencePoints.Dispose();
            worldSpaceSizes.Dispose();
            lodGroupSettings.Dispose();
            forceLODMask.Dispose();
            lodBuffers.Dispose();
        }

        public int SectionCount => instanceIDs.sectionCount;

        public int TotalLength => instanceIDs.totalLength;

        public int GetSectionLength(int sectionIndex) => instanceIDs[sectionIndex].Length;

        public bool HasAnyComponent(LODGroupComponentMask bits) => componentMask.HasAnyBit(bits);

        public void AddSection(in LODGroupUpdateSection section)
        {
            Assert.IsTrue(section.BuildComponentMask() == componentMask);

            instanceIDs.Add(section.instanceIDs);

            if (HasAnyComponent(LODGroupComponentMask.GroupSettings))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.lodGroupSettings.Length);
                lodGroupSettings.Add(section.lodGroupSettings);
            }

            if (HasAnyComponent(LODGroupComponentMask.WorldSpaceReferencePoint))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.worldSpaceReferencePoints.Length);
                worldSpaceReferencePoints.Add(section.worldSpaceReferencePoints);
            }

            if (HasAnyComponent(LODGroupComponentMask.WorldSpaceSize))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.worldSpaceSizes.Length);
                worldSpaceSizes.Add(section.worldSpaceSizes);
            }

            if (HasAnyComponent(LODGroupComponentMask.ForceLOD))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.forceLODMask.Length);
                forceLODMask.Add(section.forceLODMask);
            }

            if (HasAnyComponent(LODGroupComponentMask.LODBuffer))
            {
                Assert.IsTrue(section.instanceIDs.Length == section.lodBuffers.Length);
                lodBuffers.Add(section.lodBuffers);
            }
        }

        internal void Validate()
        {
            // Disable "Unreachable code detected" warning
#pragma warning disable CS0162
            if (!GPUResidentDrawer.EnableValidation)
                return;

            using (new ProfilerMarker("LODGroupUpdateBatch.Validate").Auto())
            {
                ValidateImpl();
            }
#pragma warning restore CS0162
        }

        private void ValidateImpl()
        {
            if (!ValidateEmptyOrSameLayout(LODGroupComponentMask.WorldSpaceReferencePoint, worldSpaceReferencePoints, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(LODGroupComponentMask.WorldSpaceSize, worldSpaceSizes, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(LODGroupComponentMask.GroupSettings, lodGroupSettings, instanceIDs))
                return;

            if (!ValidateEmptyOrSameLayout(LODGroupComponentMask.LODBuffer, lodBuffers, instanceIDs))
                return;

            if (updateMode == LODGroupUpdateBatchMode.MightIncludeNewInstances)
            {
                if (!ValidateRequiredComponentIsPresent(LODGroupComponentMask.WorldSpaceReferencePoint))
                    return;

                if (!ValidateRequiredComponentIsPresent(LODGroupComponentMask.WorldSpaceSize))
                    return;

                if (!ValidateRequiredComponentIsPresent(LODGroupComponentMask.GroupSettings))
                    return;

                if (!ValidateRequiredComponentIsPresent(LODGroupComponentMask.LODBuffer))
                    return;
            }

            // This mode is only assumed to be used for transform-only updates for now.
            // In the future it could be extended so that each component can be incrementally updated individually.
            if (updateMode == LODGroupUpdateBatchMode.OnlyKnownInstances)
            {
                if (!ValidateRequiredComponentIsPresent(LODGroupComponentMask.WorldSpaceReferencePoint))
                    return;

                if (!ValidateRequiredComponentIsPresent(LODGroupComponentMask.WorldSpaceSize))
                    return;

                if (HasAnyComponent(LODGroupComponentMask.ForceLOD))
                {
                    Debug.LogError("LODGroupComponentMask.ForceLOD component is not supported in LODGroupUpdateBatchMode.OnlyKnownInstances mode");
                    return;
                }

                if (HasAnyComponent(LODGroupComponentMask.GroupSettings))
                {
                    Debug.LogError("LODGroupComponentMask.GroupSettings component is not supported in LODGroupUpdateBatchMode.OnlyKnownInstances mode");
                    return;
                }

                if (HasAnyComponent(LODGroupComponentMask.LODBuffer))
                {
                    Debug.LogError("LODGroupComponentMask.LODBuffer component is not supported in LODGroupUpdateBatchMode.OnlyKnownInstances mode");
                    return;
                }
            }

            if (!DeepValidateImpl())
                return;
        }

        private bool DeepValidateImpl()
        {
            // Disable "Unreachable code detected" warning
#pragma warning disable CS0162
            if (!GPUResidentDrawer.EnableDeepValidation)
                return true;

            using (new ProfilerMarker("LODGroupUpdateBatch.DeepValidate").Auto())
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
        private bool ValidateRequiredComponentIsPresent(LODGroupComponentMask component)
        {
            if (!HasAnyComponent(component))
            {
                Debug.LogError($"Invalid LODGroupUpdateBatch. {component} was not provided. This is required when using the update mode {updateMode}.");
                return false;
            }

            return true;
        }

        private bool ValidateEmptyOrSameLayout<T>(LODGroupComponentMask component, JaggedSpan<T> components, JaggedSpan<EntityId> instanceIDs) where T : unmanaged
        {
            if (!components.isEmpty && !components.HasSameLayout(instanceIDs))
            {
                Debug.LogError($"Invalid LODGroupUpdateBatch. {component} jagged span has an unexpected layout.");
                return false;
            }

            return true;
        }
    }
}
