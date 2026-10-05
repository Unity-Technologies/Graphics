using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;
using UnityEngine;
using static UnityEditor.VFX.VfxTemplateBindings;

namespace UnityEditor.VFX
{
    class ParticleSystemBuilder : SystemBuilder
    {
        class BuildInfo
        {
            public BuildInfo(VFXData data)
            {
                Data = data;
            }

            public BuildInfo Parent { get; set; }
            public VFXData Data { get; }
            public VFXBasicInitialize InitContext { get; set; }
            public List<VFXBasicUpdate> UpdateContexts { get; } = new();
            public List<VFXAbstractParticleOutput> OutputContexts { get; } = new();
            public List<VFXBasicSpawner> InputCpuEvents { get; } = new();
            public List<VFXBasicGPUEvent> InputGpuEvents { get; } = new();
            public Dictionary<VFXBasicUpdate, List<VFXBasicGPUEvent>> OutputGpuEvents { get; } = new();
            public bool Built { get; set; }
        }

        Dictionary<VFXData, BuildInfo> m_ParticleSystems = new();
        Dictionary<IDataDescription, Dictionary<string, uint>> m_GraphValueNameCounts = new();

        readonly SubtaskBuilder m_SubtaskBuilder = new();
        readonly OutputUpdateTaskBuilder m_OutputUpdateTaskBuilder;

        public ParticleSystemBuilder()
        {
            m_OutputUpdateTaskBuilder = new OutputUpdateTaskBuilder(m_SubtaskBuilder);
        }

        public override bool ProcessContext(VFXContext context)
        {
            bool processed = base.ProcessContext(context);
            var data = context.GetData();
            if (data is VFXDataParticle)
            {
                var buildInfo = GetBuildInfo(data);
                switch (context)
                {
                    case VFXBasicInitialize initContext:
                        Debug.Assert(buildInfo.InitContext == null);
                        buildInfo.InitContext = initContext;
                        CollectInputEvents(initContext, buildInfo);
                        break;
                    case VFXBasicUpdate updateContext:
                        buildInfo.UpdateContexts.Add(updateContext);
                        CollectOutputEvents(updateContext, buildInfo);
                        break;
                    case VFXAbstractParticleOutput outputContext:
                        buildInfo.OutputContexts.Add(outputContext);
                        break;
                    default:
                        Debug.Assert(false, $"Unknown context type: {context.GetType().Name}");
                        break;
                }
                processed = true;
            }
            return processed;
        }

        public override void Build()
        {
            foreach (var buildInfo in m_ParticleSystems.Values)
            {
                Build(buildInfo);
            }
        }

        public override void EndBuilding()
        {
            m_ParticleSystems.Clear();
            m_GraphValueNameCounts.Clear();
            base.EndBuilding();
        }

        BuildInfo GetBuildInfo(VFXData data)
        {
            if (!m_ParticleSystems.TryGetValue(data, out var buildInfo))
            {
                buildInfo = new BuildInfo(data);
                m_ParticleSystems.Add(data, buildInfo);
            }
            return buildInfo;
        }

