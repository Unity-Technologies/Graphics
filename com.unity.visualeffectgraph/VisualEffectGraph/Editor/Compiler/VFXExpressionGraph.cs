using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    class VFXExpressionGraph
    {
        private struct ExpressionData
        {
            public int depth;
            public int index;
        }

        public VFXExpressionGraph()
        {}

        private void AddExpressionDataRecursively(Dictionary<VFXExpression, ExpressionData> dst, VFXExpression exp, int depth = 0)
        {
            ExpressionData data;
            if (!dst.TryGetValue(exp, out data) || data.depth < depth)
            {
                data.index = -1; // Will be overridden later on
                data.depth = depth;
                dst[exp] = data;
                foreach (var parent in exp.Parents)
                    AddExpressionDataRecursively(dst, parent, depth + 1);
            }
        }

        private void ProcessMapper(VFXExpressionMapper mapper, VFXExpression.Context exprContext, HashSet<VFXExpression> expressions)
        {
            foreach (var exp in mapper.expressions)
                exprContext.RegisterExpression(exp);
            expressions.UnionWith(mapper.expressions);
        }

        public void CompileExpressions(VFXGraph graph, VFXExpression.Context.ReductionOption option)
        {
            Profiler.BeginSample("CompileExpressionGraph");

            try
            {
                m_Expressions.Clear();
                m_ExpressionsToReduced.Clear();
                m_FlattenedExpressions.Clear();
                m_ExpressionsData.Clear();
                m_ContextsToCPUExpressions.Clear();
                m_ContextsToGPUExpressions.Clear();

                var models = new HashSet<Object>();
                graph.CollectDependencies(models);
                var contexts = models.OfType<VFXContext>();

                HashSet<VFXExpression> cpuExpressions = new HashSet<VFXExpression>();
                HashSet<VFXExpression> gpuExpressions = new HashSet<VFXExpression>();

                var expressionContext = new VFXExpression.Context(option);

                foreach (var context in contexts.ToArray())
                {
                    var cpuMapper = context.GetCPUExpressions();
                    var gpuMapper = context.GetGPUExpressions();

                    if (cpuMapper != null)
                    {
                        ProcessMapper(cpuMapper, expressionContext, cpuExpressions);
                        m_ContextsToCPUExpressions.Add(context, cpuMapper);
                    }

                    if (gpuMapper != null)
                    {
                        ProcessMapper(gpuMapper, expressionContext, gpuExpressions);
                        m_ContextsToGPUExpressions.Add(context, gpuMapper);
                    }
                }

                expressionContext.Compile();

                foreach (var exp in expressionContext.RegisteredExpressions)
                    m_ExpressionsToReduced.Add(exp, expressionContext.GetReduced(exp));

                // TODO Transform all not compatible CPU data to GPU data by inserting expressions in the graph
                // Here ...

                m_Expressions.UnionWith(expressionContext.BuildAllReduced());

                // flatten
                foreach (var exp in m_ExpressionsToReduced.Values)
                    AddExpressionDataRecursively(m_ExpressionsData, exp);

                var sortedList = m_ExpressionsData.Where(kvp =>
                    {
                        var exp = kvp.Key;
                        return !exp.Is(VFXExpression.Flags.PerElement);
                    }).ToList(); // remove per element expression from flattened data // TODO Remove uniform constants too
                sortedList.Sort((kvpA, kvpB) => kvpB.Value.depth.CompareTo(kvpA.Value.depth));
                m_FlattenedExpressions = sortedList.Select(kvp => kvp.Key).ToList();

                // update index in expression data
                for (int i = 0; i < m_FlattenedExpressions.Count; ++i)
                {
                    var data = m_ExpressionsData[m_FlattenedExpressions[i]];
                    data.index = i;
                    m_ExpressionsData[m_FlattenedExpressions[i]] = data;
                }

                //Debug.Log("---- Expression list");
                //for (int i = 0; i < m_FlattenedExpressions.Count; ++i)
                //    Debug.Log(string.Format("{0}\t\t{1}", i, m_FlattenedExpressions[i].GetType().Name));

                Debug.Log(string.Format("RECOMPILE EXPRESSION GRAPH - NB EXPRESSIONS: {0} - NB END EXPRESSIONS: {1}", m_Expressions.Count, m_ExpressionsToReduced.Count));
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        public int GetFlattenedIndex(VFXExpression exp)
        {
            if (m_ExpressionsData.ContainsKey(exp))
                return m_ExpressionsData[exp].index;
            return -1;
        }

        public VFXExpression GetReduced(VFXExpression exp)
        {
            VFXExpression reduced;
            m_ExpressionsToReduced.TryGetValue(exp, out reduced);
            return reduced;
        }

        public VFXExpressionMapper BuildCPUMapper(VFXContext context)
        {
            return BuildMapper(context, m_ContextsToCPUExpressions, VFXExpression.Flags.InvalidOnCPU);
        }

        public VFXExpressionMapper BuildGPUMapper(VFXContext context)
        {
            return BuildMapper(context, m_ContextsToGPUExpressions, VFXExpression.Flags.InvalidOnGPU);
        }

        public List<string> GetAllNames(VFXExpression exp)
        {
            List<string> names = new List<string>();
            foreach (var mapper in m_ContextsToCPUExpressions.Values.Concat(m_ContextsToGPUExpressions.Values))
            {
                names.AddRange(mapper.GetData(exp).Select(o => o.fullName));
            }
            return names;
        }

        private VFXExpressionMapper BuildMapper(VFXContext context, Dictionary<VFXContext, VFXExpressionMapper> dictionnary, VFXExpression.Flags check)
        {
            VFXExpressionMapper outMapper = new VFXExpressionMapper();
            VFXExpressionMapper inMapper;

            dictionnary.TryGetValue(context, out inMapper);

            if (inMapper != null)
            {
                foreach (var exp in inMapper.expressions)
                {
                    var reduced = GetReduced(exp);
                    if (reduced.Is(check))
                        throw new InvalidOperationException(string.Format("The expression is not valid as it have the invalid flag: " + check));

                    var mappedDataList = inMapper.GetData(exp);
                    foreach (var mappedData in mappedDataList)
                        outMapper.AddExpression(reduced, mappedData);
                }
            }
        }
    }

    return outMapper;
}

public HashSet<VFXExpression> Expressions
{
    get
    {
        return m_Expressions;
    }
}
public List<VFXExpression> FlattenedExpressions
{
    get
    {
        return m_FlattenedExpressions;
    }
}
public Dictionary<VFXExpression, VFXExpression> ExpressionsToReduced
{
    get
    {
        return m_ExpressionsToReduced;
    }
}

private HashSet<VFXExpression> m_Expressions = new HashSet<VFXExpression>();
private Dictionary<VFXExpression, VFXExpression> m_ExpressionsToReduced = new Dictionary<VFXExpression, VFXExpression>();
private List<VFXExpression> m_FlattenedExpressions = new List<VFXExpression>();
private Dictionary<VFXExpression, ExpressionData> m_ExpressionsData = new Dictionary<VFXExpression, ExpressionData>();
private Dictionary<VFXContext, VFXExpressionMapper> m_ContextsToCPUExpressions = new Dictionary<VFXContext, VFXExpressionMapper>();
private Dictionary<VFXContext, VFXExpressionMapper> m_ContextsToGPUExpressions = new Dictionary<VFXContext, VFXExpressionMapper>();

        //private Dictionary<VFXExpression, VFXExpression> m_CPUToGPUConversion = new Dictionary<VFXExpression, VFXExpression>(); 
}
}
