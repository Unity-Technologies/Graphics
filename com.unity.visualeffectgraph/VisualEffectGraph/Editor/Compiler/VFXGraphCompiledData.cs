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

        private static void FillSemanticsDescs(List<VFXExpressionSemanticDesc> outExpressionSementics, VFXExpressionGraph graph, HashSet<Object> models, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            foreach (var context in models.OfType<VFXContext>().Where(c => c.CanBeCompiled()))
            {
                uint contextId = (uint)context.GetParent().GetIndex(context);
                var cpuMapper = graph.BuildCPUMapper(context);

                // Add cpu mapper
                var contextData = contextToCompiledData[context];
                contextData.cpuMapper = cpuMapper;
                contextToCompiledData[context] = contextData;

                foreach (var exp in cpuMapper.expressions)
                {
                    VFXExpressionSemanticDesc desc;
                    var mappedDataList = cpuMapper.GetData(exp);
                    foreach (var mappedData in mappedDataList)
                    {
                        desc.blockID = (uint)mappedData.id;
                        desc.contextID = contextId;
                        int expIndex = graph.GetFlattenedIndex(exp);
                        if (expIndex == -1)
                            throw new Exception(string.Format("Cannot find mapped expression {0} in flattened graph", mappedData.name));
                        desc.expressionIndex = (uint)expIndex;
                        desc.name = mappedData.name;
                        outExpressionSementics.Add(desc);
                    }
                }
            }
        }

        private static void FillExposedDescs(List<VFXExposedDesc> outExposedParameters, VFXExpressionGraph graph, HashSet<Object> models)
        {
            foreach (var parameter in models.OfType<VFXParameter>())
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

        private static void FillEventAttributeDescs(List<VFXLayoutElementDesc> eventAttributeDescs, VFXExpressionGraph graph, HashSet<Object> models)
        {
            foreach (var context in models.OfType<VFXContext>().Where(o => o.contextType == VFXContextType.kSpawner))
            {
                foreach (var linked in context.outputContexts)
                {
                    foreach (var attribute in linked.GetData().GetAttributes())
                    {
                        if ((attribute.mode & VFXAttributeMode.ReadSource) != 0)
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
            var initContext = vfxContext.Where(o => o.contextType == VFXContextType.kInit && o.CanBeCompiled()).ToList();
            var spawnerList = CollectContextParentRecursively(initContext);
            return spawnerList.Where(o => o.contextType == VFXContextType.kSpawner).Reverse().ToArray();
        }

        struct SpawnInfo
        {
            public int bufferIndex;
            public int systemIndex;
        }

        private static void FillSpawner(Dictionary<VFXContext, SpawnInfo> outContextSpawnToSpawnInfo, List<VFXCPUBufferDesc> outCpuBufferDescs, List<VFXSystemDesc> outSystemDescs, HashSet<Object> models, VFXExpressionGraph graph, List<VFXLayoutElementDesc> eventAttributeDescs, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            var spawners = CollectSpawnersHierarchy(models.OfType<VFXContext>());
            foreach (var it in spawners.Select((spawner, index) => new { spawner, index }))
            {
                outContextSpawnToSpawnInfo.Add(it.spawner, new SpawnInfo() { bufferIndex = outCpuBufferDescs.Count, systemIndex = it.index });
                outCpuBufferDescs.Add(new VFXCPUBufferDesc()
                {
                    capacity = 1,
                    layout = eventAttributeDescs.ToArray()
                });
            }
            foreach (var spawnContext in spawners)
            {
                var buffers = new VFXBufferMapping[]
                {
                    new VFXBufferMapping()
                    {
                        bufferIndex = outContextSpawnToSpawnInfo[spawnContext].bufferIndex,
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
                                return new VFXValueMapping
                                {
                                    expressionIndex = graph.GetFlattenedIndex(o.exp),
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
                                buffers = Enumerable.Empty<VFXBufferMapping>().ToArray(),
                                processor = processor,
                                values = cpuExpression.ToArray()
                            };
                        }).ToArray()
                });
            }
        }

        private static void FillEvent(List<VFXEventDesc> outEventDesc, Dictionary<VFXContext, SpawnInfo> contextSpawnToSpawnInfo, HashSet<Object> models)
        {
            var allStartNotLinked = contextSpawnToSpawnInfo.Where(o => !o.Key.inputFlowSlot[0].link.Any()).Select(o => (uint)o.Value.systemIndex).ToList();
            var allStopNotLinked = contextSpawnToSpawnInfo.Where(o => !o.Key.inputFlowSlot[1].link.Any()).Select(o => (uint)o.Value.systemIndex).ToList();

            var eventDescTemp = new[]
            {
                new { eventName = "OnStart", startSystems = allStartNotLinked, stopSystems = new List<uint>() },
                new { eventName = "OnStop", startSystems = new List<uint>(), stopSystems = allStopNotLinked },
            }.ToList();

            var events = models.OfType<VFXContext>().Where(o => o.contextType == VFXContextType.kEvent);
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

        private static void GenerateShaders(List<GeneratedCodeData> outGeneratedCodeData, VFXExpressionGraph graph, HashSet<Object> models, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            Profiler.BeginSample("VFXEditor.GenerateShaders");
            try
            {
                var compilMode = new[] { /* VFXCodeGenerator.CompilationMode.Debug,*/ VFXCodeGenerator.CompilationMode.Runtime };

                foreach (var context in models.OfType<VFXContext>().Where(model => model.CanBeCompiled() && model.contextType != VFXContextType.kSpawner))
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
                    }

                    AssetDatabase.ImportAsset(path);
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
                float nbSteps = 9.0f;
                string progressBarTitle = "Compiling VFX...";

                EditorUtility.DisplayProgressBar(progressBarTitle, "Collect dependencies", 0 / nbSteps);
                var models = new HashSet<Object>();
                m_Graph.CollectDependencies(models);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Collect attributes", 1 / nbSteps);
                foreach (var data in models.OfType<VFXData>().Where(d => d.CanBeCompiled()))
                    data.CollectAttributes();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Compile expression Graph", 2 / nbSteps);
                m_ExpressionGraph = new VFXExpressionGraph();
                m_ExpressionGraph.CompileExpressions(m_Graph, VFXExpressionContextOption.Reduction, true);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate bytecode", 3 / nbSteps);
                var expressionDescs = new List<VFXExpressionDesc>();
                var valueDescs = new List<VFXExpressionValueContainerDescAbstract>();
                FillExpressionDescs(expressionDescs, valueDescs, m_ExpressionGraph);

                Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData = new Dictionary<VFXContext, VFXContextCompiledData>();
                foreach (var context in models.OfType<VFXContext>().Where(c => c.CanBeCompiled()))
                    contextToCompiledData.Add(context, new VFXContextCompiledData());

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate mappings", 4 / nbSteps);
                var semanticsDescs = new List<VFXExpressionSemanticDesc>();
                FillSemanticsDescs(semanticsDescs, m_ExpressionGraph, models, contextToCompiledData);

                var exposedParameterDescs = new List<VFXExposedDesc>();
                FillExposedDescs(exposedParameterDescs, m_ExpressionGraph, models);

                var eventAttributeDescs = new List<VFXLayoutElementDesc>() { new VFXLayoutElementDesc() { name = "spawnCount", type = VFXValueType.kFloat } };
                FillEventAttributeDescs(eventAttributeDescs, m_ExpressionGraph, models);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate Attribute layouts", 5 / nbSteps);
                foreach (var data in models.OfType<VFXData>().Where(d => d.CanBeCompiled()))
                    data.GenerateAttributeLayout();

                var expressionSheet = new VFXExpressionSheet();
                expressionSheet.expressions = expressionDescs.ToArray();
                expressionSheet.values = valueDescs.ToArray();
                expressionSheet.semantics = semanticsDescs.ToArray();
                expressionSheet.exposed = exposedParameterDescs.ToArray();

                m_Graph.vfxAsset.ClearPropertyData();
                m_Graph.vfxAsset.SetExpressionSheet(expressionSheet);

                var generatedCodeData = new List<GeneratedCodeData>();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate shaders", 6 / nbSteps);
                GenerateShaders(generatedCodeData, m_ExpressionGraph, models, contextToCompiledData);
                EditorUtility.DisplayProgressBar(progressBarTitle, "Write shader files", 7 / nbSteps);
                SaveShaderFiles(m_Graph.vfxAsset, generatedCodeData, contextToCompiledData);

                var bufferDescs = new List<VFXBufferDesc>();
                var cpuBufferDescs = new List<VFXCPUBufferDesc>();
                var systemDescs = new List<VFXSystemDesc>();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generate native systems", 8 / nbSteps);

                var contextSpawnToSpawnInfo = new Dictionary<VFXContext, SpawnInfo>();
                FillSpawner(contextSpawnToSpawnInfo, cpuBufferDescs, systemDescs, models, m_ExpressionGraph, eventAttributeDescs, contextToCompiledData);

                var eventDescs = new List<VFXEventDesc>();
                FillEvent(eventDescs, contextSpawnToSpawnInfo, models);

                var contextSpawnToBufferIndex = contextSpawnToSpawnInfo.Select(o => new { o.Key, o.Value.bufferIndex }).ToDictionary(o => o.Key, o => o.bufferIndex);
                foreach (var data in models.OfType<VFXDataParticle>().Where(d => d.CanBeCompiled()))
                    data.FillDescs(bufferDescs, systemDescs, m_ExpressionGraph, contextToCompiledData, contextSpawnToBufferIndex);

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
