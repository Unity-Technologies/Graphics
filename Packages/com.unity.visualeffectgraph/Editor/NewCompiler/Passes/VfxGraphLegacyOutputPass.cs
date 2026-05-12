using System;
using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VfxGraphLegacyCompilationOutput
    {
        public List<UnityEditor.VFX.VFXExpressionDesc> SheetExpressions { get; } = new();
        public List<UnityEditor.VFX.VFXExpressionDesc> SheetExpressionsPerSpawnEventAttribute { get; } = new();
        public List<UnityEditor.VFX.VFXExpressionValueContainerDesc> SheetValues { get; } = new();
        public List<UnityEditor.VFX.VFXExposedMapping> SheetExposed { get; } = new();
        public List<UnityEditor.VFX.VFXEditorSystemDesc> SystemDescs { get; } = new();
        public List<UnityEditor.VFX.VFXEventDesc> EventDescs { get; } = new();
        public List<UnityEditor.VFX.VFXGPUBufferDesc> GpuBufferDescs { get; } = new();
        public List<UnityEditor.VFX.VFXCPUBufferDesc> CpuBufferDescs { get; } = new();
        public List<UnityEditor.VFX.VFXTemporaryGPUBufferDesc> TemporaryBufferDescs { get; } = new();
        public List<UnityEditor.VFX.VFXShaderSourceDesc> ShaderSourceDescs { get; } = new();
        public UnityEngine.VFX.VFXCompilationMode CompilationMode { get; set; } = UnityEngine.VFX.VFXCompilationMode.Edition;
        public List<UnityEngine.Object> Objects { get; } = new();
        public uint Version { get; set; }

        public VisualEffectAssetDesc GenerateAssetDesc()
        {
            var vfxAssetDesc = new VisualEffectAssetDesc();
            vfxAssetDesc.compilationMode = VFXCompilationMode.Runtime;
            vfxAssetDesc.systemDesc = SystemDescs.ToArray();
            vfxAssetDesc.cpuBufferDesc = CpuBufferDescs.ToArray();
            vfxAssetDesc.gpuBufferDesc = GpuBufferDescs.ToArray();
            vfxAssetDesc.temporaryBufferDesc = TemporaryBufferDescs.ToArray();
            vfxAssetDesc.shaderSourceDesc = ShaderSourceDescs.ToArray();
            vfxAssetDesc.sheet = new VFXExpressionSheet()
            {
                exposed = SheetExposed.ToArray(),
                expressions = SheetExpressions.ToArray(),
                expressionsPerSpawnEventAttribute = SheetExpressionsPerSpawnEventAttribute.ToArray(),
                values = SheetValues.ToArray()
            };
            vfxAssetDesc.eventDesc = EventDescs.ToArray();
            vfxAssetDesc.rendererSettings = new()
            {
                motionVectorGenerationMode = MotionVectorGenerationMode.Camera,
                shadowCastingMode = ShadowCastingMode.Off
            };
            vfxAssetDesc.instancingDisabledReason = VFXInstancingDisabledReason.Unknown;

            return vfxAssetDesc;
        }
    }

    class VfxGraphLegacyOutputPass : DataGenerationPass<VfxGraphLegacyCompilationOutput>
    {
        VfxGraphLegacyCompilationOutput m_currentOutput;

        static readonly Dictionary<System.Type, UnityEngine.VFX.VFXValueType> s_ValueTypeConversion = new()
        {
            { typeof(float), UnityEngine.VFX.VFXValueType.Float },
            { typeof(Vector2), UnityEngine.VFX.VFXValueType.Float2 },
            { typeof(Vector3), UnityEngine.VFX.VFXValueType.Float3 },
            { typeof(Vector4), UnityEngine.VFX.VFXValueType.Float4 },
            { typeof(Color), UnityEngine.VFX.VFXValueType.Float4 },
            { typeof(int), UnityEngine.VFX.VFXValueType.Int32 },
            { typeof(uint), UnityEngine.VFX.VFXValueType.Uint32 },
            { typeof(EntityId), UnityEngine.VFX.VFXValueType.EntityId },
            { typeof(Texture2D), UnityEngine.VFX.VFXValueType.Texture2D },
            { typeof(Texture2DArray), UnityEngine.VFX.VFXValueType.Texture2DArray },
            { typeof(Texture3D), UnityEngine.VFX.VFXValueType.Texture3D },
            { typeof(Cubemap), UnityEngine.VFX.VFXValueType.TextureCube },
            { typeof(CubemapArray), UnityEngine.VFX.VFXValueType.TextureCubeArray },
            { typeof(Matrix4x4), UnityEngine.VFX.VFXValueType.Matrix4x4 },
            { typeof(AnimationCurve), UnityEngine.VFX.VFXValueType.Curve },
            { typeof(Gradient), UnityEngine.VFX.VFXValueType.ColorGradient },
            { typeof(Mesh), UnityEngine.VFX.VFXValueType.Mesh },
            { typeof(SkinnedMeshRenderer), UnityEngine.VFX.VFXValueType.SkinnedMeshRenderer },
            { typeof(bool), UnityEngine.VFX.VFXValueType.Boolean },
            { typeof(GraphicsBuffer), UnityEngine.VFX.VFXValueType.Buffer },
        };

        readonly Dictionary<IDataDescription, uint> m_GpuBufferDescIndices = new();
        readonly Dictionary<IDataDescription, uint> m_CpuBufferDescIndices = new();
        readonly Dictionary<DataNodeId, uint> m_ValuesExpressionIndices = new();
        readonly Dictionary<VfxGraphLegacyParticleSystemContainer.ParticleSystem, int> m_ParticleSystemIndices = new();
        readonly List<uint> m_StartSystems = new();
        readonly List<uint> m_StopSystems = new();

        static UnityEngine.VFX.VFXValueType GetVFXValueTypeFromType(System.Type type) => s_ValueTypeConversion.TryGetValue(type, out var valueType) ? valueType : UnityEngine.VFX.VFXValueType.None;

        public VfxGraphLegacyCompilationOutput Execute(ref CompilationContext context)
        {
            VfxGraphLegacyCompilationOutput output = new();
            Cleanup();
            m_currentOutput = output;
            m_currentOutput.Version = 7;

            AddDataContainerSources(ref context);

            GenerateExpressionSheet(ref context);
            GenerateBufferDescriptions(ref context);
            GenerateSystemDescs(ref context);

            output.EventDescs.Add(new() { name = UnityEngine.VFX.VisualEffectAsset.PlayEventName, startSystems = m_StartSystems.ToArray(), stopSystems = Array.Empty<uint>(), initSystems = Array.Empty<uint>() });
            output.EventDescs.Add(new() { name = UnityEngine.VFX.VisualEffectAsset.StopEventName, startSystems = Array.Empty<uint>(), stopSystems = m_StopSystems.ToArray(), initSystems = Array.Empty<uint>() });

            Cleanup();

            return output;
        }

        uint AddExpressionRecursively(VFXExpression expression)
        {
            List<uint> parentExpressionIndices = new();
            foreach (var parentExpression in expression.parents)
            {
                var parentExpressionValue = AddExpressionRecursively(parentExpression);
                parentExpressionIndices.Add(parentExpressionValue);
            }

            // See VFXExpressionAbstract GetOperands for reference
            var data = new VFXExpression.Operands(-1);
            for (int i = 0; i < parentExpressionIndices.Count; i++)
                data[i] = (int)parentExpressionIndices[i];
            for (int i = 0; i < expression.additionalOperands.Length; i++)
                data[VFXExpression.Operands.OperandCount - expression.additionalOperands.Length + i] = expression.additionalOperands[i];

            uint vfxExpressionIndex = AddExpression(expression.operation, data[0], data[1], data[2], data[3]);

            if (expression.Is(VFXExpression.Flags.Value))
            {
                m_currentOutput.SheetValues.Add(CreateValueContainerDesc(expression, vfxExpressionIndex));
            }

            return vfxExpressionIndex;
        }

        void AddDataContainerSources(ref CompilationContext context)
        {
            var generatedCodeContainer = context.data.Get<GeneratedCodeContainer>();
            foreach (var dataContainer in context.graph.DataContainers)
            {
                string sourceCode = generatedCodeContainer.Find(dataContainer.Id);
                if (sourceCode != null)
                {
                    AddShaderSourceDesc($"{dataContainer.Name}.hlsl", sourceCode, false);
                }
            }
        }

        void GenerateExpressionSheet(ref CompilationContext context)
        {
            foreach (var dataNode in context.graph.DataNodes)
            {
                if (dataNode.TaskNode.Task is LegacyExpressionTask expressionTask)
                {
                    foreach (var childDataNode in dataNode.Children)
                    {
                        if (childDataNode.TaskNode.Task is GpuKernelTask or PlaceholderSystemTask or RenderingTask or SpawnerTask)
                        {
                            uint vfxExpressionIndex = AddExpressionRecursively(expressionTask.Expression);
                            m_ValuesExpressionIndices.Add(childDataNode.Id, vfxExpressionIndex);
                        }
                    }
                }
            }
        }

        void GenerateBufferDescriptions(ref CompilationContext context)
        {
            GenerateAttributeBufferDescriptions(ref context);
            GenerateGraphValuesBufferDescriptions(ref context);
            GenerateDeadListBuffersDescription(ref context);
            GenerateSpawnerBuffersDescriptions(ref context);
        }

        void GenerateAttributeBufferDescriptions(ref CompilationContext context)
        {
            AttributeSetLayoutCompilationData attributeSetLayouts = context.data.Get<AttributeSetLayoutCompilationData>();
            foreach (var kvp in attributeSetLayouts)
            {
                AttributeData attributeData = kvp.Key;
                var attributeSetLayout = kvp.Value;
                uint capacity = attributeSetLayout.Capacity;

                var layoutElementDescs = new List<VFXLayoutElementDesc>();
                foreach (var attribute in attributeSetLayout.Attributes)
                {
                    (uint bucketOffset, uint bucketSize, uint elementOffset) = attributeSetLayout.GetBucketLocation(attribute);
                    layoutElementDescs.Add(new VFXLayoutElementDesc()
                    {
                        name = attribute.Name,
                        type = GetVFXValueTypeFromType(attribute.Type),
                        offset = new VFXLayoutOffset()
                        {
                            bucket = bucketOffset,
                            element = elementOffset,
                            structure = bucketSize
                        },
                    });
                }

                VFXGPUBufferDesc bufferDesc = new VFXGPUBufferDesc()
                {
                    target = GraphicsBuffer.Target.Raw,
                    size = attributeSetLayout.GetBufferSize(),
                    stride = 4u,
                    capacity = capacity,
                    mode = ComputeBufferMode.Immutable,
                    layout = layoutElementDescs.ToArray(),
                };
                uint bufferIndex = AddGPUBufferData(bufferDesc);
                m_GpuBufferDescIndices[attributeData] = bufferIndex;
            }
        }

        void GenerateGraphValuesBufferDescriptions(ref CompilationContext context)
        {
            var dataLayoutContainer = context.data.Get<DataLayoutContainer>();

            foreach (var dataContainer in context.graph.DataContainers)
            {
                if (dataLayoutContainer.TryGetLayout(dataContainer.Id, out var valueBufferLayout))
                {
                    uint bufferIndex = AddGPUBufferData(new VFXGPUBufferDesc()
                    {
                        target = GraphicsBuffer.Target.Raw,
                        size = valueBufferLayout.GetBufferSize(),
                        stride = 4u,
                        mode = ComputeBufferMode.Dynamic,
                    });

                    m_GpuBufferDescIndices[dataContainer.RootDataView.DataDescription] = bufferIndex;
                }
            }
        }

        void GenerateDeadListBuffersDescription(ref CompilationContext context)
        {
            foreach (var dataView in context.graph.DataViews)
            {
                if (dataView.DataDescription is DeadListData deadListData)
                {
                    Debug.Assert(dataView.Parent.HasValue);
                    if (dataView.Parent.Value.DataDescription is ParticleData particleData)
                    {
                        uint bufferIndex = AddGPUBufferData(new VFXGPUBufferDesc()
                        {
                            target = GraphicsBuffer.Target.Structured,
                            size = particleData.Capacity + 2,
                            stride = 4u,
                            mode = ComputeBufferMode.Immutable,
                        });

                        m_GpuBufferDescIndices[deadListData] = bufferIndex;
                    }
                }
            }
        }

        void GenerateSpawnerBuffersDescriptions(ref CompilationContext context)
        {
            foreach (var dataView in context.graph.DataViews)
            {
                if (dataView.DataDescription is EventListData eventListData)
                {
                    var bufferDesc = new VFXGPUBufferDesc()
                    {
                        target = GraphicsBuffer.Target.Structured,
                        size = eventListData.BufferSize,
                        stride = 4u
                    };

                    if (eventListData.IsCpu)
                    {
                        bufferDesc.mode = ComputeBufferMode.Dynamic;
                    }

                    uint bufferIndex = AddGPUBufferData(bufferDesc);
                    m_GpuBufferDescIndices[eventListData] = bufferIndex;
                }
            }
        }

        void GenerateSystemDescs(ref CompilationContext context)
        {
            GenerateSpawnerSystemDescs(ref context);

            var particleSystemContainer = context.data.Get<VfxGraphLegacyParticleSystemContainer>();
            foreach (var particleSystem in particleSystemContainer)
            {
                if (GenerateParticleSystemDesc(ref context, particleSystem, out var systemDesc))
                {
                    m_ParticleSystemIndices.Add(particleSystem, m_currentOutput.SystemDescs.Count);
                    m_currentOutput.SystemDescs.Add(systemDesc);
                }
            }
        }

        void GenerateSpawnerSystemDescs(ref CompilationContext context)
        {
            Dictionary<DataViewId, List<VFXEditorTaskDesc>> spawnerTasks = new();

            // Collect tasks for each spawner system
            foreach (var taskNode in context.graph.TaskNodes)
            {
                if (taskNode.Task is SpawnerTask spawnerTask)
                {
                    var eventDataBinding = taskNode.DataBindings[spawnerTask.SpawnDataKey];
                    var eventDataView = eventDataBinding.Value.DataView;
                    if (!spawnerTasks.TryGetValue(eventDataView.Id, out var taskDescs))
                    {
                        taskDescs = new();
                        spawnerTasks.Add(eventDataView.Id, taskDescs);
                    }
                    GenerateSpawnerTask(ref context, spawnerTask, taskNode, out var taskDesc);
                    taskDescs.Add(taskDesc);
                }
            }
            // Create the system desc
            foreach (var (eventDataViewId, taskDescs) in spawnerTasks)
            {
                if (taskDescs.Count > 0)
                {
                    AttributeSetLayoutCompilationData attributeSetLayouts = context.data.Get<AttributeSetLayoutCompilationData>();

                    var eventDataView = context.graph.DataViews[eventDataViewId];
                    eventDataView.FindSubData(EventData.AttributeDataKey, out var attributeDataView);
                    var attributeSetLayout = attributeSetLayouts[attributeDataView.DataDescription as AttributeData];

                    var eventData = eventDataView.DataDescription as EventData;
                    GenerateSpawnerSystemDesc(ref context, eventData, attributeSetLayout, out var systemDesc);
                    systemDesc.tasks = taskDescs.ToArray();
                    uint systemDescIndex = (uint)m_currentOutput.SystemDescs.Count;
                    m_StartSystems.Add(systemDescIndex);
                    m_StopSystems.Add(systemDescIndex);
                    m_currentOutput.SystemDescs.Add(systemDesc);
                }
            }
        }

        bool GenerateSpawnerSystemDesc(ref CompilationContext context, EventData spawnerData, AttributeSetLayout attributeSetLayout, out VFXEditorSystemDesc systemDesc)
        {
            var spawnCountAttribute = VFXAttributesManager.ConvertToNewCompiler(VFXAttribute.SpawnCount);

            List<Unity.GraphCommon.LowLevel.Editor.Attribute> attributes = new((int)attributeSetLayout.Count + 1);
            attributes.Add(spawnCountAttribute);
            foreach (var attribute in attributeSetLayout.Attributes)
            {
                if (attribute != spawnCountAttribute)
                {
                    attributes.Add(attribute);
                }
            }

            var cpuBufferDesc = GenerateAttributeCPUBufferDesc(attributes);
            var spawnerOutputIndex = AddCPUBufferData(cpuBufferDesc);
            m_CpuBufferDescIndices[spawnerData] = spawnerOutputIndex;

            systemDesc = new VFXEditorSystemDesc
            {
                name = "Spawn System",
                type = UnityEngine.VFX.VFXSystemType.Spawner,
                buffers = new[] { new UnityEditor.VFX.VFXMapping("spawner_output", (int)spawnerOutputIndex) },
                tasks = Array.Empty<VFXEditorTaskDesc>(),
                layer = ~0u
            };
            return true;
        }

        private static VFXCPUBufferDesc GenerateAttributeCPUBufferDesc(List<Unity.GraphCommon.LowLevel.Editor.Attribute> attributes)
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
                    case VFXValueType.Boolean:
                        data.PushBool((bool)attribute.DefaultValue);
                        break;
                    case VFXValueType.Float:
                        data.PushFloat((float)attribute.DefaultValue);
                        break;
                    case VFXValueType.Float2:
                        var v2 = (Vector2)attribute.DefaultValue;
                        data.PushFloat(v2.x);
                        data.PushFloat(v2.y);
                        break;
                    case VFXValueType.Float3:
                        var v3 = (Vector3)attribute.DefaultValue;
                        data.PushFloat(v3.x);
                        data.PushFloat(v3.y);
                        data.PushFloat(v3.z);
                        break;
                    case VFXValueType.Float4:
                        var v4 = (Vector4)attribute.DefaultValue;
                        data.PushFloat(v4.x);
                        data.PushFloat(v4.y);
                        data.PushFloat(v4.z);
                        data.PushFloat(v4.w);
                        break;
                    case VFXValueType.Int32:
                        data.PushInt((int)attribute.DefaultValue);
                        break;
                    case VFXValueType.Uint32:
                        data.PushUInt((uint)attribute.DefaultValue);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            var stride = elementOffset;
            for (int i = 0; i < attributes.Count; ++i)
            {
                layout[i].offset.structure = stride;
            }

            return new VFXCPUBufferDesc
            {
                capacity = 1u,
                stride = stride,
                initialData = data,
                layout = layout
            };
        }

        bool GenerateSpawnerTask(ref CompilationContext context, SpawnerTask spawnerTask, TaskNode spawnerTaskNode, out UnityEditor.VFX.VFXEditorTaskDesc taskDesc)
        {
            taskDesc = new();

            taskDesc.shaderSourceIndex = -1;
            taskDesc.type = (UnityEngine.VFX.VFXTaskType)spawnerTask.SpawnerType;

            List<VFXMapping> valueMappings = new();
            var taskNode = spawnerTaskNode;
            foreach (var dataBinding in taskNode.DataBindings)
            {
                if (m_ValuesExpressionIndices.TryGetValue(dataBinding.DataNode.Id, out var expressionIndex))
                {
                    string name = dataBinding.BindingDataKey.ToString();
                    valueMappings.Add(new VFXMapping(name, (int)expressionIndex));
                }
            }

            taskDesc.values = valueMappings.ToArray();
            return true;
        }

        bool GenerateParticleSystemDesc(ref CompilationContext context, VfxGraphLegacyParticleSystemContainer.ParticleSystem particleSystem, out UnityEditor.VFX.VFXEditorSystemDesc systemDesc)
        {
            systemDesc = new();
            systemDesc.name = particleSystem.Name;
            systemDesc.type = UnityEngine.VFX.VFXSystemType.Particle;
            systemDesc.capacity = particleSystem.Capacity;

            var bufferMappings = GenerateParticleSystemBuffersMappings(context, particleSystem);
            systemDesc.buffers = bufferMappings;

            List<UnityEditor.VFX.VFXEditorTaskDesc> taskDescs = new();
            List<UnityEditor.VFX.VFXInstanceSplitDesc> instanceSplitDescs = new();
            foreach (var task in particleSystem.Tasks)
            {
                if (GenerateParticleSystemTask(ref context, task, out var taskDesc))
                {
                    taskDescs.Add(taskDesc);
                }
                instanceSplitDescs.Add(new UnityEditor.VFX.VFXInstanceSplitDesc()
                {
                    values = Array.Empty<uint>(),
                });


                if (task.TaskType == UnityEngine.VFX.VFXTaskType.Initialize)
                {
                    var initTaskNode = context.graph.TaskNodes[task.Id];
                    foreach (var dataNode in initTaskNode.DataNodes)
                    {
                        foreach (var dataView in dataNode.UsedDataViews)
                        {
                            if (dataView.DataDescription is DeadListData)
                            {
                                systemDesc.flags |= VFXSystemFlag.SystemHasKill;
                            }
                            if (dataView.DataDescription is EventListData eventListData)
                            {
                                if (!eventListData.IsCpu)
                                {
                                    systemDesc.flags |= VFXSystemFlag.SystemReceivedEventGPU;
                                }
                            }
                        }
                    }
                }
            }
            systemDesc.values = GenerateParticleSystemValuesMappings(context, particleSystem, out var layer);
            systemDesc.tasks = taskDescs.ToArray();
            systemDesc.instanceSplitDescs = instanceSplitDescs.ToArray();
            systemDesc.layer = layer;
            return true;
        }

        VFXMapping[] GenerateParticleSystemValuesMappings(CompilationContext context, VfxGraphLegacyParticleSystemContainer.ParticleSystem particleSystem, out uint layer)
        {
            var valueMappings = new List<VFXMapping>();
            var graphValueMappings = new List<(int, VFXMapping)>();
            var taskNode = context.graph.TaskNodes[particleSystem.SystemTask.Id];

            var dataLayoutContainer = context.data.Get<DataLayoutContainer>();
            layer = 0;

            DataContainerId graphValuesContainerId = DataContainerId.Invalid;
            // Find graph values buffer in bindings
            foreach (var dataBinding in taskNode.DataBindings)
            {
                if (dataBinding.BindingDataKey == TemplatedTask.GraphValuesBufferKey)
                {
                    graphValuesContainerId = dataBinding.DataView.DataContainer.Id;
                    break;
                }
            }
            dataLayoutContainer.TryGetLayout(graphValuesContainerId, out var graphValuesBufferLayout);

            foreach (var dataBinding in taskNode.DataBindings)
            {
                if (m_ValuesExpressionIndices.TryGetValue(dataBinding.DataNode.Id, out var index))
                {
                    var name = dataBinding.BindingDataKey.ToString();
                    // "System values"
                    if(name is "bounds_center" or "bounds_size" or "boundsPadding")
                    {
                        valueMappings.Add(new VFXMapping(name, (int)index));
                        continue;
                    }

                    // Graph values
                    int graphValueOffset = graphValuesBufferLayout.GetValueOffset(dataBinding.DataView.DataDescription as ValueData);
                    graphValueMappings.Add((graphValueOffset, new VFXMapping(name, (int)index)));
                }
            }

            if (particleSystem.Parent != null)
            {
                int parentSystemIndex = m_ParticleSystemIndices[particleSystem.Parent];
                valueMappings.Add(new VFXMapping("parentSystemIndex", parentSystemIndex));
                layer = m_currentOutput.SystemDescs[parentSystemIndex].layer + 1;
            }

            valueMappings.Add(new VFXMapping("graphValuesOffset", valueMappings.Count + 1));

            //Need to add the graph value mapping in the order of graph value layout for the runtime to work correctly
            graphValueMappings.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            foreach ( (int _, VFXMapping mapping) in graphValueMappings)
            {
                valueMappings.Add(mapping);
            }
            return valueMappings.ToArray();

        }
        VFXMapping[] GenerateParticleSystemBuffersMappings(CompilationContext context, VfxGraphLegacyParticleSystemContainer.ParticleSystem particleSystem)
        {
            HashSet<VFXMapping> bufferMappings = new();
            foreach (var task in particleSystem.Tasks)
            {
                var taskNode = context.graph.TaskNodes[task.Id];
                foreach (var dataBinding in taskNode.DataBindings)
                {
                    foreach (var dataView in dataBinding.DataNode.UsedDataViews)
                    {
                        if (m_GpuBufferDescIndices.TryGetValue(dataView.DataDescription, out var gpuIndex))
                        {
                            //TODO: Get the mapping name from the data view or data binding or something
                            if(dataView.DataDescription is AttributeData)
                            {
                                if(dataBinding.BindingDataKey.ToString().Equals("EventListDataBinding"))
                                    bufferMappings.Add(new VFXMapping("sourceAttributeBuffer", (int)gpuIndex));
                                else if (dataBinding.BindingDataKey.ToString().Equals("ParticleDataBinding"))
                                    bufferMappings.Add(new VFXMapping("attributeBuffer", (int)gpuIndex));
                            }
                            else if(dataView.DataDescription is StructuredData)
                            {
                                bufferMappings.Add(new VFXMapping("graphValuesBuffer", (int)gpuIndex));
                            }
                            else if (dataView.DataDescription is DeadListData)
                            {
                                bufferMappings.Add(new VFXMapping("deadList", (int)gpuIndex));
                            }
                            else if (dataView.DataDescription is EventListData eventListData)
                            {
                                if (eventListData.IsCpu)
                                {
                                    bufferMappings.Add(new VFXMapping("spawnBuffer", (int)gpuIndex));
                                }
                                else
                                {
                                    if (dataBinding.Usage == BindingUsage.Read)
                                    {
                                        bufferMappings.Add(new VFXMapping("eventList", (int)gpuIndex));
                                    }
                                    else
                                    {
                                        // When writing to a gpu event buffer, the runtime looks for it on every system.
                                        // We should probably change that and do it here
                                    }
                                }
                            }
                        }
                    }
                }
            }
            var systemTaskNode = context.graph.TaskNodes[particleSystem.SystemTask.Id];
            foreach (var dataBinding in systemTaskNode.DataBindings)
            {
                if (m_CpuBufferDescIndices.TryGetValue(dataBinding.DataView.DataDescription, out var cpuIndex))
                {
                    bufferMappings.Add(new VFXMapping("spawner_input", (int)cpuIndex));
                }
            }

            return HashSetToArray(bufferMappings);
        }

        bool GenerateParticleSystemTask(ref CompilationContext context, VfxGraphLegacyParticleSystemContainer.Task task, out UnityEditor.VFX.VFXEditorTaskDesc taskDesc)
        {
            taskDesc = new();

            var generatedCodeContainer = context.data.Get<GeneratedCodeContainer>();
            string sourceCode = generatedCodeContainer.Find(task.Id);
            bool isCompute = !task.TaskType.HasFlag(UnityEngine.VFX.VFXTaskType.Output);
            taskDesc.shaderSourceIndex = (int)AddShaderSourceDesc(task.Name, sourceCode, isCompute);
            taskDesc.type = task.TaskType;

            HashSet<VFXMapping> bufferMappings = new();
            List<VFXMapping> valueMappings = new();
            var taskNode = context.graph.TaskNodes[task.Id];
            foreach (var dataBinding in taskNode.DataBindings)
            {
                foreach (var dataView in dataBinding.DataNode.UsedDataViews)
                {
                    // TODO: VFXMapping name should be linked to what is done in the description writers
                    if (m_GpuBufferDescIndices.TryGetValue(dataView.DataDescription, out var gpuIndex))
                    {
                        if(dataView.DataDescription is AttributeData)
                        {
                            bufferMappings.Add(new VFXMapping($"_{dataView.DataContainer.IdentifierName}_attributeBuffer", (int)gpuIndex));
                        }
                        else if (dataView.DataDescription is DeadListData)
                        {
                            bufferMappings.Add(new VFXMapping($"_{dataView.DataContainer.IdentifierName}_deadListBuffer", (int)gpuIndex));
                        }
                        else if(dataView.Root.DataDescription is StructuredData)
                        {
                            bufferMappings.Add(new VFXMapping($"_{dataView.DataContainer.IdentifierName}_buffer", (int)gpuIndex));
                        }
                        else if (dataView.DataDescription is EventListData eventListData)
                        {
                            bufferMappings.Add(new VFXMapping($"_{dataView.DataContainer.IdentifierName}_eventIndexList", (int)gpuIndex));

                            // For now, artificially add an eventListOut mapping to be read by the runtime
                            if (dataBinding.Usage == BindingUsage.Write)
                            {
                                bufferMappings.Add(new VFXMapping($"eventListOut_{dataView.DataContainer.IdentifierName}", (int)gpuIndex));
                            }
                        }
                    }
                }
                if (m_ValuesExpressionIndices.TryGetValue(dataBinding.DataNode.Id, out var expressionIndex))
                {
                    // For textures/buffers for now we need to use the name of the data container to match with what is generated in the description writer, we should find a better way to link them together
                    string name = dataBinding.DataNode.DataContainer.Name;
                    valueMappings.Add(new VFXMapping(name, (int)expressionIndex));
                }
            }
            if (taskNode.Task is GpuKernelTask gpuKernelTask)
            {
                //taskDesc.processor = gpuKernelTask.Shader;
            }
            else if (taskNode.Task is RenderingTask renderingTask)
            {
                //taskDesc.processor = renderingTask.Material;
            }

            taskDesc.values = valueMappings.ToArray();
            taskDesc.buffers = HashSetToArray(bufferMappings);

            return true;
        }

        VFXExpressionValueContainerDesc CreateValueContainerDesc(VFXExpression exp, uint expressionIndex)
        {
            VFXExpressionValueContainerDesc value;
            switch (exp.valueType)
            {
                case VFXValueType.Float: value = CreateValueDesc<float>(exp, (int)expressionIndex); break;
                case VFXValueType.Float2: value = CreateValueDesc<Vector2>(exp, (int)expressionIndex); break;
                case VFXValueType.Float3: value = CreateValueDesc<Vector3>(exp, (int)expressionIndex); break;
                case VFXValueType.Float4: value = CreateValueDesc<Vector4>(exp, (int)expressionIndex); break;
                case VFXValueType.Int32: value = CreateValueDesc<int>(exp, (int)expressionIndex); break;
                case VFXValueType.Uint32: value = CreateValueDesc<uint>(exp, (int)expressionIndex); break;
                case VFXValueType.Texture2D:
                case VFXValueType.Texture2DArray:
                case VFXValueType.Texture3D:
                case VFXValueType.TextureCube:
                case VFXValueType.TextureCubeArray:
                    value = CreateObjectValueDesc<Texture>(exp, (int)expressionIndex);
                    break;
                case VFXValueType.CameraBuffer: value = CreateObjectValueDesc<Texture>(exp, (int)expressionIndex); break;
                case VFXValueType.Matrix4x4: value = CreateValueDesc<Matrix4x4>(exp, (int)expressionIndex); break;
                case VFXValueType.Curve: value = CreateValueDesc<AnimationCurve>(exp, (int)expressionIndex); break;
                case VFXValueType.ColorGradient: value = CreateValueDesc<Gradient>(exp, (int)expressionIndex); break;
                case VFXValueType.Mesh: value = CreateObjectValueDesc<Mesh>(exp, (int)expressionIndex); break;
                case VFXValueType.SkinnedMeshRenderer: value = CreateObjectValueDesc<SkinnedMeshRenderer>(exp, (int)expressionIndex); break;
                case VFXValueType.Boolean: value = CreateValueDesc<bool>(exp, (int)expressionIndex); break;
                case VFXValueType.Buffer: value = CreateValueDesc<GraphicsBuffer>(exp, (int)expressionIndex); break;
                default: throw new InvalidOperationException("Invalid type : " + exp.valueType);
            }

            return value;
        }

        private static VFXExpressionValueContainerDesc<T> CreateValueDesc<T>(VFXExpression exp, int expressionIndex)
        {
            var desc = new VFXExpressionValueContainerDesc<T>();
            desc.value = exp.Get<T>();
            desc.expressionIndex = (uint)expressionIndex;
            return desc;
        }
        private static VFXExpressionObjectValueContainerDesc<T> CreateObjectValueDesc<T>(VFXExpression exp, int expressionIndex)
        {
            var desc = new VFXExpressionObjectValueContainerDesc<T>();
            desc.entityId = exp.Get<EntityId>();
            desc.expressionIndex = (uint)expressionIndex;
            return desc;
        }

        unsafe uint AddExpression(VFXExpressionOperation op, int data0, int data1, int data2, int data3)
        {
            UnityEditor.VFX.VFXExpressionDesc vfxExpression = new() { op = op };
            vfxExpression.data[0] = data0;
            vfxExpression.data[1] = data1;
            vfxExpression.data[2] = data2;
            vfxExpression.data[3] = data3;
            var vfxExpressionIndex = (uint)m_currentOutput.SheetExpressions.Count;
            m_currentOutput.SheetExpressions.Add(vfxExpression);
            return vfxExpressionIndex;
        }

        uint AddCPUBufferData(UnityEditor.VFX.VFXCPUBufferDesc data)
        {
            uint bufferDataIndex = (uint)m_currentOutput.CpuBufferDescs.Count;
            m_currentOutput.CpuBufferDescs.Add(data);
            return bufferDataIndex;
        }

        uint AddGPUBufferData(UnityEditor.VFX.VFXGPUBufferDesc data)
        {
            uint bufferDataIndex = (uint)m_currentOutput.GpuBufferDescs.Count;
            m_currentOutput.GpuBufferDescs.Add(data);
            return bufferDataIndex;
        }

        uint AddShaderSourceDesc(string name, string sourceCode, bool isCompute)
        {
            uint shaderSourceIndex = (uint)m_currentOutput.ShaderSourceDescs.Count;

            UnityEditor.VFX.VFXShaderSourceDesc shaderSourceDesc = new();
            shaderSourceDesc.name = name;
            shaderSourceDesc.source = sourceCode;
            shaderSourceDesc.compute = isCompute;

            m_currentOutput.ShaderSourceDescs.Add(shaderSourceDesc);
            return shaderSourceIndex;
        }

        void Cleanup()
        {
            m_GpuBufferDescIndices.Clear();
            m_CpuBufferDescIndices.Clear();
            m_ValuesExpressionIndices.Clear();
            m_ParticleSystemIndices.Clear();
            m_StartSystems.Clear();
            m_StopSystems.Clear();
            m_currentOutput = null;
        }

        T[] HashSetToArray<T>(HashSet<T> hashSet)
        {
            T[] array = new T[hashSet.Count];
            int index = 0;
            foreach (T value in hashSet)
            {
                array[index++] = value;
            }
            return array;
        }
    }
}
