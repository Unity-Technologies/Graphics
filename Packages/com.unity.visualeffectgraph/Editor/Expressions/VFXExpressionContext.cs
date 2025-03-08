using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [Flags]
    enum VFXExpressionContextOption
    {
        None = 0,
        Reduction = 1 << 0,
        CPUEvaluation = 1 << 1,
        ConstantFolding = 1 << 2,
        GPUDataTransformation = 1 << 3,
        PatchReadToEventAttribute = 1 << 4,
        CollectPerContextData = 1 << 5
    }

    abstract partial class VFXExpression
    {
        public class Context
        {
            private bool Has(VFXExpressionContextOption options)
            {
                return (m_ReductionOptions & options) == options;
            }

            private bool HasAny(VFXExpressionContextOption options)
            {
                return (m_ReductionOptions & options) != 0;
            }

            public Context(VFXExpressionContextOption reductionOption, List<VFXLayoutElementDesc> globalEventAttributes = null)
            {
                m_ReductionOptions = reductionOption;
                m_GlobalEventAttribute = globalEventAttributes;

                if (Has(VFXExpressionContextOption.CPUEvaluation) && Has(VFXExpressionContextOption.GPUDataTransformation))
                    throw new ArgumentException("Invalid reduction options");
            }

            public void RegisterExpression(VFXExpression expression, VFXContext sourceContext = null)
            {
                if (!m_EndExpressions.TryGetValue(expression, out var contexts))
                {
                    contexts = new();
                    m_EndExpressions.Add(expression, contexts);
                }

                if (sourceContext != null)
                {
                    if (!contexts.Add(sourceContext))
                        throw new InvalidOperationException("Trying to add twice the same context for the same expression.");
                }
            }

            public void UnregisterExpression(VFXExpression expression)
            {
                Invalidate(expression);
                m_EndExpressions.Remove(expression);
            }

            class CollectedData
            {
                public readonly HashSet<VFXExpression> processedExpressions = new();
                public readonly HashSet<VFXExpression> markedExpressions = new();
                public readonly Dictionary<IHLSLCodeHolder, HashSet<VFXExpression>> childrenExpressionHLSLCodeHolder = new();
                public readonly Dictionary<VFXExpressionBufferWithType, HashSet<VFXExpression>> childrenExpressionBufferWithType = new();
            }

            private void CollectPerContextDataRecursive(VFXExpression node, Stack<VFXExpression> currentChildren, CollectedData data)
            {
                if (data.processedExpressions.Contains(node))
                {
                    if (data.markedExpressions.Contains(node))
                    {
                        foreach (var hlslCodeHolderCollection in data.childrenExpressionHLSLCodeHolder)
                        {
                            if (hlslCodeHolderCollection.Value.Contains(node))
                            {
                                foreach (var child in currentChildren)
                                {
                                    data.markedExpressions.Add(child);
                                    hlslCodeHolderCollection.Value.Add(child);
                                }
                            }
                        }

                        foreach (var expressionBufferWithTypeCollection in data.childrenExpressionBufferWithType)
                        {
                            if (expressionBufferWithTypeCollection.Value.Contains(node))
                            {
                                foreach (var child in currentChildren)
                                {
                                    data.markedExpressions.Add(child);
                                    expressionBufferWithTypeCollection.Value.Add(child);
                                }
                            }
                        }
                    }
                    return;
                }

                currentChildren.Push(node);
                if (node is IHLSLCodeHolder hlslCodeHolder)
                {
                    if (!data.childrenExpressionHLSLCodeHolder.TryGetValue(hlslCodeHolder, out var childCollection))
                    {
                        childCollection = new();
                        data.childrenExpressionHLSLCodeHolder.Add(hlslCodeHolder, childCollection);
                    }

                    foreach (var child in currentChildren)
                    {
                        data.markedExpressions.Add(child);
                        childCollection.Add(child);
                    }
                }

                if (node is VFXExpressionBufferWithType expressionWithType)
                {
                    if (!data.childrenExpressionBufferWithType.TryGetValue(expressionWithType, out var childCollection))
                    {
                        childCollection = new();
                        data.childrenExpressionBufferWithType.Add(expressionWithType, childCollection);
                    }

                    foreach (var child in currentChildren)
                    {
                        data.markedExpressions.Add(child);
                        childCollection.Add(child);
                    }
                }

                foreach (var parent in node.parents)
                    CollectPerContextDataRecursive(parent, currentChildren, data);

                data.processedExpressions.Add(node);
                currentChildren.Pop();
            }

            private void CollectPerContextData()
            {
                var collectedDataCache = new CollectedData();
                var childrenStackCache = new Stack<VFXExpression>();
                foreach (var exp in m_EndExpressions)
                {
                    if (childrenStackCache.Count > 0)
                        throw new InvalidOperationException("Unexpected Children Stack after dependency collection.");
                    CollectPerContextDataRecursive(exp.Key, childrenStackCache, collectedDataCache);

                    if (collectedDataCache.markedExpressions.Contains(exp.Key))
                    {
                        foreach (var hlslCodeHolderCollection in collectedDataCache.childrenExpressionHLSLCodeHolder)
                        {
                            if (hlslCodeHolderCollection.Value.Contains(exp.Key))
                            {
                                foreach (var context in exp.Value)
                                {
                                    if (!m_HLSLCollectionPerContext.TryGetValue(context, out var codeHolders))
                                    {
                                        codeHolders = new();
                                        m_HLSLCollectionPerContext.Add(context, codeHolders);
                                    }
                                    codeHolders.Add(hlslCodeHolderCollection.Key);
                                }
                            }
                        }

                        foreach (var expressionBufferWithTypeCollection in collectedDataCache.childrenExpressionBufferWithType)
                        {
                            if (expressionBufferWithTypeCollection.Value.Contains(exp.Key))
                            {
                                foreach (var context in exp.Value)
                                {
                                    if (!m_GraphicsBufferTypeUsagePerContext.TryGetValue(context, out var usages))
                                    {
                                        usages = new();
                                        m_GraphicsBufferTypeUsagePerContext.Add(context, usages);
                                    }

                                    var usage = expressionBufferWithTypeCollection.Key.Type;
                                    var buffer = expressionBufferWithTypeCollection.Key.parents[0];
                                    if (!usages.TryAdd(buffer, usage) && usages[buffer] != usage)
                                    {
                                        throw new InvalidOperationException($"Diverging type usage for GraphicsBuffer : {buffer}, {usage}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            static readonly ProfilerMarker s_CollectPerContextData = new ProfilerMarker("VFXEditor.CollectPerContextData");
            static readonly ProfilerMarker s_CompileExpressionContext = new ProfilerMarker("VFXEditor.CompileExpressionContext");

            public void Compile()
            {
                if (Has(VFXExpressionContextOption.CollectPerContextData))
                {
                    using (s_CollectPerContextData.Auto())
                    {
                        CollectPerContextData();
                    }
                }

                using (s_CompileExpressionContext.Auto())
                {
                    foreach (var exp in m_EndExpressions)
                        Compile(exp.Key);

                    if (HasAny(VFXExpressionContextOption.GPUDataTransformation | VFXExpressionContextOption.PatchReadToEventAttribute))
                    {
                        var gpuTransformation = Has(VFXExpressionContextOption.GPUDataTransformation);
                        var spawnEventPath = Has(VFXExpressionContextOption.PatchReadToEventAttribute);
                        foreach (var exp in m_EndExpressions)
                            m_ReducedCache[exp.Key] = PatchVFXExpression(GetReduced(exp.Key), null /* no source in end expression */, gpuTransformation, spawnEventPath, m_GlobalEventAttribute);
                    }
                }
            }

            public void Recompile()
            {
                Invalidate();
                Compile();
            }

            private bool ShouldEvaluate(VFXExpression exp, VFXExpression[] reducedParents)
            {
                if (!HasAny(VFXExpressionContextOption.Reduction | VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding))
                    return false;

                if (exp.IsAny(Flags.NotCompilableOnCPU))
                    return false;

                if (!Has(VFXExpressionContextOption.CPUEvaluation) && exp.IsAny(Flags.InvalidConstant))
                    return false;

                if (!exp.Is(Flags.Value) && reducedParents.Length == 0) // not a value
                    return false;

                Flags flag = Flags.Value;
                if (!Has(VFXExpressionContextOption.CPUEvaluation))
                    flag |= Has(VFXExpressionContextOption.ConstantFolding) ? Flags.Foldable : Flags.Constant;

                if (exp.Is(Flags.Value) && ((exp.m_Flags & (flag | Flags.InvalidOnCPU)) != flag))
                    return false;

                foreach (var parent in reducedParents)
                {
                    if ((parent.m_Flags & (flag | Flags.InvalidOnCPU)) != flag)
                        return false;
                }

                return true;
            }

            private VFXExpression PatchVFXExpression(VFXExpression input, VFXExpression targetExpression, bool insertGPUTransformation, bool patchReadAttributeForSpawn, IEnumerable<VFXLayoutElementDesc> globalEventAttribute)
            {
                if (insertGPUTransformation)
                {
                    switch (input.valueType)
                    {
                        case VFXValueType.ColorGradient:
                            input = new VFXExpressionBakeGradient(input);
                            break;
                        case VFXValueType.Curve:
                            input = new VFXExpressionBakeCurve(input);
                            break;

                        case VFXValueType.Mesh:
                        case VFXValueType.SkinnedMeshRenderer:
                            if (targetExpression != null)
                            {
                                if (input.valueType == VFXValueType.Mesh)
                                {
                                    switch (targetExpression.operation)
                                    {
                                        case VFXExpressionOperation.SampleMeshVertexFloat:
                                        case VFXExpressionOperation.SampleMeshVertexFloat2:
                                        case VFXExpressionOperation.SampleMeshVertexFloat3:
                                        case VFXExpressionOperation.SampleMeshVertexFloat4:
                                        case VFXExpressionOperation.SampleMeshVertexColor:
                                            var channelFormatAndDimensionAndStream = targetExpression.parents[2];
                                            channelFormatAndDimensionAndStream = Compile(channelFormatAndDimensionAndStream);
                                            if (!(channelFormatAndDimensionAndStream is VFXExpressionMeshChannelInfos))
                                                throw new InvalidOperationException("Unexpected type of expression in mesh sampling : " + channelFormatAndDimensionAndStream);
                                            input = new VFXExpressionVertexBufferFromMesh(input, channelFormatAndDimensionAndStream);
                                            break;
                                        case VFXExpressionOperation.SampleMeshIndex:
                                            input = new VFXExpressionIndexBufferFromMesh(input);
                                            break;
                                        default:
                                            throw new InvalidOperationException("Unexpected source operation for InsertGPUTransformation : " + targetExpression.operation);
                                    }
                                }
                                else //VFXValueType.SkinnedMeshRenderer
                                {
                                    if (targetExpression is IVFXExpressionSampleSkinnedMesh skinnedMeshExpression)
                                    {
                                        var channelFormatAndDimensionAndStream = targetExpression.parents[2];
                                        channelFormatAndDimensionAndStream = Compile(channelFormatAndDimensionAndStream);
                                        if (!(channelFormatAndDimensionAndStream is VFXExpressionMeshChannelInfos))
                                            throw new InvalidOperationException("Unexpected type of expression in skinned mesh sampling : " + channelFormatAndDimensionAndStream);
                                        input = new VFXExpressionVertexBufferFromSkinnedMeshRenderer(input, channelFormatAndDimensionAndStream, skinnedMeshExpression.frame);
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException("Unexpected source operation for InsertGPUTransformation : " + targetExpression);
                                    }
                                }
                            } //else sourceExpression is null, we can't determine usage but it's possible if value is declared but not used.
                            break;

                        default:
                            //Nothing to patch on this type
                            break;
                    }
                }

                if (input is VFXExpressionBufferWithType bufferWithType)
                {
                    input = input.parents[0]; //Explicitly skip NoOp expression
                }

                if (patchReadAttributeForSpawn && input is VFXAttributeExpression attribute)
                {
                    if (attribute.attributeLocation == VFXAttributeLocation.Current)
                    {
                        if (globalEventAttribute == null)
                            throw new InvalidOperationException("m_GlobalEventAttribute is null");

                        foreach (var layoutDesc in globalEventAttribute)
                        {
                            if (layoutDesc.name == attribute.attributeName)
                            {
                                input = new VFXReadEventAttributeExpression(attribute.attribute, layoutDesc.offset.element);
                                break;
                            }
                        }

                        if (input is not VFXReadEventAttributeExpression)
                            throw new InvalidOperationException("Unable to find " + attribute.attributeName + " in globalEventAttribute");
                    }
                }

                return input;
            }

            public VFXExpression Compile(VFXExpression expression)
            {
                var gpuTransformation = Has(VFXExpressionContextOption.GPUDataTransformation);
                var patchReadAttributeForSpawn = Has(VFXExpressionContextOption.PatchReadToEventAttribute);

                VFXExpression reduced;
                if (!m_ReducedCache.TryGetValue(expression, out reduced))
                {
                    var parents = new VFXExpression[expression.parents.Length];
                    for (var i = 0; i < expression.parents.Length; i++)
                    {
                        var parent = Compile(expression.parents[i]);
                        bool currentGPUTransformation = gpuTransformation
                            && expression.IsAny(VFXExpression.Flags.NotCompilableOnCPU)
                            && !parent.IsAny(VFXExpression.Flags.NotCompilableOnCPU);
                        parent = PatchVFXExpression(parent, expression, currentGPUTransformation, patchReadAttributeForSpawn, m_GlobalEventAttribute);
                        parents[i] = parent;
                    }

                    if (ShouldEvaluate(expression, parents))
                    {
                        reduced = expression.Evaluate(parents);
                    }
                    else if (HasAny(VFXExpressionContextOption.Reduction | VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding) || !StructuralComparisons.StructuralEqualityComparer.Equals(parents, expression.parents))
                    {
                        reduced = expression.Reduce(parents);
                    }
                    else
                    {
                        reduced = expression;
                    }

                    m_ReducedCache[expression] = reduced;
                }
                return reduced;
            }

            public void Invalidate()
            {
                m_HLSLCollectionPerContext.Clear();
                m_ReducedCache.Clear();
                m_GraphicsBufferTypeUsagePerContext.Clear();
            }

            public void Invalidate(VFXExpression expression)
            {
                m_ReducedCache.Remove(expression);
            }

            public VFXExpression GetReduced(VFXExpression expression)
            {
                VFXExpression reduced;
                m_ReducedCache.TryGetValue(expression, out reduced);
                return reduced != null ? reduced : expression;
            }

            private void AddReducedGraph(HashSet<VFXExpression> dst, VFXExpression exp)
            {
                if (!dst.Contains(exp))
                {
                    dst.Add(exp);
                    foreach (var parent in exp.parents)
                        AddReducedGraph(dst, parent);
                }
            }

            public HashSet<VFXExpression> BuildAllReduced()
            {
                var reduced = new HashSet<VFXExpression>();
                foreach (var exp in m_EndExpressions)
                    if (m_ReducedCache.ContainsKey(exp.Key))
                        AddReducedGraph(reduced, m_ReducedCache[exp.Key]);
                return reduced;
            }

            public IEnumerable<VFXExpression> RegisteredExpressions => m_EndExpressions.Keys;
            public Dictionary<VFXContext, Dictionary<VFXExpression, BufferType>> GraphicsBufferTypeUsagePerContext => m_GraphicsBufferTypeUsagePerContext;

            public Dictionary<VFXContext, List<IHLSLCodeHolder>> hlslCodeHoldersPerContext => m_HLSLCollectionPerContext;

            private Dictionary<VFXExpression, VFXExpression> m_ReducedCache = new();
            private Dictionary<VFXExpression, HashSet<VFXContext>> m_EndExpressions = new ();
            private Dictionary<VFXContext, Dictionary<VFXExpression, BufferType>> m_GraphicsBufferTypeUsagePerContext = new ();

            private IEnumerable<VFXLayoutElementDesc> m_GlobalEventAttribute;
            private VFXExpressionContextOption m_ReductionOptions;
            private readonly Dictionary<VFXContext, List<IHLSLCodeHolder>> m_HLSLCollectionPerContext = new ();
        }
    }
}