        void Build(BuildInfo buildInfo)
        {
            if (buildInfo.Built)
                return;

            if (buildInfo.Parent != null)
            {
                // Recursively build parent first
                Build(buildInfo.Parent);
            }

            var particleSystemName = SystemNames.GetUniqueSystemName(buildInfo.Data);

            var systemTaskId = TaskGraphBuilder.AddTask(BuildSystemTask(), $"{particleSystemName} Task");

            var particleDataDescription = BuildParticleDataDescription(buildInfo.Data, particleSystemName);
            var particleDataViewId = TaskGraphBuilder.AddData(particleSystemName, particleDataDescription);
            TaskGraphBuilder.BindData(systemTaskId, ParticleSystemTask.ParticleDataBindingKey, particleDataViewId, BindingUsage.Write);

            var graphValuesBufferViewId = BuildGraphValuesBuffer(out var contextDataViewId);
            TaskGraphBuilder.BindData(systemTaskId, ParticleSystemTask.GraphValuesBindingKey, graphValuesBufferViewId, BindingUsage.Write);

            if (buildInfo.InitContext != null)
            {
                var contextTaskId = AddContextTask(buildInfo.InitContext, "Init", VfxTaskStage.Initialize, systemTaskId, particleSystemName, particleDataViewId, contextDataViewId, graphValuesBufferViewId);
                if (GetInputEventList(buildInfo, particleSystemName, systemTaskId, out var eventListDataViewId, out var sourceAttributesViewId))
                {
                    TaskGraphBuilder.BindData(contextTaskId, EventListDataBindingKey, eventListDataViewId);
                    TaskGraphBuilder.BindData(contextTaskId, SourceAttributesBindingKey, sourceAttributesViewId);
                }
            }

            foreach (var updateContext in buildInfo.UpdateContexts)
            {
                var contextTaskId = AddContextTask(updateContext, "Update", VfxTaskStage.Update, systemTaskId, particleSystemName, particleDataViewId, contextDataViewId, graphValuesBufferViewId);

                if (buildInfo.OutputGpuEvents.TryGetValue(updateContext, out var outputGpuEvents))
                {
                    for (int i = 0; i < outputGpuEvents.Count; i++)
                    {
                        var bindingKey = GetOutputGpuEventBindingKey(i);
                        TryGetDataView(outputGpuEvents[i], out var gpuEventListDataViewId);
                        TaskGraphBuilder.BindData(contextTaskId, bindingKey, gpuEventListDataViewId);
                    }
                }
            }

            foreach (var outputContext in buildInfo.OutputContexts)
            {
                var outputTaskId = AddContextTask(outputContext, "Output", VfxTaskStage.Output, systemTaskId, particleSystemName, particleDataViewId, contextDataViewId, graphValuesBufferViewId);

                var renderingData = BuildRenderingDataDescription(buildInfo.Data, outputContext.name);
                var renderingDataId = TaskGraphBuilder.AddData(renderingData.Name, renderingData);

                if (outputContext.HasSorting())
                {
                    TaskGraphBuilder.BindData(systemTaskId, ParticleSystemTask.InputSortingBufferKey, renderingDataId,
                        BindingUsage.Write);

                    var outputUpdateTaskId = AddOutputUpdateTask(outputContext, systemTaskId, particleSystemName,
                        particleDataDescription.Space, particleDataViewId, contextDataViewId, graphValuesBufferViewId);

                    TaskGraphBuilder.BindData(outputUpdateTaskId, RenderingDataBindingKey, renderingDataId);

                    AddSortTask(outputContext, particleSystemName, renderingDataId);
                }
                else if (outputContext.HasIndirectDraw())
                {
                    TaskGraphBuilder.BindData(systemTaskId, ParticleSystemTask.IndirectBufferKey, renderingDataId,
                        BindingUsage.Write);

                    AddFillIndirectTask(outputContext, particleSystemName, particleDataViewId, contextDataViewId,
                        renderingDataId);
                }
                TaskGraphBuilder.BindData(outputTaskId, RenderingDataBindingKey, renderingDataId, BindingUsage.Read);
            }

            RegisterDataView(buildInfo, particleDataViewId);

            buildInfo.Built = true;
        }

        void CollectInputEvents(VFXBasicInitialize initContext, BuildInfo buildInfo)
        {
            foreach (var inputContext in initContext.inputContexts)
            {
                if (inputContext is VFXBasicSpawner spawner)
                    buildInfo.InputCpuEvents.Add(spawner);
                else if (inputContext is VFXBasicGPUEvent gpuEvent)
                {
                    foreach (var slot in gpuEvent.allLinkedInputSlot)
                    {
                        var parentContext = ((VFXModel)slot.owner).GetParent() as VFXContext;
                        var parentData = parentContext?.GetData();
                        if (parentData != null)
                        {
                            buildInfo.InputGpuEvents.Add(gpuEvent);
                            var parent = GetBuildInfo(parentData);
                            Debug.Assert(buildInfo.Parent == null || buildInfo.Parent == parent);
                            buildInfo.Parent = parent;
                            break;
                        }
                    }
                }
            }
        }


