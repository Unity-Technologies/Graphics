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

        private static bool IsHighlightedSlot(VFXSlot slot)
        {
            var owner = slot.owner;
            if (slot.GetExpression() == null)
                return false;

            if (owner is VFXOperator && slot.direction == VFXSlot.Direction.kOutput) // output to operators
                return true;

            var topOwner = slot.GetTopMostParent().owner;
            if (topOwner is VFXBlock && slot.direction == VFXSlot.Direction.kInput && slot.HasLink()) // linked inputs to blocks
                return true;

            return false;
        }

        public static void DebugExpressionGraph(VFXGraph graph)
        {
            var objs = new HashSet<UnityEngine.Object>();
            graph.CollectDependencies(objs);

            var mainSlots = new HashSet<VFXSlot>(objs.OfType<VFXSlot>()
                .Where(s => IsHighlightedSlot(s))).Select(s => s.GetTopMostParent());

            var mainExpressions = new Dictionary<VFXExpression, List<VFXSlot>>();
            foreach (var slot in mainSlots)
            {
                var expr = slot.GetExpression();
                if (mainExpressions.ContainsKey(expr))
                    mainExpressions[expr].Add(slot);
                else
                {
                    var list = new List<VFXSlot>();
                    list.Add(slot);
                    mainExpressions[expr] = list;
                }
            }

            var expressions = new HashSet<VFXExpression>();

            foreach (var exp in mainExpressions.Keys)
                CollectExpressions(exp, expressions);

            DotGraph dotGraph = new DotGraph();

            var expressionsToDot = new Dictionary<VFXExpression, DotNode>();
            foreach (var exp in expressions)
            {
                var dotNode = new DotNode();

                string name = exp.GetType().Name;
                string valueStr = GetExpressionValue(exp);
                if (!string.IsNullOrEmpty(valueStr))
                    name += string.Format(" ({0})", valueStr);

                dotNode.attributes[DotAttribute.Shape] = DotShape.Box;
                if (mainExpressions.ContainsKey(exp))
                {
                    string allOwnersStr = string.Empty;
                    bool belongToBlock = false;
                    foreach (var slot in mainExpressions[exp])
                    {
                        allOwnersStr += string.Format("\n{0} - {1}", slot.owner.GetType().Name, slot.property.name);
                        belongToBlock |= slot.owner is VFXBlock;
                    }

                    name += string.Format("{0}", allOwnersStr);

                    dotNode.attributes[DotAttribute.Style] = DotStyle.Filled;
                    dotNode.attributes[DotAttribute.Color] = belongToBlock ? DotColor.Cyan : DotColor.Green;
                }

                dotNode.Label = name;

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
