//#define USE_SHADER_AS_SUBASSET
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
#if !USE_SHADER_AS_SUBASSET
    public class VFXCacheManager : EditorWindow
    {
        [MenuItem("VFX Editor/Rebuild VFXCache")]
        public static void Rebuild()
        {
            FileUtil.DeleteFileOrDirectory(VFXGraph.baseCacheFolder);
            var vfxAssets = new List<VFXAsset>();
            var vfxAssetsGuid = AssetDatabase.FindAssets("t:VFXAsset");
            foreach (var guid in vfxAssetsGuid)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VFXAsset>(assetPath);
                if (vfxAsset != null)
                {
                    vfxAssets.Add(vfxAsset);
                }
            }

            foreach (var vfxAsset in vfxAssets)
            {
                vfxAsset.GetOrCreateGraph().OnSaved();
            }
            AssetDatabase.SaveAssets();
        }
    }
#endif

    public class VFXAssetModicationProcessor : UnityEditor.AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VFXAsset>(path);
                if (vfxAsset != null)
                {
                    var graph = vfxAsset.GetOrCreateGraph();
                    graph.OnSaved();
                }
            }
            return paths;
        }
    }

    static class VFXAssetExtensions
    {
        public static VFXGraph GetOrCreateGraph(this VFXAsset asset)
        {
            ScriptableObject g = asset.graph;
            if (g == null)
            {
                g = ScriptableObject.CreateInstance<VFXGraph>();
                g.name = "VFXGraph";
                asset.graph = g;
            }

            VFXGraph graph = (VFXGraph)g;
            graph.vfxAsset = asset;
            return graph;
        }

        public static void UpdateSubAssets(this VFXAsset asset)
        {
            asset.GetOrCreateGraph().UpdateSubAssets();
        }
    }

    class VFXGraph : VFXModel
    {
        public VFXAsset vfxAsset
        {
            get
            {
                return m_Owner;
            }
            set
            {
                m_Owner = value;
                m_ExpressionGraphDirty = true;
            }
        }

        static public string baseCacheFolder
        {
            get
            {
                return "Assets/VFXCache";
            }
        }

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return !(model is VFXGraph); // Can hold any model except other VFXGraph
        }

        public override T Clone<T>()
        {
            var from = children.ToArray();
            var copy = from.Select(o => o.Clone<VFXModel>()).ToArray();
            VFXSlot.ReproduceLinkedSlotFromHierachy(from, copy);
            VFXContext.ReproduceLinkedFlowFromHiearchy(from, copy);

            var clone = CreateInstance(GetType()) as VFXGraph;
            clone.m_Children = new List<VFXModel>();
            foreach (var model in copy)
            {
                clone.AddChild(model, -1, false);
            }
            return clone as T;
        }

        public void OnSaved()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Saving...", "Rebuild", 0);
                m_ExpressionGraphDirty = true;
                RecompileIfNeeded();
                float currentStep = 0;

#if USE_SHADER_AS_SUBASSET
                float stepCount = m_GeneratedComputeShader.Count + m_GeneratedShader.Count + 1;

                var oldComputeShader = m_GeneratedComputeShader.ToArray();
                var oldShader = m_GeneratedShader.ToArray();
                var oldPath = oldComputeShader.Select(o => AssetDatabase.GetAssetPath(o)).Concat(oldShader.Select(o => AssetDatabase.GetAssetPath(o))).ToArray();

                m_GeneratedComputeShader.Clear();
                m_GeneratedShader.Clear();

                for (int i = 0; i < oldComputeShader.Length; ++i)
                {
                    var compute = oldComputeShader[i];
                    EditorUtility.DisplayProgressBar("Saving...", string.Format("ComputeShader embedding {0}/{1}", i, oldComputeShader.Length), (++currentStep) / stepCount);
                    var computeShaderCopy = Instantiate<ComputeShader>(compute);
                    DestroyImmediate(compute, true);
                    m_GeneratedComputeShader.Add(computeShaderCopy);
                }

                for (int i = 0; i < oldShader.Length; ++i)
                {
                    var shader = oldShader[i];
                    EditorUtility.DisplayProgressBar("Saving...", string.Format("Shader embedding {0}/{1}", i, oldShader.Length), (++currentStep) / stepCount);
                    var shaderCopy = Instantiate<Shader>(shader);
                    DestroyImmediate(shader, true);
                    m_GeneratedShader.Add(shaderCopy);
                }
#else
                float stepCount = 1;
