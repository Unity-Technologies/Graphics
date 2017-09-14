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
        public VFXExpressionMapper gpuMapper;
        public VFXUniformMapper uniformMapper;
        public Object processor;

        public VFXContextCompiledData(VFXExpressionMapper gpuMapper, VFXUniformMapper uniformMapper)
        {
            this.gpuMapper = gpuMapper;
            this.uniformMapper = uniformMapper;
            processor = null;
        }
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

        private VFXExpressionValueContainerDesc<T> CreateValueDesc<T>(VFXExpression exp, int expIndex)
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

                // build expressions data and set them to vfx asset
                var flatGraph = m_ExpressionGraph.FlattenedExpressions;
                var numFlattenedExpressions = flatGraph.Count;

                var expressionDescs = new VFXExpressionDesc[numFlattenedExpressions];
                m_ExpressionValues = new List<VFXExpressionValueContainerDescAbstract>();
                for (int i = 0; i < numFlattenedExpressions; ++i)
                {
                    var exp = flatGraph[i];
                    var data = exp.GetOperands(m_ExpressionGraph);

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
                        m_ExpressionValues.Add(value);
                    }

                    expressionDescs[i].op = exp.operation;
                    expressionDescs[i].data = data;
                }

                var expressionSemantics = new List<VFXExpressionSemanticDesc>();
                foreach (var context in models.OfType<VFXContext>())
                {
                    uint contextId = (uint)context.GetParent().GetIndex(context);
                    var cpuMapper = m_ExpressionGraph.BuildCPUMapper(context);
                    foreach (var exp in cpuMapper.expressions)
                    {
                        VFXExpressionSemanticDesc desc;
                        var mappedDataList = cpuMapper.GetData(exp);
                        foreach (var mappedData in mappedDataList)
                        {
                            desc.blockID = (uint)mappedData.id;
                            desc.contextID = contextId;
                            int expIndex = m_ExpressionGraph.GetFlattenedIndex(exp);
                            if (expIndex == -1)
                                throw new Exception(string.Format("Cannot find mapped expression {0} in flattened graph", mappedData.name));
                            desc.expressionIndex = (uint)expIndex;
                            desc.name = mappedData.name;
                            expressionSemantics.Add(desc);
                        }
                    }
                }

                var parameterExposed = new List<VFXExposedDesc>();
                foreach (var parameter in models.OfType<VFXParameter>())
                {
                    if (parameter.exposed)
                    {
                        var outputSlotExpr = parameter.GetOutputSlot(0).GetExpression();
                        if (outputSlotExpr != null)
                        {
                            parameterExposed.Add(new VFXExposedDesc()
                            {
                                name = parameter.exposedName,
                                expressionIndex = (uint)m_ExpressionGraph.GetFlattenedIndex(outputSlotExpr)
                            });
                        }
                    }
                }

                var eventAttributes = new List<VFXEventAttributeDesc>();
                foreach (var context in models.OfType<VFXContext>().Where(o => o.contextType == VFXContextType.kSpawner))
                {
                    foreach (var linked in context.outputContexts)
                    {
                        foreach (var attribute in linked.GetData().GetAttributes())
                        {
                            if (attribute.attrib.location == VFXAttributeLocation.Source)
                            {
                                eventAttributes.Add(new VFXEventAttributeDesc()
                                {
                                    name = attribute.attrib.name,
                                    type = attribute.attrib.type
                                });
                            }
                        }
                    }
                }

                foreach (var data in models.OfType<VFXData>())
                    data.GenerateAttributeLayout();

                var expressionSheet = new VFXExpressionSheet();
                expressionSheet.expressions = expressionDescs;
                expressionSheet.values = m_ExpressionValues.ToArray();
                expressionSheet.semantics = expressionSemantics.ToArray();
                expressionSheet.exposed = parameterExposed.ToArray();
                expressionSheet.eventAttributes = eventAttributes.ToArray();

                m_Graph.vfxAsset.ClearSpawnerData();
                m_Graph.vfxAsset.ClearPropertyData();
                m_Graph.vfxAsset.SetExpressionSheet(expressionSheet);

                foreach (var spawnerContext in models.OfType<VFXContext>().Where(model => model.contextType == VFXContextType.kSpawner))
                {
                    var spawnDescs = spawnerContext.childrenWithImplicit.Select(b =>
                        {
                            var spawner = b as VFXAbstractSpawner;
                            if (spawner == null)
                            {
                                throw new InvalidCastException("Unexpected type in spawnerContext");
                            }

                            if (spawner.spawnerType == VFXSpawnerType.kCustomCallback && spawner.customBehavior == null)
                            {
                                throw new Exception("VFXAbstractSpawner excepts a custom behavior for custom callback type");
                            }

                            if (spawner.spawnerType != VFXSpawnerType.kCustomCallback && spawner.customBehavior != null)
                            {
                                throw new Exception("VFXAbstractSpawner only expects a custom behavior for custom callback type");
                            }
                            return new VFXSpawnerDesc()
                            {
                                customBehavior = spawner.customBehavior,
                                type = spawner.spawnerType
                            };
                        }).ToArray();
                    int spawnerIndex = m_Graph.vfxAsset.AddSpawner(spawnDescs, (uint)spawnerContext.GetParent().GetIndex(spawnerContext));
                    m_Graph.vfxAsset.LinkStartEvent("OnStart", spawnerIndex);
                }

                var compilMode = new[] { /* VFXCodeGenerator.CompilationMode.Debug,*/ VFXCodeGenerator.CompilationMode.Runtime };
                var generatedList = new List<GeneratedCodeData>();
                Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData = new Dictionary<VFXContext, VFXContextCompiledData>();
                foreach (var context in models.OfType<VFXContext>().Where(model => model.contextType != VFXContextType.kSpawner))
                {
                    var codeGeneratorTemplate = context.codeGeneratorTemplate;
                    if (codeGeneratorTemplate != null)
                    {
                        var generatedContent = compilMode.Select(o => new StringBuilder()).ToArray();

                        var gpuMapper = m_ExpressionGraph.BuildGPUMapper(context);
                        var uniformMapper = new VFXUniformMapper(gpuMapper);
                        var contextData = new VFXContextCompiledData(gpuMapper, uniformMapper);
                        contextToCompiledData.Add(context, contextData);

                        VFXCodeGenerator.Build(context, compilMode, generatedContent, contextData, codeGeneratorTemplate);

                        for (int i = 0; i < compilMode.Length; ++i)
                        {
                            generatedList.Add(new GeneratedCodeData()
                            {
                                context = context,
                                computeShader = context.codeGeneratorCompute,
                                compilMode = compilMode[i],
                                content = generatedContent[i]
                            });
                        }
                    }
                }

                {
                    var generatedShader = new List<Object>();
                    var currentCacheFolder = baseCacheFolder;
                    if (m_Graph.vfxAsset != null)
                    {
                        var path = AssetDatabase.GetAssetPath(m_Graph.vfxAsset);
                        path = path.Replace("Assets", "");
                        path = path.Replace(".asset", "");
                        currentCacheFolder += path;
                    }

                    System.IO.Directory.CreateDirectory(currentCacheFolder);
                    for (int i = 0; i < generatedList.Count; ++i)
                    {
                        var generated = generatedList[i];
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
                        generatedShader.Add(imported);

                        var contextData = contextToCompiledData[generated.context];
                        contextData.processor = imported;
                        contextToCompiledData[generated.context] = contextData;
                    }
                }

                var bufferDescs = new List<VFXBufferDesc>();
                var systemDescs = new List<VFXSystemDesc>();

                foreach (var data in models.OfType<VFXDataParticle>())
                {
                    bool hasState = data.bufferSize > 0;
                    bool hasKill = data.IsAttributeStored(VFXAttribute.Alive);

                    var attributeBufferIndex = -1;
                    var deadListBufferIndex = -1;

                    var systemBufferMappings = new List<VFXBufferMapping>();

                    if (hasState)
                    {
                        attributeBufferIndex = bufferDescs.Count;
                        bufferDescs.Add(new VFXBufferDesc(ComputeBufferType.Raw, data.bufferSize >> 2, 4));
                        systemBufferMappings.Add(new VFXBufferMapping(attributeBufferIndex, "attributeBuffer"));
                    }

                    var systemFlag = VFXSystemFlag.kVFXSystemDefault;
                    if (hasKill)
                    {
                        systemFlag |= VFXSystemFlag.kVFXSystemHasKill;
                        deadListBufferIndex = bufferDescs.Count;
                        bufferDescs.Add(new VFXBufferDesc(ComputeBufferType.Append, data.capacity, 4));
                        systemBufferMappings.Add(new VFXBufferMapping(deadListBufferIndex, "deadList"));
                    }

                    var taskDescs = new List<VFXTaskDesc>();
                    var bufferMappings = new List<VFXBufferMapping>();
                    var uniformMappings = new List<VFXUniformMapping>();

                    foreach (var context in data.owners)
                    {
                        if (!contextToCompiledData.ContainsKey(context))
                            continue;

                        var taskDesc = new VFXTaskDesc();
                        switch (context.contextType)
                        {
                            case VFXContextType.kInit: taskDesc.type = VFXTaskType.kVFXInitialize; break;
                            case VFXContextType.kUpdate: taskDesc.type = VFXTaskType.kVFXUpdate; break;
                            case VFXContextType.kOutput: taskDesc.type = VFXTaskType.kVFXOutput; break;
                            default: throw new InvalidOperationException(string.Format("Not supposed to have this context types in particle system {0}", context.contextType));
                        }

                        bufferMappings.Clear();
                        if (attributeBufferIndex != -1)
                            bufferMappings.Add(new VFXBufferMapping(attributeBufferIndex, "attributeBuffer"));
                        if (deadListBufferIndex != -1 && context.contextType != VFXContextType.kOutput)
                            bufferMappings.Add(new VFXBufferMapping(deadListBufferIndex, context.contextType == VFXContextType.kUpdate ? "deadListOut" : "deadListIn"));

                        var contextData = contextToCompiledData[context];
                        uniformMappings.Clear();
                        foreach (var uniform in contextData.uniformMapper.uniforms)
                            uniformMappings.Add(new VFXUniformMapping(m_ExpressionGraph.GetFlattenedIndex(uniform), contextData.uniformMapper.GetName(uniform)));

                        taskDesc.buffers = bufferMappings.ToArray();
                        taskDesc.uniforms = uniformMappings.ToArray();

                        taskDesc.processor = contextToCompiledData[context].processor;

                        taskDescs.Add(taskDesc);
                    }

                    systemDescs.Add(new VFXSystemDesc()
                    {
                        flags = systemFlag,
                        tasks = taskDescs.ToArray(),
                        capacity = data.capacity,
                        buffers = systemBufferMappings.ToArray(),
                        type = VFXSystemType.kVFXParticle,
                    });
                }

                m_Graph.vfxAsset.SetSystem(systemDescs.ToArray(), bufferDescs.ToArray());
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Exception while compiling expression graph: {0}: {1}", e, e.StackTrace));

                // Cleaning
                if (m_Graph.vfxAsset != null)
                {
                    m_Graph.vfxAsset.ClearSpawnerData();
                    m_Graph.vfxAsset.ClearPropertyData();
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
