using System;
using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class CompilationOutput
    {
        public List<UnityEditor.VFX.VFXExpression> SheetExpressions { get; } = new();
        public Dictionary<UnityEditor.VFX.VFXExpression, uint> SheetExpressionIndices { get; } = new();
        public List<UnityEditor.VFX.VFXExpressionDesc> SheetExpressionsDescs { get; } = new();
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
                expressions = SheetExpressionsDescs.ToArray(),
                expressionsPerSpawnEventAttribute = SheetExpressionsPerSpawnEventAttribute.ToArray(),
                values = SheetValues.ToArray()
            };
            vfxAssetDesc.eventDesc = EventDescs.ToArray();
            vfxAssetDesc.rendererSettings = new()
            {
                motionVectorGenerationMode = MotionVectorGenerationMode.Camera,
                shadowCastingMode = ShadowCastingMode.Off
            };
            vfxAssetDesc.instancingDisabledReason = VFXInstancingDisabledReason.None;

            return vfxAssetDesc;
        }

        public VFXExpressionCompiledData CreateCompiledData()
        {
            // TODO: SheetExpressionIndices only holds reduced expressions, so FindReducedExpressionIndexFromSlotCPU
            // misses for slot expressions that were reduced away. The pre-reduction mapping isn't tracked yet
            return new VFXExpressionCompiledData(SheetExpressions, SheetValues.ToArray(), SheetExpressionIndices);
        }
    }

    class OutputPass : DataGenerationPass<CompilationOutput>
    {
        CompilationOutput m_currentOutput;

        readonly Dictionary<DataContainerId, uint> m_GpuBufferDescIndices = new();
        readonly Dictionary<DataContainerId, uint> m_CpuBufferDescIndices = new();
        readonly Dictionary<DataNodeId, uint> m_ValuesExpressionIndices = new();
        readonly Dictionary<ParticleSystemContainer.ParticleSystem, int> m_ParticleSystemIndices = new();

        // The runtime reads a system's tasks as one block per phase - initialize, then everything that
        // updates the particles, sorting included, then the outputs - by offset and count, and a task type
        // carries its phase in its high bits, in that same order.
        const uint k_PhaseMask = 0xF0000000u;

        // Where each task sits in a parents-first walk of the lowered graph, which is the order the tasks
        // of one phase have to run in for a task to come after everything it depends on.
        readonly Dictionary<TaskNodeId, int> m_TaskRanks = new();
        readonly List<uint> m_StartSystems = new();
        readonly List<uint> m_StopSystems = new();

        public CompilationOutput Execute(ref CompilationContext context)
        {
            CompilationOutput output = new();
            Cleanup();
            m_currentOutput = output;
            m_currentOutput.Version = 7;

            AddDataContainerSources(ref context);

            GenerateExpressionSheet(ref context);
            GenerateExposedMappings(ref context);
            GenerateBufferDescriptions(ref context);
            GenerateSystemDescs(ref context);

            output.EventDescs.Add(new() { name = UnityEngine.VFX.VisualEffectAsset.PlayEventName, startSystems = m_StartSystems.ToArray(), stopSystems = Array.Empty<uint>(), initSystems = Array.Empty<uint>() });
            output.EventDescs.Add(new() { name = UnityEngine.VFX.VisualEffectAsset.StopEventName, startSystems = Array.Empty<uint>(), stopSystems = m_StopSystems.ToArray(), initSystems = Array.Empty<uint>() });

            Cleanup();

            return output;
        }

        uint AddExpressionRecursively(VFXExpression expression)
        {
            if (m_currentOutput.SheetExpressionIndices.TryGetValue(expression, out var cachedIndex))
                return cachedIndex;

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
            m_currentOutput.SheetExpressions.Add(expression);
            m_currentOutput.SheetExpressionIndices[expression] = vfxExpressionIndex;

            if (expression.Is(VFXExpression.Flags.Value))
            {
                m_currentOutput.SheetValues.Add(CreateValueContainerDesc(expression, vfxExpressionIndex));
            }

            return vfxExpressionIndex;
        }

        void GenerateExposedMappings(ref CompilationContext context)
        {
            var exposed = new List<VFXExposedMapping>();
            foreach (var dataNode in context.graph.DataNodes)
            {
                if (dataNode.TaskNode.Task is ExternalSetTask externalSetTask)
                {
                    uint index = AddExpressionRecursively(externalSetTask.Expression);
                    exposed.Add(new VFXExposedMapping
                    {
                        mapping = new VFXMapping(externalSetTask.ExposedName, (int)index),
                        space = externalSetTask.Space
                    });

                    foreach (var childDataNode in dataNode.Children)
                    {
                        if (childDataNode.TaskNode.Task is VfxTask or ParticleSystemTask or SpawnerTask)
                            m_ValuesExpressionIndices.Add(childDataNode.Id, index);
                    }
                }
            }
            
            m_currentOutput.SheetExposed.AddRange(exposed);
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
                        if (childDataNode.TaskNode.Task is VfxTask or ParticleSystemTask or SpawnerTask)
                        {
                            uint vfxExpressionIndex = AddExpressionRecursively(expressionTask.Expression);
                            m_ValuesExpressionIndices.Add(childDataNode.Id, vfxExpressionIndex);
                        }
                    }
                }
            }
        }

        // The buffers are whatever BufferConversionPass lowered the logical data to, so this
        // only has to walk the containers and hand their descs over.
        void GenerateBufferDescriptions(ref CompilationContext context)
        {
            foreach (var dataContainer in context.graph.DataContainers)
            {
                switch (dataContainer.RootDataView.DataDescription)
                {
                    case VfxGpuBufferData gpuBuffer:
                        m_GpuBufferDescIndices[dataContainer.Id] = AddGPUBufferData(gpuBuffer.Desc);
                        break;
                    case VfxCpuBufferData cpuBuffer:
                        m_CpuBufferDescIndices[dataContainer.Id] = AddCPUBufferData(cpuBuffer.Desc);
                        break;
                }
            }
        }

        void GenerateSystemDescs(ref CompilationContext context)
        {
            GenerateSpawnerSystemDescs(ref context);

            m_TaskRanks.Clear();
            foreach (var taskNode in context.graph.CreateTraverser().TraverseTasksParentsFirst())
                m_TaskRanks.Add(taskNode.Id, m_TaskRanks.Count);

            var particleSystemContainer = context.data.Get<ParticleSystemContainer>();
            foreach (var particleSystem in particleSystemContainer.EnumerateParentsFirst())
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
            var spawnerSystemContainer = context.data.Get<SpawnerSystemContainer>();
            foreach (var spawnerSystem in spawnerSystemContainer)
            {
                if (spawnerSystem.Tasks.Count == 0)
                    continue;

                List<VFXEditorTaskDesc> taskDescs = new();
                foreach (var task in spawnerSystem.Tasks)
                {
                    GenerateSpawnerTask(ref context, task.TaskType, context.graph.TaskNodes[task.Id], out var taskDesc);
                    taskDescs.Add(taskDesc);
                }

                // The event data was lowered to the spawner's CPU output buffer, which the system is
                // identified by; skipping systems without one keeps a missing buffer from mapping to index 0.
                var containerId = context.graph.DataViews[spawnerSystem.EventDataViewId].DataContainer.Id;
                if (!m_CpuBufferDescIndices.TryGetValue(containerId, out var spawnerOutputIndex))
                {
                    Debug.LogWarning($"Spawner system has no lowered output buffer, skipping it.");
                    continue;
                }

                var systemDesc = new VFXEditorSystemDesc
                {
                    name = "Spawn System",
                    type = UnityEngine.VFX.VFXSystemType.Spawner,
                    buffers = new[] { new UnityEditor.VFX.VFXMapping("spawner_output", (int)spawnerOutputIndex) },
                    tasks = taskDescs.ToArray(),
                    layer = ~0u
                };

                uint systemDescIndex = (uint)m_currentOutput.SystemDescs.Count;
                m_StartSystems.Add(systemDescIndex);
                m_StopSystems.Add(systemDescIndex);
                m_currentOutput.SystemDescs.Add(systemDesc);
            }
        }

        bool GenerateSpawnerTask(ref CompilationContext context, UnityEngine.VFX.VFXTaskType taskType, TaskNode spawnerTaskNode, out UnityEditor.VFX.VFXEditorTaskDesc taskDesc)
        {
            taskDesc = new();

            taskDesc.shaderSourceIndex = -1;
            taskDesc.type = taskType;

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

        bool GenerateParticleSystemDesc(ref CompilationContext context, ParticleSystemContainer.ParticleSystem particleSystem, out UnityEditor.VFX.VFXEditorSystemDesc systemDesc)
        {
            systemDesc = new();
            systemDesc.name = particleSystem.Name;
            systemDesc.type = UnityEngine.VFX.VFXSystemType.Particle;
            systemDesc.capacity = particleSystem.Capacity;

            var bufferMappings = GenerateParticleSystemBuffersMappings(context, particleSystem);
            systemDesc.buffers = bufferMappings;

            List<UnityEditor.VFX.VFXEditorTaskDesc> taskDescs = new();
            foreach (var task in OrderTasksForRuntime(context, particleSystem))
            {
                if (GenerateParticleSystemTask(ref context, task, out var taskDesc))
                {
                    taskDescs.Add(taskDesc);
                }
                systemDesc.flags = particleSystem.SystemFlags | VFXSystemFlag.SystemUsesInstancedRendering;
            }
            systemDesc.values = GenerateParticleSystemValuesMappings(context, particleSystem, out var layer);
            systemDesc.tasks = taskDescs.ToArray();
            systemDesc.instanceSplitDescs = GenerateInstanceSplitDescs(ref context, particleSystem);
            systemDesc.layer = layer;
            return true;
        }

        VFXInstanceSplitDesc[] GenerateInstanceSplitDescs(ref CompilationContext context, ParticleSystemContainer.ParticleSystem particleSystem)
        {
            var instancingSplitDescContainer = context.data.Get<InstancingSplitDescContainer>();
            var splitValueSets = instancingSplitDescContainer.Find(particleSystem);
            if (splitValueSets == null)
                return new[] { new VFXInstanceSplitDesc { values = Array.Empty<uint>() } };

            var instanceSplitDescs = new VFXInstanceSplitDesc[splitValueSets.Count];
            for (int i = 0; i < splitValueSets.Count; i++)
            {
                var values = new List<uint>();
                foreach (var dataNodeId in splitValueSets[i])
                {
                    if (m_ValuesExpressionIndices.TryGetValue(dataNodeId, out var expressionIndex))
                        values.Add(expressionIndex);
                }
                instanceSplitDescs[i] = new VFXInstanceSplitDesc { values = values.ToArray() };
            }
            return instanceSplitDescs;
        }

        VFXMapping[] GenerateParticleSystemValuesMappings(CompilationContext context, ParticleSystemContainer.ParticleSystem particleSystem, out uint layer)
        {
            var valueMappings = new List<VFXMapping>();
            var graphValueMappings = new List<(int, VFXMapping)>();
            var taskNode = context.graph.TaskNodes[particleSystem.SystemTask.Id];

            var dataLayoutContainer = context.data.Get<DataLayoutContainer>();
            layer = 0;

            // Bound under BufferKeys once the data is lowered; the conversion pass carried the old layout
            // over to the concrete container.
            var graphValuesContainerId = DataContainerId.Invalid;
            var graphValuesBinding = taskNode.DataBindings[ParticleSystemTask.BufferKeys.GraphValuesBufferKey];
            if (graphValuesBinding.HasValue)
                graphValuesContainerId = graphValuesBinding.Value.DataView.DataContainer.Id;
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
                    if (graphValuesBufferLayout == null)
                        continue;
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
        // Every buffer the system needs is bound onto its system task by
        // SystemTaskBindingsPass, under the mapping name the runtime looks it up by.
        VFXMapping[] GenerateParticleSystemBuffersMappings(CompilationContext context, ParticleSystemContainer.ParticleSystem particleSystem)
        {
            HashSet<VFXMapping> bufferMappings = new();
            foreach (var binding in context.graph.TaskNodes[particleSystem.SystemTask.Id].DataBindings)
            {
                if (binding.DataView.DataDescription is not IVfxBufferData)
                    continue;

                var name = binding.BindingDataKey.ToString();
                var containerId = binding.DataView.DataContainer.Id;
                if (m_GpuBufferDescIndices.TryGetValue(containerId, out var gpuIndex))
                    bufferMappings.Add(new VFXMapping(name, (int)gpuIndex));
                else if (m_CpuBufferDescIndices.TryGetValue(containerId, out var cpuIndex))
                    bufferMappings.Add(new VFXMapping(name, (int)cpuIndex));
            }

            return HashSetToArray(bufferMappings);
        }

        /// <summary>
        /// The system's tasks in the order the runtime runs them: by phase, and parents-first within a
        /// phase. The container keeps them in the order they were met; only the asset cares about this one.
        /// </summary>
        List<ParticleSystemContainer.Task> OrderTasksForRuntime(CompilationContext context, ParticleSystemContainer.ParticleSystem particleSystem)
        {
            var ordered = new List<(ParticleSystemContainer.Task task, uint phase, int rank)>(particleSystem.Tasks.Count);
            foreach (var task in particleSystem.Tasks)
            {
                // A task still listed under an id the graph no longer has was lowered by something that
                // never reported it; one whose task is not a VfxTask was never lowered at all.
                if (!m_TaskRanks.TryGetValue(task.Id, out var rank))
                    throw new InvalidOperationException($"Task {task.Name} of {particleSystem.Name} is no longer in the graph.");
                if (context.graph.TaskNodes[task.Id].Task is not VfxTask vfxTask)
                    throw new InvalidOperationException($"Task {task.Name} of {particleSystem.Name} was never lowered.");

                ordered.Add((task, (uint)vfxTask.TaskType & k_PhaseMask, rank));
            }

            // Ranks are unique, so the sort is a total order and its stability does not matter.
            ordered.Sort((a, b) => a.phase != b.phase ? a.phase.CompareTo(b.phase) : a.rank.CompareTo(b.rank));

            var tasks = new List<ParticleSystemContainer.Task>(ordered.Count);
            foreach (var entry in ordered)
                tasks.Add(entry.task);
            return tasks;
        }

        bool GenerateParticleSystemTask(ref CompilationContext context, ParticleSystemContainer.Task task, out UnityEditor.VFX.VFXEditorTaskDesc taskDesc)
        {
            taskDesc = new();

            var taskNode = context.graph.TaskNodes[task.Id];
            var vfxTask = taskNode.Task as VfxTask;

            // A task the runtime implements itself, a sort, runs no code of ours: it is described by
            // its buffers and its parameters alone, and the runtime refuses one that names a shader.
            if (vfxTask != null && !vfxTask.HasShaderSource)
            {
                taskDesc.shaderSourceIndex = -1;
            }
            else
            {
                var generatedCodeContainer = context.data.Get<GeneratedCodeContainer>();
                string sourceCode = generatedCodeContainer.Find(task.Id);
                bool isCompute = !vfxTask.TaskType.HasFlag(UnityEngine.VFX.VFXTaskType.Output);
                taskDesc.shaderSourceIndex = (int)AddShaderSourceDesc(task.Name, sourceCode, isCompute);
            }

            taskDesc.type = vfxTask.TaskType;

            HashSet<VFXMapping> bufferMappings = new();
            List<VFXMapping> valueMappings = new();
            List<VFXMapping> parameterMappings = new();
            var indirectionBufferIndex = -1;
            foreach (var dataBinding in taskNode.DataBindings)
            {
                // A lowered task binds its buffers under the very names the generated shader declares them
                // by, so the binding key is the mapping name.
                if (dataBinding.DataView.DataDescription is IVfxBufferData
                    && m_GpuBufferDescIndices.TryGetValue(dataBinding.DataView.DataContainer.Id, out var gpuIndex))
                {
                    var name = dataBinding.BindingDataKey.ToString();
                    bufferMappings.Add(new VFXMapping(name, (int)gpuIndex));

                    if (name.EndsWith(RenderingDataDescriptionWriter.IndirectionBufferSuffix))
                        indirectionBufferIndex = (int)gpuIndex;

                    // A written GPU event list additionally needs the eventListOut mapping the runtime scans
                    // every system for. That lookup should move here so this extra mapping can go away.
                    if (dataBinding.Usage.HasFlag(BindingUsage.Write)
                        && name.EndsWith(EventListDataDescriptionWriter.EventIndexListSuffix))
                    {
                        bufferMappings.Add(new VFXMapping($"eventListOut_{dataBinding.DataView.DataContainer.IdentifierName}", (int)gpuIndex));
                    }
                }
                if (m_ValuesExpressionIndices.TryGetValue(dataBinding.DataNode.Id, out var expressionIndex))
                {
                    // Textures and buffers are declared in the generated shader under the very name they are
                    // bound by, so the binding key is the mapping name. A task parameter is not in the shader
                    // at all, and the runtime resolves it by that same name off the parameters instead.
                    var bindingKey = dataBinding.BindingDataKey;
                    var mapping = new VFXMapping(bindingKey.ToString(), (int)expressionIndex);
                    if (vfxTask != null && vfxTask.IsParameterBinding(bindingKey))
                        parameterMappings.Add(mapping);
                    else
                        valueMappings.Add(mapping);
                }
            }

            if (indirectionBufferIndex != -1
                && vfxTask.HasParameterValue(VfxOutputTaskParameters.IndirectDrawKey))
            {
                parameterMappings.Add(new VFXMapping(VfxOutputTaskParameters.IndirectIndexKey.ToString(),
                    indirectionBufferIndex));
            }

            taskDesc.values = valueMappings.ToArray();
            taskDesc.parameters = GenerateTaskParameters(vfxTask, parameterMappings);
            taskDesc.buffers = vfxTask.TaskType == UnityEngine.VFX.VFXTaskType.PerOutputSort
                ? OrderSortTaskBuffers(bufferMappings)
                : HashSetToArray(bufferMappings);
            taskDesc.modelId = vfxTask.SourceModelId;
            taskDesc.instanceSplitIndex = task.InstanceSplitIndex;

            return true;
        }

        /// <summary>
        /// Works around the runtime reading a sort task's buffers by position rather than by name
        /// (SortTaskConstants in Modules/VFX/Public/Systems/VFXParticleSystem.cpp): they have to come in
        /// src, scratch, dst order, and the scratch mapping has to be there even when no buffer backs it,
        /// which is how the runtime is told to sort in a single pass.
        /// </summary>
        static VFXMapping[] OrderSortTaskBuffers(HashSet<VFXMapping> bufferMappings)
        {
            var ordered = new List<VFXMapping>();
            foreach (var key in SortTask.OrderedBufferKeys)
            {
                var name = key.ToString();
                var index = -1;
                foreach (var mapping in bufferMappings)
                {
                    if (mapping.name == name)
                    {
                        index = mapping.index;
                        break;
                    }
                }
                ordered.Add(new VFXMapping(name, index));
            }

            foreach (var mapping in bufferMappings)
            {
                if (!ordered.Exists(orderedMapping => orderedMapping.name == mapping.name))
                    ordered.Add(mapping);
            }

            return ordered.ToArray();
        }

        // The parameters the task carries a constant for come in the order it declared them, ahead of
        // the ones a graph value feeds.
        static VFXMapping[] GenerateTaskParameters(VfxTask vfxTask, List<VFXMapping> parameterMappings)
        {
            if (vfxTask == null || vfxTask.ParameterValues.Count == 0)
                return parameterMappings.ToArray();

            var mappings = new List<VFXMapping>();
            foreach (var (key, value) in vfxTask.ParameterValues)
                mappings.Add(new VFXMapping(key.ToString(), value));

            mappings.AddRange(parameterMappings);
            return mappings.ToArray();
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
            var vfxExpressionIndex = (uint)m_currentOutput.SheetExpressionsDescs.Count;
            m_currentOutput.SheetExpressionsDescs.Add(vfxExpression);
            return vfxExpressionIndex;
        }

        uint AddCPUBufferData(UnityEditor.VFX.VFXCPUBufferDesc bufferDesc)
        {
            uint bufferDataIndex = (uint)m_currentOutput.CpuBufferDescs.Count;
            m_currentOutput.CpuBufferDescs.Add(bufferDesc);
            return bufferDataIndex;
        }

        uint AddGPUBufferData(UnityEditor.VFX.VFXGPUBufferDesc bufferDesc)
        {
            uint bufferDataIndex = (uint)m_currentOutput.GpuBufferDescs.Count;
            m_currentOutput.GpuBufferDescs.Add(bufferDesc);
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
            m_TaskRanks.Clear();
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
