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

            var startExpressions = new Dictionary<VFXExpression,VFXSlot>(objs.OfType<VFXSlot>()
                .Where(s => s.owner != null && s.direction == VFXSlot.Direction.kOutput && s.GetExpression() != null) // only master output slots with valid expression
                .ToDictionary(s => s.GetExpression()));
            var expressions = new HashSet<VFXExpression>();

            foreach (var exp in startExpressions.Keys)
                CollectExpressions(exp, expressions);

            DotGraph dotGraph = new DotGraph();

            var expressionsToDot = new Dictionary<VFXExpression, DotNode>();
            foreach (var exp in expressions)
            {
                string name = exp.GetType().Name;
                string valueStr = GetExpressionValue(exp);
                if (!string.IsNullOrEmpty(valueStr))
                    name += string.Format(" ({0})", valueStr);
                if (startExpressions.ContainsKey(exp))
                    name += string.Format(" ({0})", startExpressions[exp].m_Owner.GetType().Name);

                var dotNode = new DotNode(name);

                dotNode.attributes[DotAttribute.Shape] = DotShape.Box;
                if (startExpressions.ContainsKey(exp)) // it's an output from slot
                {
                    dotNode.attributes[DotAttribute.Style] = DotStyle.Filled;
                    dotNode.attributes[DotAttribute.Color] = DotColor.Green;
                }

                expressionsToDot[exp] = dotNode;
                dotGraph.AddElement(dotNode);
            }
          
            foreach (var exp in expressionsToDot)
            {
                var parents = exp.Key.Parents;
                for (int i = 0; i < parents.Length; ++i)
                {
                    var dotEdge = new DotEdge(expressionsToDot[parents[i]], exp.Value);
                    if (parents.Length > 1)
                        dotEdge.attributes[DotAttribute.HeadLabel] = i.ToString();
                    dotGraph.AddElement(dotEdge);
                }
            }

            dotGraph.OutputToDotFile("d:\\expGraph.dot");
        }

        private static string GetExpressionValue(VFXExpression exp)
        {
            // TODO We should have a way in VFXValue to retrieve an object representing the value
            if (exp is VFXValueFloat) return ((VFXValueFloat)exp).GetContent<float>().ToString();
            if (exp is VFXValueFloat2) return ((VFXValueFloat2)exp).GetContent<Vector2>().ToString();
            if (exp is VFXValueFloat3) return ((VFXValueFloat3)exp).GetContent<Vector3>().ToString();
            if (exp is VFXValueFloat4) return ((VFXValueFloat4)exp).GetContent<Vector4>().ToString();

            return string.Empty;
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