        void CollectOutputEvents(VFXBasicUpdate updateContext, BuildInfo buildInfo)
        {
            foreach (var slot in updateContext.allLinkedOutputSlot)
            {
                var gpuEventContext = ((VFXModel)slot.owner).GetFirstOfType<VFXContext>();
                if (gpuEventContext is VFXBasicGPUEvent gpuEvent && gpuEventContext.CanBeCompiled())
                {
                    if (!buildInfo.OutputGpuEvents.TryGetValue(updateContext, out var gpuEvents))
                    {
                        gpuEvents = new List<VFXBasicGPUEvent>();
                        buildInfo.OutputGpuEvents.Add(updateContext, gpuEvents);
                    }
                    gpuEvents.Add(gpuEvent);
                }
            }
        }

        IReadOnlyList<VFXBasicGPUEvent> GetOutputGpuEvents(VFXBasicUpdate updateContext)
        {
            var data = updateContext.GetData();
            if (m_ParticleSystems.TryGetValue(data, out var buildInfo))
            {
                if (buildInfo.OutputGpuEvents.TryGetValue(updateContext, out var gpuEvents))
                    return gpuEvents;
            }
            return System.Array.Empty<VFXBasicGPUEvent>();
        }

        bool GetInputEventList(BuildInfo buildInfo, string particleSystemName, TaskNodeId systemTaskId,
            out DataViewId eventListDataViewId, out DataViewId sourceAttributesViewId)
        {
            return GetCpuEventList(buildInfo, particleSystemName, systemTaskId, out eventListDataViewId, out sourceAttributesViewId)
                || GetGpuEventList(buildInfo, particleSystemName, systemTaskId, out eventListDataViewId, out sourceAttributesViewId);
        }

        bool GetCpuEventList(BuildInfo buildInfo, string particleSystemName, TaskNodeId systemTaskId,
            out DataViewId eventListDataViewId, out DataViewId sourceAttributesViewId)
        {
            eventListDataViewId = DataViewId.Invalid;
            sourceAttributesViewId = DataViewId.Invalid;
            if (buildInfo.InputCpuEvents.Count > 0)
            {
                var eventListDataDescription = new EventListData($"{particleSystemName}_CPUEvents", isCpu: true, (uint)buildInfo.InputCpuEvents.Count);
                eventListDataViewId = TaskGraphBuilder.AddData(eventListDataDescription.Name, eventListDataDescription);
                sourceAttributesViewId = TaskGraphBuilder.GetSubdata(eventListDataViewId, EventData.AttributeDataKey);

                foreach (var spawner in buildInfo.InputCpuEvents)
                {
                    if (TryGetDataView(spawner, out var eventData) && eventData.IsValid)
                    {
                        TaskGraphBuilder.BindData(systemTaskId, new SpawnerDataKey(spawner), eventData, BindingUsage.Read);
                    }
                }
                TaskGraphBuilder.BindData(systemTaskId, ParticleSystemTask.CpuEventListBindingKey, eventListDataViewId, BindingUsage.Write);
                return true;
            }
            return false;
        }

        bool GetGpuEventList(BuildInfo buildInfo, string particleSystemName, TaskNodeId systemTaskId,
            out DataViewId eventListDataViewId, out DataViewId sourceAttributesViewId)
        {
            eventListDataViewId = DataViewId.Invalid;
            sourceAttributesViewId = DataViewId.Invalid;
            if (buildInfo.InputGpuEvents.Count > 0)
            {
                // Only one input GPU event supported
                TryGetDataView(buildInfo.InputGpuEvents[0], out eventListDataViewId);
                TryGetDataView(buildInfo.Parent, out var parentParticleData);
                sourceAttributesViewId = TaskGraphBuilder.GetSubdata(parentParticleData, ParticleData.AttributeDataKey);
                return true;
            }
            return false;
        }

        TaskNodeId AddContextTask(VFXContext context, string templateName, VfxTaskStage stage, TaskNodeId systemTaskId, string particleSystemName,
            DataViewId particleDataViewId, DataViewId contextDataViewId, DataViewId graphValuesBufferViewId)
        {
            // Resolved once and shared: the task bindings, the subtask code and the data bindings below all
            // have to agree on the name each expression is bound under.
            var contextExpressions = new ContextExpressions(context, ExpressionGraph);

            var contextTask = BuildContextTask(context, templateName, stage, contextExpressions);
            var contextTaskId = TaskGraphBuilder.AddTask(contextTask, $"{particleSystemName} {templateName}");
            BindSharedData(context, contextTaskId, systemTaskId, particleDataViewId, contextDataViewId, graphValuesBufferViewId, contextExpressions);
            return contextTaskId;
        }

