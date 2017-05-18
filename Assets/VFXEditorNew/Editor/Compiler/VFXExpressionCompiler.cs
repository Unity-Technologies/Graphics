using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
	class VFXExpressionCompiler
	{
        public VFXExpressionCompiler()
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

		public void CompileExpressions(VFXGraph graph,bool constantFolding)
		{
            Profiler.BeginSample("CompileExpressionGraph");

            try
            {
                var models = new HashSet<Object>();
                graph.CollectDependencies(models);
                var contexts = models.OfType<VFXContext>();

                var expressions = new HashSet<VFXExpression>();
                var slotsToExpressions = new Dictionary<VFXSlot, VFXExpression>();

                var options = VFXExpression.Context.ReductionOption.CPUReduction;
                if (constantFolding)
                    options |= VFXExpression.Context.ReductionOption.ConstantFolding;

                foreach (var context in contexts.ToArray())
                {
                    var expressionContext = CreateLocalExpressionContext(context, options);
                    expressionContext.Compile();

                    expressions.UnionWith(expressionContext.AllReduced());

                    models.Clear();
                    context.CollectDependencies(models);

                    var kvps = models.OfType<VFXSlot>()
                        .Where(s => s.IsMasterSlot())
                        .SelectMany(s => s.GetExpressionSlots())
                        .Select(s => new KeyValuePair<VFXSlot, VFXExpression>(s, s.GetExpression()));

                    foreach (var kvp in kvps)
                        slotsToExpressions.Add(kvp.Key, kvp.Value);
                }

                // Keep only non per element expressions in the graph
                expressions.RemoveWhere(e => e.Is(VFXExpression.Flags.PerElement));

                Debug.Log(string.Format("RECOMPILE EXPRESSION GRAPH - NB EXPRESSIONS: {0} - NB SLOTS: {1}", expressions.Count, slotsToExpressions.Count));
            }
            finally
            {
                Profiler.EndSample();
            }
		}
	}
	
}