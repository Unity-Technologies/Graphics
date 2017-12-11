using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    struct VFXContextCompiledData
    {
        public VFXExpressionMapper cpuMapper;
        public VFXExpressionMapper gpuMapper;
        public VFXUniformMapper uniformMapper;
        public VFXMapping[] parameters;
        public Object processor;
    }

    class VFXGraphCompiledData
    {
        public VFXGraphCompiledData(VFXGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException("VFXGraph cannot be null");
            m_Graph = graph;
        }

        static public string baseCacheFolder
        {
            get
            {
                return "Assets/VFXCache";
            }
        }

        private struct GeneratedCodeData
        {
            public VFXContext context;
            public bool computeShader;
            public System.Text.StringBuilder content;
            public VFXCodeGenerator.CompilationMode compilMode;
        }

        private static VFXExpressionValueContainerDesc<T> CreateValueDesc<T>(VFXExpression exp, int expIndex)
        {
            var desc = new VFXExpressionValueContainerDesc<T>();
            desc.value = exp.Get<T>();
            return desc;
        }

        private void SetValueDesc<T>(VFXExpressionValueContainerDescAbstract desc, VFXExpression exp)
        {
            ((VFXExpressionValueContainerDesc<T>)desc).value = exp.Get<T>();
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

        private static void FillExpressionDescs(List<VFXExpressionDesc> outExpressionDescs, List<VFXExpressionValueContainerDescAbstract> outValueDescs, VFXExpressionGraph graph)
        {
            var flatGraph = graph.FlattenedExpressions;
            var numFlattenedExpressions = flatGraph.Count;

            for (int i = 0; i < numFlattenedExpressions; ++i)
            {
                var exp = flatGraph[i];

                // Must match data in C++ expression
                if (exp.Is(VFXExpression.Flags.Value))
                {
                    VFXExpressionValueContainerDescAbstract value;
                    switch (exp.valueType)
                    {
                        case VFXValueType.kFloat: value = CreateValueDesc<float>(exp, i); break;
                        case VFXValueType.kFloat2: value = CreateValueDesc<Vector2>(exp, i); break;
                        case VFXValueType.kFloat3: value = CreateValueDesc<Vector3>(exp, i); break;
                        case VFXValueType.kFloat4: value = CreateValueDesc<Vector4>(exp, i); break;
                        case VFXValueType.kInt: value = CreateValueDesc<int>(exp, i); break;
                        case VFXValueType.kUint: value = CreateValueDesc<uint>(exp, i); break;
                        case VFXValueType.kTexture2D: value = CreateValueDesc<Texture2D>(exp, i); break;
                        case VFXValueType.kTexture3D: value = CreateValueDesc<Texture3D>(exp, i); break;
                        case VFXValueType.kTransform: value = CreateValueDesc<Matrix4x4>(exp, i); break;
                        case VFXValueType.kCurve: value = CreateValueDesc<AnimationCurve>(exp, i); break;
                        case VFXValueType.kColorGradient: value = CreateValueDesc<Gradient>(exp, i); break;
                        case VFXValueType.kMesh: value = CreateValueDesc<Mesh>(exp, i); break;
                        case VFXValueType.kBool: value = CreateValueDesc<bool>(exp, i); break;
                        default: throw new InvalidOperationException("Invalid type");
                    }
                    value.expressionIndex = (uint)i;
                    outValueDescs.Add(value);
                }

                outExpressionDescs.Add(new VFXExpressionDesc
                {
                    op = exp.operation,
                    data = exp.GetOperands(graph),
                });
            }
        }

        private static void FillExposedDescs(List<VFXExposedDesc> outExposedParameters, VFXExpressionGraph graph, IEnumerable<VFXParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.exposed)
                {
                    var outputSlotExpr = parameter.GetOutputSlot(0).GetExpression();
                    if (outputSlotExpr != null)
                    {
                        outExposedParameters.Add(new VFXExposedDesc()
                        {
                            name = parameter.exposedName,
                            expressionIndex = (uint)graph.GetFlattenedIndex(outputSlotExpr)
                        });
                    }
                }
            }
        }

        private static void FillEventAttributeDescs(List<VFXLayoutElementDesc> eventAttributeDescs, VFXExpressionGraph graph, IEnumerable<VFXContext> contexts)
        {
            foreach (var context in contexts.Where(o => o.contextType == VFXContextType.kSpawner))
            {
                foreach (var linked in context.outputContexts)
                {
                    foreach (var attribute in linked.GetData().GetAttributes())
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

        private static List<VFXContext> CollectContextParentRecursively(List<VFXContext> inputList)
        {
            var contextList = inputList.SelectMany(o => o.inputContexts).Distinct().ToList();
            if (contextList.Any(o => o.inputContexts.Any()))
            {
                var parentContextList = CollectContextParentRecursively(contextList);
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

        private static VFXContext[] CollectSpawnersHierarchy(IEnumerable<VFXContext> vfxContext)
        {
            var initContext = vfxContext.Where(o => o.contextType == VFXContextType.kInit).ToList();
            var spawnerList = CollectContextParentRecursively(initContext);
            return spawnerList.Where(o => o.contextType == VFXContextType.kSpawner).Reverse().ToArray();
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
                if (element.type == VFXValueType.kBool)
                {
                    var v = useAttribute ? attribute.value.Get<bool>() : default(bool);
                    data.PushBool(v);
                }
                else if (element.type == VFXValueType.kFloat)
                {
                    var v = useAttribute ? attribute.value.Get<float>() : default(float);
                    data.PushFloat(v);
                }
                else if (element.type == VFXValueType.kFloat2)
                {
                    var v = useAttribute ? attribute.value.Get<Vector2>() : default(Vector2);
                    data.PushFloat(v.x);
                    data.PushFloat(v.y);
                }
                else if (element.type == VFXValueType.kFloat3)
                {
                    var v = useAttribute ? attribute.value.Get<Vector3>() : default(Vector3);
                    data.PushFloat(v.x);
                    data.PushFloat(v.y);
                    data.PushFloat(v.z);
                }
                else if (element.type == VFXValueType.kFloat4)
                {
                    var v = useAttribute ? attribute.value.Get<Vector4>() : default(Vector4);
                    data.PushFloat(v.x);
                    data.PushFloat(v.y);
                    data.PushFloat(v.z);
                    data.PushFloat(v.w);
                }
                else if (element.type == VFXValueType.kInt)
                {
                    var v = useAttribute ? attribute.value.Get<int>() : default(int);
                    data.PushInt(v);
                }
                else if (element.type == VFXValueType.kUint)
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

        private static void FillSpawner(Dictionary<VFXContext, SpawnInfo> outContextSpawnToSpawnInfo, List<VFXCPUBufferDesc> outCpuBufferDescs, List<VFXSystemDesc> outSystemDescs, IEnumerable<VFXContext> contexts, VFXExpressionGraph graph, List<VFXLayoutElementDesc> globalEventAttributeDescs, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            var spawners = CollectSpawnersHierarchy(contexts);
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
                var buffers = new VFXMapping[]
                {
                    new VFXMapping()
                    {
                        index = outContextSpawnToSpawnInfo[spawnContext].bufferIndex,
                        name = "spawner_output"
                    }
                };

                var contextData = contextToCompiledData[spawnContext];
                outSystemDescs.Add(new VFXSystemDesc()
                {
                    buffers = buffers,
                    capacity = 0u,
                    flags = VFXSystemFlag.kVFXSystemDefault,
                    tasks = spawnContext.activeChildrenWithImplicit.Select((b, index) =>
                        {
                            var spawnerBlock = b as VFXAbstractSpawner;
                            if (spawnerBlock == null)
                            {
                                throw new InvalidCastException("Unexpected block type in spawnerContext");
                            }
                            if (spawnerBlock.spawnerType == VFXTaskType.kSpawnerCustomCallback && spawnerBlock.customBehavior == null)
                            {
                                throw new InvalidOperationException("VFXAbstractSpawner excepts a custom behavior for custom callback type");
                            }
                            if (spawnerBlock.spawnerType != VFXTaskType.kSpawnerCustomCallback && spawnerBlock.customBehavior != null)
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
                                    throw new InvalidOperationException("Unable to retrieve ScriptatbleObject for " + spawnerBlock.customBehavior);
                                }

                                var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                                processor = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                            }

                            return new VFXTaskDesc
                            {
                                type = spawnerBlock.spawnerType,
                                buffers = new VFXMapping[0],
                                values = cpuExpression.ToArray(),
                                parameters = contextData.parameters,
                                processor = processor,
                            };
                        }).ToArray()
                });
            }
        }

        private static void FillEvent(List<VFXEventDesc> outEventDesc, Dictionary<VFXContext, SpawnInfo> contextSpawnToSpawnInfo, IEnumerable<VFXContext> contexts)
        {
            var allStartNotLinked = contextSpawnToSpawnInfo.Where(o => !o.Key.inputFlowSlot[0].link.Any()).Select(o => (uint)o.Value.systemIndex).ToList();
            var allStopNotLinked = contextSpawnToSpawnInfo.Where(o => !o.Key.inputFlowSlot[1].link.Any()).Select(o => (uint)o.Value.systemIndex).ToList();

            var eventDescTemp = new[]
            {
                new { eventName = "OnStart", startSystems = allStartNotLinked, stopSystems = new List<uint>() },
                new { eventName = "OnStop", startSystems = new List<uint>(), stopSystems = allStopNotLinked },
            }.ToList();

            var events = contexts.Where(o => o.contextType == VFXContextType.kEvent);
            foreach (var evt in events)
            {
                var eventName = (evt as VFXBasicEvent).eventName;
                foreach (var link in evt.outputFlowSlot[0].link)
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
                                startSystems = new List<uint>(),
                                stopSystems = new List<uint>(),
                            });
                        }

                        var startSystem = link.slotIndex == 0;
                        var spawnerIndex = (uint)contextSpawnToSpawnInfo[link.context].systemIndex;
                        if (startSystem)
                        {
                            eventDescTemp[eventIndex].startSystems.Add(spawnerIndex);
                        }
                        else
                        {
                            eventDescTemp[eventIndex].stopSystems.Add(spawnerIndex);
                        }
                    }
                }
            }
            outEventDesc.Clear();
            outEventDesc.AddRange(eventDescTemp.Select(o => new VFXEventDesc() { name = o.eventName, startSystems = o.startSystems.ToArray(), stopSystems = o.stopSystems.ToArray() }));
        }

        private static void GenerateShaders(List<GeneratedCodeData> outGeneratedCodeData, VFXExpressionGraph graph, IEnumerable<VFXContext> contexts, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            Profiler.BeginSample("VFXEditor.GenerateShaders");
            try
            {
                var compilMode = new[] { /* VFXCodeGenerator.CompilationMode.Debug,*/ VFXCodeGenerator.CompilationMode.Runtime };

                foreach (var context in contexts.Where(model => model.contextType != VFXContextType.kSpawner))
                {
                    var codeGeneratorTemplate = context.codeGeneratorTemplate;
                    if (codeGeneratorTemplate != null)
                    {
                        var generatedContent = compilMode.Select(o => new StringBuilder()).ToArray();

                        var gpuMapper = graph.BuildGPUMapper(context);
                        var uniformMapper = new VFXUniformMapper(gpuMapper);

                        // Add gpu and uniform mapper
                        var contextData = contextToCompiledData[context];
                        contextData.gpuMapper = gpuMapper;
                        contextData.uniformMapper = uniformMapper;
                        contextToCompiledData[context] = contextData;

                        VFXCodeGenerator.Build(context, compilMode, generatedContent, contextData, codeGeneratorTemplate);

                        for (int i = 0; i < compilMode.Length; ++i)
                        {
                            outGeneratedCodeData.Add(new GeneratedCodeData()
                            {
                                context = context,
                                computeShader = context.codeGeneratorCompute,
                                compilMode = compilMode[i],
                                content = generatedContent[i]
                            });
                        }
                    }
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private static void SaveShaderFiles(VFXAsset asset, List<GeneratedCodeData> generatedCodeData, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            Profiler.BeginSample("VFXEditor.SaveShaderFiles");
            try
            {
                var currentCacheFolder = baseCacheFolder;
                if (asset != null)
                {
                    var path = AssetDatabase.GetAssetPath(asset);
                    path = path.Replace("Assets", "");
                    path = path.Replace(".asset", "");
                    currentCacheFolder += path;
                }

                System.IO.Directory.CreateDirectory(currentCacheFolder);
                for (int i = 0; i < generatedCodeData.Count; ++i)
                {
                    var generated = generatedCodeData[i];
                    var path = string.Format("{0}/Temp_{2}_{1}_{3}_{4}.{2}", currentCacheFolder, VFXCodeGeneratorHelper.GeneratePrefix((uint)i), generated.computeShader ? "compute" : "shader", generated.context.name.ToLower(), generated.compilMode);

                    string oldContent = "";
                    if (System.IO.File.Exists(path))
                    {
                        oldContent = System.IO.File.ReadAllText(path);
                    }
                    var newContent = generated.content.ToString();
                    bool hasChanged = oldContent != newContent;
                    if (hasChanged)
                    {
                        System.IO.File.WriteAllText(path, newContent);
                        Profiler.BeginSample("VFXEditor.SaveShaderFiles.ImportAsset");
                        AssetDatabase.ImportAsset(path);
                        Profiler.EndSample();
                    }

                    Object imported = AssetDatabase.LoadAssetAtPath<Object>(path);
                    var contextData = contextToCompiledData[generated.context];
                    contextData.processor = imported;
                    contextToCompiledData[generated.context] = contextData;
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        public void Compile()
        {
            Profiler.BeginSample("VFXEditor.CompileAsset");
            try
            {
                float nbSteps = 10.0f;
                string progressBarTitle = "Compiling VFX...";

                EditorUtility.DisplayProgressBar(progressBarTitle, "Collect dependencies", 0 / nbSteps);
                var models = new HashSet<Object>();
                m_Graph.CollectDependencies(models);
                var compilableContexts = models.OfType<VFXContext>().Where(c => c.CanBeCompiled());
                var compilableData = models.OfType<VFXData>().Where(d => d.CanBeCompiled());

                EditorUtility.DisplayProgressBar(progressBarTitle, "Collect attributes", 1 / nbSteps);
                foreach (var data in compilableData)
                    data.CollectAttributes();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Compute layers", 2 / nbSteps);
                foreach (var data in compilableData)
                    data.ComputeLayer();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Compile expression Graph", 3 / nbSteps);
                m_ExpressionGraph = new VFXExpressionGraph();
                m_ExpressionGraph.CompileExpressions(m_Graph, VFXExpressionContextOption.Reduction, true);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate bytecode", 4 / nbSteps);
                var expressionDescs = new List<VFXExpressionDesc>();
                var valueDescs = new List<VFXExpressionValueContainerDescAbstract>();
                FillExpressionDescs(expressionDescs, valueDescs, m_ExpressionGraph);

                Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData = new Dictionary<VFXContext, VFXContextCompiledData>();
                foreach (var context in compilableContexts)
                    contextToCompiledData.Add(context, new VFXContextCompiledData());

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate mappings", 5 / nbSteps);
                foreach (var context in compilableContexts)
                {
                    uint contextId = (uint)context.GetParent().GetIndex(context);
                    var cpuMapper = m_ExpressionGraph.BuildCPUMapper(context);
                    var contextData = contextToCompiledData[context];
                    contextData.cpuMapper = cpuMapper;
                    contextData.parameters = context.additionalMappings.ToArray();
                    contextToCompiledData[context] = contextData;
                }

                var exposedParameterDescs = new List<VFXExposedDesc>();
                FillExposedDescs(exposedParameterDescs, m_ExpressionGraph, models.OfType<VFXParameter>());
                var globalEventAttributeDescs = new List<VFXLayoutElementDesc>() { new VFXLayoutElementDesc() { name = "spawnCount", type = VFXValueType.kFloat } };
                FillEventAttributeDescs(globalEventAttributeDescs, m_ExpressionGraph, compilableContexts);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate Attribute layouts", 6 / nbSteps);
                foreach (var data in compilableData)
                    data.GenerateAttributeLayout();

                var expressionSheet = new VFXExpressionSheet();
                expressionSheet.expressions = expressionDescs.ToArray();
                expressionSheet.values = valueDescs.ToArray();
                expressionSheet.exposed = exposedParameterDescs.ToArray();

                m_Graph.vfxAsset.ClearPropertyData();
                m_Graph.vfxAsset.SetExpressionSheet(expressionSheet);

                var generatedCodeData = new List<GeneratedCodeData>();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate shaders", 7 / nbSteps);
                GenerateShaders(generatedCodeData, m_ExpressionGraph, compilableContexts, contextToCompiledData);
                EditorUtility.DisplayProgressBar(progressBarTitle, "Write shader files", 8 / nbSteps);
                SaveShaderFiles(m_Graph.vfxAsset, generatedCodeData, contextToCompiledData);

                var bufferDescs = new List<VFXGPUBufferDesc>();
                var cpuBufferDescs = new List<VFXCPUBufferDesc>();
                var systemDescs = new List<VFXSystemDesc>();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate native systems", 9 / nbSteps);
                cpuBufferDescs.Add(new VFXCPUBufferDesc()
                {
                    capacity = 1u,
                    layout = globalEventAttributeDescs.ToArray(),
                    stride = globalEventAttributeDescs.First().offset.structure,
                    initialData = ComputeArrayOfStructureInitialData(globalEventAttributeDescs)
                });
                var contextSpawnToSpawnInfo = new Dictionary<VFXContext, SpawnInfo>();
                FillSpawner(contextSpawnToSpawnInfo, cpuBufferDescs, systemDescs, compilableContexts, m_ExpressionGraph, globalEventAttributeDescs, contextToCompiledData);

                var eventDescs = new List<VFXEventDesc>();
                FillEvent(eventDescs, contextSpawnToSpawnInfo, compilableContexts);

                //Compute all eventGPU desc
                /* WIP : Begin */
                //Prepare all attribute buffer
                var attributeBufferDictionnary = new Dictionary<VFXData, int>();
                foreach (var data in compilableData.OfType<VFXDataParticle>())
                {
                    int attributeBufferIndex = -1;
                    if (data.attributeBufferSize > 0)
                    {
                        attributeBufferIndex = bufferDescs.Count;
                        bufferDescs.Add(data.m_layoutAttributeCurrent.GetBufferDesc(data.capacity));
                    }
                    attributeBufferDictionnary.Add(data, attributeBufferIndex);
                }

                //Prepare GPU event buffer (out buffer)
                var gpuEventBufferDictionnary = new Dictionary<VFXData, int>();

                //simplify this ! TODOPAUL
                foreach (var dataParticle in compilableData.Where(o => o.layer != uint.MaxValue).SelectMany(o => o.dependenciesOut).Distinct().OfType<VFXDataParticle>())
                {
                    var index = bufferDescs.Count;
                    bufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Append, size = dataParticle.capacity });
                    gpuEventBufferDictionnary.Add(dataParticle, index);
                }

                var contextSpawnToBufferIndex = contextSpawnToSpawnInfo.Select(o => new { o.Key, o.Value.bufferIndex }).ToDictionary(o => o.Key, o => o.bufferIndex);
                foreach (var data in compilableData.OfType<VFXDataParticle>())
                {
                    data.FillDescs(bufferDescs,
                        systemDescs,
                        m_ExpressionGraph,
                        contextToCompiledData,
                        contextSpawnToBufferIndex,
                        attributeBufferDictionnary,
                        gpuEventBufferDictionnary);
                }
                /* WIP : End */

                EditorUtility.DisplayProgressBar(progressBarTitle, "Setting up systems", 9 / nbSteps);
                m_Graph.vfxAsset.SetSystems(systemDescs.ToArray(), eventDescs.ToArray(), bufferDescs.ToArray(), cpuBufferDescs.ToArray());
                m_ExpressionValues = valueDescs;
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Exception while compiling expression graph: {0}: {1}", e, e.StackTrace));

                // Cleaning
                if (m_Graph.vfxAsset != null)
                {
                    m_Graph.vfxAsset.ClearPropertyData();
                    m_Graph.vfxAsset.SetSystems(null, null, null, null);
                }

                m_ExpressionGraph = new VFXExpressionGraph();
                m_ExpressionValues = new List<VFXExpressionValueContainerDescAbstract>();
            }
            finally
            {
                Profiler.EndSample();
                EditorUtility.ClearProgressBar();
            }
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
                        case VFXValueType.kFloat: SetValueDesc<float>(desc, exp); break;
                        case VFXValueType.kFloat2: SetValueDesc<Vector2>(desc, exp); break;
                        case VFXValueType.kFloat3: SetValueDesc<Vector3>(desc, exp); break;
                        case VFXValueType.kFloat4: SetValueDesc<Vector4>(desc, exp); break;
                        case VFXValueType.kInt: SetValueDesc<int>(desc, exp); break;
                        case VFXValueType.kUint: SetValueDesc<uint>(desc, exp); break;
                        case VFXValueType.kTexture2D: SetValueDesc<Texture2D>(desc, exp); break;
                        case VFXValueType.kTexture3D: SetValueDesc<Texture3D>(desc, exp); break;
                        case VFXValueType.kTransform: SetValueDesc<Matrix4x4>(desc, exp); break;
                        case VFXValueType.kCurve: SetValueDesc<AnimationCurve>(desc, exp); break;
                        case VFXValueType.kColorGradient: SetValueDesc<Gradient>(desc, exp); break;
                        case VFXValueType.kMesh: SetValueDesc<Mesh>(desc, exp); break;
                        case VFXValueType.kBool: SetValueDesc<bool>(desc, exp); break;
                        default: throw new InvalidOperationException("Invalid type");
                    }
                }
            }

            m_Graph.vfxAsset.SetValueSheet(m_ExpressionValues.ToArray());
        }

        public VFXAsset vfxAsset
        {
            get
            {
                if (m_Graph != null)
                {
                    return m_Graph.vfxAsset;
                }
                return null;
            }
        }

        private VFXGraph m_Graph;

        [NonSerialized]
        private VFXExpressionGraph m_ExpressionGraph;
        [NonSerialized]
        private List<VFXExpressionValueContainerDescAbstract> m_ExpressionValues;
        //[NonSerialized]
        //private Dictionary<VFXContext, VFXContextCompiledData> m_ContextToCompiledData;
    }
}
