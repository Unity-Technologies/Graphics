using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class BufferConversionPass : CompilationPass
    {
        public bool Execute(ref CompilationContext context)
        {
            var graph = context.graph;
            var result = new DataConcretizationResult();

            // Snapshotted: concretizing adds the buffer containers to the very collection being walked.
            using var originalDataViewIds = graph.DataViews.Snapshot();
            foreach (var dataViewId in originalDataViewIds)
                TryConcretize(graph.DataViews[dataViewId], context, graph, result);

            context.data.Create(result);
            return true;
        }

        void TryConcretize(DataView dataView, CompilationContext context, IGraph graph,
            DataConcretizationResult result)
        {
            switch (dataView.DataDescription)
            {
                case AttributeData attributeData: ConcretizeAttributeData(dataView, context, graph, result, attributeData); break;
                case StructuredData:              ConcretizeStructuredData(dataView, context, graph, result); break;
                case DeadListData:                ConcretizeDeadListData(dataView, context, graph, result); break;
                case EventListData eventListData: ConcretizeEventListData(dataView, context, graph, result, eventListData); break;
                case EventData:                   ConcretizeEventData(dataView, context, graph, result); break;
                case SortingData sortingData:     ConcretizeSortingData(dataView, graph, result, sortingData); break;
                case SortScratchData scratchData: ConcretizeSortScratchData(dataView, graph, result, scratchData); break;
                case DrawIndirectionData indirectionData: ConcretizeDrawIndirectionData(dataView, graph, result, indirectionData); break;
            }
        }

        // The keys an output update registers, as key-value pairs ahead of a counter.
        void ConcretizeSortingData(DataView dataView, IGraph graph, DataConcretizationResult result,
            SortingData sortingData)
        {
            var containerName = dataView.DataContainer.Name;
            var gpuBuffer = new VfxGpuBufferData(new VFXGPUBufferDesc
            {
                debugName = $"{containerName}_SortingKeys",
                size = sortingData.Capacity + 1,
                target = GraphicsBuffer.Target.Structured,
                stride = 8u,
                mode = ComputeBufferMode.Immutable,
            });

            var concreteRootViewId = graph.AddData($"{containerName}_SortingKeysBuffer", gpuBuffer);
            result.Add(dataView.Id, concreteRootViewId);
        }

        void ConcretizeSortScratchData(DataView dataView, IGraph graph, DataConcretizationResult result,
            SortScratchData scratchData)
        {
            if (scratchData.Capacity <= SortScratchData.MinCapacity)
                return;

            var containerName = dataView.DataContainer.Name;
            var gpuBuffer = new VfxGpuBufferData(new VFXGPUBufferDesc
            {
                debugName = $"{containerName}_SortScratch",
                size = scratchData.Capacity + 1,
                target = GraphicsBuffer.Target.Structured,
                stride = 8u,
                mode = ComputeBufferMode.Immutable,
            });

            var concreteRootViewId = graph.AddData($"{containerName}_SortScratchBuffer", gpuBuffer);
            result.Add(dataView.Id, concreteRootViewId);
        }

        void ConcretizeDrawIndirectionData(DataView dataView, IGraph graph, DataConcretizationResult result,
            DrawIndirectionData indirectionData)
        {
            var containerName = dataView.DataContainer.Name;
            var gpuBuffer = new VfxGpuBufferData(new VFXGPUBufferDesc
            {
                debugName = $"{containerName}_Indirection",
                size = indirectionData.Capacity + 1,
                target = GraphicsBuffer.Target.Structured,
                stride = 4u,
                mode = ComputeBufferMode.Immutable,
            });

            var concreteRootViewId = graph.AddData($"{containerName}_IndirectionBuffer", gpuBuffer);
            result.Add(dataView.Id, concreteRootViewId);
        }

        void ConcretizeAttributeData(DataView dataView, CompilationContext context, IGraph graph,
            DataConcretizationResult result, AttributeData attributeData)
        {
            if (dataView.Parent.HasValue && dataView.Parent.Value.DataDescription is EventData)
                return;

            var attributeSetLayouts = context.data.Get<AttributeSetLayoutCompilationData>();
            var attributeSetLayout = attributeSetLayouts[attributeData];
            if (!attributeSetLayout.Valid)
                return;

            var containerName = dataView.DataContainer.Name;
            var gpuBuffer = new VfxGpuBufferData(new VFXGPUBufferDesc
            {
                debugName = $"{containerName}_AttributeBuffer",
                target = GraphicsBuffer.Target.Raw,
                size = attributeSetLayout.GetBufferSize(),
                stride = 4u,
                mode = ComputeBufferMode.Immutable,
                capacity = attributeSetLayout.Capacity,
                layout = BuildAttributeLayoutDescs(attributeSetLayout)
            });

            var concreteRootViewId = graph.AddData($"{containerName}_AttributeBuffer", gpuBuffer);
            result.Add(dataView.Id, concreteRootViewId);
        }

        void ConcretizeStructuredData(DataView dataView, CompilationContext context, IGraph graph,
            DataConcretizationResult result)
        {
            if (!dataView.IsRoot)
                return;

            var dataLayoutContainer = context.data.Get<DataLayoutContainer>();
            if (!dataLayoutContainer.TryGetLayout(dataView.DataContainer.Id, out var layout))
                return;

            var containerName = dataView.DataContainer.Name;
            var gpuBuffer = new VfxGpuBufferData(new VFXGPUBufferDesc
            {
                debugName = containerName,
                target = GraphicsBuffer.Target.Raw,
                size = layout.GetBufferSize(),
                stride = 4u,
                mode = ComputeBufferMode.Dynamic
            });

            var concreteRootViewId = graph.AddData($"{containerName}_GraphValuesBuffer", gpuBuffer);

            // Add the old layout associated with the new container
            dataLayoutContainer.TryAddLayout(context.graph.DataViews[concreteRootViewId].DataContainer.Id, layout);

            result.Add(dataView.Id, concreteRootViewId);
        }

        void ConcretizeDeadListData(DataView dataView, CompilationContext context, IGraph graph,
            DataConcretizationResult result)
        {
            if (!dataView.Parent.HasValue)
                return;
            if (dataView.Parent.Value.DataDescription is not ParticleData particleData)
                return;

            var containerName = dataView.DataContainer.Name;
            var gpuBuffer = new VfxGpuBufferData(new VFXGPUBufferDesc
            {
                debugName = $"{containerName}_DeadList",
                target = GraphicsBuffer.Target.Structured,
                size = particleData.Capacity + 2,
                stride = 4u,
                mode = ComputeBufferMode.Immutable
            });

            var concreteRootViewId = graph.AddData($"{containerName}_DeadListBuffer", gpuBuffer);
            result.Add(dataView.Id, concreteRootViewId);
        }

        void ConcretizeEventListData(DataView dataView, CompilationContext context, IGraph graph,
            DataConcretizationResult result, EventListData eventListData)
        {
            var containerName = dataView.DataContainer.Name;
            var gpuBuffer = new VfxGpuBufferData(new VFXGPUBufferDesc
            {
                debugName = containerName,
                target = GraphicsBuffer.Target.Structured,
                size = eventListData.BufferSize,
                stride = 4u,
                mode = eventListData.IsCpu ? ComputeBufferMode.Dynamic : ComputeBufferMode.Immutable
            });

            var concreteRootViewId = graph.AddData($"{containerName}_EventListBuffer", gpuBuffer);
            result.Add(dataView.Id, concreteRootViewId);
        }

        void ConcretizeEventData(DataView dataView, CompilationContext context, IGraph graph,
            DataConcretizationResult result)
        {
            var containerName = dataView.DataContainer.Name;

            AttributeData attributeData = null;
            if (dataView.FindSubData(EventData.AttributeDataKey, out var attributeDataView))
                attributeData = attributeDataView.DataDescription as AttributeData;

            var spawnCountAttribute = VFXAttributesManager.ConvertToNewCompiler(VFXAttribute.SpawnCount);
            var attributes = new List<Attribute>();
            attributes.Add(spawnCountAttribute);

            // Add any extra attributes from the layout (if the spawner has user-defined spawn attributes).
            if (attributeData != null)
            {
                var attributeSetLayouts = context.data.Get<AttributeSetLayoutCompilationData>();
                var attributeSetLayout = attributeSetLayouts[attributeData];
                if (attributeSetLayout.Valid)
                {
                    foreach (var attribute in attributeSetLayout.Attributes)
                    {
                        if (attribute != spawnCountAttribute)
                            attributes.Add(attribute);
                    }
                }
            }

            (VFXLayoutElementDesc[] layout, uint stride, VFXCPUBufferData initialData) = BuildSpawnerCPUBufferData(attributes);

            var cpuBuffer = new VfxCpuBufferData(new VFXCPUBufferDesc
            {
                debugName = $"{containerName}_SpawnBuffer",
                capacity = 1u,
                stride = stride,
                layout = layout,
                initialData = initialData
            });

            var concreteRootViewId = graph.AddData($"{containerName}_SpawnBuffer", cpuBuffer);
            result.Add(dataView.Id, concreteRootViewId);
        }

        static VFXLayoutElementDesc[] BuildAttributeLayoutDescs(AttributeSetLayout attributeSetLayout)
        {
            var layoutDescs = new VFXLayoutElementDesc[(int)attributeSetLayout.Count];
            int i = 0;
            foreach (var attribute in attributeSetLayout.Attributes)
            {
                (uint bucketOffset, uint bucketSize, uint elementOffset) = attributeSetLayout.GetBucketLocation(attribute);
                layoutDescs[i++] = new VFXLayoutElementDesc()
                {
                    name = attribute.Name,
                    type = VFXExpression.GetVFXValueTypeFromType(attribute.Type),
                    offset = new VFXLayoutOffset()
                    {
                        bucket = bucketOffset,
                        element = elementOffset,
                        structure = bucketSize
                    },
                };
            }
            return layoutDescs;
        }

        static (VFXLayoutElementDesc[] layout, uint stride, VFXCPUBufferData initialData) BuildSpawnerCPUBufferData(List<Attribute> attributes)
        {
            var data = new VFXCPUBufferData();
            var layout = new VFXLayoutElementDesc[attributes.Count];
            uint elementOffset = 0;

            for (int i = 0; i < attributes.Count; ++i)
            {
                var attribute = attributes[i];
                ref var layoutElement = ref layout[i];
                layoutElement.name = attribute.Name;
                layoutElement.type = VFXExpression.GetVFXValueTypeFromType(attribute.Type);
                layoutElement.offset.bucket = 0u;
                layoutElement.offset.element = elementOffset;
                elementOffset += (uint)VFXExpressionHelper.GetSizeOfType(layoutElement.type);

                switch (layoutElement.type)
                {
                    case VFXValueType.Boolean: data.PushBool((bool)attribute.DefaultValue); break;
                    case VFXValueType.Float: data.PushFloat((float)attribute.DefaultValue); break;
                    case VFXValueType.Float2:
                        var v2 = (Vector2)attribute.DefaultValue;
                        data.PushFloat(v2.x); data.PushFloat(v2.y); break;
                    case VFXValueType.Float3:
                        var v3 = (Vector3)attribute.DefaultValue;
                        data.PushFloat(v3.x); data.PushFloat(v3.y); data.PushFloat(v3.z); break;
                    case VFXValueType.Float4:
                        var v4 = (Vector4)attribute.DefaultValue;
                        data.PushFloat(v4.x); data.PushFloat(v4.y); data.PushFloat(v4.z); data.PushFloat(v4.w); break;
                    case VFXValueType.Int32: data.PushInt((int)attribute.DefaultValue); break;
                    case VFXValueType.Uint32: data.PushUInt((uint)attribute.DefaultValue); break;
                    default: throw new System.NotImplementedException();
                }
            }

            uint stride = elementOffset;
            for (int i = 0; i < layout.Length; ++i)
                layout[i].offset.structure = stride;

            return (layout, stride, data);
        }
    }

    /// <summary>
    /// Maps each logical data view onto the root view of the concrete VFX buffer it was lowered to, so the
    /// later passes can rebind tasks from the logical data to the buffers. Those passes report back the data
    /// node each rebinding landed on, so that a task rebound afterwards can find the node writing the buffer
    /// it reads by looking its logical parents up instead of searching the graph for it.
    /// </summary>
    class DataConcretizationResult
    {
        readonly Dictionary<DataViewId, DataViewId> m_ConcreteRootViews = new();
        readonly Dictionary<(DataNodeId dataNode, DataViewId dataView), DataNodeId> m_ConcreteDataNodes = new();

        public void Add(DataViewId originalDataViewId, DataViewId concreteRootDataViewId)
        {
            m_ConcreteRootViews[originalDataViewId] = concreteRootDataViewId;
        }

        public bool TryGetConcreteDataView(DataViewId originalDataViewId, out DataViewId concreteRootDataViewId)
            => m_ConcreteRootViews.TryGetValue(originalDataViewId, out concreteRootDataViewId);

        public void AddDataNodeMapping(DataNodeId originalDataNodeId, DataViewId originalDataViewId,
            DataNodeId concreteDataNodeId)
        {
            m_ConcreteDataNodes[(originalDataNodeId, originalDataViewId)] = concreteDataNodeId;
        }

        public bool TryGetConcreteDataNode(DataNodeId originalDataNodeId, DataViewId originalDataViewId,
            out DataNodeId concreteDataNodeId)
            => m_ConcreteDataNodes.TryGetValue((originalDataNodeId, originalDataViewId), out concreteDataNodeId);

        /// <summary>
        /// Fills <paramref name="parentNodes"/> with the concrete counterparts of a logical node's parents,
        /// which is what a rebinding has to name now that binding no longer infers its parents. A parent
        /// that was never lowered is kept as is; one that was, but not under this view, is left out - the
        /// dependency on it is carried by the views it did lower.
        /// </summary>
        /// <param name="loweredTasks">
        /// The tasks lowered so far, which is what tells "this parent keeps its logical node" from
        /// "this parent has been rebound, just not under this view". Only the pass doing the lowering
        /// knows them; anything running before it passes none.
        /// </param>
        public void CollectConcreteParents(DataNode logicalDataNode, DataViewId logicalDataViewId,
            List<DataNodeId> parentNodes, HashSet<TaskNodeId> loweredTasks = null)
        {
            parentNodes.Clear();
            foreach (var parent in logicalDataNode.Parents)
            {
                if (TryGetConcreteDataNode(parent.Id, logicalDataViewId, out var concreteParentId))
                    parentNodes.Add(concreteParentId);
                else if (loweredTasks == null || !loweredTasks.Contains(parent.TaskNode.Id))
                    parentNodes.Add(parent.Id);
            }
        }
    }
}