        TaskNodeId AddOutputUpdateTask(VFXAbstractParticleOutput outputContext, TaskNodeId systemTaskId,
            string particleSystemName, VFXSpace space, DataViewId particleDataViewId,
            DataViewId contextDataViewId, DataViewId graphValuesBufferViewId)
        {
            var task = m_OutputUpdateTaskBuilder.Build(outputContext, space, ExpressionGraph, CompilationMode,
                out var expressions);

            var taskId = TaskGraphBuilder.AddTask(task, $"{particleSystemName} OutputUpdate");
            BindSharedData(outputContext, taskId, systemTaskId, particleDataViewId, contextDataViewId,
                graphValuesBufferViewId, expressions);
            return taskId;
        }

        void AddFillIndirectTask(VFXAbstractParticleOutput outputContext, string particleSystemName,
            DataViewId particleDataViewId, DataViewId contextDataViewId, DataViewId renderingDataId)
        {
            var task = FillIndirectTaskBuilder.Build(outputContext);
            var taskId = TaskGraphBuilder.AddTask(task, $"{particleSystemName} FillIndirect");

            TaskGraphBuilder.BindData(taskId, ParticleDataBindingKey, particleDataViewId);
            TaskGraphBuilder.BindData(taskId, ContextDataKey, contextDataViewId);
            TaskGraphBuilder.BindData(taskId, RenderingDataBindingKey, renderingDataId);
        }

        void AddSortTask(VFXAbstractParticleOutput outputContext, string particleSystemName,
            DataViewId renderingDataId)
        {
            var sortTask = new SortTask(OutputUpdateTaskBuilder.IsPerCamera(outputContext));
            var sortTaskId = TaskGraphBuilder.AddTask(sortTask, $"{particleSystemName} Sort");

            TaskGraphBuilder.BindData(sortTaskId, SortTask.RenderingDataBindingKey, renderingDataId);
        }

        DataViewId BuildGraphValuesBuffer(out DataViewId contextDataViewId)
        {
            var graphValuesBuffer = new StructuredData();
            var graphValuesBufferViewId = TaskGraphBuilder.AddData("GraphValuesBuffer", graphValuesBuffer);

            StructuredData contextData = new StructuredData();
            contextData.AddSubdata(MaxParticleCountKey, ValueData.Create(typeof(uint)));
            contextData.AddSubdata(SystemSeedKey, ValueData.Create(typeof(uint)));
            contextData.AddSubdata(InitSpawnIndexKey, ValueData.Create(typeof(uint)));
            contextData.AddSubdata(new NameDataKey("padding"), ValueData.Create(typeof(uint)));

            graphValuesBuffer.AddSubdata(ContextDataKey, contextData); // Adds a default subdata for ContextData, which is expected from the C++ runtime.

            contextDataViewId = TaskGraphBuilder.GetSubdata(graphValuesBufferViewId, ContextDataKey);

            UnorderedData graphValuesUnordered = new UnorderedData();
            graphValuesBuffer.AddSubdata(GraphValuesKey, graphValuesUnordered);

            m_GraphValueNameCounts.Add(graphValuesUnordered, new Dictionary<string, uint>());

            return graphValuesBufferViewId;
        }

        void BindSharedData(VFXContext context, TaskNodeId contextTaskId, TaskNodeId systemTaskId,
            DataViewId particleDataViewId, DataViewId contextDataViewId, DataViewId graphValuesBufferViewId,
            ContextExpressions contextExpressions)
        {
            TaskGraphBuilder.BindData(contextTaskId, ParticleDataBindingKey, particleDataViewId);
            TaskGraphBuilder.BindData(contextTaskId, ContextDataKey, contextDataViewId);
            BindCpuExpressions(context, contextTaskId, systemTaskId, contextExpressions);
            BindGpuExpressions(contextTaskId, systemTaskId, graphValuesBufferViewId, contextExpressions);
        }

        // The init context's CPU expressions describe the system as a whole, so they belong to the system
        // task; every other context's describe that context's own task.
        static bool BindsCpuExpressionsOnContextTask(VFXContext context) => context is not VFXBasicInitialize;

