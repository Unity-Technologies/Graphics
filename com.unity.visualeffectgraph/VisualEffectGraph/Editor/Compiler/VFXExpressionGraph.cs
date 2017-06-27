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
        public enum Target
        {
            CPU,
            GPU,
        }

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

        public void CompileExpressions(VFXGraph graph, VFXExpressionContextOption options)
        {
            Profiler.BeginSample("CompileExpressionGraph");

            try
            {
                m_Expressions.Clear();
                m_CPUExpressionsToReduced.Clear();
                m_GPUExpressionsToReduced.Clear();
                m_FlattenedExpressions.Clear();
                m_ExpressionsData.Clear();
                m_ContextsToCPUExpressions.Clear();
                m_ContextsToGPUExpressions.Clear();

                var models = new HashSet<Object>();
                graph.CollectDependencies(models);
                var contexts = models.OfType<VFXContext>();

                HashSet<VFXExpression> cpuExpressions = new HashSet<VFXExpression>();
                HashSet<VFXExpression> gpuExpressions = new HashSet<VFXExpression>();

                var cpuExpressionContext = new VFXExpression.Context(options);
                var gpuExpressionContext = new VFXExpression.Context(options | VFXExpressionContextOption.GPUDataTransformation);

                foreach (var context in contexts)
                {
                    var cpuMapper = context.GetCPUExpressions();
                    var gpuMapper = context.GetGPUExpressions();

                    if (cpuMapper != null)
                    {
                        ProcessMapper(cpuMapper, cpuExpressionContext, cpuExpressions);
                        m_ContextsToCPUExpressions.Add(context, cpuMapper);
                    }

                    if (gpuMapper != null)
                    {
                        ProcessMapper(gpuMapper, gpuExpressionContext, gpuExpressions);
                        m_ContextsToGPUExpressions.Add(context, gpuMapper);
                    }
                }

                cpuExpressionContext.Compile();
                gpuExpressionContext.Compile();

                foreach (var exp in cpuExpressionContext.RegisteredExpressions)
                    m_CPUExpressionsToReduced.Add(exp, cpuExpressionContext.GetReduced(exp));

                foreach (var exp in gpuExpressionContext.RegisteredExpressions)
                    m_GPUExpressionsToReduced.Add(exp, gpuExpressionContext.GetReduced(exp));

                // TODO Transform all not compatible CPU data to GPU data by inserting expressions in the graph
                // Here ...

                m_Expressions.UnionWith(cpuExpressionContext.BuildAllReduced());
                m_Expressions.UnionWith(gpuExpressionContext.BuildAllReduced());

                // flatten
                foreach (var exp in m_CPUExpressionsToReduced.Values)
                    AddExpressionDataRecursively(m_ExpressionsData, exp);

                foreach (var exp in m_GPUExpressionsToReduced.Values)
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

                Debug.Log(string.Format("RECOMPILE EXPRESSION GRAPH - NB EXPRESSIONS: {0} - NB CPU END EXPRESSIONS: {1} - NB GPU END EXPRESSIONS: {2}", m_Expressions.Count, m_CPUExpressionsToReduced.Count, m_GPUExpressionsToReduced.Count));
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

        public VFXExpression GetReduced(VFXExpression exp, Target target)
        {
            VFXExpression reduced;
            var expressionToReduced = target == Target.GPU ? m_GPUExpressionsToReduced : m_CPUExpressionsToReduced;
            expressionToReduced.TryGetValue(exp, out reduced);
            return reduced;
        }

        public VFXExpressionMapper BuildCPUMapper(VFXContext context)
        {
            return BuildMapper(context, m_ContextsToCPUExpressions, Target.CPU);
        }

        public VFXExpressionMapper BuildGPUMapper(VFXContext context)
        {
            return BuildMapper(context, m_ContextsToGPUExpressions, Target.GPU);
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

        private VFXExpressionMapper BuildMapper(VFXContext context, Dictionary<VFXContext, VFXExpressionMapper> dictionnary, Target target)
        {
            VFXExpression.Flags check = target == Target.GPU ? VFXExpression.Flags.InvalidOnGPU | VFXExpression.Flags.PerElement : VFXExpression.Flags.InvalidOnCPU;

            VFXExpressionMapper outMapper = new VFXExpressionMapper();
            VFXExpressionMapper inMapper;

            dictionnary.TryGetValue(context, out inMapper);

            if (inMapper != null)
            {
                foreach (var exp in inMapper.expressions)
                {
                    var reduced = GetReduced(exp, target);
                    if (reduced.Is(check))
                        throw new InvalidOperationException(string.Format("The expression {0} is not valid as it have the invalid flag: {1}", reduced, check));

                    var mappedDataList = inMapper.GetData(exp);
                    foreach (var mappedData in mappedDataList)
                        outMapper.AddExpression(reduced, mappedData);
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

        public Dictionary<VFXExpression, VFXExpression> GPUExpressionsToReduced
        {
            get
            {
                return m_GPUExpressionsToReduced;
            }
        }

        public Dictionary<VFXExpression, VFXExpression> CPUExpressionsToReduced
        {
            get
            {
                return m_CPUExpressionsToReduced;
            }
        }

        private HashSet<VFXExpression> m_Expressions = new HashSet<VFXExpression>();
        private Dictionary<VFXExpression, VFXExpression> m_CPUExpressionsToReduced = new Dictionary<VFXExpression, VFXExpression>();
        private Dictionary<VFXExpression, VFXExpression> m_GPUExpressionsToReduced = new Dictionary<VFXExpression, VFXExpression>();
        private List<VFXExpression> m_FlattenedExpressions = new List<VFXExpression>();
        private Dictionary<VFXExpression, ExpressionData> m_ExpressionsData = new Dictionary<VFXExpression, ExpressionData>();
        private Dictionary<VFXContext, VFXExpressionMapper> m_ContextsToCPUExpressions = new Dictionary<VFXContext, VFXExpressionMapper>();
        private Dictionary<VFXContext, VFXExpressionMapper> m_ContextsToGPUExpressions = new Dictionary<VFXContext, VFXExpressionMapper>();

        //private Dictionary<VFXExpression, VFXExpression> m_CPUToGPUConversion = new Dictionary<VFXExpression, VFXExpression>();
    }
}
