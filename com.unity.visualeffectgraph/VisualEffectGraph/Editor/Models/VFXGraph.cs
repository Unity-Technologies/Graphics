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
        }

        private VFXExpressionValueContainerDesc<T> CreateValueDesc<T>(VFXExpression exp, int expIndex)
        {
            var desc = new VFXExpressionValueContainerDesc<T>();
            desc.value = exp.Get<T>();
            return desc;
        }

        public void RecompileIfNeeded()
        {
            if (m_ExpressionGraphDirty)
            {
                try
                {
                    var expressionGraph = new VFXExpressionGraph();
                    expressionGraph.CompileExpressions(this, VFXExpression.Context.ReductionOption.CPUReduction);

                    // build expressions data and set them to vfx asset
                    var flatGraph = expressionGraph.FlattenedExpressions;
                    var numFlattenedExpressions = flatGraph.Count;

                    var expressionDescs = new VFXExpressionDesc[numFlattenedExpressions];
                    var expressionValues = new List<VFXExpressionValueContainerDescAbstract>();
                    for (int i = 0; i < numFlattenedExpressions; ++i)
                    {
                        var exp = flatGraph[i];

                        int[] data = new int[4];
                        // Must match data in C++ expression
                        if (exp.Is(VFXExpression.Flags.Value))
                        {
                            data[0] = (int)exp.ValueType;

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
                            expressionValues.Add(value);
                        }
                        else if (exp is VFXExpressionExtractComponent)
                        {
                            var extractExp = (VFXExpressionExtractComponent)exp;
                            data[0] = expressionGraph.GetFlattenedIndex(exp.Parents[0]);
                            data[1] = extractExp.Channel;
                            data[2] = VFXExpression.TypeToSize(exp.ValueType);
                        }
                        else if (exp is VFXExpressionFloatOperation && !(exp is VFXExpressionCombine)) // TODO Make a better test
                        {
                            var parents = exp.Parents;
                            if (parents.Length > 3)
                                throw new Exception("parents length cannot be more than 3 for float operations");
                            for (int j = 0; j < parents.Length; ++j)
                                data[j] = expressionGraph.GetFlattenedIndex(parents[j]);
                            data[3] = VFXExpression.TypeToSize(exp.ValueType);
                        }
                        else
                        {
                            var parents = exp.Parents;
                            for (int j = 0; j < parents.Length; ++j)
                                data[j] = expressionGraph.GetFlattenedIndex(parents[j]);
                        }
                        // TODO Transformation expressions

                        expressionDescs[i].op = exp.Operation;
                        expressionDescs[i].data = data;
                    }

                    // Generate uniforms
                    var models = new HashSet<Object>();
                    CollectDependencies(models);

                    var expressionSemantics = new List<VFXExpressionSemanticDesc>();
                    foreach (var context in models.OfType<VFXContext>())
                    {
                        var cpuMapper = expressionGraph.BuildCPUMapper(context);
                        int contextId = context.GetHashCode(); // TODO change that

                        foreach (var exp in cpuMapper.expressions)
                        {
                            VFXExpressionSemanticDesc desc;
                            desc.blockID = 0xFFFFFFFF; //TODO
                            desc.contextID = (uint)contextId;
                            int expIndex = expressionGraph.GetFlattenedIndex(exp);
                            string name = cpuMapper.GetName(exp);
                            if (expIndex == -1)
                                throw new Exception(string.Format("Cannot find mapped expression {0} in flattened graph", name));
                            desc.expressionIndex = (uint)expIndex;
                            desc.name = name;
                            expressionSemantics.Add(desc);
                        }
                    }

                    var expressionSheet = new VFXExpressionSheet();
                    expressionSheet.expressions = expressionDescs;
                    expressionSheet.values = expressionValues.ToArray();
                    expressionSheet.semantics = expressionSemantics.ToArray();

                    vfxAsset.ClearPropertyData();
                    vfxAsset.SetExpressionSheet(expressionSheet);
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while compiling expression graph: {0}: {1}", e, e.StackTrace));
                }

                m_ExpressionGraphDirty = false;
            }
        }

        [NonSerialized]
        private bool m_ExpressionGraphDirty = true;

        private VFXAsset m_Owner;
    }
}
