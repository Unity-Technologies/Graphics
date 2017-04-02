using System.Collections.Generic;
using System.Linq;
using UnityEditor.Dot;
using UnityEngine;

namespace UnityEditor.VFX
{
    static class DotGraphOutput
    {
        public static void Test()
        {
            DotGraph graph = new DotGraph();
            graph.AddElement(new DotEdge(new DotNode("node 1"), new DotNode("node 2")));
            graph.OutputToDotFile("d:\\testDot.dot");
        }

        public static void DebugExpressionGraph(VFXGraph graph)
        {
            var objs = new HashSet<UnityEngine.Object>();
            graph.CollectDependencies(objs);

            var startExpressions = new HashSet<VFXExpression>(objs.OfType<VFXSlot>()
                .Where(s => s.owner != null && s.direction == VFXSlot.Direction.kOutput) // only master output slots
                .Select(s => s.expression));
            var expressions = new HashSet<VFXExpression>();

            foreach (var exp in startExpressions)
                CollectExpressions(exp, expressions);

            var expressionsToDot = new Dictionary<VFXExpression, DotNode>();
            foreach (var exp in expressions)
            {
                var dotNode = new DotNode(exp.GetType().Name);
                expressionsToDot[exp] = dotNode;
            }

            DotGraph dotGraph = new DotGraph();
            foreach (var exp in expressionsToDot)
            {
                var parents = exp.Key.Parents;
                for (int i = 0; i < parents.Length; ++i)
                {
                    var dotEdge = new DotEdge(expressionsToDot[parents[i]], exp.Value);
                    if (parents.Length > 1)
                        dotEdge.attributes["headlabel"] = i.ToString();
                    dotGraph.AddElement(dotEdge);
                }
            }

            dotGraph.OutputToDotFile("d:\\expGraph.dot");
        }

        private static void CollectExpressions(VFXExpression exp,HashSet<VFXExpression> expressions)
        {
            if (/*exp != null &&*/ !expressions.Contains(exp))
            {
                expressions.Add(exp);
                foreach (var parent in exp.Parents)
                    //if (parent != null)
                        CollectExpressions(parent, expressions);
            }
        }
    }
}