        void BindCpuExpressions(VFXContext context, TaskNodeId contextTaskId, TaskNodeId systemTaskId,
            ContextExpressions contextExpressions)
        {
            var cpuMapper = contextExpressions.CpuMapper;
            var taskToBind = BindsCpuExpressionsOnContextTask(context) ? contextTaskId : systemTaskId;

            foreach (var expression in cpuMapper.expressions)
            {
                TryGetDataView(expression, out var expressionDataViewId);
                string bindingName = cpuMapper.GetData(expression)[0].fullName;
                TaskGraphBuilder.BindData(taskToBind, new NameDataKey(bindingName), expressionDataViewId, BindingUsage.Read);
            }
        }

        void BindGpuExpressions(TaskNodeId contextTaskId, TaskNodeId systemTask, DataViewId graphValuesBufferViewId,
            ContextExpressions contextExpressions)
        {
            var graphValuesViewId = TaskGraphBuilder.GetSubdata(graphValuesBufferViewId, GraphValuesKey);

            var graphValuesUnordered = TaskGraphBuilder.Graph.DataViews[graphValuesViewId].DataDescription as UnorderedData;
            Debug.Assert(graphValuesUnordered != null);
            Debug.Assert(m_GraphValueNameCounts.ContainsKey(graphValuesUnordered));

            var uniformMapper = contextExpressions.UniformMapper;

            // Resources are bound by value on the context task itself, not through graph values
            foreach (var (bindingKey, expression) in contextExpressions.RequestedResources)
            {
                TryGetDataView(expression, out var resourceDataViewId);
                TaskGraphBuilder.BindData(contextTaskId, bindingKey, resourceDataViewId);
            }

            foreach (var expression in uniformMapper.uniforms)
            {
                if(expression.Is(VFXExpression.Flags.Constant))
                    continue;
                if(expression.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                    continue;
                Debug.Assert(VFXExpression.IsUniform(expression.valueType));

                TryGetDataView(expression, out var expressionDataViewId);
                string bindingName = uniformMapper.GetName(expression);
                IDataKey contextBindingKey = new NameDataKey(bindingName);

                var expressionValue = TaskGraphBuilder.Graph.DataViews[expressionDataViewId].DataDescription;

                bool addToGraphValues = graphValuesUnordered.GetSubDataKey(expressionValue) == null;
                if (addToGraphValues)
                {
                    var nameCountMap = m_GraphValueNameCounts[graphValuesUnordered];
                    if (nameCountMap.TryGetValue(bindingName, out uint count))
                    {
                        nameCountMap[bindingName] = count + 1;
                    }
                    else
                    {
                        count = 0;
                        nameCountMap.Add(bindingName, 1);
                    }
                    string systemUniqueBindingName = $"{bindingName}_{VFXCodeGeneratorHelper.GeneratePrefix(count)}";
                    IDataKey systemBindingKey = new NameDataKey(systemUniqueBindingName);
                    Debug.Assert(graphValuesUnordered.GetSubdata(systemBindingKey) == null, $"Graph value with name {systemUniqueBindingName} already exists.");
                    graphValuesUnordered.AddSubdata(systemBindingKey, expressionValue);
                    TaskGraphBuilder.BindData(systemTask, systemBindingKey, expressionDataViewId, BindingUsage.Read);
                }

                var subdataViewId = TaskGraphBuilder.GetSubdata(graphValuesViewId, graphValuesUnordered.GetSubDataKey(expressionValue));
                TaskGraphBuilder.BindData(contextTaskId, contextBindingKey, subdataViewId, BindingUsage.Read);
            }
        }

        ITask BuildSystemTask()
        {
            return new ParticleSystemTask();
        }

        ParticleData BuildParticleDataDescription(VFXData data, string particleSystemName)
        {
            uint capacity = (uint)data.GetSettingValue("capacity");
            return new ParticleData(particleSystemName, new Bounds(), data is ISpaceable spaceable ? spaceable.space : VFXSpace.None, capacity);
        }

        RenderingData BuildRenderingDataDescription(VFXData data, string outputName)
        {
            uint capacity = (uint)data.GetSettingValue("capacity");
            return new RenderingData($"{outputName}_RenderingData", capacity);
        }

        ITask BuildContextTask(VFXContext context, string templateName, VfxTaskStage stage, ContextExpressions contextExpressions)
        {
            VfxTemplatedTask.Args args = new VfxTemplatedTask.Args
            {
                Stage = stage,
                Topology = VfxOutputTopology.Quad, // The only topology the code generation emits so far.
                BaseArgs = new TemplatedTask.Args
                {
                    Subtasks = new(),
                    Bindings = new(),
                },
                ContextParameters = new(),
                ParameterValues = new(),
                AttributeKeyMappings = new()
                {
                    [IntermediateGraphBuilder.DefaultAttributeKey] = new(ParticleDataBindingKey, AttributeDataPath)
                }
            };

            m_SubtaskBuilder.GenerateSubtasks(context, contextExpressions, args);

            // In the future this should be handled by reflection
            CollectContextTaskArgs(context, args, contextExpressions);

            return new VfxTemplatedTask(templateName, args, EntityId.None);
        }

        void CollectContextTaskArgs(VFXContext context, VfxTemplatedTask.Args args, ContextExpressions contextExpressions)
        {
            foreach (var resourceBinding in contextExpressions.CreateContextResourceBindings())
            {
                args.BaseArgs.Bindings.Add(resourceBinding);
            }

            if (BindsCpuExpressionsOnContextTask(context))
            {
                foreach (var parameterBinding in contextExpressions.CreateContextParameterBindings())
                {
                    args.BaseArgs.Bindings.Add(parameterBinding);
                }
            }

            switch (context)
            {
                case VFXBasicInitialize initContext:
                    CollectInitArgs(initContext, args, contextExpressions);
                    break;
                case VFXBasicUpdate updateContext:
                    CollectUpdateArgs(updateContext, args, contextExpressions);
                    break;
                case VFXAbstractParticleOutput outputContext:
                    CollectOutputArgs(outputContext, args, contextExpressions);
                    break;
            }
        }

        void CollectInitArgs(VFXBasicInitialize initContext, VfxTemplatedTask.Args args,
            ContextExpressions contextExpressions)
        {
            var bindings = args.BaseArgs.Bindings;

            var particleSystemBinding = new TemplatedTaskBinding<ParticleData>(ParticleDataBindingKey);
            particleSystemBinding.Add(AttributeDataPath, initContext.attributes);
            particleSystemBinding.Add(AttributeDataPath, VFXAttribute.Alive, AttributeUsage.Write);
            particleSystemBinding.Add(AttributeDataPath, VFXAttribute.ParticleId, AttributeUsage.Write);
            particleSystemBinding.Add(AttributeDataPath, VFXAttribute.Seed, AttributeUsage.Write);
            particleSystemBinding.Add(AttributeDataPath, VFXAttribute.SpawnIndex, AttributeUsage.Write);
            particleSystemBinding.AddReadWrite(DataPath.Root + ParticleData.DeadlistKey);
            bindings.Add(particleSystemBinding);

            var contextDataBinding = new TemplatedTaskBinding<ValueData>(ContextDataKey);
            contextDataBinding.ReadPathSet.Add(MaxParticleCountPath);
            contextDataBinding.ReadPathSet.Add(SystemSeedPath);
            contextDataBinding.ReadPathSet.Add(InitSpawnIndexPath);
            bindings.Add(contextDataBinding);

            var eventListBinding = new TemplatedTaskBinding<EventListData>(EventListDataBindingKey);
            eventListBinding.ReadPathSet.Add(DataPath.Root);
            bindings.Add(eventListBinding);

            var sourceAttributeBinding = new TemplatedTaskBinding<AttributeData>(SourceAttributesBindingKey);
            sourceAttributeBinding.ReadPathSet.Add(DataPath.Root);
            bindings.Add(sourceAttributeBinding);

            args.AttributeKeyMappings.Add(IntermediateGraphBuilder.SourceAttributeKey, new(SourceAttributesBindingKey, DataPath.Root));
        }

        void CollectUpdateArgs(VFXBasicUpdate updateContext, VfxTemplatedTask.Args args,
            ContextExpressions contextExpressions)
        {
            var bindings = args.BaseArgs.Bindings;
            var subtasks = args.BaseArgs.Subtasks;

            var particleSystemBinding = new TemplatedTaskBinding<ParticleData>(ParticleDataBindingKey);
            particleSystemBinding.Add(AttributeDataPath, updateContext.attributes);
            particleSystemBinding.Add(AttributeDataPath, VFXAttribute.Alive, AttributeUsage.Read);
            particleSystemBinding.AddReadWrite(DataPath.Root + ParticleData.DeadlistKey);
            bindings.Add(particleSystemBinding);

            var contextDataBinding = new TemplatedTaskBinding<ValueData>(ContextDataKey);
            contextDataBinding.ReadPathSet.Add(MaxParticleCountPath);
            contextDataBinding.ReadPathSet.Add(SystemSeedPath);
            bindings.Add(contextDataBinding);

            var outputGpuEvents = GetOutputGpuEvents(updateContext);
            for (int i = 0; i < outputGpuEvents.Count; i++)
            {
                var bindingKey = GetOutputGpuEventBindingKey(i);
                var outputEventBinding = new TemplatedTaskBinding<EventListData>(bindingKey);
                outputEventBinding.WritePathSet.Add(DataPath.Root);
                bindings.Add(outputEventBinding);

                subtasks.Add(CreateTriggerEventSubtask(bindingKey));
            }
        }

        SubtaskDescription CreateTriggerEventSubtask(IDataKey bindingKey)
        {
            var subtaskName = $"Append events | {bindingKey}";

            AttributeSet attributeSet = new AttributeSet();
            attributeSet.AddAttribute(VFXAttributesManager.ConvertToNewCompiler(VFXAttribute.EventCount), AttributeUsage.Read);
            Dictionary<IDataKey, AttributeSet> attributeSets = new()
            {
                [IntermediateGraphBuilder.DefaultAttributeKey] = attributeSet
            };

            return new SubtaskDescription
            {
                Name = subtaskName,
                Bindings = new(),
                Task = new TemplateSubtask(subtaskName, $"{bindingKey}.eventListData.AppendEvents({VFXAttribute.EventCount.name}, threadData.index);", attributeSets)
            };
        }

        void CollectOutputArgs(VFXAbstractParticleOutput outputContext, VfxTemplatedTask.Args args,
            ContextExpressions contextExpressions)
        {
            var bindings = args.BaseArgs.Bindings;

            var particleSystemBinding = new TemplatedTaskBinding<ParticleData>(ParticleDataBindingKey);
            particleSystemBinding.AddAsRead(AttributeDataPath, outputContext.attributes);
            bindings.Add(particleSystemBinding);

            var contextDataBinding = new TemplatedTaskBinding<ValueData>(ContextDataKey);
            contextDataBinding.ReadPathSet.Add(MaxParticleCountPath);
            contextDataBinding.ReadPathSet.Add(SystemSeedPath);
            bindings.Add(contextDataBinding);

            var renderingDataBinding = new TemplatedTaskBinding<RenderingData>(RenderingDataBindingKey);
            if(outputContext.HasIndirectDraw())
                renderingDataBinding.ReadPathSet.Add(DataPath.Root + RenderingData.IndirectBuffer);
            bindings.Add(renderingDataBinding);

            if (outputContext.HasIndirectDraw())
            {
                args.ParameterValues.Add((VfxOutputTaskParameters.IndirectDrawKey, 1));
                if (outputContext.HasSorting() && OutputUpdateTaskBuilder.IsPerCamera(outputContext))
                    args.ParameterValues.Add((VfxOutputTaskParameters.IndirectPerCameraKey, 1));
            }

            // Remove write attributes from attribute set, because attribute writes in outputs are ignored
            foreach (var subtask in args.BaseArgs.Subtasks)
            {
                if (subtask.Task is TemplateSubtask templateSubtask)
                {
                    foreach (var attributeSet in templateSubtask.AttributeSets.Values)
                    {
                        attributeSet.ClearWrite();
                    }
                }
            }
        }

        static IDataKey GetOutputGpuEventBindingKey(int index) => new NameDataKey($"eventListOut_{VFXCodeGeneratorHelper.GeneratePrefix((uint)index)}");
    }
}
