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
            foreach (var context in models.OfType<VFXContext>())
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
                        if (attribute.attrib.location == VFXAttributeLocation.Source)
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

        private static VFXContext[] CollectSpawnersHierarchy(IEnumerable<VFXContext> vfxContext)
        {
            var allSpawner = vfxContext.Where(o => o.contextType == VFXContextType.kSpawner);
            var spawnerToResolve = new HashSet<VFXContext>(vfxContext.Where(o => o.contextType == VFXContextType.kInit).SelectMany(o => o.inputContexts));

            var spawnerProcessed = new List<VFXContext>();
            while (spawnerToResolve.Count != 0)
            {
                var currentSpawnerToResolve = spawnerToResolve.ToArray();
                foreach (var spawner in currentSpawnerToResolve)
                {
                    var dependencyNeeded = spawner.inputContexts.Where(o => spawnerProcessed.Contains(o)).ToArray();
                    if (dependencyNeeded.Length != 0)
                    {
                        foreach (var dependency in dependencyNeeded)
                        {
                            spawnerToResolve.Add(dependency);
                        }
                    }
                    else
                    {
                        spawnerProcessed.Add(spawner);
                        spawnerToResolve.Remove(spawner);
                    }
                }
            }
            return spawnerProcessed.ToArray();
        }

        private static void GenerateShaders(List<GeneratedCodeData> outGeneratedCodeData, VFXExpressionGraph graph, HashSet<Object> models, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            var compilMode = new[] { /* VFXCodeGenerator.CompilationMode.Debug,*/ VFXCodeGenerator.CompilationMode.Runtime };

            foreach (var context in models.OfType<VFXContext>().Where(model => model.contextType != VFXContextType.kSpawner))
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

        private static void SaveShaderFiles(VFXAsset asset, List<GeneratedCodeData> generatedCodeData, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            //var generatedShader = new List<Object>();
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
                Object imported = null;
                if (generated.computeShader)
                {
                    imported = AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
                }
                else
                {
                    var importer = AssetImporter.GetAtPath(path) as ShaderImporter;
                    imported = importer.GetShader();
                }
                if (hasChanged)
                {
                    EditorUtility.SetDirty(imported);
                }
                //generatedShader.Add(imported);

                var contextData = contextToCompiledData[generated.context];
                contextData.processor = imported;
                contextToCompiledData[generated.context] = contextData;
            }
        }

        public void Compile()
        {
            try
            {
                var models = new HashSet<Object>();
                m_Graph.CollectDependencies(models);

                foreach (var data in models.OfType<VFXData>())
                    data.CollectAttributes();

                m_ExpressionGraph = new VFXExpressionGraph();
                m_ExpressionGraph.CompileExpressions(m_Graph, VFXExpressionContextOption.Reduction);

                var expressionDescs = new List<VFXExpressionDesc>();
                var valueDescs = new List<VFXExpressionValueContainerDescAbstract>();
                FillExpressionDescs(expressionDescs, valueDescs, m_ExpressionGraph);

                Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData = new Dictionary<VFXContext, VFXContextCompiledData>();
                foreach (var context in models.OfType<VFXContext>())
                    contextToCompiledData.Add(context, new VFXContextCompiledData());

                var semanticsDescs = new List<VFXExpressionSemanticDesc>();
                FillSemanticsDescs(semanticsDescs, m_ExpressionGraph, models, contextToCompiledData);

                var exposedParameterDescs = new List<VFXExposedDesc>();
                FillExposedDescs(exposedParameterDescs, m_ExpressionGraph, models);

                var eventAttributeDescs = new List<VFXLayoutElementDesc>() { new VFXLayoutElementDesc() { name = "spawnCount", type = VFXValueType.kFloat } };
                FillEventAttributeDescs(eventAttributeDescs, m_ExpressionGraph, models);

                foreach (var data in models.OfType<VFXData>())
                    data.GenerateAttributeLayout();

                var expressionSheet = new VFXExpressionSheet();
                expressionSheet.expressions = expressionDescs.ToArray();
                expressionSheet.values = valueDescs.ToArray();
                expressionSheet.semantics = semanticsDescs.ToArray();
                expressionSheet.exposed = exposedParameterDescs.ToArray();

                m_Graph.vfxAsset.ClearSpawnerData();
                m_Graph.vfxAsset.ClearPropertyData();
                m_Graph.vfxAsset.SetExpressionSheet(expressionSheet);

                var generatedCodeData = new List<GeneratedCodeData>();
                GenerateShaders(generatedCodeData, m_ExpressionGraph, models, contextToCompiledData);
                SaveShaderFiles(m_Graph.vfxAsset, generatedCodeData, contextToCompiledData);

                var bufferDescs = new List<VFXBufferDesc>();
                var cpuBufferDescs = new List<VFXCPUBufferDesc>();
                var systemDescs = new List<VFXSystemDesc>();

                /* Begin WIP Spawner */
                var spawners = CollectSpawnersHierarchy(models.OfType<VFXContext>());
                var spawnContextToBufferIndex = new Dictionary<VFXContext, int>();
                foreach (var spawnContext in spawners)
                {
                    spawnContextToBufferIndex.Add(spawnContext, cpuBufferDescs.Count);
                    cpuBufferDescs.Add(new VFXCPUBufferDesc()
                    {
                        capacity = 1,
                        layout = eventAttributeDescs.ToArray()
                    });
                }
                foreach (var spawnContext in spawners)
                {
                    var buffers = spawnContext.inputContexts.Select(o => new VFXBufferMapping()
                    {
                        bufferIndex = spawnContextToBufferIndex[o],
                        name = "spawner_input"
                    }).ToList();

                    if (buffers.Count > 1)
                        throw new InvalidOperationException("Unexpected spawner with multiple inputs");

                    buffers.Add(new VFXBufferMapping()
                    {
                        bufferIndex = spawnContextToBufferIndex[spawnContext],
                        name = "spawner_output"
                    });

                    var contextData = contextToCompiledData[spawnContext];
                    systemDescs.Add(new VFXSystemDesc()
                    {
                        buffers = buffers.ToArray(),
                        capacity = 0u,
                        flags = VFXSystemFlag.kVFXSystemDefault,
                        tasks = spawnContext.childrenWithImplicit.Select((b, index) =>
                            {
                                var spawnerBlock = b as VFXAbstractSpawner;
                                if (spawnerBlock == null)
                                {
                                    throw new InvalidCastException("Unexpected block type in spawnerContext");
                                }
                                if (spawnerBlock.spawnerType == VFXSpawnerType.kCustomCallback && spawnerBlock.customBehavior == null)
                                {
                                    throw new InvalidOperationException("VFXAbstractSpawner excepts a custom behavior for custom callback type");
                                }
                                if (spawnerBlock.spawnerType != VFXSpawnerType.kCustomCallback && spawnerBlock.customBehavior != null)
                                {
                                    throw new InvalidOperationException("VFXAbstractSpawner only expects a custom behavior for custom callback type");
                                }

                                /* TODOPAUL : should not have to use two enum */
                                VFXTaskType taskType = VFXTaskType.kNone;
                                switch (spawnerBlock.spawnerType)
                                {
                                    case VFXSpawnerType.kBurst: taskType = VFXTaskType.kSpawnerBurst; break;
                                    case VFXSpawnerType.kConstantRate: taskType = VFXTaskType.kSpawnerConstantRate; break;
                                    case VFXSpawnerType.kCustomCallback: taskType = VFXTaskType.kSpawnerCustomCallback; break;
                                    case VFXSpawnerType.kPeriodicBurst: taskType = VFXTaskType.kSpawnerPeriodicBurst; break;
                                    case VFXSpawnerType.kVariableRate: taskType = VFXTaskType.kSpawnerVariableRate; break;
                                    default: throw new InvalidCastException("Unexpected spawner type");
                                }

                                var cpuExpression = contextData.cpuMapper.CollectExpression(index, false).Select(o =>
                                {
                                    return new VFXValueMapping
                                    {
                                        expressionIndex = m_ExpressionGraph.GetFlattenedIndex(o.exp),
                                        name = o.name
                                    };
                                }).ToArray();

                                return new VFXTaskDesc
                                {
                                    type = taskType,
                                    buffers = Enumerable.Empty<VFXBufferMapping>().ToArray(),
                                    processor = spawnerBlock.customBehavior == null ? null : Activator.CreateInstance(spawnerBlock.customBehavior) as Object,
                                    values = cpuExpression.ToArray()
                                };
                            }).ToArray()
                    });
                }
                /* End WIP Spawner */

                foreach (var data in models.OfType<VFXDataParticle>())
                    data.FillDescs(bufferDescs, systemDescs, m_ExpressionGraph, contextToCompiledData);

                m_Graph.vfxAsset.SetSystem(systemDescs.ToArray(), bufferDescs.ToArray(), cpuBufferDescs.ToArray());
                m_ExpressionValues = valueDescs;
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Exception while compiling expression graph: {0}: {1}", e, e.StackTrace));

                // Cleaning
                if (m_Graph.vfxAsset != null)
                {
                    m_Graph.vfxAsset.ClearSpawnerData();
                    m_Graph.vfxAsset.ClearPropertyData();
                    m_Graph.vfxAsset.SetSystem(null, null, null);
                }

                m_ExpressionGraph = new VFXExpressionGraph();
                m_ExpressionValues = new List<VFXExpressionValueContainerDescAbstract>();
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
