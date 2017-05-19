using System.Collections.Generic;
using System.Linq;
using UnityEditor.Dot;
using UnityEngine;
using System.Diagnostics;

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

        private static string GetRecursiveName(VFXSlot slot)
        {
            string name = slot.property.name;
            while (slot.GetParent() != null)
            {
                slot = slot.GetParent();
                name = slot.property.name + "." + name;
            }
            return name;
        }

        public static void DebugExpressionGraph(VFXGraph graph, VFXExpression.Context.ReductionOption option)
        {
            var expressionGraph = new VFXExpressionGraph();
            expressionGraph.CompileExpressions(graph, option);

            var mainExpressions = new Dictionary<VFXExpression, List<VFXSlot>>();
            foreach (var kvp in expressionGraph.SlotsToExpressions)
            {
                var slot = kvp.Key;
                var expr = kvp.Value;

                if (mainExpressions.ContainsKey(expr))
                    mainExpressions[expr].Add(slot);
                else
                {
                    var list = new List<VFXSlot>();
                    list.Add(slot);
                    mainExpressions[expr] = list;
                }
            }

            var expressions = expressionGraph.Expressions;

            DotGraph dotGraph = new DotGraph();

            var expressionsToDot = new Dictionary<VFXExpression, DotNode>();
            foreach (var exp in expressions)
            {
                var dotNode = new DotNode();

                string name = exp.GetType().Name;
                name += " " + exp.ValueType.ToString();
                string valueStr = GetExpressionValue(exp);
                if (!string.IsNullOrEmpty(valueStr))
                    name += string.Format(" ({0})", valueStr);

                dotNode.attributes[DotAttribute.Shape] = DotShape.Box;
                if (mainExpressions.ContainsKey(exp))
                {
                    string allOwnersStr = string.Empty;
                    //bool belongToBlock = false;
                    foreach (var slot in mainExpressions[exp])
                    {
                        var topOwner = slot.GetMasterSlot().owner;
                        allOwnersStr += string.Format("\n{0} - {1}", topOwner.GetType().Name, GetRecursiveName(slot));
                        // belongToBlock |= topOwner is VFXBlock;
                    }

                    name += string.Format("{0}", allOwnersStr);

                    dotNode.attributes[DotAttribute.Style] = DotStyle.Filled;
                    dotNode.attributes[DotAttribute.Color] = /*belongToBlock ?*/ DotColor.Cyan /*: DotColor.Green*/;
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

            var basePath = Application.dataPath;
            basePath = basePath.Replace("/Assets", "");
            basePath = basePath.Replace("/", "\\");

            var outputfile = basePath + "\\GraphViz\\output\\expGraph.dot";
            dotGraph.OutputToDotFile(outputfile);

            var proc = new Process();
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.FileName = "C:\\Windows\\system32\\cmd.exe";
            var path = basePath + "\\GraphViz\\Postbuild.bat";
            proc.StartInfo.Arguments = "/c" + path + " \"" + outputfile + "\"";
            proc.EnableRaisingEvents = true;
            proc.Start();
        }

        private static string GetExpressionValue(VFXExpression exp)
        {
            if (exp is VFXValue)
            {
                var content = exp.GetContent();
                return content == null ? "null" : content.ToString();
            }
            if (exp is VFXBuiltInExpression) return ((VFXBuiltInExpression)exp).Operation.ToString();
            if (exp is VFXAttributeExpression) return ((VFXAttributeExpression)exp).attributeName;

            return string.Empty;
        }
    }
}
