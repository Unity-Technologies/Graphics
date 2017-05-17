using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
	class VFXExpressionCompiler
	{
        public VFXExpressionCompiler()
		{}
		
		public void CompileExpressions(VFXGraph graph)
		{
            var models = new HashSet<Object>();
            graph.CollectDependencies(models);
            var contexts = models.OfType<VFXContext>();

            var expressions = new HashSet<VFXExpression>();
            var slotsToExpressions = new Dictionary<VFXSlot, VFXExpression>();
            foreach (var context in contexts.ToArray())
            {
                expressions.UnionWith(context.ExpressionContext.AllReduced());
                
                models.Clear();
                context.CollectDependencies(models);

                var kvps = models.OfType<VFXSlot>()
                    .Where(s => s.IsMasterSlot())
                    .SelectMany(s => s.GetExpressionSlots())
                    .Select(s => new KeyValuePair<VFXSlot,VFXExpression>(s,s.GetExpression()));

                foreach (var kvp in kvps)
                    slotsToExpressions.Add(kvp.Key,kvp.Value);
            }
            
            Debug.Log(string.Format("RECOMPILE EXPRESSION GRAPH - NB EXPRESSIONS: {0} - NB SLOTS: {1}",expressions.Count,slotsToExpressions.Count));	
		}
	}
	
}