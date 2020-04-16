using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.VFX;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;
using System.IO;

namespace UnityEditor.VFX
{
    struct VFXContextCompiledData
    {
        public VFXExpressionMapper cpuMapper;
        public VFXExpressionMapper gpuMapper;
        public VFXUniformMapper uniformMapper;
        public VFXMapping[] parameters;
        public int indexInShaderSource;
    }

    enum VFXCompilationMode
    {
        Edition,
        Runtime,
    }

    class VFXDependentBuffersData
    {
        public Dictionary<VFXData, int> attributeBuffers = new Dictionary<VFXData, int>();
        public Dictionary<VFXData, int> stripBuffers = new Dictionary<VFXData, int>();
        public Dictionary<VFXData, int> eventBuffers = new Dictionary<VFXData, int>();
    }

    class VFXGraphCompiledData
    {
        public const uint compiledVersion = 2;

        public VFXGraphCompiledData(VFXGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException("VFXGraph cannot be null");
            m_Graph = graph;
        }

        private struct GeneratedCodeData
        {
            public VFXContext context;
            public bool computeShader;
            public System.Text.StringBuilder content;
            public VFXCompilationMode compilMode;
        }

        private static VFXExpressionObjectValueContainerDesc<T> CreateObjectValueDesc<T>(VFXExpression exp, int expIndex)
        {
            var desc = new VFXExpressionObjectValueContainerDesc<T>();
            desc.instanceID = exp.Get<int>();
            return desc;
        }

        private static VFXExpressionValueContainerDesc<T> CreateValueDesc<T>(VFXExpression exp, int expIndex)
        {
            var desc = new VFXExpressionValueContainerDesc<T>();
            desc.value = exp.Get<T>();
            return desc;
        }

        private void SetValueDesc<T>(VFXExpressionValueContainerDesc desc, VFXExpression exp)
        {
            ((VFXExpressionValueContainerDesc<T>)desc).value = exp.Get<T>();
        }

        private void SetObjectValueDesc<T>(VFXExpressionValueContainerDesc desc, VFXExpression exp)
        {
            ((VFXExpressionObjectValueContainerDesc<T>)desc).instanceID = exp.Get<int>();
        }

        public uint FindReducedExpressionIndexFromSlotCPU(VFXSlot slot)
        {
            if (m_ExpressionGraph == null)
            {
                return uint.MaxValue;
            }
            var targetExpression = slot.GetExpression();
            if (targetExpression == null)
            {
                return uint.MaxValue;
            }

            if (!m_ExpressionGraph.CPUExpressionsToReduced.ContainsKey(targetExpression))
            {
                return uint.MaxValue;
            }

            var ouputExpression = m_ExpressionGraph.CPUExpressionsToReduced[targetExpression];
            return (uint)m_ExpressionGraph.GetFlattenedIndex(ouputExpression);
        }

        private static void FillExpressionDescs(List<VFXExpressionDesc> outExpressionDescs, List<VFXExpressionValueContainerDesc> outValueDescs, VFXExpressionGraph graph)
        {
            var flatGraph = graph.FlattenedExpressions;
            var numFlattenedExpressions = flatGraph.Count;

            for (int i = 0; i < numFlattenedExpressions; ++i)
            {
                var exp = flatGraph[i];

                // Must match data in C++ expression
                if (exp.Is(VFXExpression.Flags.Value))
                {
                    VFXExpressionValueContainerDesc value;
                    switch (exp.valueType)
                    {
                        case VFXValueType.Float: value = CreateValueDesc<float>(exp, i); break;
                        case VFXValueType.Float2: value = CreateValueDesc<Vector2>(exp, i); break;
                        case VFXValueType.Float3: value = CreateValueDesc<Vector3>(exp, i); break;
                        case VFXValueType.Float4: value = CreateValueDesc<Vector4>(exp, i); break;
                        case VFXValueType.Int32: value = CreateValueDesc<int>(exp, i); break;
                        case VFXValueType.Uint32: value = CreateValueDesc<uint>(exp, i); break;
                        case VFXValueType.Texture2D:
                        case VFXValueType.Texture2DArray:
                        case VFXValueType.Texture3D:
                        case VFXValueType.TextureCube:
                        case VFXValueType.TextureCubeArray:
                            value = CreateObjectValueDesc<Texture>(exp, i);
                            break;
                        case VFXValueType.Matrix4x4: value = CreateValueDesc<Matrix4x4>(exp, i); break;
                        case VFXValueType.Curve: value = CreateValueDesc<AnimationCurve>(exp, i); break;
                        case VFXValueType.ColorGradient: value = CreateValueDesc<Gradient>(exp, i); break;
                        case VFXValueType.Mesh: value = CreateObjectValueDesc<Mesh>(exp, i); break;
                        case VFXValueType.Boolean: value = CreateValueDesc<bool>(exp, i); break;
                        default: throw new InvalidOperationException("Invalid type");
                    }
                    value.expressionIndex = (uint)i;
                    outValueDescs.Add(value);
                }

                outExpressionDescs.Add(new VFXExpressionDesc
                {
                    op = exp.operation,
                    data = exp.GetOperands(graph).ToArray(),
                });
            }
        }

        private static void CollectExposedDesc(List<VFXMapping> outExposedParameters, string name, VFXSlot slot, VFXExpressionGraph graph)
        {
            var expression = slot.valueType != VFXValueType.None ? slot.GetInExpression() : null;
            if (expression != null)
            {
                var exprIndex = graph.GetFlattenedIndex(expression);
                if (exprIndex == -1)
                    throw new InvalidOperationException("Unable to retrieve value from exposed for " + name);

                outExposedParameters.Add(new VFXMapping()
                {
                    name = name,
                    index = exprIndex
                });
            }
            else
            {
                foreach (var child in slot.children)
                {
                    CollectExposedDesc(outExposedParameters, name + "_" + child.name, child, graph);
                }
            }
        }

