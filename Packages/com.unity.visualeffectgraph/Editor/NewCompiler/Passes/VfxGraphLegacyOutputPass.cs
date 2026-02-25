using System;
using System.Collections.Generic;
using Unity.GraphCommon.LowLevel;
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
            vfxAssetDesc.version = Version;

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

        delegate uint OnAddExpression(VfxGraphLegacyOutputPass pass, IExpression expression, VFXExpressionOperation op, List<uint> parentExpressionIndices);

        struct ExpressionHandler
        {
            VFXExpressionOperation operation;
            OnAddExpression onAddExpression;
            public ExpressionHandler(VFXExpressionOperation operation, OnAddExpression onAddExpression)
            {
                this.operation = operation;
                this.onAddExpression = onAddExpression;
            }

            public uint AddExpression(VfxGraphLegacyOutputPass pass, IExpression expression, List<uint> parentExpressionIndices)
            {
                return onAddExpression(pass, expression, operation, parentExpressionIndices);
            }
        }
        static uint AddOperationExpression(VfxGraphLegacyOutputPass pass, IExpression expression, VFXExpressionOperation op, List<uint> parentExpressionIndices)
        {
            Type resultType = expression.ResultType;
            var valueType = GetVFXValueTypeFromType(resultType);
            int[] data = { -1, -1, -1 };
            for(int i = 0; i < parentExpressionIndices.Count; i++)
                data[i] = (int)parentExpressionIndices[ i];
            return pass.AddExpression(op, data[0], data[1], data[2], (int)valueType);
        }

        static uint AddBuiltinExpression(VfxGraphLegacyOutputPass pass, IExpression expression, VFXExpressionOperation op, List<uint> parentExpressionIndices)
        {
            int[] data = { -1, -1, -1, -1 };
            for(int i = 0; i < parentExpressionIndices.Count; i++)
                data[i] = (int)parentExpressionIndices[ i];
            return  pass.AddExpression(op, data[0], data[1], data[2], data[3]);
        }

        static uint AddValueExpression(VfxGraphLegacyOutputPass pass, IExpression expression, VFXExpressionOperation op, List<uint> parentExpressionIndices)
        {
            return pass.AddValueExpression((IValueExpression)expression);
        }

        ExpressionHandler _valueExpressionHandler = new(VFXExpressionOperation.Value, AddValueExpression);

        static readonly Dictionary<Type, ExpressionHandler> s_PredefinedExpressions = new()
        {
            { typeof(DeltaTimeExpression), new ExpressionHandler(VFXExpressionOperation.DeltaTime, AddBuiltinExpression)},
            { typeof(TotalTimeExpression), new ExpressionHandler(VFXExpressionOperation.TotalTime, AddBuiltinExpression)},
            { typeof(CosineExpression<float>), new ExpressionHandler(VFXExpressionOperation.Cos, AddOperationExpression)},
            { typeof(AddExpression<float>), new ExpressionHandler(VFXExpressionOperation.Add, AddOperationExpression)},
        };

        readonly Dictionary<IDataDescription, uint> m_GpuBufferDescIndices = new();
        readonly Dictionary<IDataDescription, uint> m_CpuBufferDescIndices = new();
        readonly Dictionary<DataNodeId, uint> m_ValuesExpressionIndices = new();

        static UnityEngine.VFX.VFXValueType GetVFXValueTypeFromType(System.Type type) => s_ValueTypeConversion.TryGetValue(type, out var valueType) ? valueType : UnityEngine.VFX.VFXValueType.None;

        public VfxGraphLegacyCompilationOutput Execute(ref CompilationContext context)
        {
            VfxGraphLegacyCompilationOutput output = new();

            m_currentOutput = output;
            m_currentOutput.Version = 7;

            AddDataContainerSources(ref context);

            GenerateExpressionSheet(ref context);
            GenerateBufferDescriptions(ref context);
            GenerateSystemDescs(ref context);

            output.EventDescs.Add(new() { name = UnityEngine.VFX.VisualEffectAsset.PlayEventName, startSystems = new[] { 0u }, stopSystems = Array.Empty<uint>(), initSystems = Array.Empty<uint>() });
            output.EventDescs.Add(new() { name = UnityEngine.VFX.VisualEffectAsset.StopEventName, startSystems = Array.Empty<uint>(), stopSystems = new[] { 0u }, initSystems = Array.Empty<uint>() });

            foreach (var buffer in output.CpuBufferDescs)
            {
                //Debug.Log("Buffer " + buffer.capacity);
            }

            Cleanup();

            return output;
        }

        IValueExpression EvaluateExpressionRecursively(IExpression expression)
        {
            List<IValueExpression> parentExpressionValues = new();
            foreach (var parentExpression in expression.Parents)
            {
                var parentExpressionValue = EvaluateExpressionRecursively(parentExpression);
                parentExpressionValues.Add(parentExpressionValue);
            }
            return expression.Evaluate(parentExpressionValues);
        }

        uint AddExpressionRecursively(IExpression expression)
        {
            List<uint> parentExpressionIndices = new();
            foreach (var parentExpression in expression.Parents)
            {
                var parentExpressionValue = AddExpressionRecursively(parentExpression);
                parentExpressionIndices.Add(parentExpressionValue);
            }

            uint vfxExpressionIndex;
            if (s_PredefinedExpressions.TryGetValue(expression.GetType(), out var expressionAdder))
            {
                vfxExpressionIndex = expressionAdder.AddExpression(this, expression, parentExpressionIndices);
            }
            else if (expression is IValueExpression valueExpression)
            {
                vfxExpressionIndex = _valueExpressionHandler.AddExpression(this, valueExpression, parentExpressionIndices);
            }
            else
            {
                throw new NotImplementedException($"Expression of type {expression.GetType()} is not supported");
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
                if (dataNode.TaskNode.Task is ExpressionTask expressionTask)
                {
                    foreach (var childDataNode in dataNode.Children)
                    {
                        if (childDataNode.TaskNode.Task is GpuKernelTask or PlaceholderSystemTask or RenderingTask)
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
            GenerateSpawnBuffersDescriptions(ref context);
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
            var structuredDataLayoutContainer = context.data.Get<StructuredDataLayoutContainer>();

            foreach (var dataView in context.graph.DataViews)
            {
                if (dataView.DataDescription is StructuredData structuredData)
                {
                    if (structuredDataLayoutContainer.TryGetLayout(structuredData, out var valueBufferLayout))
                    {
                        uint bufferIndex = AddGPUBufferData(new VFXGPUBufferDesc()
                        {
                            target = GraphicsBuffer.Target.Raw,
                            size = valueBufferLayout.GetBufferSize(),
                            stride = 4u,
                            mode = ComputeBufferMode.Dynamic,
                        });

                        m_GpuBufferDescIndices[structuredData] = bufferIndex;
                    }
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
                            mode = ComputeBufferMode.Dynamic,
                        });

                        m_GpuBufferDescIndices[deadListData] = bufferIndex;
                    }
                }
            }
        }

        void GenerateSpawnBuffersDescriptions(ref CompilationContext context)
        {
            foreach (var dataView in context.graph.DataViews)
            {
                if (dataView.DataDescription is SpawnData spawnData)
                {
                    uint bufferIndex = AddGPUBufferData(new VFXGPUBufferDesc()
                    {
                        target = GraphicsBuffer.Target.Structured,
                        size = 2,
                        stride = 4u,
                        mode = ComputeBufferMode.Dynamic,
                    });
                    m_GpuBufferDescIndices[spawnData] = bufferIndex;
                }
            }
        }

        void GenerateSystemDescs(ref CompilationContext context)
        {
            foreach (var taskNode in context.graph.TaskNodes)
            {
                if (taskNode.Task is SpawnerTask spawnerTask && GenerateSpawnerSystemDesc(ref context, taskNode, spawnerTask, out var systemDesc))
                {
                    m_currentOutput.SystemDescs.Add(systemDesc);
                }
            }

            var particleSystemContainer = context.data.Get<VfxGraphLegacyParticleSystemContainer>();
            foreach (var particleSystem in particleSystemContainer)
            {
                if (GenerateParticleSystemDesc(ref context, particleSystem, out var systemDesc))
                {
                    m_currentOutput.SystemDescs.Add(systemDesc);
                }
            }
        }

        bool GenerateSpawnerSystemDesc(ref CompilationContext context, TaskNode taskNode, SpawnerTask task, out UnityEditor.VFX.VFXEditorSystemDesc systemDesc)
        {
            systemDesc = new();

            SpawnData spawnData = null;
            foreach (var dataNode in taskNode.DataNodes)
            {
                foreach (var dataView in dataNode.UsedDataViews)
                {
                    if (dataView.DataDescription is SpawnData spawnDataDescription)
                    {
                        spawnData = spawnDataDescription;
                    }
                }
            }

            var cpuData = new UnityEditor.VFX.VFXCPUBufferData();
            cpuData.PushFloat(1.0f);
            var spawnerOutputIndex = AddCPUBufferData(new()
            {
                capacity = 1u,
                stride = 1u,
                initialData = cpuData,
                layout = new[]
                    {
                        new UnityEditor.VFX.VFXLayoutElementDesc()
                        {
                            name = VFXAttribute.SpawnCount.name,
                            offset = new () { bucket = 0u, element = 0u, structure = 1u},
                            type = UnityEngine.VFX.VFXValueType.Float
                        }
                    }
            });
            m_CpuBufferDescIndices[spawnData] = spawnerOutputIndex;

            systemDesc.name = task.TemplateName;
            systemDesc.type = UnityEngine.VFX.VFXSystemType.Spawner;

            List<UnityEditor.VFX.VFXEditorTaskDesc> tasks = new();
            foreach (var block in task.Blocks)
            {
                if (GenerateSpawnerTask(ref context, block, out var taskDesc))
                {
                    tasks.Add(taskDesc);
                }
            }

            systemDesc.tasks = tasks.ToArray();
            systemDesc.type = UnityEngine.VFX.VFXSystemType.Spawner;
            systemDesc.buffers = new[] { new UnityEditor.VFX.VFXMapping("spawner_output", (int)spawnerOutputIndex) };
            return true;
        }

        bool GenerateSpawnerTask(ref CompilationContext context, SubtaskDescription block, out UnityEditor.VFX.VFXEditorTaskDesc taskDesc)
        {
            taskDesc = new();

            if (block.Name == "ConstantRate") // TODO: remove this hack
            {
                var rateExpression = block.Expressions[0].Item2.Evaluate(new ReadOnlyList<IValueExpression>());
                var enabledExpression = block.Expressions[1].Item2.Evaluate(new ReadOnlyList<IValueExpression>());
                if (rateExpression != null && enabledExpression != null)
                {
                    var vfxEnableIndex = AddValueExpression(enabledExpression);
                    var rateExpressionIndex = AddValueExpression(rateExpression);
                    taskDesc = new()
                    {
                        type = UnityEngine.VFX.VFXTaskType.ConstantRateSpawner,
                        values = new UnityEditor.VFX.VFXMapping[]
                        {
                            new() { name = "_vfx_enabled", index = (int)vfxEnableIndex },
                            new() { name = "Rate", index = (int)rateExpressionIndex }
                        },
                        shaderSourceIndex = -1,
                    };
                    return true;
                }
            }

            return false;
        }

        bool GenerateParticleSystemDesc(ref CompilationContext context, VfxGraphLegacyParticleSystemContainer.ParticleSystem particleSystem, out UnityEditor.VFX.VFXEditorSystemDesc systemDesc)
        {
            systemDesc = new();
            systemDesc.type = UnityEngine.VFX.VFXSystemType.Particle;
            systemDesc.capacity = particleSystem.Capacity;

            var bufferMappings = GenerateSystemBuffersMappings(context, particleSystem);
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
            }
            systemDesc.values = GenerateSystemValuesMappings(context, particleSystem);
            systemDesc.tasks = taskDescs.ToArray();
            systemDesc.instanceSplitDescs = instanceSplitDescs.ToArray();

            foreach(var dataView in context.graph.DataViews)
            {
                if (dataView.DataDescription is DeadListData)
                {
                    systemDesc.flags |= VFXSystemFlag.SystemHasKill;
                    break;
                }

            }


            return true;
        }

        VFXMapping[] GenerateSystemValuesMappings(CompilationContext context, VfxGraphLegacyParticleSystemContainer.ParticleSystem particleSystem)
        {
            var valueMappings = new List<VFXMapping>();
            var graphValueMappings = new Dictionary<ValueData, VFXMapping>();
            var taskNode = context.graph.TaskNodes[particleSystem.SystemTask.Id];

            ValueBufferLayout graphValueBufferLayout = new ValueBufferLayout();

            foreach (var dataBinding in taskNode.DataBindings)
            {
                if (m_ValuesExpressionIndices.TryGetValue(dataBinding.DataNode.Id, out var index))
                {
                    var name = dataBinding.BindingDataKey.ToString();
                    // Dirty approach for renaming some specific values to match the expected names in the VFX system
                    switch (name)
                    {
                        case "BoundsCenter":
                            name = "bounds_center";
                            valueMappings.Add(new VFXMapping(name, (int)index));
                            continue;
                        case "BoundsSize":
                            name = "bounds_size";
                            valueMappings.Add(new VFXMapping(name, (int)index));
                            continue;
                        case "BoundsPadding":
                            name = "boundsPadding";
                            valueMappings.Add(new VFXMapping(name, (int)index));
                            continue;
                    }

                    graphValueMappings.Add(dataBinding.DataView.DataDescription as ValueData, new VFXMapping(name, (int)index));
                    graphValueBufferLayout.AddValueData(dataBinding.DataView.DataDescription as ValueData);
                }
            }
            List<VFXMapping> mappings = new();
            foreach (var mapping in valueMappings)
            {
                mappings.Add(mapping);
            }
            mappings.Add(new VFXMapping("graphValuesOffset", valueMappings.Count + 1));

            //Need to add the graph value mapping in the order of graph value layout for the runtime to work correctly
            graphValueBufferLayout.ComputeOffsets();
            graphValueBufferLayout.GetSortedValueDatas();
            foreach (var valueData in graphValueBufferLayout.GetSortedValueDatas())
            {
                mappings.Add(graphValueMappings[valueData]);
            }
            return mappings.ToArray();
        }
        VFXMapping[] GenerateSystemBuffersMappings(CompilationContext context, VfxGraphLegacyParticleSystemContainer.ParticleSystem particleSystem)
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
                                if(dataBinding.BindingDataKey.ToString().Equals("SpawnDataBinding"))
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
                            else if (dataView.DataDescription is SpawnData)
                            {
                                bufferMappings.Add(new VFXMapping("instancingPrefixSum", (int)gpuIndex));
                            }
                        }

                        if (m_CpuBufferDescIndices.TryGetValue(dataView.DataDescription, out var cpuIndex))
                        {
                            bufferMappings.Add(new VFXMapping("spawner_input", (int)cpuIndex));
                        }
                    }
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
                            bufferMappings.Add(new VFXMapping($"_{dataView.DataContainer.Name}_attributeBuffer", (int)gpuIndex));
                        }
                        else if (dataView.DataDescription is DeadListData)
                        {
                            bufferMappings.Add(new VFXMapping($"_{dataView.DataContainer.Name}_deadListBuffer", (int)gpuIndex));
                        }
                        else if(dataView.Root.DataDescription is StructuredData)
                        {
                            bufferMappings.Add(new VFXMapping($"_{dataView.DataContainer.Name}_buffer", (int)gpuIndex));
                        }
                        else if (dataView.DataDescription is SpawnData)
                        {
                            bufferMappings.Add(new VFXMapping($"_{dataView.DataContainer.Name}_instancingPrefixSum", (int)gpuIndex));
                        }
                    }

                    if (m_ValuesExpressionIndices.TryGetValue(dataBinding.DataNode.Id, out var expressionIndex))
                    {
                        string name = dataBinding.BindingDataKey.ToString();
                        valueMappings.Add(new VFXMapping( name, (int)expressionIndex));
                    }
                }
            }
            if (taskNode.Task is GpuKernelTask gpuKernelTask)
            {
                taskDesc.processor = gpuKernelTask.Shader;
            }
            else if (taskNode.Task is RenderingTask renderingTask)
            {
                taskDesc.processor = renderingTask.Material;
            }

            taskDesc.values = valueMappings.ToArray();
            taskDesc.buffers = HashSetToArray(bufferMappings);

            return true;
        }

        VFXExpressionValueContainerDesc CreateValueContainerDesc(IValueExpression exp, uint expressionIndex)
        {
            Type resultType = exp.ResultType;

            if(resultType == typeof(bool) && exp.TryGetValue<bool>(out var boolVal))
                return new VFXExpressionValueContainerDesc<bool>() { expressionIndex = expressionIndex, value = boolVal };
            if (resultType == typeof(float) && exp.TryGetValue<float>(out var floatVal))
                return new VFXExpressionValueContainerDesc<float>() { expressionIndex = expressionIndex, value = floatVal };
            if (resultType == typeof(Vector2) && exp.TryGetValue<Vector2>(out var vector2Val))
                return new VFXExpressionValueContainerDesc<Vector2>() { expressionIndex = expressionIndex, value = vector2Val };
            if (resultType == typeof(Vector3) && exp.TryGetValue<Vector3>(out var vector3Val))
                return new VFXExpressionValueContainerDesc<Vector3>() { expressionIndex = expressionIndex, value = vector3Val };
            if (resultType == typeof(Vector4) && exp.TryGetValue<Vector4>(out var vector4Val))
                return new VFXExpressionValueContainerDesc<Vector4>() { expressionIndex = expressionIndex, value = vector4Val };
            if (resultType == typeof(int) && exp.TryGetValue<int>(out var intVal))
                return new VFXExpressionValueContainerDesc<int>() { expressionIndex = expressionIndex, value = intVal };
            if (resultType == typeof(uint) && exp.TryGetValue<uint>(out var uintVal))
                return new VFXExpressionValueContainerDesc<uint>() { expressionIndex = expressionIndex, value = uintVal };
            if (resultType == typeof(Color) && exp.TryGetValue<Color>(out var colorVal))
                return new VFXExpressionValueContainerDesc<Vector4>() { expressionIndex = expressionIndex, value = (Vector4)colorVal };
            if (resultType == typeof(Texture) && exp.TryGetValue<Texture>(out var textureVal))
                return new VFXExpressionObjectValueContainerDesc<Texture>() { expressionIndex = expressionIndex, entityId = textureVal ? textureVal.GetEntityId() : EntityId.None};
            if (resultType == typeof(Texture2D) && (exp.TryGetValue<Texture>(out var texture2DVal) || true))
                return new VFXExpressionObjectValueContainerDesc<Texture>() { expressionIndex = expressionIndex, entityId = texture2DVal ? texture2DVal.GetEntityId() : EntityId.None };


            throw new NotSupportedException($"Unsupported result type {resultType}");
        }

        uint AddExpression(VFXExpressionOperation op, int data0, int data1, int data2, int data3)
        {
            UnityEditor.VFX.VFXExpressionDesc vfxExpression = new(){ op = op };
            vfxExpression.data = new[] { data0, data1, data2, data3 };
            var vfxExpressionIndex = (uint)m_currentOutput.SheetExpressions.Count;
            m_currentOutput.SheetExpressions.Add(vfxExpression);

            return vfxExpressionIndex;
        }

        uint AddValueExpression(IValueExpression exp)
        {
            Debug.Assert(exp != null);
            Type resultType = exp.ResultType;
            if (!s_ValueTypeConversion.ContainsKey(resultType))
                throw new NotSupportedException($"Unsupported result type {resultType}");

            var valueType = GetVFXValueTypeFromType(resultType);

            uint vfxExpressionIndex = AddExpression(VFXExpressionOperation.Value, -1, -1, -1, (int)valueType);

            m_currentOutput.SheetValues.Add(CreateValueContainerDesc(exp, vfxExpressionIndex));
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
