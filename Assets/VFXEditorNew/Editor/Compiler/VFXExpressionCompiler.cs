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
        }

        public VFXExpressionGraph()
		{}

        private void AddExpressionsToContext(HashSet<VFXExpression> expressions, IVFXSlotContainer slotContainer)
        {
            int nbSlots = slotContainer.GetNbInputSlots();
            for (int i = 0; i < nbSlots; ++i)
            {
                var slot = slotContainer.GetInputSlot(i);
                slot.GetExpressions(expressions);
            }
        }

        private VFXExpression.Context CreateLocalExpressionContext(VFXContext context,VFXExpression.Context.ReductionOption options)
        {
            var expressionContext = new VFXExpression.Context(options);
            var expressions = new HashSet<VFXExpression>();

            // First add context slots
            AddExpressionsToContext(expressions, context);

            // Then block slots
            foreach (var child in context.children)
                AddExpressionsToContext(expressions, child);

            foreach (var exp in expressions)
                expressionContext.RegisterExpression(exp);

            return expressionContext;
        } 

        private void AddExpressionDataRecursively(Dictionary<VFXExpression,ExpressionData> dst, VFXExpression exp,int depth = 0)
        {
            ExpressionData data;
            if (!dst.TryGetValue(exp, out data) || data.depth < depth)
            {
                data.depth = depth;
                dst[exp] = data;
                foreach (var parent in exp.Parents)
                    AddExpressionDataRecursively(dst, parent, depth + 1);
            }
        }

		public void CompileExpressions(VFXGraph graph,bool constantFolding)
		{
            Profiler.BeginSample("CompileExpressionGraph");

            try
            {
                m_Expressions.Clear();
                m_SlotToExpressions.Clear();
                m_FlattenedExpressions.Clear();

                var models = new HashSet<Object>();
                graph.CollectDependencies(models);
                var contexts = models.OfType<VFXContext>();

                var options = constantFolding ? VFXExpression.Context.ReductionOption.ConstantFolding : VFXExpression.Context.ReductionOption.CPUReduction;

                foreach (var context in contexts.ToArray())
                {
                    var expressionContext = CreateLocalExpressionContext(context, options);
                    expressionContext.Compile();

                    m_Expressions.UnionWith(expressionContext.AllReduced());

                    models.Clear();
                    context.CollectDependencies(models);

                    var kvps = models.OfType<VFXSlot>()
                        .Where(s => s.IsMasterSlot())
                        .SelectMany(s => s.GetExpressionSlots())
                        .Select(s => new KeyValuePair<VFXSlot, VFXExpression>(s, expressionContext.GetReduced(s.GetExpression())));

                    foreach (var kvp in kvps)
                        m_SlotToExpressions.Add(kvp.Key, kvp.Value);
                }

                // flatten
                var expressionData = new Dictionary<VFXExpression,ExpressionData>();
                foreach (var exp in m_SlotToExpressions.Values)
                    AddExpressionDataRecursively(expressionData, exp);

                var sortedList = expressionData.Where(kvp => !kvp.Key.Is(VFXExpression.Flags.PerElement)).ToList();
                sortedList.Sort((kvpA, kvpB) => kvpB.Value.depth.CompareTo(kvpA.Value.depth));
                m_FlattenedExpressions = sortedList.Select(kvp => kvp.Key).ToList();

                Debug.Log("---- Expression list");
                for (int i = 0; i < m_FlattenedExpressions.Count; ++i)
                    Debug.Log(string.Format("{0}\t\t{1}", i, m_FlattenedExpressions[i].GetType().Name));

                // Keep only non per element expressions in the graph
                m_Expressions.RemoveWhere(e => e.Is(VFXExpression.Flags.PerElement));

                Debug.Log(string.Format("RECOMPILE EXPRESSION GRAPH - NB EXPRESSIONS: {0} - NB SLOTS: {1}", m_Expressions.Count, m_SlotToExpressions.Count));
            }
            finally
            {
                Profiler.EndSample();
            }
		}

        private HashSet<VFXExpression> m_Expressions = new HashSet<VFXExpression>();
        private Dictionary<VFXSlot, VFXExpression> m_SlotToExpressions = new Dictionary<VFXSlot, VFXExpression>();
        private List<VFXExpression> m_FlattenedExpressions = new List<VFXExpression>();
	}
	
}