        private static void FillExposedDescs(List<VFXMapping> outExposedParameters, VFXExpressionGraph graph, IEnumerable<VFXParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.exposed && !parameter.isOutput)
                {
                    CollectExposedDesc(outExposedParameters, parameter.exposedName, parameter.GetOutputSlot(0), graph);
                }
            }
        }

        private static void FillEventAttributeDescs(List<VFXLayoutElementDesc> eventAttributeDescs, VFXExpressionGraph graph, IEnumerable<VFXContext> contexts)
        {
            foreach (var context in contexts.Where(o => o.contextType == VFXContextType.Spawner))
            {
                foreach (var linked in context.outputContexts)
                {
                    var data = linked.GetData();
                    if (data)
                    {
                        foreach (var attribute in data.GetAttributes())
                        {
                            if ((attribute.mode & VFXAttributeMode.ReadSource) != 0 && !eventAttributeDescs.Any(o => o.name == attribute.attrib.name))
                            {
                                eventAttributeDescs.Add(new VFXLayoutElementDesc()
                                {
                                    name = attribute.attrib.name,
                                    type = attribute.attrib.type
                                });
                            }
                        }
                    }
                }
            }

            var structureLayoutTotalSize = (uint)eventAttributeDescs.Sum(e => (long)VFXExpression.TypeToSize(e.type));
            var currentLayoutSize = 0u;
            var listWithOffset = new List<VFXLayoutElementDesc>();
            eventAttributeDescs.ForEach(e =>
            {
                e.offset.element = currentLayoutSize;
                e.offset.structure = structureLayoutTotalSize;
                currentLayoutSize += (uint)VFXExpression.TypeToSize(e.type);
                listWithOffset.Add(e);
            });

            eventAttributeDescs.Clear();
            eventAttributeDescs.AddRange(listWithOffset);
        }

        private static List<VFXContext> CollectContextParentRecursively(IEnumerable<VFXContext> inputList, ref SubgraphInfos subgraphContexts)
        {
            var contextEffectiveInputLinks = subgraphContexts.contextEffectiveInputLinks;
            var contextList = inputList.SelectMany(o => contextEffectiveInputLinks[o].SelectMany(t => t)).Select(t => t.context).Distinct().ToList();

            if (contextList.Any(o => contextEffectiveInputLinks[o].Any()))
            {
                var parentContextList = CollectContextParentRecursively(contextList.Except(inputList), ref subgraphContexts);
                foreach (var context in parentContextList)
                {
                    if (!contextList.Contains(context))
                    {
                        contextList.Add(context);
                    }
                }
            }
            return contextList;
        }

        private static VFXContext[] CollectSpawnersHierarchy(IEnumerable<VFXContext> vfxContext, ref SubgraphInfos subgraphContexts)
        {
            var initContext = vfxContext.Where(o => o.contextType == VFXContextType.Init).ToList();
            var spawnerList = CollectContextParentRecursively(initContext, ref subgraphContexts);
            return spawnerList.Where(o => o.contextType == VFXContextType.Spawner).Reverse().ToArray();
        }

        struct SpawnInfo
        {
            public int bufferIndex;
            public int systemIndex;
        }

        private static VFXCPUBufferData ComputeArrayOfStructureInitialData(IEnumerable<VFXLayoutElementDesc> layout)
        {
            var data = new VFXCPUBufferData();
            foreach (var element in layout)
            {
                var attribute = VFXAttribute.AllAttribute.FirstOrDefault(o => o.name == element.name);
                bool useAttribute = attribute.name == element.name;
                if (element.type == VFXValueType.Boolean)
                {
                    var v = useAttribute ? attribute.value.Get<bool>() : default(bool);
                    data.PushBool(v);
                }
                else if (element.type == VFXValueType.Float)
                {
                    var v = useAttribute ? attribute.value.Get<float>() : default(float);
                    data.PushFloat(v);
                }
                else if (element.type == VFXValueType.Float2)
                {
                    var v = useAttribute ? attribute.value.Get<Vector2>() : default(Vector2);
                    data.PushFloat(v.x);
                    data.PushFloat(v.y);
                }
                else if (element.type == VFXValueType.Float3)
                {
                    var v = useAttribute ? attribute.value.Get<Vector3>() : default(Vector3);
                    data.PushFloat(v.x);
                    data.PushFloat(v.y);
                    data.PushFloat(v.z);
                }
                else if (element.type == VFXValueType.Float4)
                {
                    var v = useAttribute ? attribute.value.Get<Vector4>() : default(Vector4);
                    data.PushFloat(v.x);
                    data.PushFloat(v.y);
                    data.PushFloat(v.z);
                    data.PushFloat(v.w);
                }
                else if (element.type == VFXValueType.Int32)
                {
                    var v = useAttribute ? attribute.value.Get<int>() : default(int);
                    data.PushInt(v);
                }
                else if (element.type == VFXValueType.Uint32)
                {
                    var v = useAttribute ? attribute.value.Get<uint>() : default(uint);
                    data.PushUInt(v);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return data;
        }

        void RecursePutSubgraphParent(Dictionary<VFXSubgraphContext, VFXSubgraphContext> parents, List<VFXSubgraphContext> subgraphs, VFXSubgraphContext subgraph)
        {
            foreach (var subSubgraph in subgraph.subChildren.OfType<VFXSubgraphContext>().Where(t => t.subgraph != null))
            {
                subgraphs.Add(subSubgraph);
                parents[subSubgraph] = subgraph;

                RecursePutSubgraphParent(parents, subgraphs, subSubgraph);
            }
        }

        static List<VFXContextLink>[] ComputeContextEffectiveLinks(VFXContext context, ref SubgraphInfos subgraphInfos)
        {
            List<VFXContextLink>[] result = new List<VFXContextLink>[context.inputFlowSlot.Length];
            Dictionary<string, int> eventNameIndice = new Dictionary<string, int>();
            for (int i = 0; i < context.inputFlowSlot.Length; ++i)
            {
                result[i] = new List<VFXContextLink>();
                VFXSubgraphContext parentSubgraph = null;

                subgraphInfos.spawnerSubgraph.TryGetValue(context, out parentSubgraph);

                List<VFXContext> subgraphAncestors = new List<VFXContext>();

                subgraphAncestors.Add(context);

                while (parentSubgraph != null)
                {
                    subgraphAncestors.Add(parentSubgraph);
                    if (!subgraphInfos.subgraphParents.TryGetValue(parentSubgraph, out parentSubgraph))
                    {
                        parentSubgraph = null;
                    }
                }

                List<List<int>> defaultEventPaths = new List<List<int>>();

                defaultEventPaths.Add(new List<int>(new int[] { i }));

                List<List<int>> newEventPaths = new List<List<int>>();

                var usedContexts = new List<VFXContext>();

                for (int j = 0; j < subgraphAncestors.Count; ++j)
                {
                    var sg = subgraphAncestors[j];
                    var nextSg = j < subgraphAncestors.Count - 1 ? subgraphAncestors[j + 1] as VFXSubgraphContext : null;

                    foreach (var path in defaultEventPaths)
                    {
                        int currentFlowIndex = path.Last();
                        var eventSlot = sg.inputFlowSlot[currentFlowIndex];
                        var eventSlotSpawners = eventSlot.link.Where(t => !(t.context is VFXBasicEvent));

                        if (eventSlotSpawners.Any())
                        {
                            foreach (var evt in eventSlotSpawners)
                            {
                                result[i].Add(evt);
                            }
                        }

                        var eventSlotEvents = eventSlot.link.Where(t => t.context is VFXBasicEvent);

                        if (eventSlotEvents.Any())
                        {
                            foreach (var evt in eventSlotEvents)
                            {
                                string eventName = (evt.context as VFXBasicEvent).eventName;

                                switch (eventName)
                                {
                                    case VisualEffectAsset.PlayEventName:
                                        if (nextSg != null)
                                            newEventPaths.Add(path.Concat(new int[] { 0 }).ToList());
                                        else
                                            result[i].Add(evt);
                                        break;
                                    case VisualEffectAsset.StopEventName:
                                        if (nextSg != null)
                                            newEventPaths.Add(path.Concat(new int[] { 1 }).ToList());
                                        else
                                            result[i].Add(evt);
                                        break;
                                    default:
                                    {
                                        if (nextSg != null)
                                        {
                                            int eventIndex = nextSg.GetInputFlowIndex(eventName);
                                            if (eventIndex != -1)
                                                newEventPaths.Add(path.Concat(new int[] { eventIndex }).ToList());
                                        }
                                        else
                                        {
                                            result[i].Add(evt);
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        else if (!eventSlot.link.Any())
                        {
                            if (!(sg is VFXSubgraphContext))
                            {
                                if (nextSg != null)
                                {
                                    int fixedSlotIndex = currentFlowIndex > 1 ? currentFlowIndex : nextSg.GetInputFlowIndex(currentFlowIndex == 1 ? VisualEffectAsset.StopEventName : VisualEffectAsset.PlayEventName);
                                    if (fixedSlotIndex >= 0)
                                        newEventPaths.Add(path.Concat(new int[] { fixedSlotIndex }).ToList());
                                }
                                else
                                    newEventPaths.Add(path.Concat(new int[] { currentFlowIndex }).ToList());
                            }
                            else
                            {
                                var sgsg = sg as VFXSubgraphContext;

                                var eventName = sgsg.GetInputFlowName(currentFlowIndex);

                                var eventCtx = sgsg.GetEventContext(eventName);
                                if (eventCtx != null)
                                    result[i].Add(new VFXContextLink() { slotIndex = 0, context = eventCtx });
                            }
                        }
                    }
                    defaultEventPaths.Clear();
                    defaultEventPaths.AddRange(newEventPaths);
                    newEventPaths.Clear();
                }
            }
            return result;
        }

        private static void FillSpawner(Dictionary<VFXContext, SpawnInfo> outContextSpawnToSpawnInfo,
            List<VFXCPUBufferDesc> outCpuBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            IEnumerable<VFXContext> contexts,
            VFXExpressionGraph graph,
            List<VFXLayoutElementDesc> globalEventAttributeDescs,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            ref SubgraphInfos subgraphInfos,
            VFXSystemNames systemNames = null)
        {
            var spawners = CollectSpawnersHierarchy(contexts, ref subgraphInfos);
            foreach (var it in spawners.Select((spawner, index) => new { spawner, index }))
            {
                outContextSpawnToSpawnInfo.Add(it.spawner, new SpawnInfo() { bufferIndex = outCpuBufferDescs.Count, systemIndex = it.index });
                outCpuBufferDescs.Add(new VFXCPUBufferDesc()
                {
                    capacity = 1u,
                    stride = globalEventAttributeDescs.First().offset.structure,
                    layout = globalEventAttributeDescs.ToArray(),
                    initialData = ComputeArrayOfStructureInitialData(globalEventAttributeDescs)
                });
            }
            foreach (var spawnContext in spawners)
            {
                var buffers = new List<VFXMapping>();
                buffers.Add(new VFXMapping()
                {
                    index = outContextSpawnToSpawnInfo[spawnContext].bufferIndex,
                    name = "spawner_output"
                });

                for (int indexSlot = 0; indexSlot < 2 && indexSlot < spawnContext.inputFlowSlot.Length; ++indexSlot)
                {
                    foreach (var input in subgraphInfos.contextEffectiveInputLinks[spawnContext][indexSlot])
                    {
                        var inputContext = input.context;
                        if (outContextSpawnToSpawnInfo.ContainsKey(inputContext))
                        {
                            buffers.Add(new VFXMapping()
                            {
                                index = outContextSpawnToSpawnInfo[inputContext].bufferIndex,
                                name = "spawner_input_" + (indexSlot == 0 ? "OnPlay" : "OnStop")
                            });
                        }
                    }
                }

                var contextData = contextToCompiledData[spawnContext];
                var contextExpressions = contextData.cpuMapper.CollectExpression(-1);
                var systemValueMappings = new List<VFXMapping>();
                foreach (var contextExpression in contextExpressions)
                {
                    var expressionIndex = graph.GetFlattenedIndex(contextExpression.exp);
                    systemValueMappings.Add(new VFXMapping(contextExpression.name, expressionIndex));
                }
                string nativeName = string.Empty;
                if (systemNames != null)
                    nativeName = systemNames.GetUniqueSystemName(spawnContext);
                else
                    throw new InvalidOperationException("system names manager cannot be null");
                outSystemDescs.Add(new VFXEditorSystemDesc()
                {
                    values = systemValueMappings.ToArray(),
                    buffers = buffers.ToArray(),
                    capacity = 0u,
                    name = nativeName,
                    flags = VFXSystemFlag.SystemDefault,
                    layer = uint.MaxValue,
                    tasks = spawnContext.activeFlattenedChildrenWithImplicit.Select((b, index) =>
                    {
                        var spawnerBlock = b as VFXAbstractSpawner;
                        if (spawnerBlock == null)
                        {
                            throw new InvalidCastException("Unexpected block type in spawnerContext");
                        }
                        if (spawnerBlock.spawnerType == VFXTaskType.CustomCallbackSpawner && spawnerBlock.customBehavior == null)
                        {
                            throw new InvalidOperationException("VFXAbstractSpawner excepts a custom behavior for custom callback type");
                        }
                        if (spawnerBlock.spawnerType != VFXTaskType.CustomCallbackSpawner && spawnerBlock.customBehavior != null)
                        {
                            throw new InvalidOperationException("VFXAbstractSpawner only expects a custom behavior for custom callback type");
                        }

                        var cpuExpression = contextData.cpuMapper.CollectExpression(index, false).Select(o =>
                        {
                            return new VFXMapping
                            {
                                index = graph.GetFlattenedIndex(o.exp),
                                name = o.name
                            };
                        }).ToArray();

                        Object processor = null;
                        if (spawnerBlock.customBehavior != null)
                        {
                            var assets = AssetDatabase.FindAssets("t:TextAsset " + spawnerBlock.customBehavior.Name);
                            if (assets.Length != 1)
                            {
                                // AssetDatabase.FindAssets will not search in package by default. Search in our package explicitely
                                assets = AssetDatabase.FindAssets("t:TextAsset " + spawnerBlock.customBehavior.Name, new string[] { VisualEffectGraphPackageInfo.assetPackagePath });
                                if (assets.Length != 1)
                                {
                                    throw new InvalidOperationException("Unable to find the definition .cs file for " + spawnerBlock.customBehavior + " Make sure that the class name and file name match");
                                }
                            }

                            var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                            processor = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                        }

                        return new VFXEditorTaskDesc
                        {
                            type = (UnityEngine.VFX.VFXTaskType)spawnerBlock.spawnerType,
                            buffers = new VFXMapping[0],
                            values = cpuExpression.ToArray(),
                            parameters = contextData.parameters,
                            externalProcessor = processor
                        };
                    }).ToArray()
                });
            }
        }

        struct SubgraphInfos
        {
            public Dictionary<VFXSubgraphContext, VFXSubgraphContext> subgraphParents;
            public Dictionary<VFXContext, VFXSubgraphContext> spawnerSubgraph;
            public List<VFXSubgraphContext> subgraphs;
            public Dictionary<VFXContext, List<VFXContextLink>[]> contextEffectiveInputLinks;

            public List<VFXContextLink> GetContextEffectiveOutputLinks(VFXContext context, int slot)
            {
                List<VFXContextLink> effectiveOuts = new List<VFXContextLink>();

                foreach (var kv in contextEffectiveInputLinks)
                {
                    for (int i = 0; i < kv.Value.Length; ++i)
                    {
                        foreach (var link in kv.Value[i])
                        {
                            if (link.context == context && link.slotIndex == slot)
                                effectiveOuts.Add(new VFXContextLink() { context = kv.Key, slotIndex = i });
                        }
                    }
                }
                return effectiveOuts;
            }
        }

        private static void FillEvent(List<VFXEventDesc> outEventDesc, Dictionary<VFXContext, SpawnInfo> contextSpawnToSpawnInfo, IEnumerable<VFXContext> contexts, ref SubgraphInfos subgraphInfos)
        {
            var contextEffectiveInputLinks = subgraphInfos.contextEffectiveInputLinks;

            var allPlayNotLinked = contextSpawnToSpawnInfo.Where(o => !contextEffectiveInputLinks[o.Key][0].Any()).Select(o => (uint)o.Value.systemIndex).ToList();
            var allStopNotLinked = contextSpawnToSpawnInfo.Where(o => !contextEffectiveInputLinks[o.Key][1].Any()).Select(o => (uint)o.Value.systemIndex).ToList();

            var eventDescTemp = new[]
            {
                new { eventName = VisualEffectAsset.PlayEventName, playSystems = allPlayNotLinked, stopSystems = new List<uint>() },
                new { eventName = VisualEffectAsset.StopEventName, playSystems = new List<uint>(), stopSystems = allStopNotLinked },
            }.ToList();

            var specialNames = new HashSet<string>(new string[] {VisualEffectAsset.PlayEventName, VisualEffectAsset.StopEventName});


            var events = contexts.Where(o => o.contextType == VFXContextType.Event);
            foreach (var evt in events)
            {
                var eventName = (evt as VFXBasicEvent).eventName;

                if (subgraphInfos.spawnerSubgraph.ContainsKey(evt) && specialNames.Contains(eventName))
                    continue;

                List<VFXContextLink> effecitveOuts = subgraphInfos.GetContextEffectiveOutputLinks(evt, 0);

                foreach (var link in effecitveOuts)
                {
                    if (contextSpawnToSpawnInfo.ContainsKey(link.context))
                    {
                        var eventIndex = eventDescTemp.FindIndex(o => o.eventName == eventName);
                        if (eventIndex == -1)
                        {
                            eventIndex = eventDescTemp.Count;
                            eventDescTemp.Add(new
                            {
                                eventName = eventName,
                                playSystems = new List<uint>(),
                                stopSystems = new List<uint>(),
                            });
                        }

                        var startSystem = link.slotIndex == 0;
                        var spawnerIndex = (uint)contextSpawnToSpawnInfo[link.context].systemIndex;
                        if (startSystem)
                        {
                            eventDescTemp[eventIndex].playSystems.Add(spawnerIndex);
                        }
                        else
                        {
                            eventDescTemp[eventIndex].stopSystems.Add(spawnerIndex);
                        }
                    }
                }
            }
            outEventDesc.Clear();
            var relevantEvent = eventDescTemp.Where(o => o.playSystems.Any() || o.stopSystems.Any());
            outEventDesc.AddRange(relevantEvent.Select(o => new VFXEventDesc() { name = o.eventName, startSystems = o.playSystems.ToArray(), stopSystems = o.stopSystems.ToArray() }));
        }

        private void GenerateShaders(List<GeneratedCodeData> outGeneratedCodeData, VFXExpressionGraph graph, IEnumerable<VFXContext> contexts, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData, VFXCompilationMode compilationMode, HashSet<string> dependencies)
        {
            Profiler.BeginSample("VFXEditor.GenerateShaders");
            try
            {
                foreach (var context in contexts)
                {
                    var gpuMapper = graph.BuildGPUMapper(context);
                    var uniformMapper = new VFXUniformMapper(gpuMapper, context.doesGenerateShader);

                    // Add gpu and uniform mapper
                    var contextData = contextToCompiledData[context];
                    contextData.gpuMapper = gpuMapper;
                    contextData.uniformMapper = uniformMapper;
                    contextToCompiledData[context] = contextData;

                    if (context.doesGenerateShader)
                    {
                        var generatedContent = VFXCodeGenerator.Build(context, compilationMode, contextData, dependencies);

                        if (generatedContent != null)
                        {
                            outGeneratedCodeData.Add(new GeneratedCodeData()
                            {
                                context = context,
                                computeShader = context.codeGeneratorCompute,
                                compilMode = compilationMode,
                                content = generatedContent
                            });
                        }
                    }
                }

                var resource = m_Graph.GetResource();
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private static VFXShaderSourceDesc[] SaveShaderFiles(VisualEffectResource resource,
            List<GeneratedCodeData> generatedCodeData,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            VFXSystemNames systemNames)
        {
            Profiler.BeginSample("VFXEditor.SaveShaderFiles");
            try
            {
                var descs = new VFXShaderSourceDesc[generatedCodeData.Count];
                var assetName = string.Empty;
                if (resource.asset != null)
                {
                    assetName = resource.asset.name; //Most Common case, asset is already available
                }
                else
                {
                    var assetPath = AssetDatabase.GetAssetPath(resource); //Can occur during Copy/Past or Rename
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        assetName = Path.GetFileNameWithoutExtension(assetPath);
                    }
                    else if (resource.name != null) //Unable to retrieve asset path, last fallback use serialized resource name
                    {
                        assetName = resource.name;
                    }
                }

                for (int i = 0; i < generatedCodeData.Count; ++i)
                {
                    var generated = generatedCodeData[i];

                    var systemName = systemNames.GetUniqueSystemName(generated.context.GetData());
                    var contextLetter = generated.context.letter;
                    var contextName = string.IsNullOrEmpty(generated.context.label) ? generated.context.libraryName : generated.context.label;

                    var shaderName = string.Empty;
                    var fileName = string.Empty;
                    if (contextLetter == '\0')
                    {
                        fileName = string.Format("[{0}] [{1}] {2}", assetName, systemName, contextName);
                        shaderName = string.Format("Hidden/VFX/{0}/{1}/{2}", assetName, systemName, contextName);
                    }
                    else
                    {
                        fileName = string.Format("[{0}] [{1}]{2} {3}", assetName, systemName, contextLetter, contextName);
                        shaderName = string.Format("Hidden/VFX/{0}/{1}/{2}/{3}", assetName, systemName, contextLetter, contextName);
                    }

                    if (!generated.computeShader)
                    {
                        generated.content.Insert(0, "Shader \"" + shaderName + "\"\n");
                    }
                    descs[i].source = generated.content.ToString();
                    descs[i].name = fileName;
                    descs[i].compute = generated.computeShader;
                }

                for (int i = 0; i < generatedCodeData.Count; ++i)
                {
                    var generated = generatedCodeData[i];
                    var contextData = contextToCompiledData[generated.context];
                    contextData.indexInShaderSource = i;
                    contextToCompiledData[generated.context] = contextData;
                }

                return descs;
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        public void FillDependentBuffer(
            IEnumerable<VFXData> compilableData,
            List<VFXGPUBufferDesc> bufferDescs,
            VFXDependentBuffersData buffers)
        {
            // TODO This should be in VFXDataParticle
            foreach (var data in compilableData.OfType<VFXDataParticle>())
            {
                int attributeBufferIndex = -1;
                if (data.attributeBufferSize > 0)
                {
                    attributeBufferIndex = bufferDescs.Count;
                    bufferDescs.Add(data.attributeBufferDesc);
                }
                buffers.attributeBuffers.Add(data, attributeBufferIndex);

                int stripBufferIndex = -1;
                if (data.hasStrip)
                {
                    stripBufferIndex = bufferDescs.Count;
                    uint stripCapacity = (uint)data.GetSettingValue("stripCapacity");
                    bufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = stripCapacity * 4, stride = 4 });
                }
                buffers.stripBuffers.Add(data, stripBufferIndex);
            }

            //Prepare GPU event buffer
            foreach (var data in compilableData.SelectMany(o => o.dependenciesOut).Distinct().OfType<VFXDataParticle>())
            {
                var eventBufferIndex = -1;
                uint capacity = (uint)data.GetSettingValue("capacity");
                if (capacity > 0)
                {
                    eventBufferIndex = bufferDescs.Count;
                    bufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Append, size = capacity, stride = 4 });
                }
                buffers.eventBuffers.Add(data, eventBufferIndex);
            }
        }

        VFXRendererSettings GetRendererSettings(VFXRendererSettings initialSettings, IEnumerable<IVFXSubRenderer> subRenderers)
        {
            var settings = initialSettings;
            settings.shadowCastingMode = subRenderers.Any(r => r.hasShadowCasting) ? ShadowCastingMode.On : ShadowCastingMode.Off;
            settings.motionVectorGenerationMode = subRenderers.Any(r => r.hasMotionVector) ? MotionVectorGenerationMode.Object : MotionVectorGenerationMode.Camera;
            return settings;
        }

        private class VFXImplicitContextOfExposedExpression : VFXContext
        {
            private VFXExpressionMapper mapper;

            public VFXImplicitContextOfExposedExpression() : base(VFXContextType.None, VFXDataType.None, VFXDataType.None) {}

            private static void CollectExposedExpression(List<VFXExpression> expressions, VFXSlot slot)
            {
                var expression = slot.valueType != VFXValueType.None ? slot.GetInExpression() : null;
                if (expression != null)
                    expressions.Add(expression);
                else
                {
                    foreach (var child in slot.children)
                        CollectExposedExpression(expressions, child);
                }
            }

            public void FillExpression(VFXGraph graph)
            {
                var allExposedParameter = graph.children.OfType<VFXParameter>().Where(o => o.exposed);
                var expressionsList = new List<VFXExpression>();
                foreach (var parameter in allExposedParameter)
                    CollectExposedExpression(expressionsList, parameter.outputSlots[0]);

                mapper = new VFXExpressionMapper();
                for (int i = 0; i < expressionsList.Count; ++i)
                    mapper.AddExpression(expressionsList[i], "ImplicitExposedExpression", i);
            }

            public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
            {
                return target == VFXDeviceTarget.CPU ? mapper : null;
            }
        }

        static public Action<VisualEffectResource, bool> k_FnVFXResource_SetCompileInitialVariants = Find_FnVFXResource_SetCompileInitialVariants();

        static Action<VisualEffectResource, bool> Find_FnVFXResource_SetCompileInitialVariants()
        {
            var property = typeof(VisualEffectResource).GetProperty("compileInitialVariants");
            if (property != null)
            {
                return delegate(VisualEffectResource rsc, bool value)
                {
                    property.SetValue(rsc, value, null);
                };
            }
            return null;
        }

        void ComputeEffectiveInputLinks(ref SubgraphInfos subgraphInfos, IEnumerable<VFXContext> compilableContexts)
        {
            var contextEffectiveInputLinks = subgraphInfos.contextEffectiveInputLinks;


            foreach (var context in compilableContexts.Where(t => !(t is VFXSubgraphContext)))
            {
                contextEffectiveInputLinks[context] = ComputeContextEffectiveLinks(context, ref subgraphInfos);

                ComputeEffectiveInputLinks(ref subgraphInfos, contextEffectiveInputLinks[context].SelectMany(t => t).Select(t => t.context).Where(t => !contextEffectiveInputLinks.ContainsKey(t)));
            }
        }

        public void Compile(VFXCompilationMode compilationMode, bool forceShaderValidation)
        {
            // Prevent doing anything ( and especially showing progesses ) in an empty graph.
            if (m_Graph.children.Count() < 1)
            {
                // Cleaning
                if (m_Graph.visualEffectResource != null)
                    m_Graph.visualEffectResource.ClearRuntimeData();

                m_ExpressionGraph = new VFXExpressionGraph();
                m_ExpressionValues = new VFXExpressionValueContainerDesc[] {};
                return;
            }

            Profiler.BeginSample("VFXEditor.CompileAsset");
            float nbSteps = 12.0f;
            string assetPath = AssetDatabase.GetAssetPath(visualEffectResource);
            string progressBarTitle = "Compiling " + assetPath;
            try
            {
                EditorUtility.DisplayProgressBar(progressBarTitle, "Collecting dependencies", 0 / nbSteps);
                var models = new HashSet<ScriptableObject>();
                m_Graph.CollectDependencies(models, false);

                var resource = m_Graph.GetResource();
                resource.ClearSourceDependencies();

                HashSet<string> sourceDependencies = new HashSet<string>();
                foreach (VFXModel model in models.Where(t => t is IVFXSlotContainer))
                {
                    model.GetSourceDependentAssets(sourceDependencies);
                }

                var contexts = models.OfType<VFXContext>().ToArray();

                foreach (var c in contexts) // Unflag all contexts
                    c.MarkAsCompiled(false);

                IEnumerable<VFXContext> compilableContexts = contexts.Where(c => c.CanBeCompiled()).ToArray();
                var compilableData = models.OfType<VFXData>().Where(d => d.CanBeCompiled());

                IEnumerable<VFXContext> implicitContexts = Enumerable.Empty<VFXContext>();
                foreach (var d in compilableData) // Flag compiled contexts
                    implicitContexts = implicitContexts.Concat(d.InitImplicitContexts());
                compilableContexts = compilableContexts.Concat(implicitContexts.ToArray());

                foreach (var c in compilableContexts) // Flag compiled contexts
                    c.MarkAsCompiled(true);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Collecting attributes", 1 / nbSteps);
                foreach (var data in compilableData)
                    data.CollectAttributes();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Process dependencies", 2 / nbSteps);
                foreach (var data in compilableData)
                    data.ProcessDependencies();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Compiling expression Graph", 3 / nbSteps);
                m_ExpressionGraph = new VFXExpressionGraph();
                var exposedExpressionContext = ScriptableObject.CreateInstance<VFXImplicitContextOfExposedExpression>();
                exposedExpressionContext.FillExpression(m_Graph); //Force all exposed expression to be visible, only for registering in CompileExpressions

                var expressionContextOptions = compilationMode == VFXCompilationMode.Runtime ? VFXExpressionContextOption.ConstantFolding : VFXExpressionContextOption.Reduction;
                m_ExpressionGraph.CompileExpressions(compilableContexts.Concat(new VFXContext[] { exposedExpressionContext }), expressionContextOptions);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating bytecode", 4 / nbSteps);
                var expressionDescs = new List<VFXExpressionDesc>();
                var valueDescs = new List<VFXExpressionValueContainerDesc>();
                FillExpressionDescs(expressionDescs, valueDescs, m_ExpressionGraph);

                Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData = new Dictionary<VFXContext, VFXContextCompiledData>();
                foreach (var context in compilableContexts)
                    contextToCompiledData.Add(context, new VFXContextCompiledData());

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating mappings", 5 / nbSteps);
                foreach (var context in compilableContexts)
                {
                    var cpuMapper = m_ExpressionGraph.BuildCPUMapper(context);
                    var contextData = contextToCompiledData[context];
                    contextData.cpuMapper = cpuMapper;
                    contextData.parameters = context.additionalMappings.ToArray();
                    contextToCompiledData[context] = contextData;
                }

                var exposedParameterDescs = new List<VFXMapping>();
                FillExposedDescs(exposedParameterDescs, m_ExpressionGraph, m_Graph.children.OfType<VFXParameter>());
                var globalEventAttributeDescs = new List<VFXLayoutElementDesc>() { new VFXLayoutElementDesc() { name = "spawnCount", type = VFXValueType.Float } };
                FillEventAttributeDescs(globalEventAttributeDescs, m_ExpressionGraph, compilableContexts);

                SubgraphInfos subgraphInfos;
                subgraphInfos.subgraphParents = new Dictionary<VFXSubgraphContext, VFXSubgraphContext>();

                subgraphInfos.subgraphs = new List<VFXSubgraphContext>();

                foreach (var subgraph in m_Graph.children.OfType<VFXSubgraphContext>().Where(t => t.subgraph != null))
                {
                    subgraphInfos.subgraphs.Add(subgraph);
                    RecursePutSubgraphParent(subgraphInfos.subgraphParents, subgraphInfos.subgraphs, subgraph);
                }

                subgraphInfos.spawnerSubgraph = new Dictionary<VFXContext, VFXSubgraphContext>();

                foreach (var subgraph in subgraphInfos.subgraphs)
                {
                    foreach (var spawner in subgraph.subChildren.OfType<VFXContext>())
                        subgraphInfos.spawnerSubgraph.Add(spawner, subgraph);
                }

                subgraphInfos.contextEffectiveInputLinks = new Dictionary<VFXContext, List<VFXContextLink>[]>();

                ComputeEffectiveInputLinks(ref subgraphInfos, compilableContexts.OfType<VFXBasicInitialize>());


                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating Attribute layouts", 6 / nbSteps);
                foreach (var data in compilableData)
                    data.GenerateAttributeLayout(subgraphInfos.contextEffectiveInputLinks);

                var generatedCodeData = new List<GeneratedCodeData>();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating shaders", 7 / nbSteps);
                GenerateShaders(generatedCodeData, m_ExpressionGraph, compilableContexts, contextToCompiledData, compilationMode, sourceDependencies);

                m_Graph.systemNames.Sync(m_Graph);
                EditorUtility.DisplayProgressBar(progressBarTitle, "Saving shaders", 8 / nbSteps);
                VFXShaderSourceDesc[] shaderSources = SaveShaderFiles(m_Graph.visualEffectResource, generatedCodeData, contextToCompiledData, m_Graph.systemNames);

                var bufferDescs = new List<VFXGPUBufferDesc>();
                var temporaryBufferDescs = new List<VFXTemporaryGPUBufferDesc>();
                var cpuBufferDescs = new List<VFXCPUBufferDesc>();
                var systemDescs = new List<VFXEditorSystemDesc>();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating systems", 9 / nbSteps);
                cpuBufferDescs.Add(new VFXCPUBufferDesc()
                {
                    capacity = 1u,
                    layout = globalEventAttributeDescs.ToArray(),
                    stride = globalEventAttributeDescs.First().offset.structure,
                    initialData = ComputeArrayOfStructureInitialData(globalEventAttributeDescs)
                });

                var contextSpawnToSpawnInfo = new Dictionary<VFXContext, SpawnInfo>();
                FillSpawner(contextSpawnToSpawnInfo, cpuBufferDescs, systemDescs, compilableContexts, m_ExpressionGraph, globalEventAttributeDescs, contextToCompiledData, ref subgraphInfos, m_Graph.systemNames);

                var eventDescs = new List<VFXEventDesc>();
                FillEvent(eventDescs, contextSpawnToSpawnInfo, compilableContexts, ref subgraphInfos);

                var dependentBuffersData = new VFXDependentBuffersData();
                FillDependentBuffer(compilableData, bufferDescs, dependentBuffersData);

                var contextSpawnToBufferIndex = contextSpawnToSpawnInfo.Select(o => new { o.Key, o.Value.bufferIndex }).ToDictionary(o => o.Key, o => o.bufferIndex);
                foreach (var data in compilableData)
                {
                    data.FillDescs(bufferDescs,
                        temporaryBufferDescs,
                        systemDescs,
                        m_ExpressionGraph,
                        contextToCompiledData,
                        contextSpawnToBufferIndex,
                        dependentBuffersData,
                        subgraphInfos.contextEffectiveInputLinks,
                        m_Graph.systemNames);
                }

                // Update transient renderer settings
                ShadowCastingMode shadowCastingMode = compilableContexts.OfType<IVFXSubRenderer>().Any(r => r.hasShadowCasting) ? ShadowCastingMode.On : ShadowCastingMode.Off;
                MotionVectorGenerationMode motionVectorGenerationMode = compilableContexts.OfType<IVFXSubRenderer>().Any(r => r.hasMotionVector) ? MotionVectorGenerationMode.Object : MotionVectorGenerationMode.Camera;

                EditorUtility.DisplayProgressBar(progressBarTitle, "Setting up systems", 10 / nbSteps);
                var expressionSheet = new VFXExpressionSheet();
                expressionSheet.expressions = expressionDescs.ToArray();
                expressionSheet.values = valueDescs.OrderBy(o => o.expressionIndex).ToArray();
                expressionSheet.exposed = exposedParameterDescs.OrderBy(o => o.name).ToArray();


                resource.SetRuntimeData(expressionSheet, systemDescs.ToArray(), eventDescs.ToArray(), bufferDescs.ToArray(), cpuBufferDescs.ToArray(), temporaryBufferDescs.ToArray(), shaderSources, shadowCastingMode, motionVectorGenerationMode, compiledVersion);
                m_ExpressionValues = expressionSheet.values;

                foreach (var dep in sourceDependencies)
                    resource.AddSourceDependency(dep);

                if (k_FnVFXResource_SetCompileInitialVariants != null)
                {
                    k_FnVFXResource_SetCompileInitialVariants(m_Graph.visualEffectResource, forceShaderValidation);
                }
            }
            catch (Exception e)
            {
                VisualEffectAsset asset = null;

                if (m_Graph.visualEffectResource != null)
                    asset = m_Graph.visualEffectResource.asset;

                Debug.LogError(string.Format("{2} : Exception while compiling expression graph: {0}: {1}", e, e.StackTrace, (asset != null) ? asset.name : "(Null Asset)"), asset);

                // Cleaning
                if (m_Graph.visualEffectResource != null)
                    m_Graph.visualEffectResource.ClearRuntimeData();

                m_ExpressionGraph = new VFXExpressionGraph();
                m_ExpressionValues = new VFXExpressionValueContainerDesc[] {};
            }
            finally
            {
                Profiler.EndSample();
                EditorUtility.ClearProgressBar();
            }

            m_Graph.onRuntimeDataChanged?.Invoke(m_Graph);
        }

        public void UpdateValues()
        {
            var flatGraph = m_ExpressionGraph.FlattenedExpressions;
            var numFlattenedExpressions = flatGraph.Count;

            int descIndex = 0;
            for (int i = 0; i < numFlattenedExpressions; ++i)
            {
                var exp = flatGraph[i];
                if (exp.Is(VFXExpression.Flags.Value))
                {
                    var desc = m_ExpressionValues[descIndex++];
                    if (desc.expressionIndex != i)
                        throw new InvalidOperationException();

                    switch (exp.valueType)
                    {
                        case VFXValueType.Float: SetValueDesc<float>(desc, exp); break;
                        case VFXValueType.Float2: SetValueDesc<Vector2>(desc, exp); break;
                        case VFXValueType.Float3: SetValueDesc<Vector3>(desc, exp); break;
                        case VFXValueType.Float4: SetValueDesc<Vector4>(desc, exp); break;
                        case VFXValueType.Int32: SetValueDesc<int>(desc, exp); break;
                        case VFXValueType.Uint32: SetValueDesc<uint>(desc, exp); break;
                        case VFXValueType.Texture2D:
                        case VFXValueType.Texture2DArray:
                        case VFXValueType.Texture3D:
                        case VFXValueType.TextureCube:
                        case VFXValueType.TextureCubeArray:
                            SetObjectValueDesc<Texture>(desc, exp);
                            break;
                        case VFXValueType.Matrix4x4: SetValueDesc<Matrix4x4>(desc, exp); break;
                        case VFXValueType.Curve: SetValueDesc<AnimationCurve>(desc, exp); break;
                        case VFXValueType.ColorGradient: SetValueDesc<Gradient>(desc, exp); break;
                        case VFXValueType.Mesh: SetObjectValueDesc<Mesh>(desc, exp); break;
                        case VFXValueType.Boolean: SetValueDesc<bool>(desc, exp); break;
                        default: throw new InvalidOperationException("Invalid type");
                    }
                }
            }

            m_Graph.visualEffectResource.SetValueSheet(m_ExpressionValues);
        }

        public VisualEffectResource visualEffectResource
        {
            get
            {
                if (m_Graph != null)
                {
                    return m_Graph.visualEffectResource;
                }
                return null;
            }
        }

        private VFXGraph m_Graph;

        [NonSerialized]
        private VFXExpressionGraph m_ExpressionGraph;
        [NonSerialized]
        private VFXExpressionValueContainerDesc[] m_ExpressionValues;
    }
}