#endif
                EditorUtility.DisplayProgressBar("Saving...", "UpdateSubAssets", (++currentStep) / stepCount);
                UpdateSubAssets();
                m_saved = true;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Save failed : {0}", e);
            }
            EditorUtility.ClearProgressBar();
        }

        public bool UpdateSubAssets()
        {
            bool modified = false;
            if (EditorUtility.IsPersistent(this))
            {
                Profiler.BeginSample("UpdateSubAssets");

                try
                {
                    var persistentObjects = new HashSet<Object>(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)).Where(o => o is VFXModel || o is ComputeShader || o is Shader));
                    persistentObjects.Remove(this);

                    var currentObjects = new HashSet<Object>();
                    CollectDependencies(currentObjects);

#if USE_SHADER_AS_SUBASSET
                    if (m_GeneratedComputeShader != null)
                    {
                        foreach (var compute in m_GeneratedComputeShader)
                        {
                            currentObjects.Add(compute);
                        }
                    }

                    if (m_GeneratedShader != null)
                    {
                        foreach (var shader in m_GeneratedShader)
                        {
                            currentObjects.Add(shader);
                        }
                    }
#endif

                    // Add sub assets that are not already present
                    foreach (var obj in currentObjects)
                        if (!persistentObjects.Contains(obj))
                        {
                            obj.name = obj.GetType().Name;
                            AssetDatabase.AddObjectToAsset(obj, this);
                            modified = true;
                        }

                    // Remove sub assets that are not referenced anymore
                    foreach (var obj in persistentObjects)
                        if (!currentObjects.Contains(obj))
                        {
                            AssetDatabase.RemoveObject(obj);
                            modified = true;
                        }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }

                Profiler.EndSample();

                if (modified)
                    EditorUtility.SetDirty(this);
            }

            return modified;
        }

        protected override void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            m_saved = false;
            base.OnInvalidate(model, cause);

            if (cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                //Debug.Log("UPDATE SUB ASSETS");
                if (UpdateSubAssets())
                {
                    //AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
                }
            }

            if (cause != VFXModel.InvalidationCause.kExpressionInvalidated &&
                cause != VFXModel.InvalidationCause.kExpressionGraphChanged)
            {
                //Debug.Log("ASSET DIRTY " + cause);
                EditorUtility.SetDirty(this);
            }

            if (cause == VFXModel.InvalidationCause.kExpressionGraphChanged)
            {
                m_ExpressionGraphDirty = true;
            }

            if (cause == VFXModel.InvalidationCause.kParamChanged)
            {
                m_ExpressionValuesDirty = true;
            }
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

        private void UpdateValues()
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
                        case VFXValueType.kFloat:           SetValueDesc<float>(desc, exp); break;
                        case VFXValueType.kFloat2:          SetValueDesc<Vector2>(desc, exp); break;
                        case VFXValueType.kFloat3:          SetValueDesc<Vector3>(desc, exp); break;
                        case VFXValueType.kFloat4:          SetValueDesc<Vector4>(desc, exp); break;
                        case VFXValueType.kInt:             SetValueDesc<int>(desc, exp); break;
                        case VFXValueType.kUint:            SetValueDesc<uint>(desc, exp); break;
                        case VFXValueType.kTexture2D:       SetValueDesc<Texture2D>(desc, exp); break;
                        case VFXValueType.kTexture3D:       SetValueDesc<Texture3D>(desc, exp); break;
                        case VFXValueType.kTransform:       SetValueDesc<Matrix4x4>(desc, exp); break;
                        case VFXValueType.kCurve:           SetValueDesc<AnimationCurve>(desc, exp); break;
                        case VFXValueType.kColorGradient:   SetValueDesc<Gradient>(desc, exp); break;
                        case VFXValueType.kMesh:            SetValueDesc<Mesh>(desc, exp); break;
                        case VFXValueType.kBool:            SetValueDesc<bool>(desc, exp); break;
                        default: throw new InvalidOperationException("Invalid type");
                    }
                }
            }

            vfxAsset.SetValueSheet(m_ExpressionValues.ToArray());
        }

        public uint FindReducedExpressionIndexFromSlotCPU(VFXSlot slot)
        {
            RecompileIfNeeded();
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

        private struct GeneratedCodeData
        {
            public VFXContext context;
            public bool computeShader;
            public System.Text.StringBuilder content;
            public VFXCodeGenerator.CompilationMode compilMode;
        }

        public void RecompileIfNeeded()
        {
            if (m_ExpressionGraphDirty)
            {
                try
                {
                    var models = new HashSet<Object>();
                    CollectDependencies(models);

                    foreach (var data in models.OfType<VFXData>())
                        data.CollectAttributes();

                    m_ExpressionGraph = new VFXExpressionGraph();
                    m_ExpressionGraph.CompileExpressions(this, VFXExpressionContextOption.Reduction);

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
                                case VFXValueType.kFloat:           value = CreateValueDesc<float>(exp, i); break;
                                case VFXValueType.kFloat2:          value = CreateValueDesc<Vector2>(exp, i); break;
                                case VFXValueType.kFloat3:          value = CreateValueDesc<Vector3>(exp, i); break;
                                case VFXValueType.kFloat4:          value = CreateValueDesc<Vector4>(exp, i); break;
                                case VFXValueType.kInt:             value = CreateValueDesc<int>(exp, i); break;
                                case VFXValueType.kUint:            value = CreateValueDesc<uint>(exp, i); break;
                                case VFXValueType.kTexture2D:       value = CreateValueDesc<Texture2D>(exp, i); break;
                                case VFXValueType.kTexture3D:       value = CreateValueDesc<Texture3D>(exp, i); break;
                                case VFXValueType.kTransform:       value = CreateValueDesc<Matrix4x4>(exp, i); break;
                                case VFXValueType.kCurve:           value = CreateValueDesc<AnimationCurve>(exp, i); break;
                                case VFXValueType.kColorGradient:   value = CreateValueDesc<Gradient>(exp, i); break;
                                case VFXValueType.kMesh:            value = CreateValueDesc<Mesh>(exp, i); break;
                                case VFXValueType.kBool:            value = CreateValueDesc<bool>(exp, i); break;
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

                    vfxAsset.ClearSpawnerData();
                    vfxAsset.ClearPropertyData();
                    vfxAsset.SetExpressionSheet(expressionSheet);

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
                        int spawnerIndex = vfxAsset.AddSpawner(spawnDescs, (uint)spawnerContext.GetParent().GetIndex(spawnerContext));
                        vfxAsset.LinkStartEvent("OnStart", spawnerIndex);
                    }

                    var compilMode = new[] { /* VFXCodeGenerator.CompilationMode.Debug,*/ VFXCodeGenerator.CompilationMode.Runtime };
                    var generatedList = new List<GeneratedCodeData>();
                    foreach (var context in models.OfType<VFXContext>().Where(model => model.contextType != VFXContextType.kSpawner))
                    {
                        var codeGeneratorTemplate = context.codeGeneratorTemplate;
                        if (codeGeneratorTemplate != null)
                        {
                            var generatedContent = compilMode.Select(o => new StringBuilder()).ToArray();
                            var gpuMapper = m_ExpressionGraph.BuildGPUMapper(context);
                            VFXCodeGenerator.Build(context, compilMode, generatedContent, gpuMapper, codeGeneratorTemplate);

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
                        if (vfxAsset != null)
                        {
                            var path = AssetDatabase.GetAssetPath(vfxAsset);
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
                        }
                    }

                    foreach (var component in VFXComponent.GetAllActive())
                    {
                        if (component.vfxAsset == vfxAsset)
                        {
                            component.vfxAsset = vfxAsset; //TODOPAUL : find another way to detect reload
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while compiling expression graph: {0}: {1}", e, e.StackTrace));

                    // Cleaning
                    if (vfxAsset != null)
                    {
                        vfxAsset.ClearSpawnerData();
                        vfxAsset.ClearPropertyData();
                    }

                    m_ExpressionGraph = new VFXExpressionGraph();
                    m_ExpressionValues = new List<VFXExpressionValueContainerDescAbstract>();
                }

                m_ExpressionGraphDirty = false;
                m_ExpressionValuesDirty = false; // values already set
            }

            if (m_ExpressionValuesDirty)
            {
                UpdateValues();
                m_ExpressionValuesDirty = false;
            }
        }

        [NonSerialized]
        private bool m_ExpressionGraphDirty = true;
        [NonSerialized]
        private bool m_ExpressionValuesDirty = true;

        [NonSerialized]
        private VFXExpressionGraph m_ExpressionGraph;
        [NonSerialized]
        private List<VFXExpressionValueContainerDescAbstract> m_ExpressionValues;

        [SerializeField]
        protected bool m_saved = false;

        public bool saved { get { return m_saved; } }

        private VFXAsset m_Owner;
    }
}
