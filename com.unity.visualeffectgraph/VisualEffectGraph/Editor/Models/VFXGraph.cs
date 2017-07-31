using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
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

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return !(model is VFXGraph); // Can hold any model except other VFXGraph
        }

        public bool UpdateSubAssets()
        {
            bool modified = false;
            if (EditorUtility.IsPersistent(this))
            {
                Profiler.BeginSample("UpdateSubAssets");

                try
                {
                    HashSet<Object> persistentObjects = new HashSet<Object>(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)).Where(o => o is VFXModel));
                    persistentObjects.Remove(this);

                    HashSet<Object> currentObjects = new HashSet<Object>();
                    CollectDependencies(currentObjects);

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

                    switch (exp.ValueType)
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

        public void RecompileIfNeeded()
        {
            if (m_ExpressionGraphDirty)
            {
                try
                {
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

                        int[] data = new int[4];
                        exp.FillOperands(data, m_ExpressionGraph);

                        // Must match data in C++ expression
                        if (exp.Is(VFXExpression.Flags.Value))
                        {
                            VFXExpressionValueContainerDescAbstract value;
                            switch (exp.ValueType)
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
                                default: throw new InvalidOperationException("Invalid type");
                            }
                            value.expressionIndex = (uint)i;
                            m_ExpressionValues.Add(value);
                        }

                        expressionDescs[i].op = exp.Operation;
                        expressionDescs[i].data = data;
                    }

                    // Generate uniforms
                    var models = new HashSet<Object>();
                    CollectDependencies(models);

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

                        var gpuMapper = m_ExpressionGraph.BuildGPUMapper(context);
                        if (gpuMapper.expressions.Count() > 0)
                            Debug.Log("GPU EXPRESSIONS FOR " + contextId);

                        // TMP output uniform buffer
                        {
                            var uniformMapper = new VFXUniformMapper(gpuMapper);
                            Debug.Log(VFXShaderWriter.WriteCBuffer(uniformMapper));

                            foreach (var exp in gpuMapper.expressions)
                                if (!exp.Is(VFXValue.Flags.InvalidOnGPU)) // Tmp this should be notified and throw
                                    Debug.Log(VFXShaderWriter.WriteParameter(exp, uniformMapper));
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

                    var expressionSheet = new VFXExpressionSheet();
                    expressionSheet.expressions = expressionDescs;
                    expressionSheet.values = m_ExpressionValues.ToArray();
                    expressionSheet.semantics = expressionSemantics.ToArray();
                    expressionSheet.exposed = parameterExposed.ToArray();

                    vfxAsset.ClearSpawnerData();
                    vfxAsset.ClearPropertyData();
                    vfxAsset.SetExpressionSheet(expressionSheet);

                    foreach (var data in models.OfType<VFXData>())
                        data.CollectAttributes(m_ExpressionGraph);

                    // TMP Debug log
                    foreach (var data in models.OfType<VFXDataParticle>())
                        data.DebugBuildAttributeBuffers();

                    foreach (var spawnerContext in models.OfType<VFXContext>().Where(model => model.contextType == VFXContextType.kSpawner))
                    {
                        var spawnDescs = spawnerContext.children.Select(b =>
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

        private VFXAsset m_Owner;
    }
}
